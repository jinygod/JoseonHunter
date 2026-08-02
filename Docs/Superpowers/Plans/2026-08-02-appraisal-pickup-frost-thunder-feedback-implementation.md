# Appraisal, Pickup, Frost, and Thunder Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove appraisal text overlap, give affix values a tension-building count-up, animate experience absorption, make Frost Flask visibly burst and control enemies, and reduce Thunder Crash Bomb damage.

**Architecture:** Keep the appraisal timing curve in the existing pure presentation helper and keep layout ownership in `WeaponAffixRevealPresenter`. Add a small pure `ExperiencePickupMotion` helper while leaving scene ownership in `FirstPlayableController`. Extend `FrostFlaskExecutor` with a separate one-shot landing attack and lower field ticks, and make Thunder balance a content-data-only change.

**Tech Stack:** Unity 6.0, C#/.NET, Unity Test Framework with NUnit, TextMeshPro, SpriteRenderer/TrailRenderer, ScriptableObject weapon content.

## Global Constraints

- Do not generate new PixelLab or Imagegen assets.
- Do not change experience attraction radius or final collection distance.
- Frost Flask uses a 100% base-damage landing burst, 50% base-damage 0.25-second field ticks, asset-authored slow, and the existing 0.75-second freeze residence.
- Thunder Crash Bomb damage is exactly `12/15/18/21/24`; range, cooldown, trajectory, level-5 shockwave, evolution, and potential multipliers remain structurally unchanged.
- Do not stage or overwrite user-owned changes in `Gameplay.unity`, `ProjectSettings.asset`, font assets, or unrelated imported metadata.
- Commit and push each independently verified task directly to `master`.

---

## File Structure

- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalPresentation.cs`: pure count-up progress mapping.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs`: count-up duration and reveal sequencing.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`: accumulated summary, potential rows, and confirm-button layout.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ExperiencePickupMotion.cs`: pure attraction speed, position, and stretch calculations.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`: pickup state, trail/flash presentation, Frost Flask constructor wiring, and Korean behavior copy.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`: landing burst, lower field ticks, authored slow strength, and stronger existing-sprite feedback.
- `Assets/JoseonHunter/Content/Weapons/ThunderCrashBomb.asset`: balanced level damage.
- Existing EditMode and PlayMode test files named in each task own regression coverage.

---

### Task 1: Appraisal spacing and tension count-up

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalPresentation.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponAppraisalPresentationTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixRevealTimelineTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Produces: `WeaponAppraisalPresentation.CountFractionAt(float progress) : float`.
- Preserves: `WeaponAppraisalPresentation.DisplayValueAt(double target, float progress) : int` and all existing presenter public test properties.

- [ ] **Step 1: Write failing count-curve and layout tests**

Add EditMode assertions that define the approved pacing:

```csharp
[Test]
public void CountUpRunsFastThenReservesDistinctFinalValues()
{
    Assert.That(WeaponAppraisalPresentation.DisplayValueAt(20d, .25f), Is.GreaterThanOrEqualTo(12));
    Assert.That(WeaponAppraisalPresentation.DisplayValueAt(20d, .78f), Is.EqualTo(18));
    Assert.That(WeaponAppraisalPresentation.DisplayValueAt(20d, .90f), Is.EqualTo(19));
    Assert.That(WeaponAppraisalPresentation.DisplayValueAt(20d, 1f), Is.EqualTo(20));
    Assert.That(WeaponAppraisalPresentation.DisplayValueAt(-8d, 1f), Is.EqualTo(-8));
}
```

Update timeline tests to require a 1.40-second standard count window and 1.60-second high/perfect window. Add a PlayMode layout assertion using `RectNamed` and world corners so `Accumulated Affix Summary`, `Reel Window 0`, `Reel Window 1`, `Reel Window 3`, and `Confirm Result` do not overlap vertically.

- [ ] **Step 2: Run focused tests and verify RED**

Run:

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.WeaponAppraisalPresentationTests|JoseonHunter.Tests.EditMode.WeaponAffixRevealTimelineTests'
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests'
```

Expected: the new pacing assertions and non-overlap assertion fail against the 0.75/0.90-second timeline and current coordinates.

- [ ] **Step 3: Implement the piecewise curve and layout**

Implement the curve with explicit phases:

```csharp
public static float CountFractionAt(float progress)
{
    var value = Mathf.Clamp01(progress);
    if (value <= .45f)
        return Mathf.Lerp(0f, .70f, EaseOutCubic(value / .45f));
    if (value <= .78f)
        return Mathf.Lerp(.70f, .90f, Mathf.SmoothStep(0f, 1f, (value - .45f) / .33f));
    return Mathf.Lerp(.90f, 1f, Mathf.SmoothStep(0f, 1f, (value - .78f) / .22f));
}

