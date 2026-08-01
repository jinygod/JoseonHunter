# Infinite Battlefield, Authored Waves, and Bootstrap Loading Verification

## Status

**Ready with limitations** for the requested Unity vertical-slice changes.

The changed runtime, scene, prefab, and Android build surfaces passed their required automated and build checks. A physical Android device was not available, and the project-wide PlayMode suite still has the previously recorded deferred weapon-potential failures outside this change surface.

## Scope and baseline

- Project: `D:\UnityProjects\JoseonHunter`
- Unity: `6000.5.5f1` (`d16e074b49fd`)
- Render pipeline: URP `17.5.0`, 2D renderer
- Input: Input System `1.20.0`
- Code baseline before this implementation: `80d2033` after the first three approved tasks; final implementation code before this verification document: `340dd8c`
- Target: Android development APK, portrait, ARM64, IL2CPP, min SDK 26, target SDK 36
- Enabled build scenes: Bootstrap, Lobby, Gameplay
- Preserved user-owned local changes: `Assets/JoseonHunter/Scenes/Gameplay.unity`, `ProjectSettings/ProjectSettings.asset`, and the two MaruBuri dynamic TMP assets

## Implemented behavior

### Infinite battlefield

- Nine reusable 32-by-32 world-unit chunks follow the player in a deterministic 3-by-3 neighborhood.
- Chunk coordinate conversion handles negative world positions using floor semantics.
- Returning to a coordinate reconstructs the same bounded decoration signature.
- Ordinary movement inside one chunk does not rebuild views or create GameObjects.
- The new opaque Joseon folk-field ground uses point filtering, no mipmaps, and uncompressed import settings.

### Authored waves and density

- 0-45 seconds: plague rats only, active cap 72.
- 45-90 seconds: rats plus vengeful spirits and bounded spirit packs, active cap 104.
- 90-135 seconds: rats, spirits, and sakkat specters with alternating packs, active cap 128.
- 135-165 seconds: specters, dokkaebi, and bandits with larger packs, active cap 140.
- Boss warning and boss phases stop normal pack creation.
- Continuous spawns and packs share the same live-combatant budget; treasure chests are excluded from that count.
- Normal content IDs map explicitly to the five serialized sprites, with a plague-rat fallback and one development warning per unknown ID.
- Spawn geometry derives from `Camera.ViewportToWorldPoint`; complete renderer bounds begin outside the actual viewport.

### Loading transition

- Bootstrap contains an inspectable loading prefab rather than a placeholder root.
- The loader persists through `LoadSceneAsync`, uses `operation.progress / 0.9`, shows no fake percentage, and remains opaque until Gameplay publishes readiness.
- A minimum 0.35-second presentation prevents flashing; player builds wait for `WaitForEndOfFrame`, and batch tests use a headless-safe next-frame fallback.
- The fade uses unscaled time, and direct Gameplay entry remains functional.
- Failure to start or complete loading keeps the opaque screen and displays a concise Korean retry message.

## Automated evidence

### EditMode

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter "JoseonHunter.Tests.EditMode"
```

Result: **559 passed, 0 failed, 0 skipped**, duration 31.053 seconds.

Artifact: `Logs/editmode-results.xml`; log: `Logs/editmode.log`.

Covered domain boundaries, wave weights/caps, deterministic pack timing, chunk coordinates and seeds, pixel import settings, scene scaffolding, Bootstrap presenter wiring, and the existing EditMode regression surface.

### Integrated PlayMode

Filter:

```text
BattlefieldTilePresenterPlayModeTests;
WaveRosterPlayModeTests;
StagePacingPlayModeTests;
FirstPlayableLoadPlayModeTests;
BootstrapLoadingPlayModeTests;
EightWeaponCombatPlayModeTests;
CombatPerformanceInvestigationPlayModeTests;
FirstPlayablePresentationPlayModeTests
```

Result: **39 passed, 0 failed, 0 skipped**, duration 24.857 seconds.

Artifact: `Logs/playmode-results.xml`; log: `Logs/playmode.log`.

The integrated run exercised nine-chunk recycling, deterministic return, camera coverage after large travel, first-wave rat purity, later-wave mixtures and packs, the 140-enemy ceiling, exact viewport clearance, Bootstrap-to-Gameplay async lifecycle, direct Gameplay entry, existing weapon behavior, and load/performance instrumentation.

An initial integrated run exposed a test-order timing race: the active scene could change before the loader coroutine recorded progress and readiness. The test now waits independently for active scene, progress, readiness, and removal. The final integrated run passed 39/39.

## Performance evidence

Headless Editor measurements are regression evidence, not physical-device performance claims.

| Workload | Median frame | p95 frame | Movement allocation | Enemy move marker |
|---|---:|---:|---:|---:|
| 100 active enemies | 16.664 ms | 16.683 ms | 0 B across 120 manual ticks | 0.615 ms last sample |
| 140 active enemies | 16.663 ms | 16.690 ms | 0 B across 120 manual ticks | 0.806 ms last sample |

- Eight level-five weapons against 100 targets: 3.2237 ms average per 0.05-second direct simulation tick, 0 B managed allocation.
- 100-to-134 burst creation: 3.9233 ms.
- Chunk tracking inside one coordinate is test-covered for no rebuild and no GameObject churn.

## Visual evidence

- Folk-field Gameplay captures inspected at:
  - `Artifacts/PortraitValidation/720x1280/01-gameplay.png`
  - `Artifacts/PortraitValidation/1080x2340/01-gameplay.png`
- Bootstrap loading captures inspected at:
  - `Artifacts/BootstrapLoading/720x1280.png`
  - `Artifacts/BootstrapLoading/1080x2340.png`

The field captures had no black camera edge, alpha gap, or obvious chunk seam. Player and enemy silhouettes remained readable over the low-contrast field. The first loading capture revealed a square sprite-less halo; the prefab was corrected to reuse the spirit-flame sprite, recaptured, and inspected at both resolutions. The final captures are fully opaque, uncropped, and use the intended Gungseo-style title and MaruBuri subtitle.

## Android build evidence

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1
```

