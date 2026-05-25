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
                var onDayFlags = new List<BoolVar>();
                foreach (var d in groupDemands)
                {
                    var onDay = ctx.Model.NewBoolVar($"on_day_{d.Demand.Id}_{day}");
                    onDayFlags.Add(onDay);
                    ctx.Model.AddImplication(onDay, d.Presence);

                    var atSlots = new List<BoolVar>();
                    foreach (var idx in indices)
                    {
                        var atSlot = ctx.Model.NewBoolVar($"at_{d.Demand.Id}_{idx}");
                        ctx.Model.Add(d.Start == idx).OnlyEnforceIf(atSlot);
                        ctx.Model.Add(d.Start != idx).OnlyEnforceIf(atSlot.Not());
                        atSlots.Add(atSlot);
                    }

                    if (atSlots.Count > 0)
                    {
                        ctx.Model.AddBoolOr(atSlots).OnlyEnforceIf(onDay);
                        ctx.Model.AddBoolAnd(atSlots.Select(a => (ILiteral)a.Not()).ToArray()).OnlyEnforceIf(onDay.Not());
                    }
                    else
                    {
                        ctx.Model.Add(onDay == 0);
                    }
                }

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
