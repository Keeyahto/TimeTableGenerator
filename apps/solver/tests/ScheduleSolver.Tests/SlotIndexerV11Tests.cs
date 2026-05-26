using System.Text.Json;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Tests;

public class SlotIndexerV11Tests
{
    [Fact]
    public void V11_semester_week_table_uses_flat_slot_timeline_weeks_one()
    {
        var json = """
            {
              "calendar": {
                "weeks": [{ "week_index": 1, "week_parity": "upper" }],
                "slots": [
                  { "slot_id": "upper_mon_1", "day_id": "mon", "lesson_index": 1 },
                  { "slot_id": "lower_mon_1", "day_id": "mon", "lesson_index": 1 }
                ]
              }
            }
            """;
        using var doc = JsonDocument.Parse(json);
        using var input = new ParsedInput(doc, "inline.json");
        var indexer = SlotIndexer.FromInput(input);

        Assert.Equal(1, indexer.Weeks);
        Assert.Equal(2, indexer.Slots.Count);
        Assert.Equal(3, indexer.Horizon);
    }
}
