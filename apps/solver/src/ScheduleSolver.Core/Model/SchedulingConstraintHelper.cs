using Google.OrTools.Sat;

namespace ScheduleSolver.Core.Model;

public static class SchedulingConstraintHelper
{
    public static void AddForbiddenStartViolation(
        SchedulingBuildContext ctx,
        string ruleId,
        int penalty,
        string label,
        DemandScheduleVars d,
        int forbiddenStart)
    {
        var atBad = ctx.Model.NewBoolVar($"at_bad_{label}_{forbiddenStart}");
        ctx.Model.Add(d.Start == forbiddenStart).OnlyEnforceIf(atBad);
        ctx.Model.Add(d.Start != forbiddenStart).OnlyEnforceIf(atBad.Not());

        var viol = ctx.Violations.AddViolation(ctx.Model, ruleId, penalty, label, ctx.Registry.GetRequired(ruleId).Class);
        ctx.Model.AddBoolOr(new ILiteral[] { d.Presence.Not(), atBad.Not(), viol });
        ctx.Model.AddImplication(viol, d.Presence);
        ctx.Model.AddImplication(viol, atBad);
    }

    public static BoolVar CreateOnDayLiteral(
        SchedulingBuildContext ctx,
        DemandScheduleVars d,
        IReadOnlyList<int> daySlotIndices,
        string tag)
    {
        var onDay = ctx.Model.NewBoolVar($"on_day_{tag}_{d.Demand.Id}");
        ctx.Model.AddImplication(onDay, d.Presence);

        var atSlots = new List<BoolVar>();
        foreach (var idx in daySlotIndices)
        {
            var atSlot = ctx.Model.NewBoolVar($"at_{tag}_{d.Demand.Id}_{idx}");
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
