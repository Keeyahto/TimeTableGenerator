using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R17ClassHourTeacherEnforcer : IRuleEnforcer
{
    public string RuleId => "R17";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R17").DefaultPenalty;
        foreach (var d in ctx.Demands.Where(x => LessonDemandRow.IsClassHour(x.Demand)))
        {
            if (!ctx.Catalogs.Groups.TryGetValue(d.Demand.GroupId, out var group)
                || string.IsNullOrWhiteSpace(group.ClassTeacherId))
            {
                continue;
            }

            if (string.Equals(d.Demand.TeacherId, group.ClassTeacherId, StringComparison.Ordinal))
            {
                continue;
            }

            var viol = ctx.Violations.AddViolation(
                ctx.Model, "R17", penalty, $"{d.Demand.Id}@{d.Demand.TeacherId}", RuleClass.RELAXED_HARD);
            ctx.Model.AddBoolOr(new ILiteral[] { d.Presence.Not(), viol });
        }
    }
}
