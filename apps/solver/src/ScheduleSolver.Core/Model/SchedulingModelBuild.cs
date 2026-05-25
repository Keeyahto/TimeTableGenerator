using Google.OrTools.Sat;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Core.Model;

public sealed class SchedulingModelBuild
{
    public CpModel Model { get; }
    public SlotIndexer Indexer { get; }
    public IReadOnlyList<DemandScheduleVars> Demands { get; }
    public int UnscheduledPenalty { get; }

    private SchedulingModelBuild(
        CpModel model,
        SlotIndexer indexer,
        IReadOnlyList<DemandScheduleVars> demands,
        int unscheduledPenalty)
    {
        Model = model;
        Indexer = indexer;
        Demands = demands;
        UnscheduledPenalty = unscheduledPenalty;
    }

    public static SchedulingModelBuild Create(ParsedInput input, RuleRegistry registry)
    {
        var model = new CpModel();
        var indexer = SlotIndexer.FromInput(input);
        var rows = LessonDemandRow.FromInput(input.Root);
        var demands = new List<DemandScheduleVars>();

        if (indexer.Slots.Count == 0 || indexer.Horizon == 0)
        {
            return new SchedulingModelBuild(model, indexer, demands, 1000);
        }

        var r07 = registry.TryGet("R07", out var r7) ? r7 : null;
        var unscheduledPenalty = r07?.DefaultPenalty ?? 1000;

        foreach (var row in rows)
        {
            var duration = Math.Min(row.DurationSlots, indexer.Horizon);
            var maxStart = Math.Max(0, indexer.Horizon - duration);
            var start = model.NewIntVar(0, maxStart, $"start_{row.Id}");
            var presence = model.NewBoolVar($"presence_{row.Id}");
            var interval = model.NewOptionalFixedSizeIntervalVar(
                start, duration, presence, $"interval_{row.Id}");

            demands.Add(new DemandScheduleVars
            {
                Demand = row,
                Start = start,
                Presence = presence,
                Interval = interval,
            });

        }

        ApplyNoOverlap(model, demands);
        ApplySaturdayRule(model, indexer, demands);
        ApplyObjective(model, demands, unscheduledPenalty);

        return new SchedulingModelBuild(model, indexer, demands, unscheduledPenalty);
    }

    private static void ApplyNoOverlap(CpModel model, IReadOnlyList<DemandScheduleVars> demands)
    {
        AddPoolOverlap(model, demands, d => d.Demand.GroupId, "group");
        AddPoolOverlap(model, demands, d => d.Demand.TeacherId, "teacher");
        AddPoolOverlap(
            model,
            demands.Where(d => !string.IsNullOrEmpty(d.Demand.RoomId)).ToList(),
            d => d.Demand.RoomId!,
            "room");
    }

    private static void AddPoolOverlap(
        CpModel model,
        IReadOnlyList<DemandScheduleVars> demands,
        Func<DemandScheduleVars, string> keySelector,
        string _)
    {
        foreach (var pool in demands.GroupBy(keySelector))
        {
            if (pool.Count() > 1)
            {
                model.AddNoOverlap(pool.Select(d => d.Interval).ToArray());
            }
        }
    }

    private static void ApplySaturdayRule(
        CpModel model,
        SlotIndexer indexer,
        IReadOnlyList<DemandScheduleVars> demands)
    {
        var saturdayIndices = indexer.Slots
            .Where(s => string.Equals(s.Day, "saturday", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Index)
            .ToHashSet();

        if (saturdayIndices.Count == 0)
        {
            return;
        }

        foreach (var d in demands)
        {
            foreach (var slot in indexer.Slots)
            {
                if (!saturdayIndices.Contains(slot.Index))
                {
                    continue;
                }

                if (slot.SlotIndex is int si && (si < 1 || si > 4))
                {
                    var offset = slot.Index;
                    model.Add(d.Start != offset).OnlyEnforceIf(d.Presence);
                }
            }
        }
    }

    private static void ApplyObjective(
        CpModel model,
        IReadOnlyList<DemandScheduleVars> demands,
        int unscheduledPenalty)
    {
        if (demands.Count == 0)
        {
            return;
        }

        var scheduled = LinearExpr.Sum(demands.Select(d => d.Presence));
        var unscheduledCount = model.NewIntVar(0, demands.Count, "unscheduled_count");
        model.Add(unscheduledCount == demands.Count - scheduled);
        model.Minimize(LinearExpr.Term(unscheduledCount, unscheduledPenalty));
    }
}
