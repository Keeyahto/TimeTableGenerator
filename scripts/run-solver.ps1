param(
    [string]$Mode = "validate",
    [string]$Input = "",
    [string]$Output = "",
    [int]$TimeLimit = 30,
    [ValidateSet("A", "B", "")]
    [string]$DatasetVariant = "",
    [switch]$UseRealHandoff,
    [ValidateSet("A", "B")]
    [string]$HandoffVariant = "A",
    [switch]$NoWatchdog
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $RepoRoot

if (-not $Input) {
    $Input = Join-Path $RepoRoot "data\samples\synthetic-small\input.json"
}
if (-not $Output) {
    $Output = Join-Path $RepoRoot "tmp\solver-output.json"
}

if ($UseRealHandoff) {
    $variantFile = if ($HandoffVariant -eq "B") {
        "variant_B_no_merge_bakirova_valieva.json"
    } else {
        "variant_A_no_merge_bakirova_valieva.json"
    }
    $Input = Join-Path $RepoRoot "data\solver_agent_full_handoff_v2\02_canonical_solver_input_v1_1\solver_input_real_v1\$variantFile"
    if (-not (Test-Path $Input)) {
        Write-Error "Handoff file not found (local PD path): $Input"
    }
    if (-not $DatasetVariant) { $DatasetVariant = $HandoffVariant }
}

$CliDll = Join-Path $RepoRoot "apps\solver\src\ScheduleSolver.Cli\bin\Release\net10.0\ScheduleSolver.Cli.dll"
$CliProj = Join-Path $RepoRoot "apps\solver\src\ScheduleSolver.Cli\ScheduleSolver.Cli.csproj"

if (-not (Test-Path $CliDll)) {
    Write-Host "Building ScheduleSolver.Cli..."
    dotnet build $CliProj -c Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
$DevHostProj = Join-Path $RepoRoot "apps\solver\tools\ScheduleSolver.DevHost\ScheduleSolver.DevHost.csproj"

$forward = @(
    "--input", $Input,
    "--output", $Output,
    "--mode", $Mode,
    "--time-limit", $TimeLimit
)
if ($DatasetVariant) {
    $forward += @("--dataset-variant", $DatasetVariant)
}

$env:SCHED_REPO_ROOT = $RepoRoot
New-Item -ItemType Directory -Force -Path (Split-Path $Output) | Out-Null

if ($NoWatchdog) {
    & dotnet exec $CliDll @forward
} else {
    dotnet build $DevHostProj -c Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet run --project $DevHostProj -c Release --no-build -- $CliDll -- @forward
}

exit $LASTEXITCODE
