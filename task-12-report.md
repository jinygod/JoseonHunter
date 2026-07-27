# Task 12 — Jangseung Ward boundary weapon

## Implemented

- Added finite, deterministic cardinal ward post sets with closed, finite boundary segments.
- Each target keeps a prior position inside the executor. A movement segment must intersect a ward segment before a finite, authored-PPU resampled pixel mask confirms contact and central combat damage is requested.
- Crossing contact is direction-aware for knockback. Targets cannot repeatedly trigger while touching; they must leave the boundary before a later crossing can be considered, and `BoundaryReentry` receives the executor's real elapsed seconds rather than simulation ticks.
- Ward-set capacity evicts the oldest set, retires its global attack ID, and removes ward status sources. Reset performs the same cleanup.
- Level five creates four cardinal posts and repositions them only on the bounded mobile interval/step.

## Focused verification

- Added EditMode coverage for same-side movement, real-time crossing/re-entry behavior, oldest-set eviction, level-five four-post formation, and centered-pivot PPU32 transparent-mask rejection.
- Ran `git diff --check` and reviewed only the Task 12 diff. Unity and repository-wide tests were intentionally not run.
