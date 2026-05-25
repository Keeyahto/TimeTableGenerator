#pragma once

#include <nlohmann/json.hpp>
#include <string>
#include <vector>

namespace schedule_solver {

struct SolverWarning {
  std::string code;
  std::string message;
};

struct DiagnosticReport {
  std::string schema_version = "0.1";
  std::string status = "STUB";
  nlohmann::json feasible = nullptr;
  std::string solver_status = "NOT_RUN";
  nlohmann::json objective_value = nullptr;
  nlohmann::json best_objective_bound = nullptr;
  nlohmann::json gap = nullptr;
  std::vector<std::string> enabled_rules;
  std::vector<SolverWarning> warnings;
  nlohmann::json schedule = nullptr;
  nlohmann::json artifacts = nlohmann::json::array();

  nlohmann::json to_json() const;
};

DiagnosticReport make_stub_diagnostic_report();

}  // namespace schedule_solver
