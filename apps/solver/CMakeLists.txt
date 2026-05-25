cmake_minimum_required(VERSION 3.20)

project(schedule_solver LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(SCHED_ENABLE_ORTOOLS "Enable OR-Tools CP-SAT integration" OFF)

include(FetchContent)
FetchContent_Declare(
  nlohmann_json
  GIT_REPOSITORY https://github.com/nlohmann/json.git
  GIT_TAG v3.11.3
)
FetchContent_MakeAvailable(nlohmann_json)

add_executable(schedule_solver
  src/main.cpp
  src/json_io.cpp
  src/solver_engine_stub.cpp
)

target_include_directories(schedule_solver PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/src)

target_link_libraries(schedule_solver PRIVATE nlohmann_json::nlohmann_json)

if(SCHED_ENABLE_ORTOOLS)
  message(STATUS "OR-Tools integration placeholder: not wired yet (SCHED_ENABLE_ORTOOLS=ON)")
  # TODO: find_package or FetchContent for OR-Tools; link ortools::ortools
endif()

install(TARGETS schedule_solver RUNTIME DESTINATION bin)
