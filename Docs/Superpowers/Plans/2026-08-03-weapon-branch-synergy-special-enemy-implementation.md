# Weapon Branch, Synergy, and Special Enemy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add four-slot weapon commitment, deterministic two-path weapon legacies, four readable status reactions, and four soft-counter special enemies while preserving approachable base clears and mobile performance.

**Architecture:** Keep all choice and balance rules in pure Domain classes, let `FirstPlayableController` coordinate modal flow and run-owned state, and pass an immutable legacy snapshot into the existing weapon executors. Extend the existing run-owned `WeaponAffixStatusService` instead of creating a second status system, and keep special-enemy behavior in focused pure profiles/motion helpers consumed by the prototype controller. Presentation remains event-driven and cannot decide combat outcomes.

**Tech Stack:** Unity 6.0, C#/.NET, Unity Test Framework with NUnit, TextMeshPro, existing 2D URP runtime UI, PixelLab MCP for pixel assets, Git/GitHub `master`.

## Global Constraints

- Weapon slots are exactly 4; support slots are exactly 3.
- A discarded weapon is locked for the remainder of the run and loses its level, legacy path, and affixes.
- Replacement weapon level is `discarded level - 1`, clamped to 1 through 3; a level-3 replacement opens legacy selection immediately.
- Each weapon chooses exactly one of two paths at level 3, reinforces it at level 4, and completes it at level 5.
- Random general-affix count-up remains; random potential acquisition is removed from new run rewards and existing potential effects are absorbed into legacy paths.
- One hit triggers at most one reaction; per-target reaction cooldown is 0.6 seconds.
- Reaction chain/transfer caps are: Ice Shatter 5, Fire Wind 4, Formation Break 1 bonus hit, Overload 3.
- Special enemies never use immunity; resistance is capped at 35%; visible special enemies never exceed 25% of living normal enemies.
- PixelLab assets use at most 3 non-transparent colors, no white outline, no antialiasing, at most 2 value steps, and Point filtering.
- Do not change run duration, add meta progression, or redraw the eight base weapon icons in this feature.
- Preserve and do not stage user-owned changes in `Gameplay.unity`, `ProjectSettings.asset`, font assets, render-pipeline assets, or unrelated imported metadata.
- Commit and push each independently verified task directly to `master`.

---

## File Structure

- `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyTypes.cs`: stable path IDs, stages, immutable definitions, and snapshots.
- `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyCatalog.cs`: all 16 approved path definitions and Korean choice copy.
- `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyState.cs`: run-owned one-path-per-weapon selection state.
- `Assets/JoseonHunter/Scripts/Domain/Progression/RunLoadoutRules.cs`: slot, discard-lock, and replacement-level rules.
- `Assets/JoseonHunter/Scripts/Domain/Progression/ProgressionTypes.cs`: replacement marker and discarded IDs in upgrade snapshots.
- `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`: four-slot offers, three-support limit, discard exclusion, and removal of separate evolution offers.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`: pending replacement/legacy flow, state reset, executor wiring, and special-enemy integration.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`: legacy/replacement view models and branch state on weapon rack entries.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponReplacementPresenter.cs`: four owned-weapon replacement choices plus cancel.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponLegacyChoicePresenter.cs`: dedicated two-card level-3 path choice.
- `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`: controller-event to modal-presenter wiring.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalViewModel.cs`: `전승 경로`, `현재 경지`, and `다음 경지` appraisal copy.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`: deterministic legacy rows instead of random potential slots.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponRuntimeModifiers.cs`: immutable general-affix plus legacy snapshot.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponAffixStatusService.cs`: timed status flags, reaction priority/cooldown, capped propagation, and cleanup.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs`: hit traits and reaction dispatch after confirmed damage.
- Eight existing executor files under `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/`: path-specific combat behavior.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs`: four special-enemy profiles and resistance rules.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/SpecialEnemyMotion.cs`: shaman aura, bull telegraph/dash, and split-rat state transitions.
- `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`: phased special introductions and combination rules.
- `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs`: deterministic special selection under the 25% cap.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySpriteRoster.cs`: new content-ID sprite resolution.
- `Assets/JoseonHunter/Scripts/Content/CombatChoiceVisualCatalog.cs`: branch, reaction, and special-enemy sprite lookup.
- `Assets/JoseonHunter/Scripts/Editor/AssetProduction/CombatChoicePixelAssetContract.cs`: palette/import/readability validation.
- `Assets/JoseonHunter/Art/CombatChoices/`: PixelLab outputs grouped by `Branches`, `Reactions`, and `SpecialEnemies`.

---

## Exact First-Pass Combat Constants

All percentage damage is based on the weapon level's authored `BaseDamage`, rounded up after legacy and general-affix multipliers. The legacy structural multiplier is applied before general-affix totals. Nearest-target selection is deterministic by squared distance, then runtime ID. No completed effect can exceed the stated target/projectile cap.

