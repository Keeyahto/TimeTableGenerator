# ScheduleSolver (.NET 10)

Stateless JSON in/out solver for timetable generation.

## Projects

| Project | Role |
|---------|------|
| `src/ScheduleSolver.Core` | Loader, validation, rule registry, modes |
| `src/ScheduleSolver.Cli` | CLI entry |
| `tests/ScheduleSolver.Tests` | xUnit |
| `tools/ScheduleSolver.DevHost` | Dev-only memory watchdog (process tree + system RAM) |

## Memory safety (DevHost)

| Variable | Default | Meaning |
|----------|---------|---------|
| `SCHED_MEM_CHILD_MB` | min(8 GB, 70% RAM) | Kill solver tree when private/working set exceeds cap |
| `SCHED_MEM_LIMIT_PCT` | `85` | Kill when total Windows RAM usage exceeds % |
| `SCHED_MEM_CHILD_PCT` | `70` | Upper bound for child cap as % of physical RAM |
| `SCHED_MEM_POLL_MS` | `250` | Poll interval |
| `SCHED_ALLOW_LARGE_MODEL` | off | Required for handoff-scale CP-SAT (~350 demands) |
| `SCHED_CP_SAT_WORKERS` | `1` | CP-SAT `num_search_workers` (lower RAM at solve time) |
| `SCHED_MAX_DEMANDS` | `150` | Block solve/diagnostic above this without opt-in |

`compare-handoff-ab.ps1` always runs through DevHost. Log: `tmp/solver-watchdog.log` (exit `137` = killed).

## Build & test

```powershell
cd apps/solver
dotnet build ScheduleSolver.slnx -c Release
dotnet test ScheduleSolver.slnx -c Release

# Default tests skip handoff CP-SAT inside testhost (avoids 10+ GB testhost).
# Opt-in handoff diagnostic in-process (dangerous):
#   $env:SCHED_RUN_HANDOFF_DIAGNOSTIC=1
#   dotnet test --filter HandoffDiagnostic
# Safe handoff run (DevHost memory cap):
#   .\scripts\compare-handoff-ab.ps1 -Mode diagnostic -MemLimitMb 12288 -AllowLargeModel
# Kill stray testhost after interrupted test:
#   .\scripts\kill-orphan-solver.ps1
```

## Run (from repo root)

```powershell
.\scripts\run-solver.ps1 -Mode validate
.\scripts\run-solver.ps1 -Mode profile
.\scripts\run-solver.ps1 -UseRealHandoff -AllowLargeModel -Mode profile
.\scripts\compare-handoff-ab.ps1 -Mode profile
.\scripts\run-solver.ps1 -MemLimitMb 12288 -UseRealHandoff -AllowLargeModel -Mode diagnostic
.\scripts\snapshot-handoff-profile.ps1   # tmp/handoff-profile-baseline.json
.\scripts\bench-model-memory.ps1 -Sample stress-medium-v1_1 -Mode profile
.\scripts\build-stress-medium-sample.ps1 # regenerate stress-medium-v1_1
.\scripts\run-solver.ps1 -NoWatchdog -Mode validate      # skip DevHost (not for handoff)
```

Output default: `tmp/solver-output.json`

## Design

`docs/superpowers/specs/2026-05-26-csharp-solver-design.md`

## Phase status

- **Phase 0:** scaffold, validate/profile, DevHost
- **Phase 1:** CP-SAT optional intervals, R01–R09, `solve` / `diagnostic` on synthetic
- **Phase 2 wave 1:** R08–R10, R19 — `data/samples/synthetic-phase2/`
- **Phase 2 wave 2:** R11–R14, R22, SOFT R20–R21 — `data/samples/synthetic-wave2/`
- **Phase 2 complete:** R00–R32 enforced; memory waves M1/M3/M5 (phantom-start, R28 pairs, workers=1)
- **Bench:** `stress-medium-v1_1` + `scripts/bench-model-memory.ps1`
