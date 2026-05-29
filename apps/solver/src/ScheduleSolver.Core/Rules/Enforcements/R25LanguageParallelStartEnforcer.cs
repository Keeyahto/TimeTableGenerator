using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R25LanguageParallelStartEnforcer : IRuleEnforcer
{
    public string RuleId => "R25";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R25").DefaultPenalty;
        foreach (var pool in ctx.Demands
                     .Where(d => !string.IsNullOrEmpty(d.Demand.LanguageParallelKey))
                     .GroupBy(d => d.Demand.LanguageParallelKey!))
        {
            var list = pool.ToList();
            if (list.Count < 2)
            {
                continue;
            }

            var anchor = list[0];
            for (var i = 1; i < list.Count; i++)
            {
                var other = list[i];
                var aligned = ctx.Model.NewBoolVar($"lang_aligned_{anchor.Demand.Id}_{other.Demand.Id}");
                ctx.Model.Add(anchor.Start == other.Start).OnlyEnforceIf(aligned);
                ctx.Model.AddImplication(aligned, anchor.Presence);
                ctx.Model.AddImplication(aligned, other.Presence);

                var viol = ctx.Violations.AddViolation(
                    ctx.Model, "R25", penalty, $"{anchor.Demand.Id}+{other.Demand.Id}", RuleClass.SOFT_STRONG);
                ctx.Model.AddBoolOr(new ILiteral[]
                {
                    anchor.Presence.Not(), other.Presence.Not(), aligned, viol,
                });
                ctx.Model.AddImplication(viol, anchor.Presence);
                ctx.Model.AddImplication(viol, other.Presence);
                ctx.Model.AddImplication(viol, aligned.Not());
            }
        }
    }
}
