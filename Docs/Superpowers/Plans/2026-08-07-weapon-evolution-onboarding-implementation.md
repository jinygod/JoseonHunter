# Weapon Evolution Onboarding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unlock both Hwando paths for every account, activate the equipped path at weapon level 4, complete it at level 5, and make the level-5 choice unmistakable.

**Architecture:** Keep lobby research as the permanent unlock/loadout layer, but separate an equipped path from its active stage in `WeaponLegacyState`. Extend upgrade state and view models with typed final-evolution metadata. Reuse the eight legacy executors, enforce a two-dimension evolution contract, and tune only behavior that fails that contract.

**Tech Stack:** Unity 6000.5.5f1, C#, NUnit EditMode/PlayMode, Unity UI, TextMeshPro, JSON saves, Android ARM64 IL2CPP.

## Global Constraints

- Work in `D:/UnityProjects/JoseonHunter` on `master`; preserve unrelated dirty `.meta` files.
- Commit each testable task and push every meaningful commit to `origin/master`.
- Use TDD and run focused tests before broad suites.
- Run Unity sequentially at BelowNormal priority with affinity mask 15.
- Grant 독니 and 월식 without consuming mastery or coins.
- Keep other paths at 2,000/8,000 mastery and 800/2,400 coins with sequential unlocks.
- Only the path equipped before sortie may activate.
- Levels 1-3 use base behavior, level 4 activates Reinforced, level 5 activates Completed.
- Guarantee one level-5-ready equipped weapon in the three upgrade offers.
- Final-evolution UI uses opaque ink/crimson/gold with no white outline.
- Preserve portrait safe areas, Korean copy, and the current save schema when possible.

---

## File Structure Map

- `WeaponMasteryCatalog.cs`: starter path metadata.
- `SaveDataV1.cs`, `JsonSaveRepository.cs`: new/existing account normalization.
- `WeaponResearchPresenter.cs`: free switching UI.
- `WeaponLegacyState.cs`: equipped path versus active stage.
- `ProgressionTypes.cs`, `UpgradeSelector.cs`: guaranteed final offer.
- `FirstPlayableUiState.cs`, `FirstPlayableController.cs`: typed cards and milestones.
- `UpgradeChoicePresenter.cs`: final-evolution presentation.
- `WeaponLegacyTypes.cs`, `WeaponLegacyCatalog.cs`: sixteen-path behavior contract.
- Existing weapon executor files: modify only when focused behavior tests fail.

---

### Task 1: Grant and Normalize Hwando Starter Paths

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponMasteryCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/SaveDataV1.cs`
- Modify: `Assets/JoseonHunter/Scripts/Infrastructure/Save/JsonSaveRepository.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponMasteryProgressionTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs`

**Interfaces:**
- Produces: `WeaponMasteryStyleDefinition.IsStarterUnlocked : bool`.
- Produces: `WeaponMasteryCatalog.StarterPathsFor(WeaponId)`.

- [ ] **Step 1: Write failing default and migration tests**

```csharp
[Test]
public void New_account_unlocks_both_hwando_paths_and_equips_venom_for_free()
{
    var data = SaveDataV1.CreateDefaults();
    Assert.That(data.UnlockedWeaponStyles, Does.Contain(WeaponLegacyPathId.HwandoVenom.Value));
    Assert.That(data.UnlockedWeaponStyles, Does.Contain(WeaponLegacyPathId.HwandoMoonEclipse.Value));
    Assert.That(data.PatrolLoadouts[0].WeaponStyleIds[WeaponId.HwandoFlyingBlade.Value],
        Is.EqualTo(WeaponLegacyPathId.HwandoVenom.Value));
    Assert.That(data.Coins, Is.Zero);
}
```

```csharp
[Test]
public void Current_schema_load_adds_free_paths_and_preserves_valid_moon_eclipse()
{
    var data = SaveDataV1.CreateDefaults();
    data.UnlockedWeaponStyles.Clear();
    data.PatrolLoadouts[0].WeaponStyleIds[WeaponId.HwandoFlyingBlade.Value] =
        WeaponLegacyPathId.HwandoMoonEclipse.Value;
    var repository = new JsonSaveRepository(directory);
    Assert.That(repository.Save(data).Success, Is.True);

    var loaded = repository.Load().Data;

    Assert.That(loaded.UnlockedWeaponStyles, Does.Contain(WeaponLegacyPathId.HwandoVenom.Value));
    Assert.That(loaded.UnlockedWeaponStyles, Does.Contain(WeaponLegacyPathId.HwandoMoonEclipse.Value));
    Assert.That(loaded.PatrolLoadouts[0].WeaponStyleIds[WeaponId.HwandoFlyingBlade.Value],
        Is.EqualTo(WeaponLegacyPathId.HwandoMoonEclipse.Value));
}
```

- [ ] **Step 2: Run and verify failure**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.WeaponMasteryProgressionTests;JoseonHunter.Tests.EditMode.MetaSaveMigrationTests' -testResults 'Artifacts/weapon-evolution-task1.xml' -logFile 'Artifacts/weapon-evolution-task1.log' -quit
```

