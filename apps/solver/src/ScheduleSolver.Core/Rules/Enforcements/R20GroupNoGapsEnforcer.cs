using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R20GroupNoGapsEnforcer : IRuleEnforcer
{
    public string RuleId => "R20";

    public void Apply(SchedulingBuildContext ctx)
    {
        GapSoftEnforcer.ApplyToPools(
            ctx,
            "R20",
            RuleClass.SOFT_STRONG,
            ctx.Demands.GroupBy(d => d.Demand.GroupId));
    }
}
