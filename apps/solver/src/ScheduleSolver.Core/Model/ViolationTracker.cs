using Google.OrTools.Sat;
using ScheduleSolver.Core.Rules;

namespace ScheduleSolver.Core.Model;

public sealed record TrackedViolation(
    string RuleId,
    BoolVar Variable,
    int Penalty,
    string Label,
    RuleClass Class);

public sealed class ViolationTracker
{
    private readonly List<TrackedViolation> _items = [];

    public IReadOnlyList<TrackedViolation> Items => _items;

    public BoolVar AddViolation(
        CpModel model,
        string ruleId,
        int penalty,
        string label,
        RuleClass ruleClass)
    {
        var v = model.NewBoolVar($"viol_{ruleId}_{label}");
        _items.Add(new TrackedViolation(ruleId, v, penalty, label, ruleClass));
        return v;
    }

    public LinearExpr BuildPenaltyExpr()
    {
        if (_items.Count == 0)
        {
            return LinearExpr.Constant(0);
        }

        return LinearExpr.Sum(_items.Select(i => LinearExpr.Term(i.Variable, i.Penalty)));
    }
}
