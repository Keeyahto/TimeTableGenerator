using Google.OrTools.Sat;
using Google.OrTools.Util;

namespace ScheduleSolver.Core.Model;

public static class SchedulingConstraintHelper
{
    /// <summary>RELAXED/SOFT: one violation literal for any start in <paramref name="forbiddenStarts"/>.</summary>
    public static void AddForbiddenStartsViolation(
        SchedulingBuildContext ctx,
        string ruleId,
        int penalty,
        string label,
        DemandScheduleVars d,
        IReadOnlyList<int> forbiddenStarts)
    {
        if (forbiddenStarts.Count == 0)
        {
            return;
        }

        var duration = Math.Min(d.Demand.DurationSlots, ctx.Indexer.Horizon);
        var maxStart = Math.Max(0, ctx.Indexer.Horizon - duration);
        var allowed = AllowedStartResolver.ComputeForDemand(d.Demand, ctx.Indexer)
            .Where(s => s <= maxStart)
            .ToHashSet();
        var feasible = forbiddenStarts.Where(allowed.Contains).Distinct().ToList();
        if (feasible.Count == 0)
        {
            return;
        }

        var inForbidden = feasible.Count == 1
            ? CreateAtStartWhenPresentLiteral(ctx, d, feasible[0], $"forbid_{ruleId}_{label}")
            : CreateOnDayLiteral(ctx, d, feasible, $"forbid_{ruleId}_{label}");
        var viol = ctx.Violations.AddViolation(
            ctx.Model, ruleId, penalty, label, ctx.Registry.GetRequired(ruleId).Class);
        ctx.Model.AddBoolOr(new ILiteral[] { d.Presence.Not(), inForbidden.Not(), viol });
        ctx.Model.AddImplication(viol, d.Presence);
        ctx.Model.AddImplication(viol, inForbidden);
    }

    /// <summary>True iff demand is present and <paramref name="startIndex"/> is selected (2 bools, safe when only one calendar start).</summary>
    public static BoolVar CreateAtStartWhenPresentLiteral(
        SchedulingBuildContext ctx,
        DemandScheduleVars d,
        int startIndex,
        string tag)
    {
        var at = ctx.Model.NewBoolVar($"at_{tag}_{d.Demand.Id}_{startIndex}");
        ctx.Model.AddImplication(at, d.Presence);
        ctx.Model.AddImplication(d.Presence.Not(), at.Not());
        ctx.Model.Add(d.Start == startIndex).OnlyEnforceIf(new ILiteral[] { d.Presence, at });
        ctx.Model.Add(d.Start != startIndex).OnlyEnforceIf(new ILiteral[] { d.Presence, at.Not() });
        return at;
    }

    /// <inheritdoc cref="AddForbiddenStartsViolation"/>
    public static void AddForbiddenStartViolation(
        SchedulingBuildContext ctx,
        string ruleId,
        int penalty,
        string label,
        DemandScheduleVars d,
        int forbiddenStart) =>
        AddForbiddenStartsViolation(ctx, ruleId, penalty, label, d, [forbiddenStart]);

    /// <summary>True when demand is present and start lies on one of the day's slot indices (2 bools + domain).</summary>
    public static BoolVar CreateOnDayLiteral(
        SchedulingBuildContext ctx,
        DemandScheduleVars d,
        IReadOnlyList<int> daySlotIndices,
        string tag)
    {
        var onDay = ctx.Model.NewBoolVar($"on_day_{tag}_{d.Demand.Id}");

        if (daySlotIndices.Count == 0)
        {
            ctx.Model.Add(onDay == 0);
            return onDay;
        }

        var inDay = ctx.Model.NewBoolVar($"in_day_{tag}_{d.Demand.Id}");
        var domain = Domain.FromValues(daySlotIndices.Select(i => (long)i).ToArray());
        var maxStart = Math.Max(0, ctx.Indexer.Horizon - Math.Min(d.Demand.DurationSlots, ctx.Indexer.Horizon));
        var complement = Domain.FromValues(
            Enumerable.Range(0, maxStart + 1)
                .Where(i => !daySlotIndices.Contains(i))
                .Select(i => (long)i)
                .ToArray());

        ctx.Model.AddImplication(d.Presence.Not(), inDay.Not());
        ctx.Model.AddLinearExpressionInDomain(d.Start, domain)
            .OnlyEnforceIf(new ILiteral[] { d.Presence, inDay });
        if (!complement.IsEmpty())
        {
            ctx.Model.AddLinearExpressionInDomain(d.Start, complement)
                .OnlyEnforceIf(new ILiteral[] { d.Presence, inDay.Not() });
        }

        ctx.Model.AddImplication(onDay, d.Presence);
        ctx.Model.AddImplication(onDay, inDay);
        ctx.Model.AddBoolOr(new ILiteral[] { d.Presence.Not(), inDay.Not(), onDay });
        ctx.Model.AddBoolOr(new ILiteral[] { onDay, d.Presence.Not(), inDay.Not() });

        return onDay;
    }

    public static IReadOnlyList<int> IndicesForDay(SlotIndexer indexer, string day) =>
        indexer.Slots
            .Where(s => string.Equals(s.Day, day, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Index)
            .ToList();

    public static IReadOnlyList<int> IndicesForDayAndSlotIndex(SlotIndexer indexer, string day, int slotIndex) =>
        indexer.Slots
            .Where(s => string.Equals(s.Day, day, StringComparison.OrdinalIgnoreCase) && s.SlotIndex == slotIndex)
            .Select(s => s.Index)
            .ToList();

    public static IReadOnlyList<int> IndicesWhereSlotIndexNotIn(SlotIndexer indexer, int minIncl, int maxIncl) =>
        indexer.Slots
            .Where(s => s.SlotIndex is not int si || si < minIncl || si > maxIncl)
            .Select(s => s.Index)
            .ToList();
}
