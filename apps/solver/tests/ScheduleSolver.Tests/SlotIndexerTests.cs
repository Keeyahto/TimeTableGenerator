using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;
using ScheduleSolver.Core.Infrastructure;

namespace ScheduleSolver.Tests;

public class SlotIndexerTests
{
    [Fact]
    public void FromInput_synthetic_small_has_two_slots()
    {
        var repo = RepoRoot.Find();
        var path = Path.Combine(repo, "data", "samples", "synthetic-small", "input.json");
        using var input = InputLoader.Load(path);
        var indexer = SlotIndexer.FromInput(input);

        Assert.Equal(2, indexer.Slots.Count);
        Assert.Equal(3, indexer.Horizon);
        Assert.Equal("mon-1", indexer.Slots[0].Id);
    }
}
