# Jangseung and Geumjul Readability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the ambiguous Jangseung boundary and plain yellow Geumjul line with readable folk-fantasy installations whose anticipation, active state, and hit/closure events are visible on a mobile camera.

**Architecture:** Keep damage, crossing, and loop authority in `JangseungWardExecutor` and `FirstPlayableController`. Add focused presentation components that consume those existing results, use pooled sprites for repeated accents, and never create gameplay outcomes independently.

**Tech Stack:** Unity 6000.5.5f1, C#, URP 2D sprite rendering, LineRenderer with tiled texture, NUnit EditMode/PlayMode tests, PixelLab Pixen/animation assets.

## Global Constraints

- Preserve Jangseung damage, cooldown, radius, re-entry policy, evolution, and level-five movement rules.
- Preserve Geumjul trail sample count of 90 and the existing closure/damage calculation.
- Use one asset or one animation frame per PNG with transparent backgrounds and Point filtering.
- Pool repeated knots, dust, contact bursts, and seal effects; do not allocate a material or GameObject every frame.
- Keep persistent transparent coverage narrow for mobile overdraw.
- Do not modify or stage unrelated dirty Unity `.meta`, settings, generated scene, or font files.

---

### Task 1: Produce and import the folk-ward visual kit

**Files:**
- Create: `ArtSource/Pixel/Vfx/JangseungGeumjul/`
- Create: `Assets/JoseonHunter/Art/Vfx/JangseungGeumjul/`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/JangseungGeumjulAssetImporter.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/JangseungGeumjulVisualLibrary.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/JangseungGeumjulAssetTests.cs`

**Interfaces:**
- Produces: `JangseungGeumjulVisualLibrary` ScriptableObject with `GeumjulRopeTexture`, `GeumjulAnchor`, `GeumjulKnotVariants`, `GeumjulClosureFrames`, `JangseungDustFrames`, and `JangseungCrossingFrames`.
- Produces: `JangseungGeumjulAssetImporter.Rebuild()` editor entry point.
- Consumes: PixelLab outputs saved as individual PNG files.

- [ ] **Step 1: Write the failing asset contract test**

```csharp
[Test]
public void VisualLibraryContainsReadablePointFilteredAssets()
{
    var library = AssetDatabase.LoadAssetAtPath<JangseungGeumjulVisualLibrary>(
        "Assets/JoseonHunter/Content/Presentation/JangseungGeumjulVisualLibrary.asset");
    Assert.That(library, Is.Not.Null);
    Assert.That(library.GeumjulRopeTexture, Is.Not.Null);
    Assert.That(library.GeumjulAnchor, Is.Not.Null);
    Assert.That(library.GeumjulKnotVariants.Length, Is.GreaterThanOrEqualTo(2));
    Assert.That(library.GeumjulClosureFrames.Length, Is.EqualTo(6));
    Assert.That(library.JangseungDustFrames.Length, Is.EqualTo(4));
    Assert.That(library.JangseungCrossingFrames.Length, Is.EqualTo(4));

    var path = AssetDatabase.GetAssetPath(library.GeumjulRopeTexture);
    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
}
```

- [ ] **Step 2: Run the focused EditMode test and confirm the missing library failure**

Run:

```powershell
& $unityExe -batchmode -nographics -projectPath D:\UnityProjects\JoseonHunter -runTests -testPlatform EditMode -testFilter JoseonHunter.Tests.EditMode.JangseungGeumjulAssetTests -testResults Artifacts\jangseung-geumjul-assets-red.xml -logFile Logs\jangseung-geumjul-assets-red.log -quit
```

Expected: FAIL because the library asset and generated sprites do not exist.

- [ ] **Step 3: Generate the compact PixelLab source set**

Use Pixen at 64×64 or 96×64, transparent background, low top-down view, simplified Joseon folk-fantasy shapes, black outline, and flat/basic shading. Generate:

```text
tileable twisted straw ritual rope, warm ochre fibers, thick black pixel outline, tiny red cloth tie, no perspective, isolated
small wooden ritual rope anchor stake with red paper charm, Joseon folk fantasy, low top-down, isolated
small red paper charm knot tied around straw rope, two readable silhouette variants, isolated
compact golden circular sealing stamp wave with red paper fragments, centered, isolated
small dirt burst and red charm fragments from a wooden guardian post rising, centered, isolated
small fierce guardian-mask impact burst at a rope crossing, gold white and red, centered, isolated
```

Animate the seal, dirt, and impact bases into 6, 4, and 4 generated motion frames. Save the accepted frames under `ArtSource/Pixel/Vfx/JangseungGeumjul/` and copy each frame to the matching production folder as an individual PNG.

- [ ] **Step 4: Implement the visual library and deterministic importer**

```csharp
[CreateAssetMenu(menuName = "Joseon Hunter/Presentation/Jangseung Geumjul Visual Library")]
public sealed class JangseungGeumjulVisualLibrary : ScriptableObject
{
    [SerializeField] private Texture2D geumjulRopeTexture;
    [SerializeField] private Sprite geumjulAnchor;
    [SerializeField] private Sprite[] geumjulKnotVariants;
    [SerializeField] private Sprite[] geumjulClosureFrames;
    [SerializeField] private Sprite[] jangseungDustFrames;
    [SerializeField] private Sprite[] jangseungCrossingFrames;

