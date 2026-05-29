using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

/// <summary>R31 week parity is enforced structurally via <see cref="Model.AllowedStartResolver"/>.</summary>
public sealed class R31WeekParityEnforcer : IRuleEnforcer
{
    public string RuleId => "R31";

    public void Apply(SchedulingBuildContext ctx)
    {
        if (!ctx.EnforcedRuleIds.Contains(RuleId, StringComparer.Ordinal))
        {
            ctx.EnforcedRuleIds.Add(RuleId);
        }
    }
}
