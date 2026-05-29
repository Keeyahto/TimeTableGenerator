using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R10TeacherUnavailableEnforcer : IRuleEnforcer
{
    public string RuleId => "R10";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R10").DefaultPenalty;
        foreach (var d in ctx.Demands)
        {
            if (!ctx.Catalogs.Teachers.TryGetValue(d.Demand.TeacherId, out var teacher))
            {
                continue;
            }

            SchedulingConstraintHelper.AddForbiddenStartsViolation(
                ctx, "R10", penalty, d.Demand.Id, d, teacher.ForbiddenStartIndices);
        }
    }
}
