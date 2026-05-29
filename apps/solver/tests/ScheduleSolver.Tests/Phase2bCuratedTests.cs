using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class Phase2bCuratedTests
{
    [Fact]
    public async Task Validate_curated_v1_1_mini_succeeds()
    {
        var (result, _) = await SolverTestHelper.RunSampleAsync("curated-v1_1-mini", SolverMode.Validate);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Solve_curated_v1_1_mini_enforces_R29_in_model()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("curated-v1_1-mini", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R29", SolverTestHelper.RuleIds(json));
        Assert.Contains("R24", SolverTestHelper.RuleIds(json));
        var schedule = json["schedule"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(schedule);
        Assert.True(schedule!.Count >= 1);
    }

    [Fact]
    public async Task Validate_curated_v1_1_parity_real_schema_succeeds()
    {
        var (result, _) = await SolverTestHelper.RunSampleAsync("curated-v1_1-parity", SolverMode.Validate);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Solve_curated_v1_1_parity_schedules_upper_and_lower_slots()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("curated-v1_1-parity", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R31", SolverTestHelper.RuleIds(json));
        Assert.Contains("R16", SolverTestHelper.RuleIds(json));
        var schedule = json["schedule"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(schedule);
        Assert.Equal(3, schedule!.Count);
        var starts = schedule.Select(e => e!["start_index"]!.GetValue<int>()).ToHashSet();
        Assert.True(starts.Any(i => i >= 3), "lower-week lesson uses lower_mon_* indices");
        Assert.True(starts.Any(i => i <= 2), "upper-week lessons use upper_mon_* indices");
    }
}
