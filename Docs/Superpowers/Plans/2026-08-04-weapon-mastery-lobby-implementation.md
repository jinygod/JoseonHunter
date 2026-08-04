# Weapon Mastery Lobby Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a functional three-menu lobby that lets the player select a starting weapon and mastered legacy style, earn weapon mastery from final blows, purchase styles and small common training with coins, and return to the lobby with all run rewards persisted.

**Architecture:** Keep mastery, loadout, training, and settlement rules in the pure Domain assembly; extend the existing transactional JSON save path to schema version 2; use one persistent `MetaGameSession` as the runtime boundary between Lobby and Gameplay. Reuse the existing eight weapons and sixteen legacy paths, while scene-specific presenters render immutable view models and send commands to the session.

**Tech Stack:** Unity 6000.5.5f1, C# 9, Unity Test Framework 1.7.0, uGUI, TextMeshPro, URP 17.5, JSON save repository, PixelLab pixel-art generation, Android ARM64 IL2CPP.

## Global Constraints

- Lobby exposes only `무기 연구`, `출전`, and `공통 수련`; no placeholder tabs.
- Mastery is awarded only to the weapon that delivered the final blow.
- Victory, defeat, and player abandonment preserve 100% of earned mastery and coins.
- Every weapon has exactly three styles: base plus its two existing legacy paths.
- Legacy style 1 requires 2,000 mastery and 800 coins; style 2 requires 8,000 mastery and 2,400 coins.
- Common training has six tracks, five ranks, and costs `100, 180, 280, 420, 600` coins.
- Permanent numerical bonuses cap at 10% per common-training track.
- Existing schema-1 saves must migrate without losing coins, equipment, evolution, investigation, settings, or records.
- All user-facing copy is Korean and uses the existing ChosunGs/MaruBuri runtime fonts.
- Pixel art uses a restrained earth/ink/pale-jade palette with no white outline and no text baked into the image.
- Unity batch processes run sequentially at `BelowNormal` priority with processor affinity `63` to avoid saturating the workstation.
- Commit and push `master` after every completed task; stage only files owned by that task.
- Preserve the pre-existing dirty scene, font, rendering, project-setting, and unrelated `.meta` files.

---

### Task 1: Pure Weapon Mastery, Loadout, Training, and Settlement Rules

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponMasteryCatalog.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponMasteryProgression.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/CommonTrainingProgression.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/PatrolLoadout.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/RunSettlement.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponMasteryProgressionTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/CommonTrainingProgressionTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/PatrolLoadoutTests.cs`

**Interfaces:**
- Produces: `WeaponMasteryCatalog.All`, `WeaponMasteryCatalog.StylesFor(WeaponId)`.
- Produces: `WeaponMasteryProgression.CanPurchase(WeaponId, WeaponLegacyPathId)` and `Purchase(...)`.
- Produces: `CommonTrainingProgression.Purchase(CommonTrainingId)` and `Reset()`.
- Produces: immutable `PatrolLoadout` and `RunSettlement` domain values used by save/session/gameplay tasks.

- [ ] **Step 1: Write failing catalog and mastery tests**

```csharp
[Test]
public void EveryWeaponHasBaseAndTwoExistingLegacyStyles()
{
    Assert.That(WeaponMasteryCatalog.All, Has.Count.EqualTo(WeaponRoster.All.Count));
    foreach (var weapon in WeaponRoster.All)
    {
        var styles = WeaponMasteryCatalog.StylesFor(weapon);
        Assert.That(styles, Has.Count.EqualTo(3));
        Assert.That(styles[0].IsBase, Is.True);
        Assert.That(styles.Skip(1).All(style => WeaponLegacyCatalog.TryGet(style.LegacyPathId, out _)), Is.True);
    }
}

[Test]
public void StylePurchaseRequiresMasteryAndCoinsAndNeverConsumesMastery()
{
    var data = SaveDataV1.CreateDefaults();
    data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 2000;
    data.Coins = 800;
    var result = new WeaponMasteryProgression(data).Purchase(
        WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSplitFletching);
    Assert.That(result.Success, Is.True);
    Assert.That(data.Coins, Is.Zero);
    Assert.That(data.WeaponMasteryPoints[WeaponId.GakgungShot.Value], Is.EqualTo(2000));
    Assert.That(data.UnlockedWeaponStyles, Contains.Item(WeaponLegacyPathId.GakgungSplitFletching.Value));
}
```

- [ ] **Step 2: Run the new EditMode fixtures and verify RED**

Run Unity EditMode with filter:

```text
JoseonHunter.Tests.EditMode.WeaponMasteryProgressionTests
JoseonHunter.Tests.EditMode.CommonTrainingProgressionTests
JoseonHunter.Tests.EditMode.PatrolLoadoutTests
```

Expected: compile failure because the new catalog, progression, loadout, settlement, and save fields do not exist.

- [ ] **Step 3: Implement the domain types and exact initial content values**

```csharp
public readonly struct WeaponMasteryStyleDefinition
{
    public WeaponMasteryStyleDefinition(WeaponId weaponId, string styleId,
        WeaponLegacyPathId legacyPathId, string displayName, string benefit,
        string tradeoff, int requiredMastery, int coinCost, bool isBase)
    {
        WeaponId = weaponId;
        StyleId = styleId;
        LegacyPathId = legacyPathId;
        DisplayName = displayName;
        Benefit = benefit;
        Tradeoff = tradeoff;
        RequiredMastery = requiredMastery;
        CoinCost = coinCost;
        IsBase = isBase;
    }
    public WeaponId WeaponId { get; }
    public string StyleId { get; }
    public WeaponLegacyPathId LegacyPathId { get; }
    public string DisplayName { get; }
    public string Benefit { get; }
    public string Tradeoff { get; }
    public int RequiredMastery { get; }
    public int CoinCost { get; }
    public bool IsBase { get; }
}

