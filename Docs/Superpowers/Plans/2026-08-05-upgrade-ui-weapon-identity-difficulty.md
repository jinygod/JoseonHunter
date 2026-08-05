# Upgrade UI, Weapon Identity, and Stage Difficulty Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove redundant support confirmation, make weapon state readable, restore Frost Flask and Gakgung identities, and scale enemy durability through the full fifteen-minute first stage.

**Architecture:** Keep gameplay ownership in `FirstPlayableController` and weapon executors, while exposing structured affix data through `WeaponSlotView`. Add pure domain/runtime helpers for affix quality and enemy health so balance and presentation thresholds are deterministic and testable without scene objects. Reuse the existing appraisal presenter and Frost sprites instead of introducing a second modal or new art pipeline.

**Tech Stack:** Unity 6000.5.5f1, C# 9, uGUI, TextMeshPro, NUnit EditMode/PlayMode tests, URP 2D, Android ARM64 IL2CPP.

## Global Constraints

- Work on `master`; commit and push each completed task to `origin/master`.
- Preserve unrelated dirty PNG metadata and `ProjectSettings/ProjectSettings.asset` changes.
- Run Unity processes sequentially at BelowNormal priority with CPU affinity mask 15.
- Keep the run limit at four weapons and three supports.
- Add no package, scene, prefab, font, or new image asset.
- Keep gameplay target acquisition, active-enemy caps, spawn cadence, experience rewards, and pooling unchanged.
- Use Korean player-facing copy only.

---

### Task 1: Immediate Support Selection

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableUiStatePlayModeTests.cs`

**Interfaces:**
- Consumes: `ProgressionRewardEvent.Kind`, `UpgradeChoicePresenter.PresentationClosed`, `FirstPlayableController.NotifyUpgradePresentationClosed()`.
- Produces: support rewards with `waitingForRewardReveal == false` and no call to `RewardRevealPresenter.Play`; weapon and evolution routing remains unchanged.

- [ ] **Step 1: Write a failing PlayMode test for support bypass**

Add a test that selects a support through the production bootstrap and asserts that the choice closes, `RewardRevealPresenter.IsRevealing` remains false, gameplay returns to `Playing`, and the support level/stat is applied.

```csharp
[UnityTest]
public IEnumerator SupportSelectionAppliesAndReturnsWithoutRewardConfirmation()
{
    SceneManager.LoadScene("Gameplay");
    yield return null;
    yield return null;
    yield return null;
    var controller = Object.FindFirstObjectByType<FirstPlayableController>();
    var reward = Object.FindFirstObjectByType<RewardRevealPresenter>();
    var startingSpeed = controller.StartingMoveSpeedForTests;
    controller.OpenUpgradeOffersForTests(new UpgradeOffer("boots", UpgradeKind.Support, 1));
    Assert.That(controller.TryChooseUpgrade(0), Is.True);
    for (var frame = 0; frame < 60 && controller.Flow.State != GameFlowState.Playing; frame++)
        yield return null;
    Assert.That(reward.IsRevealing, Is.False);
    Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
    Assert.That(controller.StartingMoveSpeedForTests, Is.EqualTo(startingSpeed * 1.12f).Within(.001f));
}
```

- [ ] **Step 2: Run the focused PlayMode test and confirm it fails**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.RewardRevealPlayModeTests
```

Expected: the existing generic reward reveal is active and awaits confirmation.

- [ ] **Step 3: Route supports directly back to gameplay**

Change `OnUpgradeChosen` so presentation flags depend on reward kind:

```csharp
var requiresRewardPresentation = reward.Kind != ProgressionRewardKind.Support;
waitingForRewardReveal = requiresRewardPresentation;
waitingForChoiceClose = true;
pendingReward = reward;
hasPendingReward = requiresRewardPresentation;
upgradeChoice?.CloseAfterExternalSelection();
```

Keep `NotifyUpgradePresentationClosed` as the single place that calls `PlayPendingRewardAfterChoiceClose` and `NotifyUpgradeWhenPresentationComplete`. Do not call controller gameplay methods from the selection click itself.

- [ ] **Step 4: Run support and modal regression tests**

