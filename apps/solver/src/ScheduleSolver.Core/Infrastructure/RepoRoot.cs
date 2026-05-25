namespace ScheduleSolver.Core.Infrastructure;

public static class RepoRoot
{
    public static string Find(string? startDirectory = null)
    {
        var dir = startDirectory ?? AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "package.json"))
                && Directory.Exists(Path.Combine(dir, "packages", "shared-contracts")))
            {
                return dir;
            }

            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == dir)
            {
                break;
            }

            dir = parent;
        }

        throw new DirectoryNotFoundException(
            "Monorepo root not found (expected package.json and packages/shared-contracts).");
    }

    public static string ResolveFromEnvOrWalk()
    {
        var env = Environment.GetEnvironmentVariable("CONTRACTS_ROOT");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return Path.GetFullPath(env);
        }

        return Find();
    }
}
