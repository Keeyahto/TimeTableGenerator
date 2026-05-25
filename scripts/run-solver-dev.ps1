$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

$BuildDir = Join-Path $RepoRoot "apps\solver\build"
$InputPath = Join-Path $RepoRoot "data\samples\synthetic-small\input.json"
$OutputPath = Join-Path $RepoRoot "tmp\solver-output.json"

if (-not (Test-Path $BuildDir)) {
  cmake -S apps/solver -B apps/solver/build
  cmake --build apps/solver/build --config Release
}

$SolverExe = Join-Path $BuildDir "Release\schedule_solver.exe"
if (-not (Test-Path $SolverExe)) {
  $SolverExe = Join-Path $BuildDir "schedule_solver.exe"
}
if (-not (Test-Path $SolverExe)) {
  $SolverExe = Join-Path $BuildDir "schedule_solver"
}

New-Item -ItemType Directory -Force -Path (Split-Path $OutputPath) | Out-Null

& $SolverExe --input $InputPath --output $OutputPath --mode diagnostic
Write-Host "Solver output: $OutputPath"
