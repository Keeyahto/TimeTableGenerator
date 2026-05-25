# Solver Implementation Plan (Phase 0 → Milestone 2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Evolve `apps/solver` from stub CLI to diagnostic-first CP-SAT solver with full rule registry (incremental enforcement), synthetic solve with violations, and early `profile` on real variant A/B — without lesson×slot×teacher×room Cartesian product.

**Architecture:** Registry-first rules; `ParsedInput` from JSON; modes `validate` / `profile` / `diagnostic` / `solve`; OR-Tools optional intervals per `lesson_demand` + `AddNoOverlap` resource pools; objective = weighted violations. Context7 consulted per modeling wave.

**Tech Stack:** C++20, CMake, nlohmann/json, OR-Tools CP-SAT (vcpkg or local install on Windows), CTest, PowerShell scripts.

**Spec:** `docs/superpowers/specs/2026-05-25-solver-development-strategy-design.md`

---

## File map (target layout)

| Path | Responsibility |
|------|----------------|
| `apps/solver/CMakeLists.txt` | OR-Tools option, sources, CTest |
| `apps/solver/src/main.cpp` | CLI dispatch |
| `apps/solver/src/input/parsed_input.h` | Domain structs from JSON |
| `apps/solver/src/input/loader.cpp` | Versioned load |
| `apps/solver/src/validation/precheck.cpp` | R00 |
| `apps/solver/src/rules/rule_definition.h` | Rule metadata + status enum |
| `apps/solver/src/rules/rule_registry.cpp` | R00–R40 catalog + merge input `rules[]` |
| `apps/solver/src/diagnostics/diagnostic_report.h` | Output v2 |
| `apps/solver/src/diagnostics/profile_report.cpp` | Profile JSON |
| `apps/solver/src/modes/validate_mode.cpp` | validate exit codes |
| `apps/solver/src/modes/profile_mode.cpp` | profile |
| `apps/solver/src/model/slot_indexer.cpp` | Calendar → slot list |
| `apps/solver/src/model/demand_entity.cpp` | Vars per demand (no 4D) |
| `apps/solver/src/model/scheduling_model.cpp` | CpModelBuilder assembly |
| `apps/solver/src/model/resource_pool.cpp` | NoOverlap lists |
| `apps/solver/src/solver/ortools_engine.cpp` | Solve + extract diagnostics |
| `apps/solver/tests/validate_synthetic_test.cpp` | CTest |
| `packages/shared-contracts/solver-output-v2.schema.json` | Diagnostic shape |
| `packages/shared-contracts/solver-input-v1_1.schema.json` | Real input (loose) |
| `scripts/run-solver-profile-a.ps1` | Profile variant A |
| `scripts/run-solver-profile-b.ps1` | Profile variant B |
| `scripts/compare-profile-ab.ps1` | Diff key metrics |

---

## Phase 0 — Foundation

### Task 1: Diagnostic output contract (shared-contracts)

**Files:**
- Create: `packages/shared-contracts/solver-output-v2.schema.json`
- Create: `packages/shared-contracts/src/solver-output-v2.ts`
- Modify: `packages/shared-contracts/package.json` (exports)
- Modify: `packages/shared-contracts/scripts/validate-schemas.mjs`

- [ ] **Step 1:** Add schema with required fields from handoff `AGENT_START_PROMPT` diagnostic block:

```json
{
  "schema_version": { "const": "0.2" },
  "status": { "type": "string" },
  "cp_sat_status": { "type": "string" },
  "objective_value": { "type": ["number", "null"] },
  "best_objective_bound": { "type": ["number", "null"] },
  "gap": { "type": ["number", "null"] },
  "wall_time_seconds": { "type": "number" },
  "enabled_rules": { "type": "array" },
  "rule_penalties": { "type": "array" },
  "unscheduled_lessons": { "type": "array" },
  "virtual_teachers_used": { "type": "array" },
  "virtual_rooms_used": { "type": "array" },
  "relaxed_hard_violations": { "type": "array" },
  "soft_violations": { "type": "array" },
  "data_quality_warnings": { "type": "array" },
  "rules_by_status": { "type": "object" },
  "schedule": { "type": ["object", "array", "null"] },
  "warnings": { "type": "array" }
}
```

