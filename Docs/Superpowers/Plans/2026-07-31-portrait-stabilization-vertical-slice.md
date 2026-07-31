# Portrait Stabilization Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a portrait-only, pause-safe, readable, and profiled three-minute JoseonHunter combat vertical slice without adding new game content.

**Architecture:** Add a pure transition policy and one Unity time owner, then route the existing controller and presenters through it instead of rewriting the playable. Build portrait UI and viewport geometry as focused helpers, add a reusable spatial-hash separation solver, and make pooling or PixelLab work conditional on captured evidence.

**Tech Stack:** Unity 6000.5.5f1, C# 9, URP 2D, uGUI, TextMesh Pro, Input System, NUnit Unity Test Framework, Unity Profiler markers/recorders, PowerShell, Android IL2CPP/ARM64.

## Global Constraints

- Target Unity `6000.5.5f1` and Android only for this vertical slice.
- Default orientation is `Portrait`; both landscape orientations and portrait upside-down are disabled.
- Reference UI resolution is `1080 x 1920` with Safe Area anchoring.
- Required validation sizes are 720x1280, 1080x1920, 1080x2340, 1170x2532, and 1440x3200.
- Required game states are `Playing`, `LevelUpSelection`, `AugmentResult`, `Paused`, and `GameOver`.
- `GameFlowCoordinator` is the only production type allowed to write `Time.timeScale`.
- Modal UI animates with unscaled time; gameplay and camera remain frozen until explicit confirmation.
- Enemy load gates are 30, 50, and 100 active enemies.
- PixelLab starts at 1,512 remaining generations; the initial generation budget is zero.
- The current 16 GB workstation is sufficient; hardware upgrades are not part of this pass.
- Do not add characters, weapons, stages, enemies, or progression content.
- Preserve the existing assembly boundaries: Domain has no Unity references; Runtime may reference Domain/Content/Input System; Presentation may reference Runtime.
- Every completed task ends with a commit and `git push` to `origin/agent/portrait-stabilization-vertical-slice`.

## File Structure and Responsibilities

### New production files

- `Assets/JoseonHunter/Scripts/Domain/Runs/GameFlowState.cs`: enum and pure transition policy.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameFlowCoordinator.cs`: sole time-scale owner and transient hit-stop clock.
- `Assets/JoseonHunter/Scripts/Presentation/UI/PortraitUiMetrics.cs`: reference resolution and anchored portrait layout constants.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ViewportSpawnGeometry.cs`: deterministic perimeter spawn geometry.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySeparationGrid.cs`: reusable, bounded spatial-hash crowd solver.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableProfilerMarkers.cs`: subsystem marker names and scopes.
- `Assets/JoseonHunter/Scripts/Editor/Build/AndroidDevelopmentBuild.cs`: reproducible portrait development APK build.
- `Tools/Unity/Build-AndroidDevelopment.ps1`: command-line entry point for the development build.

### New tests

- `Assets/JoseonHunter/Tests/EditMode/GameFlowStateTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/GameFlowCoordinatorPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/ModalGameFlowPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/ViewportSpawnGeometryTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/EnemySeparationGridTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableLoadPlayModeTests.cs`

### Existing files with focused changes

- `Tools/Unity/Test-Unity.ps1`: deterministic EditMode/PlayMode runner that waits for Unity.
- `ProjectSettings/ProjectSettings.asset`: generated portrait player settings.
- `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`: correct pixel UI import classification.
- `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponAffixPixelAssetImporter.cs`: exact point-filtered, uncompressed, binary asset imports.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`: flow gating, portrait profile, viewport spawning, separation, instrumentation, three-minute duration.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatVisualScaleProfile.cs`: replace landscape profile with portrait values and spawn margins.
- `Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs`: delegate hit stop to the coordinator.
- `Assets/JoseonHunter/Scripts/Presentation/Combat/FirstPlayableDamageNumberBootstrap.cs`: bind feedback to controller flow.
- `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`: portrait Canvas, flow-safe reward chain, Safe Area hierarchy.
- `Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs`: portrait cards and no time ownership.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`: portrait appraisal and no time ownership.
- `Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs`: compact portrait top HUD.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`: two-column portrait rack.
- `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`: explicit confirmation contract for upgrade results.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatMotionLibrary.cs` and its asset: audited runtime animation bindings only.
- `Docs/AI/UnityProjectContext.md`: current portrait architecture and commands.
- `Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md`: test, capture, profiler, device, and PixelLab evidence.

---

### Task 1: Make Unity test execution deterministic

**Files:**
- Modify: `Tools/Unity/Test-Unity.ps1`
- Create: `Docs/Verification/2026-07-31-portrait-stabilization-baseline.md`

**Interfaces:**
- Consumes: Unity executable at `C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe`.
- Produces: `Test-Unity.ps1 -Platform editmode|playmode -Filter <filter>` with fresh XML and a reliable process exit code.

- [ ] **Step 1: Replace the detached runner with a waiting runner**

Use this complete PowerShell contract:

```powershell
param(
    [ValidateSet('editmode', 'playmode')]
    [string]$Platform = 'editmode',
    [string]$Filter = 'JoseonHunter.Tests.EditMode'
)

$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$logs = Join-Path $root 'Logs'
$results = Join-Path $logs ($Platform + '-results.xml')
$log = Join-Path $logs ($Platform + '.log')
New-Item -ItemType Directory -Path $logs -Force | Out-Null
if (Test-Path -LiteralPath $results) { Remove-Item -LiteralPath $results -Force }

$arguments = @(
    '-batchmode', '-nographics', '-projectPath', $root,
    '-runTests', '-testPlatform', $Platform, '-testFilter', $Filter,
    '-testResults', $results, '-logFile', $log
)
$process = Start-Process -FilePath $unity -ArgumentList $arguments `
    -Wait -PassThru -WindowStyle Hidden
if (-not (Test-Path -LiteralPath $results)) {
    throw "Unity did not produce $results. Inspect $log."
}
exit $process.ExitCode
```

- [ ] **Step 2: Run the full EditMode baseline and prove the result is fresh**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

Expected before Task 2: 490 tests execute and the known 13 failures are reproduced in a newly timestamped `Logs/editmode-results.xml`; the PowerShell process does not return before Unity exits.

- [ ] **Step 3: Record the exact failing test names and classification**

Create the baseline document with these exact expected failures, then append the fresh XML assertion message to each row:

