# C# Solver Design Spec

**Date:** 2026-05-26  
**Status:** Approved (brainstorming)  
**Supersedes:** C++ implementation details in `2026-05-25-solver-development-strategy-design.md` and `2026-05-25-solver-implementation-plan.md`  
**Extends:** `2026-05-26-solver-stack-pivot-csharp.md` (stack pivot)  
**Next step:** `writing-plans` → `2026-05-26-csharp-solver-implementation-plan.md`

## Summary

Build `apps/solver` as a **.NET 10** solution: **Registry-first** rule catalog (R00–R40), **diagnostic-first** CP-SAT via **Google.OrTools** NuGet, JSON in/out unchanged. Development uses **subagent-sized tasks** (Core module + Tests + script gate). **Windows-only** for phase 0–2. **DevTime memory watchdog** kills the solver process before Windows becomes unusable.

## Locked product decisions (unchanged from 2026-05-25)

| Decision | Choice |
|----------|--------|
| Track | Solver-only; web frozen except contract alignment |
| Milestone 2 | Synthetic solve + full diagnostic (violations, virtual, enabled_rules) |
| Rules | Full registry day one; **incremental enforcement** |
| Real data | Early `profile` / `diagnostic` on variant A/B (local, opt-in) |
| Modeling | **No** Cartesian product `lesson × slot × teacher × room` |
| Data quality | Defects in source data = user/web/audit problem, **not** solver |

## Locked technical decisions (brainstorming 2026-05-26)

| Topic | Choice |
|-------|--------|
| Runtime | **.NET 10** |
| Platform | **Windows only** (phase 0–2); linux-x64 deferred (csproj RID later) |
| OR-Tools | NuGet `Google.OrTools` + `Google.OrTools.runtime.win-x64` |
| Architecture approach | **#3 Registry-first + modes** |
| Solution layout | `ScheduleSolver.Core` + `ScheduleSolver.Cli` + `ScheduleSolver.Tests` |
| Verification gates | **xUnit** + **`scripts/run-solver.ps1`**; handoff **opt-in** |
| Contract validation | **C# schema read** from `packages/shared-contracts` **and** `npm run validate:contracts` in monorepo gate |
| Samples | **Clean synthetic** in `data/samples/` for CI; **separate phase** for curated samples shaped like real v1_1 without DQ defects |
| Handoff in automation | **`-UseRealHandoff`** flag; **never default in CI** |
| DevTime safety | **`ScheduleSolver.DevHost`** memory watchdog at **95%** system RAM (configurable) |

## 1. Solution layout

```
apps/solver/
  ScheduleSolver.sln
  src/
    ScheduleSolver.Core/          # net10.0 — all domain + OR-Tools
    ScheduleSolver.Cli/           # System.CommandLine — dispatch only
  tests/
    ScheduleSolver.Tests/         # xUnit
  tools/
    ScheduleSolver.DevHost/       # dev-only process wrapper + memory watchdog
```

### Core module map (subagent tasks)

| Folder | Responsibility |
|--------|----------------|
| `Input/` | Load `0.1` + `real_candidate_v1_1`, version detect |
| `Validation/` | R00 PRECHECK, schema validation (read shared-contracts JSON) |
| `Rules/` | `RuleRegistry`, `RuleDefinition`, merge `input.rules[]` |
| `Rules/Enforcements/` | One file / group per rule wave (`IRuleEnforcer`) |
| `Model/` | `SlotIndexer`, `DemandEntity`, `SchedulingModel`, `ResourcePool` |
| `Solver/` | `OrToolsEngine` wrapping `CpModel` / `CpSolver` |
| `Diagnostics/` | Profile + diagnostic v0.2 assembly |
| `Modes/` | `ValidateMode`, `ProfileMode`, `DiagnosticMode`, `SolveMode` |

### Boundaries (unchanged)

- Solver: JSON files only; no Prisma, DB, UI, XLSX.
- Schemas: authoritative in `packages/shared-contracts/`.
- Handoff directory: local evidence; not production truth.

## 2. CLI

```text
ScheduleSolver.Cli
  --input <path>
  --output <path>
  --mode validate | profile | diagnostic | solve
  --time-limit <seconds>       # solve / partial diagnostic
  --export-debug <directory>   # optional model dumps
  --dataset-variant A|B        # metadata in output only
```

| Mode | CP-SAT | Purpose |
|------|--------|---------|
| `validate` | No | Schema + R00 + references |
| `profile` | No | Counts, var budget estimate, rules_by_status |
| `diagnostic` | Optional partial | Assemble diagnostic report |
| `solve` | Yes | Full solve + export schedule |

Exit codes: `0` = success for mode; non-zero on PRECHECK/schema failure.

## 3. Validation pipeline

1. **In-process (Core):** load `solver-input*.schema.json` / `solver-output-v2.schema.json` from monorepo path (`CONTRACTS_ROOT` or walk up to repo root). Validate document shape + R00 PRECHECK.
2. **Monorepo gate:** `npm run validate:contracts` (ajv) — catches drift between TS schemas and solver expectations.

Solver does **not** fork or duplicate schema files into `apps/solver`.

## 4. Data contours

```
┌────────────────────────┐   ┌─────────────────────────────┐   ┌──────────────────┐
│ data/samples/          │   │ Handoff A/B (gitignored)    │   │ Web export       │
│ clean synthetic        │   │ -UseRealHandoff (local)     │   │ phase 4+         │
│ CI + dotnet test       │   │ profile / diagnostic        │   │                  │
└────────────────────────┘   └─────────────────────────────┘   └──────────────────┘
```