public enum CommonTrainingId { Vitality, Power, Footwork, Learning, Guard, Resonance }

public sealed class PatrolLoadout
{
    public PatrolLoadout(string name, WeaponId startingWeapon,
        IReadOnlyDictionary<WeaponId, WeaponLegacyPathId> styles, string difficultyId);
    public WeaponLegacyPathId StyleFor(WeaponId weaponId);
}
```

Use `WeaponLegacyCatalog` display/benefit/tradeoff text for the sixteen non-base style cards. Normalize invalid or locked selections to the base style, represented by an empty `WeaponLegacyPathId`.

- [ ] **Step 4: Implement common-training transactions and settlement merging**

```csharp
public readonly struct RunSettlement
{
    public RunSettlement(IReadOnlyDictionary<WeaponId, int> mastery,
        int coins, int kills, float elapsed, bool victory, bool abandoned);
}

public sealed class CommonTrainingProgression
{
    public static readonly int[] Costs = { 100, 180, 280, 420, 600 };
    public ProgressionResult Purchase(CommonTrainingId id);
    public ProgressionResult Reset();
    public float Multiplier(CommonTrainingId id);
}
```

All mutations operate on a copy and commit only after every validation passes. `Reset()` returns exactly the recorded spent coins and clears all six ranks and spent totals.

- [ ] **Step 5: Run the three new fixtures and the existing meta-progression fixture**

Expected: all new tests and `JoseonHunter.Tests.EditMode.MetaProgressionTests` pass.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Progression Assets/JoseonHunter/Tests/EditMode/WeaponMasteryProgressionTests.cs Assets/JoseonHunter/Tests/EditMode/CommonTrainingProgressionTests.cs Assets/JoseonHunter/Tests/EditMode/PatrolLoadoutTests.cs
git commit -m "feat: add weapon mastery domain rules"
git push origin master
```

---

### Task 2: Save Schema 2 and Transactional Meta Commands

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/SaveDataV1.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/ISaveRepository.cs`
- Modify: `Assets/JoseonHunter/Scripts/Infrastructure/Save/JsonSaveRepository.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/SaveRecoveryTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/MetaProgressionTests.cs`

**Interfaces:**
- Consumes: `PatrolLoadout`, `RunSettlement`, `WeaponMasteryProgression`, `CommonTrainingProgression`.
- Produces: schema-2 fields on `SaveDataV1` and atomic methods on `AutoSaveOrchestrator`.

- [ ] **Step 1: Add failing migration and round-trip tests**

```csharp
[Test]
public void SchemaOnePayloadMigratesToSchemaTwoWithoutLosingExistingProgress()
{
    WriteSchemaOneFixture(coins: 777, equipmentLevel: 4, clue: "clue_03");
    var loaded = new JsonSaveRepository(directory).Load();
    Assert.That(loaded.Data.SchemaVersion, Is.EqualTo(2));
    Assert.That(loaded.Data.Coins, Is.EqualTo(777));
    Assert.That(loaded.Data.EquipmentLevels["weapon_01"], Is.EqualTo(4));
    Assert.That(loaded.Data.InvestigationClues, Contains.Item("clue_03"));
    Assert.That(loaded.Data.PatrolLoadouts, Has.Count.EqualTo(3));
}

