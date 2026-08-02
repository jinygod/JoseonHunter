# Flat-Color Ward Readability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the noisy textured Geumjul and Jangseung ward presentation with continuous two-color lines, restrained pooled sparks, and a non-PNG closure fill while preserving combat behavior.

**Architecture:** Add a small shared flat-ward presentation layer containing the approved palette and a reusable diamond-spark pool. `GeumjulTrailPresenter` owns trail lines, a polygon fill mesh, and closure timing; `JangseungWardPresenter` owns procedural posts, paired boundary lines, rise/reposition alpha, and crossing sparks. `JangseungWardExecutor` continues to own all combat timing and geometry and only forwards rise/visibility state instead of spawning stretched field PNGs.

**Tech Stack:** Unity 6, C# runtime components, `LineRenderer`, `MeshFilter`/`MeshRenderer`, Unity Test Framework/NUnit, PowerShell test entry points.

## Global Constraints

- The default palette is exactly dark ink-brown outline plus muted ochre main color.
- Do not use white outlines, white rim lighting, rainbow colors, or multicolor gradients.
- Hit and closure emphasis changes only the ochre brightness and alpha for a short time.
- Do not change Geumjul closure rules, damage, duration, mastery branches, or retained point limits.
- Do not change Jangseung set count, boundary crossing rules, damage, re-entry rules, evolution, or level-five movement rules.
- Do not allocate new arrays, lists, meshes, or materials every frame.
- Preserve user-owned changes in `Assets/JoseonHunter/Scenes/Gameplay.unity`, `ProjectSettings/ProjectSettings.asset`, and the two MaruBuri SDF assets.

---

### Task 1: Shared two-color palette and pooled diamond sparks

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FlatWardVisualPalette.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FlatWardSparkPool.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/FlatWardVisualTests.cs`

**Interfaces:**
- Produces: `FlatWardVisualPalette.Outline`, `Main`, `MainBright`, `OutlineWidth`, and `MainWidth`.
- Produces: `FlatWardSparkPool(Transform root, Material material, int sortingOrder, int capacity)`, `PlayBurst(Vector2 origin, int count, float radius)`, `Tick(float deltaTime)`, `Clear()`, and `Dispose()`.
- Produces test properties `ActiveCountForTests`, `CreatedCountForTests`, and `UsesOnlyApprovedColorsForTests`.

- [ ] **Step 1: Write the failing palette and pool tests**

```csharp
[Test]
public void FlatWardPaletteUsesTwoNonWhiteBaseColors()
{
    Assert.That(FlatWardVisualPalette.Outline, Is.Not.EqualTo(Color.white));
    Assert.That(FlatWardVisualPalette.Main, Is.Not.EqualTo(Color.white));
    Assert.That(FlatWardVisualPalette.Outline, Is.Not.EqualTo(FlatWardVisualPalette.Main));
    Assert.That(FlatWardVisualPalette.MainBright.r, Is.LessThan(.95f));
}

[Test]
public void SparkPoolReusesEightSolidDiamondsAndReturnsThemToThePool()
{
    using var pool = new FlatWardSparkPool(root.transform, material, 4, 8);
    pool.PlayBurst(Vector2.zero, 8, .3f);
    Assert.That(pool.ActiveCountForTests, Is.EqualTo(8));
    Assert.That(pool.CreatedCountForTests, Is.EqualTo(8));
    Assert.That(pool.UsesOnlyApprovedColorsForTests, Is.True);
    pool.Tick(1f);
    pool.PlayBurst(Vector2.one, 3, .2f);
    Assert.That(pool.CreatedCountForTests, Is.EqualTo(8));
}
```

- [ ] **Step 2: Run the new test and verify red**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.FlatWardVisualTests
```

Expected: compilation fails because `FlatWardVisualPalette` and `FlatWardSparkPool` do not exist.

- [ ] **Step 3: Implement the shared palette and bounded pool**

