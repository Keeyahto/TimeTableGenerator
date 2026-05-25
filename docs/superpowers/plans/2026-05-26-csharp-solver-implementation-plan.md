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

**Next:** Phase 3 diagnostic/solve on handoff; remaining R16–R18, R23, R31 stubs.

## Phase 2 — Wave 3 DONE

- [x] R24 subject max once per day (SOFT)
- [x] R29/R30 room blocked days via `rules[]` params + `RoomBlockedDaysEnforcer`
- [x] Samples: `synthetic-r24-subject`, `synthetic-r29-room203`
- [x] Edge-case tests (29+) + valid-start fix in `ModelStructureEnforcer`

## Phase 2b — DONE (mini)

- [x] `data/samples/curated-v1_1-mini/` — `real_candidate_v1_1` + `rules[]`, no DQ defects
- [x] `Phase2bCuratedTests` validate + solve gate

---

## Phase 2 — Wave 4 DONE

- [x] v1_1 field mapping (`lesson_demand_id`, `teacher_options`, `day_id`, `slot_id`, …)
- [x] `blocked_rules` → R32; gym `max_parallel_groups` → R27 (skip strict room NoOverlap for gym)
- [x] R25/R26 language parallel link + same-teacher clash
- [x] Samples: `synthetic-r25-r26-language`, `synthetic-r27-gym`, `synthetic-r32-blocked`
- [x] `V11MappingTests` + handoff precheck when local PD present

## Phase 2 — Remaining

- [ ] R16–R18 class hour; R23 room manager; R31 week pattern

---

## Phase 3 — IN PROGRESS

- [x] Handoff variant A passes R00 precheck + profile (when `data/solver_agent_full_handoff_v2` present)
- [x] `scripts/compare-handoff-ab.ps1` — profile metrics A vs B
- [ ] `diagnostic` / `solve` on full handoff with time limit (local)
- [ ] Objective/violation parity report A vs B

---

## Phase 2b — Curated samples (ongoing)

Add more `real_candidate_v1_1` fixtures beyond `curated-v1_1-mini` as rules land.
