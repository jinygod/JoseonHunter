# Korean Readability and Simplified Feedback Verification

Date: 2026-08-02

## Delivered commits

- `3569e29` PixelLab simplified sprites
- `22d9edc` warm flat appraisal presentation
- `9cec298` experience pickup and damage-number readability
- `a4adb6f` coherent guardian descent presenter
- `d96c307` Korean Canvas run-result UI and HUD copy

The work was committed directly to `master` and pushed to `origin/master` as requested.

## PixelLab provenance

- Gakgung UI icon: PixelLab job `22125829-689a-4b2c-8b4a-83644e96dadc`
- Guardian descent sprite: PixelLab job `9066b97b-c5df-4600-b426-ea75265530b9`
- Both files are transparent, point-filtered sprites with no white contour.

## Automated verification

### EditMode

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

NUnit XML result: **564 total, 564 passed, 0 failed, 0 skipped**.

### Changed-surface PlayMode

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter "JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests|JoseonHunter.Tests.PlayMode.DamageNumberPoolPlayModeTests|JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayableUiStatePlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayablePickupRangePlayModeTests"
```

NUnit XML result: **49 total, 49 passed, 0 failed, 0 skipped**.

This includes the real Gameplay-scene restart path, background raycast restoration, one-sprite guardian pooling, pickup-range separation, damage-number pooling, appraisal layout, and Korean HUD copy.

### Existing potential-combat fixture

`WeaponPotentialCombatBPlayModeTests` remains red: **59 total, 20 passed, 39 failed**. Failures span the pre-existing potential-mask and exact-timing matrix for Jangseung, Singijeon, Frost, and Fan executors, including 12 `Potential_cell_is_not_the_base_weapon_pixel` cases. This is recorded separately and is not represented as green.

## Visual evidence

The portrait capture tool generated 20 PNGs across 720x1280, 1080x1920, 1080x2340, 1170x2532, and 1440x3200 under `Artifacts/PortraitValidation`.

Representative inspection:

- `Artifacts/PortraitValidation/720x1280/03-appraisal.png`: opaque Hanji panel, dark-brown result row, warm inset potential rows, no ornate slot/coin decoration.
- `Artifacts/PortraitValidation/720x1280/01-gameplay.png`: Korean HUD labels and distant-camera layout.

## Android build

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1
```

Result: **Success**. Output: `Builds/Android/JoseonHunter-development.apk` (172,268,438 bytes). Unity reported `Build Finished, Result: Success` and exited with code 0.

## Workspace preservation

The following pre-existing user changes were not staged or committed:

- `Assets/JoseonHunter/Scenes/Gameplay.unity`
- `Assets/JoseonHunter/Resources/Fonts/MaruBuri-Regular-Dynamic SDF.asset`
- `Assets/JoseonHunter/Resources/Fonts/MaruBuri-SemiBold-Dynamic SDF.asset`
- `ProjectSettings/ProjectSettings.asset`
- `.utmp/`

The untracked `tmp/` folder contains discarded Imagegen intermediates; the selected shipped sprites are the PixelLab files listed above.
