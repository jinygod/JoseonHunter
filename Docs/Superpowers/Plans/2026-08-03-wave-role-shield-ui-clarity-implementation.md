# Wave Role, Shield Dokkaebi, and Growth UI Clarity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Teach enemy roles in a controlled 180-second sequence, replace shield-dokkaebi sprite flicker with a six-hit directional guard, and make the legacy/appraisal UI fully opaque and understandable in Korean.

**Architecture:** Keep deterministic roster, introduction, and guard rules in existing plain-C# Domain/Runtime rule types. `FirstPlayableController` remains the runtime owner of spawned enemy state and presentation, while existing Presentation presenters own only their UI trees and copy. Add one post-deduplication combat hook so guard durability changes only for confirmed hits; do not add managers, packages, scene objects, or serialized fields.

**Tech Stack:** Unity 6000.5.5f1, C# 9, uGUI, TextMeshPro, NUnit Unity Test Framework, URP 2D, Android ARM64 IL2CPP.

## Global Constraints

- Work directly on `master`; commit and push every completed task.
- Preserve unrelated local Editor/user changes and stage only files named by the current task.
- Use strict TDD: add the behavior test, run it and observe the expected failure, then write minimal production code.
- Keep the 180-second run, 140 active-enemy ceiling, existing boss rules, runtime-generated scene composition, and Korean-only player copy.
- Do not create new art, packages, managers, singletons, prefabs, scenes, or serialized fields.
- Never replace a shield-dokkaebi body sprite with `telegraph_0/1`; use the existing base sprite and flat-color feedback.
- Keep hot-path selection, movement, and shield updates allocation-free.
- Run Unity jobs sequentially with Unity and descendants at `BelowNormal` priority and processor affinity `63` (6 of 16 logical cores).
- Normalize Unity-added trailing whitespace only under `Assets/JoseonHunter/Art/CombatChoices/**/*.meta`; do not stage the preserved dirty assets/settings.

---

## File Map

### Domain and deterministic rules

- `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`: elapsed-time normal rosters and authored introduction definitions.
- `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs`: deterministic weighted selection, one-time introductions, special reappearance cooldown, and 1:8 special cap.

### Combat and runtime ownership

- `Assets/JoseonHunter/Scripts/Runtime/Combat/ICombatTarget.cs`: optional confirmed-resistance callback contract.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs`: invoke that callback only after hit deduplication succeeds.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs`: normal-role multipliers, display scale, and pure six-charge shield rules.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs`: reusable bronze guard flash using the existing hit-motion path.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`: introduction dispatch, spawn-state application, guard durability/bar ownership, and stable shield sprite.

### Presentation

- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponLegacyChoicePresenter.cs`: opaque card underlay and ink colors.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`: player-facing growth copy, explanation row, accumulated-summary alignment, and safe vertical positions.
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`: provide complete growth-state and next-level phrases to the view model.

### Tests and evidence

- `Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/SpecialEnemyRuleTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/CombatDamageServiceTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/WaveRosterPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/SpecialEnemyCombatPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyPresentationPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`
- `Docs/Verification/2026-08-03-wave-role-shield-ui-clarity.md`

---

### Task 1: Authored normal-enemy roles and elapsed-time rosters

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/SpecialEnemyRuleTests.cs`

**Interfaces:**
- Produces: `WaveSchedule.NormalEntriesAt(float elapsedSeconds)` returning one of six cached immutable weighted rosters for the current authored window.
- Produces: `WaveSpawnDirector.SelectNormal(float elapsedSeconds)` using the director's seeded `Random` and the elapsed roster.
- Produces: `EnemyArchetypeProfile.DisplayScaleMultiplier` in addition to health, speed, and contact multipliers.
- Preserves: `WaveSchedule.For(RunPhase)` for active caps, packs, boss stopping, and existing callers until Task 2 removes obsolete special-family fields.

- [ ] **Step 1: Write failing roster-window tests**

