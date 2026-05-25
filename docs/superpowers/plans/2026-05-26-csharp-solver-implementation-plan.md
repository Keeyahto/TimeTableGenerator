# C# Solver Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development per task. Spec: `docs/superpowers/specs/2026-05-26-csharp-solver-design.md`.

**Goal:** Diagnostic-first CP-SAT solver with full rule registry and synthetic solve (milestone 2).

**Architecture:** Registry-first .NET 10 solution; optional intervals per demand; DevHost for dev safety.

**Tech Stack:** .NET 10, Google.OrTools NuGet, xUnit, System.CommandLine, PowerShell `run-solver.ps1`.

---

## Phase 0 — DONE

- [x] Solution: Core, Cli, Tests, DevHost
- [x] OR-Tools smoke test
- [x] Loader + structural + R00 precheck
- [x] Rule registry R00–R40 (stubs)
- [x] Modes: `validate`, `profile` (diagnostic/solve stub)
- [x] `scripts/run-solver.ps1` + memory watchdog

**Gate:** `dotnet test` + `.\scripts\run-solver.ps1 -Mode validate -NoWatchdog`

---

## Phase 1 — Model skeleton (next)

### Task 1: SlotIndexer

- Create `Core/Model/SlotIndexer.cs` from `calendar.slots`
- Tests: slot count, week boundaries

### Task 2: SchedulingModel + optional intervals

- `DemandEntity`, `SchedulingModel` — one optional interval per demand
- OR-Tools: `NewOptionalIntervalVar`, `AddNoOverlap` for group/teacher/room

### Task 3: Enforce R01–R06 (MODEL_HARD)

- `Rules/Enforcements/ModelHardR01_R06.cs`
- Update registry status to `enforced`

### Task 4: Enforce R07–R09 (RELAXED objective)

- Unscheduled + virtual penalties in objective

### Task 5: Solve mode

- Wire `SolveMode`, time limit, diagnostic v2 fields from CP-SAT
- Gate: `run-solver.ps1 -Mode solve` FEASIBLE on synthetic-small

---

## Phase 2 — Enforcement waves

One subagent task per RELAXED/SOFT group; synthetic test + optional `-UseRealHandoff` profile.

---

## Phase 2b — Curated samples

New fixtures under `data/samples/` mirroring v1_1 shape without handoff DQ defects.

---

## Phase 3 — Real A/B diagnostic parity

Compare variant A vs B objectives/violations (local only).
