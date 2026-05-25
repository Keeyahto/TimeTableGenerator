using System.Text.Json;
using ScheduleSolver.Core.Input;

namespace ScheduleSolver.Core.Model;

public sealed class SlotIndexer
{
    private readonly Dictionary<string, int> _idToIndex;

    private SlotIndexer(IReadOnlyList<SlotInfo> slots, int weeks)
    {
        Slots = slots;
        Weeks = Math.Max(1, weeks);
        // OR-Tools intervals use end = start + duration; need end <= horizon.
        Horizon = slots.Count == 0
            ? 0
            : Math.Max(slots.Count * Weeks, slots.Count + 1);
        _idToIndex = slots.ToDictionary(s => s.Id, s => s.Index, StringComparer.Ordinal);
    }

    public IReadOnlyList<SlotInfo> Slots { get; }
    public int Weeks { get; }
    public int Horizon { get; }

    public static SlotIndexer FromInput(ParsedInput input)
    {
        if (!input.Root.TryGetProperty("calendar", out var calendar)
            || calendar.ValueKind != JsonValueKind.Object)
        {
            return new SlotIndexer([], 1);
        }

        var weeks = 1;
        if (calendar.TryGetProperty("weeks", out var w))
        {
            if (w.TryGetInt32(out var wc) && wc > 0)
            {
                weeks = wc;
            }
            else if (w.ValueKind == JsonValueKind.Array && w.GetArrayLength() > 0)
            {
                weeks = w.GetArrayLength();
            }
        }
        if (!calendar.TryGetProperty("slots", out var slotsEl) || slotsEl.ValueKind != JsonValueKind.Array)
        {
            return new SlotIndexer([], weeks);
        }

        var list = new List<SlotInfo>();
        var i = 0;
        foreach (var slot in slotsEl.EnumerateArray())
        {
            var id = InputFieldAccess.GetString(slot, "id", "slot_id") ?? $"slot-{i}";

            var dayRaw = InputFieldAccess.GetString(slot, "day", "day_id");
            var day = InputFieldAccess.NormalizeDayId(dayRaw);
            int? slotIndex = null;
            if (slot.TryGetProperty("index", out var idxEl) && idxEl.TryGetInt32(out var idx))
            {
                slotIndex = idx;
            }
            else if (slot.TryGetProperty("lesson_index", out var liEl) && liEl.TryGetInt32(out var li))
            {
                slotIndex = li;
            }
            list.Add(new SlotInfo(i, id, day, slotIndex));
            i++;
        }

        return new SlotIndexer(list, weeks);
    }

    public bool TryGetIndex(string slotId, out int index) => _idToIndex.TryGetValue(slotId, out index);

    public int WeekOffset(int weekIndex) => weekIndex * Slots.Count;
}
