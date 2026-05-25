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

## Phase 1 — DONE

- [x] `SlotIndexer` from `calendar.slots` (+ horizon padding for interval ends)
- [x] `SchedulingModelBuild` — `NewOptionalFixedSizeIntervalVar` + `AddNoOverlap` (Context7)
- [x] R01–R09 enforced in model (overlap, Saturday filter, minimize unscheduled / R07)
- [x] `solve` + `diagnostic` modes via `CpSatSolveService`
- [x] Gate: `dotnet test` + `.\scripts\run-solver.ps1 -Mode solve -NoWatchdog`

## Phase 2 — Wave 1 DONE

- [x] `IRuleEnforcer` pipeline + handoff penalties in `RuleCatalog`
- [x] R08 virtual teacher, R09 virtual room, R10 teacher unavailable
- [x] R19 group max lessons per day (soft violation vars)
- [x] `data/samples/synthetic-phase2/input.json` + test
- [x] `relaxed_hard_violations` populated from solver

## Phase 2 — Wave 2 DONE

- [x] R11 Thursday slot 1, R12/R13 first shift 1–4, R14 no Saturday (1st course), R22 admin no Saturday
- [x] R20/R21 gap SOFT penalties (`GapSoftEnforcer`)
- [x] `SchedulingConstraintHelper`, `RuleClass` on violations → `soft_violations` / `relaxed_hard_violations`
- [x] Sample `data/samples/synthetic-wave2/input.json`

**Next:** Phase 3 handoff A/B diagnostic parity; remaining R15–R18, R23–R28, R31–R32 stubs.

## Phase 2 — Wave 3 DONE

- [x] R24 subject max once per day (SOFT)
- [x] R29/R30 room blocked days via `rules[]` params + `RoomBlockedDaysEnforcer`
- [x] Samples: `synthetic-r24-subject`, `synthetic-r29-room203`
- [x] Edge-case tests (29+) + valid-start fix in `ModelStructureEnforcer`

## Phase 2b — DONE (mini)

- [x] `data/samples/curated-v1_1-mini/` — `real_candidate_v1_1` + `rules[]`, no DQ defects
- [x] `Phase2bCuratedTests` validate + solve gate

---

## Phase 2 — Remaining (R23–R32, language, gym)

One subagent task per group; synthetic test + optional `-UseRealHandoff` profile.

---

## Phase 2b — Curated samples

New fixtures under `data/samples/` mirroring v1_1 shape without handoff DQ defects.

---

## Phase 3 — Real A/B diagnostic parity

Compare variant A vs B objectives/violations (local only).
