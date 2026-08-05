# Upgrade Clarity and Combat Identity Verification

## Scope

- Commit range: `f62c72d..f3242b0`
- Unity: `6000.5.5f1`
- Target: Android ARM64, IL2CPP, Development APK
- Build scenes: `Bootstrap`, `Lobby`, `Gameplay`

## Acceptance evidence

| Criterion | Evidence | Status |
|---|---|---|
| Passive upgrades apply without a second confirmation | `RewardRevealPlayModeTests`, `WeaponAffixRevealPlayModeTests` | Passed |
| Weapon appraisal/detail text is separated into readable effect rows | 1080x1920 `03-appraisal.png`; `WeaponAffixRevealPlayModeTests` | Passed |
| HUD shows level stars, affix-quality border, and up to three potentials | `CombatHudPlayModeTests`, including levels 1 and 5 | Passed |
| Frost Flask is low-damage control rather than burst damage | `WeaponMechanicTests`, `FrostFanLegacyPlayModeTests` | Passed |
| Gakgung has high single-shot damage and slower cadence | `WeaponContentTests`, `WeaponMechanicTests` | Passed |
| Normal-enemy durability rises throughout the 15-minute stage | `EnemyHealthCurveTests` | Passed |
| Existing maximum loadout remains four weapons and three supports | Existing `RunLoadoutRules` contracts in full test suites | Passed |

## Automated tests

- EditMode: `684 / 684` passed, `0` failed, `0` skipped.
  - Result: `Logs/editmode-results.xml`
- PlayMode: `244 / 244` passed, `0` failed, `0` skipped.
  - Result: `Logs/playmode-results.xml`
- Targeted regression coverage includes support choice flow, appraisal/detail layout,
  rack star and quality bounds, Frost damage/status/visual contracts, Gakgung targeting and
  side-arrow damage, and the 15-minute health curve.

## Portrait visual inspection

The existing portrait-state batch capture rendered 20 PNGs at 720x1280, 1080x1920,
1080x2340, 1170x2532, and 1440x3200.

Representative 1080x1920 files:

- `Artifacts/PortraitValidation/1080x1920/01-gameplay.png`
- `Artifacts/PortraitValidation/1080x1920/02-level-up.png`
- `Artifacts/PortraitValidation/1080x1920/03-appraisal.png`
- `Artifacts/PortraitValidation/1080x1920/04-resumed-combat.png`

Observed:

- HUD and pause control remain inside the portrait safe area.
- The level-up card uses an opaque hanji panel and its Korean copy is readable.
- Read-only weapon details show separate `누적 추가옵션`, `성장 방식`, and `잠재 능력`
  rows without text overlap.
- Closing the detail returns to the normal combat composition.
- The capture tool does not provide a dedicated active-Frost-field state. Frost rendering is
  covered by an automated contract requiring authored frames 14-15, alpha `0.60`, a flattened
  field, and one small landing fragment using `Impact + 3`.

## Android build

- Command: `Tools/Unity/Build-AndroidDevelopment.ps1`
- Result: `Build Finished, Result: Success` (process exit code `0`)
- Artifact: `Builds/Android/JoseonHunter-development.apk`
- Size: `173,444,358` bytes
- Log: `Logs/android-development-build.log`

## Review and regression classification

- Independent read-only review found no critical issues.
- Its important Frost visual-cue finding was corrected and protected by a regression test.
- The earlier full PlayMode run exposed three stale/cadence expectations; all were isolated,
  fixed, and the final full suite passed.
- No first-party compile or test failures remain in the recorded suites.

## Preserved unrelated working-tree state

- Existing whitespace-only texture `.meta` changes under combat-choice art were not staged.
- Unity build/import added `SENTIS_ANALYTICS_ENABLED` to the local Android scripting symbols in
  `ProjectSettings/ProjectSettings.asset`; this generated local setting was not included in the
  feature commits.

## Limitations

- The APK was built but not installed on a physical Android device in this validation pass.
- Fifteen-minute balance, input feel, sustained frame time, thermal behavior, and memory pressure
  still require a full device playthrough and profiler capture.