[Test]
public void FailedStyleSaveLeavesCoinsAndUnlocksUnchanged()
{
    var data = ReadyGakgungSave();
    var orchestrator = new AutoSaveOrchestrator(new AlwaysFailRepository(), data);
    var result = orchestrator.PurchaseWeaponStyle(
        WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSplitFletching);
    Assert.That(result.Success, Is.False);
    Assert.That(data.Coins, Is.EqualTo(800));
    Assert.That(data.UnlockedWeaponStyles, Is.Empty);
}
```

- [ ] **Step 2: Run migration tests and verify RED**

Expected: missing schema-2 fields and transaction methods.

- [ ] **Step 3: Extend the live save model with deep-copy-safe fields**

```csharp
public int SchemaVersion = 2;
public Dictionary<string, int> WeaponMasteryPoints = new();
public List<string> UnlockedWeaponStyles = new();
public Dictionary<string, int> CommonTrainingRanks = new();
public Dictionary<string, int> CommonTrainingSpentCoins = new();
public List<PatrolLoadoutData> PatrolLoadouts = new();
public int ActivePatrolLoadoutIndex;
```

`CreateDefaults`, `Copy`, and `CopyFrom` must initialize and deep-copy all new collections. Create exactly three normalized loadouts with `hwando_flying_blade` as the first starting weapon and base style for all eight weapons.

- [ ] **Step 4: Teach `JsonSaveRepository` to read schema 1 and 2 and always write schema 2**

Use explicit serializable entry arrays for mastery, training, and style mappings. Reject versions below 1 or above 2. A schema-1 document must pass through `SaveDataV1.CreateDefaults()` and overlay only old fields; a schema-2 document overlays both old and new fields.

- [ ] **Step 5: Add atomic autosave commands**

```csharp
public TransactionResult PurchaseWeaponStyle(WeaponId weaponId, WeaponLegacyPathId styleId);
public TransactionResult PurchaseCommonTraining(CommonTrainingId id);
public TransactionResult ResetCommonTraining();
public TransactionResult SavePatrolLoadout(int index, PatrolLoadout loadout);
public TransactionResult CommitRun(RunSettlement settlement);
```

Extend `AutoSaveTrigger` with `WeaponStylePurchase`, `CommonTrainingPurchase`, `CommonTrainingReset`, `LoadoutChanged`, and `RunAbandoned`.

- [ ] **Step 6: Run all save and meta fixtures**

Expected: `MetaSaveMigrationTests`, `SaveRecoveryTests`, and `MetaProgressionTests` all pass with no schema-1 regression.

- [ ] **Step 7: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Save Assets/JoseonHunter/Scripts/Infrastructure/Save Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs Assets/JoseonHunter/Tests/EditMode/SaveRecoveryTests.cs Assets/JoseonHunter/Tests/EditMode/MetaProgressionTests.cs
git commit -m "feat: persist weapon mastery and loadouts"
git push origin master
```

---

### Task 3: Final-Blow Attribution and Run Mastery Ledger

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/RunWeaponKillLedger.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponKillLedgerTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableCombatPlayModeTests.cs`

**Interfaces:**
- Produces: `RunWeaponKillLedger.RecordHit(int, WeaponId)`, `ConfirmDeath(int, EnemyMasteryClass)`, and `Snapshot()`.
- Produces: `FirstPlayableController.RunMasterySnapshotForTests`, an immutable copy of the current ledger used by the focused PlayMode test.

- [ ] **Step 1: Write final-blow and enemy-weight tests**

```csharp
[Test]
public void LastConfirmedWeaponOwnsTheKill()
{
    var ledger = new RunWeaponKillLedger();
    ledger.RecordHit(7, WeaponId.HwandoFlyingBlade);
    ledger.RecordHit(7, WeaponId.GakgungShot);
    ledger.ConfirmDeath(7, EnemyMasteryClass.Normal);
    Assert.That(ledger.PointsFor(WeaponId.HwandoFlyingBlade), Is.Zero);
    Assert.That(ledger.PointsFor(WeaponId.GakgungShot), Is.EqualTo(1));
}

