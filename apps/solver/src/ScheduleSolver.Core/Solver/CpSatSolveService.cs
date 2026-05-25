using System.Text.Json.Nodes;
using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Core.Solver;

public sealed class CpSatSolveResult
{
    public required CpSolverStatus Status { get; init; }
    public required double WallTimeSeconds { get; init; }
    public double? ObjectiveValue { get; init; }
    public required IReadOnlyList<JsonObject> Assignments { get; init; }
    public required IReadOnlyList<JsonObject> Unscheduled { get; init; }
    public required IReadOnlyList<JsonObject> RelaxedViolations { get; init; }
    public required IReadOnlyList<JsonObject> SoftViolations { get; init; }
}

public static class CpSatSolveService
{
    public static CpSatSolveResult Solve(SchedulingModelBuild build, int timeLimitSec)
    {
        var solver = new CpSolver();
        solver.StringParameters = $"max_time_in_seconds:{timeLimitSec}";

        var skipSolve = build.Demands.Count == 0;
        var status = skipSolve
            ? CpSolverStatus.Optimal
            : solver.Solve(build.Model);

        var assignments = new List<JsonObject>();
        var unscheduled = new List<JsonObject>();

        foreach (var row in build.StrandedDemands)
        {
            unscheduled.Add(new JsonObject
            {
                ["demand_id"] = row.Id,
                ["reason"] = "NO_CALENDAR_SLOTS",
            });
        }
        var relaxed = new List<JsonObject>();
        var soft = new List<JsonObject>();

        foreach (var d in build.Demands)
        {
            var scheduled = !skipSolve
                            && status is CpSolverStatus.Optimal or CpSolverStatus.Feasible
                            && solver.Value(d.Presence) > 0.5;
            if (scheduled)
            {
                assignments.Add(new JsonObject
                {
                    ["demand_id"] = d.Demand.Id,
                    ["group_id"] = d.Demand.GroupId,
                    ["teacher_id"] = d.Demand.TeacherId,
                    ["room_id"] = d.Demand.RoomId,
                    ["start_index"] = (int)solver.Value(d.Start),
                    ["duration_slots"] = d.Demand.DurationSlots,
                });
            }
            else
            {
                unscheduled.Add(new JsonObject
                {
                    ["demand_id"] = d.Demand.Id,
                    ["reason"] = "RELAXED_HARD_R07",
                });
            }
        }

        foreach (var v in build.Violations.Items)
        {
            if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
            {
                break;
            }

            if (skipSolve || solver.Value(v.Variable) < 0.5)
            {
                continue;
            }

            var entry = new JsonObject
            {
                ["rule_id"] = v.RuleId,
                ["label"] = v.Label,
                ["penalty"] = v.Penalty,
            };

            if (v.Class is RuleClass.SOFT_STRONG or RuleClass.SOFT_MEDIUM or RuleClass.SOFT_WEAK)
            {
                soft.Add(entry);
            }
            else if (v.RuleId != "R07")
            {
                relaxed.Add(entry);
            }
        }

        double? objective = null;
        if (status is CpSolverStatus.Optimal or CpSolverStatus.Feasible
            && build.Demands.Count > 0)
        {
            objective = solver.ObjectiveValue;
        }

        return new CpSatSolveResult
        {
            Status = status,
            WallTimeSeconds = skipSolve ? 0 : solver.WallTime(),
            ObjectiveValue = objective,
            Assignments = assignments,
            Unscheduled = unscheduled,
            RelaxedViolations = relaxed,
            SoftViolations = soft,
        };
    }
}
