using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R08VirtualTeacherEnforcer : IRuleEnforcer
{
    public string RuleId => "R08";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R08").DefaultPenalty;
        foreach (var d in ctx.Demands)
        {
            if (!ctx.Catalogs.Teachers.TryGetValue(d.Demand.TeacherId, out var teacher) || !teacher.IsVirtual)
            {
                continue;
            }

            if (d.Demand.Vacant)
            {
                continue;
            }

            var viol = ctx.Violations.AddViolation(ctx.Model, "R08", penalty, d.Demand.Id);
            ctx.Model.Add(viol == d.Presence);
        }
    }
}
