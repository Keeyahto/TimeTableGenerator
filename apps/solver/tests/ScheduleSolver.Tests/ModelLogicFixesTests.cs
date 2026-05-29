using System.Text.Json.Nodes;
using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class ModelLogicFixesTests
{
    [Fact]
    public async Task Solve_r27_gym_two_slots_schedules_two_of_three_not_infeasible()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r27-gym-two-slots", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.NotEqual("INFEASIBLE", json["status"]?.GetValue<string>());
        var schedule = json["schedule"] as JsonArray;
        Assert.NotNull(schedule);
        Assert.True(schedule!.Count >= 2);
    }

    [Fact]
    public async Task Solve_r25_language_two_slots_can_misalign_with_soft_penalty()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r25-language-two-slots", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var schedule = json["schedule"] as JsonArray;
        Assert.NotNull(schedule);
        Assert.Equal(2, schedule!.Count);
        var starts = schedule.Select(e => e!["start_index"]!.GetValue<int>()).ToList();
        if (starts.Distinct().Count() == 2)
        {
            Assert.True(
                SolverTestHelper.HasSoftViolation(json, "R25"),
                "misaligned parallel language rows should incur R25 soft penalty");
        }
        else
        {
            Assert.Equal(starts[0], starts[1]);
        }
    }

    [Fact]
    public async Task Solve_r28_gym_same_teacher_two_slots_both_scheduled_different_starts()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r28-gym-two-slots", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var schedule = json["schedule"] as JsonArray;
        Assert.NotNull(schedule);
        Assert.Equal(2, schedule!.Count);
        var starts = schedule.Select(e => e!["start_index"]!.GetValue<int>()).Distinct().ToList();
        Assert.Equal(2, starts.Count);
    }

    [Fact]
    public async Task Solve_gap_filled_monday_row_has_no_R20_violation()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-gap-filled", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var schedule = json["schedule"] as JsonArray;
        Assert.NotNull(schedule);
        Assert.Equal(3, schedule!.Count);
        Assert.False(SolverTestHelper.HasSoftViolation(json, "R20"));
    }

    [Fact]
    public async Task Solve_gap_soft_still_flags_window_when_middle_empty()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-gap-soft", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        Assert.True(SolverTestHelper.HasSoftViolation(json, "R20"));
    }

    [Fact]
    public async Task Solve_r18_class_hour_not_forced_on_overloaded_day_without_ch()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("synthetic-r18-class-hour-day", SolverMode.Solve);

        Assert.Equal(0, result.ExitCode);
        var schedule = json["schedule"] as JsonArray;
        Assert.NotNull(schedule);
        var chStart = schedule!
            .First(e => e!["demand_id"]!.GetValue<string>() == "ch1")!["start_index"]!
            .GetValue<int>();
        var othersOnChDay = schedule
            .Where(e => e!["demand_id"]!.GetValue<string>() != "ch1")
            .Count(e => e!["start_index"]!.GetValue<int>() == chStart);
        Assert.True(othersOnChDay <= 3 || !SolverTestHelper.HasSoftViolation(json, "R18"));
    }

    [Fact]
    public async Task Validate_missing_teacher_fails_precheck()
    {
        var path = SolverTestPaths.TempOutput();
        var inputPath = Path.ChangeExtension(path, ".input.json");
        await File.WriteAllTextAsync(inputPath, """
            {
              "schema_version": "0.1",
              "calendar": { "weeks": 1, "slots": [{ "id": "mon-1", "day": "monday", "index": 1 }] },
              "groups": [{ "id": "g1" }],
              "teachers": [{ "id": "t1" }],
              "rooms": [{ "id": "101" }],
              "subjects": [{ "id": "math" }],
              "lesson_demands": [
                { "id": "d1", "group_id": "g1", "subject_id": "math", "room_id": "101", "hours_per_week": 1 }
              ],
              "constraints": { "hard": [], "soft": [] },
              "solver_config": { "mode": "validate" }
            }
            """);

        try
        {
            var (result, json) = await SolverTestHelper.RunAsync(new SolverRunOptions
            {
                InputPath = inputPath,
                OutputPath = path,
                Mode = SolverMode.Validate,
            });

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(
                (json["warnings"] as JsonArray) ?? [],
                w => w?["code"]?.GetValue<string>() == "R00_MISSING_TEACHER");
        }
        finally
        {
            SolverTestHelper.Cleanup(path);
            SolverTestHelper.Cleanup(inputPath);
        }
    }

    [Fact]
    public async Task Validate_duplicate_group_id_fails()
    {
        var path = SolverTestPaths.TempOutput();
        var inputPath = Path.ChangeExtension(path, ".input.json");
        await File.WriteAllTextAsync(inputPath, """
            {
              "schema_version": "0.1",
              "calendar": { "weeks": 1, "slots": [{ "id": "mon-1", "day": "monday", "index": 1 }] },
              "groups": [{ "id": "g1" }, { "id": "g1" }],
              "teachers": [{ "id": "t1" }],
              "rooms": [{ "id": "101" }],
              "subjects": [{ "id": "math" }],
              "lesson_demands": [],
              "constraints": { "hard": [], "soft": [] },
              "solver_config": { "mode": "validate" }
            }
            """);

        try
        {
            var (result, json) = await SolverTestHelper.RunAsync(new SolverRunOptions
            {
                InputPath = inputPath,
                OutputPath = path,
                Mode = SolverMode.Validate,
            });

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(
                (json["warnings"] as JsonArray) ?? [],
                w => w?["code"]?.GetValue<string>() == "R00_DUPLICATE_ID");
        }
        finally
        {
            SolverTestHelper.Cleanup(path);
            SolverTestHelper.Cleanup(inputPath);
        }
    }

    [Fact]
    public async Task RunAsync_missing_input_writes_error_json()
    {
        var path = SolverTestPaths.TempOutput();
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

        try
        {
            var (result, _) = await SolverTestHelper.RunAsync(new SolverRunOptions
            {
                InputPath = missing,
                OutputPath = path,
                Mode = SolverMode.Validate,
            });

            Assert.Equal(1, result.ExitCode);
            var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
            Assert.Equal("ERROR", json["status"]!.GetValue<string>());
            Assert.Contains(
                (json["warnings"] as JsonArray) ?? [],
                w => w?["code"]?.GetValue<string>() == "INPUT_LOAD_FAILED");
        }
        finally
        {
            SolverTestHelper.Cleanup(path);
        }
    }
}