Use immutable palette members and a single shared diamond mesh per pool:

```csharp
public static class FlatWardVisualPalette
{
    public static readonly Color Outline = new Color(.12f, .065f, .035f, .88f);
    public static readonly Color Main = new Color(.68f, .39f, .11f, .78f);
    public static readonly Color MainBright = new Color(.86f, .58f, .18f, .92f);
    public const float OutlineWidth = .105f;
    public const float MainWidth = .068f;
}
```

`FlatWardSparkPool` creates at most `capacity` mesh renderers, uses a four-vertex diamond mesh, assigns the caller-owned material, applies only `Main`/`MainBright`, advances deterministic radial velocities, fades by alpha, and disables expired objects. `Dispose` destroys the owned mesh and GameObjects but not the caller-owned material.

- [ ] **Step 4: Run the focused test and verify green**

Run the command from Step 2. Expected: all `FlatWardVisualTests` pass.

- [ ] **Step 5: Commit and push the shared primitive**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/FlatWardVisualPalette.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FlatWardSparkPool.cs Assets/JoseonHunter/Tests/EditMode/FlatWardVisualTests.cs
git commit -m "feat: add flat ward visual primitives"
git push origin master
```

### Task 2: Replace Geumjul texture, knots, and stamp closure

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GeumjulTrailPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/GeumjulTrailPresenterTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePresentationPlayModeTests.cs`

**Interfaces:**
- Consumes: `FlatWardVisualPalette` and `FlatWardSparkPool` from Task 1.
- Preserves: `Configure`, `SetTrail`, `PlayClosure`, `Clear`, `IsClosureReadyForTests`, and `CachedMaterialForTests`.
- Produces: `UsesTexturedRopeForTests`, `ActiveDecorativeKnotCountForTests`, `ClosureMeshVertexCountForTests`, `ClosureSparkCountForTests`, and `UsesLegacyClosureSpritesForTests`.

- [ ] **Step 1: Replace old presenter assertions with failing flat-style assertions**

```csharp
[Test]
public void PresenterUsesContinuousTwoColorLinesWithoutKnotsOrTexture()
{
    presenter.Configure(CreateVisualLibrary(), presenter.transform, 4);
    presenter.SetTrail(BuildTrail(90, .14f), .48f);
    Assert.That(presenter.UsesTexturedRopeForTests, Is.False);
    Assert.That(presenter.ActiveDecorativeKnotCountForTests, Is.Zero);
    Assert.That(presenter.HasAnchorForTests, Is.True);
    Assert.That(presenter.UsesOnlyApprovedLineColorsForTests, Is.True);
}

[Test]
public void ClosureUsesPolygonFillAndEightOchreSparksInsteadOfStampSprites()
{
    presenter.Configure(CreateVisualLibrary(), presenter.transform, 4);
    presenter.PlayClosure(UnitSquare());
    Assert.That(presenter.ClosureMeshVertexCountForTests, Is.EqualTo(4));
    Assert.That(presenter.ClosureSparkCountForTests, Is.EqualTo(8));
    Assert.That(presenter.UsesLegacyClosureSpritesForTests, Is.False);
}
```

Update the PlayMode pool-return test to assert `ClosureSparkCountForTests == 0` and `ClosureMeshVertexCountForTests == 0` after the closure duration.

