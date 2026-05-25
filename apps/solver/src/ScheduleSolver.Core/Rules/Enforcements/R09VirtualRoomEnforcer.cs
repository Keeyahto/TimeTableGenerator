using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R09VirtualRoomEnforcer : IRuleEnforcer
{
    public string RuleId => "R09";

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired("R09").DefaultPenalty;
        foreach (var d in ctx.Demands)
        {
            var roomId = d.Demand.RoomId;
            var isVirtual = string.IsNullOrEmpty(roomId)
                            || roomId.StartsWith("virtual:", StringComparison.OrdinalIgnoreCase)
                            || roomId.StartsWith("virtual_", StringComparison.OrdinalIgnoreCase);
            if (!isVirtual)
            {
                continue;
            }

            var viol = ctx.Violations.AddViolation(ctx.Model, "R09", penalty, d.Demand.Id);
            ctx.Model.Add(viol == d.Presence);
        }
    }
}
