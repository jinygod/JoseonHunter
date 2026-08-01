# Portrait typography, appraisal, and camera verification

## Scope and outcome

- Unity: `6000.5.5f1` (`d16e074b49fd`), Windows Editor and Android development build.
- Implementation commits: `9349006` through `db24dfe` on `master`.
- Scope: licensed Korean runtime fonts, semantic typography, Korean affix labels, opaque level-up cards, rebuilt appraisal sheet and rarity seal, post-stop value count-up, and a wider portrait combat camera.
- Outcome: **ready with limitations**. All acceptance checks for this UI/camera scope passed. The complete project PlayMode suite still has 79 unrelated weapon-potential combat failures, and no Android device run was available.

## Acceptance evidence

| Requirement | Result | Evidence |
| --- | --- | --- |
| Korean font roles | Passed | Chosun Gungseo for titles, Maru Buri for UI/body text, and Black And White Picture for damage numbers. Font files and their license texts are checked in under `Assets/JoseonHunter/Art/Fonts/`. |
| Upgrade cards | Passed | Cards use an opaque hanji field; icon and Korean text stay inside a centered safe content area. Five portrait resolutions were visually inspected. |
| Appraisal panel | Passed | The stretched pixel background and coin-like emblem were removed. The panel now uses an opaque hanji sheet, ink rails, Korean rarity seals, gold titles, and high-contrast detail text. |
| Affix localization | Passed | `Damage`, `Cooldown`, `Area`, `ProjectileSpeed`, and `Duration` display as Korean terms. `완벽한 추가옵션` is now `최대 추가옵션`; the button is `확인`. |
| Value reveal | Passed | The selected percentage counts from 0 to its rolled result only after the reel stops, with tick pulses and a final punch. Confirm remains disabled until the settle phase completes. |
| Camera scale | Passed | Portrait combat orthographic size changed from 7.25 to 18, making combat actors roughly 2.5 times smaller on screen. The independent spawn profile uses 8.5. |

## Automated validation

| Validation | Result |
| --- | --- |
| Full EditMode | `544/544` passed, 0 failed; `2026-08-01 06:11:08Z` to `06:11:31Z`, duration 23.052294 s. |
| Focused appraisal PlayMode | `28/28` passed, 0 failed; `2026-08-01 06:37:36Z` to `06:38:07Z`, duration 31.2010152 s. This includes layout, timing, count-up, completion lock, and text-contrast coverage. |
| Full PlayMode audit | `188/267` passed and 79 failed. Failures are concentrated in `WeaponPotentialCombatA/B`, with one evolved-weapon and one affix vertical-slice assertion. These combat-potential/executor paths are outside the deferred UI/camera scope and were not changed here. The full project suite is therefore not green. |
| Android development build | Passed. Unity reported `Build Finished, Result: Success`. APK: `Builds/Android/JoseonHunter-development.apk`, 110,257,114 bytes, SHA-256 `6ce85ed887db68803b422385dcdc5df581075b315fc26936e07105d6fae06e3c`. |

Generated XML and logs remain ignored under `Logs/`. Dynamic TMP atlas data was cleared after the final focused test so generated glyph caches are not committed.

## Portrait visual evidence

The production Gameplay scene was captured in four real runtime states at five resolutions with `PortraitStateValidationCapture.CaptureInBatchMode`:

- 720x1280
- 1080x1920
- 1080x2340
- 1170x2532
- 1440x3200

Each resolution contains `01-gameplay`, `02-level-up`, `03-appraisal`, and `04-resumed-combat` under ignored `Artifacts/PortraitValidation/`, for 20 non-empty PNG files total. The final visual review confirmed opaque upgrade cards, readable centered Korean type, an intact appraisal sheet without the cropped pixel background, readable gold/pale appraisal text, and the wider combat view.

## Limitations and hygiene

- Android installation, device rendering, thermals, and device performance were not validated.
- Combat population/performance and monster-wave pacing remain deliberately deferred.
- The user's existing edits in `Assets/JoseonHunter/Scenes/Gameplay.unity` and `ProjectSettings/ProjectSettings.asset` were preserved and excluded from these commits.
- Unity's generated `.utmp/` directory remains untracked because the environment blocked its deletion; it is not part of any commit or build input contract.