private static float EaseOutCubic(float value)
{
    var inverse = 1f - Mathf.Clamp01(value);
    return 1f - inverse * inverse * inverse;
}
```

Make `DisplayValueAt` round `target * CountFractionAt(progress)`. Set standard count duration to `1.40f` and high/perfect count duration to `1.60f`, recomputing read/potential stops from `countEnd`. Move the accumulated line to a dedicated 24-pixel-high band below the main result, set potential row centers to `-32f - index * 128f`, and move confirm to `-385f`. Preserve skip compression and final pulse behavior.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the two commands from Step 2. Expected: all targeted tests pass with updated exact duration expectations.

- [ ] **Step 5: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalPresentation.cs Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs Assets/JoseonHunter/Tests/EditMode/WeaponAppraisalPresentationTests.cs Assets/JoseonHunter/Tests/EditMode/WeaponAffixRevealTimelineTests.cs Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs
git commit -m "fix: improve appraisal pacing and spacing"
git push origin master
```

### Task 2: Accelerating experience absorption

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ExperiencePickupMotion.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ExperiencePickupMotion.cs.meta`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/ExperiencePickupMotionTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/ExperiencePickupMotionTests.cs.meta`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePickupRangePlayModeTests.cs`

**Interfaces:**
- Produces: `ExperiencePickupMotion.SpeedAt(float attractionAge, bool forceCollect) : float`.
- Produces: `ExperiencePickupMotion.Step(Vector2 current, Vector2 target, float attractionAge, float deltaTime, bool forceCollect) : Vector2`.
- Produces: `ExperiencePickupMotion.StretchAt(Vector2 direction, float attractionAge) : Vector3`.

- [ ] **Step 1: Write failing pure-motion and PlayMode tests**

Create tests proving speed increases with attraction age and forced collection remains faster:

```csharp
[Test]
public void AttractionAcceleratesAndForcedCollectionIsFastest()
{
    var early = ExperiencePickupMotion.SpeedAt(.02f, false);
    var late = ExperiencePickupMotion.SpeedAt(.30f, false);
    Assert.That(late, Is.GreaterThan(early));
    Assert.That(ExperiencePickupMotion.SpeedAt(.02f, true), Is.GreaterThan(late));
}
```

Extend the PlayMode test to assert a half-unit pickup gets a `TrailRenderer`, moves farther during the second equal tick than the first, and the one-unit pickup remains stationary without an enabled trail.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.ExperiencePickupMotionTests'
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.FirstPlayablePickupRangePlayModeTests'
```

Expected: the missing helper fails compilation first; after the test assembly compiles, the trail and acceleration assertions fail.

- [ ] **Step 3: Implement motion, trail, and shared collection flash**

Implement clamped acceleration without frame allocations:

```csharp
public static float SpeedAt(float attractionAge, bool forceCollect) =>
    forceCollect ? 24f : Mathf.Lerp(4f, 14f, Mathf.Clamp01(attractionAge / .32f));

public static Vector2 Step(Vector2 current, Vector2 target, float attractionAge, float deltaTime,
    bool forceCollect) => Vector2.MoveTowards(current, target,
        SpeedAt(attractionAge, forceCollect) * Mathf.Max(0f, deltaTime));
```

Add `Attracting`, `AttractionAge`, `BaseScale`, and `TrailRenderer` to `PickupState`. Create and configure the trail once when an experience pickup first enters attraction, use a shared sprite material, update directional stretch through the helper, and keep `pickupRadius` and `0.42f` collection distance unchanged. Create one reusable child `SpriteRenderer` at setup for a cyan collection flash; retrigger its short scale/alpha animation on every experience collection and destroy its shared material in `OnDestroy`.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the two commands from Step 2. Expected: all pickup motion and existing range tests pass.

- [ ] **Step 5: Commit and push**

Stage only the files listed for this task, commit `feat: animate experience absorption`, and push `master`.

### Task 3: Frost Flask landing burst and readable control field

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`

**Interfaces:**
- Adds optional constructor input: `float slowFraction = .5f` after existing optional inputs, exposed as `SlowFraction`.
- Adds internal landing resolution: `ResolveLandingBurst(Field field, in WeaponExecutionContext context)`.
- Preserves all existing evolution and potential interfaces.

- [ ] **Step 1: Write failing Frost behavior tests**

Add a mechanic test with base damage 10 proving landing creates one `ContactPhase.Blast` event for 10 damage and the next 0.25-second field step creates `ContactPhase.Tick` damage for 5. Add a test target assertion that constructor slow `0.35f` is applied instead of the prior hard-coded `0.5f`. Add a content/controller wiring test that the loaded Frost level passes `SlowFraction` into the executor.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.WeaponMechanicTests|JoseonHunter.Tests.EditMode.WeaponContentTests'
```

