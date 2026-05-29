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
                var atStart = new List<LinearExpr>();
                foreach (var d in pool)
                {
                    var duration = Math.Min(d.Demand.DurationSlots, ctx.Indexer.Horizon);
                    var maxStart = Math.Max(0, ctx.Indexer.Horizon - duration);
                    var allowed = AllowedStartResolver.ComputeForDemand(d.Demand, ctx.Indexer)
                        .Where(s => s <= maxStart)
                        .ToHashSet();
                    if (!allowed.Contains(start))
                    {
                        continue;
                    }

                    atStart.Add(SchedulingConstraintHelper.CreateAtStartWhenPresentLiteral(
                        ctx, d, start, $"gym_{room.Id}"));
                }

                if (atStart.Count == 0)
                {
                    continue;
                }

                ctx.Model.Add(LinearExpr.Sum(atStart) <= room.MaxParallelGroups);
            }
        }
    }
}
