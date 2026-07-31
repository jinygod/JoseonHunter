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
- Flying-blade production-path coverage records a visible active outbound blade, resolved projectile scale and frame sprite, visible contact transient, inbound blade visibility, one outbound/inbound contact for the single target, plus a registered runtime level-five volley with three visible blade renderers, stagger progression, three outbound/inbound contacts, and all three blades returned to the pool. No damage, cooldown, range, targeting, or evolution values were changed.
- PixelLab result: **no generation**. Existing imported frames and weapon presentation assets resolve every audited named gap; no external request was attempted and cost is **0** (ledger balance: 1,512).

### Runtime capture evidence (1080x1920)

The existing deterministic Gameplay weapon capture was run through `EightWeaponPolishCapture.CaptureHwandoPortraitInBatchMode`. It renders the real Gameplay scene with Han Yeonhwa, durable stationary enemies, and the active Hwando visual phase. The generated images are ignored under `Artifacts/WeaponPolish/`.

| Capture | Dimensions | SHA-256 | Finding |
| --- | --- | --- | --- |
| `hwando_flying_blade-level-1.png` | 1080x1920 | `9c2e760cfc862352f52c792f469722ead3ce1e2a6ba1a3f8472fd32487677daa` | Bound player/enemy sprites, active blade, visible damage/contact cue; capture gate requires an active sprite-bearing `Weapon Transient Visual`, not a blade/afterimage. |
| `hwando_flying_blade-level-3.png` | 1080x1920 | `0f8f7668a7ef17da0818028ce512898f0dc6eab02f0187edeae06fe65cef91a4` | Bound sprites and continuing phase cadence visible. |
| `hwando_flying_blade-level-5.png` | 1080x1920 | `940fe02fb55ea1876278d7be2860ef2a39f30ba7058a75257d3793444114bf15` | Level-five staggered flying-blade presentation visible. |
| `hwando_flying_blade-evolved.png` | 1080x1920 | `4e6d6916db16976007afcc7e2d92a78458cd7d5acff2aa1c2dc7b03ba1441f00` | Evolved Hwando presentation visible. |

## Task 12 release-validation result (2026-08-01)

### Baseline and configuration

- Baseline HEAD and upstream: `71a73fcc50efe7267dbd6da3ed6d998c9f340218`; the worktree was clean before Task 12.
- Unity `6000.5.5f1` (`d16e074b49fd`); direct packages include URP `17.5.0`, Input System `1.20.0`, Test Framework `1.7.0`, and uGUI `2.5.0`.
- Enabled scenes: `Assets/JoseonHunter/Scenes/Bootstrap.unity`, `Lobby.unity`, `Gameplay.unity`.
- Observed Android settings before build: package `com.jinygod.joseonhunter`, version `0.1.0`/code `1`, min SDK 26, target SDK 36, ARM64 (`AndroidTargetArchitectures: 2`), IL2CPP (`scriptingBackend.Android: 1`). URP 2D is active. `m_BuildTargetGraphicsAPIs` is empty, so no graphics API was claimed.
- `adb devices -l` returned only `List of devices attached`; **Android device capture unavailable**.

### Automated validation

| Validation | Command/result | Status | Classification |
| --- | --- | --- | --- |
| Focused build-contract coverage | No narrow unit test added: this static Editor/BuildPipeline integration contract is exercised by the full Editor compilation and Android invocation below. | NA | BuildPipeline is not isolated without replacing the real build call. |
| Full EditMode | `Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode`; 529 total, 529 passed, 0 failed, 0 skipped; XML start `2026-07-31 19:53:56Z`, end `2026-07-31 19:54:21Z`, duration 24.8247754 s; runner wall time 69 s. | Passed | Fresh retained `Logs/editmode-results.xml`; no first-party compile errors found in `Logs/editmode.log`. |
| Full PlayMode | `Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode`; 261 total, 182 passed, 79 failed, 0 skipped; XML duration 70.2205603 s; command duration 102.3 s. | Failed | 77 failures are `WeaponPotentialCombatAPlayModeTests` (38) and `WeaponPotentialCombatBPlayModeTests` (39); the other two are `EvolvedWeaponCombatPlayModeTests.Moon_eclipse_keeps_outbound_and_return_contact_then_blasts_at_crossing` and `WeaponAffixVerticalSlicePlayModeTests.Perfect_hwando_jackpot_flows_from_pointer_choice_to_evolution_and_run_reset`. They are unrelated to the new Editor build class/wrapper and were not suppressed or changed. |

### Android build

`Tools/Unity/Build-AndroidDevelopment.ps1` invokes Unity with `Start-Process -Wait -PassThru -WindowStyle Hidden`; `AndroidDevelopmentBuild.Build` applies `PortraitAndroidReleaseSettings`, selects only enabled scenes, and requests `Development | ConnectWithProfiler | AllowDebugging` to output `Builds/Android/JoseonHunter-development.apk`.

The first Android invocation stalled at Bee backend4 and was terminated only after its self-started process tree stopped advancing for roughly 15 minutes; its ignored log was preserved. The second invocation reached Gradle and exposed a non-ASCII `C:\Users\전성진\.gradle` Prefab-command encoding failure. The wrapper was then changed to use an ASCII Gradle cache; a project-local cache proved too long for Ninja's 260-character limit, so the final wrapper uses the short ASCII cache `C:\jh-gradle` and restores the caller environment in `finally`. The final post-root-cause invocation succeeded: Unity reported `Build Finished, Result: Success`, build duration 114.148 s (postprocess 105.361 s), and wrapper exit code was 0. Unity-generated serialized changes were restored after that final invocation.

