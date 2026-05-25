using ScheduleSolver.Core.Infrastructure;

namespace ScheduleSolver.Tests;

internal static class SolverTestPaths
{
    public static string Root => RepoRoot.Find();

    public static string SampleInput(string sampleName) =>
        Path.Combine(Root, "data", "samples", sampleName, "input.json");

    public static string TempOutput() =>
        Path.Combine(Path.GetTempPath(), $"solver-test-{Guid.NewGuid():N}.json");
}
