#include "solver_engine.h"

namespace schedule_solver {

DiagnosticReport make_stub_diagnostic_report() {
  DiagnosticReport report;
  report.warnings.push_back(
      SolverWarning{"SOLVER_NOT_IMPLEMENTED",
                    "C++ CP-SAT solver is not implemented yet"});
  return report;
}

nlohmann::json DiagnosticReport::to_json() const {
  nlohmann::json warnings_json = nlohmann::json::array();
  for (const auto& warning : warnings) {
    warnings_json.push_back({{"code", warning.code}, {"message", warning.message}});
  }

  return nlohmann::json{
      {"schema_version", schema_version},
      {"status", status},
      {"feasible", feasible},
      {"solver_status", solver_status},
      {"objective_value", objective_value},
      {"best_objective_bound", best_objective_bound},
      {"gap", gap},
      {"enabled_rules", enabled_rules},
      {"warnings", warnings_json},
      {"schedule", schedule},
      {"artifacts", artifacts},
  };
}

class StubSolverEngine : public SolverEngine {
 public:
  SolverRunResult run(const SolverRunRequest& request) override {
    (void)request;
    SolverRunResult result;
    result.ok = true;
    result.report = make_stub_diagnostic_report();
    return result;
  }
};

std::unique_ptr<SolverEngine> create_solver_engine() {
  return std::make_unique<StubSolverEngine>();
}

}  // namespace schedule_solver
