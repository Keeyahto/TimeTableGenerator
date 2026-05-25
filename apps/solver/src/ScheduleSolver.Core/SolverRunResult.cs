namespace ScheduleSolver.Core;

public sealed class SolverRunResult
{
    public required int ExitCode { get; init; }
    public required string Status { get; init; }
}
