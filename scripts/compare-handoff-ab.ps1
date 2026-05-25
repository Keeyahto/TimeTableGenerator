# Compare profile metrics for handoff variants A vs B (local only, not CI).
param(
    [string]$HandoffRoot = (Join-Path $PSScriptRoot "..\data\solver_agent_full_handoff_v2\02_canonical_solver_input_v1_1\solver_input_real_v1"),
    [int]$TimeLimitSec = 5
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..")
$cli = Join-Path $repo "apps\solver\src\ScheduleSolver.Cli\bin\Release\net10.0\ScheduleSolver.Cli.exe"

if (-not (Test-Path $cli)) {
    dotnet build (Join-Path $repo "apps\solver\src\ScheduleSolver.Cli\ScheduleSolver.Cli.csproj") -c Release | Out-Null
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

    $out = Join-Path $env:TEMP "handoff-profile-$($v.Name).json"
    & $cli -i $input -o $out -m profile --time-limit $TimeLimitSec | Out-Null
    $json = Get-Content $out -Raw | ConvertFrom-Json
    $profile = $json.rules_by_status.profile
    $rows += [pscustomobject]@{
        Variant = $v.Name
        Status = $json.status
        Demands = $profile.lesson_demands
        Slots = $profile.calendar_slots
        Teachers = $profile.teachers
    }
}

if ($rows.Count -eq 0) {
    Write-Host "No handoff files found under $HandoffRoot"
    exit 1
}

$rows | Format-Table -AutoSize
