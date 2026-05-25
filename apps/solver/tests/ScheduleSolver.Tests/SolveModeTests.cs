using ScheduleSolver.Core;
using ScheduleSolver.Core.Infrastructure;
using System.Text.Json.Nodes;

namespace ScheduleSolver.Tests;

public class SolveModeTests
{
    [Fact]
    public async Task Solve_synthetic_small_is_feasible_with_schedule()
    {
        var repo = RepoRoot.Find();
        var input = Path.Combine(repo, "data", "samples", "synthetic-small", "input.json");
        var output = Path.Combine(Path.GetTempPath(), $"solver-solve-{Guid.NewGuid():N}.json");

        try
        {
            var result = await SolverApplication.RunAsync(new SolverRunOptions
            {
                InputPath = input,
                OutputPath = output,
                Mode = SolverMode.Solve,
                TimeLimitSec = 10,
            });

            Assert.Equal(0, result.ExitCode);
            Assert.Equal("OK", result.Status);

            var json = JsonNode.Parse(File.ReadAllText(output))!.AsObject();
            Assert.Equal("OK", json["status"]!.GetValue<string>());
            var cpStatus = json["cp_sat_status"]!.GetValue<string>();
            Assert.True(
                cpStatus.Equals("Optimal", StringComparison.OrdinalIgnoreCase)
                || cpStatus.Equals("Feasible", StringComparison.OrdinalIgnoreCase),
                $"cp_sat_status={cpStatus}");
            var schedule = json["schedule"] as JsonArray;
            Assert.NotNull(schedule);
            Assert.True(schedule.Count >= 1);
        }
        finally
        {
            if (File.Exists(output))
            {
                File.Delete(output);
            }
        }
    }
}
