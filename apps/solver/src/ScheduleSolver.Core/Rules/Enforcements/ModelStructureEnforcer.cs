using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Core.Rules.Enforcements;

/// <summary>R01–R06, R15: intervals, no-overlap pools, allowed starts.</summary>
public sealed class ModelStructureEnforcer : IRuleEnforcer
{
    public string RuleId => "R03";

    public void Apply(SchedulingBuildContext ctx)
    {
        var model = ctx.Model;
        var demands = ctx.Demands;
        var indexer = ctx.Indexer;

        foreach (var d in demands)
        {
            var duration = Math.Min(d.Demand.DurationSlots, indexer.Horizon);
            var maxStart = Math.Max(0, indexer.Horizon - duration);
            var allowed = AllowedStartResolver.ComputeForDemand(d.Demand, indexer)
                .Where(s => s <= maxStart)
                .ToHashSet();
            for (var idx = 0; idx <= maxStart; idx++)
            {
                if (!allowed.Contains(idx))
                {
                    model.Add(d.Start != idx).OnlyEnforceIf(d.Presence);
                }
            }
        }

        if (IsStructuralRuleEnabled(ctx, "R03"))
        {
            AddPoolOverlap(model, demands, d => d.Demand.GroupId);
        }

        if (IsStructuralRuleEnabled(ctx, "R04"))
        {
            var teacherDemands = demands.Where(d =>
                ctx.Catalogs.Teachers.TryGetValue(d.Demand.TeacherId, out var t) && !t.IsVirtual);
            AddPoolOverlap(model, teacherDemands, d => d.Demand.TeacherId);
        }

        if (IsStructuralRuleEnabled(ctx, "R05"))
        {
            var roomPools = demands
                .Where(d => !IsVirtualRoomId(d.Demand.RoomId))
                .GroupBy(d => d.Demand.RoomId!, StringComparer.Ordinal)
                .Where(pool =>
                {
                    if (!ctx.Catalogs.Rooms.TryGetValue(pool.Key, out var room))
                    {
                        return true;
                    }

                    return !(room.IsGym && room.MaxParallelGroups > 1);
                });

            foreach (var pool in roomPools)
            {
                if (pool.Count() > 1)
                {
                    model.AddNoOverlap(pool.Select(d => d.Interval).ToArray());
                }
            }
        }

        foreach (var id in new[] { "R01", "R02", "R03", "R04", "R05", "R06", "R15" })
        {
            if (IsStructuralRuleEnabled(ctx, id) && !ctx.EnforcedRuleIds.Contains(id, StringComparer.Ordinal))
            {
                ctx.EnforcedRuleIds.Add(id);
            }
        }
    }

    private static bool IsStructuralRuleEnabled(SchedulingBuildContext ctx, string ruleId) =>
        ctx.Registry.TryGet(ruleId, out var def) && def.DefaultStatus == EnforcementStatus.Enforced;

    internal static bool IsVirtualRoomId(string? roomId) =>
        string.IsNullOrEmpty(roomId)
        || roomId.StartsWith("virtual:", StringComparison.OrdinalIgnoreCase)
        || roomId.StartsWith("virtual_", StringComparison.OrdinalIgnoreCase);

    private static void AddPoolOverlap(
        CpModel model,
        IEnumerable<DemandScheduleVars> demands,
        Func<DemandScheduleVars, string> keySelector)
    {
        foreach (var pool in demands.GroupBy(keySelector))
        {
            var list = pool.ToList();
            if (list.Count > 1)
            {
                model.AddNoOverlap(list.Select(d => d.Interval).ToArray());
            }
        }
    }
}
