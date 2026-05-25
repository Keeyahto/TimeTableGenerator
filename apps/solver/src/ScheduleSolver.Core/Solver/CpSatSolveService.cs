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
}

public static class CpSatSolveService
{
    public static CpSatSolveResult Solve(SchedulingModelBuild build, int timeLimitSec)
    {
        var solver = new CpSolver();
        solver.StringParameters = $"max_time_in_seconds:{timeLimitSec}";

        var status = build.Demands.Count == 0
            ? CpSolverStatus.Optimal
            : solver.Solve(build.Model);

        var assignments = new List<JsonObject>();
        var unscheduled = new List<JsonObject>();

        foreach (var d in build.Demands)
        {
            var scheduled = status is CpSolverStatus.Optimal or CpSolverStatus.Feasible
                            && solver.Value(d.Presence) > 0.5;
            if (scheduled)
            {
                var start = (int)solver.Value(d.Start);
                assignments.Add(new JsonObject
                {
                    ["demand_id"] = d.Demand.Id,
                    ["group_id"] = d.Demand.GroupId,
                    ["teacher_id"] = d.Demand.TeacherId,
                    ["room_id"] = d.Demand.RoomId,
                    ["start_index"] = start,
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

        double? objective = null;
        if (status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
        {
            objective = solver.ObjectiveValue;
        }

        return new CpSatSolveResult
        {
            Status = status,
            WallTimeSeconds = solver.WallTime(),
            ObjectiveValue = objective,
            Assignments = assignments,
            Unscheduled = unscheduled,
        };
    }

    public static IReadOnlyList<string> EnforcedModelRuleIds() =>
    [
        "R01", "R02", "R03", "R04", "R05", "R06", "R07", "R08", "R09",
    ];
}