Expected: tests fail because free paths are absent.

- [ ] **Step 3: Add starter metadata and defaults**

```csharp
public bool IsStarterUnlocked { get; }

public static IReadOnlyList<WeaponMasteryStyleDefinition> StarterPathsFor(WeaponId weaponId) =>
    Array.AsReadOnly(StylesFor(weaponId).Where(style => style.IsStarterUnlocked).ToArray());
```

Build both Hwando legacy styles with mastery 0, coin 0, starter true. Add both IDs once in `CreateDefaults()` and equip venom in all default loadouts.

- [ ] **Step 4: Normalize loaded saves idempotently**

```csharp
private static void NormalizeStarterWeaponStyles(SaveDataV1 data)
{
    AddUnique(data.UnlockedWeaponStyles, WeaponLegacyPathId.HwandoVenom.Value);
    AddUnique(data.UnlockedWeaponStyles, WeaponLegacyPathId.HwandoMoonEclipse.Value);
    foreach (var loadout in data.PatrolLoadouts)
    {
        var current = loadout.WeaponStyleIds.TryGetValue(WeaponId.HwandoFlyingBlade.Value, out var value)
            ? value : string.Empty;
        if (current != WeaponLegacyPathId.HwandoVenom.Value &&
            current != WeaponLegacyPathId.HwandoMoonEclipse.Value)
            loadout.WeaponStyleIds[WeaponId.HwandoFlyingBlade.Value] =
                WeaponLegacyPathId.HwandoVenom.Value;
    }
}
```

Call it after unlocks and loadouts are deserialized.

- [ ] **Step 5: Re-run, commit, and push**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Progression/WeaponMasteryCatalog.cs Assets/JoseonHunter/Scripts/Domain/Save/SaveDataV1.cs Assets/JoseonHunter/Scripts/Infrastructure/Save/JsonSaveRepository.cs Assets/JoseonHunter/Tests/EditMode/WeaponMasteryProgressionTests.cs Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs
git commit -m "feat: unlock starter hwando evolutions"
git push origin master
```

---

### Task 2: Present Free Hwando Switching in the Lobby

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes `IsStarterUnlocked`.
- Produces exact labels 처음부터 해금 and 장착 중.

- [ ] **Step 1: Write the failing free-switch test**

```csharp
[UnityTest]
public IEnumerator Hwando_paths_switch_without_spending_coins()
{
    var data = SaveDataV1.CreateDefaults();
    data.Coins = 155;
    MetaGameSession.EnsureExists(new MemoryRepository(data));
    SceneManager.LoadScene("Lobby");
    yield return null;
    var presenter = Object.FindAnyObjectByType<WeaponResearchPresenter>(FindObjectsInactive.Include);
    Assert.That(presenter.SelectedStyleStateForTests(1), Is.EqualTo("장착 중"));
    Assert.That(presenter.SelectedStyleStateForTests(2), Is.EqualTo("처음부터 해금"));
    presenter.ActivateStyleForTests(2);
    Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(155));
}
```

- [ ] **Step 2: Run the fixture and verify label failure**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.WeaponResearchLobbyPlayModeTests' -testResults 'Artifacts/weapon-evolution-task2.xml' -logFile 'Artifacts/weapon-evolution-task2.log' -quit
```