| Path or reaction | Exact first-pass constant not already explicit in the design spec |
|---|---|
| 독니 | Poison ticks every 0.5 seconds for 4 seconds at 20% base damage; `혈독난무` targets at most 6 poisoned enemies with six 40% slashes followed by one 160% poison blast. |
| 월식 | Completed intersection blast radius is 1.25× the base contact radius. |
| 관일 | Last-penetration explosion is 180% base damage at 1.0× base contact radius. |
| 갈래깃 | Completed fourth volley fires 7 arrows at 55% base damage. |
| 천쇄봉인 | Completed sealed-death blast is 160% base damage and can trigger from at most 4 sealed enemies per attack cycle. |
| 원귀폭발 | Reinforced second blast is 100% base damage; completed chained blasts are 120% each and cap at 3. |
| 뇌옥 | Completed 300% blast uses the inner 45% of authored Thunder radius. |
| 지맥 | Ground current ticks every 0.5 seconds at 30% base damage; completed propagation copies remaining duration to at most 5 targets. |
| 사방수호 | Each of the three completed outward pulses deals 80% base damage and uses one shared pooled visual per cardinal anchor. |
| 수호신강림 | Reinforced second slam deals the same 180% as the first; completed 320% slam replaces the second slam rather than adding a third. |
| 화룡포 | Completed five salvos each deal 32% base damage, totaling 160% before target-specific modifiers. |
| 화망 | Burn trail ticks every 0.5 seconds at 30% base damage for 3 seconds; completed connected-trail detonation deals 200% base damage once per target. |
| 빙무 | Each completed frost bloom deals 60% base damage; three confirmed hits within 2 seconds cause freeze. |
| 파쇄 | Reinforced/completed chains use the approved 180% shatter damage for each target and radiate from the consumed frozen target. |
| 진공 | Each hit adds one bleed stack, capped at 3; a stack ticks every 0.5 seconds at 15% base damage for 2 seconds; reinforced rupture consumes 3 stacks for 100% base damage. |
| 천뢰 | Completed 7 bounces deal 70% each; the final marked-center explosion deals 200% base damage. |
| 빙쇄 | Consume freeze; deal 180% triggering base damage in a 1.4-unit disk, affecting at most 5 targets. |
| 화풍 | Consume 50% of the source poison/burn remaining duration and copy that duration to at most 4 nearest targets; no extra instant damage. |
| 파진 | Consume seal or armor break and deal one 150% triggering base-damage reaction hit to the same target. |
| 과부하 | Consume shock; deal 80% triggering base damage to at most 3 targets and apply 0.2-second stagger. |

---

### Task 1: Pure loadout and legacy domain

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyTypes.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyCatalog.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponLegacyState.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/RunLoadoutRules.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/ProgressionTypes.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponAffixTypes.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponAffixRoller.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponLegacyCatalogTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/RunLoadoutRulesTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixRollerTests.cs`

**Interfaces:**
- Produces: `WeaponLegacyPathId(string value)` with static IDs `HwandoVenom`, `HwandoMoonEclipse`, `GakgungSunPiercer`, `GakgungSplitFletching`, `TalismanHeavenSeal`, `TalismanGhostBurst`, `ThunderPrison`, `ThunderEarthCurrent`, `JangseungFourGuardians`, `JangseungGuardianDescent`, `SingijeonFireDragon`, `SingijeonFireNet`, `FrostMist`, `FrostShatter`, `FanVacuum`, and `FanHeavenThunder`.
- Produces: `WeaponLegacyStage { None, Chosen, Reinforced, Completed }`.
- Produces: `WeaponLegacyCatalog.PathsFor(WeaponId weaponId) : IReadOnlyList<WeaponLegacyDefinition>`.
- Produces: `WeaponLegacyCatalog.TryGet(WeaponLegacyPathId id, out WeaponLegacyDefinition definition) : bool`.
- Produces: `WeaponLegacyState.TryChoose(WeaponId weaponId, WeaponLegacyPathId pathId) : bool`.
- Produces: `WeaponLegacyState.SnapshotFor(WeaponId weaponId, int weaponLevel) : WeaponLegacySnapshot`.
- Produces: `WeaponLegacyState.Remove(WeaponId weaponId) : bool` and `Clear()`.
- Produces: `RunLoadoutRules.WeaponSlotLimit == 4`, `SupportSlotLimit == 3`, and `ReplacementLevel(int discardedLevel) : int`.
- Extends: `UpgradeOffer(..., bool requiresReplacement = false)` and `UpgradeOffer.RequiresReplacement`.
- Extends: `UpgradeState` with five-argument overloads ending in `IUpgradeIdSet discardedWeaponIds` and `ISet<string> discardedWeaponIds`; existing overloads delegate with an empty set.
- Produces: `WeaponRunAffixState.Remove(WeaponId id) : bool`.

- [ ] **Step 1: Write failing catalog, selection, replacement, and affix tests**

Add exact contracts:

```csharp
[Test]
public void EveryLaunchWeaponHasTwoDistinctLegacyPaths()
{
    foreach (var weaponId in WeaponRoster.All)
    {
        var paths = WeaponLegacyCatalog.PathsFor(weaponId);
        Assert.That(paths.Count, Is.EqualTo(2), weaponId.Value);
        Assert.That(paths[0].Id, Is.Not.EqualTo(paths[1].Id));
        Assert.That(paths.All(path => path.WeaponId.Equals(weaponId)), Is.True);
    }
}

[TestCase(1, 1)]
[TestCase(2, 1)]
[TestCase(3, 2)]
[TestCase(4, 3)]
[TestCase(5, 3)]
public void ReplacementLevelIsClamped(int discardedLevel, int expected) =>
    Assert.That(RunLoadoutRules.ReplacementLevel(discardedLevel), Is.EqualTo(expected));

