using System.Diagnostics;
using System.Text.Json.Nodes;
using ScheduleSolver.Core.Contracts;
using ScheduleSolver.Core.Diagnostics;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Rules;
using ScheduleSolver.Core.Modes;
using ScheduleSolver.Core.Validation;

namespace ScheduleSolver.Core;

public static class SolverApplication
{
    public static async Task<SolverRunResult> RunAsync(SolverRunOptions options, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        using var input = InputLoader.Load(options.InputPath);
        var registry = RuleRegistry.CreateDefault(input.Document);
        var contracts = ContractsPaths.FromRepo();

        var issues = new List<ValidationIssue>();
        issues.AddRange(StructuralValidator.Validate(input));

        var schemaPath = SchemaVersionValidator.SchemaPathForVersion(input.SchemaVersion, contracts);
        if (!SchemaVersionValidator.VersionMatchesSchemaFile(input.SchemaVersion, schemaPath))
        {
            issues.Add(new ValidationIssue(
                "SCHEMA_VERSION_MISMATCH",
                $"schema_version '{input.SchemaVersion}' does not match {Path.GetFileName(schemaPath)}."));
        }

        if (issues.Count == 0)
        {
            issues.AddRange(PrecheckValidator.Validate(input));
        }

        var report = DiagnosticReportBuilder.CreateBase(
            status: "OK",
            registry: registry,
            wallTimeSeconds: 0);

        if (!string.IsNullOrWhiteSpace(options.DatasetVariant))
        {
            var buckets = report["rules_by_status"] as System.Text.Json.Nodes.JsonObject ?? new();
            buckets["dataset_variant"] = options.DatasetVariant;
            report["rules_by_status"] = buckets;
        }

        if (issues.Count > 0)
        {
            DiagnosticReportBuilder.AddValidationErrors(report, issues);
            await DiagnosticReportBuilder.WriteAsync(options.OutputPath, report, ct);
            sw.Stop();
            return new SolverRunResult { ExitCode = 1, Status = "ERROR" };
        }

        switch (options.Mode)
        {
            case SolverMode.Validate:
                report["status"] = "VALIDATED";
                break;

            case SolverMode.Profile:
                ProfileMetrics.AttachToRulesByStatus(report, ProfileMetrics.Compute(input));
                break;

            case SolverMode.Diagnostic:
            case SolverMode.Solve:
                var limit = ResolveTimeLimit(input, options);
                SolveModeRunner.ApplyToReport(report, input, registry, limit);
                if (options.Mode == SolverMode.Diagnostic)
                {
                    report["status"] = report["status"]?.ToString() == "OK" ? "DIAGNOSTIC" : report["status"];
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
        var exitCode = status == "ERROR" ? 1 : 0;
        return new SolverRunResult { ExitCode = exitCode, Status = status };
    }

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
