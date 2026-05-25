using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

/// <summary>R01–R06, R15: intervals, no-overlap pools, Saturday slot index.</summary>
public sealed class ModelStructureEnforcer : IRuleEnforcer
{
    public string RuleId => "R03";

    public void Apply(SchedulingBuildContext ctx)
    {
        var model = ctx.Model;
        var demands = ctx.Demands;
        var indexer = ctx.Indexer;

        var validStarts = indexer.Slots.Select(s => s.Index).ToList();
        foreach (var d in demands)
        {
            if (validStarts.Count == 0)
            {
                continue;
            }

            var atValid = new List<BoolVar>();
            foreach (var idx in validStarts)
            {
                var at = model.NewBoolVar($"valid_start_{d.Demand.Id}_{idx}");
                model.Add(d.Start == idx).OnlyEnforceIf(at);
                model.Add(d.Start != idx).OnlyEnforceIf(at.Not());
                atValid.Add(at);
            }

            model.AddBoolOr(atValid).OnlyEnforceIf(d.Presence);
        }

        AddPoolOverlap(model, demands, d => d.Demand.GroupId);
        AddPoolOverlap(model, demands, d => d.Demand.TeacherId);
        AddPoolOverlap(
            model,
            demands.Where(d => !string.IsNullOrEmpty(d.Demand.RoomId)).ToList(),
            d => d.Demand.RoomId!);

        var saturdayIndices = indexer.Slots
            .Where(s => string.Equals(s.Day, "saturday", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var d in demands)
        {
            foreach (var slot in saturdayIndices)
            {
                if (slot.SlotIndex is int si && (si < 1 || si > 4))
                {
                    model.Add(d.Start != slot.Index).OnlyEnforceIf(d.Presence);
                }
            }
        }

        foreach (var id in new[] { "R01", "R02", "R03", "R04", "R05", "R06", "R15" })
        {
            if (!ctx.EnforcedRuleIds.Contains(id, StringComparer.Ordinal))
            {
                ctx.EnforcedRuleIds.Add(id);
            }
        }
    }

    private static void AddPoolOverlap(
        CpModel model,
        IReadOnlyList<DemandScheduleVars> demands,
        Func<DemandScheduleVars, string> keySelector)
    {
        foreach (var pool in demands.GroupBy(keySelector))
        {
            if (pool.Count() > 1)
            {
                model.AddNoOverlap(pool.Select(d => d.Interval).ToArray());
            }
        }
    }
}