[Test]
public void FullLoadoutMarksNewWeaponOfferForReplacementAndNeverOffersDiscardedWeapon()
{
    var state = new UpgradeState(
        new Dictionary<string, int>
        {
            [WeaponId.HwandoFlyingBlade.Value] = 2,
            [WeaponId.GakgungShot.Value] = 2,
            [WeaponId.TalismanThrow.Value] = 2,
            [WeaponId.ThunderCrashBomb.Value] = 2
        },
        new Dictionary<string, int>(),
        new HashSet<string>(WeaponRoster.All.Select(id => id.Value)),
        new HashSet<string>(),
        new HashSet<string> { WeaponId.FrostFlask.Value });
    var offers = UpgradeSelector.Select(state, 27);
    Assert.That(offers.Where(o => o.Kind == UpgradeKind.Weapon && o.NextLevel == 1)
        .All(o => o.RequiresReplacement), Is.True);
    Assert.That(offers.Any(o => o.Id == WeaponId.FrostFlask.Value), Is.False);
    Assert.That(offers.Any(o => o.Kind == UpgradeKind.Evolution), Is.False);
}
```

Add a second selector assertion with all three support IDs owned and prove no fourth support offer is produced. Update affix tests so `RollAndApply` always returns an empty `NewPotentials` list while still adding exactly one general roll.

- [ ] **Step 2: Run focused EditMode tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.WeaponLegacyCatalogTests|JoseonHunter.Tests.EditMode.RunLoadoutRulesTests|JoseonHunter.Tests.EditMode.UpgradeEvolutionTests|JoseonHunter.Tests.EditMode.WeaponAffixRollerTests'
```

Expected: compilation fails for missing legacy/loadout types; after types compile, selector and potential-removal assertions fail.

- [ ] **Step 3: Implement immutable definitions and run state**

Use stage derivation that cannot drift from weapon level:

```csharp
public WeaponLegacySnapshot SnapshotFor(WeaponId weaponId, int weaponLevel)
{
    if (!selectedPaths.TryGetValue(weaponId, out var pathId)) return default;
    var stage = weaponLevel >= 5 ? WeaponLegacyStage.Completed :
        weaponLevel >= 4 ? WeaponLegacyStage.Reinforced : WeaponLegacyStage.Chosen;
    return new WeaponLegacySnapshot(pathId, stage);
}
```

Populate all 16 catalog entries with the approved Korean name, combat-style line, benefit line, cost line, level-4 line, completion name, and exact numeric constants from the design spec. Reject a path whose catalog weapon does not match `weaponId`, and reject a second path choice for the same weapon.

Change `UpgradeSelector` to:

```csharp
var ownedWeaponCount = state.WeaponLevels.Count(pair => pair.Value > 0);
var ownedSupportCount = state.SupportLevels.Count(pair => pair.Value > 0);
// New weapons remain eligible at four slots but carry RequiresReplacement.
// Discarded weapons and separate evolution offers never enter eligible.
```

Keep owned-weapon upgrades eligible through level 5. Stop `WeaponAffixRoller` from calling `AddPotential`; return `Array.Empty<WeaponPotentialId>()` while preserving the existing general-stat distribution and reveal pacing.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: all new and existing upgrade/affix tests pass.

- [ ] **Step 5: Commit and push**

Stage only Task 1 files, commit `feat: add weapon legacy and loadout rules`, and push `master`.

### Task 2: Controller modal flow and replacement state

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameFlowCoordinator.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/GameFlowStateTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyFlowPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponReplacementFlowPlayModeTests.cs`

**Interfaces:**
- Adds: `GameFlowState.WeaponReplacement` and `GameFlowState.WeaponLegacySelection`.
- Produces: `WeaponReplacementState(NewWeaponId, NewWeaponName, IReadOnlyList<WeaponReplacementChoiceView>)`.
- Produces: `WeaponLegacyChoiceState(WeaponId, WeaponName, IReadOnlyList<WeaponLegacyChoiceView>)`.
- Emits: `FirstPlayableController.WeaponReplacementOpened` and `WeaponLegacyOpened`.
- Produces: `TryChooseWeaponReplacement(string discardedWeaponId) : bool`.
- Produces: `CancelWeaponReplacement() : bool`.
- Produces: `TryChooseWeaponLegacy(WeaponLegacyPathId pathId) : bool`.
- Exposes for tests: `LegacySnapshotForTests(WeaponId)` and `IsWeaponDiscardedForTests(WeaponId)`.

- [ ] **Step 1: Write failing PlayMode state-machine tests**

Cover these exact sequences:

```csharp
[UnityTest]
public IEnumerator FullLoadoutOpensReplacementThenLevelThreeLegacyChoice()
{
    controller.SetFourWeaponLoadoutForTests(discardedWeaponLevel: 4);
    controller.OpenUpgradeOffersForTests(new UpgradeOffer(
        WeaponId.FrostFlask.Value, UpgradeKind.Weapon, 1, requiresReplacement: true));
    Assert.That(controller.TryChooseUpgrade(0), Is.True);
    Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.WeaponReplacement));

    Assert.That(controller.TryChooseWeaponReplacement(WeaponId.HwandoFlyingBlade.Value), Is.True);
    Assert.That(controller.WeaponLevelForTests(WeaponId.FrostFlask), Is.EqualTo(3));
    Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.WeaponLegacySelection));

    Assert.That(controller.TryChooseWeaponLegacy(WeaponLegacyPathId.FrostMist), Is.True);
    Assert.That(controller.LegacySnapshotForTests(WeaponId.FrostFlask).Stage,
        Is.EqualTo(WeaponLegacyStage.Chosen));
    Assert.That(controller.IsWeaponDiscardedForTests(WeaponId.HwandoFlyingBlade), Is.True);
    yield return null;
}
```

Also prove cancel returns to the unchanged three-card selection, invalid/non-owned replacement IDs do nothing, discarded affix state is removed, reset clears discarded/legacy state, and ordinary level-3 upgrades cannot enter result presentation before a path is chosen.

- [ ] **Step 2: Run focused PlayMode tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.UpgradeChoicePlayModeTests|JoseonHunter.Tests.PlayMode.WeaponLegacyFlowPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponReplacementFlowPlayModeTests'
```