```markdown
| Test | Disposition |
| --- | --- |
| `ProductionAssetContractTests.AndroidReleaseContractIsPortraitApi36Arm64` | Apply production portrait settings |
| `SceneScaffoldTests.EachFoundationSceneHasOnlyTheSceneRoot(Gameplay)` | Assert the shipped Gameplay roots |
| `SceneScaffoldTests.GameplaySceneRootContainsWorldAndUi` | Replace obsolete foundation-scene contract |
| `StaticSpriteContentTests.GameplaySceneContainsInactiveStaticSpriteLaunchProofLineup` | Prove content through runtime catalogs |
| `MobilePixelArtImportTests.CombatAnimationBatchContainsExpectedIndividualFrames` | Update approved count to 64 |
| `MobilePixelArtImportTests.WeaponPolishTextureRemainsReadableForPixelContactMasks` | Assert `PolishPixelsPerUnit` (64) |
| `MobilePixelArtImportTests.ApprovedPolishBatchContainsOneRenderedAssetPerPng` | Add exact telegraph-fragment contract |
| `AssetImportProfileTests.AffixSlotPartsUseReadableUncompressedPixelImportProfile` | Fix affix UI import classification |
| `WeaponAffixPixelAssetContractTests.ApprovedAtlasesAreBinaryAndExactDimensions` | Normalize approved atlas alpha |
| `WeaponAffixPixelAssetContractTests.PotentialMasksAreBinarySubsetsAndEveryPotentialResolves` | Rebuild binary subset masks |
| `WeaponAffixPixelAssetContractTests.Every_potential_sprite_and_mask_uses_the_mobile_safe_pixel_import_profile` | Clear overrides in importer and reimport |
| `CombatRuleTests.Weapon_affix_catalog_has_exact_launch_balance_and_imported_contact_assets` | Assert explicit collection `.Count` |
| `WeaponAffixRollerTests.General_roll_values_stay_in_approved_range(Cooldown)` | Correct endpoint direction to `-5 -> -12` |
```

- [ ] **Step 4: Verify the PlayMode runner launches the correct assembly**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode -Filter JoseonHunter.Tests.PlayMode.FirstPlayableUiStatePlayModeTests
```

Expected: the filtered class executes and `Logs/playmode-results.xml` is created. Existing failures are recorded but not repaired in this task.

- [ ] **Step 5: Commit and push**

```powershell
git add Tools/Unity/Test-Unity.ps1 Docs/Verification/2026-07-31-portrait-stabilization-baseline.md
git commit -m "test: make Unity validation deterministic"
git push
```

---

### Task 2: Restore a truthful green baseline

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponAffixPixelAssetImporter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/SinglePngAssetValidator.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/AssetImportProfileTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatRuleTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/MobilePixelArtImportTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponPolishPixelAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/ProductionAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/SceneScaffoldTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/StaticSpriteContentTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixPixelAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixRollerTests.cs`
- Modify: affected checked-in `.png.meta` files and binary mask PNGs through Unity import code
- Modify: `ProjectSettings/ProjectSettings.asset` through the existing settings method

**Interfaces:**
- Consumes: Task 1 runner and `PortraitAndroidReleaseSettings.ApplyPortraitAndroidReleaseContract()`.
- Produces: a green 490-test EditMode baseline whose assertions describe the current vertical-slice assets and Gameplay scene.

- [ ] **Step 1: Add focused regression assertions before changing import code**

Add exact checks:

```csharp
Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.Portrait));
Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.True);
Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);

var importer = AssetImporter.GetAtPath(
    "Assets/JoseonHunter/Art/UI/AffixJackpot/SlotParts/reel_frame.png") as TextureImporter;
Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
Assert.That(importer.GetPlatformTextureSettings("Android").overridden, Is.False);
```

Change the cooldown endpoint test case to the authored direction:

```csharp
[TestCase(WeaponAffixStat.Cooldown, -5d, -12d)]
```

Use explicit collection counts where NUnit 4 cannot discover `Count`:

```csharp
Assert.That(WeaponRoster.All.Count, Is.EqualTo(8));
Assert.That(WeaponAffixCatalog.CompatiblePotentials(weapon).Count, Is.EqualTo(3));
```

- [ ] **Step 2: Run the focused tests and verify they fail for the known reasons**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform editmode -Filter "JoseonHunter.Tests.EditMode.AssetImportProfileTests;JoseonHunter.Tests.EditMode.ProductionAssetContractTests;JoseonHunter.Tests.EditMode.WeaponAffixRollerTests"
```

Expected: portrait/import checks fail before the importer/settings changes; cooldown passes after its test endpoint correction.

- [ ] **Step 3: Fix production contracts and update only demonstrably stale expectations**

Classify all affix jackpot UI as pixel art:

```csharp
private const string AffixJackpotUiRoot =
    "Assets/JoseonHunter/Art/UI/AffixJackpot/";

private static bool IsBilinearArt(string path) =>
    path.StartsWith(UiArtRoot, StringComparison.Ordinal)
    && !path.StartsWith(AffixJackpotUiRoot, StringComparison.Ordinal)
    || path.StartsWith(LobbyArtRoot, StringComparison.Ordinal);
```

In `ConfigureSlotKit`, call `ClearPlatformOverrides(importer)` before `SaveAndReimport()`. Add a private binary normalizer used for checked-in mask PNGs; preserve only source-opaque pixels:

```csharp
private static Color32[] BinarySubset(Color32[] source, Color32[] mask)
{
    var output = new Color32[mask.Length];
    for (var index = 0; index < mask.Length; index++)
    {
        var active = source[index].a == byte.MaxValue && mask[index].a > 0;
        output[index] = active
            ? new Color32(255, 255, 255, 255)
            : new Color32(0, 0, 0, 0);
    }
    return output;
}

private static Color32[] BinaryAlpha(Color32[] pixels)
{
    var output = new Color32[pixels.Length];
    for (var index = 0; index < pixels.Length; index++)
    {
        var pixel = pixels[index];
        output[index] = pixel.a == 0
            ? new Color32(0, 0, 0, 0)
            : new Color32(pixel.r, pixel.g, pixel.b, 255);
    }
    return output;
}
```

Add this writer and use `BinaryAlpha` for the approved 256x128 UI atlases and `BinarySubset` for gameplay masks before configuring their importers:

```csharp
private static void WritePixels(
    string assetPath, Color32[] pixels, int width, int height)
{
    var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    try
    {
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(texture);
    }
    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
}
```

Run `WeaponAffixPixelAssetImporter.EnsureImported()` and `PortraitAndroidReleaseSettings.ApplyPortraitAndroidReleaseContract()` in batch Unity so Unity owns the serialized `.meta` and settings changes:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
& $unity -batchmode -nographics -quit -projectPath (Get-Location).Path `
  -executeMethod JoseonHunter.Editor.AssetProduction.WeaponAffixPixelAssetImporter.EnsureImported `
  -logFile Logs/affix-import.log
& $unity -batchmode -nographics -quit -projectPath (Get-Location).Path `
  -executeMethod JoseonHunter.Editor.AssetProduction.PortraitAndroidReleaseSettings.ApplyPortraitAndroidReleaseContract `
  -logFile Logs/portrait-settings.log
```

Update stale contracts to these observed approved facts:

- combat animation batch count: 64 individual idle/walk frames;
- weapon polish runtime PPU: 64;
- Gameplay roots: `Main Camera`, `FirstPlayable`, `EventSystem`;
- static launch content is proven by the runtime catalogs, not by an obsolete inactive proof lineup;
- `fan_target_01.png` is an explicitly reviewed multi-fragment telegraph: validate 2-8 opaque components and at least four pixels per component instead of treating it as a single character silhouette.

Expose the existing flood-fill result without changing its algorithm:

```csharp
public static IReadOnlyList<int> MeasureOpaqueComponents(string assetPath)
{
    var absolutePath = Path.IsPathRooted(assetPath)
        ? assetPath
        : Path.GetFullPath(assetPath);
    if (!File.Exists(absolutePath))
        throw new FileNotFoundException("PNG asset does not exist.", absolutePath);
    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
    try
    {
        if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath), false))
            throw new InvalidDataException("Asset is not a readable PNG.");
        return FindOpaqueComponents(
            texture.GetPixels32(), texture.width, texture.height).AsReadOnly();
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(texture);
    }
}
```

The test keeps the single-principal-asset validator for every other file and uses `MeasureOpaqueComponents` only for the exact telegraph:

```csharp
const string fragmentedTelegraph =
    "Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Fan/fan_target_01.png";
if (assetPath == fragmentedTelegraph)
{
    var components = SinglePngAssetValidator.MeasureOpaqueComponents(assetPath);
    Assert.That(components.Count, Is.InRange(2, 8), assetPath);
    Assert.That(components, Has.All.GreaterThanOrEqualTo(4), assetPath);
}
else
{
    Assert.That(SinglePngAssetValidator.Validate(assetPath), Is.Empty, assetPath);
}
```

- [ ] **Step 4: Run the complete EditMode suite**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

Expected: all 490 tests pass, no test-generated tracked diff remains, and `git diff --check` reports no non-Unity serialization issue.

- [ ] **Step 5: Commit and push**

```powershell
git add Assets/JoseonHunter ProjectSettings/ProjectSettings.asset
git commit -m "fix: restore portrait asset and test contracts"
git push
```

---

### Task 3: Add the pure game-flow policy and sole time owner

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/GameFlowState.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameFlowCoordinator.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameFlowStateTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GameFlowCoordinatorPlayModeTests.cs`

**Interfaces:**
- Produces: `GameFlowTransitions.CanTransition(GameFlowState from, GameFlowState to)`, `GameFlowCoordinator.State`, `IsGameplayRunning`, `TryTransition`, `ResetToPlaying`, and `RequestHitStop`.
- Consumers: Tasks 4 and 5.

- [ ] **Step 1: Write failing transition and time ownership tests**

```csharp
[TestCase(GameFlowState.Playing, GameFlowState.LevelUpSelection, true)]
[TestCase(GameFlowState.LevelUpSelection, GameFlowState.AugmentResult, true)]
[TestCase(GameFlowState.AugmentResult, GameFlowState.LevelUpSelection, true)]
[TestCase(GameFlowState.AugmentResult, GameFlowState.Playing, true)]
[TestCase(GameFlowState.Playing, GameFlowState.Paused, true)]
[TestCase(GameFlowState.Paused, GameFlowState.Playing, true)]
[TestCase(GameFlowState.Paused, GameFlowState.AugmentResult, false)]
public void Transition_policy_is_explicit(
    GameFlowState from, GameFlowState to, bool expected)
{
    Assert.That(GameFlowTransitions.CanTransition(from, to), Is.EqualTo(expected));
}
```

```csharp
[UnityTest]
public IEnumerator Modal_state_wins_over_hit_stop()
{
    var coordinator = new GameObject("Flow").AddComponent<GameFlowCoordinator>();
    Assert.That(coordinator.RequestHitStop(.2f), Is.True);
    Assert.That(Time.timeScale, Is.Zero);
    Assert.That(coordinator.TryTransition(GameFlowState.LevelUpSelection), Is.True);
    yield return new WaitForSecondsRealtime(.25f);
    Assert.That(coordinator.State, Is.EqualTo(GameFlowState.LevelUpSelection));
    Assert.That(Time.timeScale, Is.Zero);
    Object.Destroy(coordinator.gameObject);
}
```

- [ ] **Step 2: Run tests and verify missing-type failures**

Run both new test classes with Task 1 runner. Expected: compilation fails because the types do not exist.

- [ ] **Step 3: Implement the explicit policy and coordinator**

The Domain file contains no Unity types:

```csharp
namespace JoseonHunter.Domain.Runs
{
    public enum GameFlowState
    {
        Playing,
        LevelUpSelection,
        AugmentResult,
        Paused,
        GameOver
    }

    public static class GameFlowTransitions
    {
        public static bool CanTransition(GameFlowState from, GameFlowState to)
        {
            if (from == to) return true;
            if (to == GameFlowState.GameOver) return from != GameFlowState.GameOver;
            return (from, to) switch
            {
                (GameFlowState.Playing, GameFlowState.LevelUpSelection) => true,
                (GameFlowState.LevelUpSelection, GameFlowState.AugmentResult) => true,
                (GameFlowState.AugmentResult, GameFlowState.LevelUpSelection) => true,
                (GameFlowState.AugmentResult, GameFlowState.Playing) => true,
                (GameFlowState.Playing, GameFlowState.Paused) => true,
                (GameFlowState.Paused, GameFlowState.Playing) => true,
                (GameFlowState.GameOver, GameFlowState.Playing) => true,
                _ => false
            };
        }
    }
}
```

The coordinator uses unscaled time and is the only production writer. Implement this complete body inside a `MonoBehaviour` with `using System;`, `using JoseonHunter.Domain.Runs;`, and `using UnityEngine;`:

```csharp
private float hitStopRemaining;

public GameFlowState State { get; private set; } = GameFlowState.Playing;
public bool IsGameplayRunning => State == GameFlowState.Playing;
public event Action<GameFlowState, GameFlowState> StateChanged;

public bool TryTransition(GameFlowState next)
{
    if (!GameFlowTransitions.CanTransition(State, next))
    {
        Debug.LogWarning($"Rejected game-flow transition {State} -> {next}.", this);
        return false;
    }
    var previous = State;
    State = next;
    if (next != GameFlowState.Playing) hitStopRemaining = 0f;
    ApplyTimeScale();
    if (previous != next) StateChanged?.Invoke(previous, next);
    return true;
}

public bool RequestHitStop(float seconds)
{
    if (State != GameFlowState.Playing || seconds <= 0f) return false;
    hitStopRemaining = Mathf.Max(hitStopRemaining, seconds);
    ApplyTimeScale();
    return true;
}

public void ResetToPlaying()
{
    var previous = State;
    State = GameFlowState.Playing;
    hitStopRemaining = 0f;
    ApplyTimeScale();
    if (previous != State) StateChanged?.Invoke(previous, State);
}

private void Update()
{
    if (State != GameFlowState.Playing || hitStopRemaining <= 0f) return;
    hitStopRemaining = Mathf.Max(0f, hitStopRemaining - Time.unscaledDeltaTime);
    ApplyTimeScale();
}

private void ApplyTimeScale()
{
    Time.timeScale = State == GameFlowState.Playing && hitStopRemaining <= 0f ? 1f : 0f;
}

private void OnDisable()
{
    State = GameFlowState.Playing;
    hitStopRemaining = 0f;
    Time.timeScale = 1f;
}
```

- [ ] **Step 4: Run new EditMode and PlayMode classes**

Expected: all transition, invalid transition, idempotency, hit-stop expiry, modal precedence, and disable-reset tests pass.

- [ ] **Step 5: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Runs Assets/JoseonHunter/Scripts/Runtime/Gameplay `
  Assets/JoseonHunter/Tests/EditMode/GameFlowStateTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/GameFlowCoordinatorPlayModeTests.cs
