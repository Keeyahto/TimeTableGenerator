using System.Diagnostics;
using System.Text.Json.Nodes;
using ScheduleSolver.Core.Contracts;
using ScheduleSolver.Core.Diagnostics;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Rules;
using ScheduleSolver.Core.Modes;
using ScheduleSolver.Core.Solver;
using ScheduleSolver.Core.Validation;

namespace ScheduleSolver.Core;

public static class SolverApplication
{
    private static readonly HashSet<string> NonBlockingIssueCodes = new(StringComparer.Ordinal)
    {
        "SCHEMA_FILE_NOT_FOUND",
        "R00_MULTIPLE_TEACHER_OPTIONS",
        "R00_MULTIPLE_ROOM_OPTIONS",
        "R00_VACANT_PLACEHOLDER",
    };

    public static async Task<SolverRunResult> RunAsync(SolverRunOptions options, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        ParsedInput input;
        try
        {
            input = InputLoader.Load(options.InputPath);
        }
        catch (Exception ex)
        {
            var errorReport = new JsonObject
            {
                ["schema_version"] = "0.2",
                ["status"] = "ERROR",
                ["cp_sat_status"] = "NOT_RUN",
                ["warnings"] = new JsonArray(new JsonObject
                {
                    ["code"] = "INPUT_LOAD_FAILED",
                    ["message"] = ex.Message,
                }),
            };
            sw.Stop();
            errorReport["wall_time_seconds"] = sw.Elapsed.TotalSeconds;
            await DiagnosticReportBuilder.WriteAsync(options.OutputPath, errorReport, ct);
            return new SolverRunResult { ExitCode = 1, Status = "ERROR" };
        }

        using (input)
        {
            var registry = RuleRegistry.CreateDefault(input.Document);
            var contracts = ContractsPaths.FromRepo();

            var issues = new List<ValidationIssue>();
            issues.AddRange(StructuralValidator.Validate(input));

            var schemaPath = SchemaVersionValidator.SchemaPathForVersion(input.SchemaVersion, contracts);
            var schemaMissing = SchemaVersionValidator.MissingSchemaFileIssue(schemaPath);
            if (schemaMissing is not null)
            {
                issues.Add(schemaMissing);
            }

            if (!SchemaVersionValidator.VersionMatchesSchemaFile(input.SchemaVersion, schemaPath))
            {
                issues.Add(new ValidationIssue(
                    "SCHEMA_VERSION_MISMATCH",
                    $"schema_version '{input.SchemaVersion}' does not match {Path.GetFileName(schemaPath)}."));
            }

            if (!HasBlockingIssues(issues))
            {
                issues.AddRange(PrecheckValidator.Validate(input));
            }

            var report = DiagnosticReportBuilder.CreateBase(
                status: "OK",
                registry: registry,
                wallTimeSeconds: 0);

            AttachNonBlockingWarnings(report, issues);

            if (!string.IsNullOrWhiteSpace(options.DatasetVariant))
            {
                var buckets = report["rules_by_status"] as JsonObject ?? new();
                buckets["dataset_variant"] = options.DatasetVariant;
                report["rules_by_status"] = buckets;
            }

            if (HasBlockingIssues(issues))
            {
                DiagnosticReportBuilder.AddValidationErrors(report, BlockingIssues(issues));
                sw.Stop();
                report["wall_time_seconds"] = sw.Elapsed.TotalSeconds;
                await DiagnosticReportBuilder.WriteAsync(options.OutputPath, report, ct);
                return new SolverRunResult { ExitCode = 1, Status = "ERROR" };
            }

            if (options.Mode is SolverMode.Solve or SolverMode.Diagnostic)
            {
                var priorAllowLarge = Environment.GetEnvironmentVariable("SCHED_ALLOW_LARGE_MODEL");
                if (options.AllowLargeModel)
                {
                    Environment.SetEnvironmentVariable("SCHED_ALLOW_LARGE_MODEL", "1");
                }

                try
                {
                    var budgetIssue = ModelBudgetGuard.Check(input);
                    if (budgetIssue is not null)
                    {
                        issues.Add(new ValidationIssue("MODEL_TOO_LARGE", budgetIssue, "lesson_demands"));
                    }
                }
                finally
                {
                    Environment.SetEnvironmentVariable("SCHED_ALLOW_LARGE_MODEL", priorAllowLarge);
                }

                if (HasBlockingIssues(issues))
                {
                    DiagnosticReportBuilder.AddValidationErrors(report, BlockingIssues(issues));
                    sw.Stop();
                    report["wall_time_seconds"] = sw.Elapsed.TotalSeconds;
                    await DiagnosticReportBuilder.WriteAsync(options.OutputPath, report, ct);
                    return new SolverRunResult { ExitCode = 1, Status = "ERROR" };
                }
            }

            switch (options.Mode)
            {
                case SolverMode.Validate:
                    report["status"] = "VALIDATED";
                    break;

                case SolverMode.Profile:
                {
                    var profile = ProfileMetrics.Compute(input);
                    if (options.AllowLargeModel || ProfileMetrics.DemandCount(input) <= ModelBudgetGuard.DefaultMaxDemands)
                    {
                        var modelStats = ModelBuildStats.Capture(input, registry);
                        profile["model_stats"] = ModelBuildStats.ToJson(modelStats);
                    }

                    ProfileMetrics.AttachToRulesByStatus(report, profile);
                    break;
                }

                case SolverMode.Diagnostic:
                case SolverMode.Solve:
                    var limit = ResolveTimeLimit(input, options);
                    SolveModeRunner.ApplyToReport(report, input, registry, limit);
                    if (options.Mode == SolverMode.Diagnostic)
                    {
                        report["status"] = MapDiagnosticStatus(report["status"]?.ToString());
                    }

                    break;
            }

            sw.Stop();
            if (options.Mode is not SolverMode.Solve and not SolverMode.Diagnostic)
            {
                report["wall_time_seconds"] = sw.Elapsed.TotalSeconds;
            }

            await DiagnosticReportBuilder.WriteAsync(options.OutputPath, report, ct);

            var status = report["status"] is JsonValue statusValue
                ? statusValue.GetValue<string>() ?? "OK"
                : "OK";
            var exitCode = status switch
            {
                "ERROR" => 1,
                "INFEASIBLE" => options.Mode == SolverMode.Diagnostic ? 0 : 1,
                _ => 0,
            };
            return new SolverRunResult { ExitCode = exitCode, Status = status };
        }
    }