[TestCase(EnemyMasteryClass.Normal, 1)]
[TestCase(EnemyMasteryClass.Special, 3)]
[TestCase(EnemyMasteryClass.Elite, 10)]
[TestCase(EnemyMasteryClass.MidBoss, 30)]
[TestCase(EnemyMasteryClass.FinalBoss, 100)]
public void DeathClassMapsToApprovedPoints(EnemyMasteryClass enemyClass, int expected)
{
    var ledger = new RunWeaponKillLedger();
    ledger.RecordHit(11, WeaponId.HwandoFlyingBlade);
    Assert.That(ledger.ConfirmDeath(11, enemyClass), Is.EqualTo(expected));
    Assert.That(ledger.PointsFor(WeaponId.HwandoFlyingBlade), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run `WeaponKillLedgerTests` and verify RED**

Expected: missing ledger and classification types.

- [ ] **Step 3: Implement a pure integer ledger**

```csharp
public enum EnemyMasteryClass { Normal, Special, Elite, MidBoss, FinalBoss }

public sealed class RunWeaponKillLedger
{
    public void RecordHit(int targetRuntimeId, WeaponId weaponId);
    public int ConfirmDeath(int targetRuntimeId, EnemyMasteryClass enemyClass);
    public IReadOnlyDictionary<WeaponId, int> Snapshot();
    public void Reset();
}
```

Ignore empty weapon IDs. Remove a target's last-hit record after death so duplicate death notifications cannot score twice.

- [ ] **Step 4: Wire confirmed damage and actual enemy death in `FirstPlayableController`**

Subscribe once to `CombatDamageService.DamageConfirmed`. On every confirmed hit call `RecordHit`. In the existing enemy-death path, classify the `EnemyState` before removal and call `ConfirmDeath`. Do not infer death from damage amount alone and do not save per kill.

- [ ] **Step 5: Run ledger tests plus focused combat PlayMode tests**

Expected: a spawned enemy killed by a forced weapon hit contributes only to that weapon, boss flags map to 100, and reset clears the ledger.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Progression/RunWeaponKillLedger.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/EditMode/WeaponKillLedgerTests.cs Assets/JoseonHunter/Tests/PlayMode/FirstPlayableCombatPlayModeTests.cs
git commit -m "feat: track mastery from weapon final blows"
git push origin master
```

---

### Task 4: Persistent Meta Session and Scene Routing

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/JoseonHunter.Runtime.asmdef`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Meta/MetaGameSession.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Meta/GameSceneRouter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/BootstrapLoadingPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/BootstrapLoadingBuilder.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/MetaGameSessionPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/BootstrapLoadingPlayModeTests.cs`

**Interfaces:**
- Consumes: `JsonSaveRepository`, autosave commands, `PatrolLoadout`, and `RunSettlement`.
- Produces: singleton `MetaGameSession.Current`, `Data`, `ActiveLoadout`, transaction wrappers, and `GameSceneRouter`.

- [ ] **Step 1: Write failing session lifetime and bootstrap destination tests**

```csharp
[UnityTest]
public IEnumerator BootstrapLoadsLobbyAfterSaveInitialization()
{
    SceneManager.LoadScene("Bootstrap");
    yield return WaitForScene("Lobby");
    Assert.That(MetaGameSession.Current, Is.Not.Null);
    Assert.That(MetaGameSession.Current.Data.SchemaVersion, Is.EqualTo(2));
}

[UnityTest]
public IEnumerator SessionSurvivesLobbyGameplayLobbyRoundTrip()
{
    var session = MetaGameSession.CreateForTests(repository);
    var id = session.GetInstanceID();
    yield return session.Router.LoadGameplay();
    yield return session.Router.LoadLobby();
    Assert.That(MetaGameSession.Current.GetInstanceID(), Is.EqualTo(id));
}
```

- [ ] **Step 2: Run the two PlayMode fixtures and verify RED**

Expected: missing session/router and Bootstrap still targets Gameplay.

- [ ] **Step 3: Add the Infrastructure assembly reference and implement the session**

```csharp
public sealed class MetaGameSession : MonoBehaviour
{
    public static MetaGameSession Current { get; private set; }
    public SaveDataV1 Data { get; private set; }
    public PatrolLoadout ActiveLoadout { get; }
    public TransactionResult PurchaseStyle(WeaponId weaponId, WeaponLegacyPathId styleId);
    public TransactionResult PurchaseTraining(CommonTrainingId id);
    public TransactionResult ResetTraining();
    public TransactionResult SaveLoadout(int index, PatrolLoadout loadout);
    public TransactionResult CommitRun(RunSettlement settlement);
}
```

Create it in Bootstrap, mark it `DontDestroyOnLoad`, reject duplicates, and load the repository exactly once. `OnApplicationPause(true)` saves the current data.

- [ ] **Step 4: Implement guarded async scene routing**

```csharp
public sealed class GameSceneRouter
{
    public bool IsTransitioning { get; }
    public IEnumerator LoadLobby();
    public IEnumerator LoadGameplay();
}
```

Reject duplicate requests while `IsTransitioning`. Update `BootstrapLoadingPresenter` default destination and readiness contract from Gameplay to Lobby; retain timeout and Korean failure copy.

- [ ] **Step 5: Run bootstrap/session fixtures and existing scene-load fixtures**

Expected: Bootstrap→Lobby passes, duplicate session is destroyed, and existing loading timeout tests remain green.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/JoseonHunter.Runtime.asmdef Assets/JoseonHunter/Scripts/Runtime/Meta Assets/JoseonHunter/Scripts/Presentation/UI/BootstrapLoadingPresenter.cs Assets/JoseonHunter/Scripts/Editor/Scenes/BootstrapLoadingBuilder.cs Assets/JoseonHunter/Tests/PlayMode/MetaGameSessionPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/BootstrapLoadingPlayModeTests.cs
git commit -m "feat: add persistent meta session routing"
git push origin master
```

---

### Task 5: Apply Starting Weapon, Equipped Legacy Styles, and Common Training

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponLegacyChoicePresenter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/PatrolLoadoutGameplayTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyFlowPlayModeTests.cs`

**Interfaces:**
- Consumes: `MetaGameSession.ActiveLoadout`, style snapshots, and common-training multipliers.
- Produces: gameplay whose initial weapon/stats and later acquired weapon styles match the active loadout.

- [ ] **Step 1: Write failing loadout integration tests**

```csharp
[UnityTest]
public IEnumerator SelectedStartingWeaponReplacesHardCodedHwando()
{
    ConfigureActiveLoadout(WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSunPiercer);
    yield return LoadGameplay();
    Assert.That(controller.RegisteredWeaponIds.Single(), Is.EqualTo(WeaponId.GakgungShot));
    Assert.That(controller.LegacyForTests(WeaponId.GakgungShot).PathId,
        Is.EqualTo(WeaponLegacyPathId.GakgungSunPiercer));
}

[UnityTest]
public IEnumerator CommonTrainingChangesInitialStatsButNeverExceedsTenPercent()
{
    ConfigureAllTrainingAtRankFive();
    yield return LoadGameplay();
    Assert.That(controller.StartingMaximumHealthForTests, Is.EqualTo(110f).Within(.01f));
    Assert.That(controller.StartingDamageMultiplierForTests, Is.EqualTo(1.10f).Within(.001f));
}
```

- [ ] **Step 2: Run focused fixture and verify RED**

Expected: gameplay still starts with Hwando and ignores session loadout/training.

- [ ] **Step 3: Seed run state from the active loadout before executors are built**

At run reset, read one immutable loadout snapshot. Register only `StartingWeapon`; prepopulate `WeaponLegacyState` for all non-base equipped styles; apply six common-training multipliers to the existing health, damage, movement, XP, incoming-damage, and pickup-radius calculations.

- [ ] **Step 4: Remove production entry into the level-three legacy modal**

Keep `WeaponLegacyChoicePresenter` available for old isolated presentation tests, but stop `FirstPlayableController` from opening it during a normal run. Replace the level-three gate with the standard upgrade-selection flow and update legacy-flow tests to assert that the equipped pre-run style remains stable.

- [ ] **Step 5: Run loadout, legacy, eight-weapon, and evolution fixtures**

Expected: all eight starting weapons are accepted; both legacy styles per weapon reach their existing executors; base style has no path; in-run level 3 no longer pauses for a legacy choice.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyState.cs Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs Assets/JoseonHunter/Scripts/Presentation/UI/WeaponLegacyChoicePresenter.cs Assets/JoseonHunter/Tests/PlayMode/PatrolLoadoutGameplayTests.cs Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyFlowPlayModeTests.cs
git commit -m "feat: apply patrol weapon loadouts"
git push origin master
```

---

### Task 6: PixelLab Lobby Background and Import Contract

**Files:**
- Create: `Assets/JoseonHunter/Art/UI/Lobby/lobby_courtyard.png`
- Create: `Assets/JoseonHunter/Art/UI/Lobby/lobby_courtyard.png.meta`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/LobbyAssetContractTests.cs`

**Interfaces:**
- Produces: one opaque 9:16 PixelLab lobby background sprite at the exact path above.

- [ ] **Step 1: Write a failing asset contract test**

```csharp
[Test]
public void LobbyBackgroundIsOpaquePortraitPixelArtWithPointFiltering()
{
    const string path = "Assets/JoseonHunter/Art/UI/Lobby/lobby_courtyard.png";
    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
    Assert.That(texture, Is.Not.Null);
    Assert.That(texture.height, Is.GreaterThan(texture.width));
    Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.None));
    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
    Assert.That(importer.mipmapEnabled, Is.False);
}
```

- [ ] **Step 2: Run `LobbyAssetContractTests` and verify RED because the file is missing**

- [ ] **Step 3: Generate the background through PixelLab**

Use `mcp__pixellab__create_image_pixflux` with this fixed art direction:

```text
Vertical 9:16 pixel-art background, quiet Joseon government patrol courtyard and wooden porch at dusk, restrained muted earth brown, ink black and pale jade palette, large clean pixel clusters, simple empty center behind a character and UI, no people, no text, no symbols, no white outlines, no glossy lighting, readable mobile game background.
```

Poll the matching PixelLab result tool, download the approved frame to the exact asset path, inspect it locally, and reject outputs containing text, people, white outlines, or noisy micro-pixels.

- [ ] **Step 4: Add the narrow import rule**

Apply Sprite (2D and UI), Single, Point, no mipmaps, no alpha, no Android override, and a max size sufficient for the generated portrait dimensions. Do not change other art import profiles.

- [ ] **Step 5: Run asset contract plus the existing import-profile fixture**

Expected: the new lobby asset and all existing art import tests pass.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Art/UI/Lobby Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs Assets/JoseonHunter/Tests/EditMode/LobbyAssetContractTests.cs
git commit -m "art: add restrained pixel lobby background"
git push origin master
```