- [ ] **Step 2: Run focused Geumjul tests and verify red**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GeumjulTrailPresenterTests
```

Expected: compilation fails on the new test properties.

- [ ] **Step 3: Implement continuous lines and a procedural start marker**

Remove `knotPool`, `closurePool`, rope texture assignment, and sprite-based anchor. Create solid untextured materials once in `Configure`, use `LineTextureMode.Stretch`, and apply `Outline`/`Main` with identical start and end colors. Represent the start marker as a five-point diamond `LineRenderer` using `Main`; keep the existing closure-ready pulse by scaling this marker only.

- [ ] **Step 4: Implement reusable closure mesh and spark animation**

Keep one `Mesh`, `MeshFilter`, `MeshRenderer`, fill vertex buffer, and triangle-index buffer on the presenter. Triangulate the simple polygon with an ear-clipping helper that supports clockwise and counter-clockwise concave polygons. During the existing closure coroutine, animate only fill alpha, boundary brightness, and eight pooled diamonds; never read `GeumjulClosureFrames`.

The closure cleanup must execute:

```csharp
closureMesh.Clear();
closureFill.gameObject.SetActive(false);
closureSparks.Clear();
ClosureMeshVertexCountForTests = 0;
```

- [ ] **Step 5: Run focused EditMode and PlayMode tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GeumjulTrailPresenterTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests
```

Expected: both filters pass and no closure sprite remains active.

- [ ] **Step 6: Commit and push the Geumjul replacement**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/GeumjulTrailPresenter.cs Assets/JoseonHunter/Tests/EditMode/GeumjulTrailPresenterTests.cs Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePresentationPlayModeTests.cs
git commit -m "feat: simplify geumjul presentation"
git push origin master
```

### Task 3: Replace Jangseung PNG boundaries with procedural ward geometry

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungWardPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: `FlatWardVisualPalette` and `FlatWardSparkPool` from Task 1.
- Preserves: `ShowSet`, `UpdateSet`, `PlayCrossing`, `Tick`, `RetireSet`, `Clear`, and `Dispose`.
- Produces: `SetPostRise(int setId, int postIndex, float progress)`, `SetBoundaryAlpha(int setId, int segmentIndex, float alpha)`, `UsesTexturedBoundariesForTests`, `ActiveDecorativeSpriteCountForTests`, `ActiveCrossingSparkCountForTests`, and `NewestSetHasFullEmphasisForTests`.

- [ ] **Step 1: Write failing procedural-ward tests**

Replace assertions for crossing/dust PNG frame indices and knot variants with:

```csharp
Assert.That(presenter.UsesTexturedBoundariesForTests, Is.False);
Assert.That(presenter.ActiveDecorativeSpriteCountForTests, Is.Zero);
Assert.That(presenter.ActiveCrossingSparkCountForTests, Is.EqualTo(3));
Assert.That(presenter.IsSegmentFlashingForTests(1, 0), Is.True);
Assert.That(Enumerable.Range(1, 3).All(i => !presenter.IsSegmentFlashingForTests(1, i)), Is.True);
```

Add a two-set test:

```csharp
presenter.ShowSet(1, squareA, postSprite);
presenter.ShowSet(2, squareB, postSprite);
Assert.That(presenter.NewestSetHasFullEmphasisForTests, Is.True);
Assert.That(presenter.SetMainAlphaForTests(1), Is.LessThan(presenter.SetMainAlphaForTests(2)));
```

Update rise tests to assert that `SetPostRise` and `SetBoundaryAlpha` expose the same staged progress without checking legacy windup/field sprites.

- [ ] **Step 2: Run the focused weapon tests and verify red**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter "JoseonHunter.Tests.EditMode.WeaponMechanicTests.Jangseung*|JoseonHunter.Tests.EditMode.WeaponMechanicTests.LevelFiveJangseung*"
```

Expected: compilation fails on the new presenter API and test properties.

- [ ] **Step 3: Replace persistent post, knot, seal, and rope visuals**

Inside each pooled `SetVisual`:

- replace `SpriteRenderer posts` with a procedural post made from a thick vertical ink-brown line and one short ochre crossbar;
- replace each textured rope with paired outline/main `LineRenderer`s using the shared widths and no texture;
- remove knot and center-seal renderers entirely;
- store post rise and per-boundary alpha arrays and update color alpha without allocating;
- track active-set insertion order so older sets use `0.34f` main alpha and the newest uses `0.78f`;
- on a level-five position update, reduce the set alpha and recover it over `0.12f` so relocation reads as re-establishment instead of a sliding sticker.

