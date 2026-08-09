# Gameplay Hybrid Scene Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the production Gameplay scene directly editable by authoring Han Yeonhwa, camera, field, runtime/spawn roots, and UI ownership in the Scene while preserving dynamic hordes, pooling, combat, progression, and save behavior.

**Architecture:** Add a serialized `GameplaySceneComposition` for stable scene ownership, a `GameplayBattlefieldHost` for stage presentation under a stable field root, and a `GameplayVisualFactory` for prefab binding/creation. `FirstPlayableController` remains the run/combat authority but delegates composition-facing work. The existing scene generator becomes non-destructive and authors the production hierarchy through Unity Editor APIs.

**Tech Stack:** Unity 6000.5.5f1, C# runtime/editor assemblies, Unity Test Framework 1.7.0, uGUI, Input System 1.20.0, URP 2D, Android ARM64 IL2CPP.

## Global Constraints

- Work in `D:\UnityProjects\JoseonHunter`; preserve all unrelated Lobby, art, font, capture, and metadata changes already present in the working tree.
- Do not add packages or change Project Settings, save schemas, stage balance, spawn budgets, weapon behavior, progression values, or public player-facing strings.
- Preserve existing `FirstPlayableController` public APIs, events, serialized field names, and `UNITY_INCLUDE_TESTS` seams.
- Preserve the compatibility path `FirstPlayable/RuntimeObjects/Han Yeonhwa`.
- Stable authored roots and Han Yeonhwa must survive repeated run resets; enemies, projectiles, hazards, treasure, and pickups remain runtime-created or pooled.
- The Main Camera Inspector configuration is authoritative when the authored composition is valid; legacy camera creation remains as a fallback.
- Editor generation must use Unity APIs, be idempotent, preserve valid transforms/prefab links, and refuse a dirty loaded Gameplay scene.
- The existing `GameplayVisualPreview` remains excluded from Build Settings.
- Every production change follows RED → GREEN → REFACTOR and includes the exact failing and passing test evidence.
- Unity batch processes run sequentially at BelowNormal priority with limited logical cores when practical; do not run more than one Unity process for this project at once.
- Commit only intended files for each task and push every completed commit to `origin/master` as requested by the user.

---

## File Structure

### New runtime files

- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySceneComposition.cs`: stable scene references, authored pose capture/restore, reset-scoped child cleanup.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySpawnGuide.cs`: Scene gizmo for the moving-camera spawn perimeter; no gameplay authority.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayBattlefieldHost.cs`: stage-specific infinite/bounded presentation beneath a stable root.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayVisualFactory.cs`: authored player binding plus enemy, pickup, and world-bar prefab/fallback construction.

### Modified runtime/editor files

- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`: resolve authored composition and delegate field/visual construction.
- `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`: keep scene-authored root as primary and runtime initialization as fallback without duplication.
- `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`: non-destructive production Gameplay authoring and open/validate menus.
- `Assets/JoseonHunter/Scenes/Gameplay.unity`: authored stable hierarchy and serialized references.

### New tests and documentation

- `Assets/JoseonHunter/Tests/EditMode/GameplaySceneCompositionTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/GameplayBattlefieldHostTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/GameplaySceneAuthoringContractTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/GameplayHybridSceneOwnershipPlayModeTests.cs`
- `Docs/GameplaySceneAuthoring.md`
- `Docs/Verification/2026-08-09-gameplay-hybrid-scene-refactor.md`

---

### Task 1: Stable Scene Composition and Authored Pose

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySceneComposition.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySceneComposition.cs.meta`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySpawnGuide.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySpawnGuide.cs.meta`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameplaySceneCompositionTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameplaySceneCompositionTests.cs.meta`

**Interfaces:**
- Consumes: `CombatantVisualView`, `Camera`, stable hierarchy Transforms.
- Produces:

