# Web Cockpit Design Spec

**Date:** 2026-05-29  
**Status:** Draft (brainstorming complete, pending user review)  
**Depends on:** `apps/solver` (CP-SAT solve + validate), `packages/shared-contracts` (SolverInput v1.1, SolverOutput v0.2)  
**Extends:** `docs/data-boundary.md`, `docs/architecture.md`  
**Next step:** `writing-plans` → implementation plan for web + solver progress streaming

## Summary

Build **`apps/web`** as a **product-shaped dev cockpit**: project workspace, data load, validation, preview, solver launch with **live schedule grid** (best-so-far), violations panel, results and export. Solver stays a **separate process**; live updates use **throttled progress files** on disk (`progress/latest.json`) consumed by Next.js **SSE**.

This is intentionally **between MVP and full product**: no multi-workbook import, no drag-and-drop editing, no PDF, no approval workflow — but navigation and components are designed to grow into the full feature list.

## Locked product decisions (brainstorming 2026-05-29)

| Topic | Choice |
|-------|--------|
| Primary goal | End-to-end UI useful **during solver development** and as future product shell |
| Live schedule | **Yes** — grid shows best feasible solution so far (OR-Tools `CpSolverSolutionCallback`) |
| Update throttle | **1–2 s** (implement **1.5 s** in solver `ProgressWriter`) |
| Transport | **Approach 1:** atomic `progress/latest.json` + Next.js SSE (no separate HTTP solver service in v1) |
| Multi-table / multi-sheet Excel | **Out of scope v1**; multiple files OK **one entity type at a time** |
| Import v1 | Direct **SolverInput JSON** (primary dev path), **CSV/Excel template** per entity collection |
| Versions | Auto-keep last **N=10** run snapshots; no draft/approved status workflow |
| Concurrent runs | **One active run per project** (v1) |
| Auth | None (local dev) |

## Out of scope v1 (explicit backlog)

- Multiple workbooks / complex sheet mapping pipeline (`data/phase 1` full parity)
- Drag-and-drop grid edit, cell lock, undo stack
- PDF and print layouts
- Duplicate merge wizard (beyond simple duplicate list)
- Status workflow: черновик → проверено → утверждено
- Dedicated Solver Host (HTTP/WebSocket) — deferred; file progress contract is forward-compatible

## Architecture

### Layers

```
[ Browser: Ant Design UI ]
        ↓ REST / SSE
[ Next.js Route Handlers + Prisma ]
        ↓ spawn
[ ScheduleSolver.Cli ] --writes--> storage/projects/<projectId>/runs/<runId>/
        ↑ reads progress/latest.json (SSE watches mtime)
```

Boundary unchanged: solver has no DB; web does not embed CP-SAT.

### Prisma models (additions / changes)

| Model | Purpose |
|-------|---------|
| `Project` | Root: `name`, `schemaVersion`, `status`, `currentInputPath`, `validationReportJson` |
| `DataSourceFile` | Add `projectId` |
| `ManualDecision` | Add `projectId` |
| `SolverRun` | Add `projectId`, `progressDir`, `pid`, `timeLimitSec`, `mode`, `phase`, `lastProgressAt` |
| `SolverArtifact` | Types: `input`, `output`, `progress_latest`, `progress_snapshot`, `validate_report`, `log` |

`SolverRun.status`: `queued` → `running` → `completed` | `failed` | `cancelled` | `stopped`.

On-disk layout per run:

```
storage/projects/<projectId>/runs/<runId>/
  input.json
  output.json
  progress/latest.json
  progress/events.jsonl   # optional v1
  solver.log
```

### Progress contract (`packages/shared-contracts`)

New schema: `solver-progress.schema.json`, `progress_schema_version: "1.0"`.

Written by solver (throttled), read by UI:

| Field | Purpose |
|-------|---------|
| `updated_at`, `phase` | `building_model` \| `solving` \| `finalizing` |
| `elapsed_sec`, `solution_index` | Progress metrics |
| `cp_sat_status`, `objective_value`, `best_objective_bound`, `gap` | Optimization state |
| `conflict_counts` | Aggregates: `relaxed_hard`, `soft` |
| `schedule.assignments` | Best-so-far placement for grid |
| `violations_preview` | Top N violations for side panel |
| `message` | Human-readable status line |

Write pattern: write temp file → atomic rename to `progress/latest.json`.

### Solver changes (.NET)

1. `CpSatSolveService.Solve(..., IProgressSink? sink)` — register `CpSolverSolutionCallback`.
2. `ProgressWriter` — throttle 1.5 s, build lightweight snapshot, honor cancel flag.
3. CLI: `--progress-dir <path>` required when web launches `solve`.
4. `CpSolver.StopSearch()` on cancel; web v1 may also terminate process tree on Windows.
5. Final `output.json` remains SolverOutput v0.2; on completion, progress file reflects final state or UI loads `output.json`.

### Web API (Route Handlers)

