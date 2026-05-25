#include "json_io.h"
#include "solver_engine.h"

#include <iostream>
#include <memory>
#include <string>

namespace {

struct CliOptions {
  std::string input_path;
  std::string output_path;
  schedule_solver::SolverMode mode = schedule_solver::SolverMode::Diagnostic;
  bool valid = false;
  std::string error;
};

CliOptions parse_cli(int argc, char** argv) {
  CliOptions options;
  for (int i = 1; i < argc; ++i) {
    const std::string arg = argv[i];
    if (arg == "--input" && i + 1 < argc) {
      options.input_path = argv[++i];
    } else if (arg == "--output" && i + 1 < argc) {
      options.output_path = argv[++i];
    } else if (arg == "--mode" && i + 1 < argc) {
      const std::string mode = argv[++i];
      if (mode == "diagnostic") {
        options.mode = schedule_solver::SolverMode::Diagnostic;
      } else {
        options.error = "Unsupported mode: " + mode;
        return options;
      }
    } else if (arg == "--help" || arg == "-h") {
      options.error = "help";
      return options;
    } else {
      options.error = "Unknown argument: " + arg;
      return options;
    }
  }

  if (options.input_path.empty()) {
    options.error = "Missing required argument: --input";
    return options;
  }
  if (options.output_path.empty()) {
    options.error = "Missing required argument: --output";
    return options;
  }

  options.valid = true;
  return options;
}

void print_usage() {
  std::cerr
      << "Usage: schedule_solver --input <path> --output <path> [--mode diagnostic]\n";
}

}  // namespace

int main(int argc, char** argv) {
  const CliOptions cli = parse_cli(argc, argv);
  if (!cli.valid) {
    if (cli.error == "help") {
      print_usage();
      return 0;
    }
    print_usage();
    std::cerr << "Error: " << cli.error << "\n";
    return 1;
  }

  std::string io_error;
  if (!schedule_solver::can_write_output_path(cli.output_path, io_error)) {
    std::cerr << "Error: " << io_error << "\n";
    return 2;
  }

  schedule_solver::SolverRunRequest request;
  request.input_path = cli.input_path;
  request.output_path = cli.output_path;
  request.mode = cli.mode;

  if (!schedule_solver::read_text_file(cli.input_path, request.input_text, io_error)) {
    std::cerr << "Error: " << io_error << "\n";
    return 3;
  }

  // TODO: validate input against shared solver-input contract before running engine.
  try {
    nlohmann::json::parse(request.input_text);
  } catch (const std::exception& ex) {
    std::cerr << "Error: input is not valid JSON: " << ex.what() << "\n";
    return 4;
  }

  auto engine = schedule_solver::create_solver_engine();
  const SolverRunResult result = engine->run(request);
  if (!result.ok) {
    std::cerr << "Error: " << result.error_message << "\n";
    return 5;
  }

  const auto output_json = result.report.to_json().dump(2);
  if (!schedule_solver::write_text_file(cli.output_path, output_json, io_error)) {
    std::cerr << "Error: " << io_error << "\n";
    return 6;
  }

  std::cout << "Wrote diagnostic output to " << cli.output_path << "\n";
  return 0;
}
