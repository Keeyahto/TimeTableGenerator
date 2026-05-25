using System.Text.Json;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Input;

internal static class TeacherBlockedRulesResolver
{
    public static IReadOnlyList<int> ResolveForbiddenStarts(JsonElement teacher, SlotIndexer indexer)
    {
        var indices = new HashSet<int>();
        if (!teacher.TryGetProperty("blocked_rules", out var rules) || rules.ValueKind != JsonValueKind.Array)
        {
            return indices.OrderBy(x => x).ToList();
        }

        foreach (var rule in rules.EnumerateArray())
        {
            if (!rule.TryGetProperty("day_ids", out var days) || days.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var lessonIndices = new List<int>();
            if (rule.TryGetProperty("lesson_indices", out var liArr) && liArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var li in liArr.EnumerateArray())
                {
                    if (li.TryGetInt32(out var lessonIndex))
                    {
                        lessonIndices.Add(lessonIndex);
                    }
                }
            }

            foreach (var dayEl in days.EnumerateArray())
            {
                var day = InputFieldAccess.NormalizeDayId(dayEl.GetString());
                foreach (var slot in indexer.Slots)
                {
                    if (!string.Equals(slot.Day, day, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (lessonIndices.Count == 0
                        || (slot.SlotIndex is int si && lessonIndices.Contains(si)))
                    {
                        indices.Add(slot.Index);
                    }
                }
            }
        }

        return indices.OrderBy(x => x).ToList();
    }
}
