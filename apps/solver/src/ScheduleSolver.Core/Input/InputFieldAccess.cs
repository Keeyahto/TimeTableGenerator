using System.Text.Json;

namespace ScheduleSolver.Core.Input;

internal static class InputFieldAccess
{
    public static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!element.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = prop.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static string? GetFirstTeacherId(JsonElement demand)
    {
        var direct = GetString(demand, "teacher_id");
        if (!string.IsNullOrEmpty(direct))
        {
            return direct;
        }

        if (!demand.TryGetProperty("teacher_options", out var options)
            || options.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var option in options.EnumerateArray())
        {
            if (option.ValueKind == JsonValueKind.String)
            {
                var tid = option.GetString();
                if (!string.IsNullOrWhiteSpace(tid))
                {
                    return tid;
                }
            }
        }

        return null;
    }

    public static string? GetFirstRoomId(JsonElement demand)
    {
        var direct = GetString(demand, "room_id");
        if (!string.IsNullOrEmpty(direct))
        {
            return direct;
        }

        if (!demand.TryGetProperty("allowed_room_ids", out var options)
            || options.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var option in options.EnumerateArray())
        {
            if (option.ValueKind == JsonValueKind.String)
            {
                var rid = option.GetString();
                if (!string.IsNullOrWhiteSpace(rid))
                {
                    return rid;
                }
            }
        }

        return null;
    }

    public static int CountNonEmptyStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var count = 0;
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            {
                count++;
            }
        }

        return count;
    }

    public static string NormalizeDayId(string? day)
    {
        if (string.IsNullOrWhiteSpace(day))
        {
            return "unknown";
        }

        return day.ToLowerInvariant() switch
        {
            "mon" => "monday",
            "tue" => "tuesday",
            "wed" => "wednesday",
            "thu" => "thursday",
            "fri" => "friday",
            "sat" => "saturday",
            "sun" => "sunday",
            _ => day,
        };
    }
}