Run the reward reveal, UI state, modal flow, weapon replacement, and weapon legacy PlayMode fixtures. Expected: support bypass passes; weapon/evolution confirmation and modal ownership remain green.

- [ ] **Step 5: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/FirstPlayableUiStatePlayModeTests.cs
git commit -m "feat: apply support upgrades without confirmation"
git push origin master
```

### Task 2: Structured Affix Quality and Combat Slots

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponAffixQuality.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponAffixQuality.cs.meta`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixQualityTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`

**Interfaces:**
- Produces: `WeaponAffixQuality.Score(IReadOnlyList<WeaponAffixRoll>) -> float`, `WeaponAffixQuality.BandFor(float) -> WeaponAffixQualityBand`, and `WeaponSlotView.GeneralAffixRolls`.
- Consumes: authored affix ranges Damage 10-30, Cooldown 5-12 absolute, Area 8-20, ProjectileSpeed 10-30, Duration 10-25.

- [ ] **Step 1: Write failing quality boundary tests**

```csharp
[TestCase(0f, WeaponAffixQualityBand.Ash)]
[TestCase(.30f, WeaponAffixQualityBand.Green)]
[TestCase(.50f, WeaponAffixQualityBand.Blue)]
[TestCase(.70f, WeaponAffixQualityBand.Crimson)]
[TestCase(.90f, WeaponAffixQualityBand.Gold)]
public void BandUsesApprovedBoundaries(float score, WeaponAffixQualityBand expected) =>
    Assert.That(WeaponAffixQuality.BandFor(score), Is.EqualTo(expected));

[Test]
public void ScoreAveragesNormalizedActualValues()
{
    var rolls = new[] {
        new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.Standard, 10d),
        new WeaponAffixRoll(WeaponAffixStat.Area, WeaponAffixTier.Perfect, 20d)
    };
    Assert.That(WeaponAffixQuality.Score(rolls), Is.EqualTo(.5f).Within(.0001f));
}
```

- [ ] **Step 2: Run the quality fixture and confirm missing-type failure**

Run the EditMode fixture filtered to `WeaponAffixQualityTests`. Expected: compile failure because the helper and band enum do not exist.

- [ ] **Step 3: Implement the pure quality helper**

```csharp
public enum WeaponAffixQualityBand { Ash, Green, Blue, Crimson, Gold }

public static class WeaponAffixQuality
{
    public static float Score(IReadOnlyList<WeaponAffixRoll> rolls)
    {
        if (rolls == null || rolls.Count == 0) return 0f;
        var total = 0d;
        for (var index = 0; index < rolls.Count; index++)
        {
            var roll = rolls[index];
            var (minimum, maximum) = roll.Stat switch
            {
                WeaponAffixStat.Damage => (10d, 30d),
                WeaponAffixStat.Cooldown => (5d, 12d),
                WeaponAffixStat.Area => (8d, 20d),
                WeaponAffixStat.ProjectileSpeed => (10d, 30d),
                WeaponAffixStat.Duration => (10d, 25d),
                _ => throw new ArgumentOutOfRangeException()
            };
            var unit = (Math.Abs(roll.Value) - minimum) / (maximum - minimum);
            total += Math.Max(0d, Math.Min(1d, unit));
        }
        return (float)(total / rolls.Count);
    }

