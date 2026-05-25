using System.Text.Json;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Tests;

public class RegistryEdgeCaseTests
{
    [Fact]
    public void Rules_array_can_disable_enforcement()
    {
        using var doc = JsonDocument.Parse("""
            {
              "rules": [
                { "id": "R08", "enforcement_status": "disabled" }
              ]
            }
            """);
        var registry = RuleRegistry.CreateDefault(doc);

        Assert.Equal(EnforcementStatus.Disabled, registry.GetRequired("R08").DefaultStatus);
        Assert.DoesNotContain("R08", registry.StubRuleWarnings());
    }

    [Fact]
    public void Rules_array_can_override_penalty()
    {
        using var doc = JsonDocument.Parse("""
            {
              "rules": [
                { "id": "R07", "penalty": 42 }
              ]
            }
            """);
        var registry = RuleRegistry.CreateDefault(doc);

        Assert.Equal(42, registry.GetRequired("R07").DefaultPenalty);
    }

    [Fact]
    public void Solve_output_lists_stub_rules_as_warnings_not_zero_violations()
    {
        var registry = RuleRegistry.CreateDefault();
        var stubs = registry.StubRuleWarnings();

        Assert.Contains("R23", stubs);
        Assert.True(stubs.Count >= 5);
    }
}
