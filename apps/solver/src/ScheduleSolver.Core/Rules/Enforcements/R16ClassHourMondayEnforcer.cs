using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

/// <summary>R16 class hour Monday 1/5 is enforced structurally via <see cref="Model.AllowedStartResolver"/>.</summary>
public sealed class R16ClassHourMondayEnforcer : IRuleEnforcer
{
    public string RuleId => "R16";

    public void Apply(SchedulingBuildContext ctx)
    {
        if (!ctx.EnforcedRuleIds.Contains(RuleId, StringComparer.Ordinal))
        {
            ctx.EnforcedRuleIds.Add(RuleId);
        }
    }
}
