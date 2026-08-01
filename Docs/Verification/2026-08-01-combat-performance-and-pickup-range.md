# Combat performance and pickup-range verification

## Outcome

- Unity `6000.5.5f1`, Windows Editor PlayMode and Android IL2CPP/ARM64 development build.
- Implementation commits: `fd4ebfd` and `b77a317` on `master`.
- Confirmed bottleneck: pixel-perfect projectile collision performed full mask scans for every sweep sample against every registered target, including targets that could not overlap the projectile.
- Fix: cache active-pixel bounds, reject non-overlapping mask pairs, and build a conservative swept candidate list once per projectile tick before retaining the existing exact pixel contact test.
- Weapon paths, damage, trajectories, penetration, attack order, and visuals were not simplified or redesigned.

## Before and after

The isolated weapon harness runs the real Gameplay controller for 40 ticks at 0.05 seconds with 20 durable targets. Non-Hwando rows include the normal starting Hwando plus the named level-five weapon.

| Weapon combination | Before average tick | Final average tick | Result |
| --- | ---: | ---: | --- |
| Hwando | 5.6733 ms | 0.8027 ms | Passed |
| Gakgung | 271.1035 ms | 1.6694 ms | Passed |
| Talisman | 2.0604 ms | 0.3446 ms | Passed |
| Thunder bomb | 2.0036 ms | 0.3067 ms | Passed |
| Jangseung ward | 2.3876 ms | 0.6661 ms | Passed |
| Singijeon | 60.8499 ms | 0.8888 ms | Passed |
| Frost flask | 1.9273 ms | 0.2099 ms | Passed |
| Wind-thunder fan | 2.0817 ms | 0.3201 ms | Passed |

The combined eight-level-five-weapon/100-target test improved from 17.5279 ms after the first broad-phase pass to 3.3120 ms after swept-candidate filtering. It allocated 0 managed bytes over the measured direct ticks and stays below the 12 ms combat CPU budget, leaving at least 4.67 ms of a 60 Hz frame for rendering and presentation.

The existing rendered 100-enemy load test remained green: 120 sampled frames produced 16.663 ms median and 16.683 ms p95; the final sampled run-update marker was 0.6813 ms and its weapon marker was 0.0332 ms. The 100-to-134 enemy burst took 3.7560 ms and warmed enemy movement allocated 0 bytes.

## First-render investigation

- `origin/master` tracks 353 PNG files under `Assets/JoseonHunter/Art` and 354 PNG files across `Assets`; there are no untracked visual assets under the art root.
- There are no `.anim`, `.controller`, or `.overrideController` assets by design. Combat motion uses checked-in PNG frame arrays and code-driven `CombatantVisualRig` animation.
- Gameplay scene load measured 376.874 ms cold and 375.783 ms after a Lobby round-trip in the same PlayMode run. No repeatable first-load-only delay was found after Unity compilation/import completed.
- A static TMP atlas experiment was rejected because it raised measured cold load to roughly 602 ms. The project retains its smaller dynamic font assets.
- The remaining delay seen immediately after opening or recompiling in the Unity Editor is therefore classified as Editor compilation/import/scene integration, not missing remote assets or the confirmed combat CPU bottleneck. Android device startup was not measured.

## Pickup range

- Starting attraction radius changed from 2.2 to 0.58 world units.
- A pickup at 1.0 world unit remains stationary; a pickup at 0.5 world unit moves toward the player.
- The final collection threshold remains 0.42 and the warding-bell support upgrade still adds 0.7 attraction range.

## Validation

| Check | Result |
| --- | --- |
| Full EditMode | 544/544 passed, 0 failed. |
| Combat performance regression | 3/3 passed. |
| Pickup-range PlayMode | 2/2 passed. |
| Existing 30/50/100 enemy load suite | 9/9 passed. |
| Existing eight-weapon combat PlayMode | 9/9 passed. |
| Android development build | Passed; APK SHA-256 `ab713615da27169c06d2fa9708cd54469bac6e54e11e5a38166020d81c613396`. |

The APK file is 172,148,490 bytes. ZIP entry compressed payload is about 110.16 MB; the remaining file size is Android package alignment/signing space, not 62 MB of newly added game assets.

## Limitations

- No physical Android device was connected, so device CPU/GPU frame timing, startup, thermal behavior, and memory were not measured.
- The known 79 weapon-potential failures in the complete project PlayMode suite were not part of this optimization and remain deferred. Focused gameplay and weapon behavior suites listed above are green.
- User-owned changes in `Assets/JoseonHunter/Scenes/Gameplay.unity` and `ProjectSettings/ProjectSettings.asset` were preserved and excluded from the implementation commits.
