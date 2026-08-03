# Progression, Reward, and Pickup Clarity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Slow and diversify level-up decisions, reveal appraisal grade after value, restore readable support rewards, replace the geometric Thunder blast, and enlarge important pickups.

**Architecture:** Keep deterministic balance rules in Domain (`ExperienceCurve`, `UpgradeSelector`) and use `FirstPlayableController` only to consume those rules and schedule queued choices. Keep UI timing in the existing presentation timeline/presenters and change Thunder/pickup presentation without changing combat contacts, collision radii, or serialized assets.

**Tech Stack:** Unity 6000.5.5f1, C#/.NET, Unity Test Framework with NUnit, uGUI/TextMeshPro, URP 2D, existing sprite-frame combat presentation.

## Global Constraints

- Work directly on `master`; stage only task-owned files and push every commit to `origin/master`.
- Preserve all pre-existing dirty scenes, settings, fonts, `.meta` files, `.utmp/`, and `tmp/`.
- Run Unity at BelowNormal priority with processor affinity mask `63` to limit CPU contention.
- Do not generate PixelLab or Imagegen assets; the checked-in pickup and lightning-current frames are sufficient.
- Do not change pickup attraction radius `0.58`, collection distance `0.42`, weapon damage, Thunder hit masks, or exact contact order.
- Keep player-facing runtime text Korean.
- Follow test-first red-green-refactor for every production behavior change.

---

### Task 1: Scalable experience and queued-choice grace

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/ExperienceCurve.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatRuleTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/ModalGameFlowPlayModeTests.cs`

**Interfaces:**
- Produces: `ExperienceCurve.GetThresholdForNextLevel(int level) : int` for every positive level.
- Consumes: existing `GameFlowCoordinator` transitions `AugmentResult -> Playing -> LevelUpSelection`.

- [ ] **Step 1: Write failing curve tests**

Replace the old eight fixed cases with formula and scalability assertions:

```csharp
[TestCase(1, 15)]
[TestCase(2, 24)]
[TestCase(5, 63)]
[TestCase(10, 168)]
[TestCase(22, 624)]
public void ExperienceCurveUsesScalableThresholds(int level, int expected) =>
    Assert.That(ExperienceCurve.GetThresholdForNextLevel(level), Is.EqualTo(expected));

[Test]
public void ExperienceCurveRejectsNonPositiveLevels() =>
    Assert.Throws<System.ArgumentOutOfRangeException>(() =>
        ExperienceCurve.GetThresholdForNextLevel(0));
```

- [ ] **Step 2: Run the curve test and verify RED**

Run the CPU-limited Unity test command for `JoseonHunter.Tests.EditMode.CombatRuleTests.ExperienceCurveUsesScalableThresholds`.

Expected: old level-one value `5` differs from `15`, and level 22 throws instead of returning `624`.

- [ ] **Step 3: Implement the domain curve and controller wiring**

Use checked integer math and clamp overflow:

```csharp
public static int GetThresholdForNextLevel(int level)
{
    if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
    var threshold = 8L + 6L * level + (long)level * level;
    return threshold >= int.MaxValue ? int.MaxValue : (int)threshold;
}
```

Set reset and post-level thresholds with `ExperienceCurve.GetThresholdForNextLevel(level)`. Add `const float QueuedUpgradeGraceSeconds = 1f`, a `float upgradeQueueGraceRemaining`, and a start-of-gameplay tick that counts the grace down and opens exactly one queued choice when it reaches zero.

- [ ] **Step 4: Write the failing queued-choice PlayMode test**

After forcing one reward and adding enough experience for another level, confirm the reward and assert:

```csharp
Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
Assert.That(controller.IsUpgradeOpen, Is.False);
controller.TickGameplayIfRunningForTests(.5f);
Assert.That(controller.IsUpgradeOpen, Is.False);
controller.TickGameplayIfRunningForTests(.51f);
Assert.That(controller.IsUpgradeOpen, Is.True);
```

- [ ] **Step 5: Run the queued-choice test and verify RED**

Run `JoseonHunter.Tests.PlayMode.ModalGameFlowPlayModeTests`.

Expected: the current controller opens the queued choice immediately after reward confirmation.

- [ ] **Step 6: Implement grace scheduling and verify GREEN**

On reward close with pending levels, transition to `Playing`, set the grace to `1f`, and do not decrement the pending count until the next modal actually opens. Make `AddExperience` respect an active grace timer. Reset the timer to zero in `ResetRun` and modal cancellation.

Run the focused EditMode and PlayMode tests. Expected: all focused tests pass.

- [ ] **Step 7: Commit and push**

Stage the four listed files, commit `balance: pace experience and queued choices`, and push `origin master`.

---

### Task 2: Support-forward offer economy

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatRuleTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs`

