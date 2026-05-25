# ScheduleSolver (.NET 10)

Stateless JSON in/out solver for timetable generation.

## Projects

| Project | Role |
|---------|------|
| `src/ScheduleSolver.Core` | Loader, validation, rule registry, modes |
| `src/ScheduleSolver.Cli` | CLI entry |
| `tests/ScheduleSolver.Tests` | xUnit |
| `tools/ScheduleSolver.DevHost` | Dev-only memory watchdog (95% system RAM) |

## Build & test

```powershell
cd apps/solver
dotnet build ScheduleSolver.slnx -c Release
dotnet test ScheduleSolver.slnx -c Release
```

## Run (from repo root)

```powershell
.\scripts\run-solver.ps1 -Mode validate
.\scripts\run-solver.ps1 -Mode profile
.\scripts\run-solver.ps1 -UseRealHandoff -Mode profile   # local handoff only
.\scripts\run-solver.ps1 -NoWatchdog -Mode validate      # skip DevHost
```

Output default: `tmp/solver-output.json`

## Design

`docs/superpowers/specs/2026-05-26-csharp-solver-design.md`

## Phase status

- **Phase 0 (done):** scaffold, OR-Tools smoke, R00–R40 registry, validate/profile, DevHost + script
- **Phase 1 (next):** CP-SAT model, R01–R09, `solve` on synthetic