---

### Task 7: Authored Lobby Scene Shell and Three-Menu Navigation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyNavigationPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`
- Create: `Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab`
- Modify: `Assets/JoseonHunter/Scenes/Lobby.unity`
- Test: `Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`

**Interfaces:**
- Consumes: `MetaGameSession` and the generated background sprite.
- Produces: authored scene roots and three content containers passed to later presenters.

- [ ] **Step 1: Write failing scene hierarchy and navigation tests**

```csharp
[Test]
public void LobbySceneContainsInspectableProductionHierarchy()
{
    var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);
    AssertRoot(scene, "Lobby Camera");
    AssertRoot(scene, "Lobby Canvas");
    AssertNamed(scene, "Lobby Root");
    AssertNamed(scene, "Header");
    AssertNamed(scene, "Patrol Panel");
    AssertNamed(scene, "Weapon Research Panel");
    AssertNamed(scene, "Common Training Panel");
    AssertNamed(scene, "Bottom Navigation");
    AssertRoot(scene, "EventSystem");
}

[UnityTest]
public IEnumerator LobbyShowsOnlyThreeNavigationButtonsAndDefaultsToPatrol()
{
    yield return LoadLobby();
    Assert.That(NavigationButtons(), Has.Count.EqualTo(3));
    Assert.That(ActivePanel().name, Is.EqualTo("Patrol Panel"));
}
```

