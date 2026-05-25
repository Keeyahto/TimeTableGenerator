using System.Text.Json;
using ScheduleSolver.Core.Input;

namespace ScheduleSolver.Core.Model;

public sealed class LessonDemandRow
{
    public required string Id { get; init; }
    public required string GroupId { get; init; }
    public required string TeacherId { get; init; }
    public string? RoomId { get; init; }
    public string? SubjectId { get; init; }
    public int DurationSlots { get; init; }
    public bool Vacant { get; init; }
    public string? LessonType { get; init; }
    public string? LanguageParallelKey { get; init; }

    public static IReadOnlyList<LessonDemandRow> FromInput(JsonElement root)
    {
        if (!root.TryGetProperty("lesson_demands", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rows = new List<LessonDemandRow>();
        foreach (var d in arr.EnumerateArray())
        {
            var id = InputFieldAccess.GetString(d, "id", "lesson_demand_id");
            var groupId = InputFieldAccess.GetString(d, "group_id");
            var teacherId = InputFieldAccess.GetFirstTeacherId(d);
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(teacherId))
            {
                continue;
            }

            var duration = 1;
            if (d.TryGetProperty("hours_per_week", out var hEl) && hEl.TryGetInt32(out var hours) && hours > 0)
            {
                duration = hours;
            }
            else if (d.TryGetProperty("duration_slots", out var dsEl) && dsEl.TryGetInt32(out var ds) && ds > 0)
            {
                duration = ds;
            }

            var vacant = d.TryGetProperty("vacant", out var vacEl) && vacEl.ValueKind == JsonValueKind.True;
            var lessonType = InputFieldAccess.GetString(d, "lesson_type");
            string? parallelKey = null;
            if (string.Equals(lessonType, "foreign_language", StringComparison.OrdinalIgnoreCase))
            {
                parallelKey = groupId;
            }
            else if (d.TryGetProperty("language_parallel_key", out var lk) && lk.ValueKind == JsonValueKind.String)
            {
                parallelKey = lk.GetString();
            }

            rows.Add(new LessonDemandRow
            {
                Id = id,
                GroupId = groupId,
                TeacherId = teacherId,
                RoomId = InputFieldAccess.GetFirstRoomId(d),
                SubjectId = InputFieldAccess.GetString(d, "subject_id"),
                DurationSlots = duration,
                Vacant = vacant,
                LessonType = lessonType,
                LanguageParallelKey = parallelKey,
            });
        }

        return rows;
    }
}
