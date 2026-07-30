# Jangseung / Geumjul mobile-readability verification

Date: 2026-07-30

Unity: `6000.5.5f1 (d16e074b49fd)`.

## Rebuild and capture

The Gameplay scene was regenerated only through `JoseonHunter.Editor.Scenes.FirstPlayableSceneGenerator.Generate`; no scene YAML was hand edited. The Jangseung/Geumjul asset library was then rebuilt with `JoseonHunter.Editor.AssetProduction.JangseungGeumjulAssetImporter.Rebuild`.

The deterministic capture entry point is `JoseonHunter.Editor.Scenes.EightWeaponPolishCapture.CaptureJangseungGeumjulReadabilityInBatchMode`. After the first inspection, the visual-only presenter was corrected to cap the anchor at 0.42 world units, cap knots at 0.28 world units with 1.1 world-unit spacing (maximum 10), and derive closure scale from 72% of the polygon's maximum bound. It writes 360x800 portrait frames:

- `Artifacts/WeaponPolish/jangseung_ward-jangseung-crossing.png` (42,611 bytes)
- `Artifacts/WeaponPolish/hwando_flying_blade-geumjul-closure-ready.png` (17,835 bytes)
- `Artifacts/WeaponPolish/hwando_flying_blade-geumjul-closure-impact.png` (17,074 bytes)
- `Logs/jangseung-geumjul-gameplay.png` (17,074 bytes; closure-impact verification artifact)

The first capture attempt used `-nographics` and crashed in URP's `Camera.Render`; its exact Unity process was left as an inaccessible Windows zombie after the crash. The final successful retry omitted `-nographics` and ran after the failed process no longer held the project. Unity's unrelated Search index `ArgumentOutOfRangeException` appeared in both graphical capture logs; it did not prevent any of the three PNG writes.

## Focused validation

All `-runTests` commands intentionally omit `-quit`; the Unity Test Framework exits after completion. Invocations were run sequentially. These are the exact runnable commands and filters used:

```powershell
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
$project = 'D:\UnityProjects\JoseonHunter\.worktrees\jangseung-geumjul-readability'

& $unityExe -batchmode -nographics -projectPath $project -executeMethod JoseonHunter.Editor.Scenes.FirstPlayableSceneGenerator.Generate -logFile 'Artifacts\task4-scene-generate.log' -quit
& $unityExe -batchmode -nographics -projectPath $project -executeMethod JoseonHunter.Editor.AssetProduction.JangseungGeumjulAssetImporter.Rebuild -logFile 'Artifacts\task4-asset-rebuild.log' -quit

$initialEditFilter = 'JoseonHunter.Tests.EditMode.JangseungGeumjulAssetTests|JoseonHunter.Tests.EditMode.GeumjulTrailPresenterTests|JoseonHunter.Tests.EditMode.GeumjulRuleTests|JoseonHunter.Tests.EditMode.StaticSpriteBatchContractTests|JoseonHunter.Tests.EditMode.EightWeaponPolishCapturePolicyTests|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungWardDamagesOnlyAConfirmedBoundaryCrossingAndRequiresLeaveBeforeReentry|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungCrossingPresentsOnlyTheConfirmedSegmentAndContact|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungSetRetirementClearsPersistentPostsAndRopes|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungRetirementCancelsPendingDustAndCrossingFrames|JoseonHunter.Tests.EditMode.WeaponMechanicTests.WeaponRuntimePassesTheJangseungVisualLibraryToRegisteredWards|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungWardEvictsTheOldestFiniteSetAndRetiresItsAttack|JoseonHunter.Tests.EditMode.WeaponMechanicTests.LevelFiveJangseungMaintainsFourCardinalPosts|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungWardRisesBeforeBoundaryBecomesFullyVisible|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungWardPlaysFiveRiseFramesWithoutBurstingMissedFrames|JoseonHunter.Tests.EditMode.WeaponMechanicTests.LevelFiveJangseungRevealsEachBoundaryAfterItsOwnStaggeredRise|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungWardResamplesCenteredPpu32MaskAndRejectsTransparentCrossings|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungWardUsesExactMovementCrossingTimeForLargeFrames|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungWardPpu32MaskIncludesEndpointsAndRotatedFiniteSegments'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testFilter $initialEditFilter -testResults 'Artifacts\task4-editmode.xml' -logFile 'Artifacts\task4-editmode.log'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests' -testResults 'Artifacts\task4-playmode.xml' -logFile 'Artifacts\task4-playmode.log'

$correctionEditFilter = 'JoseonHunter.Tests.EditMode.GeumjulTrailPresenterTests|JoseonHunter.Tests.EditMode.JangseungGeumjulAssetTests|JoseonHunter.Tests.EditMode.GeumjulRuleTests|JoseonHunter.Tests.EditMode.EightWeaponPolishCapturePolicyTests'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testFilter $correctionEditFilter -testResults 'Artifacts\task4-correction-editmode.xml' -logFile 'Artifacts\task4-correction-editmode.log'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests' -testResults 'Artifacts\task4-correction-playmode.xml' -logFile 'Artifacts\task4-correction-playmode.log'

$fix1EditFilter = 'JoseonHunter.Tests.EditMode.GeumjulTrailPresenterTests|JoseonHunter.Tests.EditMode.WeaponMechanicTests.JangseungCrossingPresentsOnlyTheConfirmedSegmentAndContact|JoseonHunter.Tests.EditMode.EightWeaponPolishCapturePolicyTests'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testFilter $fix1EditFilter -testResults 'Artifacts\task4-fix1-editmode.xml' -logFile 'Artifacts\task4-fix1-editmode.log'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests' -testResults 'Artifacts\task4-fix1-playmode.xml' -logFile 'Artifacts\task4-fix1-playmode.log'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testFilter $fix1EditFilter -testResults 'Artifacts\task4-fix1-final-editmode.xml' -logFile 'Artifacts\task4-fix1-final-editmode.log'

# Graphical mode is required for URP camera capture; do not add -nographics.
& $unityExe -batchmode -projectPath $project -executeMethod JoseonHunter.Editor.Scenes.EightWeaponPolishCapture.CaptureJangseungGeumjulReadabilityInBatchMode -logFile 'Artifacts\task4-fix1-final-capture.log'

& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testFilter $fix1EditFilter -testResults 'Artifacts\task4-finalwave-editmode.xml' -logFile 'Artifacts\task4-finalwave-editmode.log'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests' -testResults 'Artifacts\task4-finalwave-playmode.xml' -logFile 'Artifacts\task4-finalwave-playmode.log'
& $unityExe -batchmode -projectPath $project -executeMethod JoseonHunter.Editor.Scenes.EightWeaponPolishCapture.CaptureJangseungGeumjulReadabilityInBatchMode -logFile 'Artifacts\task4-finalwave-capture.log'
```

