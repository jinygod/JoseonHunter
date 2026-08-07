# Stage Two and Three Content Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn `도깨비 고개` and `월식 왕릉` into distinct, playable 15-minute stages with bounded maps, new enemies and bosses, PixelLab art, harder progression, stage rewards, and Android-ready presentation.

**Architecture:** Replace the single global wave table with a stage-selected immutable combat definition injected into the existing controller and spawn director. Keep first-stage infinite chunks intact while stage two and three use outer-bound-only finite battlefields, reusable enemy attack primitives, reusable boss profiles, and pooled visuals. Stage and difficulty multipliers remain separate and combine exactly once during spawn and settlement.

**Tech Stack:** Unity 6000.5.5f1, C# domain/runtime/presentation assemblies, uGUI/TMP, URP 2D, NUnit EditMode and PlayMode tests, PixelLab MCP, Android ARM64 IL2CPP.

## Global Constraints

- Work directly on `master`; commit and push every independently green task to `origin/master`.
- Preserve all unrelated pre-existing `.meta` changes and never stage them accidentally.
- Run Unity sequentially at `BelowNormal` priority with processor affinity `[IntPtr]15`.
- Stage one keeps its current infinite 3-by-3 recycled battlefield and exact approved wave values.
- Stage two uses a bounded `72 x 112` world-unit battlefield; stage three uses `84 x 84`.
- Only the outer boundary blocks movement. Interior scenery is non-colliding and enemies keep direct pursuit.
- Stage two baseline health/damage multipliers are `1.35 / 1.12`; stage three uses `1.70 / 1.25`.
- Stage reward multipliers are `1.00`, `1.25`, and `1.55` for stages one, two, and three.
- Difficulty multipliers remain the existing Normal/Omen/GreatOmen profiles and multiply stage values once.
- Maximum in-run level stays 35, with a normal final-boss target range of level 30–35.
- Stage two normal active cap is approximately 130; stage three is 110–120; no mode exceeds the global 140 cap.
- New pixel art uses dark outlines, no white outlines, 5–7 core colors, large clean clusters, Point filtering, no mipmaps, and lossless/uncompressed import.
- General enemies are 48px, elites 56–64px, midbosses 80px, and final bosses 96–112px.
- Projectiles, hazards, telegraphs, damage numbers, and hit effects must be pooled; no per-frame LINQ, lists, or Instantiate in combat loops.
- Every boss attack preserves a readable telegraph and a reachable safe route on every difficulty.

---

### Task 1: Stage-selected combat definitions

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatDefinition.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StageCombatCatalogTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WaveRosterPlayModeTests.cs`

**Interfaces:**
- Produces: `StageCombatDefinition StageCombatCatalog.For(StageId stageId)`
- Produces: `StageWaveProfile.WaveAt(float elapsedSeconds)` and `Introductions`
- Changes: `new WaveSpawnDirector(StageWaveProfile profile, int seed)`
- Consumes later: battlefield, enemy, boss, experience, and reward profiles attached to `StageCombatDefinition`

- [ ] **Step 1: Write failing catalog and first-stage compatibility tests**

```csharp
[Test]
public void EveryStageHasAStableCombatDefinition()
{
    foreach (var stage in StageCatalog.All)
        Assert.That(StageCombatCatalog.TryGet(stage.Id, out _), Is.True, stage.DisplayName);
}

[Test]
public void GwigokDefinitionPreservesApprovedOpeningAndPeakCaps()
{
    var combat = StageCombatCatalog.For(StageId.GwigokField);
    Assert.That(combat.Waves.WaveAt(0f).ActiveCap, Is.EqualTo(72));
    Assert.That(combat.Waves.WaveAt(610f).ActiveCap, Is.EqualTo(140));
    Assert.That(combat.Waves.NormalEntriesAt(0f).Single().ContentId, Is.EqualTo("plague_rat"));
}
```

- [ ] **Step 2: Run the focused EditMode tests and confirm RED**

Run: `Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.StageCombatCatalogTests|JoseonHunter.Tests.EditMode.WaveSpawnDirectorTests'`

