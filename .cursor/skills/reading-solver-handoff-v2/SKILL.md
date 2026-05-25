---
name: reading-solver-handoff-v2
description: Use when developing the .NET solver, JSON contracts, or CP-SAT rules for TimeTableGenerator and the path `data/solver_agent_full_handoff_v2` is present. Use when unsure which handoff file to open first, whether real JSON is production-ready, or how variant A/B and rule classes apply.
---

# Reading solver handoff v2

## Overview

`data/solver_agent_full_handoff_v2` is a **domain evidence + candidate input** bundle, not a drop-in production dataset. Read it in a fixed order; implement solver only against **exported JSON**, never against XLSX inside the solver process.

**Violating the letter of these rules violates the spirit of diagnostic-first solver design.**

## When to use

- Implementing loader, validator, rule registry, or CP-SAT in `apps/solver`
- Evolving `packages/shared-contracts` toward real data
- Comparing solver runs on real college data (variants A/B)
- Debugging why real input differs from `data/samples/synthetic-small`

## When NOT to use

- Web UI, Prisma, file upload, XLSX parsing → stay in `apps/web` and `docs/data-boundary.md`
- Generic OR-Tools tutorials without this repo’s contracts
- Re-normalizing dirty Excel inside the solver

## Read order (mandatory)

1. `AGENT_HANDOFF_V2_START_HERE.md` — entry + development order
2. `01_solver_backend_handoff_v1/06_agent_start/AGENT_START_PROMPT.md` — architecture, constraint classes, diagnostic report shape, CLI goals
3. `01_solver_backend_handoff_v1/06_agent_start/DATA_READINESS_STATUS.md` — **historical** v1 notes; if it says “no final JSON”, prefer v2 `02_*` (JSON exists as **candidate**)
4. `02_canonical_solver_input_v1_1/README.md` + `summary.json` — counts, accepted decisions, limitations
5. `02_canonical_solver_input_v1_1/reports/canonical_dataset_v1_1_report.md` — v1.1 language-row patch
6. `01_solver_backend_handoff_v1/04_rule_registry_v1/rule_registry_v1/rule_registry_v1.md` — rule IDs, penalties, MODEL_HARD vs RELAXED_HARD

Deep dives only when needed:

| Need | Open |
|------|------|
| DQ / issues | `01_.../02_data_quality_audit_v2/` |
| Teacher aliases | `01_.../05_teacher_audits/` |
| Week counts ТДК | `01_.../03_week_count_dependency_audit/` |
| CSV source of JSON | `02_.../canonical_dataset_v1/` |
| Trace to Excel cell | `lesson_demands[].source` in real JSON |

## Bundle map

```
solver_agent_full_handoff_v2/
├── 01_solver_backend_handoff_v1/   # Context: XLSX, audits, prompts (solver must NOT depend on this)
└── 02_canonical_solver_input_v1_1/ # Candidate JSON + CSV for solver
    ├── solver_input_real_v1/
    │   ├── variant_A_no_merge_bakirova_valieva.json
    │   └── variant_B_merge_bakirova_valieva.json
    └── canonical_dataset_v1/       # Tabular mirror; use JSON for CLI runs
```

## Contract vs monorepo today

| Handoff (real) | Monorepo scaffold (`0.1`) |
|----------------|---------------------------|
| `schema_version`: `real_candidate_v1_1` | `0.1` |
| `rules[]` with penalty classes | `constraints` stub |
| Rich `calendar` (upper/lower, 68 slots) | Minimal slots |
| `bundles`, `data_quality_warnings` | Not modeled yet |

Do **not** force variant A into `0.1` without an adapter. Extend `shared-contracts` or version the schema explicitly.

## Development order (from handoff — do not reorder)

1. Scaffold ✓ (repo)
2. JSON loader
3. Validator (PRECHECK before CP-SAT)
4. Diagnostic report contract (match `AGENT_START_PROMPT` fields)
5. Rule registry from `rules[]` / `rule_registry.csv`
6. **Solve synthetic-small first**
7. Validate real JSON only (no full solve required)
8. Real solve + compare **variant A vs B** only after synthetic is stable

## Variant A vs B (unresolved branch)

| Variant | Teachers |
|---------|----------|
| A | `Бакирова Л.Л.` and `Валиева Л.Л.` **separate** (54) |
| B | `Валиева Л.Л.` → `Бакирова Л.Л.` (53) |

Always run both for diagnostics; never pick one silently. Compare: objective, unscheduled, virtual teachers/rooms, relaxed-hard violations, soft penalties, room-owner conflicts.

## Constraint classes (solver code)

- **MODEL_HARD** — few physical invariants (non-overlap, room capacity, active weeks, Saturday 1–4)
- **RELAXED_HARD** — business rules via violation vars + large penalties (unscheduled, virtual resources, availability)
- **SOFT_*** — preferences (gaps, language parallel, subject once per day = **SOFT_WEAK**)

Do **not** implement most business rules as true CP-SAT hard constraints. Do **not** hide violations.

## Accepted data facts (treat as input truth unless user overrides)

- `academic_hours/week = total_hours / regular_load_weeks`; `pairs/week = hours/2`
- ТДК-609/610/611: **17** regular-load weeks
- Merged aliases (both variants): Запланова→Заплавнова, Бикмулина→Бикмуллина, Шавалиева А.Ф.→А.Ш., Федонин→Федонин Н.А.
- v1.1: zero-hour language rows → parallel subgroup demands inheriting primary language load

## Known limitations (do not “fix” in solver)

- Room `capacity` often null
- Aggregated `lesson_demands`, not full weekly expansion
- Language bundles heuristic on `(1)/(2)` in subject names
- Labs/practice/tech-lab rules partly **UNRESOLVED** in registry

## Quick commands

```powershell
# Контракты (всегда)
npm run validate:contracts

# Solver CLI — после появления .NET проекта в apps/solver
# dotnet run --project apps/solver -- --input ... --output tmp/out.json --mode diagnostic
```

## Red flags — stop and re-read handoff

- Opening `.xlsx` from `00_original_xlsx` inside the solver
- Treating candidate JSON as signed-off production data
- Skipping synthetic-small to “save time” on real 18k-line JSON
- Single variant only for Бакирова/Валиева
- Following `DATA_READINESS_STATUS` “no final JSON” while ignoring `02_canonical_solver_input_v1_1`
- Mapping handoff `rules[]` into Prisma or web DB models
- Expecting `INFEASIBLE` with no diagnostic payload

## Rationalizations

| Excuse | Reality |
|--------|---------|
| “Handoff is the source of truth” | It is **candidate final-test** with warnings |
| “I’ll parse XLSX once in solver” | Boundary: web/audit normalizes; solver reads JSON only |
| “Real data first proves progress” | Unstable without validator + synthetic + rule registry |
| “A and B are the same except names” | Teacher count and feasibility differ — compare diagnostics |
| “All rules should be hard” | Handoff explicitly diagnostic-first with RELAXED_HARD |
| “I'll copy the whole zip into git” | Large + XLSX; keep local or LFS; commit contracts + samples only |

## Link to repo docs

- `docs/architecture.md` — web / solver / contracts boundary
- `docs/data-boundary.md` — no XLSX in solver
- `docs/solver-contract.md` — CLI stub today
- `packages/shared-contracts/` — evolve toward `real_candidate_v1_1`