    public Texture2D GeumjulRopeTexture => geumjulRopeTexture;
    public Sprite GeumjulAnchor => geumjulAnchor;
    public Sprite[] GeumjulKnotVariants => geumjulKnotVariants;
    public Sprite[] GeumjulClosureFrames => geumjulClosureFrames;
    public Sprite[] JangseungDustFrames => jangseungDustFrames;
    public Sprite[] JangseungCrossingFrames => jangseungCrossingFrames;
}
```

`JangseungGeumjulAssetImporter.Rebuild()` must set every PNG to Sprite/Single, alpha transparency, Point filtering, no mipmaps, clamp wrapping for accents, repeat wrapping for the rope tile, and uncompressed texture import. It then creates or updates the library asset at `Assets/JoseonHunter/Content/Presentation/JangseungGeumjulVisualLibrary.asset`.

- [ ] **Step 5: Run the asset test**

Expected: PASS with every frame present and correctly imported.

- [ ] **Step 6: Commit the asset kit**

```powershell
git add -- ArtSource/Pixel/Vfx/JangseungGeumjul Assets/JoseonHunter/Art/Vfx/JangseungGeumjul Assets/JoseonHunter/Scripts/Editor/AssetProduction/JangseungGeumjulAssetImporter.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/JangseungGeumjulVisualLibrary.cs Assets/JoseonHunter/Content/Presentation/JangseungGeumjulVisualLibrary.asset Assets/JoseonHunter/Tests/EditMode/JangseungGeumjulAssetTests.cs
git commit -m "feat: add jangseung and geumjul visual kit"
```

### Task 2: Replace the plain Geumjul line with a stateful presenter

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GeumjulTrailPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/GeumjulTrailPresenterTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePresentationPlayModeTests.cs`

**Interfaces:**
- Consumes: `JangseungGeumjulVisualLibrary`.
- Produces: `GeumjulTrailPresenter.Configure(library, root, sortingOrder)`.
- Produces: `SetTrail(IReadOnlyList<Vector2> points, float closureDistance)`, `PlayClosure(IReadOnlyList<Vector2> polygon)`, and `Clear()`.

- [ ] **Step 1: Write failing presenter state tests**

```csharp
[Test]
public void PresenterCapsPooledKnotsAndMarksClosureReadiness()
{
    var presenter = new GameObject("Geumjul").AddComponent<GeumjulTrailPresenter>();
    presenter.Configure(TestVisualLibrary.Create(), presenter.transform, 4);
    presenter.SetTrail(BuildTrail(90, .14f), .48f);

    Assert.That(presenter.ActiveKnotCountForTests, Is.LessThanOrEqualTo(18));
    Assert.That(presenter.HasAnchorForTests, Is.True);
    Assert.That(presenter.IsClosureReadyForTests, Is.True);
}
```

```csharp
[UnityTest]
public IEnumerator ClosureAnimationReturnsAllTemporarySpritesToPool()
{
    var presenter = CreatePresenter();
    presenter.PlayClosure(UnitSquare());
    yield return new WaitForSeconds(.8f);
    Assert.That(presenter.ActiveClosureVisualCountForTests, Is.Zero);
}
```

