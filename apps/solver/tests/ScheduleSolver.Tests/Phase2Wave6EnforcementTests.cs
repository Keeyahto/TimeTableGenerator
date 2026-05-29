using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class Phase2Wave6EnforcementTests
{
    [Fact]
    public async Task Solve_r16_class_hour_monday_bad_slot_stranded_by_hard_domain()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r16-class-hour", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R16", SolverTestHelper.RuleIds(json));
        var unscheduled = json["unscheduled_lessons"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(unscheduled);
        Assert.Single(unscheduled!);
        Assert.Equal("NO_CALENDAR_SLOTS", unscheduled![0]!["reason"]!.GetValue<string>());
    }

    [Fact]
    public async Task Solve_r17_class_hour_wrong_teacher_relaxed_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r17-class-teacher", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R17", SolverTestHelper.RuleIds(json));
        Assert.True(SolverTestHelper.HasRelaxedViolation(json, "R17"));
    }

    [Fact]
    public async Task Solve_r18_class_hour_day_four_others_soft_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r18-class-hour-day", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R18", SolverTestHelper.RuleIds(json));
        Assert.True(SolverTestHelper.HasSoftViolation(json, "R18"));
    }

    [Fact]
    public async Task Solve_r28_gym_same_teacher_parallel_relaxed_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r28-gym-teachers", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R28", SolverTestHelper.RuleIds(json));
        var schedule = json["schedule"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(schedule);
        Assert.True(
            SolverTestHelper.HasRelaxedViolation(json, "R28") || schedule.Count < 2,
            "same teacher cannot parallelize in gym without R28 clash or leaving a row unscheduled");
    }

    [Fact]
    public async Task Solve_r31_upper_demand_on_lower_slot_stranded_by_hard_domain()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r31-week-parity", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("R31", SolverTestHelper.RuleIds(json));
        var unscheduled = json["unscheduled_lessons"] as System.Text.Json.Nodes.JsonArray;
        Assert.NotNull(unscheduled);
        Assert.Single(unscheduled!);
        Assert.Equal("NO_CALENDAR_SLOTS", unscheduled![0]!["reason"]!.GetValue<string>());
    }
}
