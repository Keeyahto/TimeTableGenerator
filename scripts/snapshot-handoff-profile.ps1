# Writes handoff A/B profile metrics to tmp/handoff-profile-baseline.json (local PD, not CI).
param(
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..")
$outPath = Join-Path $repo "tmp\handoff-profile-baseline.json"
$compare = Join-Path $repo "scripts\compare-handoff-ab.ps1"

& $compare -Mode profile -Build:$Build
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$rows = @()
foreach ($name in @("A", "B")) {
    $jsonPath = Join-Path $env:TEMP "handoff-profile-$name.json"
    if (-not (Test-Path $jsonPath)) {
        Write-Warning "Missing $jsonPath"
        continue
    }
    $json = Get-Content $jsonPath -Raw | ConvertFrom-Json
    $profile = $json.rules_by_status.profile
    $rows += [ordered]@{
        variant = $name
        captured_at = (Get-Date).ToUniversalTime().ToString("o")
        status = $json.status
        lesson_demands = $profile.lesson_demands
        calendar_slots = $profile.calendar_slots
        estimated_primary_variables = $profile.estimated_primary_variables
        enforced_count = @($json.rules_by_status.enforced).Count
        stub_count = @($json.rules_by_status.stub).Count
    }
}

if ($rows.Count -eq 0) {
    Write-Error "No profile JSON — check handoff path under data/solver_agent_full_handoff_v2/"
}

New-Item -ItemType Directory -Force -Path (Split-Path $outPath) | Out-Null
@{ handoff_profile = $rows } | ConvertTo-Json -Depth 6 | Set-Content $outPath -Encoding UTF8
Write-Host "Wrote $outPath"
