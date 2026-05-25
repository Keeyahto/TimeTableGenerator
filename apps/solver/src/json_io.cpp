#include "json_io.h"

#include <fstream>
#include <filesystem>

namespace fs = std::filesystem;

namespace schedule_solver {

bool read_text_file(const std::string& path, std::string& out, std::string& error) {
  std::ifstream input(path, std::ios::binary);
  if (!input) {
    error = "Cannot read input file: " + path;
    return false;
  }
  out.assign((std::istreambuf_iterator<char>(input)), std::istreambuf_iterator<char>());
  return true;
}

bool write_text_file(const std::string& path, const std::string& content, std::string& error) {
  std::error_code ec;
  const fs::path output_path(path);
  if (output_path.has_parent_path()) {
    fs::create_directories(output_path.parent_path(), ec);
    if (ec) {
      error = "Cannot create output directory: " + ec.message();
      return false;
    }
  }

  std::ofstream output(path, std::ios::binary | std::ios::trunc);
  if (!output) {
    error = "Cannot write output file: " + path;
    return false;
  }
  output << content;
  if (!output.good()) {
    error = "Failed while writing output file: " + path;
    return false;
  }
  return true;
}

bool can_write_output_path(const std::string& path, std::string& error) {
  std::error_code ec;
  const fs::path output_path(path);
  if (output_path.has_parent_path()) {
    fs::create_directories(output_path.parent_path(), ec);
    if (ec) {
      error = "Cannot create output directory: " + ec.message();
      return false;
    }
  }

  std::ofstream probe(path, std::ios::binary | std::ios::app);
  if (!probe) {
    error = "Cannot open output path for writing: " + path;
    return false;
  }
  return true;
}

}  // namespace schedule_solver
