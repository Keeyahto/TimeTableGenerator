# Compare handoff variants A vs B (local PD only — not CI). Uses DevHost memory watchdog.
param(
    [ValidateSet("profile", "diagnostic", "solve")]
    [string]$Mode = "profile",
    [int]$TimeLimitSec = 30,
    [int]$MemLimitMb = 10240,
    [switch]$AllowLargeModel,
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..")
$runner = Join-Path $repo "scripts\run-solver.ps1"

if ($Mode -ne "profile" -and -not $AllowLargeModel) {
    $AllowLargeModel = $true
    Write-Warning "CP-SAT modes require -AllowLargeModel for full handoff (353 demands)."
}

if ($Build) {
    dotnet build (Join-Path $repo "apps\solver\ScheduleSolver.slnx") -c Release --verbosity minimal | Out-Null
}

$variants = @("A", "B")
$rows = @()

foreach ($name in $variants) {
    $out = Join-Path $env:TEMP "handoff-run-$name-$Mode.json"
    $runnerArgs = @{
        UseRealHandoff = $true
        HandoffVariant = $name
        Mode = $Mode
        OutputPath = $out
        TimeLimit = $TimeLimitSec
        MemLimitMb = $MemLimitMb
        DatasetVariant = $name
    }
    if ($AllowLargeModel) { $runnerArgs.AllowLargeModel = $true }

    & $runner @runnerArgs
    if ($LASTEXITCODE -eq 137) {
        Write-Warning "$name : killed by memory watchdog (exit 137). Lower load or raise -MemLimitMb."
        continue
    }
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $out)) {
        Write-Warning "$name : solver exit $LASTEXITCODE"
        continue
    }

    if ($Mode -eq "profile") {
        $baseline = Join-Path $env:TEMP "handoff-profile-$name.json"
        if ($out -ne $baseline) {
            Copy-Item -Force $out $baseline
        }
    }

    $json = Get-Content $out -Raw | ConvertFrom-Json
    $row = [ordered]@{
        Variant = $name
        Status = $json.status
        CpSat = $json.cp_sat_status
    }

    if ($Mode -eq "profile") {
        $profile = $json.rules_by_status.profile
        $row.Demands = $profile.lesson_demands
        $row.Slots = $profile.calendar_slots
        $row.EstVars = $profile.estimated_primary_variables
    }
    else {
        $row.Objective = $json.objective_value
        $row.Scheduled = @($json.schedule).Count
        $row.Unscheduled = @($json.unscheduled_lessons).Count
        $row.Relaxed = @($json.relaxed_hard_violations).Count
        $row.Soft = @($json.soft_violations).Count
        $row.WallSec = [math]::Round($json.wall_time_seconds, 2)
    }

    $rows += [pscustomobject]$row
}

if ($rows.Count -eq 0) {
    Write-Host "No results. Check handoff path and tmp/solver-watchdog.log"
    exit 1
}

Write-Host "Mode: $Mode  TimeLimit: ${TimeLimitSec}s  MemLimitMb: $MemLimitMb  Watchdog: on"
$rows | Format-Table -AutoSize

if ($rows.Count -eq 2 -and $Mode -ne "profile") {
    $a = $rows[0]
    $b = $rows[1]
    Write-Host ""
    Write-Host "Delta (B - A): objective=$([math]::Round(($b.Objective - $a.Objective), 2)) scheduled=$($b.Scheduled - $a.Scheduled) unscheduled=$($b.Unscheduled - $a.Unscheduled) relaxed=$($b.Relaxed - $a.Relaxed) soft=$($b.Soft - $a.Soft)"
}