- [ ] **Step 3: Render starter-specific state**

```csharp
if (style.IsStarterUnlocked &&
    session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value))
    return "처음부터 해금";
```

Starter cards show 이름, state, and 처음부터 해금 · 눌러서 장착, without mastery fractions.

- [ ] **Step 4: Re-run, commit, and push**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs
git commit -m "feat: present free hwando evolution choices"
git push origin master
```

---

### Task 3: Separate Equipped Paths from Active Stages

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponLegacyStateTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyFlowPlayModeTests.cs`

**Interfaces:**
- Produces: `TryGetEquippedPath(WeaponId, out WeaponLegacyPathId) : bool`.
- Equipped stages: None at 1-3, Reinforced at 4, Completed at 5.

- [ ] **Step 1: Write failing stage tests**

```csharp
[TestCase(1, WeaponLegacyStage.None)]
[TestCase(3, WeaponLegacyStage.None)]
[TestCase(4, WeaponLegacyStage.Reinforced)]
[TestCase(5, WeaponLegacyStage.Completed)]
public void Equipped_path_activates_at_four_and_completes_at_five(int level, WeaponLegacyStage expected)
{
    var state = new WeaponLegacyState();
    state.EquipForRun(WeaponId.HwandoFlyingBlade, WeaponLegacyPathId.HwandoVenom);
    Assert.That(state.TryGetEquippedPath(WeaponId.HwandoFlyingBlade, out var path), Is.True);
    Assert.That(path, Is.EqualTo(WeaponLegacyPathId.HwandoVenom));
    Assert.That(state.SnapshotFor(WeaponId.HwandoFlyingBlade, level).Stage, Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run and verify levels 1-3 fail**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.WeaponLegacyStateTests' -testResults 'Artifacts/weapon-evolution-task3-edit.xml' -logFile 'Artifacts/weapon-evolution-task3-edit.log' -quit
```

- [ ] **Step 3: Implement stage separation**

```csharp
public bool TryGetEquippedPath(WeaponId weaponId, out WeaponLegacyPathId pathId) =>
    selectedPaths.TryGetValue(weaponId, out pathId);

if (equippedFromRunStart.Contains(weaponId))
{
    if (weaponLevel < 4) return default;
    return new WeaponLegacySnapshot(pathId,
        weaponLevel >= 5 ? WeaponLegacyStage.Completed : WeaponLegacyStage.Reinforced);
}
```

Preserve the level-3 Chosen behavior only for paths selected by the isolated no-meta flow.

- [ ] **Step 4: Update HUD milestones**

Use `TryGetEquippedPath` even when the active snapshot is None:

```csharp
var hasEquipped = weaponLegacyState.TryGetEquippedPath(weaponId, out var equippedPath);
var legacyName = hasEquipped && WeaponLegacyCatalog.TryGet(equippedPath, out var equippedDefinition)
    ? equippedDefinition.DisplayName : "미선택";
var stageName = legacy.Stage == WeaponLegacyStage.None && hasEquipped
    ? "4레벨에 진화 발현" : LegacyStageName(legacy.Stage);
var milestone = legacy.Stage == WeaponLegacyStage.None && hasEquipped
    ? $"{legacyName} 장착 · 무기 4레벨에 발현"
    : legacy.Stage == WeaponLegacyStage.Reinforced && hasEquipped
        ? $"무기 5레벨에 {equippedDefinition.CompletionName} 완성"
        : NextLegacyMilestone(legacy.Stage);
```

- [ ] **Step 5: Add Gameplay level 3/4/5 assertions, run, commit, and push**

