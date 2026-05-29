# Benchmark model build / diagnostic memory (DevHost). Appends tmp/bench-history.jsonl
param(
    [string]$Sample = "stress-medium-v1_1",
    [ValidateSet("profile", "diagnostic")]
    [string]$Mode = "profile",
    [int]$TimeLimitSec = 30,
    [int]$MemLimitMb = 8192,
    [switch]$AllowLargeModel,
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..")
$sampleInput = Join-Path $repo "data\samples\$Sample\input.json"
if (-not (Test-Path $sampleInput)) {
    Write-Error "Sample not found: $sampleInput"
}

if ($Build) {
    dotnet build (Join-Path $repo "apps\solver\ScheduleSolver.slnx") -c Release --verbosity minimal | Out-Null
}

$logBefore = if (Test-Path (Join-Path $repo "tmp\solver-watchdog.log")) {
    (Get-Item (Join-Path $repo "tmp\solver-watchdog.log")).Length
} else { 0 }

$ts = Get-Date -Format "yyyyMMdd-HHmmss"
$out = Join-Path $repo "tmp\bench-$ts.json"
$runner = Join-Path $repo "scripts\run-solver.ps1"

$useLarge = $AllowLargeModel.IsPresent -or $Sample -eq "stress-medium-v1_1"
if ($useLarge) {
    & $runner -InputPath $sampleInput -OutputPath $out -Mode $Mode -TimeLimit $TimeLimitSec `
        -MemLimitMb $MemLimitMb -AllowLargeModel
} else {
    & $runner -InputPath $sampleInput -OutputPath $out -Mode $Mode -TimeLimit $TimeLimitSec -MemLimitMb $MemLimitMb
}
$exit = $LASTEXITCODE

$peakMb = $null
$logPath = Join-Path $repo "tmp\solver-watchdog.log"
if (Test-Path $logPath) {
    $newLines = Get-Content $logPath | Select-Object -Skip ([math]::Max(0, (Get-Content $logPath).Count - 5))
    $peakLine = $newLines | Where-Object { $_ -match 'peak tree (\d+)' } | Select-Object -Last 1
    if ($peakLine -match 'peak tree (\d+)') {
        $peakMb = [int]$Matches[1]
    }
}

$row = [ordered]@{
    timestamp = (Get-Date).ToUniversalTime().ToString("o")
    sample = $Sample
    mode = $Mode
    exit_code = $exit
    mem_limit_mb = $MemLimitMb
    peak_tree_mb = $peakMb
}
if (Test-Path $out) {
    $json = Get-Content $out -Raw | ConvertFrom-Json
    $row.status = $json.status
    $row.cp_sat_status = $json.cp_sat_status
    $row.wall_time_seconds = $json.wall_time_seconds
    $row.scheduled = @($json.schedule).Count
    $row.unscheduled = @($json.unscheduled_lessons).Count
    $ms = $json.rules_by_status.profile.model_stats
    if (-not $ms) { $ms = $json.rules_by_status.model_stats }
    if ($ms) {
        $row.violation_literals = $ms.violation_literal_count
        $row.model_stats = $ms.model_stats
        if ($ms.model_stats -match '#Variables:\s*(\d+)') {
            $row.variable_count = [int]$Matches[1]
        }
    }
}

$history = Join-Path $repo "tmp\bench-history.jsonl"
New-Item -ItemType Directory -Force -Path (Join-Path $repo "tmp") | Out-Null
($row | ConvertTo-Json -Compress) | Add-Content $history -Encoding UTF8

Write-Host "Wrote $out exit=$exit peak_mb=$peakMb"
$row | Format-List

if ($exit -ne 0 -and $exit -ne 137) { exit $exit }