**Interfaces:**
- Produces: `UpgradeSelector.Select(UpgradeState state, int seed, int playerLevel) : IReadOnlyList<UpgradeOffer>`.
- Preserves: `Select(UpgradeState state, int seed)` as a compatibility overload forwarding with `playerLevel: 1`.
- Consumes: player `level` from `FirstPlayableController.OpenUpgrade`.

- [ ] **Step 1: Replace weapon-guarantee tests with failing composition tests**

Add tests across seeds proving:

```csharp
var offers = UpgradeSelector.Select(state, seed, playerLevel: 5);
Assert.That(offers.Count(offer => offer.Kind == UpgradeKind.Support), Is.GreaterThanOrEqualTo(1));
Assert.That(offers.Count(offer => offer.Kind == UpgradeKind.Weapon), Is.LessThanOrEqualTo(1));
```

Add a level-eight pity test requiring exactly one weapon for every seed, and a non-pity distribution test across 100 seeds requiring both weapon-present and weapon-absent outcomes. Keep uniqueness, replacement, discard, max-level, and deterministic-seed assertions.

- [ ] **Step 2: Run selector tests and verify RED**

Run `JoseonHunter.Tests.EditMode.CombatRuleTests|JoseonHunter.Tests.EditMode.UpgradeEvolutionTests`.

Expected: current selection returns two weapon cards and has no player-level pity contract.

- [ ] **Step 3: Implement category-first deterministic selection**

Split eligible offers into supports, owned weapons, and new weapons. Shuffle each list with the seeded `Random`. Add up to two distinct supports first. Set `weaponDue = playerLevel % 4 == 0 || random.NextDouble() < .25`. When due, add one weapon, preferring owned on odd levels and new on even levels, with the other weapon pool as fallback. Fill remaining cards from unused eligible offers, preferring support and enforcing at most one weapon unless fewer than three non-weapon choices exist.

- [ ] **Step 4: Pass the player level from the controller and verify GREEN**

Change `OpenUpgrade` to call:

```csharp
var selected = UpgradeSelector.Select(state, level * 397 ^ kills, level);
```

Run the two focused test fixtures. Expected: all composition, replacement, uniqueness, and deterministic ordering tests pass.

- [ ] **Step 5: Commit and push**

Stage the four listed files, commit `balance: make weapon offers consequential`, and push `origin master`.

---

### Task 3: Value-first appraisal verdict

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponAppraisalPresentationTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Produces: `WeaponAffixRevealTimeline.TierRevealsAt : float`.
- Produces: `WeaponAffixRevealPresenter.IsTierVerdictVisible : bool` and existing `DisplayedAffixText` test surface.

- [ ] **Step 1: Write failing timeline and presenter tests**

Assert:

```csharp
Assert.That(timeline.TierRevealsAt, Is.GreaterThan(timeline.CountEndsAt));
Assert.That(timeline.TierRevealsAt, Is.LessThan(timeline.ReadStartsAt));
```

In PlayMode, start a standard reveal, assert title `추가옵션 감정 중` and hidden verdict during count-up, then advance past `TierRevealsAt` and assert title `일반`, visible seal, and final numeric detail.

- [ ] **Step 2: Run the appraisal tests and verify RED**

Run `WeaponAppraisalPresentationTests` and `WeaponAffixRevealPlayModeTests`.

Expected: the timeline has no verdict time and the title exposes `일반` from the first frame.

- [ ] **Step 3: Implement the verdict beat**

Add `TierRevealsAt = CountEndsAt + .10f`; move `ReadStartsAt` to `TierRevealsAt + .14f`; adjust potential stop times from that boundary. During earlier frames, keep the seal and final grade symbol hidden and set title to `추가옵션 감정 중`. At `TierRevealsAt`, show `TierName(activeResult.General.Tier)` and the rarity seal. Preserve read-only detail behavior.

- [ ] **Step 4: Update exact duration expectations and verify GREEN**

Update only duration assertions affected by the added verdict beat, then rerun the two focused fixtures. Expected: all tests pass and skip still lands in the confirmable state.

- [ ] **Step 5: Commit and push**

Stage the four listed files, commit `feat: reveal appraisal grade after value`, and push `origin master`.

---