Add literal assertions to `WaveSpawnDirectorTests`:

```csharp
[TestCase(20f, "plague_rat")]
[TestCase(46f, "vengeful_spirit")]
[TestCase(94f, "dokkaebi")]
public void Introduction_windows_select_only_the_new_normal_family(float elapsed, string expected)
{
    var director = new WaveSpawnDirector(1701);
    Assert.That(Enumerable.Range(0, 32).Select(_ => director.SelectNormal(elapsed)),
        Is.All.EqualTo(expected));
}

[Test]
public void Learned_normal_families_mix_only_at_the_approved_weights()
{
    Assert.That(WaveSchedule.NormalEntriesAt(60f).Select(entry => (entry.ContentId, entry.Weight)),
        Is.EqualTo(new[] { ("plague_rat", 60), ("vengeful_spirit", 40) }));
    Assert.That(WaveSchedule.NormalEntriesAt(110f).Select(entry => (entry.ContentId, entry.Weight)),
        Is.EqualTo(new[] { ("plague_rat", 25), ("vengeful_spirit", 40), ("dokkaebi", 35) }));
    Assert.That(WaveSchedule.NormalEntriesAt(145f).Select(entry => (entry.ContentId, entry.Weight)),
        Is.EqualTo(new[] { ("plague_rat", 20), ("vengeful_spirit", 40), ("dokkaebi", 40) }));
}
```

Add literal profile assertions to `SpecialEnemyRuleTests`:

```csharp
[Test]
public void Normal_enemy_profiles_have_distinct_readable_roles()
{
    AssertProfile("plague_rat", .75f, 1.10f, .80f, 1f);
    AssertProfile("vengeful_spirit", .55f, 1.65f, .75f, 1f);
    AssertProfile("dokkaebi", 2.60f, .55f, 1.35f, 1.15f);
}
```

- [ ] **Step 2: Run the focused EditMode tests and verify RED**

Run capped Unity EditMode with filter:

```text
JoseonHunter.Tests.EditMode.WaveSpawnDirectorTests|JoseonHunter.Tests.EditMode.SpecialEnemyRuleTests
```

Expected failures: missing `NormalEntriesAt(float)`, missing `SelectNormal(float)`, and default `1f` normal-role multipliers.

- [ ] **Step 3: Implement the elapsed rosters and role profiles**

Create the six roster arrays once as `private static readonly IReadOnlyList<WeightedEnemyEntry>` fields. Add `WaveSchedule.NormalEntriesAt` with these exact ranges and return only those cached fields:

```csharp
public static IReadOnlyList<WeightedEnemyEntry> NormalEntriesAt(float elapsedSeconds)
{
    if (float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds) || elapsedSeconds < 0f)
        throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
    if (elapsedSeconds < 45f) return RatOnlyEntries;
    if (elapsedSeconds < 53f) return SpiritOnlyEntries;
    if (elapsedSeconds < 90f) return RatSpiritEntries;
    if (elapsedSeconds < 100f) return DokkaebiOnlyEntries;
    if (elapsedSeconds < 135f) return LearnedEntries;
    return PeakEntries;
}
```

Change weighted selection to consume this method:

```csharp
public string SelectNormal(float elapsedSeconds) =>
    SelectWeighted(WaveSchedule.NormalEntriesAt(elapsedSeconds));
```

Keep one private `SelectWeighted` loop; do not use LINQ in production.

Update the broad `WaveSchedule.For` rosters to the same learned-family sets and change pack content to: wave one `plague_rat`, wave two `vengeful_spirit`, wave three `dokkaebi`, peak alternating `vengeful_spirit` and `dokkaebi`. This removes `sakkat_specter` and `bandit` from both continuous and pack paths.

Extend the profile constructor and mappings with `DisplayScaleMultiplier`. Keep `EnemyArchetype.Normal` for all three ordinary families so `IsSpecial` remains correct.

- [ ] **Step 4: Run the focused EditMode tests and verify GREEN**