- [ ] **Step 2:** Add `solver-input-v1_1.schema.json` — `additionalProperties: true`, required: `schema_version`, `calendar`, `groups`, `teachers`, `rooms`, `subjects`, `lesson_demands`, `rules`, `solver_config`.

- [ ] **Step 3:** Run `npm run validate:contracts` — Expected: OK both schemas.

- [ ] **Step 4:** Commit

```bash
git add packages/shared-contracts/
git commit -m "feat(contracts): add solver output v0.2 and real input v1.1 schemas"
```

---

### Task 2: OR-Tools CMake integration (Windows)

**Files:**
- Modify: `apps/solver/CMakeLists.txt`
- Create: `apps/solver/cmake/FindORTools.cmake` (if find_package fails)
- Modify: `apps/solver/README.md`
- Modify: `docs/local-development.md`

**Context7 before coding:** query `/websites/developers_google_optimization` — "C++ CMake install OR-Tools Windows vcpkg"

- [ ] **Step 1:** Document prerequisite in `apps/solver/README.md`:

```text
vcpkg install ortools:x64-windows
cmake -S apps/solver -B apps/solver/build -DSCHED_ENABLE_ORTOOLS=ON `
  -DCMAKE_TOOLCHAIN_FILE=[vcpkg]/scripts/buildsystems/vcpkg.cmake
