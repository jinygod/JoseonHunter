# Infinite Battlefield, Wave Roster, and Loading Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a deterministic nine-chunk infinite pixel battlefield, phase-authored enemy rosters and packs up to 140 active enemies, and a real Bootstrap-to-Gameplay loading transition.

**Architecture:** Keep deterministic rules in `JoseonHunter.Domain`, Unity object recycling and spawn integration in `JoseonHunter.Runtime`, and loading UI in `JoseonHunter.Presentation`. A Resources-loaded presentation library references inspectable prefabs without modifying the user's dirty Gameplay scene. Runtime entities remain dynamic or pooled.

**Tech Stack:** Unity 6000.5.5f1, C# 9, URP 2D, Unity Input System, uGUI/TextMesh Pro, NUnit EditMode and PlayMode tests, Android ARM64 IL2CPP.

## Global Constraints

- The portrait Gameplay camera keeps orthographic size 18.
- Maintain exactly nine reusable 32-by-32-world-unit battlefield chunks.
- Ordinary movement inside one chunk must allocate no managed memory and create no GameObjects.
- Active enemies from continuous and pack spawning share a hard ceiling of 140.
- Wave boundaries are 0, 45, 90, 135, 165, and 180 seconds.
- Wave one normal enemies are 100% `plague_rat`.
- Do not change weapon attack behavior, the optimized exact pixel-contact path, target registration order, save data, packages, input bindings, or URP settings.
- Preserve the user's uncommitted `Assets/JoseonHunter/Scenes/Gameplay.unity` and `ProjectSettings/ProjectSettings.asset` changes.
- Use point filtering, no mipmaps, and lossless/uncompressed pixel import settings for the new ground art.
- Every production behavior starts with a failing automated test and a verified RED result.
- Commit and push each completed task to `origin/master`.

---

### Task 1: Authored wave roster and deterministic pack rules

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/RunClock.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs`
- Create: matching `.meta` file for `WaveSpawnDirector.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/RunRuleTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs`
- Create: matching `.meta` file for `WaveSpawnDirectorTests.cs`

**Interfaces:**
- Produces: `RunClock.PhaseAt(float elapsedSeconds) : RunPhase`
- Produces: `WeightedEnemyEntry(string contentId, int weight)`
- Produces: `WavePackDefinition(IReadOnlyList<string> contentIds, int minimumSize, int maximumSize, float minimumIntervalSeconds, float maximumIntervalSeconds)`
- Produces: `WaveDefinition.ActiveCap`, `WeightedContent`, and optional `Pack`
- Produces: `WaveSpawnDirector(int seed)`, `Reset()`, `SelectNormal(RunPhase)`, and `TryCreatePack(float elapsedSeconds, int availableSlots, out EnemyPackPlan plan)`

- [ ] **Step 1: Update the existing rule tests to the approved boundaries, caps, and weights**

```csharp
[TestCase(RunPhase.WaveOne, 72)]
[TestCase(RunPhase.WaveTwo, 104)]
[TestCase(RunPhase.WaveThree, 128)]
[TestCase(RunPhase.Peak, 140)]
public void WaveScheduleUsesApprovedActiveCaps(RunPhase phase, int expected) =>
    Assert.That(WaveSchedule.For(phase).ActiveCap, Is.EqualTo(expected));

[Test]
public void OpeningWaveContainsOnlyPlagueRats()
{
    var wave = WaveSchedule.For(RunPhase.WaveOne);
    Assert.That(wave.WeightedContent, Has.Count.EqualTo(1));
    Assert.That(wave.WeightedContent[0].ContentId, Is.EqualTo("plague_rat"));
    Assert.That(wave.WeightedContent[0].Weight, Is.EqualTo(100));
}
```

- [ ] **Step 2: Add deterministic selection and pack-bound tests**

```csharp
[Test]
public void SameSeedProducesSameNormalAndPackSequence()
{
    var left = new WaveSpawnDirector(1701);
    var right = new WaveSpawnDirector(1701);
    Assert.That(Enumerable.Range(0, 32).Select(_ => left.SelectNormal(RunPhase.WaveTwo)),
        Is.EqualTo(Enumerable.Range(0, 32).Select(_ => right.SelectNormal(RunPhase.WaveTwo))));
    Assert.That(left.TryCreatePack(60f, 20, out var a), Is.True);
    Assert.That(right.TryCreatePack(60f, 20, out var b), Is.True);
    Assert.That((a.ContentId, a.Count, a.Side), Is.EqualTo((b.ContentId, b.Count, b.Side)));
}

