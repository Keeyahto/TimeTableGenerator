# Synthetic samples only

`synthetic-small/input.json` is fictional data safe for git.

**Curated v1_1** (git-safe, no handoff PD):

- `curated-v1_1-mini/` — rooms R29/R30, R19 cap
- `curated-v1_1-parity/` — `real_candidate_v1_1` calendar (`upper_*` / `lower_*` slots), R16 + R31
- `stress-medium-v1_1/` — ~135 demands, 68 slots (bench for memory); regenerate: `scripts/build-stress-medium-sample.ps1`

Expected profile: `lesson_demands` ≥ 120, `calendar_slots` = 68, `model_stats` present.

Do not place real college exports or handoff JSON here.