    public static WeaponAffixQualityBand BandFor(float score) => Math.Max(0f, Math.Min(1f, score)) switch
    {
        >= .90f => WeaponAffixQualityBand.Gold,
        >= .70f => WeaponAffixQualityBand.Crimson,
        >= .50f => WeaponAffixQualityBand.Blue,
        >= .30f => WeaponAffixQualityBand.Green,
        _ => WeaponAffixQualityBand.Ash
    };
}
```

Use `System.Math` rather than Unity presentation APIs inside the Domain assembly.

- [ ] **Step 4: Carry structured rolls through UI state**

Extend `WeaponSlotView` with an optional `IEnumerable<WeaponAffixRoll> generalAffixRolls` argument and immutable `GeneralAffixRolls`. In `BuildUiState`, pass `profile?.GeneralRolls` without parsing `GeneralAffixSummary`.

- [ ] **Step 5: Write failing rack tests for stars and quality**

Render a level-three weapon with two rolls and assert:

```csharp
Assert.That(TextNamed(slot.gameObject, "Level Stars"), Is.EqualTo("★★★"));
Assert.That(ImageNamed(slot.gameObject, "Quality Border").color, Is.EqualTo(WeaponRackPresenter.ColorFor(WeaponAffixQualityBand.Blue)));
```

Also assert levels one and five render one and five stars and potential cells remain capped at three.

- [ ] **Step 6: Build the readable 124-pixel slot**

Rename the border object to `Quality Border`, increase slot size to 124, add one centered TMP `Level Stars` label below the icon, move potential cells to a non-overlapping upper row, and color the border with the quality band. Use ash, green, blue, restrained crimson, and gold; never black.

- [ ] **Step 7: Run quality and rack fixtures**

Run `WeaponAffixQualityTests` in EditMode and `CombatHudPlayModeTests` in PlayMode. Expected: all new assertions and existing tap/pulse/detail tests pass.

- [ ] **Step 8: Commit and push**

Stage only the helper, UI-state, rack, and their tests. Commit `feat: show weapon level and affix quality in combat`, then push `master`.

### Task 3: Clear Appraisal and Read-Only Weapon Details

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalViewModel.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixPresentationTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponSlotView.GeneralAffixRolls`, `GeneralAffixSummary`, `LegacyName`, `LegacyStageName`, and `PotentialIds`.
- Produces: three explicit final rows `누적 추가옵션`, `성장 방식`, `잠재 능력`; read-only title `현재 적용 효과`; appraisal title `적용 후 누적 효과`.

- [ ] **Step 1: Replace legacy-row expectations with effect-summary expectations**

Update tests to assert the final rows contain:

```csharp
Assert.That(TextValue(RectNamed(presenter, "Affix Summary Row")), Does.Contain("누적 추가옵션"));
Assert.That(TextValue(RectNamed(presenter, "Growth Summary Row")), Does.Contain("성장 방식"));
Assert.That(TextValue(RectNamed(presenter, "Potential Summary Row")), Does.Contain("잠재 능력"));
Assert.That(TextValue(RectNamed(presenter, "Legacy Path")), Is.Empty.Or.Null);
```

For read-only details with two affixes, assert the large detail label contains only `추가옵션 2개` and the actual modifiers appear in the first summary row.

- [ ] **Step 2: Run appraisal fixtures and confirm old copy fails**

Run the EditMode presentation fixture and the two PlayMode fixtures. Expected: old `선택한 성장/현재 상태/다음 강화` strings fail.

- [ ] **Step 3: Carry rolls into the appraisal view model**

Add `IReadOnlyList<WeaponAffixRoll> GeneralAffixRolls` to `WeaponAppraisalViewModel`, copied from the slot in `From`. Keep `ForResult` compatible with an empty list.

- [ ] **Step 4: Bind concrete final rows**

Replace `BindLegacyRows` with:

```csharp
private void BindEffectRows(string affixSummary, string legacyName, string legacyStage,
    IReadOnlyList<WeaponPotentialId> potentials)
{
    effectRows[0].text = "누적 추가옵션\n" + (string.IsNullOrWhiteSpace(affixSummary) ? "없음" : affixSummary);
    effectRows[1].text = "성장 방식\n" + GrowthSummary(legacyName, legacyStage);
    effectRows[2].text = "잠재 능력\n" + PotentialSummary(potentials);
}
```

Use the presenter's existing Korean `PotentialName` mapping. Do not show raw potential IDs.

- [ ] **Step 5: Prevent overflow by layout contract**

Keep the newly rolled value in a single-line 600 x 62 label with wrapping disabled and a maximum font size of 38. Set the accumulated section title at Y 44. Give each summary row an 84-pixel text rect inside its 108-pixel panel, font size 20-22, and word wrapping enabled. In read-only mode show `추가옵션 N개` in the large detail label instead of concatenating every modifier there.

- [ ] **Step 6: Run appraisal and portrait layout fixtures**