Expected: FAIL because `StageCombatCatalog`, `StageCombatDefinition`, and the injected director constructor do not exist.

- [ ] **Step 3: Implement immutable stage combat and wave profile types**

```csharp
public sealed class StageCombatDefinition
{
    public StageCombatDefinition(
        StageId stageId,
        StageWaveProfile waves,
        StageBattlefieldDefinition battlefield,
        StageStatProfile stats,
        StageRewardProfile rewards,
        IReadOnlyList<StageBossDefinition> bosses)
    {
        StageId = stageId;
        Waves = waves ?? throw new ArgumentNullException(nameof(waves));
        Battlefield = battlefield;
        Stats = stats;
        Rewards = rewards;
        Bosses = bosses ?? throw new ArgumentNullException(nameof(bosses));
    }

    public StageId StageId { get; }
    public StageWaveProfile Waves { get; }
    public StageBattlefieldDefinition Battlefield { get; }
    public StageStatProfile Stats { get; }
    public StageRewardProfile Rewards { get; }
    public IReadOnlyList<StageBossDefinition> Bosses { get; }
}
```

Move the exact existing stage-one entries, packs, introductions, and boss IDs into `StageCombatCatalog`. Add explicit stage-two and stage-three definitions with their approved IDs and timing, while keeping their presentation readiness false until their asset tasks complete.

- [ ] **Step 4: Inject the selected wave profile**

```csharp
waveSpawnDirector = new WaveSpawnDirector(activeStageCombat.Waves, RunSpawnSeed);
var wave = activeStageCombat.Waves.WaveAt(elapsed);
```

Replace production `WaveSchedule.For`, `NormalEntriesAt`, and `Introductions` reads in `FirstPlayableController` with the selected profile. Keep `WaveSchedule` as a stage-one compatibility facade for older tests and editor captures.

- [ ] **Step 5: Run focused EditMode and PlayMode tests**

Run EditMode filters from Step 2, then:

`Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.WaveRosterPlayModeTests|JoseonHunter.Tests.PlayMode.StagePacingPlayModeTests'`

Expected: catalog/director tests pass; the complete stage-one roster and timing remain unchanged.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatDefinition.cs Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatDefinition.cs.meta Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatCatalog.cs Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatCatalog.cs.meta Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/EditMode/StageCombatCatalogTests.cs Assets/JoseonHunter/Tests/EditMode/StageCombatCatalogTests.cs.meta Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs Assets/JoseonHunter/Tests/PlayMode/WaveRosterPlayModeTests.cs
git commit -m "refactor: select combat data by stage"
git push origin master
```

---

### Task 2: Finite battlefield and safe spawn geometry

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/StageBattlefieldDefinition.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BoundedBattlefieldPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BattlefieldTilePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ViewportSpawnGeometry.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StageBattlefieldDefinitionTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/BoundedSpawnGeometryTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/BattlefieldTilePresenterPlayModeTests.cs`

**Interfaces:**
- Produces: `Float2 StageBattlefieldDefinition.ClampPlayer(Float2 position, Float2 cameraHalfExtents)`
- Produces: `bool BoundedSpawnGeometry.TrySelect(Vector2 player, Rect bounds, Camera camera, float t, out Vector2 position)`
- Produces: `BoundedBattlefieldPresenter.Configure(StageBattlefieldDefinition, BattlefieldPresentationLibrary, int seed)`
- Consumes: `StageCombatDefinition.Battlefield`

- [ ] **Step 1: Write failing bounds and spawn tests**

```csharp
[TestCase(36f, 56f, 15f, 35f)]
[TestCase(-36f, -56f, -15f, -35f)]
public void DokkaebiPassClampsCameraSafePlayerPosition(
    float x, float y, float expectedX, float expectedY)
{
    var field = StageBattlefieldDefinition.Bounded(72f, 112f, "dokkaebi_pass");
    Assert.That(field.ClampPlayer(new Float2(x, y), new Float2(21f, 21f)),
        Is.EqualTo(new Float2(expectedX, expectedY)));
}
```