Expected: missing events/states/methods fail compilation.

- [ ] **Step 3: Implement one pending-choice coordinator in the controller**

Add one private pending record rather than parallel booleans:

```csharp
private sealed class PendingWeaponChoice
{
    public UpgradeOffer Offer;
    public string DiscardedWeaponId;
    public int ResolvedLevel;
}
```

Flow rules:

1. `TryChooseUpgrade` sees `RequiresReplacement`, stores the offer, enters `WeaponReplacement`, and publishes four owned choices without applying a reward.
2. `CancelWeaponReplacement` clears pending replacement data, returns to `LevelUpSelection`, and republishes the same three upgrade cards.
3. `TryChooseWeaponReplacement` validates ownership, calculates the replacement level, removes the old weapon level/legacy/affixes/evolution compatibility state, adds the discarded ID, and continues.
4. Any weapon resolving to level 3 without a path enters `WeaponLegacySelection` and publishes exactly two catalog paths.
5. `TryChooseWeaponLegacy` validates that the path belongs to the pending weapon, records it once, then performs the weapon upgrade, general affix roll, executor rebuild, and result presentation.
6. Level 4 and 5 derive reinforced/completed stage automatically and never open another branch modal.

Separate evolution offers are unreachable in normal selection after Task 1. Keep the old evolution state and test hook only for regression compatibility; normal legacy runs leave it false, and completed behavior comes exclusively from `WeaponLegacySnapshot.Stage == Completed`.

Do not increment `AppliedUpgradeCount` until an actual reward is applied. Ensure cancel/disable/reset clears the pending record and returns game flow to a legal state.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2 plus:

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.GameFlowStateTests'
```

Expected: all modal transition and reset tests pass.

- [ ] **Step 5: Commit and push**

Stage only Task 2 files, commit `feat: add legacy and weapon replacement flow`, and push `master`.

### Task 3: Dedicated Korean choice and appraisal presentation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponReplacementPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponLegacyChoicePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalViewModel.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyPresentationPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Produces presenters with `Build()`, `Open(state, choose)`, `CloseImmediately()`, `IsOpen`, and `PresentationClosed` matching existing modal conventions.
- Extends `WeaponSlotView` with `LegacyName`, `LegacyStageName`, and `NextLegacyMilestone`.
- Extends `WeaponAppraisalViewModel` with the same three strings.

- [ ] **Step 1: Write failing layout, copy, and binding tests**

Assert the legacy modal has exactly two visible cards and each card renders non-empty `전투 방식`, `강점`, and `약점` lines. Assert replacement renders four owned weapons, their current levels/path names, and one `교체하지 않기` button. Assert all rects remain inside the 1080×1920 safe modal area and no card text overlaps its button.

Replace former potential-slot expectations with:

```csharp
Assert.That(TextNamed(root, "Legacy Path").text, Is.EqualTo("전승 경로 · 빙무"));
Assert.That(TextNamed(root, "Legacy Stage").text, Is.EqualTo("현재 경지 · 선택"));
Assert.That(TextNamed(root, "Legacy Next").text, Does.StartWith("다음 경지 ·"));
```

Scan all new visible strings with `Regex.IsMatch(text, "[A-Za-z]") == false` except internal object names hidden from players.

- [ ] **Step 2: Run presentation tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.WeaponLegacyPresentationPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests|JoseonHunter.Tests.PlayMode.PortraitUiLayoutPlayModeTests'
```

Expected: missing presenters/labels fail compilation or lookup.

- [ ] **Step 3: Build two simple text-first modals and rebind appraisal**

Reuse `RuntimeUiFactory`, Maru Buri body roles, the existing hanji/ink palette, and the existing card frame. Do not add a scroll illustration. Legacy cards show branch emblem if available and fall back to the base weapon icon; replacement cards show the base icon.

Modal headings and buttons are exactly:

```text
전승 경로를 선택하세요
버릴 무기를 선택하세요
이 무기를 버림
교체하지 않기
```

Change appraisal rows from three random potential slots to `전승 경로`, `현재 경지`, `다음 경지`. Preserve the general-affix count-up panel and `확인` button. The weapon rack adds only the path name below level so it remains readable at the far camera scale.

- [ ] **Step 4: Run presentation tests and verify GREEN**

Run the command from Step 2. Expected: all layout, Korean copy, close/cancel, and appraisal tests pass.

- [ ] **Step 5: Commit and push**

Stage only Task 3 files, commit `feat: present weapon legacy choices in Korean`, and push `master`.

