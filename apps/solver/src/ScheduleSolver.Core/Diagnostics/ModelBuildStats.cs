using System.Text.Json.Nodes;
using Google.OrTools.Sat;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Core.Diagnostics;

public sealed record ModelBuildStatsSnapshot(
    int DemandCount,
    int StrandedDemandCount,
    int ViolationLiteralCount,
    int Horizon,
    int SlotCount,
    string ModelStatsLine);

public static class ModelBuildStats
{
    public static ModelBuildStatsSnapshot Capture(ParsedInput input, RuleRegistry registry)
    {
        var build = SchedulingModelBuild.Create(input, registry);
        return FromBuild(build);
    }

    public static ModelBuildStatsSnapshot FromBuild(SchedulingModelBuild build)
    {
        var statsLine = build.Model.ModelStats();
        return new ModelBuildStatsSnapshot(
            build.Demands.Count,
            build.StrandedDemands.Count,
            build.Violations.Items.Count,
            build.Indexer.Horizon,
            build.Indexer.Slots.Count,
            statsLine);
    }

    public static JsonObject ToJson(ModelBuildStatsSnapshot snapshot) =>
        new()
        {
            ["demand_count"] = snapshot.DemandCount,
            ["stranded_demand_count"] = snapshot.StrandedDemandCount,
            ["violation_literal_count"] = snapshot.ViolationLiteralCount,
            ["horizon"] = snapshot.Horizon,
            ["slot_count"] = snapshot.SlotCount,
            ["model_stats"] = snapshot.ModelStatsLine,
        };
}
