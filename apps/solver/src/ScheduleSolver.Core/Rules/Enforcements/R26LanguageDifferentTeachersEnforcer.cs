using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R26LanguageDifferentTeachersEnforcer : IRuleEnforcer
{
    public string RuleId => "R26";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R26").DefaultPenalty;
        foreach (var pool in ctx.Demands
                     .Where(d => !string.IsNullOrEmpty(d.Demand.LanguageParallelKey))
                     .GroupBy(d => d.Demand.LanguageParallelKey!))
        {
            var list = pool.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    var d1 = list[i];
                    var d2 = list[j];
                    if (!string.Equals(d1.Demand.TeacherId, d2.Demand.TeacherId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var sameStart = ctx.Model.NewBoolVar($"lang_same_{d1.Demand.Id}_{d2.Demand.Id}");
                    ctx.Model.Add(d1.Start == d2.Start).OnlyEnforceIf(sameStart);
                    ctx.Model.Add(d1.Start != d2.Start).OnlyEnforceIf(sameStart.Not());

                    var viol = ctx.Violations.AddViolation(
                        ctx.Model, "R26", penalty, $"{d1.Demand.Id}+{d2.Demand.Id}", RuleClass.RELAXED_HARD);
                    ctx.Model.AddBoolOr(new ILiteral[]
                    {
                        d1.Presence.Not(), d2.Presence.Not(), sameStart.Not(), viol,
                    });
                    ctx.Model.AddImplication(viol, d1.Presence);
                    ctx.Model.AddImplication(viol, d2.Presence);
                    ctx.Model.AddImplication(viol, sameStart);
                }
            }
        }
    }
}
