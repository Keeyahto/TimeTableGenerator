# Compare handoff variants A vs B (local PD only — not CI).
param(
    [ValidateSet("profile", "diagnostic", "solve")]
    [string]$Mode = "profile",
    [string]$HandoffRoot = (Join-Path $PSScriptRoot "..\data\solver_agent_full_handoff_v2\02_canonical_solver_input_v1_1\solver_input_real_v1"),
    [int]$TimeLimitSec = 30,
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..")
$cliProj = Join-Path $repo "apps\solver\src\ScheduleSolver.Cli\ScheduleSolver.Cli.csproj"
$cliDll = Join-Path $repo "apps\solver\src\ScheduleSolver.Cli\bin\Release\net10.0\ScheduleSolver.Cli.dll"

if ($Build -or -not (Test-Path $cliDll)) {
    dotnet build $cliProj -c Release --verbosity minimal | Out-Null
}

$variants = @(
    @{ Name = "A"; File = "variant_A_no_merge_bakirova_valieva.json" },
    @{ Name = "B"; File = "variant_B_merge_bakirova_valieva.json" }
)

$rows = @()
foreach ($v in $variants) {
    $input = Join-Path $HandoffRoot $v.File
    if (-not (Test-Path $input)) {
        Write-Warning "Skip $($v.Name): not found $input"
        continue
    }

    $out = Join-Path $env:TEMP "handoff-$Mode-$($v.Name).json"
    & dotnet exec $cliDll -i $input -o $out -m $Mode --time-limit $TimeLimitSec --dataset-variant $v.Name | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "$($v.Name): solver exit $LASTEXITCODE"
        continue
    }

    $json = Get-Content $out -Raw | ConvertFrom-Json
  $row = [ordered]@{
        Variant = $v.Name
        Status = $json.status
        CpSat = $json.cp_sat_status
    }

    if ($Mode -eq "profile") {
        $profile = $json.rules_by_status.profile
        $row.Demands = $profile.lesson_demands
        $row.Slots = $profile.calendar_slots
        $row.Teachers = $profile.teachers
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
    Write-Host "No handoff files found under $HandoffRoot"
    exit 1
}

Write-Host "Mode: $Mode  TimeLimit: ${TimeLimitSec}s"
$rows | Format-Table -AutoSize

if ($rows.Count -eq 2 -and $Mode -ne "profile") {
    $a = $rows[0]
    $b = $rows[1]
    Write-Host ""
    Write-Host "Delta (B - A): objective=$([math]::Round(($b.Objective - $a.Objective), 2)) scheduled=$($b.Scheduled - $a.Scheduled) unscheduled=$($b.Unscheduled - $a.Unscheduled) relaxed=$($b.Relaxed - $a.Relaxed) soft=$($b.Soft - $a.Soft)"
}
