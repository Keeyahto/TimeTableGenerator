using System.Text.Json;
using System.Text.Json.Nodes;
using ScheduleSolver.Core;
using ScheduleSolver.Core.Input;
using ScheduleSolver.Core.Validation;

namespace ScheduleSolver.Tests;

public class ValidationEdgeCaseTests
{
    [Fact]
    public async Task Validate_unknown_group_reference_fails()
    {
        var (result, json) = await SolverTestHelper.RunSampleAsync("edge-unknown-group", SolverMode.Validate);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("ERROR", result.Status);
        var warnings = json["warnings"] as JsonArray;
        Assert.NotNull(warnings);
        Assert.Contains(warnings, w => w?["code"]?.GetValue<string>() == "R00_UNKNOWN_GROUP");
    }

    [Fact]
    public async Task Validate_unsupported_schema_version_fails()
    {
        var path = SolverTestPaths.TempOutput();
        var inputPath = Path.ChangeExtension(path, ".input.json");
        await File.WriteAllTextAsync(inputPath, """
            {
              "schema_version": "99.99",
              "calendar": { "weeks": 1, "slots": [] },
              "groups": [],
              "teachers": [],
              "rooms": [],
              "subjects": [],
              "lesson_demands": [],
              "constraints": { "hard": [], "soft": [] },
              "solver_config": { "mode": "validate" }
            }
            """);

        try
        {
            var (result, json) = await SolverTestHelper.RunAsync(new SolverRunOptions
            {
                InputPath = inputPath,
                OutputPath = path,
                Mode = SolverMode.Validate,
            });

            Assert.Equal(1, result.ExitCode);
            Assert.Contains(
                (json["warnings"] as JsonArray) ?? [],
                w => w?["code"]?.GetValue<string>() == "UNSUPPORTED_SCHEMA_VERSION");
        }
        finally
        {
            SolverTestHelper.Cleanup(path);
            SolverTestHelper.Cleanup(inputPath);
        }
    }

    [Fact]
    public void Structural_validator_requires_constraints_for_v01()
    {
        using var doc = JsonDocument.Parse("""
            {
              "schema_version": "0.1",
              "calendar": { "weeks": 1, "slots": [] },
              "groups": [],
              "teachers": [],
              "rooms": [],
              "subjects": [],
              "lesson_demands": [],
              "solver_config": {}
            }
            """);
        using var input = new ParsedInput(doc, "x.json");
        var issues = StructuralValidator.Validate(input);

        Assert.Contains(issues, i => i.Code == "MISSING_PROPERTY" && i.Path == "constraints");
    }

    [Fact]
    public void Structural_validator_requires_rules_for_real_candidate()
    {
        using var doc = JsonDocument.Parse("""
            {
              "schema_version": "real_candidate_v1_1",
              "calendar": { "weeks": 1, "slots": [] },
              "groups": [],
              "teachers": [],
              "rooms": [],
              "subjects": [],
              "lesson_demands": [],
              "solver_config": {}
            }
            """);
        using var input = new ParsedInput(doc, "x.json");
        var issues = StructuralValidator.Validate(input);

        Assert.Contains(issues, i => i.Code == "MISSING_PROPERTY" && i.Path == "rules");
    }
}