### Task 4: Legacy runtime snapshot and reaction engine

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponRuntimeModifiers.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponAffixStatusService.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/ICombatTarget.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/CombatTypes.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponLegacyRuntimeModifierTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StatusReactionServiceTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatDamageServiceTests.cs`

**Interfaces:**
- Adds `[Flags] WeaponHitTrait { None, Slash, Pierce, Explosion, Heavy, Wind, Pull, Barrier, Knockback, Reaction }`.
- Adds `CombatStatusKind { Poison, Burn, Seal, ArmorBreak, Shock, Freeze, Bleed }`.
- Extends `WeaponDamageRequest.Create` with optional `WeaponHitTrait traits = WeaponHitTrait.None` and `Float2? attackOrigin = null`.
- Produces `WeaponAffixStatusService.ApplyTimedStatus(int targetId, CombatStatusKind kind, float duration, int stacks, WeaponId source) : bool`.
- Produces `WeaponAffixStatusService.HasStatus(int targetId, CombatStatusKind kind) : bool`.
- Produces `WeaponAffixStatusService.TryResolveReaction(in WeaponDamageRequest hit, in ConfirmedDamageEvent confirmed) : StatusReactionResult`.
- Produces `StatusReactionKind { None, IceShatter, FireWind, FormationBreak, Overload }` and immutable `StatusReactionResult`.
- Adds `IControlStatusTarget.ApplyStagger(float durationSeconds)` for Overload and charge interruption.
- Adds `IIncomingDamageResistanceTarget.IncomingDamageMultiplier(Float2 attackOrigin, WeaponHitTrait traits) : float`; ordinary targets do not implement it and therefore use `1f`.
- Extends `WeaponRuntimeModifiers.From(WeaponRunAffixProfile profile, WeaponLegacySnapshot legacy)` and exposes `Legacy`.

- [ ] **Step 1: Write failing reaction priority, cap, cooldown, and cleanup tests**

Build registry targets around the existing combat test double. Cover:

```csharp
[Test]
public void OneHitConsumesOnlyHighestPriorityEligibleReaction()
{
    statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Freeze, 2f, 1, WeaponId.FrostFlask);
    statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Shock, 2f, 1, WeaponId.ThunderCrashBomb);
    var hit = Hit(target, WeaponHitTrait.Explosion | WeaponHitTrait.Pull, hitTime: 1f);
    var result = statuses.TryResolveReaction(hit, Confirmed(hit));
    Assert.That(result.Kind, Is.EqualTo(StatusReactionKind.IceShatter));
    Assert.That(statuses.HasStatus(target.RuntimeId, CombatStatusKind.Freeze), Is.False);
    Assert.That(statuses.HasStatus(target.RuntimeId, CombatStatusKind.Shock), Is.True);
}
```

Priority is Ice Shatter, Fire Wind, Formation Break, Overload. Prove a second hit at `1.59f` produces none and a hit at `1.60f` can react. Prove transfer/chain counts never exceed 5/4/1/3, unregister clears all statuses, invalid durations/stacks are rejected, and a reaction hit carrying `WeaponHitTrait.Reaction` cannot recursively react.

- [ ] **Step 2: Run focused EditMode tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.WeaponLegacyRuntimeModifierTests|JoseonHunter.Tests.EditMode.StatusReactionServiceTests|JoseonHunter.Tests.EditMode.CombatDamageServiceTests'
```

Expected: missing traits/status APIs fail compilation.

- [ ] **Step 3: Extend the existing status owner without per-frame allocations**

Store one fixed record per target:

```csharp
private sealed class TargetStatusState
{
    public readonly float[] Remaining = new float[7];
    public readonly byte[] Stacks = new byte[7];
    public float NextReactionTime;
}
```

Use reusable target-ID and nearby-target buffers. Tick records at the existing runtime tick, remove empty target records, and retain the existing periodic-damage list for actual poison/burn/bleed ticks. Reaction resolution consumes the required status, records `NextReactionTime = hit.HitTime + .6f`, selects nearest valid targets through `CombatTargetRegistry`, and submits capped `Reaction`-trait attacks through `CombatDamageService`.

`ApplyPeriodic` and `ApplyOrRefreshPeriodic` synchronize poison/burn/bleed remaining time and stacks into the target record. Armor break uses a 1.25 incoming multiplier, reinforced seal uses 1.15, and the higher active multiplier wins rather than multiplying both. `CombatDamageService` multiplies that status vulnerability by `IIncomingDamageResistanceTarget` when implemented, then resolves one final damage request.

Call reaction resolution only after the original hit is confirmed. Visual notification is an event:

```csharp
public event Action<StatusReactionEvent> ReactionTriggered;
```

The event includes kind, world position, and affected count, but subscribers cannot alter damage.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: modifier defaults remain neutral and all reaction contracts pass.

- [ ] **Step 5: Commit and push**

Stage only Task 4 files, commit `feat: add capped status reaction engine`, and push `master`.

### Task 5: Hwando and Gakgung legacy paths

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponPotentialVisuals.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatAPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/HwandoGakgungLegacyPlayModeTests.cs`

**Interfaces:**
- Consumes `WeaponRuntimeModifiers.Legacy` and the status APIs from Task 4.
- Emits correct `Slash` and `Pierce` traits on confirmed attacks.
- Preserves executor constructor signatures apart from receiving the extended modifier value.

- [ ] **Step 1: Write failing behavior tests for four paths**

Prove exact values from the design:

- `독니`: direct damage ×0.80, four-second poison, death transfer capped at 3, completed `혈독난무` only targets poisoned enemies.
- `월식`: cooldown ×1.20, returning afterimage deals 70%, reinforced path adds the crossing return, completion intersection deals 220%.
- `관일`: cadence ×1.25, pierce +3, +15% per penetration capped at +60%, reinforced armor break 25% for 2.5 seconds, completed boss bonus +30%.
- `갈래깃`: projectile damage ×0.75, 3 arrows at 70%; reinforced 5 arrows at 60%; every fourth completed volley uses the approved fan burst without exceeding 7 live arrows.

Also assert the opposite path effect never appears and default/no-path behavior matches baseline tests.

- [ ] **Step 2: Run focused PlayMode tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.HwandoGakgungLegacyPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponPotentialCombatAPlayModeTests'
```