### Samples policy

- **Synthetic-small** (and future fixtures): intentionally valid; exercise rules without mirroring college DQ issues.
- **Curated samples phase (2b):** new JSON files structurally aligned with `real_candidate_v1_1` but edited for consistency; used for regression after Core stabilizes.
- **Never** treat handoff JSON defects as “solver must accept.”

### `scripts/run-solver.ps1`

- Default: build Release, run via **DevHost** → Cli on `data/samples/synthetic-small/input.json`.
- `-UseRealHandoff`: path to local `variant_A_*.json` or `variant_B_*.json` (developer machine only).
- `-Mode`, `-TimeLimit`, `-NoWatchdog` forwarded.

## 5. Rule registry

All rules **R00–R40** registered at startup:

| `enforcement_status` | Behavior |
|--------------------|----------|
| `enforced` | Active in PRECHECK or CP-SAT |
| `stub` | Listed; `RULE_NOT_ENFORCED_YET` in output |
| `disabled` | Metadata only (UNRESOLVED / DOMAIN_FACT) |

**Stub ≠ satisfied.** Zero violations for a stub rule is forbidden.

```csharp
interface IRuleEnforcer {
  string RuleId { get; }
  void Apply(SchedulingModel model, ParsedInput input);
}
```

Subagent task pattern: one PR ≈ one enforcement group in `Rules/Enforcements/`.

## 6. CP-SAT model (OR-Tools .NET)

| Entity | Implementation |
|--------|----------------|
| Timeline | `SlotIndexer` → discrete slot indices |
| Demand | **One optional interval per `lesson_demand`** |
| Presence | `presence` literal; unscheduled penalized (R07) |
| Resources | `AddNoOverlap` on teacher / group / room interval lists |
| Objective | `LinearExpr`: penalties × violation literals + unscheduled + virtual costs |

**Forbidden:** explicit BoolVar per `(demand, slot, teacher, room)` tuple at scale.

**Allowed:** `AddExactlyOne` when branch count ≤ `MaxBranches` (default 8, configurable).

### Phase 1 enforcement scope

- **MODEL_HARD:** R01–R06 (lesson state, slot validity, no-overlap, active weeks, Saturday 1–4).
- **RELAXED_HARD in objective:** R07–R09 (unscheduled, virtual teacher/room).
- All other rules: `stub` until phase 2 waves.

### Output

Must conform to `solver-output-v2.schema.json`: `cp_sat_status`, `objective_value`, `enabled_rules`, `relaxed_hard_violations`, `unscheduled_lessons`, `virtual_*`, etc.

## 7. DevTime memory watchdog

**Problem:** CP-SAT development runs can exhaust RAM and force OS reboot.

**Solution:** `ScheduleSolver.DevHost` (tools project, dev-only).

| Setting | Default |
|---------|---------|
| `SCHED_MEM_LIMIT_PCT` | `95` |
| Poll interval | 500 ms |
| On breach | Kill entire child process tree |
| Exit code | `137` (watchdog OOM) |
| Log | `tmp/solver-watchdog.log` (peak MB, timestamp) |
| Bypass | `--no-watchdog` on `run-solver.ps1` |

**Measurement:** child working set (include child processes if spawned) vs total visible memory (WMI `Win32_OperatingSystem` or `GC.GetGcmemoryInfo().TotalAvailableMemoryBytes`).

**Not embedded in Core** — production publish can invoke Cli directly without DevHost.

```
run-solver.ps1 → DevHost → ScheduleSolver.Cli
```

## 8. Phased roadmap

| Phase | Deliverables | Gate |
|-------|--------------|------|
| **0** | Solution scaffold, NuGet smoke, loader, registry, validate/profile, DevHost + script | `dotnet test`; script on synthetic; validate contracts npm |
| **1** | SchedulingModel, R01–R09, solve on synthetic | FEASIBLE + diagnostic v2 fields |
| **2** | Enforcement waves → milestone 2 | All enforced rules accounted; stubs explicit |
| **2b** | Curated samples (clean, real-shaped) | New fixtures + tests; no handoff DQ |
| **3** | diagnostic/solve A/B local, comparison script | opt-in handoff only |
| **4** | Web export, HTTP | out of scope |

## 9. Testing strategy

| Layer | Tool |
|-------|------|
| Unit / integration | xUnit in `ScheduleSolver.Tests` |
| CLI smoke | `scripts/run-solver.ps1` (DevHost on by default) |
| Contracts | `npm run validate:contracts` |
| Real A/B | Manual / `-UseRealHandoff`; not CI default |
| Golden JSON | Optional later for synthetic only (phase 2b+) |

## 10. Error handling

| Event | Response |
|-------|----------|
| Schema / PRECHECK failure | Non-zero exit; errors in diagnostic JSON |
| Watchdog trip | Kill process; exit 137; log peak memory |
| CP-SAT time limit | Best-effort solution + status in output |
| Unenforced rule | `RULE_NOT_ENFORCED_YET`, never silent pass |

## 11. Out of scope (this spec)

- Linux/macOS RID and CI runners
- HTTP wrapper over solver
- Web UI solver launch / Prisma import pipeline
- Full enforcement of UNRESOLVED R37–R40
- Weekly expansion of aggregated `lesson_demands`
- Importing XLSX inside solver

## 12. References

- Handoff skill: `.cursor/skills/reading-solver-handoff-v2/SKILL.md`
- Contracts: `packages/shared-contracts/`
- Archived strategy (product only): `2026-05-25-solver-development-strategy-design.md`
- Stack pivot: `2026-05-26-solver-stack-pivot-csharp.md`
