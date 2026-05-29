using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R32TeacherSpecificDaysEnforcer : IRuleEnforcer
{
    public string RuleId => "R32";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R32").DefaultPenalty;
        foreach (var d in ctx.Demands)
        {
            if (!ctx.Catalogs.Teachers.TryGetValue(d.Demand.TeacherId, out var teacher))
            {
                continue;
            }

            SchedulingConstraintHelper.AddForbiddenStartsViolation(
                ctx, "R32", penalty, $"{d.Demand.Id}@blocked", d, teacher.BlockedRuleStartIndices);
        }
    }
}
