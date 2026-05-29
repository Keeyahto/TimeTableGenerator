using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R28GymParallelDifferentTeachersEnforcer : IRuleEnforcer
{
    public string RuleId => "R28";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R28").DefaultPenalty;
        foreach (var room in ctx.Catalogs.Rooms.Values.Where(r => r.IsGym && r.MaxParallelGroups > 1))
        {
            var pool = ctx.Demands
                .Where(d => string.Equals(d.Demand.RoomId, room.Id, StringComparison.Ordinal))
                .ToList();

            for (var i = 0; i < pool.Count; i++)
            {
                for (var j = i + 1; j < pool.Count; j++)
                {
                    var d1 = pool[i];
                    var d2 = pool[j];
                    if (!string.Equals(d1.Demand.TeacherId, d2.Demand.TeacherId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var sameStart = ctx.Model.NewBoolVar($"g28_same_{d1.Demand.Id}_{d2.Demand.Id}");
                    ctx.Model.Add(d1.Start == d2.Start).OnlyEnforceIf(sameStart);
                    ctx.Model.AddImplication(sameStart, d1.Presence);
                    ctx.Model.AddImplication(sameStart, d2.Presence);

                    var viol = ctx.Violations.AddViolation(
                        ctx.Model,
                        "R28",
                        penalty,
                        $"{d1.Demand.Id}+{d2.Demand.Id}",
                        RuleClass.RELAXED_HARD);
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