git commit -m "feat: centralize game flow time ownership"
git push
```

---

### Task 4: Route combat, camera, and hit stop through game flow

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/FirstPlayableDamageNumberBootstrap.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatFeedbackDirectorPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePresentationPlayModeTests.cs`

**Interfaces:**
- Consumes: Task 3 `GameFlowCoordinator`.
- Produces: `FirstPlayableController.Flow` and a gameplay update/camera gate controlled by flow state.

- [ ] **Step 1: Write failing freeze tests**

Add a PlayMode test that records an enemy, camera, and elapsed time; transition to `Paused`; wait in realtime; assert every recorded gameplay value is unchanged. This also proves the camera does not continue its `SmoothDamp` while paused.

```csharp
var beforeCamera = Camera.main.transform.position;
var beforeElapsed = controller.UiState.Elapsed;
var enemy = controller.SpawnEnemyForTests(new Vector2(4f, 0f));
var beforeEnemy = enemy.WorldPosition;
Assert.That(controller.Flow.TryTransition(GameFlowState.Paused), Is.True);
yield return new WaitForSecondsRealtime(.2f);
Assert.That(controller.UiState.Elapsed, Is.EqualTo(beforeElapsed));
Assert.That(Camera.main.transform.position, Is.EqualTo(beforeCamera));
Assert.That(enemy.WorldPosition, Is.EqualTo(beforeEnemy));
```

- [ ] **Step 2: Run the focused PlayMode tests**

Expected: failure because the controller exposes no flow and the camera still uses unscaled follow time.

- [ ] **Step 3: Bind the coordinator and remove competing time writers**

In `FirstPlayableController`:

```csharp
private GameFlowCoordinator flow;
public GameFlowCoordinator Flow => flow;

// Insert at the beginning of the existing Awake method.
flow = GetComponent<GameFlowCoordinator>() ??
       gameObject.AddComponent<GameFlowCoordinator>();

private void Update()
{
    if (runEnded)
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ResetRun();
        return;
    }
    if (flow == null || !flow.IsGameplayRunning) return;
    TickGameplay(Time.deltaTime);
}

private void LateUpdate()
{
    if (flow != null && flow.IsGameplayRunning && gameplayCamera != null && player != null)
        UpdateCamera();
}
```

Move the existing Update body into `TickGameplay(float delta)` without changing subsystem order. Change camera smoothing to `Time.deltaTime`. Replace `EndRun` with `flow.TryTransition(GameFlowState.GameOver)` and reset with `flow.ResetToPlaying()`. Remove every `Time.timeScale` assignment from the controller.

In `CombatFeedbackDirector`, replace stored time-scale ownership with:

```csharp
private GameFlowCoordinator flow;
public void BindGameFlow(GameFlowCoordinator value)
{
    if (flow != null && isActiveAndEnabled)
        flow.StateChanged -= OnFlowStateChanged;
    flow = value;
    if (flow != null && isActiveAndEnabled)
        flow.StateChanged += OnFlowStateChanged;
}

private void OnFlowStateChanged(GameFlowState previous, GameFlowState current)
{
    if (current == GameFlowState.Playing) return;
    impulseRemaining = 0f;
    RestoreRenderBaseline();
}

private void BeginHitStop(float duration) => flow?.RequestHitStop(duration);
```

`OnCameraPreCull` returns without applying an impulse when `flow != null && !flow.IsGameplayRunning`. `OnEnable` subscribes to `flow.StateChanged`; `OnDisable` unsubscribes and restores the render baseline; `OnDestroy` calls `BindGameFlow(null)`. `FirstPlayableDamageNumberBootstrap` calls `feedbackDirector.BindGameFlow(controller.Flow)` whenever it binds a damage service.

- [ ] **Step 4: Prove sole ownership and run regressions**

Run:

```powershell
rg -n "Time\.timeScale\s*=" Assets/JoseonHunter/Scripts
```

Expected after Task 5 completes: only `GameFlowCoordinator.cs` appears. For this task, upgrade/appraisal presenters are the only temporary additional matches. Run the focused controller and feedback PlayMode classes and the full EditMode suite.

- [ ] **Step 5: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs `
  Assets/JoseonHunter/Scripts/Presentation/Combat `
  Assets/JoseonHunter/Tests/PlayMode
git commit -m "refactor: gate combat and feedback through game flow"
git push
```

---

### Task 5: Keep level-up and appraisal paused through confirmation

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/ModalGameFlowPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Consumes: `GameFlowCoordinator` and controller `Flow`.
- Produces: authoritative `Playing -> LevelUpSelection -> AugmentResult -> Playing|LevelUpSelection` flow and `Playing <-> Paused` weapon detail flow.

- [ ] **Step 1: Write the end-to-end modal test first**

```csharp
SceneManager.LoadScene("Gameplay");
yield return null;
yield return null;
var controller = Object.FindFirstObjectByType<FirstPlayableController>();
var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
var elapsedBefore = controller.UiState.Elapsed;
controller.OpenUpgradeOffersForTests(new UpgradeOffer(
    WeaponId.HwandoFlyingBlade.Value, UpgradeKind.Weapon, 2));
Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.LevelUpSelection));
Assert.That(Time.timeScale, Is.Zero);
Assert.That(controller.TryChooseUpgrade(0), Is.True);
Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.AugmentResult));
yield return new WaitForSecondsRealtime(.4f);
Assert.That(Time.timeScale, Is.Zero);
Assert.That(controller.UiState.Elapsed, Is.EqualTo(elapsedBefore));
affix.Skip();
yield return new WaitForSecondsRealtime(1.2f);
Assert.That(affix.IsAwaitingConfirmation, Is.True);
Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.AugmentResult));
affix.Confirm();
yield return new WaitForSecondsRealtime(.2f);
Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
```

Update `OpenUpgradeOffersForTests` to call the same `flow.TryTransition(GameFlowState.LevelUpSelection)` gate before it publishes forced offers; test helpers must not bypass production flow semantics.

Add a second test that queues another level and expects `AugmentResult -> LevelUpSelection`, not a one-frame return to `Playing`:

```csharp
controller.OpenUpgradeOffersForTests(new UpgradeOffer(
    WeaponId.HwandoFlyingBlade.Value, UpgradeKind.Weapon, 2));