Expected: all `WaveSpawnDirectorTests` and `SpecialEnemyRuleTests` pass.

- [ ] **Step 5: Commit and push Task 1**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs Assets/JoseonHunter/Tests/EditMode/SpecialEnemyRuleTests.cs
git commit -m "feat: give normal enemies readable combat roles"
git push origin master
```

---

### Task 2: Authored special introductions and bounded reappearances

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs`

**Interfaces:**
- Produces: `EnemyIntroductionDefinition` with `AtSeconds`, `ContentId`, and `SpawnCount`.
- Produces: `EnemyIntroductionPlan` with `ContentId` and `SpawnCount`.
- Produces: `WaveSpawnDirector.TryCreateIntroduction(float elapsedSeconds, int availableSlots, out EnemyIntroductionPlan plan)`.
- Replaces: `TrySelectSpecial(RunPhase, int, int, out string)` with `TrySelectSpecial(float elapsedSeconds, int livingNormalCount, int livingSpecialCount, out string contentId)`.
- Contract: special cap is `floor(livingNormalCount / 8)`, reappearance cooldown is 8 seconds, and only already introduced families can reappear.

- [ ] **Step 1: Write failing introduction and special-cap tests**

```csharp
[Test]
public void Authored_introductions_are_emitted_once_in_chronological_order()
{
    var director = new WaveSpawnDirector(1701);
    Assert.That(director.TryCreateIntroduction(102f, 10, out var shield), Is.True);
    Assert.That((shield.ContentId, shield.SpawnCount), Is.EqualTo(("shield_dokkaebi", 1)));
    Assert.That(director.TryCreateIntroduction(102f, 10, out _), Is.False);
    Assert.That(director.TryCreateIntroduction(120f, 10, out var horn), Is.True);
    Assert.That(horn.ContentId, Is.EqualTo("charging_horn_ghost"));
    Assert.That(director.TryCreateIntroduction(138f, 10, out var shaman), Is.True);
    Assert.That(shaman.ContentId, Is.EqualTo("spirit_shaman"));
    Assert.That(director.TryCreateIntroduction(150f, 10, out var rat), Is.True);
    Assert.That(rat.ContentId, Is.EqualTo("splitting_rat"));
}

[Test]
public void Specials_require_introduction_one_eighth_capacity_and_eight_second_cooldown()
{
    var director = new WaveSpawnDirector(1701);
    Assert.That(director.TrySelectSpecial(101f, 80, 0, out _), Is.False);
    Assert.That(director.TryCreateIntroduction(102f, 10, out _), Is.True);
    Assert.That(director.TrySelectSpecial(109.9f, 80, 0, out _), Is.False);
    Assert.That(director.TrySelectSpecial(110f, 80, 0, out var id), Is.True);
    Assert.That(id, Is.EqualTo("shield_dokkaebi"));
    Assert.That(director.TrySelectSpecial(118f, 80, 10, out _), Is.False);
}
```

- [ ] **Step 2: Run `WaveSpawnDirectorTests` and verify RED**

Expected: introduction types/methods are missing and current 1:4 phase-random special behavior violates literals.

- [ ] **Step 3: Implement immutable introductions and director state**

Expose a read-only static introduction array from `WaveSchedule`:

```csharp
new EnemyIntroductionDefinition(102f, "shield_dokkaebi", 1),
new EnemyIntroductionDefinition(120f, "charging_horn_ghost", 1),
new EnemyIntroductionDefinition(138f, "spirit_shaman", 1),
new EnemyIntroductionDefinition(150f, "splitting_rat", 1)
```

In `WaveSpawnDirector`, replace per-phase random family state with:

```csharp
private int nextIntroductionIndex;
private int introducedSpecialCount;
private int specialOrdinal;
private float lastSpecialSpawnSeconds = float.NegativeInfinity;
```

