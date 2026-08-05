# Frost Flask and Gakgung Combat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Frost Flask land quickly as a readable control field and make Gakgung a powerful long-range weapon that only acquires targets visible in the gameplay camera.

**Architecture:** Keep weapon authority in the existing `WeaponRuntimeController` and weapon executors. Add one cached visibility predicate from `FirstPlayableController` to the runtime, consume it only when Gakgung acquires a target, and keep authored balance values in `GakgungShot.asset`. Separate Frost Flask's fixed lob duration from its authored field duration at the existing factory call.

**Tech Stack:** Unity 6000.5.5f1, C#, Unity Test Framework/NUnit, ScriptableObject weapon data

## Global Constraints

- Gakgung only acquires living targets whose center is inside the current gameplay camera viewport and within its authored range.
- Gakgung priority is boss, elite, threat, distance, then runtime ID.
- Gakgung damage is `15 / 19 / 24 / 30 / 38`.
- Gakgung range is `22 / 24 / 26 / 28 / 30`.
- Gakgung speed is `26 / 28 / 30 / 32 / 34`.
- Gakgung cooldown is `0.72 / 0.69 / 0.66 / 0.63 / 0.60` seconds.
- Frost Flask lob duration is fixed at `0.4` seconds while field duration remains `1.4 / 1.6 / 1.8 / 2.0 / 2.2` seconds.
- No new images, packages, scene saves, prefab saves, splash damage, or homing behavior.
- Preserve unrelated dirty files and run Unity processes sequentially at BelowNormal priority with processor affinity mask `15`.

---

### Task 1: Add the camera-visibility boundary

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponRuntimeController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Produces: `WeaponRuntimeController.SetTargetVisibilityResolver(Func<Float2, bool> resolver)`
- Produces: `WeaponRuntimeController.IsTargetVisible(Float2 position) : bool`
- Consumes: `FirstPlayableController.gameplayCamera`

- [ ] **Step 1: Write the failing visibility-boundary test**

Add a test proving that an unset resolver accepts a point and a configured resolver can reject it:

```csharp
[Test]
public void WeaponRuntimeTargetVisibilityDefaultsVisibleAndUsesConfiguredResolver()
{
    var mask = PixelHitMask.FromRows("1");
    var registry = new CombatTargetRegistry();
    var runtime = new WeaponRuntimeController(registry, new CombatDamageService(registry), mask);
    Assert.That(runtime.IsTargetVisible(new Float2(12f, 0f)), Is.True);
    runtime.SetTargetVisibilityResolver(position => position.X <= 5f);
    Assert.That(runtime.IsTargetVisible(new Float2(4f, 0f)), Is.True);
    Assert.That(runtime.IsTargetVisible(new Float2(12f, 0f)), Is.False);
}
```

- [ ] **Step 2: Run the focused EditMode test and verify RED**

Run `WeaponMechanicTests` through Unity EditMode. Expected: compilation failure because the two visibility methods do not exist.

- [ ] **Step 3: Implement the minimal runtime boundary**

Add a cached `Func<Float2, bool>` to `WeaponRuntimeController`, default visible behavior, setter, and disposal cleanup:

```csharp
private Func<Float2, bool> targetVisibilityResolver;
public void SetTargetVisibilityResolver(Func<Float2, bool> resolver) => targetVisibilityResolver = resolver;
public bool IsTargetVisible(Float2 position) => targetVisibilityResolver?.Invoke(position) ?? true;
```

In both `FirstPlayableController` runtime creation paths, call:

```csharp
weaponRuntime.SetTargetVisibilityResolver(IsInsideGameplayViewport);
```

Implement allocation-free point testing:

```csharp
private bool IsInsideGameplayViewport(Float2 position)
{
    if (gameplayCamera == null) return true;
    var viewport = gameplayCamera.WorldToViewportPoint(new Vector3(position.X, position.Y, 0f));
    return viewport.z >= 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
}
```

- [ ] **Step 4: Run the focused EditMode test and verify GREEN**

Expected: the visibility-boundary test passes with no new compile errors.

### Task 2: Restrict and prioritize Gakgung acquisition

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`

**Interfaces:**
- Consumes: `WeaponRuntimeController.IsTargetVisible(Float2 position)`
- Preserves: existing launch, pierce, legacy, potential, and damage-confirmation behavior

- [ ] **Step 1: Write failing Gakgung acquisition tests**

Add these separate tests:

```csharp
[Test]
public void GakgungIgnoresInvisibleBossAndSelectsVisibleTarget()
{
    var mask = PixelHitMask.FromRows("1");
    var registry = new CombatTargetRegistry();
    var runtime = new WeaponRuntimeController(registry, new CombatDamageService(registry), mask);
    runtime.SetTargetVisibilityResolver(position => position.X <= 5f);
    var bow = new GakgungExecutor(runtime, 15f, .72f, 22f, 26f, 1);
    registry.Register(new TestTarget(1, new Float2(8f, 0f), mask, isBoss: true));
    registry.Register(new TestTarget(2, new Float2(3f, 0f), mask));

    bow.Tick(.01f, new WeaponExecutionContext(default, root.transform, null, 0, 1));

    Assert.That(bow.LastSelectedTargetRuntimeId, Is.EqualTo(2));
}

