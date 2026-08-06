# Stage Difficulty Progression Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add three visible stages with per-stage Normal/Omen/Great Omen progression, persistent clear records, Stage 1 difficulty scaling, and a lobby selection flow without duplicating unfinished Stage 2/3 combat.

**Architecture:** Pure Domain C# owns stage IDs, difficulty profiles, clear records, and derived unlock rules. Save schema 4 persists records and the current selection; the existing atomic autosave transaction records victories and applies reward multipliers. The lobby selects only unlocked nodes, while runtime combat consumes the selected difficulty profile and keeps the existing fifteen-minute Stage 1 timeline.

**Tech Stack:** Unity 6.0 (`6000.5.5f1`), C# Domain/Infrastructure/Runtime/Presentation assemblies, Unity Test Framework with NUnit, runtime-built portrait uGUI/TMP.

## Global Constraints

- A new account can play only `Stage 1 / Normal`.
- Normal victory unlocks the same stage's Omen and the next stage's Normal.
- Omen victory unlocks the same stage's Great Omen.
- Account level and common training never gate stage progression.
- Stage 2 and Stage 3 remain visible but cannot launch until authored combat definitions exist; Stage 1 is never reused as fake content.
- Enemy active count remains capped at `StagePacingTimeline.MobileActiveCap` (140).
- Existing coins, weapon mastery, account experience, common training, and loadout data must survive schema migration.
- All player-facing copy added by this feature is Korean.
- Unrelated dirty `.meta` files are excluded from every commit.

---

### Task 1: Domain stage catalog, profiles, and unlock rules

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/StageProgression.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StageProgressionTests.cs`
- Create: Unity `.meta` files for both new files

**Interfaces:**
- Produces: `StageId`, `StageDifficulty`, `StageSelection`, `StageClearRecord`, `StageCatalog`, `StageDifficultyProfile`, and `StageUnlockRules`.
- Consumes: `StagePacingTimeline.MobileActiveCap` for density clamping.

- [ ] **Step 1: Write failing unlock and profile tests**

```csharp
[Test]
public void NewAccountUnlocksOnlyStageOneNormal()
{
    var records = Array.Empty<StageClearRecord>();
    Assert.That(StageUnlockRules.IsUnlocked(new StageSelection(StageId.GwigokField, StageDifficulty.Normal), records), Is.True);
    Assert.That(StageUnlockRules.IsUnlocked(new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal), records), Is.False);
    Assert.That(StageUnlockRules.IsUnlocked(new StageSelection(StageId.GwigokField, StageDifficulty.Omen), records), Is.False);
}

[Test]
public void StageOneNormalVictoryOpensNextNormalAndCurrentOmen()
{
    var records = new[] { StageClearRecord.Victory(new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35) };
    Assert.That(StageUnlockRules.IsUnlocked(new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal), records), Is.True);
    Assert.That(StageUnlockRules.IsUnlocked(new StageSelection(StageId.GwigokField, StageDifficulty.Omen), records), Is.True);
    Assert.That(StageUnlockRules.IsUnlocked(new StageSelection(StageId.GwigokField, StageDifficulty.GreatOmen), records), Is.False);
}
```

- [ ] **Step 2: Run the focused EditMode test and verify it fails**

Run:

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Mode EditMode -TestFilter JoseonHunter.Tests.EditMode.StageProgressionTests
```

Expected: compilation failure because the stage progression types do not exist.

- [ ] **Step 3: Implement immutable domain types and derived rules**

Implement exact stable IDs and labels:

```csharp
public static readonly StageId GwigokField = new("stage_01_gwigok_field");
public static readonly StageId DokkaebiPass = new("stage_02_dokkaebi_pass");
public static readonly StageId MoonlitTomb = new("stage_03_moonlit_tomb");

public enum StageDifficulty { Normal, Omen, GreatOmen }
```

`StageCatalog` exposes all three ordered stage definitions, Korean names (`귀곡 들판`, `도깨비 고개`, `월식 왕릉`), and `HasPlayableContent=true` only for Stage 1. `StageUnlockRules` derives all locks from victory records and returns exact Korean lock reasons. `StageDifficultyProfile.For` returns the design multipliers and clamps scaled active caps to 140.

- [ ] **Step 4: Run focused tests and verify they pass**

Run the Task 1 command again. Expected: all `StageProgressionTests` pass.

- [ ] **Step 5: Commit the domain slice**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Runs/StageProgression.cs Assets/JoseonHunter/Scripts/Domain/Runs/StageProgression.cs.meta Assets/JoseonHunter/Tests/EditMode/StageProgressionTests.cs Assets/JoseonHunter/Tests/EditMode/StageProgressionTests.cs.meta
git commit -m "feat: add stage difficulty progression rules"
git push origin master
```

### Task 2: Schema 4 clear records and selection transaction

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/ProjectIdentity.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/SaveDataV1.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/ISaveRepository.cs`
- Modify: `Assets/JoseonHunter/Scripts/Infrastructure/Save/JsonSaveRepository.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Meta/MetaGameSession.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs`

