using System.Text.Json;
using ScheduleSolver.Core.Input;

namespace ScheduleSolver.Core.Model;

internal static class WeekParityParser
{
    public static string? FromSlot(JsonElement slot, string slotId)
    {
        var explicitParity = InputFieldAccess.GetString(slot, "week_parity");
        if (!string.IsNullOrWhiteSpace(explicitParity))
        {
            return Normalize(explicitParity);
        }

        if (slotId.StartsWith("upper_", StringComparison.OrdinalIgnoreCase))
        {
            return "upper";
        }

        if (slotId.StartsWith("lower_", StringComparison.OrdinalIgnoreCase))
        {
            return "lower";
        }

        return null;
    }

    public static string? FromDemand(JsonElement demand)
    {
        var parity = InputFieldAccess.GetString(demand, "week_parity", "required_week_parity");
        return string.IsNullOrWhiteSpace(parity) ? null : Normalize(parity);
    }

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "upper" or "up" => "upper",
            "lower" or "down" => "lower",
            _ => value.Trim().ToLowerInvariant(),
        };
}