Expected: appraisal confirmation still waits after the numeric/grade reveal, read-only details do not own game time, all Korean copy is visible, and long summaries do not overlap at supported portrait sizes.

- [ ] **Step 7: Commit and push**

Commit `feat: clarify weapon appraisal and details` with only the appraisal/view-model/tests, then push `master`.

### Task 4: Frost Flask Control Rebalance

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`

**Interfaces:**
- Produces: `TickInterval = .5f`, standard landing multiplier `.2f`, standard tick multiplier `.2f`, first tick at `.5f`, freeze duration `.3f`, and field presentation indices `Impact + 3`/`Impact + 4`.
- Preserves: legacy shatter-specific damage, overlap-safe status source cleanup, field cap, target contact masks, and pooled transient visuals.

- [ ] **Step 1: Write failing Frost behavior tests**

Create a full-residence standard level-five test that asserts landing plus four ticks totals 10 damage before affixes for base damage 10, the first tick does not occur at landing, slow remains while inside, and freeze duration is .3 seconds after .75-second residence.

Add a presentation assertion:

```csharp
Assert.That(frost.FirstVisualPartIndexForTests,
    Is.InRange(WeaponVisualPartIndex.FrostFlask.Impact + 3,
               WeaponVisualPartIndex.FrostFlask.Impact + 4));
```

- [ ] **Step 2: Run Frost mechanic/content tests and confirm old burst behavior fails**

Run filtered EditMode fixtures `WeaponMechanicTests` and `WeaponContentTests`.

- [ ] **Step 3: Implement standard damage cadence**

Set `TickInterval` to `.5f`, set `field.NextDamageAge = TickInterval` on landing, use `BaseDamage * .2f` for standard landing and ticks, and change standard freeze to `.3f`. Preserve Frost Shatter multipliers in their explicit legacy branches.

- [ ] **Step 4: Remove implicit level-five burst spikes**

Delete the standard `Level == 5` periodic `RaiseSpike` loop. Keep expiry spikes only when the corresponding potential is owned and keep evolved stored-target resolution.

- [ ] **Step 5: Rebind persistent presentation to restrained flakes**

In `UpdateFieldVisual`, alternate `Impact + 3` and `Impact + 4`, scale to the gameplay diameter, then multiply local Y scale by `.58f`. Use pale cyan at alpha `.55-.65`. `PlayLandingFragments` uses `Impact + 3` once at a small scale; it must not use `frost_growth` frames.

- [ ] **Step 6: Run Frost and nearby status/reaction fixtures**

Expected: Frost tests, status source overlap, reset cleanup, evolution, and reaction tests pass.

- [ ] **Step 7: Commit and push**

Commit `balance: make frost flask a control field`, then push `master`.

### Task 5: Gakgung Sniper Rebalance

**Files:**
- Modify: `Assets/JoseonHunter/Content/Weapons/GakgungShot.asset`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`

**Interfaces:**
- Produces: damage `28/40/54/72/96`, cooldown `1.35/1.30/1.25/1.20/1.15`, level-five side-arrow damage multiplier `.5f`.
- Preserves: `IsTargetVisible`, boss/elite/threat/distance/runtime-ID target priority, range, speed, pierce, legacy, and potential behavior.

- [ ] **Step 1: Update tests to the approved authored values**

Assert exact arrays in `WeaponContentTests` and add a level-five launch test that observes primary damage 96 and each side arrow damage 48 before modifiers.

- [ ] **Step 2: Run Gakgung tests and confirm current values fail**

Run the two filtered EditMode fixtures.

- [ ] **Step 3: Update content and side-arrow calculation**

Change the five serialized content levels. In `GakgungExecutor.Launch`, replace both side arrow damage arguments with `Mathf.CeilToInt(BaseDamage * .5f)`.

- [ ] **Step 4: Run Gakgung targeting and mechanic regressions**

Expected: off-screen targets remain ignored, boss/elite priority is stable, level-five launch count remains three, and damage/cooldown values match the approved table.

- [ ] **Step 5: Commit and push**

Commit `balance: reinforce gakgung sniper identity`, then push `master`.

