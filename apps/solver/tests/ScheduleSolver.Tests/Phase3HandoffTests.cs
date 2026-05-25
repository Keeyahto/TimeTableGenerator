using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class Phase3HandoffTests
{
    private static string? VariantAPath
    {
        get
        {
            var candidate = Path.Combine(
                SolverTestPaths.Root,
                "data",
                "solver_agent_full_handoff_v2",
                "02_canonical_solver_input_v1_1",
                "solver_input_real_v1",
                "variant_A_no_merge_bakirova_valieva.json");
            return File.Exists(candidate) ? candidate : null;
        }
    }

    [Fact]
    public async Task Profile_real_handoff_variant_A_when_present()
    {
        var input = VariantAPath;
        if (input is null)
        {
            return;
        }

        var output = SolverTestPaths.TempOutput();
        try
        {
            var (result, json) = await SolverTestHelper.RunAsync(new SolverRunOptions
            {
                InputPath = input,
                OutputPath = output,
                Mode = SolverMode.Profile,
                TimeLimitSec = 5,
            });

            Assert.Equal(0, result.ExitCode);
            var buckets = json["rules_by_status"] as System.Text.Json.Nodes.JsonObject;
            Assert.NotNull(buckets);
            var profile = buckets["profile"] as System.Text.Json.Nodes.JsonObject;
            Assert.NotNull(profile);
            Assert.True(profile!["lesson_demands"]!.GetValue<int>() > 300);
        }
        finally
        {
            SolverTestHelper.Cleanup(output);
        }
    }
}
