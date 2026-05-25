using System.Text.Json;

namespace ScheduleSolver.Core.Input;

public sealed class TeacherInfo
{
    public required string Id { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsAdmin { get; init; }
    public bool ThursdaySlot1Forbidden { get; init; }
    public IReadOnlyList<int> ForbiddenStartIndices { get; init; } = [];
}

public sealed record GroupInfo(
    string Id,
    int? MaxLessonsPerDay,
    bool IsFirstCourse,
    bool IsGraduation,
    int? CourseYear);

public sealed record RoomInfo(string Id, IReadOnlyList<string> BlockedDays, string? SourceRuleId = null);

public sealed class InputCatalogs
{
    public IReadOnlyDictionary<string, TeacherInfo> Teachers { get; init; } =
        new Dictionary<string, TeacherInfo>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, GroupInfo> Groups { get; init; } =
        new Dictionary<string, GroupInfo>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, RoomInfo> Rooms { get; init; } =
        new Dictionary<string, RoomInfo>(StringComparer.Ordinal);

    public static InputCatalogs FromRoot(JsonElement root)
    {
        var teachers = ParseTeachers(root);
        var groups = ParseGroups(root);
        var rooms = ParseRooms(root);
        MergeRuleParams(root, groups, rooms);
        return new InputCatalogs { Teachers = teachers, Groups = groups, Rooms = rooms };
    }

    private static Dictionary<string, TeacherInfo> ParseTeachers(JsonElement root)
    {
        var map = new Dictionary<string, TeacherInfo>(StringComparer.Ordinal);
        if (!root.TryGetProperty("teachers", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var t in arr.EnumerateArray())
        {
            var id = t.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var isVirtual = t.TryGetProperty("virtual", out var vEl) && vEl.ValueKind == JsonValueKind.True
                            || id.StartsWith("virtual:", StringComparison.OrdinalIgnoreCase)
                            || id.StartsWith("virtual_", StringComparison.OrdinalIgnoreCase);

            var isAdmin = t.TryGetProperty("admin", out var aEl) && aEl.ValueKind == JsonValueKind.True;
            var thuForbidden = t.TryGetProperty("thursday_slot1_forbidden", out var thuEl)
                               && thuEl.ValueKind == JsonValueKind.True;

            var forbidden = new List<int>();
            if (t.TryGetProperty("forbidden_start_indices", out var fArr) && fArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var fi in fArr.EnumerateArray())
                {
                    if (fi.TryGetInt32(out var idx))
                    {
                        forbidden.Add(idx);
                    }
                }
            }

            map[id] = new TeacherInfo
            {
                Id = id,
                IsVirtual = isVirtual,
                IsAdmin = isAdmin,
                ThursdaySlot1Forbidden = thuForbidden,
                ForbiddenStartIndices = forbidden,
            };
        }

        return map;
    }

    private static Dictionary<string, GroupInfo> ParseGroups(JsonElement root)
    {
        var map = new Dictionary<string, GroupInfo>(StringComparer.Ordinal);
        if (!root.TryGetProperty("groups", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var g in arr.EnumerateArray())
        {
            var id = g.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            int? maxPerDay = null;
            if (g.TryGetProperty("max_lessons_per_day", out var mEl) && mEl.TryGetInt32(out var m))
            {
                maxPerDay = m;
            }

            int? courseYear = null;
            if (g.TryGetProperty("course_year", out var cyEl) && cyEl.TryGetInt32(out var cy))
            {
                courseYear = cy;
            }

            var isFirst = g.TryGetProperty("first_course", out var fcEl) && fcEl.ValueKind == JsonValueKind.True
                          || courseYear == 1;
            var isGrad = g.TryGetProperty("graduation", out var grEl) && grEl.ValueKind == JsonValueKind.True;

            map[id] = new GroupInfo(id, maxPerDay, isFirst, isGrad, courseYear);
        }

        return map;
    }

    private static Dictionary<string, RoomInfo> ParseRooms(JsonElement root)
    {
        var map = new Dictionary<string, RoomInfo>(StringComparer.Ordinal);
        if (!root.TryGetProperty("rooms", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var r in arr.EnumerateArray())
        {
            var id = r.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var blockedDays = ParseBlockedDays(r);
            map[id] = new RoomInfo(id, blockedDays, null);
        }

        return map;
    }

    private static List<string> ParseBlockedDays(JsonElement roomOrRuleParams)
    {
        var days = new List<string>();
        if (!roomOrRuleParams.TryGetProperty("blocked_days", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return days;
        }

        foreach (var d in arr.EnumerateArray())
        {
            var name = d.GetString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                days.Add(name);
            }
        }

        return days;
    }

    private static void MergeRuleParams(
        JsonElement root,
        Dictionary<string, GroupInfo> groups,
        Dictionary<string, RoomInfo> rooms)
    {
        if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var rule in rules.EnumerateArray())
        {
            var id = rule.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (!rule.TryGetProperty("params", out var p))
            {
                continue;
            }

            if (id == "R19"
                && p.TryGetProperty("max_lessons_per_day", out var maxEl)
                && maxEl.TryGetInt32(out var max))
            {
                foreach (var key in groups.Keys.ToList())
                {
                    groups[key] = groups[key] with { MaxLessonsPerDay = max };
                }
            }

            if (id is "R29" or "R30")
            {
                MergeRoomBlockedDaysRule(id, p, rooms);
            }
        }
    }

    private static void MergeRoomBlockedDaysRule(string ruleId, JsonElement p, Dictionary<string, RoomInfo> rooms)
    {
        var roomId = p.TryGetProperty("room_id", out var ridEl) ? ridEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(roomId))
        {
            roomId = ruleId switch
            {
                "R29" => "203",
                "R30" => "305",
                _ => null,
            };
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        var blockedDays = ParseBlockedDays(p);
        if (blockedDays.Count == 0)
        {
            blockedDays = ruleId switch
            {
                "R29" => ["wednesday", "thursday"],
                "R30" => ["wednesday", "friday"],
                _ => blockedDays,
            };
        }

        rooms[roomId] = new RoomInfo(roomId, blockedDays, ruleId);
    }
}
