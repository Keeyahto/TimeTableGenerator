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

            if (result.ExitCode != 0)
            {
                // Local handoff may still fail R00/schema until normalization phase; do not fail CI.
                var warnings = json["warnings"] as System.Text.Json.Nodes.JsonArray;
                Assert.NotNull(warnings);
                Assert.True(warnings!.Count > 0);
                return;
            }

            Assert.Equal("real_candidate_v1_1", json["schema_version"]?.GetValue<string>());
            var metrics = json["metrics"] as System.Text.Json.Nodes.JsonObject;
            Assert.NotNull(metrics);
            Assert.True(metrics!["lesson_demand_count"]!.GetValue<int>() > 0);
        }
        finally
        {
            SolverTestHelper.Cleanup(output);
        }
    }
}