```csharp
public sealed class GameplaySceneComposition : MonoBehaviour
{
    public Camera GameplayCamera { get; }
    public Transform BattlefieldRoot { get; }
    public Transform RuntimeObjectsRoot { get; }
    public Transform RuntimeSystemsRoot { get; }
    public Transform SpawnGuidesRoot { get; }
    public CombatantVisualView AuthoredPlayer { get; }
    public GameObject UiRoot { get; }
    public bool IsComplete { get; }

    public void Configure(
        Camera camera,
        Transform battlefieldRoot,
        Transform runtimeObjectsRoot,
        Transform runtimeSystemsRoot,
        Transform spawnGuidesRoot,
        CombatantVisualView authoredPlayer,
        GameObject uiRoot);
    public void CaptureAuthoredState();
    public void RestoreAuthoredState();
    public void ClearRunScopedChildren();
}
```

```csharp
public sealed class GameplaySpawnGuide : MonoBehaviour
{
    public void Configure(Camera camera, float minimumMargin, float maximumMargin);
}
```

- [ ] **Step 1: Write the failing composition tests**

Create real GameObjects and assert these observable contracts:

```csharp
[Test]
public void CompleteCompositionPreservesStableRootsAndRestoresAuthoredCameraAndPlayerPose()
{
    // Configure a non-default camera/player pose, capture, mutate both,
    // add transient children, clear/restore, then assert the original
    // instance IDs and literal poses are restored and transient children are gone.
}

[Test]
public void IncompleteOrCrossHierarchyReferencesAreRejected()
{
    // A player outside RuntimeObjects and a root from another Scene must make IsComplete false.
}
```

The production mutation caught is deleting a stable root/player during reset or accepting a reference from the wrong ownership hierarchy.

- [ ] **Step 2: Run the tests and verify RED**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameplaySceneCompositionTests
```

Expected: compilation/test failure because `GameplaySceneComposition` and `GameplaySpawnGuide` do not exist.

- [ ] **Step 3: Implement the minimal composition**

Use private serialized references, validate same-Scene ownership and the exact player-under-`RuntimeObjects` relationship, capture state only once, and clear every runtime child except the authored player. Use `Destroy` in Play Mode and `DestroyImmediate` outside Play Mode. Restore the captured player local position/rotation/scale/active state and camera world position/rotation.

`GameplaySpawnGuide.OnDrawGizmosSelected()` draws the camera viewport rectangle plus minimum/maximum margins. It must not alter spawn calculations or execute per-frame gameplay code.

- [ ] **Step 4: Run GREEN and refactor**

Run the same fixture. Expected: all composition tests pass with zero failures. Then run `git diff --check` for the new source and test files.

- [ ] **Step 5: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySceneComposition.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySceneComposition.cs.meta Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySpawnGuide.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplaySpawnGuide.cs.meta Assets/JoseonHunter/Tests/EditMode/GameplaySceneCompositionTests.cs Assets/JoseonHunter/Tests/EditMode/GameplaySceneCompositionTests.cs.meta
git commit -m "feat: add stable gameplay scene composition"
git push origin master
```

---

### Task 2: Stable Battlefield Host

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayBattlefieldHost.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayBattlefieldHost.cs.meta`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameplayBattlefieldHostTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameplayBattlefieldHostTests.cs.meta`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs:83-87,967-1029,1069,1270-1274,1455-1468`

**Interfaces:**
- Consumes: `StageId`, `StageBattlefieldDefinition`, `StagePresentationCatalog`, `BattlefieldPresentationLibrary`, `BattlefieldTilePresenter`, `BoundedBattlefieldPresenter`.
- Produces:

```csharp
public sealed class GameplayBattlefieldHost : MonoBehaviour
{
    public StageId PresentedStageId { get; }
    public bool IsBuilt { get; }
    public bool HasBoundedBounds { get; }
    public Rect BoundedBounds { get; }
    public Transform RuntimeRoot { get; }

    public void ConfigureAuthoringRoots(Transform runtimeRoot, GameObject authoringPreviewRoot);
    public void ConfigureForStage(
        StageId stageId,
        StageBattlefieldDefinition battlefield,
        StagePresentationCatalog stagePresentationCatalog,
        BattlefieldPresentationLibrary presentation,
        Sprite fallbackSprite,
        int seed);
    public void Track(Vector2 playerPosition);
}
```

- [ ] **Step 1: Write the failing host tests**

```csharp
[Test]
public void ReconfiguringStageKeepsHostAndRuntimeRootIdentityWhileReplacingGeneratedPresentation()
{
    // Configure unbounded, capture host/runtime IDs, configure bounded,
    // assert IDs unchanged, exactly one active presenter kind, and bounded bounds are available.
}

