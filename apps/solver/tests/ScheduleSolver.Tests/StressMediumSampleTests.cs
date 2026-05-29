using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class StressMediumSampleTests
{
    [Fact]
    public async Task Validate_stress_medium_v1_1_succeeds()
    {
        var (result, _) = await SolverTestHelper.RunSampleAsync("stress-medium-v1_1", SolverMode.Validate);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Profile_stress_medium_includes_model_stats()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("stress-medium-v1_1", SolverMode.Profile);

        Assert.Equal(0, result.ExitCode);
        var profile = (json["rules_by_status"] as System.Text.Json.Nodes.JsonObject)?["profile"]
            as System.Text.Json.Nodes.JsonObject;
        Assert.NotNull(profile?["model_stats"]);
        Assert.True(profile!["lesson_demands"]!.GetValue<int>() >= 120);
        Assert.Equal(68, profile["calendar_slots"]!.GetValue<int>());
    }
}
