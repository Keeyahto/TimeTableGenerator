using System.Text.Json.Nodes;
using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class SolveEdgeCaseTests
{
    [Fact]
    public async Task Solve_empty_calendar_leaves_all_unscheduled()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-empty-calendar", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var unscheduled = json["unscheduled_lessons"] as JsonArray;
        Assert.NotNull(unscheduled);
        Assert.Single(unscheduled!);
        Assert.Equal("NO_CALENDAR_SLOTS", unscheduled![0]!["reason"]!.GetValue<string>());
    }

    [Fact]
    public async Task Solve_same_teacher_one_calendar_slot_only_one_lesson_scheduled()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-teacher-overlap", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var schedule = json["schedule"] as JsonArray;
        var unscheduled = json["unscheduled_lessons"] as JsonArray;
        Assert.NotNull(schedule);
        Assert.NotNull(unscheduled);
        Assert.Single(schedule!);
        Assert.Single(unscheduled!);
    }

    [Fact]
    public async Task Solve_vacant_virtual_skips_R08_but_non_vacant_triggers_R08()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-virtual-vacant", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.True(SolverTestHelper.HasRelaxedViolation(json, "R08"));
        var relaxed = json["relaxed_hard_violations"] as JsonArray;
        var r08Labels = relaxed!
            .Where(e => e?["rule_id"]?.GetValue<string>() == "R08")
            .Select(e => e!["label"]!.GetValue<string>())
            .ToList();
        Assert.Contains(r08Labels, l => l.Contains("must-not-use-virtual", StringComparison.Ordinal));
        Assert.DoesNotContain(r08Labels, l => l.Contains("vacant-ok", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Solve_r19_two_lessons_same_day_one_gets_relaxed_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-r19-cap", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var schedule = json["schedule"] as JsonArray;
        var unscheduled = json["unscheduled_lessons"] as JsonArray;
        Assert.NotNull(schedule);
        Assert.Equal(2, schedule!.Count);
        Assert.True(SolverTestHelper.HasRelaxedViolation(json, "R19"));
    }

    [Fact]
    public async Task Solve_gap_soft_can_fire_R20_when_monday_span_has_window()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-gap-soft", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var schedule = json["schedule"] as JsonArray;
        Assert.NotNull(schedule);
        Assert.Equal(2, schedule!.Count);
        var starts = schedule
            .Select(e => e!["start_index"]!.GetValue<int>())
            .OrderBy(x => x)
            .ToList();
        Assert.Equal(new[] { 0, 2 }, starts);
        Assert.True(SolverTestHelper.HasSoftViolation(json, "R20"));
    }

    [Fact]
    public async Task Solve_phase2_rules_do_not_emit_RULE_NOT_ENFORCED_YET_for_enforced_ids()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-small", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var warnings = json["warnings"] as JsonArray ?? [];
        Assert.DoesNotContain(
            warnings,
            w => w?["code"]?.GetValue<string>() == "RULE_NOT_ENFORCED_YET"
                 && w?["rule_id"]?.GetValue<string>() is "R16" or "R28" or "R31");
    }
}