[Test]
public void InfiniteHostTracksAcrossChunksWithoutCreatingASecondRuntimeRoot()
{
    // Configure GwigokField, Track beyond one chunk, assert nine active chunks and one runtime root.
}
```

The production mutation caught is returning to `Destroy(FlatField)` or allowing both presenter types to remain active.

- [ ] **Step 2: Run the fixture and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameplayBattlefieldHostTests
```

Expected: failure because `GameplayBattlefieldHost` is absent.

- [ ] **Step 3: Implement host and delegate controller field work**

Move the asset-selection/configuration branches from `CreateField()` into `GameplayBattlefieldHost.ConfigureForStage()`. Generated visuals live under `RuntimeRoot`; `authoringPreviewRoot` is disabled during Play. Deactivate old generated objects before destroying them so scene-authored preview geometry cannot flash for one frame.

Replace controller fields `flatField`, `battlefieldPresenter`, `boundedBattlefieldPresenter`, `presentedStageId`, and `fieldBuilt` with one `GameplayBattlefieldHost battlefieldHost`. Keep a legacy fallback that creates `FlatField`, `Runtime Battlefield`, and the host when no complete composition exists. `UpdateField()` delegates to `Track()`. Bounded spawn selection reads `HasBoundedBounds` and `BoundedBounds`.

- [ ] **Step 4: Verify GREEN and existing geometry behavior**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameplayBattlefieldHostTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.Battlefield
```

Expected: both fixtures pass; no new Console errors.

- [ ] **Step 5: Commit and push**

Stage only the host, its tests/meta, and `FirstPlayableController.cs`. Commit `refactor: extract gameplay battlefield host`, then push `master`.

---

### Task 3: Visual Factory and Authored Player Reuse

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayVisualFactory.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayVisualFactory.cs.meta`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs:3770-4020`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/GameplayVisualPrefabPlayModeTests.cs`

**Interfaces:**
- Consumes: `GameplayVisualPrefabLibrary`, `CombatMotionLibrary`, shared solid sprite, one-time warning callback.
- Produces:

```csharp
public enum GameplayPickupVisualKind { Experience, Yeopjeon, Magnet }

public sealed class GameplayVisualFactory
{
    public GameplayVisualFactory(
        GameplayVisualPrefabLibrary library,
        CombatMotionLibrary motionLibrary,
        Sprite solidSprite,
        Action<string, string> warnOnce);

    public GameObject BindAuthoredCombatant(
        GameObject root, string objectName, Sprite sprite, int sortingOrder,
        MotionWeight weight, float phaseOffset, out CombatantVisualRig visualRig,
        CombatantVisualRole role);
    public GameObject CreateCombatant(
        string objectName, Sprite sprite, Vector2 position, int sortingOrder,
        Transform parent, MotionWeight weight, float phaseOffset,
        out CombatantVisualRig visualRig, CombatantVisualRole role);
    public GameObject CreatePickup(
        GameplayPickupVisualKind kind, string objectName, Sprite sprite,
        Vector2 position, Transform parent, out PickupVisualView pickupView);
    public Transform CreateHealthBar(
        Transform owner, Vector3 fallbackLocalPosition, float fallbackLocalScale,
        bool overrideAuthoredAnchor = false);
    public Transform CreateShieldBar(
        Transform owner, Vector3 fallbackLocalPosition, float fallbackLocalScale);
    public static void UpdateBarFill(Transform fill, float normalizedValue, float width, float height);
}
```

- [ ] **Step 1: Add RED PlayMode tests to the existing prefab fixture**

```csharp
[UnityTest]
public IEnumerator AuthoredPlayerAndNestedHealthBarAreReusedWithoutInstantiation()
{
    // Instantiate PlayerVisual and WorldHealthBar once, call the factory,
    // assert identical root/bar instance IDs and exactly one renderer/bar.
}

[UnityTest]
public IEnumerator FactoryStillCreatesEnemyAndReusesExperiencePickupTrailContract()
{
    // Create real enemy/pickup visuals and assert prefab bindings and root TrailRenderer.
}
```

The production mutation caught is always instantiating a second player or health bar and breaking pooling/trail behavior during extraction.