Add cases proving spawn points are inside bounds, outside the viewport, and still found when the player hugs each edge.

- [ ] **Step 2: Run focused EditMode tests and confirm RED**

Run: `Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.StageBattlefieldDefinitionTests|JoseonHunter.Tests.EditMode.BoundedSpawnGeometryTests'`

Expected: FAIL because the bounded battlefield types do not exist.

- [ ] **Step 3: Implement the pure bounds model**

```csharp
public readonly struct StageBattlefieldDefinition
{
    public bool IsBounded { get; }
    public float Width { get; }
    public float Height { get; }
    public string PresentationId { get; }

    public static StageBattlefieldDefinition Infinite(string presentationId) =>
        new(false, 0f, 0f, presentationId);

    public static StageBattlefieldDefinition Bounded(float width, float height, string presentationId) =>
        new(true, width, height, presentationId);
}
```

Clamp camera-safe player coordinates without allocating. Return the original position for infinite fields.

- [ ] **Step 4: Implement bounded presentation and controller wiring**

Stage one continues using `BattlefieldTilePresenter`. For bounded stages, create fixed tiles once, place non-colliding deterministic decorations, add four simple outer colliders, clamp the player after movement, and clamp the camera look-ahead center. Never show black outside the decorative border.

- [ ] **Step 5: Run bounds tests and battlefield PlayMode tests**

Run the EditMode filter from Step 2 and:

`Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.BattlefieldTilePresenterPlayModeTests'`

Expected: all bounds/spawn cases pass and stage-one recycling tests remain green.

- [ ] **Step 6: Commit and push**

Commit message: `feat: add bounded stage battlefields`

Stage only the files listed in this task and their newly generated `.meta` files, then push `master`.

---

### Task 3: Dokkaebi Pass enemies, bosses, and wave behavior

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/EnemyAttackPatterns.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/StageBossCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/BossAttackPattern.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyAttackPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BossTelegraphPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/DokkaebiPassCombatTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StageBossCatalogTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/DokkaebiPassPlayModeTests.cs`

**Interfaces:**
- Produces: stage-two content IDs `club_dokkaebi`, `shield_guard_dokkaebi`, `iron_horn_dokkaebi`, `stone_thrower_dokkaebi`, `red_horn_elite`
- Produces: boss IDs `one_horn_captain`, `iron_shield_general`, `dokkaebi_king`
- Produces: `EnemyAttackSnapshot EnemyAttackController.Tick(...)`
- Produces: `StageBossDefinition StageBossCatalog.Get(string bossId)`
- Consumes: stage-selected wave and bounded battlefield systems

- [ ] **Step 1: Write failing enemy-role and boss-pattern tests**

```csharp
[Test]
public void ShieldGuardBreaksAndCreatesAVulnerabilityWindow()
{
    var guard = new DirectionalGuardState(charges: 6, blockedDamageMultiplier: .15f);
    for (var i = 0; i < 5; i++) Assert.That(guard.ConfirmBlockedHit(), Is.False);
    Assert.That(guard.ConfirmBlockedHit(), Is.True);
    Assert.That(guard.IsBroken, Is.True);
}

[Test]
public void DokkaebiKingPhaseTwoStillTelegraphsEveryLinkedAttack()
{
    var profile = StageBossCatalog.Get("dokkaebi_king");
    Assert.That(profile.PatternFor(.49f, 2).All(step => step.WarningSeconds >= .75f), Is.True);
}
```

Also assert the exact 0/120/300/420/600/720/840/900-second roster transitions and 130 normal cap.

- [ ] **Step 2: Run focused EditMode tests and confirm RED**

Run: `Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.DokkaebiPassCombatTests|JoseonHunter.Tests.EditMode.StageBossCatalogTests'`