Expected: new path tests fail while baseline behavior still passes.

- [ ] **Step 3: Replace potential gates with legacy gates**

Use `modifiers.Legacy.Is(pathId)` and `Stage >= Reinforced/Completed`. Retain existing pooled sprites and contact masks; repurpose returning-afterimage, venom, armor-break, and split-arrow code rather than duplicating it. Apply trait/status calls only after `TryApply` confirms contact. Do not instantiate new materials per attack.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: all four paths, no-path baseline, pooling, and contact tests pass.

- [ ] **Step 5: Commit and push**

Stage only Task 5 files, commit `feat: branch hwando and gakgung combat`, and push `master`.

### Task 6: Talisman and Thunder Bomb legacy paths

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatAPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/TalismanThunderLegacyPlayModeTests.cs`

**Interfaces:**
- Talisman and Thunder blast contacts emit `Explosion`; only Thunder prison pull contacts emit `Pull`.
- Applies `Seal` and `Shock` through `WeaponAffixStatusService`.

- [ ] **Step 1: Write failing tests for four paths**

Test:

- `천쇄봉인`: damage ×0.75, two-second seal, reinforced +15% incoming non-periodic damage, completed sealed-death chain.
- `원귀폭발`: no seal transfer, 0.6-second delay, 200% explosion, reinforced area +30% plus smaller second burst, completed maximum three chained bursts.
- `뇌옥`: cooldown ×1.25, one-second pull, reinforced center damage +60%, completed 300% compressed blast.
- `지맥`: initial blast ×0.70, three-second current at fixed 0.5-second ticks, reinforced four seconds and three targets, completed death propagation capped at five.

Assert ground-current queries use a capped reusable buffer and retired attacks return tracked attack count to baseline.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.TalismanThunderLegacyPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponPotentialCombatAPlayModeTests'
```

Expected: new delayed/center/current assertions fail.

- [ ] **Step 3: Implement path behavior by adapting existing potential mechanics**

Move seal transfer and vengeful burst behind the two approved path IDs. Keep one timer record per active talisman burst. Keep Thunder ground ticks at 0.5 seconds and reuse current area queries; center damage uses distance already calculated for the blast. Mark all reaction-eligible hits with exact traits.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: all path, baseline, cleanup, and cap assertions pass.

- [ ] **Step 5: Commit and push**

Stage only Task 6 files, commit `feat: branch talisman and thunder combat`, and push `master`.

### Task 7: Jangseung and Singijeon legacy paths

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungGuardianDescentPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungWardPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatBPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/JangseungSingijeonLegacyPlayModeTests.cs`

**Interfaces:**
- Emits `Barrier`, `Knockback`, `Heavy`, and `Explosion` traits.
- Applies `Burn` through the shared status service.
- Keeps all temporary visuals inside `WeaponTransientVisualPool`.

- [ ] **Step 1: Write failing tests for four paths and flat-color presentation**

Test:

- `사방수호`: damage ×0.70, four-direction ward, knockback, reinforced 20% contact-damage reduction while warded, completed three synchronized outward pulses.
- `수호신강림`: ward lifetime ×0.60, 180% slam, reinforced second slam, completed 320% center slam.
- `화룡포`: radius ×0.65, strongest-target priority, total focused damage +60%, completed five capped salvos.
- `화망`: initial explosion ×0.70, three-second burn trail, reinforced death ignition capped at 3, completed connected-trail simultaneous explosion.

Presentation tests require no white outline color, at most three active colors, whole guardian silhouettes rather than cropped thirds, and full visual-pool return after reset.

- [ ] **Step 2: Run focused PlayMode tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.JangseungSingijeonLegacyPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponPotentialCombatBPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponTransientVisualPoolPlayModeTests'
```

Expected: path and simplified-guardian assertions fail.

- [ ] **Step 3: Implement combat and simplify guardian presentation**

Reuse the existing four-direction flat ward anchors. Represent the completed twelve-spirit theme as three pulses from the same four cardinal anchors; never instantiate twelve large sprites. Guardian descent uses one complete centered silhouette with squash, shadow, and dust cues from existing pooled parts. Focused rockets reuse the target selector; trail connections store endpoint indices in a bounded list rather than spawning line objects per frame.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: combat, palette, silhouette, and pool cleanup tests pass.

- [ ] **Step 5: Commit and push**

Stage only Task 7 files, commit `feat: branch jangseung and singijeon combat`, and push `master`.

### Task 8: Frost Flask and Wind-Thunder Fan legacy paths

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatBPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/FrostFanLegacyPlayModeTests.cs`

**Interfaces:**
- Emits `Explosion`, `Wind`, and `Pull` traits.
- Applies `Freeze`, `Shock`, and `Bleed` through the shared status service.

- [ ] **Step 1: Write failing tests for four paths**

Test:

- `빙무`: direct damage ×0.65, area ×1.35, 45% slow, freeze on third confirmed hit, reinforced +10% incoming damage, completed three frost blooms.
- `파쇄`: field lifetime ×0.50, landing damage ×1.50, frozen target consumes freeze for 180% area damage, reinforced chain cap 3, completed cap 5.
- `진공`: lightning damage ×0.70, pull ×1.50, three bleed stacks, reinforced three-stack rupture, completed repeating vacuum without unbounded target searches.
- `천뢰`: no pull, 70% bounce damage capped at 4, reinforced returning 80% hit, completed seven bounces followed by one marked-center explosion.

Assert Ice Shatter consumes freeze once, Fan can trigger Fire Wind and Overload but only one reaction per hit, and reset retires every repeated attack.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.FrostFanLegacyPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponPotentialCombatBPlayModeTests'
```

