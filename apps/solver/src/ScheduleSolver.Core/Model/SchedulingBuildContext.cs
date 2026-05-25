using Google.OrTools.Sat;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Core.Model;

public sealed class SchedulingBuildContext
{
    public required CpModel Model { get; init; }
    public required SlotIndexer Indexer { get; init; }
    public required IReadOnlyList<DemandScheduleVars> Demands { get; init; }
    public required RuleRegistry Registry { get; init; }
    public required InputCatalogs Catalogs { get; init; }
    public required ViolationTracker Violations { get; init; }
    public required List<string> EnforcedRuleIds { get; init; }

    public IntVar? UnscheduledCountVar { get; set; }
    public int UnscheduledPenalty { get; set; }
}
