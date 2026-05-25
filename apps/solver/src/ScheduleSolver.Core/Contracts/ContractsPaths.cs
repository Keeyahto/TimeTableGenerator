using ScheduleSolver.Core.Infrastructure;

namespace ScheduleSolver.Core.Contracts;

public sealed class ContractsPaths
{
    public ContractsPaths(string repoRoot)
    {
        var contractsDir = Path.Combine(repoRoot, "packages", "shared-contracts");
        SolverInput01 = Path.Combine(contractsDir, "solver-input.schema.json");
        SolverInputV11 = Path.Combine(contractsDir, "solver-input-v1_1.schema.json");
        SolverOutputV2 = Path.Combine(contractsDir, "solver-output-v2.schema.json");
    }

    public string SolverInput01 { get; }
    public string SolverInputV11 { get; }
    public string SolverOutputV2 { get; }

    public static ContractsPaths FromRepo()
    {
        return new ContractsPaths(RepoRoot.ResolveFromEnvOrWalk());
    }
}