- [ ] **Step 2: Run both fixtures and verify RED against the empty Lobby scene**

- [ ] **Step 3: Implement the lobby shell builder**

Build a 720×1280 reference Canvas with safe-area root, opaque background sprite, top header, three empty content panels, and bottom navigation. Add exactly these Korean buttons: `무기 연구`, `출전`, `공통 수련`. Use `JoseonUiPalette` solid hanji/ink colors and existing runtime fonts.

- [ ] **Step 4: Generate and save the prefab and scene without touching dirty open scenes**

`LobbySceneBuilder.Build()` must refuse to replace a dirty open Lobby scene, mirror the safety behavior of `BootstrapLoadingBuilder`, instantiate `LobbyShell.prefab`, and save an inspectable hierarchy.

- [ ] **Step 5: Run scene and navigation tests at 720×1280 and 1080×2340**

Expected: all three panels stay in the safe area, only one is active, the coin header renders, and there are no placeholder tabs.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/Lobby Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab Assets/JoseonHunter/Scenes/Lobby.unity Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs
git commit -m "feat: build functional lobby shell"
git push origin master
```

---

### Task 8: Weapon Research Menu

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes: mastery catalog, save-backed mastery points/unlocks, session purchase and loadout commands.
- Produces: eight-weapon selection and three-state style cards with purchase/equip interactions.

- [ ] **Step 1: Write failing menu state and purchase tests**

```csharp
[UnityTest]
public IEnumerator ResearchMenuShowsEightWeaponsAndThreeStylesForSelection()
{
    yield return LoadLobbyAndOpen("무기 연구");
    Assert.That(WeaponButtons(), Has.Count.EqualTo(8));
    SelectWeapon(WeaponId.GakgungShot);
    Assert.That(StyleCards(), Has.Count.EqualTo(3));
    Assert.That(StatusFor(WeaponLegacyPathId.GakgungSplitFletching), Is.EqualTo("연구 중"));
}

[UnityTest]
public IEnumerator ReadyStylePurchaseDeductsCoinsOnceAndCanBeEquipped()
{
    ConfigureReadyStyle(WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSplitFletching, 800);
    yield return LoadResearch();
    Click("해금"); Click("해금");
    Assert.That(session.Data.Coins, Is.Zero);
    Assert.That(StatusFor(WeaponLegacyPathId.GakgungSplitFletching), Is.EqualTo("해금 완료"));
    Click("장착");
    Assert.That(session.ActiveLoadout.StyleFor(WeaponId.GakgungShot),
        Is.EqualTo(WeaponLegacyPathId.GakgungSplitFletching));
}
```

- [ ] **Step 2: Run the research fixture and verify RED**

- [ ] **Step 3: Implement immutable view-model construction**

```csharp
public sealed class WeaponResearchView
{
    public WeaponId WeaponId { get; }
    public int Mastery { get; }
    public int NextRequirement { get; }
    public IReadOnlyList<WeaponStyleView> Styles { get; }
}

public enum WeaponStyleUiState { Base, Researching, Purchasable, Unlocked, Equipped }
```

View models contain final Korean strings and button-enabled states; presenter code does not recalculate progression rules.

- [ ] **Step 4: Build readable list/detail UI**

Use one reusable button per weapon, one progress bar, and exactly three style cards. Each card shows benefit, tradeoff, requirement, price, and explicit state copy. No white outline, transparent panel, English text, or baked image text.

- [ ] **Step 5: Run research, save round-trip, and portrait layout tests**

Expected: purchase is idempotent, insufficient states show exact shortage, equipped selection persists after reloading Lobby, and long Korean strings remain inside cards.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs
git commit -m "feat: add lobby weapon research"
git push origin master
```

---

### Task 9: Common Training Menu

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes: session-backed `CommonTrainingProgression` commands and six training view models.
- Produces: purchase previews, shortage feedback, and full-refund reset UI.

- [ ] **Step 1: Write failing purchase-preview and reset tests**

