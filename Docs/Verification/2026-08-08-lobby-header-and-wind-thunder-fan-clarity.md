# Lobby Header and Wind-Thunder Fan Clarity Verification

## Scope

- Rebuilt the lobby account header as separate account, currency, and settings regions.
- Replaced the procedural gear drawing with a dedicated PixelLab sprite and constrained audio slider handles.
- Added a dedicated Wind-Thunder Fan UI icon instead of reusing a combat animation frame.
- Replaced all 15 Wind-Thunder Fan combat frames with simpler limited-palette silhouettes and increased their presentation scale.
- Preserved structured and summary-only affix grades so the weapon detail screen no longer reports `등급 미상` when tier data exists.
- Added a portrait-only Wind-Thunder Fan capture entry point and made the capture rig resolve the live player reference after a run reset.

## Automated Verification

| Suite | Result |
| --- | --- |
| Full EditMode | 899 passed, 0 failed, 0 skipped |
| Full PlayMode | 301 passed, 0 failed, 0 skipped |
| Capture policy regression | 11 passed, 0 failed |
| Lobby navigation focused suite | 8 passed |
| Weapon affix detail focused suite | 33 passed |
| Weapon content focused suite | 20 passed |
| Wind-Thunder Fan combat focused suite | 10 passed |
| Weapon mechanic focused suite | 65 passed |
| Mobile pixel-art import focused suite | 7 passed |

Final full-suite result files:

- `Logs/final-editmode-results.xml`
- `Logs/final-playmode-results.xml`

## Visual Verification

Lobby captures:

- `Artifacts/LobbyPremium/720x1280-patrol.png`
- `Artifacts/LobbyPremium/1080x2340-patrol.png`

The account medallion, account name, green experience bar, coin icon with `155`, and settings button are separate and readable without overlap. The settings icon uses the same dark-gold palette as the lobby frame.

Wind-Thunder Fan portrait captures:

- `Artifacts/WeaponPolish/wind_thunder_fan-level-1.png`
- `Artifacts/WeaponPolish/wind_thunder_fan-level-3.png`
- `Artifacts/WeaponPolish/wind_thunder_fan-level-5.png`
- `Artifacts/WeaponPolish/wind_thunder_fan-evolved.png`

The level-one mark is larger than the player sprite and remains legible against the grass field. Higher-level captures show multiple clear navy/cyan wind seals without white halos or fragmented pixel islands. The new opened-fan inventory icon reads as a fan at 64x64 and is visually distinct from combat effects.

The capture command must run with a graphics device. Unity 6000.5.5f1 crashes while rendering a camera under `-nographics`; batch mode without `-nographics` completed all four captures.

## PixelLab Generation Record

Twelve generation credits were used. Accepted outputs include the lobby gear, opened-fan UI icon, simplified gust, thick lightning, and connected wind-seal animation. Early gust/lightning drafts and the concentric-ring target draft were rejected because they were noisy or split into independent pixel islands.

Accepted/relevant job IDs:

- Lobby gear: `4e249e64-db90-4e8b-b8cb-8a530bc09a11`
- Wind-Thunder Fan UI icon: `fac304ea-1634-4f30-a7af-6cd946867b4f`
- Simplified gust source: `ffd04ea7-9c40-4fb6-8a13-9357ec763d65`
- Thick lightning source: `445d5f0d-d7c2-46a9-9452-5656f10b7fb1`
- Gust animation: `dc69f5b7-d2db-4979-81c3-7d00de428843`
- Lightning animation: `c1b5fc19-2920-4e5d-a06d-626372246ddb`
- Connected target source: `d9b59732-c96d-41fc-87e2-0ab8734c09a7`
- Connected target animation: `14bd1342-816f-4007-912d-33cc281b98bf`

## Remaining Manual Check

No physical Android device was connected during this pass. The portrait editor captures and automated tests are green; touch sizing and final display brightness should still be checked once on the target phone before a release build.
