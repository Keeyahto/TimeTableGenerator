using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R21TeacherNoGapsEnforcer : IRuleEnforcer
{
    public string RuleId => "R21";

    public void Apply(SchedulingBuildContext ctx)
    {
        GapSoftEnforcer.ApplyToPools(
            ctx,
            "R21",
            RuleClass.SOFT_MEDIUM,
            ctx.Demands.GroupBy(d => d.Demand.TeacherId));
    }
}