```csharp
[UnityTest]
public IEnumerator TrainingShowsCurrentNextAndExactCost()
{
    yield return LoadLobbyAndOpen("공통 수련");
    SelectTraining(CommonTrainingId.Vitality);
    Assert.That(CurrentText(), Is.EqualTo("현재 최대 체력 +0%"));
    Assert.That(NextText(), Is.EqualTo("강화 후 최대 체력 +2%"));
    Assert.That(CostText(), Is.EqualTo("필요 엽전 100"));
}

[UnityTest]
public IEnumerator ResetRefundsEveryRecordedCoinAndClearsRanks()
{
    ConfigureTraining(new[] { 2, 1, 0, 0, 0, 0 }, coins: 0);
    yield return LoadTraining();
    Click("전체 초기화"); ConfirmReset();
    Assert.That(session.Data.Coins, Is.EqualTo(380));
    Assert.That(session.Data.CommonTrainingRanks.Values, Has.All.Zero);
}
```

- [ ] **Step 2: Run fixture and verify RED**

- [ ] **Step 3: Implement six fixed training cards and detail preview**

Use the approved Korean names `활력`, `완력`, `보법`, `학습`, `호신`, `감응`. A purchase refreshes header coins and all dependent views without reconstructing the whole Canvas.

- [ ] **Step 4: Add explicit free-reset confirmation**

The confirmation shows the exact refund amount. Cancel changes nothing. Confirm calls the one atomic session transaction; save failure leaves ranks and coins unchanged and displays `저장하지 못했습니다. 다시 시도해 주세요.`

- [ ] **Step 5: Run training UI, domain, save, and portrait tests**

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs
git commit -m "feat: add common training lobby menu"
git push origin master
```

---

### Task 10: Patrol Menu, Three Presets, and Gameplay Launch

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolLoadoutEditorPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`

**Interfaces:**
- Consumes: three normalized save loadouts, weapon/style availability, and scene router.
- Produces: starting-weapon/style preset editing and guarded Gameplay launch.

- [ ] **Step 1: Write failing preset persistence and launch tests**

```csharp
[UnityTest]
public IEnumerator PatrolEditorPersistsThreeIndependentPresets()
{
    yield return LoadPatrol();
    EditPreset(0, "각궁 저격", WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSunPiercer);
    EditPreset(1, "빙결 시험", WeaponId.FrostFlask, WeaponLegacyPathId.FrostShatter);
    ReloadLobby();
    AssertPreset(0, "각궁 저격", WeaponId.GakgungShot);
    AssertPreset(1, "빙결 시험", WeaponId.FrostFlask);
}

[UnityTest]
public IEnumerator PatrolButtonSavesActivePresetAndLoadsGameplayOnlyOnce()
{
    yield return LoadPatrol();
    ClickPatrolTwiceSameFrame();
    yield return WaitForScene("Gameplay");
    Assert.That(router.GameplayLoadCountForTests, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run fixture and verify RED**

- [ ] **Step 3: Build the default patrol panel**

Show the existing `rookie_constable.png`, preset name/index, starting weapon icon/name, equipped style, difficulty, best record, edit button, and one large `출전` button. Do not add locked menus around it.

- [ ] **Step 4: Build the preset editor**

Allow switching among three presets, selecting any of eight starting weapons, and selecting only unlocked styles. Style selection for all eight weapons is saved in the preset even when they are not the starting weapon. Invalid input displays a Korean message and preserves the old preset.

- [ ] **Step 5: Connect guarded scene launch and run focused tests**

Disable all launch/edit input while routing. Save the active preset before calling `LoadGameplay`; if saving fails, remain in Lobby and re-enable input.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolLoadoutEditorPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs
git commit -m "feat: add patrol presets and launch flow"
git push origin master
```

---

### Task 11: Run Settlement, Abandonment, and Lobby Return

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/RunSettlementLobbyPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`

**Interfaces:**
- Consumes: run mastery ledger snapshot, run coins/record, `MetaGameSession.CommitRun`, and scene router.
- Produces: exactly-once settlement for victory, defeat, and confirmed abandonment, plus Lobby return.

- [ ] **Step 1: Write failing exactly-once and abandon tests**

```csharp
[UnityTest]
public IEnumerator DefeatSettlementPersistsCoinsAndMasteryOnceThenReturnsToLobby()
{
    yield return LoadConfiguredGameplay();
    AwardRunProgress(WeaponId.GakgungShot, mastery: 13, coins: 21);
    controller.EndRunForTests(false);
    Click("로비로 돌아가기"); Click("로비로 돌아가기");
    yield return WaitForScene("Lobby");
    Assert.That(session.Data.Coins, Is.EqualTo(startCoins + 21));
    Assert.That(session.Data.WeaponMasteryPoints[WeaponId.GakgungShot.Value],
        Is.EqualTo(startMastery + 13));
}

