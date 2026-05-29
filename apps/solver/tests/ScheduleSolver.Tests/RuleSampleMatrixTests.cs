using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

public class RuleSampleMatrixTests
{
    public static TheoryData<string, string[]> SampleRules => new()
    {
        { "synthetic-r16-class-hour", ["R16"] },
        { "synthetic-r17-class-teacher", ["R17"] },
        { "synthetic-r18-class-hour-day", ["R18"] },
        { "synthetic-r24-subject", ["R24"] },
        { "synthetic-r27-gym", ["R27"] },
        { "synthetic-r28-gym-teachers", ["R28"] },
        { "synthetic-r31-week-parity", ["R31"] },
        { "curated-v1_1-mini", ["R24", "R29"] },
        { "curated-v1_1-parity", ["R16", "R31"] },
    };

    [Theory]
    [MemberData(nameof(SampleRules))]
    public async Task Solve_sample_enables_expected_rules(string sample, string[] expectedRules)
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync(sample, SolverMode.Solve, timeLimitSec: 20);

        Assert.Equal(0, result.ExitCode);
        var enabled = SolverTestHelper.RuleIds(json);
        foreach (var rule in expectedRules)
        {
            Assert.Contains(rule, enabled);
        }
    }
}
