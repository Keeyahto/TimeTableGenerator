using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R11ThursdayMeetingEnforcer : IRuleEnforcer
{
    public string RuleId => "R11";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R11").DefaultPenalty;
        var thuSlot1Starts = SchedulingConstraintHelper.IndicesForDayAndSlotIndex(ctx.Indexer, "thursday", 1);
        if (thuSlot1Starts.Count == 0)
        {
            return;
        }

        foreach (var d in ctx.Demands)
        {
            if (!ctx.Catalogs.Teachers.TryGetValue(d.Demand.TeacherId, out var teacher)
                || !teacher.ThursdaySlot1Forbidden)
            {
                continue;
            }

            foreach (var badStart in thuSlot1Starts)
            {
                SchedulingConstraintHelper.AddForbiddenStartViolation(
                    ctx, "R11", penalty, $"{d.Demand.Id}@thu1_{badStart}", d, badStart);
            }
        }
    }
}
