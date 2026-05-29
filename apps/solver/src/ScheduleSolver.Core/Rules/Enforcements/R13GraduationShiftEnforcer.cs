using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R13GraduationShiftEnforcer : IRuleEnforcer
{
    public string RuleId => "R13";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R13").DefaultPenalty;
        var disallowed = SchedulingConstraintHelper.IndicesWhereSlotIndexNotIn(ctx.Indexer, 1, 4);
        if (disallowed.Count == 0)
        {
            return;
        }

        foreach (var d in ctx.Demands)
        {
            if (!ctx.Catalogs.Groups.TryGetValue(d.Demand.GroupId, out var group) || !group.IsGraduation)
            {
                continue;
            }

            SchedulingConstraintHelper.AddForbiddenStartsViolation(
                ctx, "R13", penalty, $"{d.Demand.Id}@grad", d, disallowed);
        }
    }
}