```

- [ ] **Step 2:** In `CMakeLists.txt` when `SCHED_ENABLE_ORTOOLS`:

```cmake
find_package(ortools CONFIG REQUIRED)
target_link_libraries(schedule_solver PRIVATE ortools::ortools)
target_compile_definitions(schedule_solver PRIVATE SCHED_HAS_ORTOOLS=1)
```

Keep building without OR-Tools when OFF (stub path).

- [ ] **Step 3:** Configure and build both configs:

```powershell
cmake -S apps/solver -B apps/solver/build -DSCHED_ENABLE_ORTOOLS=OFF
cmake --build apps/solver/build --config Release
cmake -S apps/solver -B apps/solver/build-ortools -DSCHED_ENABLE_ORTOOLS=ON -DCMAKE_TOOLCHAIN_FILE=$env:VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake
cmake --build apps/solver/build-ortools --config Release
```

Expected: both produce `schedule_solver.exe` (ortools build links).

- [ ] **Step 4:** Commit

```bash
git add apps/solver/CMakeLists.txt apps/solver/README.md docs/local-development.md
git commit -m "build(solver): optional OR-Tools via vcpkg on Windows"
```

---

### Task 3: Rule definition + static registry R00–R40

**Files:**
- Create: `apps/solver/src/rules/rule_definition.h`
- Create: `apps/solver/src/rules/rule_registry.h`
- Create: `apps/solver/src/rules/rule_registry.cpp`
- Create: `apps/solver/data/rule_catalog.json` (copy from handoff CSV semantics)

- [ ] **Step 1:** Define enums in `rule_definition.h`:

```cpp
enum class RuleClass {
  PRECHECK_HARD, MODEL_HARD, RELAXED_HARD,
  SOFT_STRONG, SOFT_MEDIUM, SOFT_WEAK,
  DOMAIN_FACT, DIAGNOSTIC_POLICY, UNRESOLVED
};
enum class EnforcementStatus { enforced, stub, disabled };
struct RuleDefinition {
  std::string id;      // R00
  std::string code;    // group_no_overlap
  RuleClass rule_class;
  int64_t penalty_weight;  // 0 if N/A
  EnforcementStatus default_status;
};
```

- [ ] **Step 2:** Populate `kDefaultCatalog` array with all 41 rows from `data/solver_agent_full_handoff_v2/.../rule_registry_v1.csv`:
  - R33–R36, R37–R40 → `default_status = disabled`
  - R00 → `enforced` (precheck)
  - R01–R06, R15, R27 → `stub` until Phase 1 (then flip to enforced)
  - All other RELAXED/SOFT → `stub` in Phase 0

- [ ] **Step 3:** `RuleRegistry::LoadFromInput(json rules array)` merges input overrides (penalty, enabled flag) onto catalog by `code`.

- [ ] **Step 4:** Add CTest-less smoke in `apps/solver/tests/rule_registry_test.cpp`:

```cpp
TEST(RuleRegistry, Has41Rules) {
  RuleRegistry reg;
  reg.LoadDefaults();
  EXPECT_EQ(reg.All().size(), 41u);
  EXPECT_EQ(reg.Get("R37").default_status, EnforcementStatus::disabled);
}
```

- [ ] **Step 5:** Register test in CMake `enable_testing()` + `add_test`.

- [ ] **Step 6:** Commit

```bash
git add apps/solver/src/rules/ apps/solver/tests/rule_registry_test.cpp apps/solver/CMakeLists.txt
git commit -m "feat(solver): static rule registry R00-R40 with enforcement status"
```

---

### Task 4: ParsedInput loader (0.1 + real_candidate_v1_1)

**Files:**
- Create: `apps/solver/src/input/parsed_input.h`
- Create: `apps/solver/src/input/loader.cpp`
- Modify: `apps/solver/CMakeLists.txt`

- [ ] **Step 1:** `parsed_input.h` structs: `Calendar`, `Slot`, `Group`, `Teacher`, `Room`, `Subject`, `LessonDemand`, `RuleConfig`, `schema_version`, `dataset_variant`, `data_quality_warnings`.

- [ ] **Step 2:** `LoadInputFromJson(nlohmann::json)`:
  - if `schema_version == "0.1"` → minimal synthetic path
  - if `schema_version == "real_candidate_v1_1"` → full path including `rules`, `bundles`
  - else → error `UNSUPPORTED_SCHEMA_VERSION`

- [ ] **Step 3:** Test `loader_test.cpp` loads `data/samples/synthetic-small/input.json` and variant A path (first 200 lines not needed — full parse).

```cpp
TEST(Loader, Synthetic) {
  auto j = LoadJsonFile("data/samples/synthetic-small/input.json");
  auto in = LoadInputFromJson(j);
  ASSERT_EQ(in.schema_version, "0.1");
  ASSERT_GE(in.lesson_demands.size(), 1u);
}
```

- [ ] **Step 4:** Run CTest — Expected: PASS

- [ ] **Step 5:** Commit

```bash
git add apps/solver/src/input/ apps/solver/tests/loader_test.cpp
git commit -m "feat(solver): versioned JSON loader for synthetic and real v1_1"
```

---

### Task 5: PRECHECK R00 (validate references)

**Files:**
- Create: `apps/solver/src/validation/precheck.h`
- Create: `apps/solver/src/validation/precheck.cpp`
- Create: `apps/solver/tests/precheck_test.cpp`

- [ ] **Step 1:** Build ID sets from `groups`, `teachers`, `rooms`, `subjects`, slot ids from `calendar.slots`.

- [ ] **Step 2:** For each `lesson_demand` verify `group_id`, `subject_id`, each `teacher_options[]`, `subgroup_ids[]` exist; collect errors `{field, id, message}`.

- [ ] **Step 3:** Failing test — demand with bad `teacher_id`:

```cpp
TEST(Precheck, InvalidTeacherFails) {
  ParsedInput in = MinimalSynthetic();
  in.lesson_demands[0].teacher_options = {"missing"};
  auto r = RunPrecheck(in);
  EXPECT_FALSE(r.ok);
  EXPECT_GE(r.errors.size(), 1u);
}
```

- [ ] **Step 4:** Run CTest — Expected: PASS

- [ ] **Step 5:** Commit

```bash
git add apps/solver/src/validation/
git commit -m "feat(solver): R00 precheck for reference integrity"
```

---

### Task 6: CLI mode dispatch

**Files:**
- Modify: `apps/solver/src/main.cpp`
- Create: `apps/solver/src/modes/validate_mode.cpp`
- Create: `apps/solver/src/modes/profile_mode.cpp`
- Modify: `apps/solver/src/diagnostic_report.h` → move/rename to `diagnostics/`

- [ ] **Step 1:** Extend CLI parser:

```cpp
enum class RunMode { validate, profile, diagnostic, solve };
// --mode validate|profile|diagnostic|solve
// --time-limit 30 (solve only)
```

- [ ] **Step 2:** Pipeline in main:

```text
load JSON → loader → precheck (validate/solve/profile/diagnostic)
→ branch on mode
```

Exit codes: `0` ok, `1` cli, `2` io, `3` parse, `4` precheck fail, `5` solver fail.

- [ ] **Step 3:** `validate_mode`: write `{ "status": "VALIDATED", "precheck": {...}, "rules_by_status": {...} }`

- [ ] **Step 4:** Manual run:

```powershell
apps\solver\build\Release\schedule_solver.exe `
  --input data\samples\synthetic-small\input.json `
  --output tmp\validate.json --mode validate
