# Solver development strategy (design spec)

**Date:** 2026-05-25  
**Status:** Approved — see plan `docs/superpowers/plans/2026-05-25-solver-implementation-plan.md`  
**Scope:** Solver-only track (4–8 weeks), web minimal  
**Inputs:** Monorepo scaffold, handoff v2, skill `reading-solver-handoff-v2`

## Decisions locked by product owner

| Decision | Choice |
|----------|--------|
| Primary track | **A** — solver-only; web frozen except contract alignment |
| Phase-1 milestone | **2** — synthetic solve + full diagnostic (violations, virtual, enabled_rules) |
| Rule scope | **F** — full registry day one; **incremental enforcement** |
| Real data timing | **Early** — variant A/B in `profile` / `diagnostic` before stable synthetic solve |
| Modeling ban | **No** full Cartesian product `lesson × slot × teacher × room` |
| OR-Tools reference | **Context7** (`/websites/developers_google_optimization`, `/google/or-tools`) during implementation |

## 1. Principles

### 1.1 Diagnostic-first

A draft schedule with explicit violations beats `INFEASIBLE` without explanation. Every run emits structured diagnostics (handoff `AGENT_START_PROMPT` shape).

### 1.2 Boundary discipline

- Solver: JSON in → JSON out; no XLSX, Prisma, DB, UI.
- Handoff: domain evidence; candidate JSON, not production truth.
- Web: normalization/export later; not on critical path for this spec.

### 1.3 Registry-first, enforce-later

All rules **R00–R40** exist in code from phase 0 with metadata: `id`, `code`, `class`, `penalty`, `enforcement_status`.

| `enforcement_status` | Meaning in diagnostic |
|---------------------|------------------------|
| `enforced` | Active in CP-SAT or PRECHECK |
| `stub` | Listed; not in model; `warnings: RULE_NOT_ENFORCED_YET` |
| `disabled` | `UNRESOLVED` / `DOMAIN_FACT` / `DIAGNOSTIC_POLICY` only |

**Stub ≠ satisfied.** Never report zero violations for a stub rule.

### 1.4 Incremental proof

Each enforcement wave must:

1. Pass synthetic regression (CTest or script).
2. Optionally run **profile** on real A/B (no requirement for FEASIBLE).
3. Update `enforced_count` in diagnostic summary.

### 1.5 Model size discipline (OR-Tools)

Follow CP-SAT scheduling patterns from Google docs:

- **One scheduling entity per `lesson_demand`** (not per combination).
- **Optional interval** per demand: `presence` = scheduled; `~presence` = unscheduled (R07).
- **Start time** variable on a discrete slot index (or interval on slot timeline).
- **Resource conflict** via `AddNoOverlap` on interval lists per teacher / group / room (job-shop style).
- **Objective:** `LinearExpr` = Σ penalty × violation literals + unscheduled costs (see ranking / optional interval samples in OR-Tools scheduling docs).

**Forbidden:** explicit BoolVar for every tuple `(demand, slot, teacher, room)` when |slots|×|options| is large.

**Allowed compact encodings:**

- Single teacher / room in demand → fixed in normalization, no choice vars.
- Small finite domains → `AddExactlyOne` over **k** literals (`k ≤ 8` configurable), not full catalog.
- Channeling: `start_slot` + `presence` + resource NoOverlap, not 4D product.

### 1.6 Context7 usage

Before each modeling wave, query Context7 for the specific API (e.g. `NewOptionalIntervalVar`, `AddNoOverlap`, `OnlyEnforceIf`, cumulative for gym). Record link/snippet id in PR or commit message optional, not in solver binary.

## 2. CLI modes

Extend `schedule_solver` beyond today’s stub:

| Mode | Purpose | CP-SAT | Real A/B |
|------|---------|--------|----------|
| `validate` | PRECHECK R00, schema, references | No | Yes |
| `profile` | Stats: counts, bounds, rule stub/enforced map, est. var budget | No | **Yes, early** |
| `diagnostic` | Current stub → full diagnostic assembly | Optional partial | Yes |
| `solve` | Full solve + export | Yes | After synthetic green |

Flags (evolution):

```text
--input --output --mode {validate|profile|diagnostic|solve}
--time-limit SEC
--export-debug DIR
--dataset-variant {A|B}   # metadata only
```

**Profile output** (additive JSON):

- `lesson_demands`, `slots`, `calendar` counts
- `estimated_primary_variables` (document formula, not exact CP-SAT count)
- `rules_by_status`: enforced / stub / disabled
- `data_quality_warnings` passthrough from input
- **No** requirement for `schedule`

## 3. Phased roadmap

### Phase 0 — Foundation (week 1)

- OR-Tools CMake (`SCHED_ENABLE_ORTOOLS=ON`, Windows first).
- Loader: `real_candidate_v1_1` + `0.1` synthetic (version detect).
- Validator: R00 PRECHECK + JSON Schema subset.
- Rule registry: load all R00–R40 from input `rules[]` + static defaults for synthetic.
- CLI: `validate`, `profile`.
- **Gate:** `validate` passes on synthetic + variant A/B; `profile` completes &lt; 30s.

### Phase 1 — Model skeleton (week 2)

