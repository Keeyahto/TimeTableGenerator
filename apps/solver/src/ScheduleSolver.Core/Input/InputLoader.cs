using System.Text.Json;

namespace ScheduleSolver.Core.Input;

public static class InputLoader
{
    private static readonly JsonDocumentOptions Options = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    public static ParsedInput Load(string inputPath)
    {
        var fullPath = Path.GetFullPath(inputPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Input file not found: {fullPath}");
        }

        var json = File.ReadAllText(fullPath);
        var doc = JsonDocument.Parse(json, Options);
        return new ParsedInput(doc, fullPath);
    }
}