`TryCreateIntroduction` checks only the next authored entry, returns false until `elapsedSeconds >= AtSeconds`, and does not advance while `availableSlots < SpawnCount`; this lets a full battlefield retry the introduction on a later tick. On success it increments `introducedSpecialCount` and records `lastSpecialSpawnSeconds`. `TrySelectSpecial` rejects calls before the first introduction, during cooldown, or at the 1:8 cap; otherwise it cycles deterministically through the introduced prefix of the authored array. Remove `SpecialContentIds`, `MaximumSpecialFamilies`, `specialPhase`, and `specialFamilies` after all callers use this path.

- [ ] **Step 4: Run `WaveSpawnDirectorTests` and verify GREEN**

Expected: deterministic, introduction, cooldown, and cap tests all pass.

- [ ] **Step 5: Commit and push Task 2**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs
git commit -m "feat: introduce special enemies on an authored cadence"
git push origin master
```

---

### Task 3: Six-hit directional shield with confirmed-hit semantics

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/ICombatTarget.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/CombatDamageServiceTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/SpecialEnemyRuleTests.cs`

**Interfaces:**
- Produces: `IConfirmedDamageResistanceTarget : IIncomingDamageResistanceTarget` with `void ConfirmIncomingHit(Float2 attackOrigin, WeaponHitTrait traits)`.
- Produces: `ShieldDokkaebiGuard.MaximumCharges = 6`, `DamageMultiplier(...)`, and `ConfirmHit(...)` in `EnemyArchetypeProfile.cs`.
- Produces: immutable `ShieldGuardHitResult` with `RemainingCharges`, `Blocked`, and `Broke`.
- Contract: multiplier lookup is pure; durability changes only after `AttackInstance.TryRecordHit` succeeds.

- [ ] **Step 1: Write failing pure guard-rule tests**

```csharp
[Test]
public void Shield_guard_breaks_after_six_confirmed_front_direct_hits()
{
    var remaining = ShieldDokkaebiGuard.MaximumCharges;
    for (var hit = 0; hit < 6; hit++)
    {
        Assert.That(ShieldDokkaebiGuard.DamageMultiplier(remaining, Vector2.right, Vector2.right,
            WeaponHitTrait.Slash), Is.EqualTo(.15f));
        var result = ShieldDokkaebiGuard.ConfirmHit(remaining, Vector2.right, Vector2.right,
            WeaponHitTrait.Slash);
        remaining = result.RemainingCharges;
        Assert.That(result.Broke, Is.EqualTo(hit == 5));
    }
    Assert.That(remaining, Is.Zero);
    Assert.That(ShieldDokkaebiGuard.DamageMultiplier(remaining, Vector2.right, Vector2.right,
        WeaponHitTrait.Slash), Is.EqualTo(1f));
}

[TestCase(WeaponHitTrait.Explosion)]
[TestCase(WeaponHitTrait.Pull)]
[TestCase(WeaponHitTrait.Reaction)]
public void Shield_guard_bypass_traits_deal_full_damage_without_consuming_durability(WeaponHitTrait trait)
{
    Assert.That(ShieldDokkaebiGuard.DamageMultiplier(6, Vector2.right, Vector2.right, trait), Is.EqualTo(1f));
    Assert.That(ShieldDokkaebiGuard.ConfirmHit(6, Vector2.right, Vector2.right, trait).RemainingCharges,
        Is.EqualTo(6));
    Assert.That(ShieldDokkaebiGuard.ConfirmHit(6, Vector2.right, Vector2.left,
        WeaponHitTrait.Slash).RemainingCharges, Is.EqualTo(6));
}
```

- [ ] **Step 2: Write a failing damage-service deduplication test**

Add a fake implementing `IConfirmedDamageResistanceTarget`. Submit the same once-per-instance request twice and assert:

```csharp
Assert.That(service.TryApply(request, out _), Is.True);
Assert.That(service.TryApply(request, out _), Is.False);
Assert.That(target.ConfirmedHitCount, Is.EqualTo(1));
```

