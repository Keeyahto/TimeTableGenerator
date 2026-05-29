using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R19GroupMaxPerDayEnforcer : IRuleEnforcer
{
    public string RuleId => "R19";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R19").DefaultPenalty;
        var slotsByDay = ctx.Indexer.Slots
            .GroupBy(s => s.Day ?? "unknown", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Index).ToList(), StringComparer.Ordinal);

        foreach (var groupDemands in ctx.Demands.GroupBy(d => d.Demand.GroupId))
        {
            if (!ctx.Catalogs.Groups.TryGetValue(groupDemands.Key, out var group)
                || group.MaxLessonsPerDay is not int maxPerDay
                || maxPerDay <= 0)
            {
                continue;
            }

            foreach (var (day, indices) in slotsByDay)
            {
                var onDayFlags = groupDemands
                    .Select(d => SchedulingConstraintHelper.CreateOnDayLiteral(ctx, d, indices, $"r19_{day}"))
                    .Cast<BoolVar>()
                    .ToList();

                if (onDayFlags.Count == 0)
                {
                    continue;
                }

                var excess = ctx.Model.NewIntVar(0, onDayFlags.Count, $"excess_{groupDemands.Key}_{day}");
                ctx.Model.Add(excess >= LinearExpr.Sum(onDayFlags) - maxPerDay);
                var viol = ctx.Violations.AddViolation(
                    ctx.Model, "R19", penalty, $"{groupDemands.Key}_{day}", RuleClass.RELAXED_HARD);
                ctx.Model.Add(excess >= 1).OnlyEnforceIf(viol);
                ctx.Model.Add(excess == 0).OnlyEnforceIf(viol.Not());
            }
        }
    }
}
