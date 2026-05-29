using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R18ClassHourDayMaxOtherPairsEnforcer : IRuleEnforcer
{
    public string RuleId => "R18";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R18").DefaultPenalty;
        var slotsByDay = ctx.Indexer.Slots
            .GroupBy(s => s.Day ?? "unknown", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Index).ToList(), StringComparer.Ordinal);

        foreach (var groupDemands in ctx.Demands.GroupBy(d => d.Demand.GroupId))
        {
            var classHour = groupDemands.Where(d => LessonDemandRow.IsClassHour(d.Demand)).ToList();
            var other = groupDemands.Where(d => !LessonDemandRow.IsClassHour(d.Demand)).ToList();
            if (classHour.Count == 0 || other.Count == 0)
            {
                continue;
            }

            foreach (var (day, indices) in slotsByDay)
            {
                var classOnDayFlags = classHour
                    .Select(d => SchedulingConstraintHelper.CreateOnDayLiteral(ctx, d, indices, $"ch_{day}"))
                    .ToList();

                var classOnDay = ctx.Model.NewBoolVar($"class_on_{groupDemands.Key}_{day}");
                ctx.Model.AddBoolOr(classOnDayFlags).OnlyEnforceIf(classOnDay);
                ctx.Model.AddBoolAnd(classOnDayFlags.Select(f => (ILiteral)f.Not()).ToArray()).OnlyEnforceIf(classOnDay.Not());

                var otherOnDay = other
                    .Select(d => SchedulingConstraintHelper.CreateOnDayLiteral(ctx, d, indices, $"oth_{day}"))
                    .ToList();

                var excess = ctx.Model.NewIntVar(0, otherOnDay.Count, $"oth_excess_{groupDemands.Key}_{day}");
                ctx.Model.Add(excess >= LinearExpr.Sum(otherOnDay) - 3);

                var tooManyOthers = ctx.Model.NewBoolVar($"oth_many_{groupDemands.Key}_{day}");
                ctx.Model.Add(excess >= 1).OnlyEnforceIf(tooManyOthers);
                ctx.Model.Add(excess == 0).OnlyEnforceIf(tooManyOthers.Not());

                var viol = ctx.Violations.AddViolation(
                    ctx.Model, "R18", penalty, $"{groupDemands.Key}_{day}", RuleClass.SOFT_STRONG);
                ctx.Model.AddImplication(viol, classOnDay);
                ctx.Model.AddImplication(viol, tooManyOthers);
                ctx.Model.AddBoolOr(new ILiteral[] { classOnDay.Not(), tooManyOthers.Not(), viol });
            }
        }
    }
}
