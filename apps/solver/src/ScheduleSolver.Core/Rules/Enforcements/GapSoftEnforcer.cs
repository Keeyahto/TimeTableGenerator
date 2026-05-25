using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Core.Rules.Enforcements;

internal static class GapSoftEnforcer
{
    public static void ApplyToPools(
        SchedulingBuildContext ctx,
        string ruleId,
        RuleClass ruleClass,
        IEnumerable<IGrouping<string, DemandScheduleVars>> pools)
    {
        var penalty = ctx.Registry.GetRequired(ruleId).DefaultPenalty;
        var slotsByDay = ctx.Indexer.Slots
            .GroupBy(s => s.Day ?? "unknown", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Index).OrderBy(x => x).ToList(), StringComparer.Ordinal);

        foreach (var pool in pools)
        {
            var demands = pool.ToList();
            if (demands.Count < 2)
            {
                continue;
            }

            foreach (var (day, orderedIndices) in slotsByDay)
            {
                if (orderedIndices.Count < 3)
                {
                    continue;
                }

                for (var i = 0; i < orderedIndices.Count; i++)
                {
                    for (var j = i + 2; j < orderedIndices.Count; j++)
                    {
                        var startLo = orderedIndices[i];
                        var startHi = orderedIndices[j];

                        foreach (var d1 in demands)
                        {
                            foreach (var d2 in demands)
                            {
                                if (ReferenceEquals(d1, d2))
                                {
                                    continue;
                                }

                                AddGapViolation(ctx, ruleId, ruleClass, penalty, day, d1, d2, startLo, startHi);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void AddGapViolation(
        SchedulingBuildContext ctx,
        string ruleId,
        RuleClass ruleClass,
        int penalty,
        string day,
        DemandScheduleVars d1,
        DemandScheduleVars d2,
        int startLo,
        int startHi)
    {
        var atLo1 = ctx.Model.NewBoolVar($"gap_lo_{d1.Demand.Id}_{startLo}");
        ctx.Model.Add(d1.Start == startLo).OnlyEnforceIf(atLo1);
        ctx.Model.Add(d1.Start != startLo).OnlyEnforceIf(atLo1.Not());

        var atHi2 = ctx.Model.NewBoolVar($"gap_hi_{d2.Demand.Id}_{startHi}");
        ctx.Model.Add(d2.Start == startHi).OnlyEnforceIf(atHi2);
        ctx.Model.Add(d2.Start != startHi).OnlyEnforceIf(atHi2.Not());

        var viol = ctx.Violations.AddViolation(
            ctx.Model, ruleId, penalty, $"{d1.Demand.Id}+{d2.Demand.Id}@{day}_{startLo}-{startHi}", ruleClass);
        ctx.Model.AddBoolOr(new ILiteral[] { d1.Presence.Not(), atLo1.Not(), d2.Presence.Not(), atHi2.Not(), viol });
        ctx.Model.AddImplication(viol, d1.Presence);
        ctx.Model.AddImplication(viol, d2.Presence);
        ctx.Model.AddImplication(viol, atLo1);
        ctx.Model.AddImplication(viol, atHi2);
    }
}
