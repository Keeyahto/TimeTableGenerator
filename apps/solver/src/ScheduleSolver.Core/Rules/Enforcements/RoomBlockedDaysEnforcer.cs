using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class RoomBlockedDaysEnforcer(string ruleId) : IRuleEnforcer
{
    public string RuleId => ruleId;

    public void Apply(SchedulingBuildContext ctx)
    {
        var penalty = ctx.Registry.GetRequired(ruleId).DefaultPenalty;
        var rooms = ctx.Catalogs.Rooms.Values
            .Where(r => r.SourceRuleId == ruleId && r.BlockedDays.Count > 0)
            .ToList();

        if (rooms.Count == 0)
        {
            return;
        }

        foreach (var d in ctx.Demands)
        {
            var roomId = d.Demand.RoomId;
            if (string.IsNullOrEmpty(roomId))
            {
                continue;
            }

            if (!TryResolveRoom(rooms, roomId, out var room))
            {
                continue;
            }

            foreach (var day in room.BlockedDays)
            {
                foreach (var badStart in SchedulingConstraintHelper.IndicesForDay(ctx.Indexer, day))
                {
                    SchedulingConstraintHelper.AddForbiddenStartViolation(
                        ctx, ruleId, penalty, $"{d.Demand.Id}@{room.Id}_{day}_{badStart}", d, badStart);
                }
            }
        }
    }

    private static bool TryResolveRoom(
        IReadOnlyList<RoomInfo> configured,
        string demandRoomId,
        out RoomInfo room)
    {
        foreach (var r in configured)
        {
            if (string.Equals(r.Id, demandRoomId, StringComparison.Ordinal)
                || demandRoomId.EndsWith(r.Id, StringComparison.Ordinal))
            {
                room = r;
                return true;
            }
        }

        room = default!;
        return false;
    }
}
