using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R22AdminNoSaturdayEnforcer : IRuleEnforcer
{
    public string RuleId => "R22";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R22").DefaultPenalty;
        var saturdayStarts = SchedulingConstraintHelper.IndicesForDay(ctx.Indexer, "saturday");
        if (saturdayStarts.Count == 0)
        {
            return;
        }

        foreach (var d in ctx.Demands)
        {
            if (!ctx.Catalogs.Teachers.TryGetValue(d.Demand.TeacherId, out var teacher) || !teacher.IsAdmin)
            {
                continue;
            }

            SchedulingConstraintHelper.AddForbiddenStartsViolation(
                ctx, "R22", penalty, $"{d.Demand.Id}@admin_sat", d, saturdayStarts);
        }
    }
}
