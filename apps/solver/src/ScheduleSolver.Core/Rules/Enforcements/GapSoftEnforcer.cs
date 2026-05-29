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

                var atStartCache = new Dictionary<(string DemandId, int Start), BoolVar>(
                    demands.Count * orderedIndices.Count);

                for (var i = 0; i + 2 < orderedIndices.Count; i++)
                {
                    AddGapWindowViolation(
                        ctx,
                        ruleId,
                        ruleClass,
                        penalty,
                        pool.Key,
                        day,
                        demands,
                        orderedIndices[i],
                        orderedIndices[i + 1],
                        orderedIndices[i + 2],
                        atStartCache);
                }
            }
        }
    }

    private static BoolVar GetAtStart(
        SchedulingBuildContext ctx,
        DemandScheduleVars d,
        int startIndex,
        string day,
        Dictionary<(string DemandId, int Start), BoolVar> cache)
    {
        var key = (d.Demand.Id, startIndex);
        if (!cache.TryGetValue(key, out var at))
        {
            at = SchedulingConstraintHelper.CreateAtStartWhenPresentLiteral(
                ctx, d, startIndex, $"gap_{day}");
            cache[key] = at;
        }

        return at;
    }

    private static BoolVar CreateAnyOf(
        CpModel model,
        IReadOnlyList<BoolVar> flags,
        string name)
    {
        var any = model.NewBoolVar(name);
        model.AddBoolOr(flags).OnlyEnforceIf(any);
        model.AddBoolAnd(flags.Select(f => (ILiteral)f.Not()).ToArray()).OnlyEnforceIf(any.Not());
        return any;
    }

    /// <summary>Gap when someone is at lo and hi but the middle slot between them is empty.</summary>
    private static void AddGapWindowViolation(
        SchedulingBuildContext ctx,
        string ruleId,
        RuleClass ruleClass,
        int penalty,
        string poolKey,
        string day,
        IReadOnlyList<DemandScheduleVars> demands,
        int startLo,
        int startMid,
        int startHi,
        Dictionary<(string DemandId, int Start), BoolVar> atStartCache)
    {
        var anyLo = CreateAnyOf(
            ctx.Model,
            demands.Select(d => GetAtStart(ctx, d, startLo, day, atStartCache)).ToList(),
            $"gap_any_lo_{poolKey}_{day}_{startLo}");
        var anyMid = CreateAnyOf(
            ctx.Model,
            demands.Select(d => GetAtStart(ctx, d, startMid, day, atStartCache)).ToList(),
            $"gap_any_mid_{poolKey}_{day}_{startMid}");
        var anyHi = CreateAnyOf(
            ctx.Model,
            demands.Select(d => GetAtStart(ctx, d, startHi, day, atStartCache)).ToList(),
            $"gap_any_hi_{poolKey}_{day}_{startHi}");

        var viol = ctx.Violations.AddViolation(
            ctx.Model,
            ruleId,
            penalty,
            $"{poolKey}@{day}_{startLo}-{startHi}",
            ruleClass);
        ctx.Model.AddBoolOr(new ILiteral[] { anyLo.Not(), anyHi.Not(), anyMid, viol });
        ctx.Model.AddImplication(viol, anyLo);
        ctx.Model.AddImplication(viol, anyHi);
        ctx.Model.AddImplication(viol, anyMid.Not());
    }
}
