using System.Text.Json.Nodes;
using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class ProfileDiagnosticEdgeCaseTests
{
    [Fact]
    public async Task Profile_two_week_calendar_includes_slot_counts()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-two-weeks", SolverMode.Profile);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("PROFILE", json["status"]!.GetValue<string>());
        var buckets = json["rules_by_status"] as JsonObject;
        Assert.NotNull(buckets);
        var profile = buckets["profile"] as JsonObject;
        Assert.NotNull(profile);
        Assert.Equal(2, profile["calendar_slots"]!.GetValue<int>());
        Assert.Equal(1, profile["lesson_demands"]!.GetValue<int>());
    }

    [Fact]
    public async Task Diagnostic_mode_runs_cp_sat_and_returns_status()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-small", SolverMode.Diagnostic);

        Assert.Equal(0, result.ExitCode);
        var status = json["status"]!.GetValue<string>();
        Assert.True(status is "DIAGNOSTIC" or "OK");
        var cp = json["cp_sat_status"]!.GetValue<string>();
        Assert.NotEqual("NOT_RUN", cp);
    }

    [Fact]
    public async Task Profile_synthetic_small_lists_stub_and_enforced_buckets()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-small", SolverMode.Profile);

        Assert.Equal(0, result.ExitCode);
        var buckets = json["rules_by_status"] as JsonObject;
        Assert.NotNull(buckets);
        var enforced = buckets["enforced"] as JsonArray;
        var stub = buckets["stub"] as JsonArray;
        Assert.NotNull(enforced);
        Assert.NotNull(stub);
        Assert.Contains(enforced, x => x!.GetValue<string>() == "R00");
        Assert.Contains(enforced, x => x!.GetValue<string>() == "R16");
        Assert.Empty(stub!);
    }
}
