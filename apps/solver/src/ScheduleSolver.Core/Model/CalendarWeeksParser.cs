using System.Text.Json;

namespace ScheduleSolver.Core.Model;

internal static class CalendarWeeksParser
{
    /// <summary>
    /// Synthetic 0.1 uses numeric weeks; v1_1 uses a semester week table while slots[] is the discrete timeline.
    /// </summary>
    public static int ParseWeekCount(JsonElement calendar)
    {
        if (!calendar.TryGetProperty("weeks", out var weeksEl))
        {
            return 1;
        }

        if (weeksEl.ValueKind == JsonValueKind.Number
            && weeksEl.TryGetInt32(out var weekCount)
            && weekCount > 0)
        {
            return weekCount;
        }

        if (weeksEl.ValueKind == JsonValueKind.Array)
        {
            if (weeksEl.GetArrayLength() > 0
                && weeksEl[0].ValueKind == JsonValueKind.Object
                && weeksEl[0].TryGetProperty("week_index", out _))
            {
                return 1;
            }

            return Math.Max(1, weeksEl.GetArrayLength());
        }

        return 1;
    }
}
