using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Tests;

public class RuleRegistryTests
{
    [Fact]
    public void Default_registry_has_R00_through_R40()
    {
        var registry = RuleRegistry.CreateDefault();
        var ids = registry.All.Select(r => r.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

        Assert.Equal(41, ids.Count);
        Assert.Equal("R00", ids[0]);
        Assert.Equal("R40", ids[^1]);
    }

    [Fact]
    public void R00_is_enforced_precheck()
    {
        var registry = RuleRegistry.CreateDefault();
        var r00 = registry.GetRequired("R00");

        Assert.Equal(EnforcementStatus.Enforced, r00.DefaultStatus);
        Assert.Equal(RuleClass.PRECHECK, r00.Class);
    }

    [Fact]
    public void Stub_rules_emit_warnings_list()
    {
        var registry = RuleRegistry.CreateDefault();
        var stubs = registry.StubRuleWarnings();

        Assert.DoesNotContain("R00", stubs);
        Assert.DoesNotContain("R08", stubs);
        Assert.Contains("R25", stubs);
    }
}
