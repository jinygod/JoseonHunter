# Portrait stabilization baseline

Date: 2026-07-31

## Deterministic test runner

`Tools/Unity/Test-Unity.ps1` now accepts `-Platform editmode|playmode`, removes the prior platform result file, starts Unity with `Start-Process -Wait -PassThru`, verifies that Unity wrote fresh XML, and returns Unity's process exit code.

## Fresh EditMode baseline

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

The warmed runner returned exit code `1` only after Unity completed. Fresh `Logs/editmode-results.xml` timestamp: `2026-07-31T20:08:49.7702428+09:00`.

- Total: 490
- Passed: 479
- Failed: 11
- Skipped: 0
- Duration: 20.5244274 seconds

The table preserves the expected baseline dispositions. The assertion messages are copied from the fresh XML; the two expected failures absent from this XML are explicitly noted as passing/not emitted rather than repaired.

| Test | Disposition | Fresh XML assertion |
| --- | --- | --- |
| `ProductionAssetContractTests.AndroidReleaseContractIsPortraitApi36Arm64` | Apply production portrait settings | `Expected: Portrait; But was: LandscapeLeft` |
| `SceneScaffoldTests.EachFoundationSceneHasOnlyTheSceneRoot(Gameplay)` | Assert the shipped Gameplay roots | `Expected: property Length equal to 1; But was: 3` |
| `SceneScaffoldTests.GameplaySceneRootContainsWorldAndUi` | Replace obsolete foundation-scene contract | `System.InvalidOperationException: Sequence contains more than one element` |
| `StaticSpriteContentTests.GameplaySceneContainsInactiveStaticSpriteLaunchProofLineup` | Prove content through runtime catalogs | `System.InvalidOperationException: Sequence contains no matching element` |
| `MobilePixelArtImportTests.CombatAnimationBatchContainsExpectedIndividualFrames` | Update approved count to 64 | `Expected: property Length equal to 48; But was: 64` |
| `MobilePixelArtImportTests.WeaponPolishTextureRemainsReadableForPixelContactMasks` | Assert `PolishPixelsPerUnit` (64) | `Expected: 32.0f; But was: 64.0f` |
| `MobilePixelArtImportTests.ApprovedPolishBatchContainsOneRenderedAssetPerPng` | Add exact telegraph-fragment contract | `Assets/JoseonHunter/Art/Weapons/Runtime/Polish\\Fan\\fan_target_01.png Expected: <empty>; But was: < "multiple independent asset islands" >` |
| `AssetImportProfileTests.AffixSlotPartsUseReadableUncompressedPixelImportProfile` | Fix affix UI import classification | `reel_frame Expected: Point; But was: Bilinear` |
| `WeaponAffixPixelAssetContractTests.ApprovedAtlasesAreBinaryAndExactDimensions` | Normalize approved atlas alpha | Not emitted as a failure in fresh XML (passed in this baseline). |
| `WeaponAffixPixelAssetContractTests.PotentialMasksAreBinarySubsetsAndEveryPotentialResolves` | Rebuild binary subset masks | Not emitted as a failure in fresh XML (passed in this baseline). |
| `WeaponAffixPixelAssetContractTests.Every_potential_sprite_and_mask_uses_the_mobile_safe_pixel_import_profile` | Clear overrides in importer and reimport | `Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/Sprites/hwando_venom_fang.png Android Expected: False; But was: True` |
| `CombatRuleTests.Weapon_affix_catalog_has_exact_launch_balance_and_imported_contact_assets` | Assert explicit collection `.Count` | `System.ArgumentException: Property Count was not found (Parameter name: name)` |
| `WeaponAffixRollerTests.General_roll_values_stay_in_approved_range(Cooldown)` | Correct endpoint direction to `-5 -> -12` | `Expected: -12.0d; But was: -5.0d` |

## Fresh PlayMode execution

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode -Filter JoseonHunter.Tests.PlayMode.FirstPlayableUiStatePlayModeTests
```

Fresh `Logs/playmode-results.xml` timestamp: `2026-07-31T20:09:35.4847582+09:00`.

- Total: 1
- Passed: 1
- Failed: 0
- Skipped: 0
- Executed: `JoseonHunter.Tests.PlayMode.FirstPlayableUiStatePlayModeTests.Upgrade_presentation_contract_guards_choices_and_queues_rewards`
