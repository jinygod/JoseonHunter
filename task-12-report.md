# Task 12 — Jangseung Ward boundary weapon

## Implemented

- Added finite, deterministic cardinal ward post sets with closed, finite boundary segments.
- Each target keeps a prior position inside the executor. A movement segment must intersect a ward segment before the ward pixel mask confirms contact and central combat damage is requested.
- Crossing contact is direction-aware for knockback. Targets cannot repeatedly trigger while touching; they must leave the boundary before a later crossing can be considered, and `BoundaryReentry` enforces its configured interval.
- Ward-set capacity evicts the oldest set, retires its global attack ID, and removes ward status sources. Reset performs the same cleanup.
- Level five creates four cardinal posts and repositions them only on the bounded mobile interval/step.

## Focused verification

- Added EditMode coverage for same-side movement, crossing/re-entry behavior, oldest-set eviction, and level-five four-post formation.
- Ran `git diff --check` and reviewed only the Task 12 diff. Unity and repository-wide tests were intentionally not run.
