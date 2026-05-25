using System.Text.Json.Nodes;
using Google.OrTools.Sat;
using ScheduleSolver.Core.Diagnostics;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;
using ScheduleSolver.Core.Rules;
using ScheduleSolver.Core.Solver;

namespace ScheduleSolver.Core.Modes;

public static class SolveModeRunner
{
    public static void ApplyToReport(
        JsonObject report,
        ParsedInput input,
        RuleRegistry registry,
        int timeLimitSec)
    {
        var build = SchedulingModelBuild.Create(input, registry);
        var result = CpSatSolveService.Solve(build, timeLimitSec);

        report["status"] = MapStatus(result.Status);
        report["cp_sat_status"] = result.Status.ToString();
        report["objective_value"] = result.ObjectiveValue;
        report["wall_time_seconds"] = result.WallTimeSeconds;
        report["enabled_rules"] = new JsonArray(
            build.EnforcedRuleIds.OrderBy(x => x, StringComparer.Ordinal)
                .Select(id => JsonValue.Create(id))
                .Cast<JsonNode?>()
                .ToArray());
        report["unscheduled_lessons"] = new JsonArray(
            result.Unscheduled.Cast<JsonNode?>().ToArray());
        report["schedule"] = new JsonArray(result.Assignments.Cast<JsonNode?>().ToArray());
        report["relaxed_hard_violations"] = new JsonArray(
            result.RelaxedViolations.Cast<JsonNode?>().ToArray());
        report["soft_violations"] = new JsonArray(
            result.SoftViolations.Cast<JsonNode?>().ToArray());

        if (result.Unscheduled.Count > 0)
        {
            (report["relaxed_hard_violations"] as JsonArray)?.Add(new JsonObject
            {
                ["rule_id"] = "R07",
                ["count"] = result.Unscheduled.Count,
            });
        }

        var buckets = report["rules_by_status"] as JsonObject ?? new JsonObject();
        buckets["enforced_in_model"] = new JsonArray(
            build.EnforcedRuleIds.OrderBy(x => x, StringComparer.Ordinal)
                .Select(id => JsonValue.Create(id))
                .Cast<JsonNode?>()
                .ToArray());
        report["rules_by_status"] = buckets;
    }

    private static string MapStatus(CpSolverStatus status) =>
        status switch
        {
            CpSolverStatus.Optimal => "OK",
            CpSolverStatus.Feasible => "OK",
            CpSolverStatus.Infeasible => "INFEASIBLE",
            _ => "ERROR",
        };
}
