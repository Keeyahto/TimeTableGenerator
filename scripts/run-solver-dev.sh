#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

BUILD_DIR="$REPO_ROOT/apps/solver/build"
INPUT_PATH="$REPO_ROOT/data/samples/synthetic-small/input.json"
OUTPUT_PATH="$REPO_ROOT/tmp/solver-output.json"

if [[ ! -d "$BUILD_DIR" ]]; then
  cmake -S apps/solver -B apps/solver/build
  cmake --build apps/solver/build
fi

SOLVER_BIN="$BUILD_DIR/schedule_solver"
if [[ ! -x "$SOLVER_BIN" ]]; then
  SOLVER_BIN="$BUILD_DIR/Release/schedule_solver"
fi

mkdir -p "$(dirname "$OUTPUT_PATH")"
"$SOLVER_BIN" --input "$INPUT_PATH" --output "$OUTPUT_PATH" --mode diagnostic
echo "Solver output: $OUTPUT_PATH"
