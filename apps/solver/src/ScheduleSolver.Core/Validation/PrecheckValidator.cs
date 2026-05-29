using System.Text.Json;
using ScheduleSolver.Core.Input;

using static ScheduleSolver.Core.Input.InputFieldAccess;

namespace ScheduleSolver.Core.Validation;

/// <summary>R00 — reference integrity and basic demand shape.</summary>
public static class PrecheckValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(ParsedInput input)
    {
        var issues = new List<ValidationIssue>();
        var root = input.Root;

        issues.AddRange(FindDuplicateIds(root, "groups", new[] { "id", "group_id" }));
        issues.AddRange(FindDuplicateIds(root, "teachers", new[] { "id", "teacher_id" }));
        issues.AddRange(FindDuplicateIds(root, "rooms", new[] { "id", "room_id" }));
        issues.AddRange(FindDuplicateIds(root, "subjects", new[] { "id", "subject_id" }));
        issues.AddRange(FindDuplicateDemandIds(root));
        issues.AddRange(FindDuplicateSlotIds(root));

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
            var id = GetString(demand, "id", "lesson_demand_id");
            if (string.IsNullOrWhiteSpace(id))
            {
                issues.Add(new ValidationIssue("R00_MISSING_ID", "lesson_demand.id is required.", path));
            }

            var groupId = GetString(demand, "group_id");
            if (string.IsNullOrWhiteSpace(groupId))
            {
                issues.Add(new ValidationIssue("R00_MISSING_GROUP", "lesson_demand.group_id is required.", path));
            }
            else if (!groupIds.Contains(groupId))
            {
                issues.Add(new ValidationIssue(
                    "R00_UNKNOWN_GROUP",
                    $"Unknown group_id '{groupId}' in demand '{id}'.",
                    path));
            }

            var teacherId = GetFirstTeacherId(demand);
            if (string.IsNullOrWhiteSpace(teacherId))
            {
                if (IsVacancyPlaceholder(demand))
                {
                    issues.Add(new ValidationIssue(
                        "R00_VACANT_PLACEHOLDER",
                        "Demand has no teacher yet (empty teacher_options); it is skipped by the solver.",
                        path));
                }
                else
                {
                    issues.Add(new ValidationIssue(
                        "R00_MISSING_TEACHER",
                        "lesson_demand requires teacher_id or a non-empty teacher_options entry.",
                        path));
                }
            }
            else if (!teacherIds.Contains(teacherId))
            {
                issues.Add(new ValidationIssue(
                    "R00_UNKNOWN_TEACHER",
                    $"Unknown teacher_id '{teacherId}' in demand '{id}'.",
                    path));
            }

            if (CountNonEmptyStringArray(demand, "teacher_options") > 1)
            {
                issues.Add(new ValidationIssue(
                    "R00_MULTIPLE_TEACHER_OPTIONS",
                    "Multiple teacher_options are present; solver uses the first entry only.",
                    path));
            }

            if (CountNonEmptyStringArray(demand, "allowed_room_ids") > 1)
            {
                issues.Add(new ValidationIssue(
                    "R00_MULTIPLE_ROOM_OPTIONS",
                    "Multiple allowed_room_ids are present; solver uses the first entry only.",
                    path));
            }

            var roomId = GetFirstRoomId(demand);
            if (!string.IsNullOrWhiteSpace(roomId) && !roomIds.Contains(roomId))
            {
                issues.Add(new ValidationIssue(
                    "R00_UNKNOWN_ROOM",
                    $"Unknown room_id '{roomId}' in demand '{id}'.",
                    path));
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

    private static IEnumerable<ValidationIssue> FindDuplicateIds(
        JsonElement root,
        string arrayName,
        string[] idPropertyNames)
    {
        if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;
        foreach (var item in arr.EnumerateArray())
        {
            var id = GetString(item, idPropertyNames);
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (seen.TryGetValue(id, out var firstIndex))
                {
                    yield return new ValidationIssue(
                        "R00_DUPLICATE_ID",
                        $"Duplicate {arrayName} id '{id}' (first at index {firstIndex}).",
                        $"{arrayName}[{index}]");
                }
                else
                {
                    seen[id] = index;
                }
            }

            index++;
        }
    }

    private static IEnumerable<ValidationIssue> FindDuplicateDemandIds(JsonElement root)
    {
        if (!root.TryGetProperty("lesson_demands", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;
        foreach (var item in arr.EnumerateArray())
        {
            var id = GetString(item, "id", "lesson_demand_id");
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (seen.TryGetValue(id, out var firstIndex))
                {
                    yield return new ValidationIssue(
                        "R00_DUPLICATE_ID",
                        $"Duplicate lesson_demands id '{id}' (first at index {firstIndex}).",
                        $"lesson_demands[{index}]");
                }
                else
                {
                    seen[id] = index;
                }
            }

            index++;
        }
    }

    private static IEnumerable<ValidationIssue> FindDuplicateSlotIds(JsonElement root)
    {
        if (!root.TryGetProperty("calendar", out var cal)
            || cal.ValueKind != JsonValueKind.Object
            || !cal.TryGetProperty("slots", out var slots)
            || slots.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;
        foreach (var slot in slots.EnumerateArray())
        {
            var id = GetString(slot, "id", "slot_id");
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (seen.TryGetValue(id, out var firstIndex))
                {
                    yield return new ValidationIssue(
                        "R00_DUPLICATE_ID",
                        $"Duplicate calendar slot id '{id}' (first at index {firstIndex}).",
                        $"calendar.slots[{index}]");
                }
                else
                {
                    seen[id] = index;
                }
            }

            index++;
        }
    }

    private static bool IsVacancyPlaceholder(JsonElement demand) =>
        demand.TryGetProperty("teacher_options", out var options)
        && options.ValueKind == JsonValueKind.Array
        && options.GetArrayLength() == 0;

    private static HashSet<string> CollectIds(JsonElement root, string arrayName)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return set;
        }

        var idPropertyNames = arrayName switch
        {
            "groups" => new[] { "id", "group_id" },
            "teachers" => new[] { "id", "teacher_id" },
            "rooms" => new[] { "id", "room_id" },
            "subjects" => new[] { "id", "subject_id" },
            _ => new[] { "id" },
        };

        foreach (var item in arr.EnumerateArray())
        {
            var id = GetString(item, idPropertyNames);
            if (!string.IsNullOrWhiteSpace(id))
            {
                set.Add(id);
            }
        }

        return set;
    }
}