- [ ] **Step 3: Implement reusable attack primitives and stage-two profiles**

Add explicit types for direct chase, directional guard, warned line charge, warned single projectile, circle slam, cone sweep, and rockfall. The domain controllers emit value snapshots; runtime presenters own pooled visuals and damage queries.

- [ ] **Step 4: Add stage-two boss profiles**

Use `one_horn_captain` at 300 seconds, `iron_shield_general` at 600, and `dokkaebi_king` at 900. Assign 1.7x, 1.9x, and 2.8x visual/contact scale. Great Omen may link patterns but may not shorten any warning below its profile minimum.

- [ ] **Step 5: Implement stage-two PlayMode behavior**

Add test hooks that select `DokkaebiPass`, advance canonical time, inspect living content IDs, execute one complete shield break, miss one charge, and defeat both midbosses without ending the run. Assert only `dokkaebi_king` victory ends the run.

- [ ] **Step 6: Run focused EditMode and PlayMode tests**

Run the filters from Steps 2 and 5. Expected: all stage-two behavior passes without enabling lobby sortie yet.

- [ ] **Step 7: Commit and push**

Commit message: `feat: add dokkaebi pass combat rules`

---

### Task 4: Dokkaebi Pass PixelLab art and presentation

**Files:**
- Create: `Assets/JoseonHunter/Art/Stages/DokkaebiPass/` assets and `.meta`
- Create: `Assets/JoseonHunter/Art/Enemies/DokkaebiPass/` assets and `.meta`
- Create: `Assets/JoseonHunter/Art/Bosses/DokkaebiPass/` assets and `.meta`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/StagePixelAssetImporter.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/StagePresentationLibraryBuilder.cs`
- Create: `Assets/JoseonHunter/Resources/StagePresentationCatalog.asset`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySpriteRoster.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BattlefieldPresentationLibrary.cs`
- Create: `Docs/Assets/stage-pixellab-generation-ledger.csv`
- Create: `Assets/JoseonHunter/Tests/EditMode/DokkaebiPassAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/DokkaebiPassPlayModeTests.cs`

**Interfaces:**
- Produces: `StagePresentationCatalog.TryGet(StageId, out StagePresentationEntry)`
- Produces: five stage-two enemy sprites, three boss sprite sets, ground/edge tiles, and decorations
- Consumes: PixelLab generation jobs and approved style prompt

- [ ] **Step 1: Write failing asset contract tests**

```csharp
[TestCase("club_dokkaebi", 48)]
[TestCase("shield_guard_dokkaebi", 48)]
[TestCase("red_horn_elite", 64)]
[TestCase("one_horn_captain", 80)]
[TestCase("dokkaebi_king", 112)]
public void DokkaebiPassSpritesMeetApprovedCanvasAndImportContract(string id, int pixels)
{
    var sprite = StageAssetTestCatalog.LoadSprite(id);
    Assert.That(sprite.rect.width, Is.EqualTo(pixels));
    StageAssetAssertions.IsPointFilteredWithoutMipmaps(sprite.texture);
}
```

Add palette/alpha-border tests and reject visible near-white outline pixels around opaque sprite edges.

- [ ] **Step 2: Run the asset tests and confirm RED**

Expected: all new stage-two assets are missing.

- [ ] **Step 3: Generate and review PixelLab jobs**

Create a `dokkaebi_pass` top-down tileset and eight character/object jobs using the approved requirements: chunky dark outline, 5–7 colors, simplified shading, no white rim, transparent character background. Record job ID, prompt, credit cost, accepted candidate, and rejection reason in the ledger. Inspect every candidate at original size and scaled to representative gameplay size.

- [ ] **Step 4: Import accepted art**

Download accepted PNGs to the exact stage directories. Run `StagePixelAssetImporter` to set Sprite, Point, mipmap off, lossless/uncompressed, stable PPU/pivot, and correct texture alpha. Do not scale a noisy candidate down to force acceptance; regenerate it.

