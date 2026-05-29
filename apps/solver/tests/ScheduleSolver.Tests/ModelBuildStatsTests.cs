using ScheduleSolver.Core.Diagnostics;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Tests;

public class ModelBuildStatsTests
{
    [Fact]
    public void Stress_medium_build_has_fewer_phantom_constraints_than_slot_reification()
    {
        using var input = InputLoader.Load(SolverTestPaths.SampleInput("stress-medium-v1_1"));
        var registry = RuleRegistry.CreateDefault(input.Document);
        var snapshot = ModelBuildStats.Capture(input, registry);

        Assert.True(snapshot.DemandCount >= 120);
        Assert.Equal(68, snapshot.SlotCount);
        Assert.Contains("variables", snapshot.ModelStatsLine, StringComparison.OrdinalIgnoreCase);
        Assert.True(snapshot.ViolationLiteralCount > 0);
    }
}