Final result: `Build Finished, Result: Success.`

- Artifact: `Builds/Android/JoseonHunter-development.apk`
- Size: 172,227,945 bytes
- SHA-256: `D5D87DC397281BEF098FB4145E511315C711EEE01478FC6FC7FBA31DAEC1157C`
- Configuration: Development, Connect With Profiler, Allow Debugging, ARM64, IL2CPP
- Build log: `Logs/android-development-build.log`

Unity touched three URP/volume assets as build-time serialization side effects. They were clean before validation and were restored after each build. No package or rendering setting change was committed.

## Working tree and regression classification

The implementation and documentation are committed independently from local user state. The following files were intentionally not staged:

- `Assets/JoseonHunter/Scenes/Gameplay.unity`
- `ProjectSettings/ProjectSettings.asset`
- `Assets/JoseonHunter/Resources/Fonts/MaruBuri-Regular-Dynamic SDF.asset`
- `Assets/JoseonHunter/Resources/Fonts/MaruBuri-SemiBold-Dynamic SDF.asset`
- untracked `.utmp/`

The manually authored C# and documentation pass whitespace inspection. Empty serialized fields in the new Unity UI prefab/meta files were normalized before handoff. Global `git diff --check` still reports one trailing-space line in the preserved local Gameplay scene (`m_Name:` at line 216); that user-owned scene was not rewritten to remove it.

The complete PlayMode suite was not used as the acceptance gate because the previous verified baseline already records 79 deferred weapon-potential combat failures outside this scope. The changed and adjacent runtime surface is covered by the 39-test integrated filter above; no new failure remained in that filter.

## Acceptance matrix

| Criterion | Status | Evidence |
|---|---|---|
| No finite-map black edge during travel | Passed | Nine-chunk PlayMode tests and portrait visual captures |
| Deterministic, bounded infinite field | Passed | EditMode coordinate/seed tests and PlayMode recycling tests |
| Cute low-contrast Joseon folk ground | Passed | Import contract and two inspected Gameplay captures |
| First wave contains only rats | Passed | WaveRoster PlayMode test |
| Later waves introduce recognizable mixtures and packs | Passed | Domain director tests and WaveRoster PlayMode tests |
| Shared active ceiling of 140 | Passed | Peak simulation and 140-enemy measurement |
| Enemies begin outside the real viewport | Passed | All-side/rank viewport clearance tests |
| Real opaque Bootstrap loading flow | Passed | Scene test, async PlayMode tests, and two inspected loading captures |
| Gameplay direct-entry workflow remains available | Passed | Direct Gameplay PlayMode test |
| Android player build succeeds | Passed | Final APK and build log |
| Physical Android frame pacing, GPU, memory, and thermal behavior | Not run | No attached Android device |
| Project-wide PlayMode suite is green | Not met, pre-existing | Previous baseline records 79 deferred weapon-potential failures |

## Recommended next action

Install the development APK on the target Android phone and profile a real 140-enemy peak for CPU/GPU frame time, memory, thermal behavior, and touch/safe-area handling. Treat that as device validation rather than changing the authored 140 cap from Editor-only evidence.
