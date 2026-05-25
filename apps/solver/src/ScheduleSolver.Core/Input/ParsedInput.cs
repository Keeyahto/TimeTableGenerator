using System.Text.Json;

namespace ScheduleSolver.Core.Input;

public sealed class ParsedInput : IDisposable
{
    public ParsedInput(JsonDocument document, string sourcePath)
    {
        Document = document;
        SourcePath = sourcePath;
        Root = document.RootElement;
        SchemaVersion = Root.TryGetProperty("schema_version", out var v)
            ? v.GetString() ?? "unknown"
            : "unknown";
    }

    public JsonDocument Document { get; }
    public JsonElement Root { get; }
    public string SourcePath { get; }
    public string SchemaVersion { get; }

    public void Dispose() => Document.Dispose();
}
