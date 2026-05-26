using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R23RoomManagerOwnRoomEnforcer : IRuleEnforcer
{
    public string RuleId => "R23";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R23").DefaultPenalty;
        foreach (var d in ctx.Demands)
        {
            var roomId = d.Demand.RoomId;
            if (string.IsNullOrEmpty(roomId)
                || !ctx.Catalogs.Teachers.TryGetValue(d.Demand.TeacherId, out var teacher)
                || teacher.ManagedRoomIds.Count == 0)
            {
                continue;
            }

            if (teacher.ManagedRoomIds.Contains(roomId, StringComparer.Ordinal))
            {
                continue;
            }

            var viol = ctx.Violations.AddViolation(
                ctx.Model, "R23", penalty, $"{d.Demand.Id}@{roomId}", RuleClass.SOFT_STRONG);
            ctx.Model.Add(viol == d.Presence);
        }
    }
}