Expected: branch and reaction integration assertions fail.

- [ ] **Step 3: Implement bounded field, shatter, vacuum, and bounce behavior**

Count Frost confirmed contacts in the status service instead of on visual objects. Use existing field timers and fan chain target selection. Completed blooms reuse one field visual three times; completed Fan uses one bounded target buffer and at most seven confirmed chain steps. Do not create one particle system per target.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: all path, reaction, cleanup, and baseline tests pass.

- [ ] **Step 5: Commit and push**

Stage only Task 8 files, commit `feat: branch frost and fan combat`, and push `master`.

### Task 9: Four soft-counter special enemies and phased waves

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/SpecialEnemyMotion.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySpriteRoster.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/SpecialEnemyRuleTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WaveRosterPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/SpecialEnemyCombatPlayModeTests.cs`

**Interfaces:**
- Produces `EnemyArchetype { Normal, ShieldDokkaebi, SpiritShaman, ChargingHornGhost, SplittingRat }`.
- Produces `EnemyArchetypeProfile.ForContentId(string) : EnemyArchetypeProfile`.
- Produces `EnemyArchetypeProfile.IncomingDamageMultiplier(Vector2 facing, Vector2 attackDirection, WeaponHitTrait traits) : float`.
- Implements `IIncomingDamageResistanceTarget.IncomingDamageMultiplier(Float2 attackOrigin, WeaponHitTrait traits)` on the prototype combat target by delegating to its enemy profile and current facing.
- Extends `WaveSpawnDirector.TrySelectSpecial(RunPhase phase, int livingNormalCount, int livingSpecialCount, out string contentId) : bool`.
- Produces pure `SpecialEnemyMotion.Tick(...) : SpecialEnemyMotionResult` with no Unity object allocation.

- [ ] **Step 1: Write failing schedule, resistance, motion, and cap tests**

Prove:

- Wave One contains no special enemy.
- Wave Two introduces only `shield_dokkaebi` or `spirit_shaman`, one family at a time.
- Wave Three can introduce `charging_horn_ghost` and `splitting_rat` but returns at most one special family per selection.
- Peak combines at most two special families.
- `TrySelectSpecial` is false when `livingSpecialCount >= floor(livingNormalCount * .25)`.
- Shield front direct damage multiplier is exactly `.65f`; back, area, pull, and reaction hits are `1f`.
- Bull telegraph lasts `.6f` before a bounded dash; freeze/knockback cancels or interrupts the dash.
- Shaman aura updates at `.25f` intervals and grants nearby enemies +20% movement/contact damage, never itself.
- Split rat creates at most two children; at active cap it returns a fallback small-blast request instead.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.WaveSpawnDirectorTests|JoseonHunter.Tests.EditMode.SpecialEnemyRuleTests'
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.WaveRosterPlayModeTests|JoseonHunter.Tests.PlayMode.SpecialEnemyCombatPlayModeTests'
```

Expected: missing archetype/special APIs fail compilation.

- [ ] **Step 3: Integrate profiles with the existing pooled enemy loop**

Add archetype, facing, motion state, aura multiplier, and split generation fields to `EnemyState`. Keep the existing separation grid. Update special behaviors in the movement loop with reusable buffers and timers. Extend the combat target with a resistance interface consumed by `CombatDamageService`; calculate front/back using the request attack origin and enemy facing.

Display each first-seen guide once per run:

```text
방패 도깨비 · 정면을 피해 공격하세요
원혼 무당 · 주변 적을 강화합니다
돌진 쇠뿔귀 · 붉은 예고선에서 벗어나세요
분열 쥐 · 범위 공격으로 한꺼번에 처리하세요
```

Use existing wave-announcement presentation and reset the seen set on run reset.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run both commands from Step 2. Expected: schedule, 25% cap, resistance, behavior interruption, split cap, Korean guide, and roster tests pass.

- [ ] **Step 5: Commit and push**

Stage only Task 9 files, commit `feat: add readable special enemy counters`, and push `master`.

### Task 10: PixelLab branch, reaction, and special-enemy assets

**Files:**
- Create: `Assets/JoseonHunter/Art/CombatChoices/Branches/*.png` and matching `.meta` files
- Create: `Assets/JoseonHunter/Art/CombatChoices/Reactions/*.png` and matching `.meta` files
- Create: `Assets/JoseonHunter/Art/CombatChoices/SpecialEnemies/**/*.png` and matching `.meta` files
- Create: `Assets/JoseonHunter/Scripts/Content/CombatChoiceVisualCatalog.cs`
- Create: `Assets/JoseonHunter/Content/CombatChoiceVisualCatalog.asset`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/CombatChoicePixelAssetContract.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/CombatChoicePixelAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponLegacyChoicePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySpriteRoster.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Produces `CombatChoiceVisualCatalog.LegacyIcon(WeaponLegacyPathId) : Sprite`.
- Produces `CombatChoiceVisualCatalog.ReactionIcon(StatusReactionKind) : Sprite`.
- Produces `CombatChoiceVisualCatalog.EnemyFrames(string contentId) : IReadOnlyList<Sprite>`.
- Produces editor validation `CombatChoicePixelAssetContract.Validate(string assetPath) : IReadOnlyList<string>`.

- [ ] **Step 1: Write failing asset-count, import, palette, and reference tests**

