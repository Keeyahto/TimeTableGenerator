using ScheduleSolver.Core;
using ScheduleSolver.Core.Infrastructure;
using System.Text.Json.Nodes;

namespace ScheduleSolver.Tests;

public class Phase2EnforcementTests
{
    [Fact]
    public async Task Solve_phase2_sample_reports_relaxed_violations()
    {
        var repo = RepoRoot.Find();
        var input = Path.Combine(repo, "data", "samples", "synthetic-phase2", "input.json");
        var output = Path.Combine(Path.GetTempPath(), $"solver-p2-{Guid.NewGuid():N}.json");

        try
        {
            var result = await SolverApplication.RunAsync(new SolverRunOptions
            {
                InputPath = input,
                OutputPath = output,
                Mode = SolverMode.Solve,
                TimeLimitSec = 15,
            });

            Assert.Equal(0, result.ExitCode);
            var json = JsonNode.Parse(File.ReadAllText(output))!.AsObject();
            var relaxed = json["relaxed_hard_violations"] as JsonArray;
            Assert.NotNull(relaxed);
            Assert.True(relaxed.Count >= 1);

            var enabled = json["enabled_rules"] as JsonArray;
            Assert.NotNull(enabled);
            Assert.Contains(enabled, n => n!.GetValue<string>() == "R08");
            Assert.Contains(enabled, n => n!.GetValue<string>() == "R19");
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