This catches the bug where querying resistance before deduplication could consume two shield charges.

- [ ] **Step 3: Run the two focused EditMode classes and verify RED**

Filter:

```text
JoseonHunter.Tests.EditMode.CombatDamageServiceTests|JoseonHunter.Tests.EditMode.SpecialEnemyRuleTests
```

Expected: missing guard and callback types.

- [ ] **Step 4: Implement the pure guard and confirmed callback**

Use the existing front test `Vector2.Dot(facing.normalized, attackDirection.normalized) >= .5f`. Bypass masks are exactly `Explosion | Pull | Reaction`. `ConfirmHit` decrements only when `DamageMultiplier` would be `.15f`.

In `CombatDamageService.TryApply`, keep resistance multiplier resolution before `DamageResolver`, but call the mutation hook only after successful hit recording:

```csharp
if (!attack.TryRecordHit(request.Target.RuntimeId, request.Phase, request.HitTime)) return false;
if (resistance is IConfirmedDamageResistanceTarget confirmedResistance)
    confirmedResistance.ConfirmIncomingHit(origin, request.Traits);
request.Target.ApplyResolvedDamage(result.FinalDamage);
```

Declare `origin` and `resistance` outside the conditional so the exact values used for preview are used for confirmation.

- [ ] **Step 5: Run the focused EditMode tests and verify GREEN**

Expected: guard rules, one-confirmation deduplication, and all existing damage service tests pass.