Assert.That(controller.TryChooseUpgrade(0), Is.True);
controller.AddExperienceForTests(100);
yield return new WaitForSecondsRealtime(.4f);
affix.Skip();
yield return new WaitForSecondsRealtime(1.2f);
affix.Confirm();
yield return new WaitForSecondsRealtime(.2f);
Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.LevelUpSelection));
Assert.That(controller.IsUpgradeOpen, Is.True);
```

- [ ] **Step 2: Run and verify flow-state failures**

Expected: tests fail because presenters restore time scale independently and controller does not transition the new states.

- [ ] **Step 3: Implement modal transitions and remove presenter ownership**

In `OpenUpgrade`, transition before publishing choices:

```csharp
if (!flow.TryTransition(GameFlowState.LevelUpSelection)) return;
upgradeOpen = true;
```

In `TryChooseUpgrade`, transition before applying the accepted choice so a rejected state transition cannot mutate progression:

```csharp
if (!flow.TryTransition(GameFlowState.AugmentResult)) return false;
var reward = ApplyUpgrade(upgradeOfferData[index]);
upgradeOpen = false;
upgradeOffers.Clear();
upgradeOfferData.Clear();
awaitingUpgradePresentationClose = true;
UpgradeChosen?.Invoke(reward);
```

In `NotifyUpgradePresentationClosed` (the transition policy already allows result to selection):

```csharp
if (pendingUpgradeCount > 0)
{
    pendingUpgradeCount--;
    OpenUpgrade(); // transition AugmentResult -> LevelUpSelection, then publish
}
else
{
    flow.TryTransition(GameFlowState.Playing);
}
```

Remove all time-scale writes and slowdown interpolation from `UpgradeChoicePresenter` and `WeaponAffixRevealPresenter`. Keep entrance, scroll, skip, close, and confirm coroutines on `Time.unscaledDeltaTime`.

For weapon details opened from the rack, `FirstPlayableUiBootstrap` transitions `Playing -> Paused` before `ShowDetails` and transitions `Paused -> Playing` from the detail completion callback. Background raycasts are disabled with the active modal `CanvasGroup.blocksRaycasts = true` and normal HUD/rack groups false.

Change generic support/evolution reward reveal to hold on a visible confirm button and emit `RevealCompleted` only after confirmation, matching appraisal behavior.

- [ ] **Step 4: Verify sole time ownership and modal regressions**

Run all modal PlayMode classes, then:

```powershell
rg -n "Time\.timeScale\s*=" Assets/JoseonHunter/Scripts
```

Expected: the only production assignment is in `GameFlowCoordinator.cs`; all modal tests pass and queued upgrades never briefly resume gameplay.

- [ ] **Step 5: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs `
  Assets/JoseonHunter/Scripts/Presentation/UI `
  Assets/JoseonHunter/Tests/PlayMode
git commit -m "feat: hold modal game flow through confirmation"
git push
```

---

### Task 6: Establish portrait Canvas, Safe Area, HUD, rack, and modal geometry

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/PortraitUiMetrics.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RuntimeUiFactory.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`
- Move: `Assets/JoseonHunter/Art/Fonts/NotoSansKR-Dynamic SDF.asset` to `Assets/JoseonHunter/Resources/Fonts/NotoSansKR-Dynamic SDF.asset` while preserving its `.meta` GUID
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs` (display copy only)

**Interfaces:**
- Produces: portrait layout constants and safe-area-contained presenter bounds at every required size.

- [ ] **Step 1: Write layout-bound tests for all five resolutions**

```csharp
[UnityTest]
public IEnumerator Interactive_rects_stay_inside_safe_area()
{
    foreach (var size in PortraitUiMetrics.ValidationResolutions)
    {
        Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
        yield return null;
        var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
        var safe = new Rect(0f, size.y * .04f, size.x, size.y * .925f);
        bootstrap.ApplySafeArea(safe, size);
        Canvas.ForceUpdateCanvases();
        foreach (var button in bootstrap.GetComponentsInChildren<Button>(true))
        {
            var owner = button.transform.IsChildOf(bootstrap.ModalSafeAreaContainer)
                ? bootstrap.ModalSafeAreaContainer
                : bootstrap.SafeAreaContainer;
            Assert.That(IsContained(owner,
                button.GetComponent<RectTransform>()), Is.True, button.name);
        }
    }
}

private static bool IsContained(RectTransform parent, RectTransform child)
{
    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, child);
    return parent.rect.Contains((Vector2)bounds.min) &&
           parent.rect.Contains((Vector2)bounds.max);
}
```

Also assert Canvas reference resolution is exactly `new Vector2(1080f, 1920f)`, upgrade cards are at most 936 pixels wide, the confirm button bottom is inside the safe root, and every created `TextMeshProUGUI.font.name` equals `NotoSansKR-Dynamic SDF`.

- [ ] **Step 2: Run the layout tests**

Expected: failures show the 1920x1080 reference and landscape weapon rack/card offsets.

- [ ] **Step 3: Implement focused portrait metrics and anchored layouts**

```csharp
public static class PortraitUiMetrics
{
    public static readonly Vector2 ReferenceResolution = new(1080f, 1920f);
    public static readonly Vector2Int[] ValidationResolutions =
    {
        new(720, 1280), new(1080, 1920), new(1080, 2340),
        new(1170, 2532), new(1440, 3200)
    };
    public const float SideMargin = 48f;
    public const float TopMargin = 32f;
    public const float BottomMargin = 36f;
    public const float ModalWidth = 936f;
    public const float UpgradeCardHeight = 236f;
    public const float RackSlotWidth = 474f;
    public const float RackSlotHeight = 104f;
}
```

Set `CanvasScaler.referenceResolution = PortraitUiMetrics.ReferenceResolution` and keep match at `.5f`. Preserve the existing normalized Safe Area anchor math.

Create a full-canvas `Modal Layer` with a stretched `Modal Scrim`, then create `Modal Safe Area` beneath it. Expose `ModalSafeAreaContainer` for tests and apply the same normalized safe-area anchors to both safe containers. Parent upgrade/reward/appraisal presenters under `Modal Safe Area`; toggle only the full-canvas scrim outside it.

Move the existing dynamic SDF asset and `.meta` together into `Assets/JoseonHunter/Resources/Fonts/`. Cache and apply it in `RuntimeUiFactory`:

```csharp
private const string KoreanFontPath = "Fonts/NotoSansKR-Dynamic SDF";
private static TMP_FontAsset koreanFont;

private static TMP_FontAsset KoreanFont
{
    get
    {
        if (koreanFont == null)
            koreanFont = Resources.Load<TMP_FontAsset>(KoreanFontPath);
        return koreanFont;
    }
}

// In Text(...), immediately after AddComponent<TextMeshProUGUI>():
var font = KoreanFont;
if (font == null)
    Debug.LogError($"Missing runtime UI font at Resources/{KoreanFontPath}.");
else
    result.font = font;
```

Replace corrupted display separators with ` · ` and use these exact visible labels: `강화를 선택하세요`, `신규 무기`, `무기 강화`, `지원 강화`, `진화`, `환도 비검`, `각궁`, `주술 부적`, `벽력탄`, `장승진`, `신기전`, `서리병`, and `풍뢰선`. IDs and combat behavior remain unchanged.

Use these layout decisions:

- HUD: one 984x176 top panel, run clock centered, health/XP left-to-right, boss bar below it;
- rack: two columns by up to four rows, anchored to the bottom with 24-pixel gaps;
- upgrade modal: 936-wide parchment region, three vertical 236-high cards, 28-pixel gaps, confirm/result region below;
- appraisal: maximum 936x1320 inside Safe Area, internal scroll content clipped by a `RectMask2D`;
- full-screen scrim remains outside the Safe Area but all interactive children remain inside it.

Do not add a real-time blur effect; use the existing dark scrim and parchment assets.

- [ ] **Step 4: Run portrait layout and modal PlayMode suites**

Expected: all five simulated resolutions pass bounds checks; text uses the checked-in Noto Sans KR TMP asset without fallback warnings; old landscape test names and values are removed.