- [ ] **Step 2: Run the focused tests and confirm missing type failures**

Expected: FAIL because `GeumjulTrailPresenter` does not exist.

- [ ] **Step 3: Implement the presenter**

Use one persistent `LineRenderer` with a single cached material whose main texture is `GeumjulRopeTexture`. Set texture mode to tile, use a dark outline line behind a narrower rope line, and update only when the trail sample list changes.

```csharp
public void SetTrail(IReadOnlyList<Vector2> points, float closureDistance)
{
    UpdateLines(points);
    UpdateAnchor(points);
    UpdatePooledKnots(points, minimumWorldSpacing: .75f);
    SetClosureReady(points.Count >= 16 &&
        Vector2.Distance(points[0], points[points.Count - 1]) <= closureDistance);
    FadeOldSegments(points);
}
```

The presenter must keep at most 18 active knot sprites, pulse only the anchor when closure-ready, and play the six closure frames once at the polygon centroid before clearing.

- [ ] **Step 4: Integrate with the existing controller**

Replace the direct `geumjulRenderer` creation and position updates with:

```csharp
geumjulPresenter = new GameObject("Geumjul Presentation")
    .AddComponent<GeumjulTrailPresenter>();
geumjulPresenter.Configure(jangseungGeumjulVisuals, runtimeObjects, 4);
```

Call `SetTrail(trail, .48f)` after adding a point. In `TryCloseSeal`, call `PlayClosure(polygon)` only after the loop passes area validation and damage resolution, then clear the gameplay trail. Call `Clear()` during run reset and destruction.

- [ ] **Step 5: Wire the library in the scene generator**

Load `Assets/JoseonHunter/Content/Presentation/JangseungGeumjulVisualLibrary.asset` and assign it to the new serialized `jangseungGeumjulVisuals` field without hand-editing scene YAML.

- [ ] **Step 6: Run focused EditMode and PlayMode tests**

Expected: PASS; gameplay closure damage remains unchanged and presenter pools return to zero.

- [ ] **Step 7: Commit the Geumjul presenter**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/GeumjulTrailPresenter.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs Assets/JoseonHunter/Tests/EditMode/GeumjulTrailPresenterTests.cs Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePresentationPlayModeTests.cs
git commit -m "feat: make geumjul closure readable"
```

### Task 3: Make Jangseung installation and boundary crossings explicit

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungWardPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponExecutionContext.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: Jangseung positions, boundary segments, exact confirmed contact, and `JangseungGeumjulVisualLibrary`.
- Produces: `ShowSet`, `UpdateSet`, `PlayCrossing`, `RetireSet`, and `Clear` methods owned by `JangseungWardPresenter`.

- [ ] **Step 1: Extend the existing failing presentation tests**

```csharp
[Test]
public void JangseungCrossingPresentsOnlyTheConfirmedSegmentAndContact()
{
    var ward = CreateWardWithPresenter(out var presenter, out var target);
    ward.Tick(.02f, ContextAt(Float2.Zero));
    MoveAcrossRightBoundary(target);
    ward.Tick(.02f, ContextAt(Float2.Zero));

    Assert.That(presenter.CrossingCountForTests, Is.EqualTo(1));
    Assert.That(presenter.LastCrossingContactForTests.X, Is.GreaterThan(0f));
}
```

```csharp
[Test]
public void JangseungSetRetirementClearsPersistentPostsAndRopes()
{
    var ward = CreateWardWithPresenter(out var presenter, out _);
    ward.Tick(.02f, ContextAt(Float2.Zero));
    ward.Reset();
    Assert.That(presenter.ActiveSetCountForTests, Is.Zero);
}
```

- [ ] **Step 2: Run focused tests and confirm missing presenter behavior**

Expected: FAIL because no persistent set presenter or crossing callback exists.

- [ ] **Step 3: Implement the persistent presenter**

Each active set owns pooled post renderers, a tiled rope segment renderer for each boundary, a low-alpha center seal, and transient dust/crossing players.

```csharp
public void PlayCrossing(
    int setId,
    int segmentIndex,
    Vector2 start,
    Vector2 end,
    Vector2 contact)
{
    FlashOnly(segmentIndex, .12f);
    crossingPool.Play(library.JangseungCrossingFrames, contact, .04f);
}
```

Do not use a full-screen or opaque fill. Keep the center seal below combatants and the post faces above the rope. Retired sets return every renderer to a bounded pool.

- [ ] **Step 4: Route executor results to the presenter**

Create the persistent set after `PlaceSet`, update post positions after `MoveMobilePosts`, and invoke `PlayCrossing` only after `TryConfirmPixelContact` and `TryApply` both succeed. The existing damage request and crossing time remain the authoritative sequence.

Use the presentation library passed through `WeaponExecutionContext`; do not load assets through `Resources` from the executor.

- [ ] **Step 5: Preserve the current 14-frame weapon contract**

Continue using the existing five rise, four ward, and five strike frames for the main post art. The new library supplies only dust, tiled boundary, knot, and confirmed-crossing accents so `WeaponVisualPartIndex.Jangseung.RequiredCount` remains 14.

- [ ] **Step 6: Run Jangseung mechanics and eight-weapon regression tests**

Expected: PASS for confirmed boundary contact, re-entry, eviction, mobile reposition, frame ordering, and all eight weapon registrations.

- [ ] **Step 7: Commit the Jangseung presenter**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungWardPresenter.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponExecutionContext.cs Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs
git commit -m "feat: clarify jangseung boundary attacks"
```

