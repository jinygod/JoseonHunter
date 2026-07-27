# Task 11 — Bomb and Frost Field Weapons

## Implemented

- `ThunderBombExecutor`: deterministic crowd-center lob, fuse, pixel-confirmed expanding blast ring, globally allocated attack IDs, retirement, and level-five secondary shockwave.
- `FrostFlaskExecutor`: deterministic lob into a bounded persistent field, mask-gated slow/status handling, timed damage, residence freeze, exit decay, oldest-first capacity expiry, and level-five masked ice spikes.
- Added deterministic EditMode coverage for delayed bomb-ring damage and frost slow/tick/freeze/exit/capacity behavior.
- Review follow-up: frost effects are keyed by field attack ID, so one field's exit/eviction only clears its own slow source; prototype enemies aggregate the strongest active slow and timed freezes. Ring expansion now sweeps intermediate radii in bounded samples and carries excess work to later ticks.
- Reset follow-up: every active frost field releases its own target slow source before its attack is retired and state is cleared.

## Validation

- Scoped static inspection completed.
- `git diff --check` found only pre-existing unrelated trailing whitespace in dirty sprite metadata; task files have no reported whitespace errors.
- Unity and broad test runs were intentionally not run for this task.
