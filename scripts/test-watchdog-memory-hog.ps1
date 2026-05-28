# Smoke test: DevHost must kill MemoryHog before system RAM is exhausted.
param(
    [int]$MemLimitMb = 512,
    [int]$ChunkMb = 64
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..")
$hogProj = Join-Path $repo "apps\solver\tools\MemoryHog\MemoryHog.csproj"
$hogDll = Join-Path $repo "apps\solver\tools\MemoryHog\bin\Release\net10.0\MemoryHog.dll"
$devHostProj = Join-Path $repo "apps\solver\tools\ScheduleSolver.DevHost\ScheduleSolver.DevHost.csproj"
$logPath = Join-Path $repo "tmp\solver-watchdog.log"

Write-Host "Building MemoryHog + DevHost..."
dotnet build $hogProj -c Release --verbosity quiet | Out-Null
dotnet build $devHostProj -c Release --verbosity quiet | Out-Null

$env:SCHED_REPO_ROOT = $repo
$env:SCHED_MEM_CHILD_MB = "$MemLimitMb"
$env:SCHED_MEM_LIMIT_PCT = "99"
$env:SCHED_MEM_POLL_MS = "200"

Write-Host "Launching DevHost -> MemoryHog (chunk ${ChunkMb} MB, cap ${MemLimitMb} MB)..."
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet run --project $devHostProj -c Release --no-build -- $hogDll -- $ChunkMb
$exit = $LASTEXITCODE
$sw.Stop()

Write-Host "Exit code: $exit  Elapsed: $([int]$sw.Elapsed.TotalSeconds)s"

if ($exit -ne 137) {
    Write-Error "Expected exit 137 (watchdog kill), got $exit"
}

if (-not (Test-Path $logPath)) {
    Write-Error "Missing log: $logPath"
}

$tail = @(Get-Content $logPath -Tail 5)
Write-Host "Last log lines:"
$tail | ForEach-Object { Write-Host "  $_" }

$killedLine = $tail | Where-Object { $_ -match "WATCHDOG.*killed PID" } | Select-Object -Last 1
if (-not $killedLine) {
    Write-Error "Log does not contain WATCHDOG kill entry"
}

Write-Host "OK — watchdog killed MemoryHog within cap."
exit 0