### Task 4: Rebuild the playable scene and capture mobile readability

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/EightWeaponPolishCapture.cs`
- Create: `Docs/Verification/2026-07-30-jangseung-geumjul-readability.md`
- Create: `Logs/jangseung-geumjul-gameplay.png`

**Interfaces:**
- Consumes: completed visual library, Geumjul presenter, and Jangseung presenter.
- Produces: deterministic captures and verification evidence.

- [ ] **Step 1: Add capture checkpoints**

Add two deterministic capture cases:

```csharp
CaptureCase.JangseungCrossing
CaptureCase.GeumjulClosureReady
CaptureCase.GeumjulClosureImpact
```

The Jangseung case places one enemy just before and just after a visible boundary. The Geumjul cases feed a nearly closed polygon, capture the pulsing anchor, close it, and capture the seal frame.

- [ ] **Step 2: Rebuild scene-owned references**

Run the existing scene generator plus `JangseungGeumjulAssetImporter.Rebuild()` in batch mode. Confirm no user scene YAML is hand-edited.

- [ ] **Step 3: Run focused and regression validation**

Run the new asset/presenter tests, `GeumjulRuleTests`, Jangseung sections of `WeaponMechanicTests`, `EightWeaponCombatPlayModeTests`, and the existing combat sprite contract suite. Record exact pass/fail counts in the verification note.

- [ ] **Step 4: Capture and inspect**

Generate a portrait gameplay capture at the project’s target resolution. Verify:

- the Geumjul reads as straw rope and red charms at 1× Game view scale;
- the start anchor is visible without overpowering the player;
- the closure pulse clearly occupies the closed polygon;
- Jangseung posts remain legible and the rope boundaries stay thinner than a combatant;
- only the crossed segment flashes;
- no persistent effect remains after reset.

- [ ] **Step 5: Write verification evidence**

Document Unity version, command lines, test results, capture path, known unrelated dirty files, and any remaining baseline failure in `Docs/Verification/2026-07-30-jangseung-geumjul-readability.md`.

- [ ] **Step 6: Commit capture support and verification**

```powershell
git add -- Assets/JoseonHunter/Scripts/Editor/Scenes/EightWeaponPolishCapture.cs Docs/Verification/2026-07-30-jangseung-geumjul-readability.md Logs/jangseung-geumjul-gameplay.png
git commit -m "test: verify jangseung and geumjul readability"
```

## Self-Review

- Spec coverage: installation, persistent boundaries, exact crossing response, rope identity, anchor, closure readiness, completion, fade, cleanup, pooling, and mobile inspection are each assigned to a task.
- Placeholder scan: the plan contains no deferred implementation placeholders.
- Type consistency: the visual library feeds both presenters; gameplay owners remain unchanged; presenter method names are consistent across producer and consumers.