```

Expected: exit 0, JSON status VALIDATED.

- [ ] **Step 5:** Commit

```bash
git add apps/solver/src/main.cpp apps/solver/src/modes/
git commit -m "feat(solver): validate mode and CLI dispatch"
```

---

### Task 7: Profile mode + var budget estimate

**Files:**
- Modify: `apps/solver/src/modes/profile_mode.cpp`
- Create: `apps/solver/src/model/var_budget.cpp`

- [ ] **Step 1:** Var budget formula (document in output):

```text
primary_vars ≈ Σ_demands (1 presence + 1 start_slot + k_teacher_choice + k_room_choice)
where k = max(0, options-1) capped at 8 per demand
FORBIDDEN: multiply |demands|×|slots|×|teachers|×|rooms|
```

- [ ] **Step 2:** `profile_mode` output JSON:

```json
{
  "schema_version": "0.2",
  "status": "PROFILE",
  "counts": { "lesson_demands": 353, "slots": 68, "teachers": 54 },
  "estimated_primary_variables": 1200,
  "var_budget_method": "demand_centric_v1",
  "rules_by_status": { "enforced": ["R00"], "stub": ["R01", "..."], "disabled": ["R37","R38","R39","R40"] },
  "data_quality_warnings": []
}
```

- [ ] **Step 3:** Scripts:

`scripts/run-solver-profile-a.ps1`:

```powershell
$exe = "apps/solver/build/Release/schedule_solver.exe"
& $exe --input data/solver_agent_full_handoff_v2/02_canonical_solver_input_v1_1/solver_input_real_v1/variant_A_no_merge_bakirova_valieva.json `
  --output tmp/profile_A.json --mode profile
```

Mirror for B.

- [ ] **Step 4:** Run profile A — Expected: completes &lt; 30s, exit 0.

- [ ] **Step 5:** Commit

```bash
git add apps/solver/src/modes/profile_mode.cpp scripts/run-solver-profile-*.ps1
git commit -m "feat(solver): profile mode with var budget and A/B scripts"
```

---

### Task 8: Phase 0 gate script

**Files:**
- Create: `scripts/phase0-gate.ps1`

- [ ] **Step 1:** Script runs:

```powershell
validate synthetic, validate variant A, validate variant B
profile A, profile B
ctest -C Release (if OR-Tools build dir)
```

- [ ] **Step 2:** Document in `docs/local-development.md` § Phase 0 gate.

- [ ] **Step 3:** Commit

```bash
git add scripts/phase0-gate.ps1 docs/local-development.md
git commit -m "chore(solver): phase 0 gate script"
```

**Phase 0 Done when:** all checkboxes in spec §9 Phase 0 are satisfied.

---

## Phase 1 — CP-SAT skeleton (synthetic)

**Context7 queries (mandatory before Task 9):**
1. `NewOptionalIntervalVar` C++ `CpModelBuilder`
2. `AddNoOverlap` multiple interval lists
3. `Minimize` linear expression with BoolVar penalties

### Task 9: Slot indexer

**Files:**
- Create: `apps/solver/src/model/slot_indexer.h`
- Create: `apps/solver/src/model/slot_indexer.cpp`

- [ ] Build ordered `slot_index → SlotMeta` from calendar; filter Saturday slots 5–6 when R15 enforced.
- [ ] Unit test: synthetic 2 slots → size 2; real input → 68.

### Task 10: Demand entity (no 4D product)

**Files:**
- Create: `apps/solver/src/model/demand_entity.h`
- Create: `apps/solver/src/model/demand_entity.cpp`

- [ ] Per demand create only:
  - `BoolVar presence`
  - `IntVar start_index` in `[0, num_slots-1]`
  - Optional teacher choice: `AddExactlyOne` over ≤8 literals OR fixed teacher index
  - Map start_index → optional interval on slot timeline (fixed duration from demand)

- [ ] **Assert in debug:** `vars_created < 20 * num_demands`.

### Task 11: Resource pools + MODEL_HARD R01–R06, R15, R27

