# Weapon Appraisal Detail Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the casino-like affix reels with a readable weapon detail appraisal that counts the general affix from zero and reveals up to three potential slots.

**Architecture:** Keep `WeaponAffixRevealPresenter.Play(WeaponAffixRollResult)` and its completion event compatible with the existing reward flow. Add small deterministic presentation models for number interpolation and potential-slot states, rebuild the presenter as the approved vertical detail sheet, and pass the selected weapon metadata from `FirstPlayableUiBootstrap`.

**Tech Stack:** Unity 6000.5.5f1, C#, uGUI, TextMeshPro, NUnit EditMode and PlayMode tests.

## Global Constraints

- Preserve existing affix values, jackpot probabilities, and combat effects.
- Normal automatic motion stays between 1.2 and 1.55 seconds; potential jackpots stay at or below 2.4 seconds.
- Existing potentials render immediately; only the next eligible empty slots shake or reveal.
- A failed potential roll ends as a quiet empty slot without failure copy or red VFX.
- Reuse imported PixelLab appraisal frames, potential icons, locked slots, and jackpot bursts.
- Do not use runtime primitive shapes as final replacement art.
- Use pooled/reused UI objects and `Time.unscaledDeltaTime`.

---

### Task 1: Deterministic appraisal presentation state

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalPresentation.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponAppraisalPresentationTests.cs`

**Interfaces:**
- Consumes: `WeaponAffixRoll`, `WeaponPotentialId`
- Produces: `WeaponAppraisalPresentation.DisplayValueAt(double target, float progress)` and `WeaponPotentialSlotState ResolveSlot(int index, int existingCount, IReadOnlyList<WeaponPotentialId> awarded, float time, WeaponAffixRevealTimeline timeline)`

- [ ] **Step 1: Write failing interpolation and slot-state tests**

```csharp
[TestCase(23.88, 0f, 0)]
[TestCase(23.88, 1f, 24)]
[TestCase(-8.4, 1f, -8)]
public void Display_value_moves_from_zero_to_rounded_target(double target, float progress, int expected)
{
    Assert.That(WeaponAppraisalPresentation.DisplayValueAt(target, progress), Is.EqualTo(expected));
}

[Test]
public void Existing_potential_stays_revealed_while_next_empty_slot_is_eligible()
{
    var state = WeaponAppraisalPresentation.ResolveSlot(0, 1,
        Array.Empty<WeaponPotentialId>(), .5f, WeaponAffixRevealTimeline.For(StandardResult()));
    Assert.That(state.Kind, Is.EqualTo(WeaponPotentialSlotKind.Existing));
}
```

- [ ] **Step 2: Run the focused EditMode test and confirm missing-type failures**

Run Unity EditMode tests filtered to `WeaponAppraisalPresentationTests`.
Expected: compilation fails because `WeaponAppraisalPresentation` does not exist.

- [ ] **Step 3: Implement eased count-up and explicit slot kinds**

```csharp
public enum WeaponPotentialSlotKind { Existing, Shaking, Revealed, Empty }

