using System.Text.Json;
using ScheduleSolver.Core.Input;

namespace ScheduleSolver.Tests;

public class InputCatalogsEdgeCaseTests
{
    [Fact]
    public void Rules_merge_R29_blocked_days_into_rooms()
    {
        var json = """
            {
              "groups": [],
              "teachers": [],
              "rooms": [{ "id": "203" }],
              "rules": [
                {
                  "id": "R29",
                  "params": { "room_id": "203", "blocked_days": ["wednesday"] }
                }
              ]
            }
            """;
        using var doc = JsonDocument.Parse(json);
        var catalogs = InputCatalogs.FromRoot(doc.RootElement);

        Assert.True(catalogs.Rooms.TryGetValue("203", out var room));
        Assert.Equal("R29", room.SourceRuleId);
        Assert.Contains("wednesday", room.BlockedDays, StringComparer.OrdinalIgnoreCase);
    }
}
