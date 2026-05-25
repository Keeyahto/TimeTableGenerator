using Google.OrTools.Sat;

namespace ScheduleSolver.Core.Model;

public sealed class DemandScheduleVars
{
    public required LessonDemandRow Demand { get; init; }
    public required IntVar Start { get; init; }
    public required BoolVar Presence { get; init; }
    public required IntervalVar Interval { get; init; }
}
