using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public interface IRuleEnforcer
{
    string RuleId { get; }
    void Apply(SchedulingBuildContext ctx);
}