**Files:**
- Create: `apps/solver/src/model/resource_pool.cpp`
- Create: `apps/solver/src/model/scheduling_model.cpp`
- Create: `apps/solver/src/rules/enforcements/model_hard.cpp`

- [ ] Group/teacher/room interval lists → `AddNoOverlap`.
- [ ] R01: presence implies valid start; scheduled ↔ interval active.
- [ ] R06: inactive weeks → forbid corresponding slot subset.
- [ ] Flip R01–R06, R15, R27 to `enforced` in registry after tests pass.

### Task 12: RELAXED_HARD R07–R09 + objective

**Files:**
- Create: `apps/solver/src/rules/enforcements/relaxed_unscheduled_virtual.cpp`
- Modify: `apps/solver/src/solver/ortools_engine.cpp`

- [ ] R07: `objective += 1'000'000 * (1 - presence)` per demand.
- [ ] R08–R09: virtual teacher/room literals (stub assignment to sentinel resources on synthetic).
- [ ] `solve` mode: write diagnostic v0.2 + `schedule` array.

### Task 13: Enrich synthetic-small

**Files:**
- Modify: `data/samples/synthetic-small/input.json`
- Create: `data/samples/synthetic-small/expected_feasible.json` (partial golden)

- [ ] 3 demands, 6 slots, 2 teachers, 2 rooms — force one unscheduled path test.

### Task 14: CTest solve integration

- [ ] `solve_synthetic_test.cpp` runs CLI solve, expects `cp_sat_status` in `{OPTIMAL,FEASIBLE}` within 30s.

**Phase 1 gate:** `.\scripts\run-solver-dev.ps1` with `--mode solve` on synthetic.

---

## Phase 2 — Enforcement waves (Milestone 2)

Implement one wave per commit group; after each wave run `phase0-gate.ps1` profile + synthetic solve.

| Wave | Rules | Files (pattern) |
|------|-------|-----------------|
| 2a | R10–R14, R22 | `enforcements/availability_shift.cpp` |
| 2b | R16–R19, R29–R32 | `enforcements/class_hour_rooms.cpp` |
| 2c | R20–R24 SOFT | `enforcements/soft_quality.cpp` |
| 2d | R25–R26 language | `enforcements/language_parallel.cpp` — link intervals, not 4D |
| 2e | R28 gym | `enforcements/gym_cumulative.cpp` — Context7 RCPSP optional intervals |

Each wave:
- [ ] Flip rules to `enforced` in registry defaults
- [ ] Add violation extraction to `ortools_engine.cpp` post-solve
- [ ] Add golden fragment test for violation code

**Milestone 2 gate:** all non-disabled rules `enforced` or `stub` with `RULE_NOT_ENFORCED_YET` in profile; synthetic solve returns schedule + `rule_penalties` non-empty where seeded violation.

---

## Phase 3 — Real A/B diagnostic parity

### Task 15: Compare script

**Files:** `scripts/compare-profile-ab.ps1` — diff teachers, vars, top warnings.

### Task 16: solve on real (time-limited)

- [ ] `solve` variant A, 60s limit, output `tmp/solve_A.json`
- [ ] Document: candidate data banner in output metadata
- [ ] No requirement for OPTIMAL

### Task 17: `diagnostic` mode merges profile + partial solve stats

---

## Code review checklist (every PR touching model)

- [ ] No nested loop creating BoolVar for `(demand, slot, teacher, room)` quadruple
- [ ] `profile.estimated_primary_variables` uses demand-centric formula
- [ ] Stub rules appear in `rules_by_status.stub`, not silent pass
- [ ] Context7 query noted in PR description for new OR-Tools APIs

---

## Spec coverage self-review

| Spec § | Tasks |
|--------|-------|
| Registry-first F | Task 3, all Phase 2 waves |
| No Cartesian product | Tasks 7, 10, checklist |
| Early A/B profile | Tasks 7–8, 15 |
| CLI modes | Task 6, 16–17 |
| OR-Tools Context7 | Tasks 2, 9–12, 2e |
| Milestone 2 diagnostic | Tasks 12, 14, Phase 2 |
| Web frozen | No web tasks |
| UNRESOLVED disabled | Task 3 R37–R40 |

No TBD placeholders in task steps above.

---

## Execution handoff

Plan targets **Phase 0 Tasks 1–8 first** (~1 week), then Phase 1 Tasks 9–14, then Phase 2 waves.
