using System.Text.Json;
using ScheduleSolver.Core.Contracts;

namespace ScheduleSolver.Core.Validation;

public static class SchemaVersionValidator
{
    public static bool VersionMatchesSchemaFile(string schemaVersion, string schemaFilePath)
    {
        if (!File.Exists(schemaFilePath))
        {
            return true;
        }

        using var schema = JsonDocument.Parse(File.ReadAllText(schemaFilePath));
        if (!schema.RootElement.TryGetProperty("properties", out var props)
            || !props.TryGetProperty("schema_version", out var sv)
            || !sv.TryGetProperty("const", out var constant))
        {
            return true;
        }

        var expected = constant.GetString();
        return string.IsNullOrEmpty(expected) || string.Equals(expected, schemaVersion, StringComparison.Ordinal);
    }

    public static string SchemaPathForVersion(string schemaVersion, ContractsPaths paths) =>
        schemaVersion switch
        {
            "0.1" => paths.SolverInput01,
            "real_candidate_v1_1" => paths.SolverInputV11,
            _ => paths.SolverInput01,
        };
}