[Test]
public void GakgungChoosesNearestTargetWhenRankAndThreatMatch()
{
    var mask = PixelHitMask.FromRows("1");
    var registry = new CombatTargetRegistry();
    var runtime = new WeaponRuntimeController(registry, new CombatDamageService(registry), mask);
    var bow = new GakgungExecutor(runtime, 15f, .72f, 22f, 26f, 1);
    registry.Register(new TestTarget(1, new Float2(6f, 0f), mask));
    registry.Register(new TestTarget(2, new Float2(2f, 0f), mask));

    bow.Tick(.01f, new WeaponExecutionContext(default, root.transform, null, 0, 1));

    Assert.That(bow.LastSelectedTargetRuntimeId, Is.EqualTo(2));
}

[Test]
public void GakgungDoesNotAcquireTargetBeyondAuthoredRange()
{
    var mask = PixelHitMask.FromRows("1");
    var registry = new CombatTargetRegistry();
    var runtime = new WeaponRuntimeController(registry, new CombatDamageService(registry), mask);
    var bow = new GakgungExecutor(runtime, 15f, .72f, 22f, 26f, 1);
    registry.Register(new TestTarget(1, new Float2(23f, 0f), mask, isBoss: true));
    registry.Register(new TestTarget(2, new Float2(3f, 0f), mask));

    bow.Tick(.01f, new WeaponExecutionContext(default, root.transform, null, 0, 1));

    Assert.That(bow.LastSelectedTargetRuntimeId, Is.EqualTo(2));
}
```

- [ ] **Step 2: Run the focused tests and verify RED**

Expected: Gakgung selects the invisible, oldest, or out-of-range target under the old global priority logic.

- [ ] **Step 3: Implement filtered acquisition and deterministic distance tie-break**

Pass `context.OwnerPosition` into `TrySelectTarget`, filter dead, invisible, and out-of-range candidates, and compare equal rank/threat targets by squared distance before runtime ID.

- [ ] **Step 4: Run the focused tests and verify GREEN**

Expected: all new Gakgung acquisition tests and existing `WeaponMechanicTests` pass.

### Task 3: Author the Gakgung sniper balance

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`
- Modify: `Assets/JoseonHunter/Content/Weapons/GakgungShot.asset`

**Interfaces:**
- Consumes: existing `WeaponDefinitionAsset.Levels`
- Produces: the exact five-level damage, cooldown, range, and speed arrays in the approved design

- [ ] **Step 1: Write the failing content contract test**

Load `GakgungShot.asset` and assert the five levels exactly equal:

```csharp
Assert.That(levels.Select(level => level.BaseDamage), Is.EqualTo(new[] { 15f, 19f, 24f, 30f, 38f }));
Assert.That(levels.Select(level => level.CooldownSeconds), Is.EqualTo(new[] { .72f, .69f, .66f, .63f, .60f }));
Assert.That(levels.Select(level => level.Range), Is.EqualTo(new[] { 22f, 24f, 26f, 28f, 30f }));
Assert.That(levels.Select(level => level.Speed), Is.EqualTo(new[] { 26f, 28f, 30f, 32f, 34f }));
```

- [ ] **Step 2: Run the focused content test and verify RED**

Expected: old damage, cooldown, range, and speed arrays differ.

- [ ] **Step 3: Update only the four approved fields in `GakgungShot.asset`**

Preserve IDs, GUIDs, sprites, pierce, projectile counts, duration, and all other serialized data.

- [ ] **Step 4: Run `WeaponContentTests` and verify GREEN**

Expected: all weapon content contracts pass.

### Task 4: Separate Frost Flask flight and field duration

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Preserves: `FrostFlaskExecutor` constructor and authored `DurationSeconds`
- Produces: factory wiring with lob duration `0.4f`

- [ ] **Step 1: Extend the existing frost factory test**

In `RebuildWeaponExecutors_FrostUsesAuthoredSlowFraction`, also assert:

```csharp
Assert.That(executor.LobDuration, Is.EqualTo(.4f).Within(.001f));
Assert.That(executor.Duration, Is.EqualTo(1.4f).Within(.001f));
```

- [ ] **Step 2: Run the focused test and verify RED**

Expected: `LobDuration` is `1.4`, not `0.4`.

- [ ] **Step 3: Change the Frost Flask factory call**

Pass `0.4f` as `lobDuration` and keep `data.DurationSeconds` as `duration`.

- [ ] **Step 4: Run frost and weapon content tests and verify GREEN**

Expected: fixed factory test passes; landing burst, field ticks, slow, freeze, cleanup, and capacity tests remain green.

### Task 5: Integration validation and delivery

**Files:**
- Review all files changed in Tasks 1-4

**Interfaces:**
- Validates: camera visibility integration, executor behavior, ScriptableObject data, and regression safety

- [ ] **Step 1: Run focused EditMode suites**

Run `WeaponMechanicTests`, `WeaponContentTests`, and relevant target-selection tests. Expected: zero failures.

- [ ] **Step 2: Run focused PlayMode combat suites**

Run `EightWeaponCombatPlayModeTests`, `HwandoGakgungLegacyPlayModeTests`, and weapon polish/runtime tests. Expected: zero failures.

- [ ] **Step 3: Run the full EditMode suite**

Expected: zero failures.

- [ ] **Step 4: Run the full PlayMode suite**

Expected: zero failures.

- [ ] **Step 5: Inspect the diff and serialized asset safety**

Confirm only the approved Gakgung fields changed in its asset, no scene/prefab/project settings are staged, no temporary diagnostics remain, and unrelated dirty files are untouched.

- [ ] **Step 6: Commit and push**

Stage only plan, tests, the two runtime code paths, and `GakgungShot.asset`. Commit as `fix: clarify frost and gakgung combat roles`, push `master`, and verify local and remote hashes match.
