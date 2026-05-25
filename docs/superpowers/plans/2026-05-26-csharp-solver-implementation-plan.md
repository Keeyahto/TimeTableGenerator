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

**Next:** Phase 2 wave 2 (R11–R14, R22) + SOFT R20–R24; then phase 2b curated samples.

---

## Phase 2 — Remaining waves

One subagent task per RELAXED/SOFT group; synthetic test + optional `-UseRealHandoff` profile.

---

## Phase 2b — Curated samples

New fixtures under `data/samples/` mirroring v1_1 shape without handoff DQ defects.

---

## Phase 3 — Real A/B diagnostic parity

Compare variant A vs B objectives/violations (local only).
