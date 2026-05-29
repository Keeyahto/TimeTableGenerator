using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Tests;

public class AllowedStartResolverTests
{
    [Fact]
    public void ComputeForDemand_excludes_saturday_slots_5_and_6()
    {
        var json = """
            {
              "calendar": {
                "slots": [
                  { "id": "sat-5", "day": "saturday", "index": 5 },
                  { "id": "sat-1", "day": "saturday", "index": 1 },
                  { "id": "mon-1", "day": "monday", "index": 1 }
                ]
              },
              "lesson_demands": [{ "id": "d1", "group_id": "g", "teacher_id": "t", "hours_per_week": 1 }]
            }
            """;
        using var input = new ParsedInput(System.Text.Json.JsonDocument.Parse(json), "inline.json");
        var indexer = SlotIndexer.FromInput(input);
        var rows = LessonDemandRow.FromInput(input.Root);

        var allowed = AllowedStartResolver.ComputeForDemand(rows[0], indexer);

        Assert.Equal(2, allowed.Count);
        Assert.DoesNotContain(0, allowed);
        Assert.Contains(1, allowed);
        Assert.Contains(2, allowed);
    }

    [Fact]
    public void ComputeForDemand_filters_week_parity_and_class_hour_monday()
    {
        var json = """
            {
              "calendar": {
                "slots": [
                  { "id": "lower", "day": "mon", "lesson_index": 1, "week_parity": "lower" },
                  { "id": "upper", "day": "mon", "lesson_index": 5, "week_parity": "upper" },
                  { "id": "mon-2", "day": "monday", "index": 2, "week_parity": "lower" }
                ]
              },
              "lesson_demands": [
                { "id": "u1", "group_id": "g", "teacher_id": "t", "week_parity": "upper", "hours_per_week": 1 },
                { "id": "ch1", "group_id": "g", "teacher_id": "t", "lesson_type": "class_hour", "hours_per_week": 1 }
              ]
            }
            """;
        using var input = new ParsedInput(System.Text.Json.JsonDocument.Parse(json), "inline.json");
        var indexer = SlotIndexer.FromInput(input);
        var rows = LessonDemandRow.FromInput(input.Root);

        var upperOnly = AllowedStartResolver.ComputeForDemand(rows[0], indexer);
        Assert.Single(upperOnly);
        Assert.Equal(1, upperOnly[0]);

        var classHour = AllowedStartResolver.ComputeForDemand(rows[1], indexer);
        Assert.DoesNotContain(2, classHour);
        Assert.Contains(1, classHour);
    }
}