Require exactly 16 legacy icons, 4 reaction icons, 4 base special-enemy sprites, and 2–4 telegraph frames for each special enemy. For every PNG assert:

```csharp
Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
Assert.That(importer.mipmapEnabled, Is.False);
Assert.That(CombatChoicePixelAssetContract.NonTransparentColorCount(texture), Is.LessThanOrEqualTo(3));
Assert.That(CombatChoicePixelAssetContract.HasOpaqueWhiteOutline(texture), Is.False);
```

Catalog tests require every legacy/reaction/content ID to resolve without fallback.

- [ ] **Step 2: Run contract tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.CombatChoicePixelAssetContractTests'
```

Expected: asset and catalog files are missing.

- [ ] **Step 3: Generate the approved limited-palette assets with PixelLab**

Use PixelLab MCP, not Imagegen. Supply the current simplified rat, dokkaebi, and weapon icons as style references. Generation prompts must include this invariant text:

```text
Joseon folk-horror top-down pixel game, distant camera readability, bold simple silhouette,
maximum three opaque colors, two value steps, no white outline, no antialiasing,
no realistic texture, no tiny ornament, transparent background
```

Generate branch emblems as one 16-item 48×48 object batch with per-item descriptions matching the Korean paths. Generate each special enemy at the same apparent scale as existing normal enemies, then request 2–4 frame telegraph motion: shield brace, shaman aura pulse, horn-ghost crouch/dash cue, and split-rat swelling. Generate four single-symbol reaction icons. Inspect PixelLab review candidates and select only silhouettes that remain readable at the in-game 32–48 pixel display size; reroll candidates that violate the palette or outline contract.

- [ ] **Step 4: Import, bind, and verify assets**

Save approved PNGs in the listed folders, let Unity create metadata, apply the importer contract, populate `CombatChoiceVisualCatalog.asset`, and bind the catalog without modifying `Gameplay.unity`. Load through `Resources` or the existing controller catalog field fallback so scene serialization is unnecessary.

Run the command from Step 2 plus:

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.WeaponLegacyPresentationPlayModeTests|JoseonHunter.Tests.PlayMode.SpecialEnemyCombatPlayModeTests'
```

Expected: every asset resolves, palette/import contracts pass, and real sprites replace development silhouettes.

- [ ] **Step 5: Commit and push**

Stage only Task 10 assets, catalog/importer code, and tests; commit `art: add PixelLab legacy and special enemy sprites`; push `master`.

### Task 11: Integrated performance, Android, and verification

**Files:**
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatPerformanceInvestigationPlayModeTests.cs`
- Create: `Docs/Verification/2026-08-03-weapon-branch-synergy-special-enemy.md`

**Interfaces:**
- Consumes all prior task outputs and produces a reproducible verification record.

- [ ] **Step 1: Run full EditMode tests**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode'
```

Expected: zero failures. Record total/pass/fail counts from `Logs/editmode-results.xml`.

- [ ] **Step 2: Run all changed-surface PlayMode tests**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.UpgradeChoicePlayModeTests|JoseonHunter.Tests.PlayMode.WeaponLegacyFlowPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponReplacementFlowPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponLegacyPresentationPlayModeTests|JoseonHunter.Tests.PlayMode.HwandoGakgungLegacyPlayModeTests|JoseonHunter.Tests.PlayMode.TalismanThunderLegacyPlayModeTests|JoseonHunter.Tests.PlayMode.JangseungSingijeonLegacyPlayModeTests|JoseonHunter.Tests.PlayMode.FrostFanLegacyPlayModeTests|JoseonHunter.Tests.PlayMode.SpecialEnemyCombatPlayModeTests|JoseonHunter.Tests.PlayMode.WaveRosterPlayModeTests'
```

Expected: zero failures. Record counts from `Logs/playmode-results.xml`.

- [ ] **Step 3: Run the high-load combat regression**

Extend `CombatPerformanceInvestigationPlayModeTests` to create 140 enemies with the four special archetypes capped at 25%, then run completed 갈래깃, 지맥, 화망, and 분열 쥐 together for 30 simulated seconds. Assert:

```csharp
Assert.That(allocatedBytesAfterWarmup, Is.LessThanOrEqualTo(4096));
Assert.That(averageTickMilliseconds, Is.LessThanOrEqualTo(12d));
Assert.That(weaponRuntime.DamageService.TrackedAttackCount, Is.LessThan(256));
Assert.That(WeaponTransientVisualPool.ActiveCountForTests, Is.LessThanOrEqualTo(pool.Capacity));
Assert.That(livingSpecialCount, Is.LessThanOrEqualTo(Mathf.FloorToInt(livingNormalCount * .25f)));
```

Warm up for 40 ticks before taking `System.GC.GetAllocatedBytesForCurrentThread()` and stopwatch measurements. If this test fails, return to the task that owns the measured system, add the failing measurement to that task's focused test, fix it there, and rerun Tasks 5–9 focused suites before continuing.

- [ ] **Step 4: Build Android development APK**

```powershell
& .\Tools\Unity\Build-AndroidDevelopment.ps1
```

Expected: exit code 0 and a non-empty `Builds/Android/JoseonHunter-development.apk`.

- [ ] **Step 5: Record results, inspect diff, commit, and push**

Write exact commands, timestamps, test counts, APK size, PixelLab asset IDs, and any remaining manual-check notes in the verification document. Run:

```powershell
git diff --check
git status --short
git log --oneline origin/master..HEAD
```

Confirm user-owned dirty files remain unstaged. Stage only the verification document and any measured regression fix, commit `docs: verify weapon branch combat pass`, and push `master`.
