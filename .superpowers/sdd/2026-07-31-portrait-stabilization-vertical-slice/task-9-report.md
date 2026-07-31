# Task 9 report — profiler instrumentation and load evidence

## RED / GREEN

- RED recorder: the 30/50/100 PlayMode load runs failed exactly because `JoseonHunter.Enemy.Move` did not yet exist; fresh XML supplied the BEFORE rows in the verification document.
- RED marker names: the focused EditMode run compiled with `FirstPlayableProfilerMarkers` deliberately absent and failed with the expected missing-marker symbol errors.
- GREEN marker names: `FirstPlayableProfilerMarkerTests` passed 1/1.
- GREEN load: `FirstPlayableLoadPlayModeTests` passed 7/7 with 30 warmup frames and 120 samples at every tier; all eight recorder names were valid and the direct warmed movement path allocated 0 bytes.

## Implementation

- Centralized the eight `Unity.Profiling.ProfilerMarker` constants in `FirstPlayableProfilerMarkers`.
- Added scopes without call reordering: run update; enemy grid snapshot/rebuild; enemy resolution/movement; spawn; weapon; pickup; HUD render/rack refresh; modal presentation callbacks.
- Load measurement restores Random, flow, time scale, and recorders in `finally`, uses reusable frame-duration storage, reports exact recorder availability/sample values, and records Task 10 inputs without making a pooling decision.

## Evidence

See `Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md`. This is Editor/headless evidence only; the whole-frame GC recorder is explicitly not attributed to enemy movement.

## Final regression evidence

- Fresh final-HEAD `EnemySeparationGridTests`: **8/8** passed.
- Fresh final-HEAD representative PlayMode filter (`StagePacing`, `PortraitUiLayout`, `CombatHud`, `FirstPlayableLoad`): **21/21** passed.
- Fresh final-HEAD full EditMode: **522/522** passed.
- Fresh XML/log inspection found no unexpected compilation, profiler, RenderTexture, or UI exceptions. Unity-generated changes to eight sprite `.meta` files were restored exactly to HEAD after validation; Unity was not run again after restoration.

## Review fix round 1

- `Enemy.Grid` now contains only snapshot/rebuild work; status ticks are attributed to `Enemy.Move` before the grid pass and movement resolution remains in its own `Enemy.Move` scope.
- Milestone burst spawning is covered by the existing Spawn marker inside the production `SpawnBurst` path. Focused PlayMode recorder coverage proves nonzero buffered Spawn, HUD, and Modal samples while exercising a first surge, HUD refresh, upgrade open, disable cleanup, and destroy cleanup.
- Load evidence now asserts GC recorder validity and requested active count. Its focused lifecycle evidence scenario recorded 100-enemy spawn p95 **0.1056 ms**, cleanup-entry p95 **0.0614 ms**, steady movement/lifecycle allocation **0 B/frame**, and actual production `SpawnBurst(34)` **0.2566 ms**. All Task 10 gates are mechanically evaluable and No; no pooling was introduced.