[Test]
public void PackCountNeverExceedsDefinitionOrAvailableSlots()
{
    var director = new WaveSpawnDirector(9);
    Assert.That(director.TryCreatePack(60f, 11, out var plan), Is.True);
    Assert.That(plan.Count, Is.InRange(10, 11));
}
```

- [ ] **Step 3: Run the focused EditMode tests and verify RED**

Run:
`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter "JoseonHunter.Tests.EditMode.RunRuleTests;JoseonHunter.Tests.EditMode.WaveSpawnDirectorTests"`

Expected: FAIL because approved caps, weighted roster APIs, and `WaveSpawnDirector` do not exist.

- [ ] **Step 4: Implement the minimal domain model**

```csharp
public readonly struct WeightedEnemyEntry
{
    public WeightedEnemyEntry(string contentId, int weight)
    {
        if (string.IsNullOrWhiteSpace(contentId)) throw new ArgumentException(nameof(contentId));
        if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
        ContentId = contentId;
        Weight = weight;
    }
    public string ContentId { get; }
    public int Weight { get; }
}

public static RunPhase PhaseAt(float seconds)
{
    if (!float.IsFinite(seconds) || seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
    if (seconds >= 180f) return RunPhase.Boss;
    if (seconds >= 165f) return RunPhase.BossWarning;
    if (seconds >= 135f) return RunPhase.Peak;
    if (seconds >= 90f) return RunPhase.WaveThree;
    if (seconds >= 45f) return RunPhase.WaveTwo;
    return RunPhase.WaveOne;
}
```

Implement `WaveSpawnDirector` with one private `System.Random`, a next-pack timestamp, and a pack ordinal. Clamp every pack to `availableSlots`; return false for boss warning, boss, expired, or non-positive capacity.

- [ ] **Step 5: Run the focused tests and verify GREEN**

Expected: all `RunRuleTests` and `WaveSpawnDirectorTests` pass with zero failures.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Runs Assets/JoseonHunter/Tests/EditMode
git commit -m "feat: author enemy wave rosters"
git push origin master
```

---

### Task 2: Infinite chunk layout and recycling presenter

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BattlefieldChunkLayout.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BattlefieldChunkView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BattlefieldTilePresenter.cs`
- Create: matching `.meta` files
- Create: `Assets/JoseonHunter/Tests/EditMode/BattlefieldChunkLayoutTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/BattlefieldTilePresenterPlayModeTests.cs`
- Create: matching `.meta` file for the new test

**Interfaces:**
- Produces: `BattlefieldChunkLayout.ChunkSize = 32f`
- Produces: `BattlefieldChunkLayout.CoordinateAt(Vector2 worldPosition) : Vector2Int`
- Produces: `BattlefieldChunkLayout.FillRequired(Vector2Int center, Vector2Int[] output)`
- Produces: `BattlefieldChunkLayout.DecorationSeed(Vector2Int coordinate, int battlefieldSeed) : int`
- Produces: `BattlefieldTilePresenter.BuildInfinite(...)` and `Track(Vector2 playerPosition)`
- Produces test observations: `ActiveChunkCount`, `ChunkCoordinates`, `RebuildCount`, and `DecorationSignature(Vector2Int)` under `UNITY_INCLUDE_TESTS`

- [ ] **Step 1: Write coordinate and deterministic-seed EditMode tests**

```csharp
[TestCase(0f, 0f, 0, 0)]
[TestCase(31.99f, 31.99f, 0, 0)]
[TestCase(32f, 0f, 1, 0)]
[TestCase(-0.01f, -0.01f, -1, -1)]
public void CoordinateAtUsesFloorAcrossNegativeWorldSpace(float x, float y, int expectedX, int expectedY) =>
    Assert.That(BattlefieldChunkLayout.CoordinateAt(new Vector2(x, y)),
        Is.EqualTo(new Vector2Int(expectedX, expectedY)));

[Test]
public void RequiredCoordinatesAlwaysContainAThreeByThreeNeighborhood()
{
    var output = new Vector2Int[9];
    BattlefieldChunkLayout.FillRequired(new Vector2Int(4, -3), output);
    Assert.That(output.Distinct().Count(), Is.EqualTo(9));
    Assert.That(output, Does.Contain(new Vector2Int(3, -4)));
    Assert.That(output, Does.Contain(new Vector2Int(5, -2)));
}
```

- [ ] **Step 2: Replace the finite-ground PlayMode expectation with recycling behavior tests**

```csharp
[UnityTest]
public IEnumerator InfiniteBattlefieldKeepsNineChunksAndDoesNotRebuildInsideOneCoordinate()
{
    var presenter = CreatePresenter();
    presenter.BuildInfinite(primary, null, decals, fallback, 0x4A4F5345);
    var initialRebuilds = presenter.RebuildCount;
    presenter.Track(new Vector2(15f, 15f));
    yield return null;
    Assert.That(presenter.ActiveChunkCount, Is.EqualTo(9));
    Assert.That(presenter.RebuildCount, Is.EqualTo(initialRebuilds));
}

[UnityTest]
public IEnumerator ReturningToCoordinateRestoresDecorationSignature()
{
    var presenter = CreatePresenter();
    presenter.BuildInfinite(primary, null, decals, fallback, 0x4A4F5345);
    var before = presenter.DecorationSignature(Vector2Int.zero);
    presenter.Track(new Vector2(96f, 0f));
    presenter.Track(Vector2.zero);
    yield return null;
    Assert.That(presenter.DecorationSignature(Vector2Int.zero), Is.EqualTo(before));
}
```

- [ ] **Step 3: Run focused tests and verify RED**

Run the two classes through `Tools/Unity/Test-Unity.ps1`; expect missing chunk layout and infinite presenter APIs.

- [ ] **Step 4: Implement nine preallocated chunk views**

Use fixed arrays of length nine for required coordinates and views. `Track` returns immediately when the center coordinate is unchanged. When it changes, retain views already assigned to required coordinates and reassign only the remaining views. Configure a bounded four-decoration-renderer array per view from the deterministic seed; do not instantiate in `Track`.

- [ ] **Step 5: Run focused tests and verify GREEN**

Expected: coordinate, nine-chunk, no-rebuild, and return-signature tests pass.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/Battlefield* Assets/JoseonHunter/Tests/EditMode/BattlefieldChunkLayoutTests.cs* Assets/JoseonHunter/Tests/PlayMode/BattlefieldTilePresenterPlayModeTests.cs
git commit -m "feat: add deterministic infinite battlefield"
git push origin master
```

---

### Task 3: Joseon folk-field pixel asset and inspectable world prefab

**Files:**
- Create: `Assets/JoseonHunter/Art/World/Runtime/Battlefield/joseon_folk_field_tile.png`
- Create: generated `.meta` through Unity import
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BattlefieldPresentationLibrary.cs`
- Create: matching `.meta`
- Create: `Assets/JoseonHunter/Prefabs/World/BattlefieldChunk.prefab`
- Create: `Assets/JoseonHunter/Resources/Presentation/BattlefieldPresentationLibrary.asset`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/BattlefieldPresentationBuilder.cs`
- Create: matching `.meta`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/AssetImportProfileTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/BattlefieldTilePresenterPlayModeTests.cs`

**Interfaces:**
- Produces: `BattlefieldPresentationLibrary` containing chunk prefab, ground tile, and bounded decoration sprite list
- Consumes: `BattlefieldTilePresenter.BuildInfinite(...)` from Task 2
- FirstPlayable loads `Resources.Load<BattlefieldPresentationLibrary>("Presentation/BattlefieldPresentationLibrary")`, falling back to serialized legacy art if missing

- [ ] **Step 1: Add failing import and resource-wiring tests**

```csharp
[Test]
public void JoseonFolkFieldUsesPixelImportProfile()
{
    var importer = AssetImporter.GetAtPath(TilePath) as TextureImporter;
    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
    Assert.That(importer.mipmapEnabled, Is.False);
    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
}
```

Add a PlayMode assertion that the Gameplay field resolves the library, uses the folk-field sprite, and owns nine chunks.

- [ ] **Step 2: Run the tests and verify RED**

Expected: tile and presentation library assets do not exist.

- [ ] **Step 3: Generate the approved A-style source image**

Use the image-generation tool with this art contract: seamless top-down orthographic pixel ground; muted mugwort-green soil; subtle 16-bit pixel texture; sparse tiny grass and pebbles only; no characters, buildings, horizon, text, borders, shadows, large landmarks, transparency, or lighting gradient. Save the accepted image to the exact tile path without overwriting the legacy occult battlefield.

- [ ] **Step 4: Import and build the prefab/library through Unity**

`BattlefieldPresentationBuilder.Build` creates an inactive chunk prefab with one ground `SpriteRenderer` and four child decoration renderers, then creates the Resources library referencing the prefab, new ground tile, and existing ward-paper, roof-fragment, reed, and ritual-stone sprites. The builder sets importer values before `AssetDatabase.ImportAsset`.

Run Unity with `-executeMethod JoseonHunter.Editor.Scenes.BattlefieldPresentationBuilder.BuildInBatchMode` and inspect the generated assets.

- [ ] **Step 5: Wire FirstPlayable without changing Gameplay scene serialization**

Resolve the Resources library in `CreateField`, instantiate its chunk prefab through the presenter, call `Track(player.transform.position)` from `UpdateField`, and retain the legacy fallback art path for tests or missing resources.

- [ ] **Step 6: Run focused tests and verify GREEN**

Expected: import profile, Resources wiring, nine chunks, and deterministic-return tests pass.

- [ ] **Step 7: Visually inspect a Gameplay capture**

Capture at 720x1280 and 1080x2340. Verify no visible seams, black gaps, perspective, large repeated landmarks, or reduced enemy/pickup readability.

- [ ] **Step 8: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Art/World/Runtime/Battlefield Assets/JoseonHunter/Prefabs/World Assets/JoseonHunter/Resources/Presentation Assets/JoseonHunter/Scripts/Runtime/Gameplay Assets/JoseonHunter/Scripts/Editor/Scenes Assets/JoseonHunter/Tests
git commit -m "feat: add Joseon folk infinite battlefield art"
git push origin master
```

---

### Task 4: Connect wave species, packs, density, and real viewport spawning

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySpriteRoster.cs`
- Create: matching `.meta`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/StagePacingPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WaveRosterPlayModeTests.cs`
- Create: matching `.meta`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableLoadPlayModeTests.cs`

**Interfaces:**
- Consumes: `RunClock.PhaseAt`, `WaveSchedule`, and `WaveSpawnDirector`
- Produces: `EnemySpriteRoster.Resolve(string contentId) : Sprite`
- Production spawn path: `SpawnEnemy(bool isBoss, int midBossTier = 0, string normalContentId = null, Vector2? forcedPosition = null)`
- Test observations: `LivingNormalEnemyIdsForTests`, `LastSpawnContentIdForTests`, `TickSpawningForTests(float delta)`, and `SetElapsedForTests(float seconds)`

- [ ] **Step 1: Add failing species and active-cap PlayMode tests**

```csharp
[UnityTest]
public IEnumerator FirstWaveSpawnsOnlyRats()
{
    var controller = LoadGameplay();
    controller.SetElapsedForTests(20f);
    controller.TickSpawningForTests(8f);
    Assert.That(controller.LivingNormalEnemyIdsForTests, Is.Not.Empty);
    Assert.That(controller.LivingNormalEnemyIdsForTests, Is.All.EqualTo("plague_rat"));
    yield return null;
}

[UnityTest]
public IEnumerator SecondWaveContainsRatsSpiritsAndASpiritPack()
{
    var controller = LoadGameplay();
    controller.SetElapsedForTests(60f);
    controller.TickSpawningForTests(20f);
    Assert.That(controller.LivingNormalEnemyIdsForTests, Does.Contain("plague_rat"));
    Assert.That(controller.LivingNormalEnemyIdsForTests, Does.Contain("vengeful_spirit"));
    Assert.That(controller.LivingNormalEnemyIdsForTests.Count, Is.LessThanOrEqualTo(104));
    yield return null;
}
```

Update viewport tests to assert `Renderer.bounds` is fully outside bounds derived from `camera.ViewportToWorldPoint`, not the independent spawn profile.

- [ ] **Step 2: Run focused PlayMode tests and verify RED**

Expected: test APIs do not exist and current random sprite selection violates first-wave purity.

- [ ] **Step 3: Implement explicit ID-to-sprite mapping and pack spawning**

Map scene array indices exactly: rat 0, bandit 1, dokkaebi 2, sakkat specter 3, vengeful spirit 4. Store `ContentId` in each normal `EnemyState`. Continuous spawning selects from the current wave through `WaveSpawnDirector`; packs select one authored pack ID and share a perimeter side with spaced `t` values.

Use `WaveSchedule.For(RunClock.PhaseAt(elapsed)).ActiveCap` as the shared budget. Count only live combat enemies toward the cap, excluding treasure chests. Boss warning stops packs and reduces continuous pressure per the schedule.

- [ ] **Step 4: Replace spawn bounds with actual camera bounds**

```csharp
private Rect CurrentVisibleBounds()
{
    var bottomLeft = gameplayCamera.ViewportToWorldPoint(Vector3.zero);
    var topRight = gameplayCamera.ViewportToWorldPoint(Vector3.one);
    return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
}
```

Expand the perimeter by the authored margin, then retain `MoveRendererOutsideViewport` as the exact renderer-clearance check.

- [ ] **Step 5: Run wave, viewport, load, and performance tests and verify GREEN**

Expected: first-wave purity, second-wave mix/pack, exact viewport clearance, active caps, existing 30/50/100 load tests, and existing eight-weapon tests pass.

- [ ] **Step 6: Measure 140 active enemies**

Add or extend a profiler test to spawn 140 durable enemies, sample 120 frames after warmup, record p95 frame time, hot-path marker timing, and managed allocation. Compare with the existing 100-enemy evidence; report any regression rather than weakening the cap test.

- [ ] **Step 7: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay Assets/JoseonHunter/Tests/PlayMode
git commit -m "feat: add authored enemy waves and packs"
git push origin master
```

---

### Task 5: Bootstrap asynchronous loading presentation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/BootstrapLoadingPresenter.cs`
- Create: matching `.meta`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayReadySignal.cs`
- Create: matching `.meta`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Prefabs/UI/BootstrapLoading.prefab`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/BootstrapLoadingBuilder.cs`
- Create: matching `.meta`
- Modify through builder: `Assets/JoseonHunter/Scenes/Bootstrap.unity`
- Create: `Assets/JoseonHunter/Tests/PlayMode/BootstrapLoadingPlayModeTests.cs`
- Create: matching `.meta`
- Modify: `Assets/JoseonHunter/Tests/EditMode/SceneScaffoldTests.cs`

**Interfaces:**
- Produces: `GameplayReadySignal.IsReady`, `MarkReady()`, and `Reset()`
- Produces: `BootstrapLoadingPresenter.Begin(string gameplaySceneName = "Gameplay")`
- FirstPlayable calls `GameplayReadySignal.MarkReady()` after field, player, weapon runtime, and presentation roots are initialized

- [ ] **Step 1: Add failing scene and PlayMode loading tests**

```csharp
[Test]
public void BootstrapContainsLoadingPresenter()
{
    var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
    try { Assert.That(Object.FindFirstObjectByType<BootstrapLoadingPresenter>(), Is.Not.Null); }
    finally { EditorSceneManager.CloseScene(scene, true); }
}

[UnityTest]
public IEnumerator BootstrapKeepsOpaqueOverlayUntilGameplayReadyThenRemovesIt()
{
    SceneManager.LoadScene("Bootstrap");
    yield return WaitForScene("Gameplay");
    var loader = Object.FindFirstObjectByType<BootstrapLoadingPresenter>();
    Assert.That(loader, Is.Not.Null);
    Assert.That(loader.OpaqueForTests, Is.True);
    yield return WaitForReadyAndFade();
    Assert.That(Object.FindFirstObjectByType<BootstrapLoadingPresenter>(), Is.Null);
}
```

- [ ] **Step 2: Run focused tests and verify RED**

Expected: loading presenter, ready signal, prefab, and Bootstrap wiring do not exist.

- [ ] **Step 3: Implement loading state and actual progress bar**

The presenter calls `DontDestroyOnLoad`, uses `SceneManager.LoadSceneAsync`, sets fill to `Mathf.Clamp01(operation.progress / .9f)`, and does not show a numeric percent. It remains fully opaque until GameplayReadySignal is true and one `WaitForEndOfFrame` has completed, then fades its `CanvasGroup` with `Time.unscaledDeltaTime`. Enforce a minimum visible duration of 0.35 seconds.

If the load operation fails to start, keep the overlay and replace the subtitle with a concise Korean retry message. Prevent duplicate loaders on direct scene reload.

- [ ] **Step 4: Generate prefab and wire only the clean Bootstrap scene**

`BootstrapLoadingBuilder.BuildInBatchMode` creates the full-screen portrait Canvas, title, spirit-flame pulse image, brush-stroke progress background/fill, and presenter. It replaces only the placeholder `SceneRoot` in Bootstrap and refuses to save if Bootstrap is already dirty in an interactive Editor.

- [ ] **Step 5: Run loading and direct-Gameplay tests and verify GREEN**

Expected: Bootstrap async transition passes, overlay lifecycle passes, direct Gameplay tests remain green, and no duplicate UI/bootstrap objects are created.

- [ ] **Step 6: Visually inspect the transition**

Capture or run Bootstrap at 720x1280 and 1080x2340. Verify opaque coverage, readable Korean text, actual progress movement, no black/partial Gameplay frame, and a smooth unscaled fade.

- [ ] **Step 7: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/BootstrapLoadingPresenter.cs* Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayReadySignal.cs* Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Prefabs/UI/BootstrapLoading.prefab Assets/JoseonHunter/Scripts/Editor/Scenes/BootstrapLoadingBuilder.cs* Assets/JoseonHunter/Scenes/Bootstrap.unity Assets/JoseonHunter/Tests
git commit -m "feat: add Bootstrap loading transition"
git push origin master
```

---

### Task 6: Integrated validation, documentation, and release artifact

**Files:**
- Modify: `Docs/AI/UnityProjectContext.md`
- Create: `Docs/Verification/2026-08-02-infinite-battlefield-wave-loading.md`

**Interfaces:**
- Consumes every prior task's tests and runtime behavior
- Produces reproducible evidence, APK hash, performance numbers, limitations, and handoff notes

- [ ] **Step 1: Run full EditMode**

Run:
`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode`

Record total, passed, failed, skipped, duration, and any new warnings.

- [ ] **Step 2: Run focused integrated PlayMode suites**

Run battlefield, wave roster, stage pacing, first playable load, Bootstrap loading, eight-weapon combat, and combat-performance classes in one semicolon-separated filter. Record exact totals and the 100/140-enemy measurements.

- [ ] **Step 3: Inspect dynamic font and scene side effects**

Restore only TMP dynamic atlas data created by validation. Preserve the user's pre-existing Gameplay and ProjectSettings changes. Verify no test or builder modified Gameplay serialization unexpectedly.

- [ ] **Step 4: Build Android development APK**

Run:
`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1`

Verify exit code, `Build Finished, Result: Success`, APK byte size, and SHA-256.

- [ ] **Step 5: Write the verification record**

Document symptom, confirmed causes, implementation, exact tests, visual checks, performance, build artifact, uncommitted user-owned files, and device-profile limitations. Do not claim physical-device GPU/thermal evidence unless it was measured.

- [ ] **Step 6: Run final diff and remote checks**

Verify `git diff --check`, no staged unrelated files, local `HEAD` equals `origin/master` after push, and untracked `.utmp` remains excluded.

- [ ] **Step 7: Commit and push documentation**

```powershell
git add -- Docs/AI/UnityProjectContext.md Docs/Verification/2026-08-02-infinite-battlefield-wave-loading.md
git commit -m "docs: record infinite battlefield verification"
git push origin master
```
