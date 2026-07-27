# Task 11 — Bomb and Frost Field Weapons

## Implemented

- `ThunderBombExecutor`: deterministic crowd-center lob, fuse, pixel-confirmed expanding blast ring, globally allocated attack IDs, retirement, and level-five secondary shockwave.
- `FrostFlaskExecutor`: deterministic lob into a bounded persistent field, mask-gated slow/status handling, timed damage, residence freeze, exit decay, oldest-first capacity expiry, and level-five masked ice spikes.
- Added deterministic EditMode coverage for delayed bomb-ring damage and frost slow/tick/freeze/exit/capacity behavior.

## Validation

- Scoped static inspection completed.
- `git diff --check` found only pre-existing unrelated trailing whitespace in dirty sprite metadata; task files have no reported whitespace errors.
- Unity and broad test runs were intentionally not run for this task.