[UnityTest]
public IEnumerator ConfirmedAbandonmentKeepsOneHundredPercentOfEarnedProgress()
{
    yield return LoadConfiguredGameplay();
    AwardRunProgress(WeaponId.ThunderCrashBomb, mastery: 9, coins: 7);
    OpenReturn(); ConfirmReturn();
    yield return WaitForScene("Lobby");
    AssertSavedDelta(WeaponId.ThunderCrashBomb, mastery: 9, coins: 7);
}
```

- [ ] **Step 2: Run settlement fixture and verify RED**

- [ ] **Step 3: Add one cached settlement state to the controller**

Build `RunSettlement` once when a run ends or abandonment is confirmed. Call the session transaction once, cache success/failure, and never apply the same settlement twice. Reset clears the cache only when a new run actually starts.

- [ ] **Step 4: Replace restart copy with Lobby return copy and add abandon confirmation**

Change result button to `로비로 돌아가기`. Add a small `귀환` HUD button that opens an opaque hanji confirmation: `현재까지 획득한 숙련도와 엽전을 저장하고 로비로 돌아갑니다.` Buttons are `돌아가기` and `계속 전투`.

- [ ] **Step 5: Route all successful exits to Lobby and handle save failure**

On save failure, stay in Gameplay with paused flow and show `전투 기록을 저장하지 못했습니다. 다시 시도해 주세요.` with a retry action. Do not silently restart or discard progress.

- [ ] **Step 6: Run settlement, HUD, modal-flow, and scene-round-trip fixtures**

Expected: victory, defeat, and abandon each save once; repeated button input is ignored; all paths return to Lobby; no progress is lost.

- [ ] **Step 7: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs Assets/JoseonHunter/Tests/PlayMode/RunSettlementLobbyPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs
git commit -m "feat: settle runs and return to lobby"
git push origin master
```

---

### Task 12: Integrated Validation and Handoff

**Files:**
- Create: `Docs/Verification/2026-08-04-weapon-mastery-lobby.md`
- Modify only if a real regression is found: changed production/test files from Tasks 1–11

**Interfaces:**
- Consumes: the complete feature.
- Produces: reproducible test/build evidence and final remote `master` state.

- [ ] **Step 1: Inspect the final owned diff and scene/build configuration**

Record `git rev-parse HEAD`, `git status --short`, Unity version, enabled scenes, package versions, and the exact owned file list. Confirm Bootstrap, Lobby, and Gameplay are enabled in that order and no unrelated dirty file is staged.

- [ ] **Step 2: Run targeted changed-area tests**

Run all new EditMode fixtures:

```text
WeaponMasteryProgressionTests
CommonTrainingProgressionTests
PatrolLoadoutTests
MetaSaveMigrationTests
WeaponKillLedgerTests
LobbyAssetContractTests
LobbySceneContractTests
```

Run all new PlayMode fixtures:

```text
MetaGameSessionPlayModeTests
PatrolLoadoutGameplayTests
LobbyNavigationPlayModeTests
WeaponResearchLobbyPlayModeTests
CommonTrainingLobbyPlayModeTests
LobbyPatrolPlayModeTests
RunSettlementLobbyPlayModeTests
```

Expected: zero failures and zero ignored tests.

- [ ] **Step 3: Capture and inspect representative Lobby screens**

Use a batch capture method in `LobbySceneBuilder` to render at 720×1280 and 1080×2340:

- default Patrol
- Weapon Research with a locked style
- Weapon Research with a purchasable style
- Common Training detail
- preset editor
- insufficient-coin feedback

Inspect each PNG for Korean glyphs, safe-area containment, opaque panels, limited palette, no white outlines, and no overlapping text.

- [ ] **Step 4: Run full EditMode and PlayMode suites**

Use direct Unity batch processes with filters `JoseonHunter.Tests.EditMode` and `JoseonHunter.Tests.PlayMode`. Record exact totals, durations, failed/skipped counts, and XML result paths.

- [ ] **Step 5: Build and inspect the Android development APK**

Run `JoseonHunter.Editor.Build.AndroidDevelopmentBuild.Build` in batch mode with `GRADLE_USER_HOME=C:\jh-gradle`. Verify:

- non-empty `Builds/Android/JoseonHunter-development.apk`
- package `com.jinygod.joseonhunter`
- version `0.1.0` / code `1`
- min SDK 26 / target SDK 36
- `arm64-v8a/libil2cpp.so`
- SHA-256 and build log success

- [ ] **Step 6: Write the verification report**

The report must classify every acceptance criterion as Passed, Failed, Blocked, or Not Run; distinguish introduced failures from pre-existing dirty files/warnings; and state that physical-device frame pacing, thermal, and touch validation remain limitations unless actually performed.

- [ ] **Step 7: Commit and push validation evidence**

```powershell
git add -- Docs/Verification/2026-08-04-weapon-mastery-lobby.md
git commit -m "docs: verify weapon mastery lobby"
git push origin master
```

- [ ] **Step 8: Confirm remote synchronization**

Verify `git rev-parse HEAD` equals `git rev-parse origin/master`, the staging area is empty, and only the preserved pre-existing unrelated changes remain unstaged.
