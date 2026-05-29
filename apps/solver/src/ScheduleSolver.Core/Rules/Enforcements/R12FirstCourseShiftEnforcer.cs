using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R12FirstCourseShiftEnforcer : IRuleEnforcer
{
    public string RuleId => "R12";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R12").DefaultPenalty;
        var disallowed = SchedulingConstraintHelper.IndicesWhereSlotIndexNotIn(ctx.Indexer, 1, 4);
        if (disallowed.Count == 0)
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
                ctx, "R12", penalty, $"{d.Demand.Id}@shift", d, disallowed);
        }
    }
}
