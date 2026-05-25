namespace ScheduleSolver.Core;

public sealed class SolverRunOptions
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public SolverMode Mode { get; init; } = SolverMode.Validate;
    public int TimeLimitSec { get; init; } = 30;
    public string? ExportDebugDir { get; init; }
    public string? DatasetVariant { get; init; }
}

public enum SolverMode
{
    Validate,
    Profile,
    Diagnostic,
    Solve,
}