```csharp
[UnityTest]
public IEnumerator Meta_equipped_venom_is_inactive_at_three_then_reinforced_and_completed()
{
    var data = SaveDataV1.CreateDefaults();
    MetaGameSession.EnsureExists(new MemoryRepository(data));
    SceneManager.LoadScene("Gameplay");
    yield return null;
    yield return null;
    var controller = Object.FindAnyObjectByType<FirstPlayableController>();
    controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 3);
    Assert.That(controller.LegacySnapshotForTests(WeaponId.HwandoFlyingBlade).Stage,
        Is.EqualTo(WeaponLegacyStage.None));
    controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 4);
    Assert.That(controller.LegacySnapshotForTests(WeaponId.HwandoFlyingBlade).Stage,
        Is.EqualTo(WeaponLegacyStage.Reinforced));
    controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
    Assert.That(controller.LegacySnapshotForTests(WeaponId.HwandoFlyingBlade).Stage,
        Is.EqualTo(WeaponLegacyStage.Completed));
}
```

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.WeaponLegacyFlowPlayModeTests' -testResults 'Artifacts/weapon-evolution-task3-play.xml' -logFile 'Artifacts/weapon-evolution-task3-play.log' -quit
git add Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyState.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/EditMode/WeaponLegacyStateTests.cs Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyFlowPlayModeTests.cs
git commit -m "feat: activate weapon paths at evolution levels"
git push origin master
```

---

### Task 4: Guarantee and Describe Evolution Offers

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/ProgressionTypes.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyFlowPlayModeTests.cs`

**Interfaces:**
- Produces `UpgradePresentationTier { Standard, Evolution, FinalEvolution }`.
- Produces `UpgradeChoiceView.PresentationTier` and `LegacyPathId`.
- Produces `UpgradeState.FinalEvolutionReadyWeaponIds`.

- [ ] **Step 1: Write the guaranteed-offer test**

```csharp
[Test]
public void Level_five_ready_weapon_is_guaranteed_in_three_offers()
{
    var state = FinalReadyState(WeaponId.HwandoFlyingBlade);
    for (var seed = 0; seed < 40; seed++)
        Assert.That(UpgradeSelector.Select(state, seed, 9).Any(offer =>
            offer.Id == WeaponId.HwandoFlyingBlade.Value && offer.NextLevel == 5), Is.True);
}
```

- [ ] **Step 2: Run selector tests and verify failure**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.UpgradeEvolutionTests' -testResults 'Artifacts/weapon-evolution-task4-edit.xml' -logFile 'Artifacts/weapon-evolution-task4-edit.log' -quit
```

- [ ] **Step 3: Extend state and selector**

```csharp
var finalEvolution = eligible.FirstOrDefault(offer =>
    offer.Kind == UpgradeKind.Weapon && offer.NextLevel == 5 &&
    state.FinalEvolutionReadyWeaponIds.Contains(offer.Id));
if (!string.IsNullOrEmpty(finalEvolution.Id)) offers.Add(finalEvolution);
```

Old `UpgradeState` overloads pass an empty set. Existing ID deduplication prevents duplicates.

- [ ] **Step 4: Add typed view metadata and path-aware copy**

```csharp
public enum UpgradePresentationTier { Standard, Evolution, FinalEvolution }

var final = offer.NextLevel == 5;
return new UpgradeChoiceView(offer.Id, offer.Kind, offer.NextLevel,
    final ? "최종 진화" : "진화 발현",
    final ? legacy.CompletionName : $"{legacy.DisplayName} · {WeaponDisplayName(offer.Id)}",
    final ? legacy.CompletionSummary : legacy.Benefit,
    final ? "최종 기술 완성" : legacy.Cost,
    ResolveWeaponSprite(weaponId),
    final ? UpgradePresentationTier.FinalEvolution : UpgradePresentationTier.Evolution,
    pathId);
```

- [ ] **Step 5: Assert only the equipped path appears**

```csharp
UpgradeChoiceState opened = null;
controller.UpgradeOpened += state => opened = state;
controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 4);
controller.SetUpgradeOffersForTests(new UpgradeOffer(
    WeaponId.HwandoFlyingBlade.Value, UpgradeKind.Weapon, 5));
