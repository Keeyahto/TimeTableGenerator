using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class Phase2Wave3EnforcementTests
{
    [Fact]
    public async Task Solve_r24_two_math_same_group_monday_soft_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r24-subject", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R24", SolverTestHelper.RuleIds(json));
        var schedule = json["schedule"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(schedule);
        Assert.Equal(2, schedule!.Count);
        Assert.True(SolverTestHelper.HasSoftViolation(json, "R24"));
    }

    [Fact]
    public async Task Solve_r29_room203_on_wednesday_relaxed_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r29-room203", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R29", SolverTestHelper.RuleIds(json));
        Assert.True(SolverTestHelper.HasRelaxedViolation(json, "R29"));
    }
}
