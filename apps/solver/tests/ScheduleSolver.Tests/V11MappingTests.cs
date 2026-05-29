using ScheduleSolver.Core;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;
using ScheduleSolver.Core.Validation;

namespace ScheduleSolver.Tests;

public class V11MappingTests
{
    [Fact]
    public async Task Validate_curated_v11_real_schema_succeeds()
    {
        var (result, _) = await SolverTestHelper.RunSampleAsync("curated-v1_1-mini", SolverMode.Validate);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Handoff_variant_A_passes_precheck_when_present()
    {
        var path = Path.Combine(
            SolverTestPaths.Root,
            "data",
            "solver_agent_full_handoff_v2",
            "02_canonical_solver_input_v1_1",
            "solver_input_real_v1",
            "variant_A_no_merge_bakirova_valieva.json");

        if (!File.Exists(path))
        {
            return;
        }

        using var input = InputLoader.Load(path);
        var issues = PrecheckValidator.Validate(input);
        var blocking = issues.Where(i => i.Code is not (
            "SCHEMA_FILE_NOT_FOUND"
            or "R00_MULTIPLE_TEACHER_OPTIONS"
            or "R00_MULTIPLE_ROOM_OPTIONS"
            or "R00_VACANT_PLACEHOLDER"));
        Assert.Empty(blocking);
    }

    [Fact]
    public void Handoff_variant_A_loads_hundreds_of_demands_when_present()
    {
        var path = Path.Combine(
            SolverTestPaths.Root,
            "data",
            "solver_agent_full_handoff_v2",
            "02_canonical_solver_input_v1_1",
            "solver_input_real_v1",
            "variant_A_no_merge_bakirova_valieva.json");

        if (!File.Exists(path))
        {
            return;
        }

        using var input = InputLoader.Load(path);
        var rows = LessonDemandRow.FromInput(input.Root);
        Assert.True(rows.Count > 300);
    }
}