### Task 6: Fifteen-Minute Enemy Durability

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyHealthCurve.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyHealthCurve.cs.meta`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/EnemyHealthCurveTests.cs`

**Interfaces:**
- Produces: `EnemyHealthCurve.BaseHealthAt(float elapsedSeconds) -> float` with milestones `(0,18)`, `(180,42)`, `(360,68)`, `(600,105)`, `(900,155)`.
- Consumes: result before existing `EnemyRankProfile.HealthMultiplier` and `EnemyArchetypeProfile.HealthMultiplier`.

- [ ] **Step 1: Write failing milestone and monotonic tests**

```csharp
[TestCase(0f, 18f)]
[TestCase(180f, 42f)]
[TestCase(360f, 68f)]
[TestCase(600f, 105f)]
[TestCase(900f, 155f)]
public void CurveMatchesApprovedMilestones(float time, float expected) =>
    Assert.That(EnemyHealthCurve.BaseHealthAt(time), Is.EqualTo(expected).Within(.001f));
```

Also sample every second from 0 to 900 and assert non-decreasing values; clamp negative time to 18 and values after 900 to 155.

- [ ] **Step 2: Run the fixture and confirm missing helper failure**

Run filtered EditMode `EnemyHealthCurveTests`.

- [ ] **Step 3: Implement piecewise interpolation**

```csharp
public static float BaseHealthAt(float elapsedSeconds)
{
    var time = Mathf.Clamp(elapsedSeconds, 0f, 900f);
    if (time <= 180f) return Mathf.Lerp(18f, 42f, time / 180f);
    if (time <= 360f) return Mathf.Lerp(42f, 68f, (time - 180f) / 180f);
    if (time <= 600f) return Mathf.Lerp(68f, 105f, (time - 360f) / 240f);
    return Mathf.Lerp(105f, 155f, (time - 600f) / 300f);
}
```

- [ ] **Step 4: Use the helper when spawning normal enemies**

Replace only `Mathf.Lerp(18f, 42f, elapsed / PrototypeDurationSeconds)` with `EnemyHealthCurve.BaseHealthAt(elapsed)`. Do not change boss/midboss health or rank/archetype multipliers.

- [ ] **Step 5: Run health, archetype, rank, wave, and stage pacing tests**

Expected: new curve and all existing spawn-role contracts pass.

- [ ] **Step 6: Commit and push**

Commit `balance: scale enemy durability through stage one`, then push `master`.

### Task 7: Integrated Verification and Evidence

**Files:**
- Create: `Docs/Verification/2026-08-05-upgrade-ui-weapon-identity-difficulty.md`
- Modify only if required by verified failures: task-owned files above.

**Interfaces:**
- Consumes: all completed task contracts.
- Produces: full test counts, Android build evidence, visual-capture notes, final commit and pushed `master`.

- [ ] **Step 1: Run full EditMode sequentially**

Run the canonical Unity test script with the EditMode filter. Expected: zero failures and no new compile errors.

- [ ] **Step 2: Run full PlayMode sequentially**

Run the canonical Unity test script with the PlayMode filter. Expected: zero failures and no modal, appraisal, HUD, Frost, or stage regressions.

- [ ] **Step 3: Capture representative portrait states**

Use the existing batch capture method without `-nographics`. Inspect at least one combat rack, one appraisal, one read-only detail, and one active Frost field at 1080 x 1920 or taller. Record whether stars, quality frame, Korean rows, and Frost flakes remain readable.

- [ ] **Step 4: Build Android ARM64 IL2CPP development APK**

Run `Tools/Unity/Build-AndroidDevelopment.ps1`. Expected: successful APK at `Builds/Android/JoseonHunter-development.apk` with no new first-party errors.

- [ ] **Step 5: Write verification evidence**

Record commit range, test totals, build artifact size/path, visual inspection, preserved dirty files, and remaining physical-device balance risks.

- [ ] **Step 6: Commit and push**

Commit `docs: verify upgrade clarity and combat identity`, push `master`, verify `git rev-parse HEAD` equals `git rev-parse origin/master`, and confirm only pre-existing unrelated changes remain dirty.
