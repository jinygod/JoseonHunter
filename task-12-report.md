# Task 12 — Jangseung Ward boundary weapon

## Implemented

- Added finite, deterministic cardinal ward post sets with closed, finite boundary segments.
- Each target keeps a prior position inside the executor. A movement segment must intersect a ward segment before a finite, authored-PPU resampled pixel mask confirms contact and central combat damage is requested.
- Crossing contact is direction-aware for knockback. Targets cannot repeatedly trigger while touching; they must leave the boundary before a later crossing can be considered, and `BoundaryReentry` receives the movement-interpolated real crossing time rather than simulation ticks or frame-end time.
- Ward-set capacity evicts the oldest set, retires its global attack ID, and removes ward status sources. Reset performs the same cleanup.
- Level five creates four cardinal posts and repositions them only on the bounded mobile interval/step.

## Focused verification

- Added EditMode coverage for same-side movement, real-time crossing/re-entry behavior including large frames, oldest-set eviction, level-five four-post formation, and centered-pivot PPU32 transparent, endpoint, and 45-degree mask geometry.
- Ran `git diff --check` and reviewed only the Task 12 diff. Unity and repository-wide tests were intentionally not run.

## Final Integrated Fix Round

- Fixed the capture-session `EditorPrefs` leak that could resume a stale portrait capture in a different Unity batch process and call `Camera.Render` under `-nographics`. Pending capture state now records the originating OS PID; foreign or missing owners clear every capture key without resuming capture.
- Focused capture-policy EditMode verification passed 2/2. The full EditMode suite passed 529/529 at `2026-07-31 20:24:27Z`–`20:24:47Z` (19.974525 s).
- The stale explicit-confirmation test regression was fixed: the Hwando vertical slice now waits for `IsAwaitingConfirmation`, calls `Confirm()`, and waits for completion before reading the result. Its earlier `Skip()`-then-`LastCompletedResult` NRE no longer occurs; execution now reaches its later unresolved Venom Fang committed-mask assertion.
- Final PlayMode, retained at `Logs/playmode-results.xml`, failed 79/261: 38 `WeaponPotentialCombatAPlayModeTests`, 39 `WeaponPotentialCombatBPlayModeTests`, Moon Eclipse, and the vertical-slice Venom Fang mask assertion. They remain release limitations. No retained pre-branch full baseline or branch mask-topology/executor evidence establishes their origin as introduced or pre-existing.
