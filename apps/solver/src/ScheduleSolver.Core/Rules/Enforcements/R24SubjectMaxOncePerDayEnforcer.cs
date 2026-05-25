using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R24SubjectMaxOncePerDayEnforcer : IRuleEnforcer
{
    public string RuleId => "R24";

    public void Apply(SchedulingBuildContext ctx)
    {
        var def = ctx.Registry.GetRequired("R24");
        var penalty = def.DefaultPenalty;
        var days = ctx.Indexer.Slots
            .Select(s => s.Day ?? "unknown")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var pool in ctx.Demands.GroupBy(d => (d.Demand.GroupId, SubjectId: d.Demand.SubjectId ?? "")))
        {
            if (string.IsNullOrEmpty(pool.Key.SubjectId))
            {
                continue;
            }

            foreach (var day in days)
            {
                var indices = SchedulingConstraintHelper.IndicesForDay(ctx.Indexer, day);
                if (indices.Count == 0)
                {
                    continue;
                }

                var onDayFlags = new List<BoolVar>();
                foreach (var d in pool)
                {
                    onDayFlags.Add(SchedulingConstraintHelper.CreateOnDayLiteral(
                        ctx, d, indices, $"{pool.Key.GroupId}_{pool.Key.SubjectId}_{day}"));
                }

                if (onDayFlags.Count < 2)
                {
                    continue;
                }

                var excess = ctx.Model.NewIntVar(0, onDayFlags.Count, $"subj_excess_{pool.Key.GroupId}_{day}");
                ctx.Model.Add(excess >= LinearExpr.Sum(onDayFlags) - 1);
                var viol = ctx.Violations.AddViolation(
                    ctx.Model, "R24", penalty, $"{pool.Key.GroupId}+{pool.Key.SubjectId}_{day}", def.Class);
                ctx.Model.Add(excess >= 1).OnlyEnforceIf(viol);
                ctx.Model.Add(excess == 0).OnlyEnforceIf(viol.Not());
            }
        }
    }
}
