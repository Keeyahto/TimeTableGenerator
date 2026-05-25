using System.Text.Json;
using System.Text.Json.Nodes;
using ScheduleSolver.Core.Rules;
using ScheduleSolver.Core.Validation;

namespace ScheduleSolver.Core.Diagnostics;

public static class DiagnosticReportBuilder
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static JsonObject CreateBase(
        string status,
        RuleRegistry registry,
        string cpSatStatus = "NOT_RUN",
        double wallTimeSeconds = 0)
    {
        var buckets = registry.RulesByStatusBuckets();
        var warnings = registry.StubRuleWarnings()
            .Select(id => new JsonObject
            {
                ["code"] = "RULE_NOT_ENFORCED_YET",
                ["message"] = $"Rule {id} is registered but not enforced in CP-SAT yet.",
            })
            .Cast<JsonNode>()
            .ToArray();

        return new JsonObject
        {
            ["schema_version"] = "0.2",
            ["status"] = status,
            ["cp_sat_status"] = cpSatStatus,
            ["objective_value"] = null,
            ["best_objective_bound"] = null,
            ["gap"] = null,
            ["wall_time_seconds"] = wallTimeSeconds,
            ["enabled_rules"] = new JsonArray(
                registry.All
                    .Where(r => r.DefaultStatus == EnforcementStatus.Enforced)
                    .Select(r => JsonValue.Create(r.Id))
                    .Cast<JsonNode?>()
                    .ToArray()),
            ["rule_penalties"] = new JsonArray(),
            ["unscheduled_lessons"] = new JsonArray(),
            ["virtual_teachers_used"] = new JsonArray(),
            ["virtual_rooms_used"] = new JsonArray(),
            ["relaxed_hard_violations"] = new JsonArray(),
            ["soft_violations"] = new JsonArray(),
            ["data_quality_warnings"] = new JsonArray(),
            ["rules_by_status"] = JsonSerializer.SerializeToNode(buckets)?.AsObject() ?? new JsonObject(),
            ["schedule"] = null,
            ["warnings"] = new JsonArray(warnings),
        };
    }

    public static void AddValidationErrors(JsonObject report, IEnumerable<ValidationIssue> issues)
    {
        var warnings = report["warnings"] as JsonArray ?? new JsonArray();
        foreach (var issue in issues)
        {
            warnings.Add(new JsonObject
            {
                ["code"] = issue.Code,
                ["message"] = issue.Message,
                ["path"] = issue.Path,
            });
        }

        report["warnings"] = warnings;
        report["status"] = "ERROR";
    }

    public static async Task WriteAsync(string outputPath, JsonObject report, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(outputPath, report.ToJsonString(WriteOptions), ct);
    }
}