- [ ] **Step 5: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/UI `
  Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs `
  Assets/JoseonHunter/Resources/Fonts Assets/JoseonHunter/Art/Fonts `
  Assets/JoseonHunter/Tests/PlayMode
git commit -m "feat: rebuild first playable UI for portrait"
git push
```

---

### Task 7: Tune portrait combat scale, viewport spawning, and three-minute pacing

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ViewportSpawnGeometry.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatVisualScaleProfile.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/ViewportSpawnGeometryTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatVisualScaleProfileTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/StagePacingTimelineTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/StagePacingPlayModeTests.cs`

**Interfaces:**
- Produces: `ViewportSpawnGeometry.PointOnExpandedPerimeter(Rect bounds, int side, float t, float margin)` and `CombatVisualScaleProfile.MobilePortrait`.
- Consumers: Task 8 load tests.

- [ ] **Step 1: Write geometry, visibility, and duration tests**

```csharp
[TestCase(0, 0.25f)]
[TestCase(1, 0.50f)]
[TestCase(2, 0.75f)]
[TestCase(3, 1.00f)]
public void Spawn_is_outside_view_and_on_requested_side(int side, float t)
{
    var view = new Rect(-4.5f, -8f, 9f, 16f);
    var point = ViewportSpawnGeometry.PointOnExpandedPerimeter(view, side, t, 1f);
    Assert.That(view.Contains(point), Is.False);
    if (side == 0) Assert.That(point.y, Is.EqualTo(view.yMax + 1f));
    if (side == 1) Assert.That(point.x, Is.EqualTo(view.xMax + 1f));
    if (side == 2) Assert.That(point.y, Is.EqualTo(view.yMin - 1f));
    if (side == 3) Assert.That(point.x, Is.EqualTo(view.xMin - 1f));
}
```

Assert `MobilePortrait.CameraOrthographicSize == 7.25f`, player scale `0.82f`, normal/elite/boss ordering, spawn margins `0.75f..1.50f`, and controller duration `180f` through `UiState.Duration`.

- [ ] **Step 2: Run focused tests and verify landscape/radius failures**

Expected: missing geometry type, `MobileLandscape` values, and 60-second duration fail.

- [ ] **Step 3: Implement deterministic perimeter geometry and portrait profile**

```csharp
public static Vector2 PointOnExpandedPerimeter(
    Rect bounds, int side, float t, float margin)
{
    t = Mathf.Clamp01(t);
    margin = Mathf.Max(0f, margin);
    return side switch
    {
        0 => new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, t), bounds.yMax + margin),
        1 => new Vector2(bounds.xMax + margin, Mathf.Lerp(bounds.yMin, bounds.yMax, t)),
        2 => new Vector2(Mathf.Lerp(bounds.xMax, bounds.xMin, t), bounds.yMin - margin),
        3 => new Vector2(bounds.xMin - margin, Mathf.Lerp(bounds.yMax, bounds.yMin, t)),
        _ => throw new ArgumentOutOfRangeException(nameof(side))
    };
}
```

Create `MobilePortrait` with initial reviewed values:

```csharp
new CombatVisualScaleProfile(
    baselineCameraOrthographicSize: 6.25f,
    cameraOrthographicSize: 7.25f,
    playerScale: .82f,
    normalEnemyScale: .78f,
    eliteEnemyScale: 1.00f,
    bossEnemyScale: 1.42f,
    normalContactRadius: .42f,
    eliteContactRadius: .55f,
    bossContactRadius: .78f,
    spawnMarginMinimum: .75f,
    spawnMarginMaximum: 1.50f);
```

In the controller, compute world bounds from camera viewport corners, choose `side = Random.Range(0, 4)`, `t = Random.value`, and margin from the profile. Replace `TestDuration` with `PrototypeDurationSeconds = 180f`; keep `StagePacingTimeline.ForDuration(180f)` so authored milestones scale across three minutes.

- [ ] **Step 4: Run scale, spawn, stage, and gameplay smoke tests**

Expected: spawn points are outside every tested aspect's visible bounds, the final boss milestone is 180 seconds, and no enemy begins inside the viewport.

- [ ] **Step 5: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay Assets/JoseonHunter/Tests
git commit -m "feat: tune portrait combat viewport and pacing"
git push
```

---

### Task 8: Add bounded spatial-hash enemy separation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySeparationGrid.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/EnemySeparationGridTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableLoadPlayModeTests.cs`

**Interfaces:**
- Produces: `EnemySeparationGrid.Rebuild(IReadOnlyList<EnemySeparationAgent>)` and `Resolve(int agentIndex, int maximumNeighbors)`.
- Consumes: portrait rank contact radii from Task 7.

- [ ] **Step 1: Write solver tests for overlap, pursuit, bounds, and reuse**

```csharp
var grid = new EnemySeparationGrid(.84f);
var agents = new[]
{
    new EnemySeparationAgent(10, Vector2.zero, .42f),
    new EnemySeparationAgent(11, Vector2.zero, .42f)
};
grid.Rebuild(agents);
var first = grid.Resolve(0, 8);
var second = grid.Resolve(1, 8);
Assert.That(first.sqrMagnitude, Is.GreaterThan(0f));
Assert.That(Vector2.Dot(first, second), Is.LessThan(0f));
```

Add parameterized 30/50/100-agent tests, a maximum-eight-neighbor assertion, deterministic output for exact overlap IDs, and a warmed-up allocation check around repeated `Rebuild/Resolve` calls.

- [ ] **Step 2: Run and verify missing solver failures**

Expected: compilation fails because `EnemySeparationGrid` and `EnemySeparationAgent` do not exist.

- [ ] **Step 3: Implement a reusable grid and blend it into pursuit**

Use a `Dictionary<Vector2Int, List<int>>`, a reusable bucket stack, and a reusable list of occupied keys. Exact overlap fallback is derived from stable IDs:

```csharp
public readonly struct EnemySeparationAgent
{
    public EnemySeparationAgent(int id, Vector2 position, float radius)
    {
        Id = id;
        Position = position;
        Radius = Mathf.Max(0f, radius);
    }

    public int Id { get; }
    public Vector2 Position { get; }
    public float Radius { get; }
}

private static Vector2 CoincidentDirection(int firstId, int secondId)
{
    var low = Math.Min(firstId, secondId);
    var high = Math.Max(firstId, secondId);
    var hash = unchecked(low * 397 ^ high * 7919);
    var angle = (hash & 1023) * (Mathf.PI * 2f / 1024f);
    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    return firstId <= secondId ? direction : -direction;
}
```

`EnemySeparationGrid(float cellSize)` rejects non-positive cell sizes. It exposes `LastNeighborCount` for validation. For each neighbor inside combined radii, accumulate normalized displacement weighted by penetration, set `LastNeighborCount`, stop after eight neighbors, and clamp the result to magnitude 1.

In `UpdateEnemies`, prune destroyed entries first. Reuse parallel `List<EnemyState> separationEnemies` and `List<EnemySeparationAgent> separationAgents`; add only living non-treasure enemies to both so `agentIndex` addresses the same enemy in each list. Rebuild the grid once, then blend:

```csharp
var enemy = separationEnemies[agentIndex];
var enemyPosition = (Vector2)enemy.Object.transform.position;
var chase = (playerPosition - enemyPosition).normalized;
var separate = separationGrid.Resolve(agentIndex, 8);
var direction = Vector2.ClampMagnitude(chase + separate * .72f, 1f);
var velocity = direction * (enemy.Speed * enemy.MovementMultiplier);
```

Treasure objects do not enter the grid. Boss/elite radii come from `ContactRadiusFor(rank)`.

- [ ] **Step 4: Run solver and 30/50/100 load tests**

Expected: no exact coincident pairs remain after repeated ticks, enemies still reduce distance to the player, neighbor work is bounded, and the warmed steady-state solver path allocates zero managed bytes.

- [ ] **Step 5: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySeparationGrid.cs `
  Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs `
  Assets/JoseonHunter/Tests/EditMode/EnemySeparationGridTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/FirstPlayableLoadPlayModeTests.cs
git commit -m "feat: separate dense enemy crowds"
git push
```

---

### Task 9: Instrument the vertical slice and capture load evidence

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableProfilerMarkers.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableLoadPlayModeTests.cs`
- Create: `Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md`

**Interfaces:**
- Produces markers named `JoseonHunter.Run.Update`, `.Enemy.Grid`, `.Enemy.Move`, `.Spawn`, `.Weapon`, `.Pickup`, `.UI.Hud`, and `.UI.Modal`.
- Produces evidence rows for 30, 50, and 100 enemies.

- [ ] **Step 1: Add marker-name and recorder smoke tests**

```csharp
Assert.That(FirstPlayableProfilerMarkers.RunUpdateName,
    Is.EqualTo("JoseonHunter.Run.Update"));
Assert.That(FirstPlayableProfilerMarkers.EnemyMoveName,
    Is.EqualTo("JoseonHunter.Enemy.Move"));
```

The PlayMode load test uses `ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame")`, warms for 30 frames, samples 120 frames, and writes median/p95 values to the test output.

- [ ] **Step 2: Run the load test without markers**

Expected: marker assertions fail; retain the initial frame-time/GC numbers as the before sample in the verification document.

- [ ] **Step 3: Add named scopes without changing behavior**

```csharp
public static class FirstPlayableProfilerMarkers
{
    public const string RunUpdateName = "JoseonHunter.Run.Update";
    public const string EnemyGridName = "JoseonHunter.Enemy.Grid";
    public const string EnemyMoveName = "JoseonHunter.Enemy.Move";
    public static readonly ProfilerMarker RunUpdate = new(RunUpdateName);
    public static readonly ProfilerMarker EnemyGrid = new(EnemyGridName);
    public static readonly ProfilerMarker EnemyMove = new(EnemyMoveName);
}
```

Wrap subsystem calls with `using (FirstPlayableProfilerMarkers.EnemyMove.Auto())` and equivalent markers. Do not reorder calls or change gameplay in this task.

- [ ] **Step 4: Capture and record 30/50/100 evidence**

Run `FirstPlayableLoadPlayModeTests`. Record active count, median frame time, p95 frame time, maximum GC allocation, and minimum enemy spacing. Acceptance for the test environment is no recurring GC allocation from enemy movement after warmup and no unbounded frame-time increase between tiers.

- [ ] **Step 5: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts Assets/JoseonHunter/Tests/PlayMode/FirstPlayableLoadPlayModeTests.cs `
  Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md
git commit -m "perf: instrument portrait combat load"
git push
```

---

### Task 10: Apply lifecycle pooling only when profiler thresholds require it

**Files:**
- Conditional create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableObjectPool.cs`
- Conditional modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Conditional create: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableObjectPoolPlayModeTests.cs`
- Modify in either outcome: `Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md`

**Interfaces:**
- Consumes: Task 9 profiler evidence.
- Produces: either a bounded enemy/pickup pool or an evidence-backed decision not to add one.

- [ ] **Step 1: Evaluate the exact decision gate**

Implement pooling only if at least one captured 100-enemy condition is true:

- `Instantiate`/`Destroy` samples exceed 1.0 ms p95;
- steady gameplay produces more than 512 bytes GC per frame for lifecycle work;
- a visible spawn burst misses the 16.67 ms frame budget because of lifecycle work.

If none is true, add a verification row `Pooling rejected: thresholds not crossed`, commit only the evidence document, push, and complete this task.

- [ ] **Step 2: If triggered, write failing reuse and capacity tests**

```csharp
var first = pool.Rent();
pool.Return(first);
var second = pool.Rent();
Assert.That(second, Is.SameAs(first));
Assert.That(pool.InactiveCount, Is.Zero);
Assert.That(pool.TotalCount, Is.LessThanOrEqualTo(140));
```

Test enemy reset fields, pickup reset fields, duplicate return rejection, and pool disposal on run teardown.

- [ ] **Step 3: Implement a bounded factory/reset pool**

```csharp
public sealed class FirstPlayableObjectPool<T> where T : class
{
    private readonly Func<T> create;
    private readonly Action<T> onRent;
    private readonly Action<T> onReturn;
    private readonly Stack<T> inactive = new();
    private readonly HashSet<T> leased = new();
    private readonly int maximum;
    public int TotalCount { get; private set; }
    public int InactiveCount => inactive.Count;

    public FirstPlayableObjectPool(
        Func<T> create,
        Action<T> onRent,
        Action<T> onReturn,
        int maximum)
    {
        this.create = create ?? throw new ArgumentNullException(nameof(create));
        this.onRent = onRent ?? throw new ArgumentNullException(nameof(onRent));
        this.onReturn = onReturn ?? throw new ArgumentNullException(nameof(onReturn));
        this.maximum = maximum > 0
            ? maximum
            : throw new ArgumentOutOfRangeException(nameof(maximum));
    }

    public T Rent()
    {
        if (inactive.Count == 0 && TotalCount >= maximum)
            throw new InvalidOperationException("Pool capacity exhausted.");
        var item = inactive.Count > 0 ? inactive.Pop() : CreateOne();
        leased.Add(item);
        onRent(item);
        return item;
    }

    public void Return(T item)
    {
        if (!leased.Remove(item))
            throw new InvalidOperationException("Item is not leased by this pool.");
        onReturn(item);
        inactive.Push(item);
    }

    private T CreateOne()
    {
        TotalCount++;
        return create();
    }

    public void Prewarm(int count)
    {
        var target = Math.Min(maximum, Math.Max(0, count));
        while (TotalCount < target)
        {
            var item = CreateOne();
            onReturn(item);
            inactive.Push(item);
        }
    }
}
```

Prewarm enemy capacity to 100 and pickup capacity to 160; hard maximums are 140 and 240. Reset renderer, transform, health, status, combat target registration, and animation state on every rent/return.

- [ ] **Step 4: Re-run the same profiler scenario**

Keep pooling only if it removes the triggered hotspot without introducing recurring GC or state leakage. Record before/after numbers. If it does not, revert only this task's production/test files, retain the evidence decision, commit, and push.

- [ ] **Step 5: Commit and push the retained outcome**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay Assets/JoseonHunter/Tests/PlayMode `
  Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md
git commit -m "perf: resolve measured lifecycle hotspot"
git push
```

