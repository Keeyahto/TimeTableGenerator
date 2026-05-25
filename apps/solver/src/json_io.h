#pragma once

#include <nlohmann/json.hpp>
#include <string>

namespace schedule_solver {

bool read_text_file(const std::string& path, std::string& out, std::string& error);
bool write_text_file(const std::string& path, const std::string& content, std::string& error);
bool can_write_output_path(const std::string& path, std::string& error);

}  // namespace schedule_solver
