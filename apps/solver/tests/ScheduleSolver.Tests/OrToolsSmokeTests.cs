using Google.OrTools.Sat;
using ScheduleSolver.Core.Solver;

namespace ScheduleSolver.Tests;

public class OrToolsSmokeTests
{
    [Fact]
    public void OrTools_cp_sat_returns_feasible_or_optimal()
    {
        var status = OrToolsSmoke.Run();
        Assert.True(
            status.Equals("OPTIMAL", StringComparison.OrdinalIgnoreCase)
            || status.Equals("FEASIBLE", StringComparison.OrdinalIgnoreCase),
            $"Expected OPTIMAL or FEASIBLE, got {status}");
    }
}
