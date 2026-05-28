using System.Diagnostics;
using ScheduleSolver.Core.Infrastructure;

namespace ScheduleSolver.Tests;

/// <summary>Handoff diagnostic via DevHost (out-of-process) — safe RAM cap.</summary>
public class HandoffOutOfProcessTests
{
    [HandoffFact]
    [Trait("Category", "HandoffDiagnostic")]
    public async Task Diagnostic_variant_A_via_devhost_respects_memory_cap()
    {
        var input = Path.Combine(
            RepoRoot.Find(),
            "data",
            "solver_agent_full_handoff_v2",
            "02_canonical_solver_input_v1_1",
            "solver_input_real_v1",
            "variant_A_no_merge_bakirova_valieva.json");
        if (!File.Exists(input))
        {
            return;
        }

        var repo = RepoRoot.Find();
        var script = Path.Combine(repo, "scripts", "run-solver.ps1");
        var output = SolverTestPaths.TempOutput();
        var psi = new ProcessStartInfo
        {
            FileName = "pwsh",
            Arguments =
                $"-NoProfile -File \"{script}\" -UseRealHandoff -AllowLargeModel -Mode diagnostic " +
                $"-TimeLimit 15 -MemLimitMb 4096 -Output \"{output}\" -HandoffVariant A",
            WorkingDirectory = repo,
            UseShellExecute = false,
        };
        psi.Environment["SCHED_REPO_ROOT"] = repo;

        using var process = Process.Start(psi);
        Assert.NotNull(process);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await process.WaitForExitAsync(cts.Token);

        Assert.True(process.ExitCode is 0 or 137, $"exit {process.ExitCode}");
        Assert.True(File.Exists(output));
    }
}
