#pragma once

#include "diagnostic_report.h"

#include <memory>
#include <string>

namespace schedule_solver {

enum class SolverMode {
  Diagnostic,
};

struct SolverRunRequest {
  std::string input_path;
  std::string output_path;
  SolverMode mode = SolverMode::Diagnostic;
  std::string input_text;
};

struct SolverRunResult {
  bool ok = false;
  DiagnosticReport report;
  std::string error_message;
};

class SolverEngine {
 public:
  virtual ~SolverEngine() = default;
  virtual SolverRunResult run(const SolverRunRequest& request) = 0;
};

// Factory for current build (stub). TODO: OR-Tools engine when SCHED_ENABLE_ORTOOLS=ON.
std::unique_ptr<SolverEngine> create_solver_engine();

}  // namespace schedule_solver
