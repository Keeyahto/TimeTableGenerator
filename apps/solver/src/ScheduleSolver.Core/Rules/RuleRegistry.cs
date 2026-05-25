using System.Text.Json;

namespace ScheduleSolver.Core.Rules;

public sealed class RuleRegistry
{
    private readonly Dictionary<string, RuleDefinition> _rules;

    public RuleRegistry(IEnumerable<RuleDefinition> rules)
    {
        _rules = rules.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static RuleRegistry CreateDefault(JsonDocument? input = null)
    {
        var merged = RuleCatalog.All.ToDictionary(r => r.Id, r => r, StringComparer.OrdinalIgnoreCase);

        if (input is not null
            && input.RootElement.TryGetProperty("rules", out var rulesElement)
            && rulesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in rulesElement.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || !merged.TryGetValue(id, out var existing))
                {
                    continue;
                }

                var penalty = item.TryGetProperty("penalty", out var penEl) && penEl.TryGetInt32(out var p)
                    ? p
                    : existing.DefaultPenalty;
                var status = existing.DefaultStatus;
                if (item.TryGetProperty("enforcement_status", out var stEl))
                {
                    status = ParseStatus(stEl.GetString()) ?? status;
                }

                merged[id] = existing with { DefaultPenalty = penalty, DefaultStatus = status };
            }
        }

        return new RuleRegistry(merged.Values);
    }

    public IReadOnlyCollection<RuleDefinition> All => _rules.Values;

    public RuleDefinition GetRequired(string id) => _rules[id];

    public bool TryGet(string id, out RuleDefinition? rule) =>
        _rules.TryGetValue(id, out rule);

    public Dictionary<string, List<string>> RulesByStatusBuckets()
    {
        var buckets = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["enforced"] = [],
            ["stub"] = [],
            ["disabled"] = [],
        };

        foreach (var rule in _rules.Values.OrderBy(r => r.Id, StringComparer.Ordinal))
        {
            var key = rule.DefaultStatus switch
            {
                EnforcementStatus.Enforced => "enforced",
                EnforcementStatus.Stub => "stub",
                EnforcementStatus.Disabled => "disabled",
                _ => "stub",
            };
            buckets[key].Add(rule.Id);
        }

        return buckets;
    }

    public IReadOnlyList<string> StubRuleWarnings()
    {
        return _rules.Values
            .Where(r => r.DefaultStatus == EnforcementStatus.Stub)
            .Select(r => r.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    private static EnforcementStatus? ParseStatus(string? raw) =>
        raw?.ToLowerInvariant() switch
        {
            "enforced" => EnforcementStatus.Enforced,
            "stub" => EnforcementStatus.Stub,
            "disabled" => EnforcementStatus.Disabled,
            _ => null,
        };
}
