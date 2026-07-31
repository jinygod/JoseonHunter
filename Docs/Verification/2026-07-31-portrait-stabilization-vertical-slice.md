# Portrait stabilization vertical-slice performance evidence

## Environment and method

- **Environment:** Unity `6000.5.5f1`, Windows Editor batchmode (`-nographics`), PlayMode test runner; this is Editor/headless evidence, not Android/device evidence.
- **Provenance:** final evidence HEAD `30e299d993370220ad7c8f3c44d70c6a7ef66a61`; raw Unity XML is refreshed at `Logs/playmode-results.xml` by the commands below (the working artifact is intentionally not source-controlled).
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

## Task 10 decision-gate inputs

| Gate input at 100 enemies | Evidence | Task 10 decision |
| --- | --- | --- |
| Instantiate/Destroy p95 > 1.0 ms | 24 high-resolution Stopwatch samples through existing `SpawnEnemy` = **0.1522 ms p95**; existing `ApplyEnemyDamage` synchronous cleanup-entry/Destroy-scheduling path = **0.0746 ms p95**. | No — neither exceeds 1.0 ms. |
| Steady lifecycle GC > 512 B/frame | 120 direct warmed existing `UpdateEnemies` calls = **0 B/frame** current-thread managed allocation. | No — 0 B is not greater than 512 B. |
| Visible spawn burst misses 16.67 ms from lifecycle work | Existing production `SpawnBurst(34)` from final-surge pacing = **5.8618 ms**, with active count asserted **100 → 134** and a nonzero Spawn recorder buffer. | No — below 16.67 ms. |

The lifecycle measurement test is `FirstPlayableLoadPlayModeTests.LifecycleEvidenceMeasuresExistingSpawnCleanupAndBurstAtOneHundredEnemyTier`, run at 2026-07-31 17:09:08Z. It disables automatic spawn/chest timers, captures and restores the controller elapsed/run state in addition to Random, flow, time scale, and recorders, sets `elapsed` through the authored final-surge pacing conversion (without crossing a milestone), seeds 100 enemies through the established load seam, then calls the existing private production `SpawnBurst(34)` via its test seam. The test mechanically asserts the measured real 100→134 synchronous SpawnBurst(34) duration is `< 16.67 ms`. Stopwatch attribution is deliberately narrow to existing synchronous production calls; delayed visual death destruction and Editor/headless rendering are not device evidence.

All three mechanical Task 10 gates are **No** in this captured Editor/headless scenario. No pooling was implemented or selected in Task 9; a future Task 10 decision can consume these rows directly.

## Task 10 outcome

| Outcome | Decision | Evidence provenance |
| --- | --- | --- |
| Pooling rejected: thresholds not crossed | Keep the current vertical-slice build unpooled; do not create `FirstPlayableObjectPool`, alter `FirstPlayableController`, or add pool tests. | Final Task 9 evidence HEAD `30e299d993370220ad7c8f3c44d70c6a7ef66a61`; `FirstPlayableLoadPlayModeTests.LifecycleEvidenceMeasuresExistingSpawnCleanupAndBurstAtOneHundredEnemyTier` at 2026-07-31 17:09:08Z. |

- `SpawnEnemy` p95 is **0.1522 ms** and `ApplyEnemyDamage` synchronous cleanup-entry/Destroy-scheduling p95 is **0.0746 ms**; both are at or below the **1.0 ms** threshold.
- Steady lifecycle/movement current-thread GC is **0 B/frame**, at or below the **512 B/frame** threshold.
- The production final-surge `SpawnBurst(34)` takes **5.8618 ms**, with active enemies asserted **100 → 134**; this is below the **16.67 ms** frame budget.

This decision is limited to the captured Windows Editor batchmode/headless scenario. Deferred rendered `Destroy` work and Android/device performance remain unvalidated; future equivalent device evidence that crosses a gate can reopen the pooling decision. Until then, this vertical-slice build remains unpooled.

## Task 11 motion and flying-blade asset audit

| Controller-consumed base sprites | Result |
| --- | --- |
| Han Yeonhwa, Plague Rat, Bandit, Dokkaebi, Sakkat Specter, Vengeful Spirit, Dokkaebi Captain, Fallen General | All 8 resolve in the checked-in `CombatMotionLibrary.asset`; every idle/move frame is non-null, Point-filtered, and exactly 64 PPU. Han Yeonhwa is exactly 4 idle / 8 move frames. |

- The audit regression opens `Gameplay.unity` and reads the actual `FirstPlayableController` serialized fields; it does not construct a duplicate library fixture.
- Flying-blade production-path coverage records a visible active outbound blade, resolved projectile scale and frame sprite, visible contact transient, inbound blade visibility, one outbound/inbound contact for the single target, plus level-five two-blade staggering and all three blades returned to the pool. No damage, cooldown, range, targeting, or evolution values were changed.
- PixelLab result: **no generation**. Existing imported frames and weapon presentation assets resolve every audited named gap; no external request was attempted and cost is **0** (ledger balance: 1,512).

### Runtime capture evidence (1080x1920)

The existing deterministic Gameplay weapon capture was run through `EightWeaponPolishCapture.CaptureHwandoPortraitInBatchMode`. It renders the real Gameplay scene with Han Yeonhwa, durable stationary enemies, and the active Hwando visual phase. The generated images are ignored under `Artifacts/WeaponPolish/`.

| Capture | Dimensions | SHA-256 | Finding |
| --- | --- | --- | --- |
| `hwando_flying_blade-level-1.png` | 1080x1920 | `3e86f5ef8c102be835b5ad232d75e630a5fb0b0b23d6ba6b9d5aa092fa8d1e4e` | Bound player/enemy sprites and level-one outbound/contact phase visible. |
| `hwando_flying_blade-level-3.png` | 1080x1920 | `cfde197c12af83c5ee8cef2f192bc8e4490b7a8661975ac321f462f0fce62556` | Bound sprites and continuing phase cadence visible. |
| `hwando_flying_blade-level-5.png` | 1080x1920 | `7c0c89f013cf72e508c428e9a3d7a441433c3b8efc665d935be57be64e26df86` | Level-five staggered flying-blade presentation visible. |
| `hwando_flying_blade-evolved.png` | 1080x1920 | `fe00840f13513bab704ba3318c85659059e94b1ddf4e4eda7a41d538f9e4c56f` | Evolved Hwando presentation visible. |