- EditMode: 80 total, 80 passed, 0 failed, 0 skipped (`Artifacts/task4-editmode.xml`). This includes `JangseungGeumjulAssetTests`, `GeumjulTrailPresenterTests`, `GeumjulRuleTests`, `StaticSpriteBatchContractTests`, capture policy tests, and the Jangseung-only `WeaponMechanicTests` methods.
- PlayMode: 9 total, 9 passed, 0 failed, 0 skipped (`Artifacts/task4-playmode.xml`).
- Corrected presenter EditMode rerun: 44 total, 44 passed, 0 failed, 0 skipped (`Artifacts/task4-correction-editmode.xml`), including explicit anchor/knot size caps and bounds-derived closure-scale assertions.
- Corrected PlayMode rerun: 9 total, 9 passed, 0 failed, 0 skipped (`Artifacts/task4-correction-playmode.xml`).
- Flash-gate EditMode reruns: 12 total, 12 passed, 0 failed, 0 skipped (`Artifacts/task4-fix1-editmode.xml` and `Artifacts/task4-fix1-final-editmode.xml`).
- Flash-gate PlayMode rerun: 9 total, 9 passed, 0 failed, 0 skipped (`Artifacts/task4-fix1-playmode.xml`).
- Sprite-bounds closure-scale final wave: EditMode 12 total, 12 passed, 0 failed, 0 skipped (`Artifacts/task4-finalwave-editmode.xml`); PlayMode 9 total, 9 passed, 0 failed, 0 skipped (`Artifacts/task4-finalwave-playmode.xml`).
- Compilation: no C# errors in the successful rebuild/test/capture logs. Baseline warnings remain Unity API-obsolescence warnings in unrelated import, production-settings, and test code.

## Portrait visual inspection (360x800)

| Criterion | Result | Evidence |
| --- | --- | --- |
| Jangseung posts and thin rope boundary legible | Pass | Crossing frame shows four posts and a rope materially thinner than the combatants. |
| Only crossed segment flashes | Pass | The crossing frame has a localized bright accent at the crossed lower-left boundary; other boundary segments retain their normal rope treatment. |
| Closure seal frame visible | Pass | The closure-impact frame contains the transient red/yellow seal frame. |
| Geumjul rope/charms, anchor hierarchy, and closure filling the polygon | Pass | The corrected ready frame has a readable straw-rope outline, sparse small red charms, and an anchor smaller than the player. The final impact frame uses the frame sprite bounds to fit the polygon's 72%-coverage target without full-screen overdraw. |
| No persistent effect after reset | Automated pass | The focused Jangseung presenter/reset coverage passed in the 80-test EditMode run. |