public static int DisplayValueAt(double target, float progress)
{
    var p = Mathf.Clamp01(progress);
    var eased = 1f - Mathf.Pow(1f - p, 3f);
    return Mathf.RoundToInt((float)target * eased);
}
```

`ResolveSlot` must return existing slots first, then newly awarded slots at their timeline stop, one shaking next slot while unresolved, and `Empty` afterward.

- [ ] **Step 4: Run the focused EditMode test**

Expected: all `WeaponAppraisalPresentationTests` pass.

### Task 2: Vertical weapon detail appraisal UI

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixValueFormatter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Consumes: Task 1 interpolation and slot state
- Produces: compatible `Play(WeaponAffixRollResult result)`, plus `Play(WeaponAppraisalViewModel model)` and read-only properties for tests

- [ ] **Step 1: Add failing PlayMode tests for approved hierarchy**

```csharp
[UnityTest]
public IEnumerator Appraisal_shows_weapon_header_large_value_and_three_vertical_potential_rows()
{
    presenter.Play(ModelWithWeaponAndResult());
    yield return null;
    Assert.That(presenter.PanelSize.x, Is.GreaterThanOrEqualTo(900f));
    Assert.That(presenter.PotentialRowY(0), Is.GreaterThan(presenter.PotentialRowY(1)));
    Assert.That(presenter.PotentialRowY(1), Is.GreaterThan(presenter.PotentialRowY(2)));
}
```

Add cases for count-up starting at zero, quiet failure, one-to-three potential reveals, and a single completion event after rapid skip/confirm taps.

- [ ] **Step 2: Run focused PlayMode tests and confirm they fail**

Expected: missing view-model overload and vertical-detail properties.

- [ ] **Step 3: Replace four horizontal reels with the approved detail sheet**

Build one centered panel around `1040 x 820` reference pixels:

- header: 144px icon, weapon name, level, behavior
- general affix: large 42px value and tier-colored progress accent
- potentials: three rows around 112px tall with icon, name, and effect description
- confirmation button at bottom

Remove spinning reel symbols from the visible hierarchy. Keep imported shell/locked-slot/burst sprites and existing fallback behavior when the catalog is missing.

- [ ] **Step 4: Implement count-up and restrained potential motion**

Use `WeaponAppraisalPresentation.DisplayValueAt` during the timeline. Apply a damped horizontal/rotation shake only to the next unresolved potential row. Enable purple ritual overlay and burst only when `NewPotentials.Count > 0`; reveal multiple results 0.18 seconds apart.

- [ ] **Step 5: Run focused PlayMode tests**

Expected: all `WeaponAffixRevealPlayModeTests` pass.

### Task 3: Supply weapon detail context and reopen it from the rack

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/FirstPlayableUiStateTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponRackPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponSlotView`, `ProgressionRewardEvent`, Task 2 `WeaponAppraisalViewModel`
- Produces: `WeaponRackPresenter.WeaponSelected` event and read-only detail open/close flow

- [ ] **Step 1: Write failing tests for view-model context and rack tap**

```csharp
[Test]
public void Appraisal_model_combines_reward_with_current_weapon_state()
{
    var model = WeaponAppraisalViewModel.From(reward, slot);
    Assert.That(model.WeaponId, Is.EqualTo(reward.WeaponId));
    Assert.That(model.CurrentPotentials, Is.EquivalentTo(slot.PotentialIds));
}
```

Add a PlayMode test that invokes a rack slot button and observes one `WeaponSelected` callback with its current `WeaponSlotView`.

- [ ] **Step 2: Run focused tests and confirm missing API failures**

- [ ] **Step 3: Extend UI state without changing combat authority**

Add `Behavior` to `WeaponSlotView`, preserving the existing constructor through an optional parameter. Build `WeaponAppraisalViewModel` from the reward plus the already-rendered slot state. Derive pre-roll existing potential count as:

```csharp
Mathf.Max(0, currentSlot.PotentialIds.Count - reward.AffixResult.NewPotentials.Count)
```

- [ ] **Step 4: Add rack buttons and read-only detail opening**

Each rack slot gets one reused `Button`. `FirstPlayableUiBootstrap` subscribes to `WeaponSelected`, opens the same detail presenter without animation, and closes it from backdrop or close button. Do not call the reward completion event for read-only detail.

- [ ] **Step 5: Run state and rack tests**

Expected: focused EditMode and PlayMode tests pass.

### Task 4: Regression and runtime visual validation

**Files:**
- Modify only if evidence requires it: `Assets/JoseonHunter/Scripts/Editor/Scenes/EightWeaponPolishCapture.cs`

**Interfaces:**
- Consumes: completed Tasks 1-3
- Produces: test XML/log evidence and one appraisal screenshot

- [ ] **Step 1: Run appraisal and upgrade regression tests**

Run EditMode filters for appraisal/timeline/value formatting and PlayMode filters for affix reveal, upgrade choice, weapon rack, and reward sequencing.
Expected: zero failures in the selected suites.

- [ ] **Step 2: Run eight-weapon combat tests**

Run `EightWeaponCombatPlayModeTests`.
Expected: 8/8 pass and no changes to damage or potential behavior.

- [ ] **Step 3: Capture a standard roll and a one-potential jackpot**

Check:

- header and final percentage readable at mobile reference size
- three potential rows remain inside safe area
- failed slot settles quietly
- jackpot overlay does not cover weapon name or result value
- automatic motion stays within specified duration

- [ ] **Step 4: Review and commit only intended files**

Use `git diff --check`, inspect staged names, preserve unrelated imported `.meta` and `ProjectSettings` changes, then commit the implementation.