**Interfaces:**
- Consumes: Task 1 `StageSelection`, `StageClearRecord`, and `StageUnlockRules`.
- Produces: schema 4 fields `SelectedStageId`, `SelectedStageDifficulty`, `StageClearRecords`; `MetaGameSession.ActiveStageSelection`; `SaveStageSelection(StageSelection)`.

- [ ] **Step 1: Add failing migration, copy, and selection tests**

Add tests proving schema 3 victory records migrate to Stage 1 Normal, schema 4 round trips all records, `Copy/CopyFrom` preserve detached records, and locked selections return `ProgressionError.InvalidSelection` without saving.

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Mode EditMode -TestFilter JoseonHunter.Tests.EditMode.MetaSaveMigrationTests
```

Expected: failures because schema 4 fields and selection methods are missing.

- [ ] **Step 3: Add serializable record DTOs and schema migration**

Add `StageClearRecordData` to the save domain with `StageId`, `Difficulty`, `Victory`, `BestElapsed`, `BestKills`, and `BestLevel`. Raise `ProjectIdentity.SaveSchemaVersion` to 4. `JsonSaveRepository` accepts schemas 1–4, normalizes duplicate keys by victory/logical maximum rules, migrates a schema 1–3 save containing `BestPatrolResults["victory_kills"]` into a Stage 1 Normal victory, and otherwise starts with no victories.

- [ ] **Step 4: Add atomic selection saving**

Add `AutoSaveTrigger.StageSelectionChanged`, `AutoSaveOrchestrator.SaveStageSelection`, and `MetaGameSession.SaveStageSelection`. Reject unknown or locked selections. Allow unlocked-but-not-yet-authored stages to be selected so the lobby can show `아직 준비 중인 지역입니다`; launching is handled separately.

- [ ] **Step 5: Run focused migration tests and verify pass**

Run the Task 2 command again. Expected: all migration tests pass.

- [ ] **Step 6: Commit and push the persistence slice**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/ProjectIdentity.cs Assets/JoseonHunter/Scripts/Domain/Save/SaveDataV1.cs Assets/JoseonHunter/Scripts/Domain/Save/ISaveRepository.cs Assets/JoseonHunter/Scripts/Infrastructure/Save/JsonSaveRepository.cs Assets/JoseonHunter/Scripts/Runtime/Meta/MetaGameSession.cs Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs
git commit -m "feat: persist stage clears and selection"
git push origin master
```

### Task 3: Atomic victory rewards and Stage 1 difficulty combat scaling

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/RunSettlement.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/AccountProgression.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/ISaveRepository.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/BossAttackPattern.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/AccountProgressionTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/BossAttackPatternTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/StagePacingPlayModeTests.cs`

**Interfaces:**
- Consumes: Task 1 `StageDifficultyProfile` and Task 2 active selection/save records.
- Produces: settlement records keyed by selection, multiplied coins/account XP/mastery, scaled enemy health/damage/density, and difficulty-aware boss pressure.

- [ ] **Step 1: Write failing reward and combat profile tests**

Add tests proving Normal/Omen/Great Omen rewards use `1.00/1.35/1.75` coins, `1.00/1.25/1.50` account XP, and `1.00/1.20/1.40` mastery; first victory writes one merged record; save failure rolls every reward and record back; Omen and Great Omen health/contact damage increase; density never exceeds 140.

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Mode EditMode -TestFilter "JoseonHunter.Tests.EditMode.AccountProgressionTests|JoseonHunter.Tests.EditMode.BossAttackPatternTests|JoseonHunter.Tests.EditMode.MetaSaveMigrationTests"
```

Expected: new reward/profile assertions fail.

- [ ] **Step 3: Extend settlement and atomic commit**

Add a `StageSelection` property to `RunSettlement`, preserving the existing constructor as a Stage 1 Normal overload. In `CommitRun`, resolve the profile, round multiplied integer rewards with `MidpointRounding.AwayFromZero`, merge the clear record only on victory, and save everything through the existing copy/save/`CopyFrom` transaction.

- [ ] **Step 4: Apply combat profile at run reset and spawn**

At `ResetRun`, read `MetaGameSession.ActiveStageSelection` and cache its profile. Apply health and contact-damage multipliers to normal enemies, elites, midbosses, and final boss. Scale spawn interval and active cap through the profile while preserving the 140 cap. Increase elite probability by `0/.04/.08` for Normal/Omen/Great Omen.

- [ ] **Step 5: Add readable boss pressure without shortening warnings**

Give `BossAttackController` a pressure tier. Omen shortens recovery to `0.88`, Great Omen to `0.75`; warnings keep their existing durations. At Great Omen, first midboss alternates slam/volley and second midboss alternates charge/slam. Final boss retains all three attacks and enters its faster phase below 65% health instead of 50%.