### Task 4: Opaque and explicit support reward

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`

**Interfaces:**
- Produces: `RewardRevealPresenter.FinalRootAlphaForTests : float`, `PanelColorForTests : Color`, and `DetailTextForTests : string` under `UNITY_INCLUDE_TESTS`.
- Produces: `SupportRewardSummary(string id) : string` in the controller.

- [ ] **Step 1: Write failing presentation and Korean-copy tests**

Play a support reward and, after `.5f` realtime, assert final root alpha `1f`, opaque panel alpha `1f`, dark readable title/detail colors, dark button with light label, and the exact detail `경험치 획득 범위 +0.7`.

- [ ] **Step 2: Run the reward tests and verify RED**

Run `RewardRevealPlayModeTests|PortraitUiLayoutPlayModeTests`.

Expected: support final alpha is `.70`, the panel is dark/translucent, the button is white, and reward detail is raw `+0.7`.

- [ ] **Step 3: Implement the opaque hanji card**

Animate `CanvasGroup.alpha` from 0 to 1 independently of reward intensity. Use intensity only for a scale overshoot on the panel. Set panel color to opaque hanji, add four simple ink border rails, set title/detail to hanji ink, reduce glyph to `64`, and use `JoseonUiPalette.AppraisalResult` for the button with `DarkPanelText` label.

- [ ] **Step 4: Emit complete support summaries and verify GREEN**

Return `최대 체력 +20`, `이동 속도 +12%`, and `경험치 획득 범위 +0.7` from `SupportRewardSummary` and use it in `ProgressionRewardEvent`. Run the focused tests. Expected: all pass at portrait layouts.

- [ ] **Step 5: Commit and push**

Stage the four listed files, commit `fix: make support rewards explicit`, and push `origin master`.

---

### Task 5: Transparent Thunder blast and larger pickups

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePickupRangePlayModeTests.cs`

**Interfaces:**
- Consumes: existing `ThunderBombExecutor.FirstVisualPartIndexForTests : int` active-bomb test surface.
- Preserves: Thunder state, damage, radius, contact phase, mask, and repeat-hit behavior.
- Produces: test-visible pickup transforms through existing named runtime objects.

- [ ] **Step 1: Write failing Thunder presentation tests**

Advance a standard bomb to `Blast` and assert its visual part is within:

```csharp
Is.InRange(WeaponVisualPartIndex.ThunderCrash.Field,
    WeaponVisualPartIndex.ThunderCrash.Field +
    WeaponVisualPartIndex.ThunderCrash.FieldFrameCount - 1)
```

Retain the existing assertions for blast contacts and state order.

- [ ] **Step 2: Write failing pickup scale tests**

Spawn named experience and yeopjeon pickups, tick their animation, and assert experience scale is centered near `.48f`, yeopjeon near `.34f`, attraction begins only inside `.58f`, and collection still requires `.42f`.

- [ ] **Step 3: Run both fixtures and verify RED**

Run `WeaponMechanicTests|FirstPlayablePickupRangePlayModeTests`.

Expected: Blast uses the Detonation range and scales are centered on `.30` and `.18`.

- [ ] **Step 4: Switch Thunder presentation without changing combat**

For `Blast` and `CompressedBlast`, resolve the frame from `ThunderCrash.Field`. Change `PlayConfirmedBlast` to the first Field frame and a pale-blue tint. Leave `ResolveBlast`, `SweptRadius`, hit masks, damage requests, and timings untouched.

- [ ] **Step 5: Enlarge and pulse existing pickup objects**

Use `.48f` as the experience pulse base and `.34f` for yeopjeon. Apply a small sine pulse to yeopjeon in the existing pickup loop. Keep the magnet scale and all distance checks unchanged; create no child objects or new materials.

- [ ] **Step 6: Run focused tests and verify GREEN**

Rerun the two fixtures. Expected: presentation and scale assertions pass together with existing contact and pickup-range tests.

- [ ] **Step 7: Commit and push**

Stage the four listed files, commit `feat: clarify thunder and pickup feedback`, and push `origin master`.

---

### Task 6: Integrated verification and handoff

**Files:**
- Create: `Docs/Verification/2026-08-03-progression-reward-clarity.md`

**Interfaces:**
- Consumes: all behavior from Tasks 1–5.
- Produces: reproducible test/build evidence and remaining manual device checks.

- [ ] **Step 1: Run the full EditMode suite**

Run CPU-limited `Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode` and record total, passed, failed, and skipped counts from `Logs/editmode-results.xml`.

- [ ] **Step 2: Run the full PlayMode suite**

Run CPU-limited `Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode` and record the XML counts.

- [ ] **Step 3: Build the Android development APK**

Run CPU-limited `Tools/Unity/Build-AndroidDevelopment.ps1`. Record exit code, output path, and byte size for `Builds/Android/JoseonHunter-development.apk`.

- [ ] **Step 4: Inspect evidence and final diff**

Run `git diff --check`, inspect every task-owned diff, confirm no unrelated file is staged, and record that existing dirty assets remain uncommitted.

- [ ] **Step 5: Write, verify, commit, and push the report**

Document exact commands, timestamps, test counts, build size, automatic coverage, and physical-device limitations. Scan for placeholders, run `git diff --check`, commit `docs: verify progression reward clarity pass`, and push `origin master`.
