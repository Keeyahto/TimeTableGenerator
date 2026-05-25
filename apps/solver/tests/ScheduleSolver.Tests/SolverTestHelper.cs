using System.Text.Json.Nodes;
using ScheduleSolver.Core;

namespace ScheduleSolver.Tests;

internal static class SolverTestHelper
{
    public static async Task<(SolverRunResult Result, JsonObject Json)> RunAsync(SolverRunOptions options)
    {
        var result = await SolverApplication.RunAsync(options);
        var json = JsonNode.Parse(await File.ReadAllTextAsync(options.OutputPath))!.AsObject();
        return (result, json);
    }

    public static async Task<(SolverRunResult Result, JsonObject Json)> RunSampleAsync(
        string sampleName,
        SolverMode mode,
        int timeLimitSec = 15)
    {
        var output = SolverTestPaths.TempOutput();
        try
        {
            var tuple = await RunAsync(new SolverRunOptions
            {
                InputPath = SolverTestPaths.SampleInput(sampleName),
                OutputPath = output,
                Mode = mode,
                TimeLimitSec = timeLimitSec,
            });
            return tuple;
        }
        finally
        {
            Cleanup(output);
        }
    }

    public static void Cleanup(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static HashSet<string> RuleIds(JsonObject json) =>
        (json["enabled_rules"] as JsonArray ?? [])
        .Select(n => n!.GetValue<string>())
        .ToHashSet(StringComparer.Ordinal);

    public static bool HasRelaxedViolation(JsonObject json, string ruleId) =>
        (json["relaxed_hard_violations"] as JsonArray ?? [])
        .Any(e => e?["rule_id"]?.GetValue<string>() == ruleId);

    public static bool HasSoftViolation(JsonObject json, string ruleId) =>
        (json["soft_violations"] as JsonArray ?? [])
        .Any(e => e?["rule_id"]?.GetValue<string>() == ruleId);
}