| Method | Path | Action |
|--------|------|--------|
| POST | `/api/projects` | Create project |
| GET/PATCH | `/api/projects/[id]` | Metadata, validation report |
| POST | `/api/projects/[id]/input` | Upload JSON or CSV→merge |
| POST | `/api/projects/[id]/validate` | Web precheck + CLI `validate` |
| POST | `/api/projects/[id]/runs` | Spawn solver, return `runId` |
| GET | `/api/runs/[id]` | Status, paths |
| GET | `/api/runs/[id]/stream` | SSE on `progress/latest.json` |
| POST | `/api/runs/[id]/stop` | Cancel run |
| GET | `/api/runs/[id]/output` | Final SolverOutput |

Process spawn: `dotnet ScheduleSolver.Cli.dll` with paths matching `scripts/run-solver.ps1` conventions.

### UI routes (project-scoped)

| Route | Screen |
|-------|--------|
| `/` | Project list |
| `/p/[id]` | Project dashboard |
| `/p/[id]/data` | Import + entity tables + manual CRUD |
| `/p/[id]/validate` | Errors / warnings, blocking list |
| `/p/[id]/preview` | Counts, included/excluded demands |
| `/p/[id]/generate` | Run settings + **live workspace** (grid, status, log, violations) |
| `/p/[id]/results/[runId]` | Final view: slices, filters, export |
| `/p/[id]/runs` | Run history |

### Shared UI components

| Component | Role |
|-----------|------|
| `ScheduleGrid` | Slot grid; slice by group / teacher / room / day |
| `ViolationPanel` | From progress preview or full output |
| `RunMonitor` | SSE hook, phase, stop, metrics |
| `EntityTables` | CRUD for reference entities |
| `ValidationReport` | Unified issues list (`code`, `severity`, `path`) |

## Data flow

### Happy path

1. Create project → upload `input.json` (or CSV template merge) → `projects/<id>/current/input.json`.
2. Web precheck → `validationReportJson` (blocking errors prevent run).
3. Optional CLI `validate` → artifact + merge issues into report.
4. Preview screen: entity counts, demands excluded with reason.
5. Generate: copy input to `runs/<runId>/`, spawn `solve` with `--progress-dir`.
6. UI subscribes SSE; until first feasible snapshot: empty grid + “searching” + bound metrics if available.
7. On each throttled progress file: update grid + violation panel.
8. On complete: load full `output.json`, persist `SolverRun.summaryJson`, enable export.
9. Keep last 10 run artifacts per project.

### Issue format (web + solver alignment where possible)

```json
{
  "code": "R00_UNKNOWN_GROUP",
  "severity": "error",
  "message": "...",
  "path": "lesson_demands[3]",
  "entityType": "lesson_demand",
  "entityId": "ld-42"
}
```

### Error handling

| Situation | UI | Persistence |
|-----------|-----|-------------|
| Invalid upload JSON | Toast + ValidationReport | No input write |
| Web precheck errors | Block run | `validationReportJson` |
| Validate CLI failure | Show issues + exit code | `validate_report` artifact |
| Solver crash | `failed` + log excerpt | `solver.log` |
| INFEASIBLE / timeout | `completed` with status; show last progress / partial | `output.json` |
| SSE disconnect while running | Poll `GET /runs/[id]` + re-subscribe | — |
| Second run while one active | 409 or disabled button | — |

### Manual decisions

`ManualDecision` applied during merge/export pipeline before validate/solve (canonical replaces source).

## Import v1

1. **SolverInput JSON** — primary path; load from `data/samples/*` or handoff files.
2. **CSV template** — one file → one array (`teachers.csv` → `teachers[]`); wizard: entity type → column mapping → 10-row preview → merge.
3. **Excel** — same wizard, single sheet via SheetJS; no multi-sheet workbook orchestration.

## Export v1

- Result schedule: **Excel + CSV** (selected slice/filter).
- Normalized input export: JSON download.

## Testing

### Solver

- Unit: `ProgressWriter` throttle (injectable clock).
- Integration: `synthetic-small` with `--progress-dir` → multiple progress updates + valid final output schema.
- Callback: `solution_index` increments on small model.

### Web

- API integration: project → upload sample → validate → stub/spawn short solve → SSE receives ≥2 events.
- Component: `ScheduleGrid` fixture render.
- Optional Playwright: upload sample → run 5s limit → `completed`.

### Contracts

- `npm run validate:contracts` includes new `solver-progress.schema.json`.

## Non-functional (v1)

- SQLite + local `storage/` sufficient for dev.
- **Allow large model** toggle mirrors CLI `--allow-large-model` with UI warning.
- Windows-first process management (align with existing DevHost patterns where useful).

## Implementation phasing (suggested for writing-plans)

| Phase | Deliverable |
|-------|-------------|
| **P0** | `Project` + storage paths + JSON upload + web precheck + validate CLI hook |
| **P1** | Progress contract + solver callback + SSE + Generate page with live grid |
| **P2** | Preview + Results slices/filters + export CSV/Excel |
| **P3** | CSV template import + entity CRUD tables + run history |
| **P4** | Light duplicate list, manual decisions UI, polish |

## References

- `docs/data-boundary.md`
- `docs/architecture.md`
- `apps/solver/src/ScheduleSolver.Core/Solver/CpSatSolveService.cs`
- `scripts/run-solver.ps1`
- OR-Tools: `CpSolverSolutionCallback`, `CpSolver.StopSearch()`