- [ ] **Step 6: Run focused EditMode and Stage pacing PlayMode tests**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Mode EditMode -TestFilter "JoseonHunter.Tests.EditMode.AccountProgressionTests|JoseonHunter.Tests.EditMode.BossAttackPatternTests|JoseonHunter.Tests.EditMode.MetaSaveMigrationTests"
& .\Tools\Unity\Test-Unity.ps1 -Mode PlayMode -TestFilter JoseonHunter.Tests.PlayMode.StagePacingPlayModeTests
```

Expected: all focused tests pass.

- [ ] **Step 7: Commit and push the runtime slice**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Progression/RunSettlement.cs Assets/JoseonHunter/Scripts/Domain/Progression/AccountProgression.cs Assets/JoseonHunter/Scripts/Domain/Save/ISaveRepository.cs Assets/JoseonHunter/Scripts/Domain/Combat/BossAttackPattern.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/EditMode/AccountProgressionTests.cs Assets/JoseonHunter/Tests/EditMode/BossAttackPatternTests.cs Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs Assets/JoseonHunter/Tests/PlayMode/StagePacingPlayModeTests.cs
git commit -m "feat: apply stage difficulty to combat and rewards"
git push origin master
```

### Task 4: Lobby stage and difficulty selection

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`

**Interfaces:**
- Consumes: `MetaGameSession.ActiveStageSelection`, `SaveStageSelection`, `StageCatalog`, and `StageUnlockRules`.
- Produces: stage carousel, three difficulty buttons, Korean lock/coming-soon feedback, and a launch guard.

- [ ] **Step 1: Add failing lobby tests**

Tests verify the initial screen shows `귀곡 들판`, only `보통` is interactable, locked buttons show their exact condition, Stage 1 Normal victory exposes Stage 2 Normal and Stage 1 Omen, and Stage 2 displays `아직 준비 중인 지역입니다` with the launch button disabled.

- [ ] **Step 2: Run focused PlayMode tests and verify failure**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Mode PlayMode -TestFilter JoseonHunter.Tests.PlayMode.LobbyPatrolPlayModeTests
```

Expected: missing stage/difficulty controls.

- [ ] **Step 3: Build and bind the selection controls**

Create runtime UI objects named `Previous Stage`, `Next Stage`, `Difficulty Normal`, `Difficulty Omen`, and `Difficulty Great Omen`. Reflow the existing hero and weapon selector to keep the start button large. Stage browsing may show locked stages, but selection persistence occurs only for unlocked nodes.

- [ ] **Step 4: Guard launch and show exact Korean feedback**

`StartPatrol` checks unlock and `StageDefinition.HasPlayableContent`. Locked nodes show the rule reason; Stage 2/3 show `아직 준비 중인 지역입니다`; only authored Stage 1 nodes route to Gameplay.

- [ ] **Step 5: Run focused lobby tests and verify pass**

Run the Task 4 command again. Expected: all lobby patrol tests pass.

- [ ] **Step 6: Commit and push the lobby slice**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs
git commit -m "feat: add lobby stage difficulty selection"
git push origin master
```

### Task 5: Result clarity, regression validation, and documentation closeout

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/RunSettlementLobbyPlayModeTests.cs`
- Modify: `Docs/Superpowers/Specs/2026-08-06-stage-difficulty-progression-design.md`

**Interfaces:**
- Consumes: completed settlement selection and unlock results.
- Produces: Korean result lines for stage/difficulty and newly opened nodes; verified final feature.

- [ ] **Step 1: Add failing result presentation tests**

Verify the victory result shows `귀곡 들판 · 보통`, `새 지역: 도깨비 고개 · 보통`, and `새 난이도: 귀곡 들판 · 흉조`, while defeat shows no unlock lines.

- [ ] **Step 2: Run focused result tests and verify failure**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Mode PlayMode -TestFilter JoseonHunter.Tests.PlayMode.RunSettlementLobbyPlayModeTests
```

Expected: result state does not yet expose stage/unlock labels.

- [ ] **Step 3: Add result state and Korean presentation**

Extend `FirstPlayableUiState` with stage name, difficulty name, actual multiplied coin/mastery/account rewards, and newly unlocked node labels. Populate them only after successful settlement. Render them beneath the existing run summary without English fallback text.

- [ ] **Step 4: Run all automated validation**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Mode EditMode
& .\Tools\Unity\Test-Unity.ps1 -Mode PlayMode
```

Expected: the full EditMode and PlayMode suites pass with zero failures.

- [ ] **Step 5: Validate project load and Android development build**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UnityProjects\JoseonHunter' -logFile 'D:\UnityProjects\JoseonHunter\Logs\stage-progression-load.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\UnityProjects\JoseonHunter' -executeMethod JoseonHunter.Editor.Build.AndroidDevelopmentBuild.Build -logFile 'D:\UnityProjects\JoseonHunter\Logs\stage-progression-android.log'
```

Expected: both commands exit 0; logs contain no compile errors and the APK is produced at the build script's configured path.

- [ ] **Step 6: Mark the spec implemented and commit only relevant files**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs Assets/JoseonHunter/Tests/PlayMode/RunSettlementLobbyPlayModeTests.cs Docs/Superpowers/Specs/2026-08-06-stage-difficulty-progression-design.md
git commit -m "docs: verify stage difficulty progression"
git push origin master
```