var final = opened.Choices.Single();
Assert.That(final.PresentationTier, Is.EqualTo(UpgradePresentationTier.FinalEvolution));
Assert.That(final.LegacyPathId, Is.EqualTo(WeaponLegacyPathId.HwandoMoonEclipse));
Assert.That(final.Name, Is.EqualTo("환도·월식"));
Assert.That(opened.Choices.All(choice => !choice.Name.Contains("독니")), Is.True);
```

- [ ] **Step 6: Run, commit, and push**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Progression/ProgressionTypes.cs Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyFlowPlayModeTests.cs
git commit -m "feat: guarantee final evolution choices"
git push origin master
```

---

### Task 5: Add the Final-Evolution Presentation

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`

**Interfaces:**
- Consumes `UpgradeChoiceView.PresentationTier`.
- Produces test hooks `IsFinalEvolutionPresentationForTests` and `HeadingForTests`.

- [ ] **Step 1: Write the failing presentation test**

```csharp
[UnityTest]
public IEnumerator Final_evolution_uses_opaque_special_overlay_and_korean_heading()
{
    var go = new GameObject("Final Evolution Presenter");
    var presenter = go.AddComponent<UpgradeChoicePresenter>();
    presenter.BuildForTests();
    presenter.Open(FinalEvolutionChoices(), _ => true);
    yield return new WaitForSecondsRealtime(.25f);
    Assert.That(presenter.IsFinalEvolutionPresentationForTests, Is.True);
    Assert.That(presenter.HeadingForTests, Is.EqualTo("최종 진화가 깨어납니다"));
    Assert.That(go.transform.Find("Upgrade Choice Overlay").GetComponent<Image>().color.a, Is.EqualTo(1f));
    Object.Destroy(go);
}
```

- [ ] **Step 2: Run and verify missing state**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.UpgradeChoicePlayModeTests' -testResults 'Artifacts/weapon-evolution-task5.xml' -logFile 'Artifacts/weapon-evolution-task5.log' -quit
```

- [ ] **Step 3: Add special styling**

```csharp
private static readonly Color FinalOverlay = new(.035f, .018f, .025f, 1f);
private static readonly Color FinalInterior = new(.22f, .055f, .035f, 1f);
private static readonly Color MutedInterior = new(.47f, .43f, .34f, 1f);
```

Detect FinalEvolution from typed choices, show 최종 진화가 깨어납니다, use a gold frame and FinalInterior on the final card, and use MutedInterior on standard cards.

- [ ] **Step 4: Add input-safe intro and pulse**

Lock input for the first 0.2 unscaled seconds. Pulse only the final card between scale 1 and 1.025, stop it on close, restore all scales, and preserve the accepted-choice lock.

- [ ] **Step 5: Run presentation and portrait tests**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.UpgradeChoicePlayModeTests;JoseonHunter.Tests.PlayMode.PortraitUiLayoutPlayModeTests' -testResults 'Artifacts/weapon-evolution-task5-all.xml' -logFile 'Artifacts/weapon-evolution-task5-all.log' -quit
```

- [ ] **Step 6: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs
git commit -m "feat: add final evolution choice presentation"
git push origin master
```

---

### Task 6: Enforce Distinct Behavior for All Sixteen Paths

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyTypes.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyCatalog.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponLegacyCatalogTests.cs`
- Test: the four existing weapon-family legacy PlayMode fixtures.
- Modify only on failing behavior: matching executors under `Runtime/Combat/Weapons`.

**Interfaces:**
- Produces `WeaponLegacyDefinition.ChangedDimensions`.
- Every path declares two distinct dimensions and has observable staged behavior.

- [ ] **Step 1: Write the failing catalog test**

```csharp
[Test]
public void Every_path_changes_at_least_two_distinct_combat_dimensions()
{
    foreach (var path in WeaponLegacyCatalog.All)
        Assert.That(path.ChangedDimensions.Distinct().Count(), Is.GreaterThanOrEqualTo(2), path.Id.Value);
}
```

- [ ] **Step 2: Run and verify the property is absent**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.WeaponLegacyCatalogTests' -testResults 'Artifacts/weapon-evolution-task6-edit.xml' -logFile 'Artifacts/weapon-evolution-task6-edit.log' -quit
```