- Internal `SchedulingModel`: demands → optional intervals on slot axis.
- MODEL_HARD: R01–R06 (lesson state, slot validity, group/teacher/room no-overlap, active weeks, Saturday 1–4).
- RELAXED_HARD: R07–R09 (unscheduled, virtual teacher/room) in objective.
- `solve` on **synthetic-small** only.
- Diagnostic v2 fields: `cp_sat_status`, `objective_value`, `unscheduled_lessons`, `virtual_*`, `enabled_rules`, `rule_penalties`, `relaxed_hard_violations`.
- **Gate:** synthetic → FEASIBLE or FEASIBLE with expected violations; no Cartesian explosion (profile var estimate stable).

### Phase 2 — Enforcement waves (weeks 3–5)

Implement remaining rules in handoff priority order:

1. RELAXED_HARD by descending penalty (R10–R32…).
2. SOFT_STRONG → SOFT_MEDIUM → SOFT_WEAK.
3. Special: R25–R26 language parallel (interval linking, not 4D product).
4. R27–R28 gym cumulative / NoOverlap (OR-Tools `AddCumulative` + optional intervals per RCPSP examples).

Each wave: synthetic test + `profile` on real A/B.

**Gate milestone 2 (phase-1 complete):** synthetic solve + full violation accounting for **all enforced rules**; stubs explicitly listed.

### Phase 3 — Real diagnostic parity (week 6+)

- `diagnostic` + `solve` on variant A/B with time limits (30–120s).
- Compare A vs B script: objective, unscheduled, virtual, top violations.
- No claim of “production schedule” until user signs off data.

### Phase 4 — Deferred (out of scope here)

- Web export pipeline, HTTP wrapper, full `constraints` UI.
- UNRESOLVED R37–R40 enforcement.
- Weekly expansion of aggregated demands.

## 4. `apps/solver` module layout

```text
apps/solver/src/
  main.cpp                 # CLI
  json_io.*
  input/
    loader.*
    schema_version.*
  validation/
    precheck.*             # R00
  rules/
    rule_registry.*        # all rules, status
    rule_definition.h
    enforcements/          # one file per rule or group
  model/
    slot_indexer.*         # calendar → slot ids
    demand_entity.*        # one demand → vars
    scheduling_model.*     # builds CP-SAT
    resource_pool.*        # NoOverlap groups
  solver/
    solver_engine.h
    ortools_engine.*       # SCHED_ENABLE_ORTOOLS
    solver_engine_stub.cpp # fallback
  diagnostics/
    diagnostic_report.*
    profile_report.*
  modes/
    validate_mode.*
    profile_mode.*
    solve_mode.*
```

## 5. Contracts evolution

| Artifact | Action |
|----------|--------|
| `packages/shared-contracts` | Add `real_candidate_v1_1` schema branch or v0.2; diagnostic report schema from handoff |
| `data/samples/synthetic-small` | Enrich minimally to exercise R03–R09, one virtual path |
| Handoff JSON | Read-only under `data/solver_agent_full_handoff_v2/`; scripts `run-solver-profile-a.ps1` |

Solver **must not** depend on Prisma/Next.

## 6. Testing strategy

| Layer | What |
|-------|------|
| Unit | slot indexer, demand entity bounds, rule registry load |
| Golden | synthetic input → expected diagnostic fragments |
| Integration | CLI exit codes; solve &lt; N sec on synthetic |
| Regression | A/B profile JSON snapshot (counts, not full schedule) |
| Forbidden test | model var count ∝ product of all catalogs |

Use CTest (CMake) when OR-Tools enabled.

## 7. Real A/B early profile

**When:** end of phase 0, every enforcement wave.

**Command:**

```powershell
schedule_solver --input .../variant_A_no_merge_bakirova_valieva.json \
  --output tmp/profile_A.json --mode profile
```

**Compare A vs B:** teachers count, estimated vars, stub/enforced map, top `data_quality_warnings`.

**Not required:** FEASIBLE, optimal schedule, full rule enforcement.

## 8. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| F scope too large | Registry-first; stub visible; waves |
| Model blow-up | Ban 4D product; profile estimates; Context7 review per wave |
| OR-Tools build pain | Windows CI first; document vcpkg/binary install |
| Schema drift | Version field + adapter layer |
| False confidence on real data | `candidate_final_test` banner in every output |

## 9. Definition of Done

### Phase 0 Done

- [ ] OR-Tools links on Windows
- [ ] `validate` + `profile` on synthetic, A, B
- [ ] 45 rules in registry with status

### Milestone 2 Done (phase 1–2)

- [ ] `solve` synthetic → FEASIBLE/OPTIMAL with schedule
- [ ] Diagnostic includes violations, virtual, unscheduled, rule_penalties
- [ ] All non-UNRESOLVED rules either enforced or stub (never silent)
- [ ] A/B profile runs documented in `docs/local-development.md`
- [ ] No Cartesian product in model builder (code review checklist)

## 10. References

- Repo: `docs/architecture.md`, `docs/data-boundary.md`, skill `.cursor/skills/reading-solver-handoff-v2/`
- Handoff: `data/solver_agent_full_handoff_v2/AGENT_HANDOFF_V2_START_HERE.md`
- OR-Tools (Context7): scheduling job shop C++, optional intervals, `AddNoOverlap`, RCPSP optional intervals example (`rcpsp_sat.cc`)

---

**Next step after approval:** invoke `writing-plans` for file-level implementation plan (phase 0 tasks).
