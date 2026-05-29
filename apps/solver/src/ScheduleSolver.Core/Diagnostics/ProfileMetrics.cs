using System.Text.Json;
using System.Text.Json.Nodes;
using ScheduleSolver.Core.Input;

namespace ScheduleSolver.Core.Diagnostics;

public static class ProfileMetrics
{
    public static int DemandCount(ParsedInput input) => CountArray(input.Root, "lesson_demands");

    public static Dictionary<string, object> Compute(ParsedInput input)
    {
        var root = input.Root;
        var demandCount = DemandCount(input);
        var slotCount = CountCalendarSlots(root);
        var groupCount = CountArray(root, "groups");
        var teacherCount = CountArray(root, "teachers");
        var roomCount = CountArray(root, "rooms");

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["schema_version"] = input.SchemaVersion,
            ["lesson_demands"] = demandCount,
            ["calendar_slots"] = slotCount,
            ["groups"] = groupCount,
            ["teachers"] = teacherCount,
            ["rooms"] = roomCount,
            ["estimated_primary_variables"] = demandCount * Math.Max(slotCount, 1) * 2,
        };
    }

    public static void AttachToRulesByStatus(JsonObject report, Dictionary<string, object> profile)
    {
        var buckets = report["rules_by_status"] as JsonObject ?? new JsonObject();
        buckets["profile"] = JsonSerializer.SerializeToNode(profile);
        report["rules_by_status"] = buckets;
        report["status"] = "PROFILE";
        report["cp_sat_status"] = "NOT_RUN";
    }

    private static int CountArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return arr.GetArrayLength();
    }

    private static int CountCalendarSlots(JsonElement root)
    {
        if (!root.TryGetProperty("calendar", out var cal) || cal.ValueKind != JsonValueKind.Object)
        {
            return 0;
        }

        if (cal.TryGetProperty("slots", out var slots) && slots.ValueKind == JsonValueKind.Array)
        {
            return slots.GetArrayLength();
        }

        return 0;
    }
}
