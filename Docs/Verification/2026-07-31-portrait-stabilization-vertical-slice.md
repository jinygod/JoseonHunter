# Portrait stabilization vertical-slice performance evidence

## Environment and method

- **Environment:** Unity `6000.5.5f1`, Windows Editor batchmode (`-nographics`), PlayMode test runner; this is Editor/headless evidence, not Android/device evidence.
- **Scenario:** `FirstPlayableLoadPlayModeTests` loads Gameplay, disables automatic enemy/chest timers through the Task 8 seam, creates exactly 30/50/100 enemies at the same point, warms 30 rendered frames, then samples 120 rendered frames.
- **Metrics:** `Main Thread` recorder (nanoseconds, sorted in a reusable 120-entry array for median/p95), `GC Allocated In Frame` maximum, living enemy count and pairwise minimum spacing. The test restores random state, GameFlow, time scale, and all recorder resources in `finally`.
- **Contract:** each tier's headless p95 must be at most 33.34 ms (two 60 Hz frame budgets). This is an explicit non-flaky guard against an unbounded tier increase in the headless harness, not a device-frame-budget claim. After warmup, 120 direct `UpdateEnemies` calls allocate 0 managed bytes on the current thread; this isolates the movement/grid path from Editor/UI/test-harness frame allocations.

## BEFORE markers (fresh RED recorder run)

| Tier | Active | Warmup / samples | Median ms | p95 ms | Max GC/frame | Min spacing | Marker recorder |
| --- | ---: | --- | ---: | ---: | ---: | ---: | --- |
| 30 | 30 | 30 / 120 | 16.663 | 16.686 | 171,471 B | 0.0084 | `Enemy.Move` unavailable (expected RED) |
| 50 | 50 | 30 / 120 | 16.662 | 16.700 | 145,686 B | 0.0006 | `Enemy.Move` unavailable (expected RED) |
| 100 | 100 | 30 / 120 | 16.663 | 16.685 | 165,077 B | 0.0000 | `Enemy.Move` unavailable (expected RED) |

## AFTER markers (fresh GREEN recorder run)

| Tier | Active | Warmup / samples | Median ms | p95 ms | Max GC/frame | Min spacing | Movement steady GC |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 30 | 30 | 30 / 120 | 16.663 | 16.687 | 168,571 B | 0.0066 | 0 B / 120 direct movement ticks |
| 50 | 50 | 30 / 120 | 16.662 | 16.683 | 146,318 B | 0.0005 | 0 B / 120 direct movement ticks |
| 100 | 100 | 30 / 120 | 16.661 | 16.686 | 150,843 B | 0.0000 | 0 B / 120 direct movement ticks |

All eight marker recorders were valid in every AFTER tier: `JoseonHunter.Run.Update`, `.Enemy.Grid`, `.Enemy.Move`, `.Spawn`, `.Weapon`, `.Pickup`, `.UI.Hud`, and `.UI.Modal`. Their recorder buffers each reported one sample; the HUD/modal sample values may be zero in this non-interactive load scenario because no HUD refresh or modal callback necessarily occurs in its final sampled frame.

`GC Allocated In Frame` is intentionally reported as a whole-frame Editor/harness metric and remains nonzero. It must not be interpreted as enemy movement allocation: the direct warmed movement measurement above is the allocation proof for that path.

## Task 10 decision-gate inputs only

| Gate input at 100 enemies | Evidence | Task 10 decision |
| --- | --- | --- |
| Instantiate/Destroy p95 > 1.0 ms | Not measurable in this steady-state scenario; no marker was added for lifecycle calls. | Not decided here. |
| Steady lifecycle GC > 512 B/frame | Not isolated; whole-frame Editor GC is 150,843 B max and is not lifecycle attribution. | Not decided here. |
| Visible spawn burst misses 16.67 ms from lifecycle work | Not exercised by this steady-state headless scenario. | Not decided here. |

No pooling was implemented or selected in Task 9. Device validation and the targeted lifecycle/burst capture remain required before Task 10 evaluates its gate.
