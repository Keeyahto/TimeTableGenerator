using ScheduleSolver.Core;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Solver;

namespace ScheduleSolver.Tests;

public class ModelBudgetGuardTests
{
    [Fact]
    public void Small_sample_passes_budget_guard()
    {
        using var input = InputLoader.Load(SolverTestPaths.SampleInput("synthetic-small"));
        Environment.SetEnvironmentVariable("SCHED_ALLOW_LARGE_MODEL", null);
        Assert.Null(ModelBudgetGuard.Check(input));
    }

    [Fact]
    public async Task Diagnostic_handoff_blocked_without_allow_large_model_flag()
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

        Environment.SetEnvironmentVariable("SCHED_ALLOW_LARGE_MODEL", null);
        var output = SolverTestPaths.TempOutput();
        try
        {
            var (result, json) = await SolverTestHelper.RunAsync(new SolverRunOptions
            {
                InputPath = path,
                OutputPath = output,
                Mode = SolverMode.Diagnostic,
                TimeLimitSec = 5,
                AllowLargeModel = false,
            });

            Assert.Equal(1, result.ExitCode);
            var warnings = json["warnings"] as System.Text.Json.Nodes.JsonArray;
            Assert.NotNull(warnings);
            Assert.Contains(warnings, w => w?["code"]?.GetValue<string>() == "MODEL_TOO_LARGE");
        }
        finally
        {
            SolverTestHelper.Cleanup(output);
            Environment.SetEnvironmentVariable("SCHED_ALLOW_LARGE_MODEL", null);
        }
    }
}
