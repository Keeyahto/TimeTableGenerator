using System.Text.Json;

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

    public static IReadOnlyList<LessonDemandRow> FromInput(JsonElement root)
    {
        if (!root.TryGetProperty("lesson_demands", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rows = new List<LessonDemandRow>();
        foreach (var d in arr.EnumerateArray())
        {
            var id = d.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var groupId = d.TryGetProperty("group_id", out var gEl) ? gEl.GetString() : null;
            var teacherId = d.TryGetProperty("teacher_id", out var tEl) ? tEl.GetString() : null;
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

            rows.Add(new LessonDemandRow
            {
                Id = id,
                GroupId = groupId,
                TeacherId = teacherId,
                RoomId = d.TryGetProperty("room_id", out var rEl) ? rEl.GetString() : null,
                SubjectId = d.TryGetProperty("subject_id", out var sEl) ? sEl.GetString() : null,
                DurationSlots = duration,
                Vacant = vacant,
            });
        }

        return rows;
    }
}
