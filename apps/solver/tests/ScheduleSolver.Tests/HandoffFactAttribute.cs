namespace ScheduleSolver.Tests;

/// <summary>
/// Opt-in only: handoff CP-SAT runs inside testhost and can use 10+ GB RAM.
/// Default <c>dotnet test</c> skips these. Set SCHED_RUN_HANDOFF_DIAGNOSTIC=1 to enable.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HandoffFactAttribute : FactAttribute
{
    public HandoffFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("SCHED_RUN_HANDOFF_DIAGNOSTIC"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Handoff CP-SAT skipped in testhost (high RAM). " +
                   "Use: $env:SCHED_RUN_HANDOFF_DIAGNOSTIC=1; dotnet test --filter HandoffDiagnostic. " +
                   "Or run .\\scripts\\compare-handoff-ab.ps1 with DevHost watchdog.";
        }
    }
}