- [ ] **Step 2: Run the fixture and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.GameplayVisualPrefabPlayModeTests
```

Expected: failure because `GameplayVisualFactory` and authored reuse do not exist.

- [ ] **Step 3: Implement the factory and replace controller helpers**

Move prefab validation, binding, pickup creation, world-bar creation, and bar-fill logic without changing existing warning text. `BindAuthoredCombatant()` requires a valid root `CombatantVisualView` and never instantiates. `CreateHealthBar()` first reuses one valid direct `WorldBarView` under the chosen anchor; only otherwise does it instantiate the library prefab or use the legacy fallback.

Keep generic non-prefab `CreateSpriteObject()` in the controller for unrelated projectiles/chest/VFX. Map the controller's private `PickupKind` to `GameplayPickupVisualKind` at the call site. Construct one factory after resolving the visual library and shared sprite, then reuse it for the run.

- [ ] **Step 4: Verify GREEN and unchanged prefab fallbacks**

Run the full `GameplayVisualPrefabPlayModeTests` fixture. Expected: all old and new tests pass, including exact invalid-player/bar/pickup warning assertions, boss shadow role scale, bar geometry, and pickup pooling.

- [ ] **Step 5: Commit and push**

Stage only the factory/meta, controller, and modified prefab PlayMode tests. Commit `refactor: extract gameplay visual factory`, then push `master`.

---

### Task 4: Non-Destructive Authored Gameplay Scene

**Files:**
- Create: `Assets/JoseonHunter/Tests/EditMode/GameplaySceneAuthoringContractTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameplaySceneAuthoringContractTests.cs.meta`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/SceneScaffoldTests.cs`
- Generate through Unity Editor API: `Assets/JoseonHunter/Scenes/Gameplay.unity`

**Interfaces:**
- Consumes: production visual prefabs/library, `GameplaySceneComposition`, `GameplayBattlefieldHost`, `FirstPlayableUiBootstrap`, existing sprite/catalog assets.
- Produces:

```csharp
[MenuItem("JoseonHunter/Gameplay Editing/Open Authored Gameplay Scene")]
public static void OpenAuthoredGameplayScene();

[MenuItem("JoseonHunter/Gameplay Editing/Create or Validate Authored Gameplay Scene")]
public static void CreateOrValidateAuthoredGameplayScene();

public static void Generate();
public static void GenerateInBatchMode();
```

- [ ] **Step 1: Write RED authored-scene contracts**

Open `Gameplay.unity` additively and assert:

```csharp
Assert.That(FindSingleRoot("Main Camera"), Is.Not.Null);
Assert.That(FindSingleRoot("FirstPlayable"), Is.Not.Null);
Assert.That(FindSingleRoot("First Playable UI"), Is.Not.Null);
Assert.That(FindSingleRoot("EventSystem"), Is.Not.Null);
```

Then assert one complete `GameplaySceneComposition`, children `FlatField`, `RuntimeObjects`, `RuntimeSystems`, `Spawn Guides`, connected `PlayerVisual` at `RuntimeObjects/Han Yeonhwa`, connected `WorldHealthBar` below its health anchor, one `GameplayBattlefieldHost`, one UI bootstrap, one EventSystem/input module, and serialized controller composition/library references. Assert exact Build Settings order remains Bootstrap/Lobby/Gameplay.

Add generator tests that record literal transforms and prefab asset hashes, invoke validation twice, and assert they are unchanged. Mark a loaded Gameplay scene dirty, invoke validation, and assert `InvalidOperationException` while the dummy object and dirty state remain.

The production mutation caught is the current generator deleting every root or creating duplicate player/UI objects.

