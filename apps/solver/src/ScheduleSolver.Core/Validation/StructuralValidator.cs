using System.Text.Json;
using ScheduleSolver.Core.Input;

namespace ScheduleSolver.Core.Validation;

public static class StructuralValidator
{
    private static readonly HashSet<string> SupportedVersions =
    [
        "0.1",
        "real_candidate_v1_1",
    ];

    public static IReadOnlyList<ValidationIssue> Validate(ParsedInput input)
    {
        var issues = new List<ValidationIssue>();
        var root = input.Root;

        if (!SupportedVersions.Contains(input.SchemaVersion))
        {
            issues.Add(new ValidationIssue(
                "UNSUPPORTED_SCHEMA_VERSION",
                $"schema_version '{input.SchemaVersion}' is not supported yet."));
        }

        RequireProperty(root, "calendar", issues);
        RequireArray(root, "groups", issues);
        RequireArray(root, "teachers", issues);
        RequireArray(root, "rooms", issues);
        RequireArray(root, "subjects", issues);
        RequireArray(root, "lesson_demands", issues);
        RequireProperty(root, "solver_config", issues);

        if (input.SchemaVersion == "0.1")
        {
            RequireProperty(root, "constraints", issues);
        }
        else if (input.SchemaVersion == "real_candidate_v1_1")
        {
            RequireArray(root, "rules", issues);
        }

        return issues;
    }

    private static void RequireProperty(JsonElement root, string name, List<ValidationIssue> issues)
    {
        if (!root.TryGetProperty(name, out _))
        {
            issues.Add(new ValidationIssue("MISSING_PROPERTY", $"Required property '{name}' is missing.", name));
        }
    }

    private static void RequireArray(JsonElement root, string name, List<ValidationIssue> issues)
    {
        if (!root.TryGetProperty(name, out var el))
        {
            issues.Add(new ValidationIssue("MISSING_PROPERTY", $"Required array '{name}' is missing.", name));
            return;
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            issues.Add(new ValidationIssue("INVALID_TYPE", $"Property '{name}' must be an array.", name));
        }
    }
}
