using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R07UnscheduledEnforcer : IRuleEnforcer
{
    public string RuleId => "R07";

    public void Apply(SchedulingBuildContext ctx)
    {
        if (ctx.Demands.Count == 0)
        {
            return;
        }

        ctx.UnscheduledPenalty = ctx.Registry.GetRequired("R07").DefaultPenalty;
        var scheduled = LinearExpr.Sum(ctx.Demands.Select(d => d.Presence));
        var unscheduledCount = ctx.Model.NewIntVar(0, ctx.Demands.Count, "unscheduled_count");
        ctx.Model.Add(unscheduledCount == ctx.Demands.Count - scheduled);
        ctx.UnscheduledCountVar = unscheduledCount;
    }
}