- [ ] **Step 2: Run the new fixture and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameplaySceneAuthoringContractTests
```

Expected: hierarchy/reference failures because the production Gameplay scene is still minimal.

- [ ] **Step 3: Implement the non-destructive generator**

Replace the root-deletion loop. If the Gameplay scene is loaded and dirty, throw before any asset or scene write. Find exactly one object by expected name or create it; throw on duplicates. Preserve existing valid transforms and prefab links. Add missing components/references only.

Instantiate `PlayerVisual.prefab` as a connected instance named `Han Yeonhwa` under `RuntimeObjects`. Instantiate `WorldHealthBar.prefab` as a connected nested instance named `Health Bar` under `CombatantVisualView.HealthBarAnchor`. Create `FlatField/Authoring Preview` tagged `EditorOnly` and `FlatField/Runtime Battlefield`; build a 3×3 default-stage visual preview from the production battlefield chunk prefab without changing gameplay data. Configure composition, host, spawn guide, controller serialized fields, scene-authored UI bootstrap, GameFlowCoordinator, camera, EventSystem, and InputSystemUIInputModule.

`FirstPlayableUiBootstrap.EnsureBootstrap()` keeps its fallback but finds the scene-authored component with inactive objects included before creating a root.

- [ ] **Step 4: Generate the scene through Unity and verify GREEN**

Run Unity sequentially:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UnityProjects\JoseonHunter' -executeMethod JoseonHunter.Editor.Scenes.FirstPlayableSceneGenerator.GenerateInBatchMode -logFile 'Logs\hybrid-scene-generate.log'
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameplaySceneAuthoringContractTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.SceneScaffoldTests
```

Expected: generation exit 0 and both fixtures pass. Inspect the scene diff for only intended hierarchy/reference changes and scan YAML for `m_Script: {fileID: 0}`.

- [ ] **Step 5: Commit and push**

Stage only generator, UI bootstrap, scene, scene/scaffold tests, and their metadata. Commit `feat: author stable gameplay scene hierarchy`, then push `master`.

---

### Task 5: Controller Integration and Reset Identity

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs:21-180,816-825,928-943,1031-1201,1247-1284,3770-4020`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GameplayHybridSceneOwnershipPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GameplayHybridSceneOwnershipPlayModeTests.cs.meta`
- Modify when required by preserved-path assertions: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePresentationPlayModeTests.cs`
- Modify when required by authored player assertions: `Assets/JoseonHunter/Tests/PlayMode/GameplayVisualPrefabPlayModeTests.cs`

**Interfaces:**
- Consumes: complete `GameplaySceneComposition`, `GameplayBattlefieldHost`, `GameplayVisualFactory` from Tasks 1–3.
- Produces: authored-first initialization with legacy runtime fallback and unchanged public controller/event APIs.

- [ ] **Step 1: Write RED production-scene ownership tests**

```csharp
[UnityTest]
public IEnumerator ResetRunPreservesAuthoredCameraFieldRuntimeRootsPlayerAndUiIdentity()
{
    // Load Gameplay, capture instance IDs and literal authored poses,
    // mutate player/camera, call ResetRunForTests twice, yield frames,
    // assert the same IDs and original poses, one of each stable object, and no duplicate bar/canvas.
}

[UnityTest]
public IEnumerator RuntimeEnemiesAndPickupsRemainTransientWhileAuthoredPlayerAndPickupPoolContractsSurvive()
{
    // Spawn a real enemy and XP pickup, assert descendants of RuntimeObjects but not authored player,
    // collect/reuse pickup, reset, then assert transient content cleared and authored IDs unchanged.
}

[UnityTest]
public IEnumerator MissingCompositionUsesLegacyRuntimeFallbackWithoutBreakingDirectControllerCreation()
{
    // Add FirstPlayableController to a standalone GameObject and assert one runtime player/camera/field.
}
```

The production mutation caught is deleting/reinstantiating authored objects, overwriting camera authoring, or removing legacy direct-scene support.

- [ ] **Step 2: Run the fixture and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.GameplayHybridSceneOwnershipPlayModeTests
```

Expected: stable identity/start-pose assertions fail because the controller still destroys `RuntimeObjects`, recreates Han Yeonhwa, and resets camera values.

- [ ] **Step 3: Integrate authored composition**

Add `[SerializeField] private GameplaySceneComposition sceneComposition;` without renaming existing fields. In `Awake()`, resolve `GetComponent<GameplaySceneComposition>()`, call `CaptureAuthoredState()`, resolve camera, create shared render assets, and reset the run.

When the composition is complete:

- use its camera without overwriting Inspector projection, size, clear flags, background, or initial pose;
- call `ClearRunScopedChildren()` and `RestoreAuthoredState()` instead of deleting stable roots;
- set `runtimeObjects` to the authored root and parent reset-scoped presenters/pools under `RuntimeSystemsRoot`;
- bind `AuthoredPlayer.gameObject` through `GameplayVisualFactory.BindAuthoredCombatant()`;
- reuse the authored health bar;
- use the authored `GameplayBattlefieldHost` and leave `FlatField` identity intact.

