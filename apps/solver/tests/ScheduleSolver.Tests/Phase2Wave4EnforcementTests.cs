using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class Phase2Wave4EnforcementTests
{
    [Fact]
    public async Task Solve_r26_same_teacher_parallel_language_relaxed_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r25-r26-language", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R26", SolverTestHelper.RuleIds(json));
        Assert.True(
            SolverTestHelper.HasRelaxedViolation(json, "R26")
            || (json["unscheduled_lessons"] as System.Text.Json.Nodes.JsonArray)?.Count >= 1,
            "R26 clash or leave a parallel row unscheduled");
    }

    [Fact]
    public async Task Solve_r27_gym_allows_two_groups_one_unscheduled()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r27-gym", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R27", SolverTestHelper.RuleIds(json));
        var schedule = json["schedule"] as System.Text.Json.Nodes.JsonArray;
        var unscheduled = json["unscheduled_lessons"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(schedule);
        Assert.Equal(2, schedule!.Count);
        Assert.Single(unscheduled!);
    }

    [Fact]
    public async Task Solve_r32_blocked_thursday_relaxed_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r32-blocked", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R32", SolverTestHelper.RuleIds(json));
        var schedule = json["schedule"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(schedule);
        Assert.Single(schedule!);
        Assert.Equal(0, schedule[0]!["start_index"]!.GetValue<int>());
        Assert.True(SolverTestHelper.HasRelaxedViolation(json, "R32"));
    }
}