- [ ] **Step 6: Commit and push Task 3**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/ICombatTarget.cs Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyArchetypeProfile.cs Assets/JoseonHunter/Tests/EditMode/CombatDamageServiceTests.cs Assets/JoseonHunter/Tests/EditMode/SpecialEnemyRuleTests.cs
git commit -m "feat: add confirmed six-hit shield guard"
git push origin master
```

---

### Task 4: Integrate authored waves and stable shield presentation

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WaveRosterPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/SpecialEnemyCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: `SelectNormal(float)`, `TryCreateIntroduction(...)`, `TrySelectSpecial(float, ...)`, `DisplayScaleMultiplier`, and `ShieldDokkaebiGuard` from Tasks 1–3.
- Produces: `CombatantVisualRig.ShowGuardHit(Vector2 incomingDirection, bool broke)` using bronze flash and existing hit motion.
- Runtime state: `EnemyState.ShieldChargesRemaining`, `EnemyState.ShieldFill`; no serialized state.
- Preserves: existing `ShowSpecialEnemyGuide` one-time announcement channel and 140 total active cap.

- [ ] **Step 1: Write failing PlayMode wave-integration tests**

Replace the old broad phase expectations with observable roster behavior:

```csharp
[UnityTest]
public IEnumerator Spirit_and_dokkaebi_intro_windows_spawn_only_the_new_family()
{
    var controller = LoadGameplay();
    controller.SetElapsedForTests(46f);
    yield return null;
    TickSpawning(controller, 2f);
    Assert.That(controller.LivingNormalEnemyIdsForTests, Is.All.EqualTo("vengeful_spirit"));

    SceneManager.LoadScene("Gameplay");
    controller = Object.FindAnyObjectByType<FirstPlayableController>();
    controller.SetElapsedForTests(94f);
    yield return null;
    TickSpawning(controller, 2f);
    Assert.That(controller.LivingNormalEnemyIdsForTests, Is.All.EqualTo("dokkaebi"));
}
```

Add a 110-second integration assertion that all normal IDs belong to `plague_rat`, `vengeful_spirit`, `dokkaebi`, and that no `sakkat_specter` or `bandit` appears.

- [ ] **Step 2: Write failing shield-presentation PlayMode tests**

Spawn one shield dokkaebi at a unique position, retain its body `SpriteRenderer`, tick movement while near the player, and assert the sprite reference stays equal to its initial base frame. Assert a child named `Shield Guard Bar` exists, begins full, decreases after confirmed front hits, and becomes inactive/zero-width after six unique confirmed hits.

Also assert the first guide is exactly:

```text
방패 도깨비 · 정면을 여러 번 치거나 뒤를 노리세요
```

- [ ] **Step 3: Run the focused PlayMode classes and verify RED**

Filter:

```text
JoseonHunter.Tests.PlayMode.WaveRosterPlayModeTests|JoseonHunter.Tests.PlayMode.SpecialEnemyCombatPlayModeTests
```

Expected: controller still selects by broad phase, shield body alternates frames, and there is no guard bar/state.

- [ ] **Step 4: Integrate elapsed roster selection and introductions**

Change continuous and burst spawn selection from `SelectNormal(phase)` to `SelectNormal(elapsed)`. At each gameplay tick, process crossed authored introductions before ordinary spawning. Spawn fixed special introductions through existing `SpawnEnemy(false, 0, contentId)` and let `ShowSpecialEnemyGuide` publish the one-time message.

Publish the two normal-family announcements exactly at 45 and 90 seconds:

```text
원한 처녀귀신 출현 · 매우 빠르지만 약합니다
도깨비 출현 · 느리지만 매우 단단합니다
```

Apply `DisplayScaleMultiplier` to normal display scale after the existing rank scale.

- [ ] **Step 5: Integrate shield runtime state and visual feedback**

Initialize shield enemies with six charges. Create a health bar for them when `HealthFill == null`, then create `Shield Guard Bar` at local Y `-1.48` with existing `solidSprite`: background scale `(2.2, .20, 1)`, color `(.12, .09, .06, .94)`, sorting order 20; fill scale `(2, .10, 1)`, color `(.72, .45, .14, 1)`, sorting order 21. Rename `UpdateHealthBar` to `UpdateBarFill(Transform fill, float ratio, float fullWidth, float height)` and call it with `(2f, .14f)` for health and `(2f, .10f)` for shield durability.

Make `PrototypeCombatTarget` implement `IConfirmedDamageResistanceTarget`:

- `IncomingDamageMultiplier` delegates to the pure guard using current charges for shields; other profiles retain current behavior.
- `ConfirmIncomingHit` applies `ShieldDokkaebiGuard.ConfirmHit`, updates the fill, and calls `ShowGuardHit` only for blocked front hits.

In `UpdateSpecialEnemyFrame`, return immediately for `ShieldDokkaebi`; never assign frame 1 or 2. Keep existing frame behavior for the other three special families.

Extend `CombatantVisualRig` so guard feedback uses a bronze flash color and existing hit-motion strength (`.075f` block, `.15f` break) without a coroutine or new GameObject.

- [ ] **Step 6: Run focused PlayMode tests and focused EditMode guard tests**

Expected: all wave, special enemy, damage callback, and guard tests pass.

- [ ] **Step 7: Commit and push Task 4**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/PlayMode/WaveRosterPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/SpecialEnemyCombatPlayModeTests.cs
git commit -m "feat: stage enemy introductions and shield feedback"
git push origin master
```

---

### Task 5: Opaque legacy cards and understandable appraisal copy

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponLegacyChoicePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyPresentationPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Produces UI child: `Legacy Card Background {index}` as an opaque first sibling inside each card.
- Produces UI child: `Growth Guide` in the appraisal shell.
- Changes player copy only; keeps `WeaponSlotView` and `WeaponAppraisalViewModel` signatures unchanged.
- Produces complete `LegacyStageName`/`NextLegacyMilestone` phrases from `FirstPlayableController`.

- [ ] **Step 1: Write failing opaque-card and color tests**

In `WeaponLegacyPresentationPlayModeTests`, for both cards assert:

```csharp
var background = card.Find("Legacy Card Background " + index).GetComponent<Image>();
Assert.That(background.color.a, Is.EqualTo(1f));
Assert.That(background.transform.GetSiblingIndex(), Is.Zero);
Assert.That(background.sprite, Is.Null);
Assert.That(TextColor(TextNamed(card, "Combat Style")), Is.EqualTo(JoseonUiPalette.HanjiInk));
```