- [ ] **Step 5: Build the stage presentation catalog and verify PlayMode**

Wire the exact content IDs to sprites and animation frames. Assert stage two uses no stage-one enemy or floor fallback and visually distinct bosses use the approved scale.

- [ ] **Step 6: Run focused asset and PlayMode tests**

Run: `Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.DokkaebiPassAssetContractTests'`

Then run `DokkaebiPassPlayModeTests`.

- [ ] **Step 7: Commit and push**

Commit message: `feat: add dokkaebi pass pixel presentation`

---

### Task 5: Moonlit Tomb enemies, hazards, bosses, and safe lanes

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/EnemyAttackPatterns.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/StageBossCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyAttackPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyProjectilePool.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/StageHazardPool.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BossTelegraphPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/MoonlitTombCombatTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/BossSafeLaneTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/MoonlitTombPlayModeTests.cs`

**Interfaces:**
- Produces: content IDs `tomb_attendant`, `tomb_archer_ghost`, `red_lantern_wraith`, `curse_shaman`, `grave_ambusher_elite`
- Produces: boss IDs `royal_guard_wraith`, `eclipse_priest`, `eclipse_queen`
- Produces: pooled projectile and hazard handles with explicit lifetime and damage cadence
- Produces: `BossSafeLaneValidator.HasReachableGap(...)`

- [ ] **Step 1: Write failing ranged, hazard, and safe-lane tests**

```csharp
[Test]
public void ArcherCannotBeginAimingOutsideCameraMargin()
{
    Assert.That(RangedAttackRules.CanAcquireTarget(false, 18f, 16f), Is.False);
}

[Test]
public void GreatOmenQueenPatternAlwaysLeavesASafeAngularGap()
{
    var pattern = StageBossCatalog.Get("eclipse_queen").PatternFor(.35f, 2);
    Assert.That(BossSafeLaneValidator.HasReachableGap(pattern, minimumDegrees: 28f), Is.True);
}
```

Assert exact stage-three introductions, 110–120 cap, projectile cap, hazard cap, and hazard expiry.

- [ ] **Step 2: Run focused EditMode tests and confirm RED**

Run Moonlit Tomb and safe-lane test filters.

- [ ] **Step 3: Implement stage-three enemy primitives and pools**

Implement warned line projectile, temporary circular curse field, predicted-position field, and warned burrow emergence. Pools have fixed capacities, reclaim the oldest expired/least-recent handle when full, and clear all active visuals on run reset.

- [ ] **Step 4: Implement stage-three boss profiles**

Use `royal_guard_wraith`, `eclipse_priest`, and `eclipse_queen` at 300/600/900 seconds. Add crescent sweep, radial volley with explicit gap, sequential curse cells, and warned spirit hands. Validate a safe gap before committing every generated pattern; skip invalid patterns.

- [ ] **Step 5: Run focused EditMode and PlayMode tests**

Exercise one complete attack from every enemy and boss, prove no off-screen aiming, prove hazards expire, and prove only the final boss ends the run.

- [ ] **Step 6: Commit and push**

Commit message: `feat: add moonlit tomb combat rules`

---

### Task 6: Moonlit Tomb PixelLab art and presentation

**Files:**
- Create: `Assets/JoseonHunter/Art/Stages/MoonlitTomb/` assets and `.meta`
- Create: `Assets/JoseonHunter/Art/Enemies/MoonlitTomb/` assets and `.meta`
- Create: `Assets/JoseonHunter/Art/Bosses/MoonlitTomb/` assets and `.meta`
- Modify: `Assets/JoseonHunter/Resources/StagePresentationCatalog.asset`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/StagePixelAssetImporter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/StagePresentationLibraryBuilder.cs`
- Modify: `Docs/Assets/stage-pixellab-generation-ledger.csv`
- Create: `Assets/JoseonHunter/Tests/EditMode/MoonlitTombAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/MoonlitTombPlayModeTests.cs`

