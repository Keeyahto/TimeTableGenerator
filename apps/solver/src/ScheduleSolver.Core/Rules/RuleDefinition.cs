namespace ScheduleSolver.Core.Rules;

public sealed record RuleDefinition(
    string Id,
    string Code,
    RuleClass Class,
    int DefaultPenalty,
    EnforcementStatus DefaultStatus);
