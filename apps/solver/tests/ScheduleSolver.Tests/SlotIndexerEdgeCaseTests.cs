using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Tests;

public class SlotIndexerEdgeCaseTests
{
    [Fact]
    public void Two_weeks_expands_horizon_beyond_slot_count()
    {
        using var input = InputLoader.Load(SolverTestPaths.SampleInput("edge-two-weeks"));
        var indexer = SlotIndexer.FromInput(input);

        Assert.Equal(2, indexer.Slots.Count);
        Assert.Equal(2, indexer.Weeks);
        Assert.True(indexer.Horizon > indexer.Slots.Count);
    }

    [Fact]
    public void Empty_slots_yield_zero_horizon()
    {
        using var input = InputLoader.Load(SolverTestPaths.SampleInput("edge-empty-calendar"));
        var indexer = SlotIndexer.FromInput(input);

        Assert.Empty(indexer.Slots);
        Assert.Equal(0, indexer.Horizon);
    }

    [Fact]
    public void TryGetIndex_resolves_slot_id()
    {
        using var input = InputLoader.Load(SolverTestPaths.SampleInput("synthetic-small"));
        var indexer = SlotIndexer.FromInput(input);

        Assert.True(indexer.TryGetIndex("mon-1", out var idx));
        Assert.Equal(0, idx);
    }
}
