using System.Diagnostics;
using System.Text.Json.Nodes;
using ScheduleSolver.Core.Contracts;
using ScheduleSolver.Core.Diagnostics;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Rules;
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
                report["status"] = "DIAGNOSTIC";
                report["cp_sat_status"] = "NOT_RUN";
                break;

            case SolverMode.Solve:
                report["status"] = "STUB";
                report["cp_sat_status"] = "NOT_RUN";
                (report["warnings"] as System.Text.Json.Nodes.JsonArray)?.Add(
                    new System.Text.Json.Nodes.JsonObject
                    {
                        ["code"] = "SOLVE_NOT_IMPLEMENTED",
                        ["message"] = "CP-SAT solve is planned for phase 1.",
                    });
                break;
        }

        sw.Stop();
        report["wall_time_seconds"] = sw.Elapsed.TotalSeconds;
        await DiagnosticReportBuilder.WriteAsync(options.OutputPath, report, ct);

        var status = report["status"] is JsonValue statusValue
            ? statusValue.GetValue<string>() ?? "OK"
            : "OK";
        var exitCode = status == "ERROR" ? 1 : 0;
        return new SolverRunResult { ExitCode = exitCode, Status = status };
    }
}
