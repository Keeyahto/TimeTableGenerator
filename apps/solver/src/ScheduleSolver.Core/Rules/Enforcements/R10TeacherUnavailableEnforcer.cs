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

            foreach (var badStart in teacher.ForbiddenStartIndices)
            {
                var atBad = ctx.Model.NewBoolVar($"at_bad_{d.Demand.Id}_{badStart}");
                ctx.Model.Add(d.Start == badStart).OnlyEnforceIf(atBad);
                ctx.Model.Add(d.Start != badStart).OnlyEnforceIf(atBad.Not());

                var viol = ctx.Violations.AddViolation(ctx.Model, "R10", penalty, $"{d.Demand.Id}@{badStart}");
                ctx.Model.AddBoolOr(new ILiteral[] { d.Presence.Not(), atBad.Not(), viol });
                ctx.Model.AddImplication(viol, d.Presence);
                ctx.Model.AddImplication(viol, atBad);
            }
        }
    }
}