Add a local reflection helper `TextColor(Component text) => (Color)text.GetType().GetProperty("color").GetValue(text)`. Assert the modal heading uses `HanjiInk` and the cost remains `SealCrimson`.

- [ ] **Step 2: Write failing appraisal-copy and geometry tests**

Use a level-3 `WeaponSlotView` with legacy `수호신강림`, stage `성장 방향 선택 완료`, and next milestone `무기 4레벨 달성 시 효과 강화`. Assert exact strings:

```text
선택한 성장 · 수호신강림
현재 상태 · 성장 방향 선택 완료
다음 강화 · 무기 4레벨 달성 시 효과 강화
```

Assert `Growth Guide` equals:

```text
무기 3레벨에 성장 방식을 선택하고, 4·5레벨에 선택한 효과가 강화됩니다.
```

Update the accumulated-summary expectation to start with `지금까지 적용된 효과 · `. Geometry assertions must prove:

- the summary left edge is inside the first result window's left edge plus 32 pixels;
- at least 6 pixels separate result window, summary, first growth row, subsequent rows, and confirm button;
- all elements are contained at both 720×1280 and 1080×1920 virtual canvases.

- [ ] **Step 3: Run the two focused PlayMode classes and verify RED**

Filter:

```text
JoseonHunter.Tests.PlayMode.WeaponLegacyPresentationPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests
```

Expected: missing card backgrounds/guide, old jargon, and summary X alignment failure.

- [ ] **Step 4: Implement the opaque legacy card underlay**

Inside `CreateCard`, after setting the frame sprite, add a sprite-less `Image` named `Legacy Card Background {index}`, stretch it with 12-pixel inset, color it `Hanji`, disable raycasts, and call `SetAsFirstSibling`. Explicitly set heading, name, combat style, and benefit to `HanjiInk`; keep cost crimson.

- [ ] **Step 5: Implement appraisal copy and safe vertical layout**

Add `Growth Guide` at anchored position `(80, 202)`, size `(620, 20)`, font size `17`, below weapon behavior using body font and `HanjiMutedInk`. Change the accumulated label prefix and move it from `(-80, 44)` to `(0, 44)` while keeping size `(740, 24)`. Keep the established result window at `(0, 126)`, growth-row centers at `-32`, `-160`, and `-288`, and confirmation center at `-385`; these literal positions provide 6, 10, and 12 pixels of vertical separation respectively.

Replace `BindLegacyRows` output with the approved labels. In `FirstPlayableController` return these complete state phrases:

```csharp
WeaponLegacyStage.Chosen => "성장 방향 선택 완료",
WeaponLegacyStage.Reinforced => "선택 효과 강화됨",
WeaponLegacyStage.Completed => "최종 효과 완성",
_ => "무기 3레벨에서 두 방식 중 하나 선택"
```

and:

```csharp
WeaponLegacyStage.Chosen => "무기 4레벨 달성 시 효과 강화",
WeaponLegacyStage.Reinforced => "무기 5레벨 달성 시 최종 효과 완성",
WeaponLegacyStage.Completed => "최종 효과 적용 중",
_ => "무기 3레벨에서 두 방식 중 하나 선택"
```

For completed growth, render row three as `성장 완료 · 최종 효과 적용 중`; otherwise use `다음 강화 · ...`.

- [ ] **Step 6: Run the focused PlayMode tests and verify GREEN**

Expected: both UI classes pass, including exact Korean copy and geometry.