- [ ] **Step 3: Declare exact dimension pairs**

| Path | Dimensions |
| --- | --- |
| 독니 / 월식 | EnemyResponse+Payoff / Geometry+Payoff |
| 관일 / 갈래깃 | Rhythm+Payoff / Geometry+Rhythm |
| 천쇄봉인 / 원귀폭발 | EnemyResponse+Payoff / Rhythm+Geometry |
| 뇌옥 / 지맥 | EnemyResponse+Payoff / Rhythm+EnemyResponse |
| 사방수호 / 수호신강림 | Geometry+EnemyResponse / Rhythm+Payoff |
| 화룡포 / 화망 | Rhythm+Geometry / Geometry+Payoff |
| 빙무 / 파쇄 | Geometry+EnemyResponse / EnemyResponse+Payoff |
| 진공 / 천뢰 | Geometry+EnemyResponse / Geometry+Rhythm |

- [ ] **Step 4: Add observable family assertions**

Retain and run the existing executable contracts that already assert the declared dimensions:

- `Venom_applies_four_second_poison_and_moon_return_hits_for_seventy_percent`
- `Completed_venom_focuses_poisoned_enemy_while_moon_path_does_not`
- `Split_fletching_launches_three_five_then_completed_fourth_volley_seven`
- `Completed_sun_piercer_scales_each_penetration_caps_and_adds_boss_bonus`
- `Heaven_seal_lasts_two_seconds_and_completed_death_chain_caps_at_four`
- `Ghost_burst_delays_then_reinforces_and_completed_chain_caps_at_three`
- `Thunder_prison_pulls_for_one_second_and_completed_core_deals_three_hundred_percent`
- `Completed_earth_current_death_propagates_to_at_most_five_targets`
- `Completed_four_guardians_emits_three_synchronized_eighty_percent_pulses`
- `Guardian_descent_reinforces_second_slam_and_completed_replaces_it_with_center_slam`
- `Fire_dragon_prioritizes_strongest_and_completed_fires_five_capped_salvos`
- `Fire_net_ticks_for_three_seconds_and_completed_detonates_connected_trail_once`
- `Completed_mist_freezes_on_third_hit_and_emits_three_sixty_percent_blooms`
- `Frost_shatter_consumes_freeze_and_chains_three_then_five_targets`
- `Vacuum_builds_three_bleed_stacks_then_reinforced_ruptures_and_cleans_up`
- `Heaven_thunder_bounces_four_then_completed_seven_and_explodes_marked_center`

If one of these named tests fails after the level-gate change, modify only that path's stage gate or tuning value until the named assertion passes; add no new particle system.

- [ ] **Step 5: Run all family fixtures**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.HwandoGakgungLegacyPlayModeTests;JoseonHunter.Tests.PlayMode.TalismanThunderLegacyPlayModeTests;JoseonHunter.Tests.PlayMode.JangseungSingijeonLegacyPlayModeTests;JoseonHunter.Tests.PlayMode.FrostFanLegacyPlayModeTests' -testResults 'Artifacts/weapon-evolution-task6-play.xml' -logFile 'Artifacts/weapon-evolution-task6-play.log' -quit
```

- [ ] **Step 6: Commit and push**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyTypes.cs Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyCatalog.cs Assets/JoseonHunter/Tests/EditMode/WeaponLegacyCatalogTests.cs Assets/JoseonHunter/Tests/PlayMode Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons
git commit -m "feat: enforce distinct weapon evolution behaviors"
git push origin master
```

---

### Task 7: Consolidate the Parallel Evolution Flow

