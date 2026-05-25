using ScheduleSolver.Core;
using ScheduleSolver.Core.Infrastructure;

namespace ScheduleSolver.Tests;

public class ValidateModeTests
{
    [Fact]
    public async Task Validate_synthetic_small_returns_VALIDATED()
    {
        var repo = RepoRoot.Find();
        var input = Path.Combine(repo, "data", "samples", "synthetic-small", "input.json");
        var output = Path.Combine(Path.GetTempPath(), $"solver-test-{Guid.NewGuid():N}.json");

        try
        {
            var result = await SolverApplication.RunAsync(new SolverRunOptions
            {
                InputPath = input,
                OutputPath = output,
                Mode = SolverMode.Validate,
            });

            Assert.Equal(0, result.ExitCode);
            Assert.Equal("VALIDATED", result.Status);
            Assert.True(File.Exists(output));
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
