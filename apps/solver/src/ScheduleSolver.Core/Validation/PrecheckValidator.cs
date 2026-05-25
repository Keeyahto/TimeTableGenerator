using System.Text.Json;
using ScheduleSolver.Core.Input;

namespace ScheduleSolver.Core.Validation;

/// <summary>R00 — reference integrity and basic demand shape.</summary>
public static class PrecheckValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(ParsedInput input)
    {
        var issues = new List<ValidationIssue>();
        var root = input.Root;

        var groupIds = CollectIds(root, "groups");
        var teacherIds = CollectIds(root, "teachers");
        var roomIds = CollectIds(root, "rooms");
        var subjectIds = CollectIds(root, "subjects");

        if (!root.TryGetProperty("lesson_demands", out var demands) || demands.ValueKind != JsonValueKind.Array)
        {
            return issues;
        }

        var index = 0;
        foreach (var demand in demands.EnumerateArray())
        {
            var path = $"lesson_demands[{index}]";
            var id = demand.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
            {
                issues.Add(new ValidationIssue("R00_MISSING_ID", "lesson_demand.id is required.", path));
            }

            if (demand.TryGetProperty("group_id", out var g) && g.ValueKind == JsonValueKind.String)
            {
                var gid = g.GetString();
                if (!string.IsNullOrEmpty(gid) && !groupIds.Contains(gid))
                {
                    issues.Add(new ValidationIssue(
                        "R00_UNKNOWN_GROUP",
                        $"Unknown group_id '{gid}' in demand '{id}'.",
                        path));
                }
            }

            if (demand.TryGetProperty("teacher_id", out var t) && t.ValueKind == JsonValueKind.String)
            {
                var tid = t.GetString();
                if (!string.IsNullOrEmpty(tid) && !teacherIds.Contains(tid))
                {
                    issues.Add(new ValidationIssue(
                        "R00_UNKNOWN_TEACHER",
                        $"Unknown teacher_id '{tid}' in demand '{id}'.",
                        path));
                }
            }

            if (demand.TryGetProperty("room_id", out var r) && r.ValueKind == JsonValueKind.String)
            {
                var rid = r.GetString();
                if (!string.IsNullOrEmpty(rid) && !roomIds.Contains(rid))
                {
                    issues.Add(new ValidationIssue(
                        "R00_UNKNOWN_ROOM",
                        $"Unknown room_id '{rid}' in demand '{id}'.",
                        path));
                }
            }

            if (demand.TryGetProperty("subject_id", out var s) && s.ValueKind == JsonValueKind.String)
            {
                var sid = s.GetString();
                if (!string.IsNullOrEmpty(sid) && !subjectIds.Contains(sid))
                {
                    issues.Add(new ValidationIssue(
                        "R00_UNKNOWN_SUBJECT",
                        $"Unknown subject_id '{sid}' in demand '{id}'.",
                        path));
                }
            }

            index++;
        }

        return issues;
    }

    private static HashSet<string> CollectIds(JsonElement root, string arrayName)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return set;
        }

        foreach (var item in arr.EnumerateArray())
        {
            if (item.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
            {
                var id = idEl.GetString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    set.Add(id);
                }
            }
        }

        return set;
    }
}