    private static bool HasBlockingIssues(IEnumerable<ValidationIssue> issues) =>
        issues.Any(i => !NonBlockingIssueCodes.Contains(i.Code));

    private static IEnumerable<ValidationIssue> BlockingIssues(IEnumerable<ValidationIssue> issues) =>
        issues.Where(i => !NonBlockingIssueCodes.Contains(i.Code));

    private static void AttachNonBlockingWarnings(JsonObject report, IEnumerable<ValidationIssue> issues)
    {
        var dataQuality = report["data_quality_warnings"] as JsonArray ?? new JsonArray();
        foreach (var issue in issues.Where(i => NonBlockingIssueCodes.Contains(i.Code)))
        {
            dataQuality.Add(new JsonObject
            {
                ["code"] = issue.Code,
                ["message"] = issue.Message,
                ["path"] = issue.Path,
            });
        }

        report["data_quality_warnings"] = dataQuality;
    }

    private static string MapDiagnosticStatus(string? solveStatus) =>
        solveStatus switch
        {
            "OK" => "DIAGNOSTIC",
            "INFEASIBLE" => "INFEASIBLE",
            "ERROR" => "DIAGNOSTIC",
            _ => solveStatus ?? "DIAGNOSTIC",
        };

    private static int ResolveTimeLimit(ParsedInput input, SolverRunOptions options)
    {
        if (options.TimeLimitSec > 0)
        {
            return options.TimeLimitSec;
        }

        if (input.Root.TryGetProperty("solver_config", out var cfg)
            && cfg.TryGetProperty("time_limit_sec", out var tl)
            && tl.TryGetInt32(out var sec)
            && sec > 0)
        {
            return sec;
        }

        return 30;
    }
}
