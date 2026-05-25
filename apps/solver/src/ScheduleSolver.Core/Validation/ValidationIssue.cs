namespace ScheduleSolver.Core.Validation;

public sealed record ValidationIssue(string Code, string Message, string? Path = null);