- [ ] **Step 4: Replace crossing sprite sequences with three pooled diamonds**

`PlayCrossing` must call:

```csharp
set.FlashOnly(segmentIndex, .12f);
crossingSparks.PlayBurst(contact, 3, .18f);
```

Delete crossing and dust `FrameSequence` collections and their PNG playback. Keep `CrossingCountForTests`, `LastCrossingContactForTests`, and one-segment flash behavior.

- [ ] **Step 5: Route executor rise state and remove stretched field PNG playback**

In `AdvanceWardPresentation`, replace windup and field calls to `transientVisuals.Play` with:

```csharp
wardPresenter?.SetPostRise(set.Attack.InstanceId, postIndex, rise);
wardPresenter?.SetBoundaryAlpha(set.Attack.InstanceId, directionIndex, alpha);
```

Continue populating `visibleBoundaryDirectionsForTests` and leave collision masks, crossing timestamps, damage calls, guardian/potential effects, and level-five `MoveMobilePosts` untouched.

- [ ] **Step 6: Run focused EditMode and eight-weapon PlayMode tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter "JoseonHunter.Tests.EditMode.WeaponMechanicTests.Jangseung*|JoseonHunter.Tests.EditMode.WeaponMechanicTests.LevelFiveJangseung*"
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests
```

Expected: boundary behavior, rise order, crossing order, retirement, and the nine eight-weapon cases pass.

- [ ] **Step 7: Commit and push the Jangseung replacement**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungWardPresenter.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs
git commit -m "feat: simplify jangseung ward presentation"
git push origin master
```

### Task 4: Regression, visual capture, and verification record

**Files:**
- Create: `Docs/Verification/2026-08-02-flat-color-ward-readability.md`
- Modify only if generated by approved tooling: no scene, font, or project-setting files.

**Interfaces:**
- Consumes: completed Geumjul and Jangseung presentation.
- Produces: reproducible test commands, counts, console result, capture paths, and known remaining visual issues.

- [ ] **Step 1: Run focused and full EditMode regression**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter "JoseonHunter.Tests.EditMode.FlatWardVisualTests|JoseonHunter.Tests.EditMode.GeumjulTrailPresenterTests|JoseonHunter.Tests.EditMode.WeaponMechanicTests|JoseonHunter.Tests.EditMode.GeumjulRuleTests"
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

Expected: focused filters and the full 559-plus-test suite pass with zero failures.

- [ ] **Step 2: Run related PlayMode regression**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter "JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests|JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests|JoseonHunter.Tests.PlayMode.EvolvedWeaponCombatPlayModeTests"
```

Expected: all selected related tests pass.

- [ ] **Step 3: Capture visual evidence in a clean Unity Editor session**

Open the project only after batch tests release the Unity lock. Capture at the normal portrait camera scale:

- an open Geumjul trail with at least four bends;
- a successful Geumjul closure during the fill/spark pulse;
- one Jangseung set with a crossing flash;
- overlapping old/new Jangseung sets;
- level-five Jangseung immediately after reposition.

Inspect each capture for white contours, texture seams, stretched sprites, more than two persistent base colors, and decorative images larger than their attack range.

- [ ] **Step 4: Write the verification record with exact evidence**

Record test totals and results, capture paths, console errors/warnings, and explicitly note that unrelated monsters and weapon art were audited but not silently redesigned without a concrete failing capture.

- [ ] **Step 5: Commit and push verification**

```powershell
git add -- Docs/Verification/2026-08-02-flat-color-ward-readability.md
git commit -m "docs: verify flat-color ward readability"
git push origin master
```

- [ ] **Step 6: Confirm repository handoff state**

Run `git status --short`, `git log -5 --oneline`, and `git rev-parse origin/master`. Confirm only the four pre-existing user-owned files and `.utmp/` remain outside commits and that `HEAD` equals `origin/master`.

