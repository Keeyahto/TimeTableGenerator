using System.Text.Json;

namespace ScheduleSolver.Core.Input;

public sealed class TeacherInfo
{
    public required string Id { get; init; }
    public bool IsVirtual { get; init; }
    public IReadOnlyList<int> ForbiddenStartIndices { get; init; } = [];
}

public sealed record GroupInfo(string Id, int? MaxLessonsPerDay);

public sealed class InputCatalogs
{
    public IReadOnlyDictionary<string, TeacherInfo> Teachers { get; init; } =
        new Dictionary<string, TeacherInfo>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, GroupInfo> Groups { get; init; } =
        new Dictionary<string, GroupInfo>(StringComparer.Ordinal);

    public static InputCatalogs FromRoot(JsonElement root)
    {
        var teachers = ParseTeachers(root);
        var groups = ParseGroups(root);
        MergeRuleParams(root, teachers, groups);
        return new InputCatalogs { Teachers = teachers, Groups = groups };
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

            map[id] = new TeacherInfo { Id = id, IsVirtual = isVirtual, ForbiddenStartIndices = forbidden };
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

            map[id] = new GroupInfo(id, maxPerDay);
        }

        return map;
    }

    private static void MergeRuleParams(
        JsonElement root,
        Dictionary<string, TeacherInfo> teachers,
        Dictionary<string, GroupInfo> groups)
    {
        if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var rule in rules.EnumerateArray())
        {
            var id = rule.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (id == "R19" && rule.TryGetProperty("params", out var p))
            {
                if (p.TryGetProperty("max_lessons_per_day", out var maxEl) && maxEl.TryGetInt32(out var max))
                {
                    foreach (var key in groups.Keys.ToList())
                    {
                        var g = groups[key];
                        groups[key] = g with { MaxLessonsPerDay = max };
                    }
                }
            }
        }
    }
}
