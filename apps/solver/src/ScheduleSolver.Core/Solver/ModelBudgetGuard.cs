using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Solver;

public static class ModelBudgetGuard
{
    public const int DefaultMaxDemands = 150;
    public const long DefaultMaxEstimate = 120_000;

    public static string? Check(ParsedInput input)
    {
        if (IsLargeModelAllowed())
        {
            return null;
        }

        var maxDemands = ReadIntEnv("SCHED_MAX_DEMANDS", DefaultMaxDemands);
        var maxEstimate = ReadLongEnv("SCHED_MAX_MODEL_ESTIMATE", DefaultMaxEstimate);
        var demands = LessonDemandRow.FromInput(input.Root).Count;
        if (demands > maxDemands)
        {
            return $"MODEL_TOO_LARGE: {demands} lesson_demands exceeds cap {maxDemands}. " +
                   "Set SCHED_ALLOW_LARGE_MODEL=1 or pass --allow-large-model to opt in.";
        }

        var indexer = SlotIndexer.FromInput(input);
        var estimate = (long)demands * Math.Max(indexer.Horizon, 1) * 8;
        if (estimate > maxEstimate)
        {
            return $"MODEL_TOO_LARGE: estimated budget {estimate} exceeds cap {maxEstimate} " +
                   $"(demands={demands}, horizon={indexer.Horizon}). " +
                   "Set SCHED_ALLOW_LARGE_MODEL=1 or pass --allow-large-model to opt in.";
        }

        return null;
    }

    public static bool IsLargeModelAllowed() =>
        string.Equals(Environment.GetEnvironmentVariable("SCHED_ALLOW_LARGE_MODEL"), "1", StringComparison.Ordinal)
        || string.Equals(Environment.GetEnvironmentVariable("SCHED_ALLOW_LARGE_MODEL"), "true", StringComparison.OrdinalIgnoreCase);

    private static int ReadIntEnv(string name, int defaultValue) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0
            ? value
            : defaultValue;

    private static long ReadLongEnv(string name, long defaultValue) =>
        long.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0
            ? value
            : defaultValue;
}
