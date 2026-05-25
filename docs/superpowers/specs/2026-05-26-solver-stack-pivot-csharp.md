# Pivot: solver stack → C# (.NET)

**Date:** 2026-05-26  
**Status:** Approved  
**Supersedes:** C++ / CMake parts of `2026-05-25-solver-development-strategy-design.md` and `2026-05-25-solver-implementation-plan.md`

## Decision

Отказываемся от **C++20 + CMake + FetchContent OR-Tools** для `apps/solver`.

Причина: первая конфигурация/сборка на Windows тянет десятки зависимостей (absl, protobuf, SCIP, …) и не даёт быстрого прогресса по плану.

## New direction

| Было | Стало |
|------|--------|
| `schedule_solver.exe` (C++) | .NET console (имя TBD) |
| vcpkg / FetchContent | **NuGet** `Google.OrTools` |
| `SCHED_ENABLE_ORTOOLS` | обычный .NET project reference |

## Unchanged

- Monorepo, `apps/web`, `packages/shared-contracts`
- JSON in / JSON out, diagnostic-first
- Handoff rules R00–R40, variant A/B
- Web не трогает XLSX в solver

## Repo cleanup

- Удалены исходники C++ solver и скрипты `run-solver-dev`, `setup-ortools-build`
- `apps/solver/README.md` — placeholder под .NET
- Документация без упоминаний CMake/C++ как текущего пути

## Next step (when ready)

Новый план: scaffold `dotnet console`, OR-Tools NuGet, validate/profile modes, затем CP-SAT по registry-first подходу из handoff.