| Artifact | Result | Status |
| --- | --- | --- |
| `Builds/Android/JoseonHunter-development.apk` | 101,971,466 bytes; SHA-256 `86a4a922b05e17e6ffaa5774cde009443070437f1b50ef5b62b159e08efe586a`. | Passed |
| Android installation/run, device model/OS/resolution, 30/50/100 enemy frame metrics and GC | Android device capture unavailable. | Blocked |

### Required portrait captures

`JoseonHunter.Editor.Scenes.PortraitStateValidationCapture.CaptureInBatchMode` was run in batchmode **without `-nographics`** because its synchronous `Camera.Render` evidence path requires a graphics device. It persists the session through play-mode domain reload, gates each real controller/presenter state, waits an Editor update after every transition, and reversibly routes the production overlay canvas through the gameplay camera only while rendering. The final run exited 0 after `EnteredEditMode` and wrote exactly 20 non-empty PNGs under ignored `Artifacts/PortraitValidation/`.

Safe-area emulation was the full screen rect (`0,0,width,height`) through `FirstPlayableUiBootstrap.ApplySafeArea`. Reviewer visual inspection found the real HUD, level-up modal, appraisal sheet, and resumed HUD at every size with **0 visible clipped elements**; the appraisal sheet's white option-card art is existing content, not a substitute overlay.

| Resolution | `01-gameplay` SHA-256 | `02-level-up` SHA-256 | `03-appraisal` SHA-256 | `04-resumed-combat` SHA-256 | Reviewer |
| --- | --- | --- | --- | --- | --- |
| 720x1280 | `422cfe43e127cd7b4bac0ccf7f9d21afe28b58ffb1be7eada1d938a8d2672167` | `bfa2f8ceb95e5e726ff3d92241669ea507ebc619e6fc6cf977dd4498e660bf4f` | `d254daefed14def41a0bf55fa5786e7d37cdf48e2aa18287617036ff93e217ab` | `bec1cbcd2d38241ae7120100099efe3703016c9d539d9db36b677f70a4a55dd1` | Pass; 0 clipped |
| 1080x1920 | `f5edf0d91bdc92f75601d5242b254fed65ace16470fcf18d5f1b578f13f4c476` | `40dbd637f331b4db6486e6f289848103b2ea9c289e91d06fb3478eead19694d1` | `0c774a6c68f1c56b780f486ca5dfeda06f1afaf4629295a729819d038105ee44` | `f5edf0d91bdc92f75601d5242b254fed65ace16470fcf18d5f1b578f13f4c476` | Pass; 0 clipped |
| 1080x2340 | `e4cdf8c12f56eaa09f42edfd06dd65f8c5906a9d07d63648df1fb98481c67c49` | `4f1898ad2826fedad53470750d2839f80de65647e0e05a912741a9e169ea25dc` | `fbb5486328f7372ee959fe01d6bfcda4c466d72d86a52271a311630102e9ef67` | `e47c6a2337fe6612e5abd1ed05c70506fe6eb5faa61c14e460091404dfdb5c47` | Pass; 0 clipped |
| 1170x2532 | `cc43ca7dfbb23473bde0302d6af910eb247497b93c23640ba183c8054e6a4fe9` | `2dcb67634755fbb21643cb53b0cc6062907414bdd6941e87389c938f398ee608` | `c6a1130ef2c933ab86944a427520e0632ba7f0ed103a56c752a68b3d52b4b4af` | `cc43ca7dfbb23473bde0302d6af910eb247497b93c23640ba183c8054e6a4fe9` | Pass; 0 clipped |
| 1440x3200 | `2164b52ac15cb745a3b932328fa19d2311a59a5ddf3b707cdf8a97fc7439ecda` | `0404bfc44e5c0b4fe7ad5f3dcc359405e8dbe73beeb67ce126bb76d741b5ab89` | `3ea5850da2ae0e608ed76448255d7249f85338b25eea9d055519894ad559bb7e` | `5561db31d58e5c9c557c8a7c3075f6b9a8468f35bb1c46a363bbf4eae2069080` | Pass; 0 clipped |

Every listed PNG IHDR exactly matches its resolution-directory name. The capture gates asserted `LevelUpSelection` plus open `UpgradeChoicePresenter`, `Paused` plus open `WeaponAffixRevealPresenter`, and `Playing` after `DismissDetails`.

### Acceptance matrix

| Criterion | Status | Evidence/limitation |
| --- | --- | --- |
| Reproducible portrait Android development build contract | Passed | New editor method and wrapper compile in 527/527 EditMode run; static contract is documented above. |
| Full automated validation is green | Failed | EditMode green; PlayMode 79 failures retained. |
| Non-empty Android APK | Passed | Development APK built successfully; size/hash recorded above. |
| Five resolutions x four genuine states, exact PNG dimensions and hashes | Passed | 20 real controller/presenter-driven PNGs, exact dimensions and SHA-256 rows above. |
| Visual review/safe area/clipping evidence | Passed | Full-screen safe-area emulation; reviewer found 0 visible clips at every required size. |
| Android device/performance evidence | Blocked | Android device capture unavailable. Editor headless Task 9/10 performance evidence remains separate and is not device evidence. |
| Java-to-Unity handoff and PixelLab ledger | Passed | `Docs/AI/UnityProjectContext.md` updated; balance 1,512 -> 1,512, accepted existing assets, rejected/generated none, cost 0. |
| Final source/diff hygiene | Passed with limitation | Unity-generated sprite/meta/rendering/settings changes were restored after the final Unity invocation; generated outputs remain ignored. |

Overall status: **Failed**. The Android artifact and required visual evidence are available, but Android device validation is unavailable and full PlayMode remains red.
