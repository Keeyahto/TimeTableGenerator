using Google.OrTools.Sat;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public sealed class R27GymMaxParallelEnforcer : IRuleEnforcer
{
    public string RuleId => "R27";

    public void Apply(SchedulingBuildContext ctx)
    {
        var validStarts = ctx.Indexer.Slots.Select(s => s.Index).ToList();
        foreach (var room in ctx.Catalogs.Rooms.Values.Where(r => r.IsGym && r.MaxParallelGroups > 1))
        {
            var pool = ctx.Demands
                .Where(d => string.Equals(d.Demand.RoomId, room.Id, StringComparison.Ordinal))
                .ToList();

            if (pool.Count == 0)
            {
                continue;
            }

            foreach (var start in validStarts)
            {
                var atStart = new List<BoolVar>();
                foreach (var d in pool)
                {
                    var placed = ctx.Model.NewBoolVar($"gym_{room.Id}_{d.Demand.Id}_{start}");
                    ctx.Model.Add(d.Start == start).OnlyEnforceIf(placed);
                    ctx.Model.Add(d.Start != start).OnlyEnforceIf(placed.Not());
                    ctx.Model.AddImplication(placed, d.Presence);
                    atStart.Add(placed);
                }

                ctx.Model.Add(LinearExpr.Sum(atStart) <= room.MaxParallelGroups);
            }
        }
    }
}