---

### Task 11: Audit runtime animation and flying-blade presentation before generation

**Files:**
- Modify if binding defects exist: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatMotionLibrary.cs`
- Modify if binding defects exist: `Assets/JoseonHunter/Content/Motion/CombatMotionLibrary.asset`
- Modify if visibility defects exist: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatMotionLibraryTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Docs/Assets/pixellab-mobile-polish-generation-ledger.csv`
- Modify: `Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md`

**Interfaces:**
- Consumes: portrait captures and existing Han Yeonhwa/enemy/weapon frames.
- Produces: audited bindings, a flying-blade phase capture, and a zero-generation result unless an exact reviewed gap remains.

- [ ] **Step 1: Add asset-binding and flying-blade phase assertions**

Assert every active player/enemy base sprite resolves to a non-empty motion entry, frames use Point filtering/64 PPU, and flying blade produces visible outbound, contact, inbound, and return-to-pool phase cues.

```csharp
Assert.That(library.Find(hanBase).MoveFrames.Count, Is.EqualTo(8));
Assert.That(library.Find(hanBase).IdleFrames.Count, Is.EqualTo(4));
Assert.That(fixture.Events.Count(e => e.Phase == ContactPhase.Outbound), Is.EqualTo(1));
Assert.That(fixture.Events.Count(e => e.Phase == ContactPhase.Inbound), Is.EqualTo(1));
Assert.That(fixture.Executor.ReturnedToPoolCount, Is.EqualTo(1));
```

- [ ] **Step 2: Run the audit tests and capture 1080x1920 runtime evidence**

Record which existing frames are actually bound and visible. A missing binding is a code/content-reference defect, not authorization to generate art.

- [ ] **Step 3: Repair bindings and timing with existing assets**

Use `CombatMotionLibraryBuilder` to rebuild the library after correcting imports. Tune existing flying-blade frame cadence and scale only within the current executor/presentation data; do not change damage, cooldown, target count, or evolution behavior.

- [ ] **Step 4: Apply the PixelLab gate**

If all named gaps are resolved, append this ledger entry and generate nothing:

```csv
2026-07-31T19:49:02+09:00,portrait_stabilization_audit,,get_balance,portrait-stabilization-v1,0,no_generation_existing_assets_sufficient,1512,
```

If a gap remains, stop this task before generation and present the user with the exact asset ID, runtime screenshot, dimensions, frame count, prompt, expected credit cost, and destination. Generation proceeds only after that separate approval, then the real timestamp/job ID/cost/status/path replaces the audit-only row.

- [ ] **Step 5: Run content and weapon regressions, commit, and push**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform editmode -Filter "JoseonHunter.Tests.EditMode.CombatMotionLibraryTests;JoseonHunter.Tests.EditMode.WeaponMechanicTests"
git add Assets/JoseonHunter Docs/Assets Docs/Verification
git commit -m "fix: bind existing combat motion for portrait"
git push
```

---

### Task 12: Build Android, validate all resolutions, and publish the handoff

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/Build/AndroidDevelopmentBuild.cs`
- Create: `Tools/Unity/Build-AndroidDevelopment.ps1`
- Modify: `Docs/AI/UnityProjectContext.md`
- Complete: `Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md`

**Interfaces:**
- Produces: `Builds/Android/JoseonHunter-development.apk`, final automated/visual/performance evidence, and Java-to-Unity handoff.

- [ ] **Step 1: Add the reproducible development-build method**

```csharp
public static void Build()
{
    PortraitAndroidReleaseSettings.ApplyPortraitAndroidReleaseContract();
    var scenes = EditorBuildSettings.scenes
        .Where(scene => scene.enabled)
        .Select(scene => scene.path)
        .ToArray();
    var output = Path.GetFullPath("Builds/Android/JoseonHunter-development.apk");
    Directory.CreateDirectory(Path.GetDirectoryName(output));
    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
    {
        scenes = scenes,
        locationPathName = output,
        target = BuildTarget.Android,
        options = BuildOptions.Development |
                  BuildOptions.ConnectWithProfiler |
                  BuildOptions.AllowDebugging
    });
    if (report.summary.result != BuildResult.Succeeded)
        throw new BuildFailedException(report.summary.result.ToString());
}
```

The PowerShell wrapper uses `Start-Process -Wait -PassThru -WindowStyle Hidden` with `-executeMethod JoseonHunter.Editor.Build.AndroidDevelopmentBuild.Build` and fails if the APK is absent.

- [ ] **Step 2: Run full automated validation**

Run full EditMode and PlayMode assemblies with Task 1 runner, then build the APK. Expected: zero unexpected test failures, zero first-party compile errors, and a non-empty APK.

- [ ] **Step 3: Capture all five resolution states**

For each required resolution capture these four named states under `Artifacts/PortraitValidation/<width>x<height>/`:

1. `01-gameplay.png`
2. `02-level-up.png`
3. `03-appraisal.png`
4. `04-resumed-combat.png`

Record safe-area device emulation, clipped-element count, state, and reviewer result in the verification document. Captures stay ignored; the verification document records hashes and findings.

- [ ] **Step 4: Complete Android/performance and developer handoff evidence**

On an available Android device, record model, OS, resolution, median/p95 frame time, maximum GC, and 30/50/100-enemy results. If no physical device is available, state `Android device capture unavailable` and keep Editor results explicitly separate; do not label Editor data as device performance.

Update `UnityProjectContext.md` with:

- `MonoBehaviour.Awake/Update/OnDisable` mapped to managed component lifecycle callbacks;
- ScriptableObject assets mapped to versioned configuration objects, not services;
- Inspector references mapped to explicit application wiring/DI;
- coroutines distinguished from `Task` and cancellation tokens;
- scaled versus unscaled time and the single coordinator owner;
- EditMode versus PlayMode test commands;
- portrait capture and Android build commands;
- PixelLab starting balance 1,512, ending balance, and accepted/rejected/generated asset list.

- [ ] **Step 5: Run final checks, commit, and push**

```powershell
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform editmode -Filter JoseonHunter.Tests.EditMode
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode -Filter JoseonHunter.Tests.PlayMode
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1
git add Assets/JoseonHunter/Scripts/Editor/Build Tools/Unity Docs/AI Docs/Verification
git commit -m "docs: verify portrait stabilization vertical slice"
git push
```

Expected final repository state: clean worktree, local feature branch equals its remote tracking branch, full automated test evidence is fresh, Android build evidence is recorded, and every spec acceptance item has a corresponding verification row.

## Execution Order and Review Gates

- Tasks 1-2 are the trustworthy baseline gate.
- Tasks 3-5 are the game-flow gate; no portrait styling begins until pause ownership is green.
- Tasks 6-7 are the portrait presentation gate.
- Tasks 8-10 are the crowd/performance gate; Task 10 may retain only a documented no-pooling decision.
- Task 11 is the asset gate; no PixelLab generation occurs without a separate exact-asset approval.
- Task 12 is the release-evidence gate.

After each task: inspect `git diff`, run the listed focused tests, commit only that task's files, push, and report the commit hash plus test result before beginning the next task.
