using Google.OrTools.Sat;

namespace ScheduleSolver.Core.Model;

/// <summary>MODEL_HARD allowed start indices per demand (calendar, Saturday R15, week parity, class-hour Monday).</summary>
public static class AllowedStartResolver
{
    public static IReadOnlyList<int> ComputeForDemand(LessonDemandRow row, SlotIndexer indexer)
    {
        IEnumerable<SlotInfo> candidates = indexer.Slots;

        candidates = candidates.Where(s => !IsSaturdaySlotForbidden(s));

        if (!string.IsNullOrWhiteSpace(row.WeekParity))
        {
            candidates = candidates.Where(s =>
                string.IsNullOrWhiteSpace(s.WeekParity)
                || string.Equals(s.WeekParity, row.WeekParity, StringComparison.OrdinalIgnoreCase));
        }

        if (LessonDemandRow.IsClassHour(row))
        {
            candidates = candidates.Where(s =>
                !IsMonday(s.Day) || s.SlotIndex is 1 or 5);
        }

        return candidates.Select(s => s.Index).ToList();
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<int>> Compute(
        IReadOnlyList<LessonDemandRow> rows,
        SlotIndexer indexer)
    {
        var map = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            map[row.Id] = ComputeForDemand(row, indexer);
        }

        return map;
    }

    public static bool IsSaturdaySlotForbidden(SlotInfo slot) =>
        (string.Equals(slot.Day, "saturday", StringComparison.OrdinalIgnoreCase)
         || string.Equals(slot.Day, "sat", StringComparison.OrdinalIgnoreCase))
        && slot.SlotIndex is int si
        && (si < 1 || si > 4);

    private static bool IsMonday(string? day) =>
        string.Equals(day, "monday", StringComparison.OrdinalIgnoreCase)
        || string.Equals(day, "mon", StringComparison.OrdinalIgnoreCase);

    public static IntVar CreateStartVar(
        CpModel model,
        LessonDemandRow row,
        IReadOnlyList<int> allowedStarts,
        int horizon,
        int durationSlots)
    {
        var maxStart = Math.Max(0, horizon - durationSlots);
        var feasible = allowedStarts.Where(s => s <= maxStart).Distinct().OrderBy(x => x).ToList();
        if (feasible.Count == 0)
        {
            return model.NewIntVar(0, 0, $"start_{row.Id}");
        }

        if (feasible.Count == 1)
        {
            var only = feasible[0];
            return model.NewIntVar(only, only, $"start_{row.Id}");
        }

        return model.NewIntVar(0, maxStart, $"start_{row.Id}");
    }
}
