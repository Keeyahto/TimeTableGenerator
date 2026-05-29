using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R14FirstCourseNoSaturdayEnforcer : IRuleEnforcer
{
    public string RuleId => "R14";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R14").DefaultPenalty;
        var saturdayStarts = SchedulingConstraintHelper.IndicesForDay(ctx.Indexer, "saturday");
        if (saturdayStarts.Count == 0)
        {
            return;
        }

        foreach (var d in ctx.Demands)
        {
            if (!ctx.Catalogs.Groups.TryGetValue(d.Demand.GroupId, out var group) || !group.IsFirstCourse)
            {
                continue;
            }

            SchedulingConstraintHelper.AddForbiddenStartsViolation(
                ctx, "R14", penalty, $"{d.Demand.Id}@sat", d, saturdayStarts);
        }
    }
}
