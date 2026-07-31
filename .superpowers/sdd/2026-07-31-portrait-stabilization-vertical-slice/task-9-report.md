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