**Files:**
- Inspect/modify: `WeaponEvolutionCatalog.cs`, `WeaponEvolutionState.cs`, `FirstPlayableController.cs`
- Test: `UpgradeEvolutionTests.cs`, `EvolvedWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Produces one player-facing authority: `WeaponLegacyCatalog + WeaponLegacyState`.

- [ ] **Step 1: Classify references**

```powershell
rg -n "WeaponEvolutionCatalog|WeaponEvolutionState|UpgradeKind[.]Evolution|acquiredEvolutionIds|unlockedUpgradeIds" Assets/JoseonHunter -g "*.cs"
```

If production selection never creates Evolution offers, remove the dead controller branch and forced test hooks. If it is reachable, adapt it to the same typed level-5 legacy card and prevent duplicate modifiers.

- [ ] **Step 2: Add the single-authority test**

```csharp
[Test]
public void Selector_never_emits_parallel_catalog_evolution_offers()
{
    var state = FullyUnlockedStateWithLevelFourHwando();
    for (var seed = 0; seed < 100; seed++)
        Assert.That(UpgradeSelector.Select(state, seed, 12)
            .Any(offer => offer.Kind == UpgradeKind.Evolution), Is.False);
}
```

- [ ] **Step 3: Remove only proven unreachable references**

The only level-5 player card must be the weapon-level card with FinalEvolution. Retain compatibility data still read by saves or editor tools.

- [ ] **Step 4: Run regression tests, commit, and push**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.WeaponEvolutionCatalogTests;JoseonHunter.Tests.EditMode.UpgradeEvolutionTests' -testResults 'Artifacts/weapon-evolution-task7-edit.xml' -logFile 'Artifacts/weapon-evolution-task7-edit.log' -quit
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EvolvedWeaponCombatPlayModeTests;JoseonHunter.Tests.PlayMode.WeaponLegacyFlowPlayModeTests' -testResults 'Artifacts/weapon-evolution-task7-play.xml' -logFile 'Artifacts/weapon-evolution-task7-play.log' -quit
git add Assets/JoseonHunter/Scripts Assets/JoseonHunter/Tests
git commit -m "refactor: unify runtime weapon evolution flow"
git push origin master
```

---

### Task 8: Full Verification and Android Build

**Files:**
- Create: `Docs/Verification/2026-08-07-weapon-evolution-onboarding.md`
- Modify: `Docs/AI/UnityProjectContext.md`

**Interfaces:**
- Produces exact test counts, APK bytes, checksum, and manual-check notes.

- [ ] **Step 1: Run complete EditMode**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform EditMode -testResults 'Artifacts/weapon-evolution-full-edit.xml' -logFile 'Artifacts/weapon-evolution-full-edit.log' -quit
```

- [ ] **Step 2: Run complete PlayMode**

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProjects/JoseonHunter' -runTests -testPlatform PlayMode -testResults 'Artifacts/weapon-evolution-full-play.xml' -logFile 'Artifacts/weapon-evolution-full-play.log' -quit
```

- [ ] **Step 3: Build and hash Android APK**

```powershell
& ./Tools/Unity/Build-AndroidDevelopment.ps1
Get-Item ./Builds/Android/JoseonHunter-development.apk | Select-Object Length, LastWriteTime
Get-FileHash -Algorithm SHA256 ./Builds/Android/JoseonHunter-development.apk
```

- [ ] **Step 4: Write exact verification evidence**

Record XML totals, APK bytes/SHA-256, and manual checks for free switching, levels 1-3 base behavior, level-4 equipped path only, level-5 special UI, and other paths remaining locked.

- [ ] **Step 5: Update context, commit, and push**

```powershell
git diff --check
git add Docs/Verification/2026-08-07-weapon-evolution-onboarding.md Docs/AI/UnityProjectContext.md
git commit -m "docs: verify weapon evolution onboarding"
git push origin master
```

- [ ] **Step 6: Confirm repository state**

```powershell
git status --short
git rev-parse HEAD
git rev-parse origin/master
```

Expected: revisions match; only pre-existing unrelated `.meta` modifications remain.
