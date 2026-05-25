using Google.OrTools.Sat;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Rules;
using ScheduleSolver.Core.Rules.Enforcements;

namespace ScheduleSolver.Core.Model;

public sealed class SchedulingModelBuild
{
    public CpModel Model { get; }
    public SlotIndexer Indexer { get; }
    public IReadOnlyList<DemandScheduleVars> Demands { get; }
    public ViolationTracker Violations { get; }
    public IReadOnlyList<string> EnforcedRuleIds { get; }

    private SchedulingModelBuild(
        CpModel model,
        SlotIndexer indexer,
        IReadOnlyList<DemandScheduleVars> demands,
        ViolationTracker violations,
        List<string> enforcedRuleIds)
    {
        Model = model;
        Indexer = indexer;
        Demands = demands;
        Violations = violations;
        EnforcedRuleIds = enforcedRuleIds;
    }

    public static SchedulingModelBuild Create(ParsedInput input, RuleRegistry registry)
    {
        var model = new CpModel();
        var indexer = SlotIndexer.FromInput(input);
        var rows = LessonDemandRow.FromInput(input.Root);
        var catalogs = InputCatalogs.FromRoot(input.Root);
        var demands = new List<DemandScheduleVars>();
        var enforced = new List<string> { "R00" };
        var violations = new ViolationTracker();

        if (indexer.Slots.Count == 0 || indexer.Horizon == 0)
        {
            return new SchedulingModelBuild(model, indexer, demands, violations, enforced);
        }

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

        var ctx = new SchedulingBuildContext
        {
            Model = model,
            Indexer = indexer,
            Demands = demands,
            Registry = registry,
            Catalogs = catalogs,
            Violations = violations,
            EnforcedRuleIds = enforced,
        };

        RuleEnforcerPipeline.ApplyAll(ctx);
        ApplyObjective(model, ctx);

        return new SchedulingModelBuild(model, indexer, demands, violations, ctx.EnforcedRuleIds);
    }

    private static void ApplyObjective(CpModel model, SchedulingBuildContext ctx)
    {
        var terms = new List<LinearExpr>();

        if (ctx.UnscheduledCountVar is not null)
        {
            terms.Add(LinearExpr.Term(ctx.UnscheduledCountVar, ctx.UnscheduledPenalty));
        }

        var penaltyExpr = ctx.Violations.BuildPenaltyExpr();
        if (!ReferenceEquals(penaltyExpr, LinearExpr.Constant(0)))
        {
            terms.Add(penaltyExpr);
        }

        if (terms.Count == 0)
        {
            return;
        }

        model.Minimize(LinearExpr.Sum(terms));
    }
}
