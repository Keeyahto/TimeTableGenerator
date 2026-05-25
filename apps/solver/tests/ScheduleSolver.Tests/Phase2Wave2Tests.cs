using ScheduleSolver.Core;
using ScheduleSolver.Core.Infrastructure;
using System.Text.Json.Nodes;

namespace ScheduleSolver.Tests;

public class Phase2Wave2Tests
{
    [Fact]
    public async Task Solve_wave2_sample_enables_R11_R20_in_model()
    {
        var repo = RepoRoot.Find();
        var input = Path.Combine(repo, "data", "samples", "synthetic-wave2", "input.json");
        var output = Path.Combine(Path.GetTempPath(), $"solver-w2-{Guid.NewGuid():N}.json");

        try
        {
            var result = await SolverApplication.RunAsync(new SolverRunOptions
            {
                InputPath = input,
                OutputPath = output,
                Mode = SolverMode.Solve,
                TimeLimitSec = 20,
            });

            Assert.Equal(0, result.ExitCode);
            var json = JsonNode.Parse(File.ReadAllText(output))!.AsObject();
            var enabled = json["enabled_rules"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);

            Assert.Contains("R11", enabled);
            Assert.Contains("R12", enabled);
            Assert.Contains("R20", enabled);
            Assert.Contains("R22", enabled);
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