**Interfaces:**
- Produces: five stage-three enemy sprites, three boss sets, Moonlit Tomb ground/edge tiles and decorations
- Extends: `StagePresentationCatalog`

- [ ] **Step 1: Write failing asset contract tests**

Mirror the stage-two contract with exact Moonlit Tomb IDs and sizes. Add contrast assertions proving warning crimson/violet differs sufficiently from sampled ground colors.

- [ ] **Step 2: Run asset tests and confirm RED**

- [ ] **Step 3: Generate PixelLab tiles and characters**

Use dark indigo outlines, midnight/pale stone/turquoise/violet/crimson palette, 5–7 colors per creature, simplified large clusters, no white rim, and transparent character backgrounds. Record every job and decision in the generation ledger.

- [ ] **Step 4: Import and build catalog entries**

Apply the same importer contract as stage two. Use darker, lower-contrast map tiles than actors and hazards. Verify all content IDs resolve without stage-one fallback.

- [ ] **Step 5: Run focused asset and PlayMode tests**

- [ ] **Step 6: Commit and push**

Commit message: `feat: add moonlit tomb pixel presentation`

---

### Task 7: Stage stats, experience, rewards, unlocks, and lobby activation

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/StageCombatDefinition.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/StageProgression.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/AccountProgression.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/ISaveRepository.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyHealthCurve.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StageRewardProfileTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/StageProgressionTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/RunSettlementLobbyPlayModeTests.cs`

**Interfaces:**
- Produces: `StageStatProfile.ScaleHealth/ScaleDamage/ScaleExperience`
- Produces: `StageRewardProfile.ScaleCoins/ScaleAccountExperience/ScaleMastery`
- Changes: all three `StageCatalog` entries have `HasPlayableContent == true`

- [ ] **Step 1: Write failing multiplier and settlement tests**

```csharp
[TestCase("stage_01_gwigok_field", 1.00f)]
[TestCase("stage_02_dokkaebi_pass", 1.25f)]
[TestCase("stage_03_moonlit_tomb", 1.55f)]
public void StageRewardMultiplierIsAppliedExactlyOnce(string stageId, float expected)
{
    var reward = StageCombatCatalog.For(new StageId(stageId)).Rewards.ScaleCoins(100);
    Assert.That(reward, Is.EqualTo((int)Math.Round(100 * expected)));
}
```

Add combined Stage 3 Great Omen tests and a save-failure rollback test covering coins, account XP, mastery, and stage clear record.

- [ ] **Step 2: Run focused tests and confirm RED**

- [ ] **Step 3: Apply stage stats and XP values once during spawn/death**

Compute final health and damage as `base * stage * difficulty`. Give higher XP values to stronger archetypes, then pass values through the existing orb compaction budget. Do not change the maximum level or pickup attraction range.

- [ ] **Step 4: Apply reward multipliers once during atomic settlement**

Calculate stage reward first and difficulty reward second in one helper used by both preview/result UI and repository commit. Do not scale the already-scaled result a second time.

- [ ] **Step 5: Enable stages and verify lobby flow**

Set 2/3 content readiness true only after both presentation catalog entries validate. Assert stage one normal unlocks stage two, stage two normal unlocks stage three, and each stage normal/omen unlocks its next difficulty as before.

- [ ] **Step 6: Run progression, lobby, and settlement test groups**

- [ ] **Step 7: Commit and push**

Commit message: `feat: activate stage progression and rewards`

---

### Task 8: Stage music, warnings, and portrait presentation

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameMusicRole.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameMusicCatalogAsset.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiBootstrap.cs`
- Create: `Assets/JoseonHunter/Audio/Music/CC0/dokkaebi_pass.ogg`
- Create: `Assets/JoseonHunter/Audio/Music/CC0/moonlit_tomb.ogg`
- Modify: `Assets/JoseonHunter/Resources/Audio/GameMusicCatalog.asset`
- Modify: `Docs/ThirdPartyAudio/free-audio-source-manifest.md`
- Modify: `Docs/Assets/audio-rights-ledger.csv`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StageMusicRoleTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/StagePresentationPlayModeTests.cs`

**Interfaces:**
- Produces: `GameMusicRole.DokkaebiPass` and `GameMusicRole.MoonlitTomb`
- Produces: `StageMusicRoleResolver.For(StageId, RunPhase, bool midBoss, bool finalBoss)`
- Reuses: existing MidBoss and FinalBoss roles with encounter priority

- [ ] **Step 1: Write failing role and presentation tests**

Assert each normal stage requests its own track, midboss/final boss override it, and boss defeat returns to the selected stage track without restarting an unchanged role.

- [ ] **Step 2: Run focused tests and confirm RED**

- [ ] **Step 3: Select and import two suitable CC0 tracks**

Use one percussion/wood-heavy restrained track for Dokkaebi Pass and one low-string/ritual track for Moonlit Tomb. Verify direct license evidence, source URL, author, original filename, and SHA-256 before adding. Import as stereo Vorbis Streaming, background loading, preload disabled, quality 0.55.

- [ ] **Step 4: Wire stage music and Korean warnings**

Preserve existing encounter priority and pause behavior. Display exact stage-specific Korean names for surges and bosses; do not add English fallback text.

- [ ] **Step 5: Run focused audio/presentation tests**

- [ ] **Step 6: Commit and push**

Commit message: `feat: add stage two and three presentation audio`

---

### Task 9: Full verification, performance, captures, and Android build

**Files:**
- Create: `Assets/JoseonHunter/Tests/PlayMode/StageTwoThreePerformancePlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/PortraitStateValidationCapture.cs`
- Create: `Docs/Verification/2026-08-07-stage-two-three-content.md`
- Modify: `Docs/AI/UnityProjectContext.md`

**Interfaces:**
- Consumes all completed stage systems
- Produces final XML/log/capture/APK evidence and an AI handoff record

- [ ] **Step 1: Add deterministic load tests**

Measure stage two at its highest approved enemy count and stage three at its projectile/hazard cap. Assert zero steady-state managed allocation for movement/pattern ticks and record p50/p95 elapsed time without inventing a device FPS claim.

- [ ] **Step 2: Run all focused 15-minute accelerated stage tests**

Run both stages at Normal, Omen, and Great Omen using canonical-time acceleration. Verify introductions, midbosses, final boss, victory, XP cap, rewards, and pool cleanup.

- [ ] **Step 3: Capture portrait evidence**

Capture lobby stage selection and representative early/mid/final-boss frames for stages two and three at 720x1280 and 1080x2340. Visually inspect outlines, role silhouettes, ground contrast, telegraphs, edge coverage, and Korean text.

- [ ] **Step 4: Run full EditMode and PlayMode suites**

Run full `JoseonHunter.Tests.EditMode`, then full `JoseonHunter.Tests.PlayMode`, sequentially with BelowNormal/4-core limits. Record totals, failures, skipped tests, XML paths, and relevant warning classification.

- [ ] **Step 5: Build Android ARM64 IL2CPP development APK**

Run `Tools/Unity/Build-AndroidDevelopment.ps1` under the same CPU limit. Record the new APK byte size, timestamp, package/version, and SHA-256.

- [ ] **Step 6: Write verification and context documents**

Document exact implemented roster/patterns, PixelLab ledger summary, full test counts, performance measurements, captures, build hash, and remaining physical-device checks. Update `UnityProjectContext.md` with the stable architecture and limitations.

- [ ] **Step 7: Commit and push**

Commit message: `docs: verify stage two and three content`

- [ ] **Step 8: Confirm clean task state**

Verify `HEAD == @{u}`, staged count is zero, and every remaining dirty file is one of the pre-existing unrelated `.meta` files.