When incomplete, execute the current legacy creation path with the same default camera values, names, parents, and warnings. Keep `GameplayReadySignal.MarkReady()` after both paths finish.

- [ ] **Step 4: Verify focused GREEN and existing behavior**

Run sequentially:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.GameplayHybridSceneOwnershipPlayModeTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.GameplayVisualPrefabPlayModeTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.FirstPlayablePickupRangePlayModeTests
```

Expected: all fixtures pass, exact old public paths remain, no duplicate player/bar/UI/EventSystem, and fallback warnings remain unchanged.

- [ ] **Step 5: Refactor only after GREEN**

Remove now-unused controller scene/field/visual helper fields and methods. Do not move combat, wave, save, settlement, or balance logic. Search every removed member with `rg` and keep any public/test seam still consumed.

- [ ] **Step 6: Commit and push**

Stage only controller and hybrid/adjusted PlayMode tests. Commit `refactor: bind gameplay to authored scene composition`, then push `master`.

---

### Task 6: Authoring Guide, Full Validation, and Final Review

**Files:**
- Create: `Docs/GameplaySceneAuthoring.md`
- Create: `Docs/Verification/2026-08-09-gameplay-hybrid-scene-refactor.md`
- Modify only if verification finds a covered regression: files already listed in Tasks 1–5 and their direct tests.

**Interfaces:**
- Consumes: completed production scene, menus, prefabs, tests, and Android build pipeline.
- Produces: beginner workflow documentation and exact validation evidence.

- [ ] **Step 1: Write the authoring guide**

Document:

- open `Gameplay.unity` or use `JoseonHunter/Gameplay Editing/Open Authored Gameplay Scene`;
- move `FirstPlayable/RuntimeObjects/Han Yeonhwa` to change the starting position;
- edit `PlayerVisual`, `EnemyVisual`, and world-bar prefabs in Prefab Mode, then Apply;
- camera Inspector ownership and follow behavior;
- `FlatField/Authoring Preview` versus runtime battlefield children;
- stable versus reset-scoped hierarchy;
- why enemies/projectiles/pickups remain dynamic;
- Play Mode changes are temporary unless applied to a prefab or edited before Play;
- recovery through `Create or Validate Authored Gameplay Scene` and its dirty-scene refusal.

- [ ] **Step 2: Run focused EditMode and PlayMode suites fresh**

Run the new composition, host, scene contract, hybrid ownership, visual prefab, presentation, pickup, and scaffold fixtures sequentially. Copy XML/log files to `Artifacts/GameplayHybridScene/` after each run.

- [ ] **Step 3: Run full suites fresh**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode
```

Compare failures to the recorded baseline: full PlayMode previously had one unrelated Lobby difficulty-card skin failure. Any new failure is a regression and must be fixed through a new RED/GREEN cycle before proceeding.

- [ ] **Step 4: Run Android build and serialized-reference audit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1
```

Record APK path, bytes, timestamp, and SHA-256. Scan modified `.unity`, `.prefab`, and `.asset` files for missing scripts, inspect Console errors, verify Build Settings order, and confirm the Preview scene remains disabled/excluded.

- [ ] **Step 5: Perform visual/manual acceptance**

Open the production Gameplay scene, record the visible authored hierarchy, move Han Yeonhwa to a temporary non-default position, enter Play, and verify that exact start position. Exit without saving the temporary move. Verify camera framing, field preview/runtime transition, HUD uniqueness, horde/pickup generation, pause, and reset restoration. Capture a portrait Gameplay screenshot for the verification report without committing generated dynamic font atlas changes.

- [ ] **Step 6: Request final code review and address findings once**

Review the complete feature diff against the design acceptance criteria. Any Critical/Important finding receives one consolidated fix wave with focused test reruns and a scoped re-review. Record non-blocking limitations explicitly.

- [ ] **Step 7: Commit and push final documentation/fixes**

Stage only feature documentation and reviewed direct fixes. Commit `docs: verify authored gameplay scene workflow`, push `master`, then verify local and remote HEAD hashes match while unrelated working-tree changes remain unstaged.