Expected: no landing damage event exists, field ticks still deal full base damage, and slow is hard-coded.

- [ ] **Step 3: Implement separate burst, lower ticks, and authored slow**

At the landing transition, call `ResolveLandingBurst` before `PlayLandingFragments`. Allocate a `RepeatHitPolicy.OncePerInstance` attack, test the existing disk mask at the field landing and radius, apply `Mathf.CeilToInt(BaseDamage)` with `ContactPhase.Blast`, then retire that attack. Change field tick damage to `Mathf.CeilToInt(BaseDamage * .5f)`. Store a clamped `SlowFraction` and use it in `ApplyFrostSlow`. Pass `data.SlowFraction` from `FirstPlayableController` using a named argument.

Increase field alpha from `.58f` to approximately `.82f`, tint the field with a limited pale-blue/teal palette, and play the existing impact sprite in a center pulse plus two rotated fragment cues. Do not add textures. Change the Korean behavior copy to `착지 폭발 후 서리 지대: 지속 피해·둔화·빙결`.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: all existing Frost evolution/potential tests and the new burst/tick/slow tests pass.

- [ ] **Step 5: Commit and push**

Stage the four task files, commit `feat: clarify frost flask impact`, and push `master`.

### Task 4: Thunder Crash Bomb damage balance

**Files:**
- Modify: `Assets/JoseonHunter/Content/Weapons/ThunderCrashBomb.asset`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`

**Interfaces:**
- Preserves `ThunderBombExecutor`; only authored `WeaponLevelData.BaseDamage` changes.

- [ ] **Step 1: Write the failing balance contract**

Add this asset test:

```csharp
[Test]
public void ThunderCrashBombUsesApprovedAreaDamageCurve()
{
    var definition = LoadDefinition(WeaponId.ThunderCrashBomb);
    CollectionAssert.AreEqual(new[] { 12f, 15f, 18f, 21f, 24f },
        definition.Levels.Select(level => level.BaseDamage).ToArray());
}
```

- [ ] **Step 2: Run the test and verify RED**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.WeaponContentTests.ThunderCrashBombUsesApprovedAreaDamageCurve'
```

Expected: actual values are `18/22/26/30/34`.

- [ ] **Step 3: Change only the five authored damage values**

Edit `ThunderCrashBomb.asset` levels 1-5 to `12`, `15`, `18`, `21`, and `24`. Do not alter cooldown, range, duration, projectile count, presentation sprites, or executor code.

- [ ] **Step 4: Run targeted and weapon mechanic tests**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.WeaponContentTests|JoseonHunter.Tests.EditMode.WeaponMechanicTests'
```

Expected: all tests pass.

- [ ] **Step 5: Commit and push**

Stage the asset and `WeaponContentTests.cs`, commit `balance: reduce thunder bomb area damage`, and push `master`.

### Task 5: Integrated verification and handoff

**Files:**
- Create: `Docs/Verification/2026-08-02-appraisal-pickup-frost-thunder-feedback.md`

**Interfaces:**
- Consumes all prior task outputs; produces a reproducible verification record.

- [ ] **Step 1: Run full EditMode tests**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode'
```

Expected: zero failed tests. Record total/pass/fail counts from `Logs/editmode-results.xml`.

- [ ] **Step 2: Run changed-surface PlayMode tests**

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayablePickupRangePlayModeTests'
```

Expected: zero failed changed-surface tests. Record counts from `Logs/playmode-results.xml`.

- [ ] **Step 3: Build Android development APK**

```powershell
& .\Tools\Unity\Build-AndroidDevelopment.ps1
```

Expected: exit code 0 and non-empty `Builds/Android/JoseonHunter-development.apk`.

- [ ] **Step 4: Record results and inspect the final diff**

Write the exact commands, timestamps, result counts, APK size, and any known unrelated failing suite to the verification document. Run:

```powershell
git diff --check
git status --short
git diff --stat origin/master...HEAD
```

Confirm the user-owned dirty files remain unstaged and unchanged by these commits.

- [ ] **Step 5: Commit and push verification**

Stage only the verification document, commit `docs: verify feedback and balance pass`, and push `master`.