- [ ] **Step 7: Commit and push Task 5**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/WeaponLegacyChoicePresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/PlayMode/WeaponLegacyPresentationPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs
git commit -m "fix: clarify legacy choice and appraisal UI"
git push origin master
```

---

### Task 6: Performance, full regression, portrait evidence, and Android build

**Files:**
- Create: `Docs/Verification/2026-08-03-wave-role-shield-ui-clarity.md`

**Interfaces:**
- Consumes: completed behavior from Tasks 1–5.
- Produces: repeatable test/build evidence and explicit remaining device-only checks.

- [ ] **Step 1: Run all related tests sequentially**

Run these capped filters and parse XML, because Unity process exit code alone is insufficient:

```text
EditMode: JoseonHunter.Tests.EditMode.WaveSpawnDirectorTests|JoseonHunter.Tests.EditMode.SpecialEnemyRuleTests|JoseonHunter.Tests.EditMode.CombatDamageServiceTests
PlayMode: JoseonHunter.Tests.PlayMode.WaveRosterPlayModeTests|JoseonHunter.Tests.PlayMode.SpecialEnemyCombatPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponLegacyPresentationPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests
```

Expected: zero failed tests.

- [ ] **Step 2: Run the 140-target 30-second performance gate**

Run:

```text
JoseonHunter.Tests.PlayMode.CombatPerformanceInvestigationPlayModeTests.CompletedAreaBranchesAgainstOneHundredFortyTargetsStayBoundedForThirtySeconds
```

Expected: pass, managed allocation remains 0 B per measured tick, active combatants never exceed 140.

- [ ] **Step 3: Run full EditMode and full PlayMode sequentially**

Expected baseline before new tests: EditMode 597/597 and PlayMode 211/211. Final totals must be at least those baselines plus newly added cases, with zero failures and zero unexpected skips.

- [ ] **Step 4: Capture and inspect portrait gameplay/UI evidence**

Run `JoseonHunter.Editor.Scenes.PortraitStateValidationCapture.CaptureInBatchMode` without `-nographics` and inspect its real Gameplay/appraisal captures at 720×1280 and 1080×2340. Inspect:

- accumulated options aligned below the first result box;
- all growth rows and confirm button visible;

The existing capture command does not author a legacy-choice or shield-introduction state. Validate those two surfaces with the focused PlayMode renderer/layout tests in Steps 1–2 and record interactive Editor/device visual inspection as remaining work instead of claiming it occurred.

Do not commit generated screenshots unless the existing verification convention requires them.

- [ ] **Step 5: Build the Android development APK**

Execute `JoseonHunter.Editor.Build.AndroidDevelopmentBuild.Build` with `GRADLE_USER_HOME=C:\jh-gradle` under the global CPU limits. Verify Unity exits 0 and `Builds/Android/JoseonHunter-development.apk` is non-empty. Record byte size, timestamp, SHA-256, and Unity-reported duration.

- [ ] **Step 6: Write verification evidence**

Document:

- root causes and implemented behavior;
- exact focused/full test totals and XML/log paths;
- performance duration and allocation result;
- portrait resolutions inspected and any visual limitation;
- APK size, hash, duration, and log path;
- CPU/affinity policy and observed system CPU/memory range;
- physical-device frame pacing as remaining work if no device run occurs.

- [ ] **Step 7: Review diff, commit, and push Task 6**

Normalize only generated CombatChoices meta whitespace, run `git diff --check` on staged files, and confirm unrelated dirty files remain unstaged.

```powershell
git add -- Docs/Verification/2026-08-03-wave-role-shield-ui-clarity.md
git commit -m "docs: verify readable waves shield and growth UI"
git push origin master
```

---

## Completion Gate

- Every new behavior test was observed failing for the intended missing behavior before implementation.
- All focused and full suites report zero failures.
- Shield durability changes once per confirmed unique hit, never on rejected duplicate contact.
- Normal and special enemy timing matches the approved 180-second schedule.
- No normal spawn uses `sakkat_specter` or `bandit` in this run.
- Legacy cards are opaque; shield body is stable; appraisal copy and geometry match the approved Korean design.
- The 140-target performance gate keeps 0 B measured allocation and the active cap.
- Android ARM64 IL2CPP development build succeeds.
- All task commits and final documentation are pushed to `origin/master`.