No gameplay rule or balance value was changed; the correction is limited to visual scale/spacing and the focused presenter assertions.

## Working-tree baseline

Known unrelated dirty files include numerous Unity-generated texture `.meta` files, `Artifacts/`, `ProjectSettings/SceneTemplateSettings.json`, `ProjectSettings/ProjectSettings.asset`, and the generator-written `Assets/JoseonHunter/Scenes/Gameplay.unity`. They are deliberately not staged or reverted by this task.

## Resources integration fallback (option 2 merge decision)

The primary visual library now lives at `Assets/JoseonHunter/Resources/Presentation/JangseungGeumjulVisualLibrary.asset`. Its existing meta GUID (`8f260d29299e8cd4e9b4e07244e15ff6`) was moved intact. The importer, asset test, and scene generator use this canonical asset path. If `FirstPlayableController` has no serialized Jangseung/Geumjul visual-library assignment, it loads `Resources.Load<JangseungGeumjulVisualLibrary>("Presentation/JangseungGeumjulVisualLibrary")`; both the initial runtime setup and weapon-executor rebuild resolve through that fallback.

Focused validation was sequential and intentionally did not rerun the full suite:

```powershell
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.JangseungGeumjulAssetTests|JoseonHunter.Tests.EditMode.GeumjulTrailPresenterTests|JoseonHunter.Tests.EditMode.WeaponMechanicTests' -testResults 'Artifacts\task4-resources-editmode.xml' -logFile 'Artifacts\task4-resources-editmode.log'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests.ResetRunLoadsResourcesVisualLibraryWhenSerializedAssignmentIsMissing' -testResults 'Artifacts\task4-resources-controller-playmode.xml' -logFile 'Artifacts\task4-resources-controller-playmode.log'
& $unityExe -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests' -testResults 'Artifacts\task4-resources-eightweapon-playmode.xml' -logFile 'Artifacts\task4-resources-eightweapon-playmode.log'
```

- EditMode asset/presenter/weapon-mechanic filter: 59 total, 59 passed, 0 failed, 0 skipped.
- Serialized-null controller fallback: 1 total, 1 passed, 0 failed, 0 skipped.
- EightWeapon PlayMode: 9 total, 9 passed, 0 failed, 0 skipped.

The final recorded full EditMode baseline is **484 total, 471 passed, 13 failed** (`Artifacts/final-full-editmode.xml`). The user selected option 2: merge this load-bearing integration fix while explicitly accepting the following unrelated project-health failures, which were not changed by this task:

- `JoseonHunter.Tests.EditMode.AssetImportProfileTests.AffixSlotPartsUseReadableUncompressedPixelImportProfile`
- `JoseonHunter.Tests.EditMode.CombatRuleTests.Weapon_affix_catalog_has_exact_launch_balance_and_imported_contact_assets`
- `JoseonHunter.Tests.EditMode.MobilePixelArtImportTests.ApprovedPolishBatchContainsOneRenderedAssetPerPng`
- `JoseonHunter.Tests.EditMode.MobilePixelArtImportTests.CombatAnimationBatchContainsExpectedIndividualFrames`
- `JoseonHunter.Tests.EditMode.MobilePixelArtImportTests.WeaponPolishTextureRemainsReadableForPixelContactMasks`
- `JoseonHunter.Tests.EditMode.ProductionAssetContractTests.AndroidReleaseContractIsPortraitApi36Arm64`
- `JoseonHunter.Tests.EditMode.SceneScaffoldTests.EachFoundationSceneHasOnlyTheSceneRoot("Assets/JoseonHunter/Scenes/Gameplay.unity")`
- `JoseonHunter.Tests.EditMode.SceneScaffoldTests.GameplaySceneRootContainsWorldAndUi`
- `JoseonHunter.Tests.EditMode.StaticSpriteContentTests.GameplaySceneContainsInactiveStaticSpriteLaunchProofLineup`
- `JoseonHunter.Tests.EditMode.WeaponAffixPixelAssetContractTests.ApprovedAtlasesAreBinaryAndExactDimensions`
- `JoseonHunter.Tests.EditMode.WeaponAffixPixelAssetContractTests.Every_potential_sprite_and_mask_uses_the_mobile_safe_pixel_import_profile`
- `JoseonHunter.Tests.EditMode.WeaponAffixPixelAssetContractTests.PotentialMasksAreBinarySubsetsAndEveryPotentialResolves`
- `JoseonHunter.Tests.EditMode.WeaponAffixRollerTests.General_roll_values_stay_in_approved_range(Cooldown,-12.0d,-5.0d)`
