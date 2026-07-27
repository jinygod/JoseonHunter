# JoseonHunter First Playable Run Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a complete, repeatable 60-second Unity test run with movement, enemy pursuit, automatic hwando combat, XP upgrades, geumjul sealing, a 50-second boss, results, and retry, while representing the production 5/10/15-minute boss schedule as data.

**Architecture:** Existing pure Domain rules remain authoritative for time, damage, XP, upgrades, and geumjul geometry. Focused Runtime components own mutable Unity session state, pooling, actors, and combat; Presentation owns input and HUD feedback; an Editor generator creates all prefabs and scene wiring so serialized YAML is never hand-edited.

**Tech Stack:** Unity 6000.5.5f1, C# 9, URP 2D, Unity Input System, Unity Test Framework/NUnit, EditMode and PlayMode tests, Editor asset APIs.

## Global Constraints

- The first playable run lasts 60 seconds and spawns one test boss at 50 seconds after a warning.
- The production run defines bosses at 300, 600, and 900 seconds; the 900-second boss is the final and hardest boss.
- The production survival clock stops at 15:00 when the final boss appears; normal spawning stops and the final boss fight continues without extending the displayed survival timer.
- The player uses a front-facing static 64x64 sprite, procedural movement feedback, and horizontal flip only.
- No character attack animation is created; weapons and VFX communicate attacks.
- Gameplay terrain is a clean collider-free flat field.
- Desktop uses WASD and arrow keys; mobile uses a floating drag joystick through the same normalized movement path.
- Gameplay pauses while an upgrade offer is open and offers exactly three legal choices.
- Existing Domain assemblies remain free of Unity API references.
- No `record struct`, `IsExternalInit`, or BCL `IReadOnlySet`; the project compiles with Unity C# 9.
- Normal enemies, weapon effects, and pickups use pools; per-frame scene-wide searches are forbidden.
- Scene and prefab changes are made only through Editor APIs or official Unity MCP, never by hand-editing YAML.
- Existing user-created scenes, the untracked `Assets/character.unity`, generated ProjectSettings, and unrelated working-tree files are not staged or overwritten.
- Each task follows RED -> GREEN -> REFACTOR, receives an independent review, and ends with a focused commit.

---

### Task 1: Add Data-Driven Test And Production Run Profiles

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/RunProfile.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/RunSession.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/RunClock.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/RunProfileTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/RunRuleTests.cs`

**Interfaces:**
- Produces:
  `RunProfile.Test60Seconds() : RunProfile`.
- Produces:
  `RunProfile.Production15Minutes() : RunProfile`.
- Produces:
  `RunSession.Advance(float deltaSeconds) : RunTick`.
- Produces:
  `RunSession.MarkBossDefeated(string bossId) : RunOutcome`.
- `RunTick` exposes `ElapsedSeconds`, `RemainingSeconds`, `BossWarningId`,
  `BossSpawnId`, and `Outcome`.
- Later tasks consume boss IDs `fallen_general_test`, `boss_first`,
  `boss_second`, and `boss_final`.

- [ ] **Step 1: Write failing profile schedule tests**

```csharp
[Test]
public void TestProfileWarnsThenSpawnsBossAtFiftySeconds()
{
    var session = new RunSession(RunProfile.Test60Seconds());

    Assert.That(session.Advance(45f).BossWarningId, Is.EqualTo("fallen_general_test"));
    Assert.That(session.Advance(5f).BossSpawnId, Is.EqualTo("fallen_general_test"));
    Assert.That(session.Advance(1f).BossSpawnId, Is.Null);
}

[Test]
public void ProductionProfileDefinesIncreasingFiveTenFifteenMinuteBosses()
{
    var profile = RunProfile.Production15Minutes();

    Assert.That(profile.Bosses.Select(value => value.SpawnSeconds),
        Is.EqualTo(new[] { 300f, 600f, 900f }));
    Assert.That(profile.Bosses.Select(value => value.DifficultyTier),
        Is.EqualTo(new[] { 1, 2, 3 }));
    Assert.That(profile.Bosses[2].IsFinal, Is.True);
}
```

- [ ] **Step 2: Run the focused tests and confirm RED**

Run:

```powershell
& Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.RunProfileTests
```

Expected: compilation fails because `RunProfile` and `RunSession` do not exist.

- [ ] **Step 3: Implement immutable profile values**

Use C# 9-compatible readonly structs/classes:

```csharp
public readonly struct BossScheduleEntry
{
    public BossScheduleEntry(string bossId, float warningSeconds,
        float spawnSeconds, int difficultyTier, bool isFinal) { ... }

    public string BossId { get; }
    public float WarningSeconds { get; }
    public float SpawnSeconds { get; }
    public int DifficultyTier { get; }
    public bool IsFinal { get; }
}

public sealed class RunProfile
{
    public float DurationSeconds { get; }
    public IReadOnlyList<BossScheduleEntry> Bosses { get; }

    public static RunProfile Test60Seconds() => new RunProfile(
        60f,
        new[] { new BossScheduleEntry(
            "fallen_general_test", 45f, 50f, 1, true) });

    public static RunProfile Production15Minutes() => new RunProfile(
        900f,
        new[]
        {
            new BossScheduleEntry("boss_first", 285f, 300f, 1, false),
            new BossScheduleEntry("boss_second", 585f, 600f, 2, false),
            new BossScheduleEntry("boss_final", 885f, 900f, 3, true)
        });
}
```

Validate finite positive duration, sorted schedules, unique boss IDs, warning
before spawn, spawn not after duration, strictly increasing difficulty, and
exactly one final boss.

- [ ] **Step 4: Implement deterministic session transitions**

`Advance` emits each warning/spawn once. Timeout at 60 seconds returns
`RunOutcome.DefeatTimeout` if the test boss remains alive. In production,
elapsed survival time clamps at 900 seconds after the final boss spawns and
normal waves stop; the final fight itself is untimed. Defeating a non-final
production boss continues the run; defeating the final boss returns
`RunOutcome.Victory`.

- [ ] **Step 5: Add boundary and invalid-input tests**

Cover:

```text
negative delta clamps to zero
NaN/infinity throws before mutation
warning and spawn emit once
test timeout defeats
death defeats immediately
non-final boss defeat continues
final boss defeat wins
duplicate or unsorted profiles are rejected
```

- [ ] **Step 6: Run focused and full EditMode tests**

Run:

```powershell
& Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.RunProfileTests
& Tools/Unity/Test-Unity.ps1
```

Expected: both commands exit 0 with all tests passed.

- [ ] **Step 7: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Runs `
  Assets/JoseonHunter/Tests/EditMode/RunProfileTests.cs `
  Assets/JoseonHunter/Tests/EditMode/RunRuleTests.cs
git commit -m "feat: add configurable playable run profiles"
```

---

### Task 2: Implement Player Movement, Health, Enemy Pursuit, And Contact Damage

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/MovementInputState.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/HealthState.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ContactDamageGate.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/PlayerActor.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyActor.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ActiveEnemyRegistry.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/KeyboardMovementInput.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/ActorRuntimeRuleTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/ActorMovementPlayModeTests.cs`

**Interfaces:**
- Produces:
  `MovementInputState.Set(Vector2 rawInput)`.
- Produces:
  `MovementInputState.Value : Vector2`, clamped to magnitude 1.
- Produces:
  `PlayerActor.Configure(MovementInputState input, float speed,
  HealthState health)`.
- Produces:
  `EnemyActor.Configure(PlayerActor target, EnemyRuntimeDefinition definition,
  ActiveEnemyRegistry registry)`.
- Produces:
  `ActiveEnemyRegistry.FindNearest(Vector2 origin, float maxRange) : EnemyActor`.
- `PlayerActor` exposes `Position`, `Velocity`, `Health`, `IsDead`, and
  `Died`.

- [ ] **Step 1: Write failing movement and health rule tests**

```csharp
[Test]
public void DiagonalInputIsClampedAndReleaseStopsImmediately()
{
    var input = new MovementInputState();
    input.Set(new Vector2(1f, 1f));
    Assert.That(input.Value.magnitude, Is.EqualTo(1f).Within(0.0001f));

    input.Set(Vector2.zero);
    Assert.That(input.Value, Is.EqualTo(Vector2.zero));
}

[Test]
public void ContactDamageUsesCooldownInsteadOfEveryFrame()
{
    var gate = new ContactDamageGate(0.5f);
    Assert.That(gate.TryConsume(0f), Is.True);
    Assert.That(gate.TryConsume(0.1f), Is.False);
    Assert.That(gate.TryConsume(0.5f), Is.True);
}
```

- [ ] **Step 2: Run tests and confirm RED**

Run:

```powershell
& Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.ActorRuntimeRuleTests
```

Expected: compilation fails for missing runtime types.

- [ ] **Step 3: Implement pure runtime state**

Reject non-finite vectors, speeds, health, damage, and timestamps. `HealthState`
applies integer damage, never falls below zero, and fires death once.
`ContactDamageGate` accepts the first hit and then one hit per configured
cooldown.

- [ ] **Step 4: Implement actors without scene searches**

`PlayerActor.Update` reads only injected `MovementInputState`, moves with
`Time.deltaTime`, clamps to configured rectangular play bounds, and forwards
velocity to `StaticSpriteMotionPresenter`.

`EnemyActor.Update` pursues its injected player and registers/unregisters with
`ActiveEnemyRegistry` in `OnEnable`/`OnDisable`. It resets health, cooldown,
target, and subscriptions when returned to a pool.

- [ ] **Step 5: Write and run PlayMode actor tests**

```csharp
[UnityTest]
public IEnumerator KeyboardEquivalentInputMovesAndFlipsPlayer()
{
    var fixture = ActorFixture.Create();
    fixture.Input.Set(Vector2.left);
    yield return null;

    Assert.That(fixture.Player.transform.position.x, Is.LessThan(0f));
    Assert.That(fixture.Sprite.flipX, Is.True);
}

[UnityTest]
public IEnumerator EnemyPursuesAndDamagesAtCooldownIntervals()
{
    var fixture = ActorFixture.CreateWithEnemy();
    yield return new WaitForSeconds(0.6f);

    Assert.That(fixture.Enemy.DistanceToPlayer,
        Is.LessThan(fixture.InitialDistance));
    Assert.That(fixture.Player.Health.Current, Is.LessThan(fixture.Player.Health.Maximum));
}
```

Use created test GameObjects and destroy them in `TearDown`. Do not save a
scene in this task.

- [ ] **Step 6: Run actor-focused EditMode and PlayMode tests**

Run EditMode with `Tools/Unity/Test-Unity.ps1`. Run PlayMode directly:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath (Get-Location) `
  -runTests -testPlatform playmode `
  -testFilter JoseonHunter.Tests.PlayMode.ActorMovementPlayModeTests `
  -testResults Logs/actor-playmode-results.xml `
  -logFile Logs/actor-playmode.log
```

Expected: both suites pass and the Console contains no new errors.

- [ ] **Step 7: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay `
  Assets/JoseonHunter/Scripts/Presentation/Gameplay `
  Assets/JoseonHunter/Tests/EditMode/ActorRuntimeRuleTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/ActorMovementPlayModeTests.cs
git commit -m "feat: add playable actor movement and contact damage"
```

---

### Task 3: Add Pooled Enemy Spawning And Automatic Hwando Combat

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Pooling/ComponentPool.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySpawnDirector.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/HwandoController.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/HwandoStrike.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyRuntimeDefinition.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/CombatRuntimeRuleTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/PooledCombatPlayModeTests.cs`

**Interfaces:**
- Consumes:
  `ActiveEnemyRegistry.FindNearest`.
- Produces:
  `ComponentPool<T>.Rent() : T` and `Return(T item)`.
- Produces:
  `EnemySpawnDirector.Configure(PlayerActor player, ComponentPool<EnemyActor>
  pool, SpawnProfile profile)`.
- Produces:
  `HwandoController.Configure(PlayerActor owner, ActiveEnemyRegistry registry,
  ComponentPool<HwandoStrike> pool, HwandoRuntimeDefinition definition)`.
- `HwandoRuntimeDefinition` contains `Damage`, `Range`, `IntervalSeconds`,
  `StrikeLifetimeSeconds`, and `MaximumSimultaneousStrikes`.

- [ ] **Step 1: Write failing deterministic targeting and cap tests**

```csharp
[Test]
public void NearestTargetUsesStableRuntimeIdForDistanceTies()
{
    var registry = EnemyRegistryFixture.WithEnemies(
        (8, new Vector2(-1f, 0f)),
        (2, new Vector2(1f, 0f)));

    Assert.That(registry.FindNearest(Vector2.zero, 2f).RuntimeId, Is.EqualTo(2));
}

[Test]
public void SpawnProfileCapsNormalEnemiesAtEighty()
{
    var profile = SpawnProfile.Test60Seconds();
    Assert.That(profile.MaximumActiveNormalEnemies, Is.EqualTo(80));
}
```

- [ ] **Step 2: Run tests and confirm RED**

Expected: missing registry targeting/spawn/weapon definitions.

- [ ] **Step 3: Implement bounded resettable pools**

`ComponentPool<T>` prewarms, rents inactive entries, grows only to a configured
maximum, and returns `null` when exhausted. `IPoolable.OnRent` and
`IPoolable.OnReturn` reset subscriptions, timers, health, velocity, and visuals.

- [ ] **Step 4: Implement edge spawning and pursuit population**

Spawn positions are selected outside the camera view along one of four edges,
using a seeded `System.Random`. The 60-second profile interpolates spawn
interval and enemy speed while respecting the 80-enemy cap. Pool exhaustion
skips the spawn without throwing.

- [ ] **Step 5: Implement automatic hwando**

Every configured interval, target the nearest valid enemy in range. Rent a
visible strike, move or sweep it to the target, apply one `DamageRequest`, and
return it after impact/lifetime. The player sprite does not animate an attack.

- [ ] **Step 6: Add PlayMode combat tests**

Cover:

```text
spawner never exceeds active cap
returned enemy is reset before reuse
nearest enemy receives configured damage
tie uses lowest stable runtime ID
no target produces no strike
pool exhaustion logs no error
defeated enemy unregisters exactly once
```

- [ ] **Step 7: Run focused and full tests, inspect Console, commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Pooling `
  Assets/JoseonHunter/Scripts/Runtime/Gameplay `
  Assets/JoseonHunter/Tests/EditMode/CombatRuntimeRuleTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/PooledCombatPlayModeTests.cs
git commit -m "feat: add pooled enemies and automatic hwando"
```

---

### Task 4: Connect XP Pickups, Coins, Level-Up Pause, And Three Choices

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/PickupActor.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/RunProgressionController.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/UpgradeApplication.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/UpgradeChoicePresenter.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/RunProgressionRuntimeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`

**Interfaces:**
- Consumes:
  existing `ExperienceCurve`, `UpgradeSelector`, `UpgradeState`, and pooled
  defeated-enemy events.
- Produces:
  `RunProgressionController.AddExperience(int amount)`.
- Produces:
  `RunProgressionController.AddCoins(int amount)`.
- Produces events:
  `UpgradeOffered(IReadOnlyList<UpgradeDefinition> offers)`,
  `LevelChanged(int level)`, `CoinsChanged(int coins)`, `ExperienceChanged`.
- Produces:
  `ChooseUpgrade(string upgradeId)` and rejects calls without an open offer.

- [ ] **Step 1: Write failing progression integration tests**

```csharp
[Test]
public void CrossingThresholdOpensExactlyThreeLegalOffersAndPausesRun()
{
    var controller = ProgressionFixture.Create(seed: 27);
    controller.AddExperience(ExperienceCurve.RequiredForLevel(2));

    Assert.That(controller.IsUpgradeOpen, Is.True);
    Assert.That(controller.CurrentOffers, Has.Count.EqualTo(3));
    Assert.That(controller.IsCombatPaused, Is.True);
}
```

- [ ] **Step 2: Verify RED**

Expected: `RunProgressionController` does not exist.

- [ ] **Step 3: Implement pickup and progression flow**

Normal enemies deterministically drop one XP flame. The configured coin rule
uses the session seed. Pickups magnetize only within pickup range and credit
once. Multiple queued levels are offered sequentially, never as fewer than
three choices.

- [ ] **Step 4: Implement upgrade application**

Launch upgrades modify only approved runtime values:

```text
hwando damage
hwando interval
movement speed
maximum health
pickup radius
geumjul base damage
```

Validate min/max caps, copy state before mutation, and resume combat only after
a valid choice.

- [ ] **Step 5: Implement presentation and PlayMode tests**

`UpgradeChoicePresenter` renders three buttons from current offers, accepts one
selection, disables duplicate taps, then closes. Tests verify time scale/session
pause, input lock, one choice, resume, and queued second level.

- [ ] **Step 6: Run focused EditMode/PlayMode and full EditMode tests**

Expected: all pass, no new errors.

- [ ] **Step 7: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay `
  Assets/JoseonHunter/Scripts/Presentation/Gameplay/UpgradeChoicePresenter.cs `
  Assets/JoseonHunter/Tests/EditMode/RunProgressionRuntimeTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs
git commit -m "feat: add playable pickups and upgrade choices"
```

---

### Task 5: Connect Player Trails To Geumjul Sealing

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GeumjulRuntimeController.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/GeumjulTrailPresenter.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GeumjulRuntimeIntegrationTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GeumjulPlayModeTests.cs`

**Interfaces:**
- Consumes:
  existing `GeumjulTrail`, `LoopDetector`, `SealResolver`,
  `ActiveEnemyRegistry`, and player world position.
- Produces event:
  `SealClosed(LoopResult loop, IReadOnlyList<SealHit> hits)`.
- Produces:
  `ResetForRun(MasteryState mastery)`.
- Presentation consumes the retained trail points and seal event without
  owning geometry decisions.

- [ ] **Step 1: Write failing world-to-domain integration tests**

```csharp
[Test]
public void ClosedPlayerPathDamagesOnlyContainedEnemiesInStableOrder()
{
    var fixture = GeumjulRuntimeFixture.SquareWithInsideAndOutsideEnemies();
    fixture.SampleSquareAndClose();

    Assert.That(fixture.DamagedTargetIds, Is.EqualTo(new[] { 2, 8 }));
    Assert.That(fixture.OutsideEnemy.Health, Is.EqualTo(fixture.OutsideEnemy.MaximumHealth));
}
```

- [ ] **Step 2: Verify RED**

Expected: runtime controller missing.

- [ ] **Step 3: Implement sampled trail and damage application**

Sample only after the player moves at least `1f / 32f` world units or after the
maximum sample interval. Convert Unity `Vector2` to Domain `Float2`, pass
monotonic session time, and never include map boundaries. Resolve active enemy
positions into stable `TargetPoint` values and apply each `SealHit` once.

- [ ] **Step 4: Implement trail presentation**

Use a pooled or bounded `LineRenderer`/sprite-segment presentation with warm
straw-yellow color. A completed loop flashes once, then resets. Presentation
is replaceable by approved PixelLab VFX later.

- [ ] **Step 5: Add PlayMode tests**

Cover valid closure, invalid short loop, expired trail, target on edge,
contained normal enemy bind, boss no-bind rule, visual reset, and no duplicate
damage on subsequent frames.

- [ ] **Step 6: Run focused/full tests and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/GeumjulRuntimeController.cs `
  Assets/JoseonHunter/Scripts/Presentation/Gameplay/GeumjulTrailPresenter.cs `
  Assets/JoseonHunter/Tests/EditMode/GeumjulRuntimeIntegrationTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/GeumjulPlayModeTests.cs
git commit -m "feat: connect playable geumjul sealing"
```

---

### Task 6: Add Test Boss, Results, Victory, Defeat, And Retry

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BossActor.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/RunSessionController.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/RunResetCoordinator.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/ResultPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/BossHudPresenter.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/RunSessionControllerTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/RunOutcomePlayModeTests.cs`

**Interfaces:**
- Consumes:
  `RunProfile`, `RunSession`, pools, player death, boss death, progression, and
  save repository boundary.
- Produces:
  `RunSessionController.StartRun(RunProfile profile, int seed)`.
- Produces:
  `RunSessionController.Retry()`.
- Produces events:
  `BossWarning`, `BossSpawned`, `RunEnded`, `RunReset`.
- `RunResult` contains outcome, elapsed seconds, kills, level, coins, and boss
  defeats.

- [ ] **Step 1: Write failing orchestration tests**

```csharp
[Test]
public void TestBossSpawnsOnceAndDefeatWins()
{
    var fixture = SessionFixture.TestProfile();
    fixture.AdvanceTo(50f);
    Assert.That(fixture.SpawnedBossIds, Is.EqualTo(new[] { "fallen_general_test" }));

    fixture.DefeatCurrentBoss();
    Assert.That(fixture.Result.Outcome, Is.EqualTo(RunOutcome.Victory));
}

[Test]
public void RetryClearsActorsPickupsOffersAndSubscriptions()
{
    var fixture = SessionFixture.CompletedRun();
    fixture.Controller.Retry();

    Assert.That(fixture.ActiveEnemyCount, Is.Zero);
    Assert.That(fixture.ActivePickupCount, Is.Zero);
    Assert.That(fixture.UpgradeIsOpen, Is.False);
    Assert.That(fixture.RunStartedEventCount, Is.EqualTo(2));
}
```

- [ ] **Step 2: Verify RED**

Expected: orchestration types missing.

- [ ] **Step 3: Implement one-owner session state machine**

States are:

```text
Idle -> Running <-> UpgradePaused -> Results -> Resetting -> Running
```

Player death produces `DefeatDeath`. Test timeout produces `DefeatTimeout`.
Test boss death produces `Victory`. Results freeze gameplay input and spawning
but keep UI responsive.

- [ ] **Step 4: Implement boss and HUD**

The test Fallen General uses static sprite motion, higher health, larger
collision radius, increased contact damage, and a boss health bar. Complex
charge/cone/summon/enrage patterns are excluded from this plan.

- [ ] **Step 5: Implement retry and save-warning behavior**

Return all pooled objects, clear registries and event handlers, reset player,
progression, geumjul, clock, random seed, HUD, and input, then start a fresh
test run. Save results through `ISaveRepository`; failure shows a non-blocking
warning and retry remains available.

- [ ] **Step 6: Run focused/full tests and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay `
  Assets/JoseonHunter/Scripts/Presentation/Gameplay `
  Assets/JoseonHunter/Tests/EditMode/RunSessionControllerTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/RunOutcomePlayModeTests.cs
git commit -m "feat: complete test boss results and retry loop"
```

---

### Task 7: Generate The Playable Scene, Mobile HUD, And End-To-End Validation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/FloatingJoystick.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/GameplayHudPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/SafeAreaRoot.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- Modify through Editor API: `Assets/JoseonHunter/Scenes/Gameplay.unity`
- Create through Editor API:
  `Assets/JoseonHunter/Prefabs/Gameplay/Player.prefab`
- Create through Editor API:
  `Assets/JoseonHunter/Prefabs/Gameplay/EnemyPlagueRat.prefab`
- Create through Editor API:
  `Assets/JoseonHunter/Prefabs/Gameplay/EnemyBandit.prefab`
- Create through Editor API:
  `Assets/JoseonHunter/Prefabs/Gameplay/FallenGeneralTest.prefab`
- Create through Editor API:
  `Assets/JoseonHunter/Prefabs/Gameplay/ExperiencePickup.prefab`
- Create through Editor API:
  `Assets/JoseonHunter/Prefabs/Gameplay/CoinPickup.prefab`
- Create through Editor API:
  `Assets/JoseonHunter/Prefabs/Gameplay/HwandoStrike.prefab`
- Create: `Assets/JoseonHunter/Tests/EditMode/FirstPlayableSceneContractTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableEndToEndTests.cs`
- Create: `Tools/Unity/Test-PlayMode.ps1`
- Create: `Docs/Verification/2026-07-27-first-playable-run.md`

**Interfaces:**
- Consumes all Tasks 1-6.
- Produces Editor menu/CLI method:
  `FirstPlayableSceneGenerator.Generate()`.
- Produces a no-setup `Gameplay` scene that starts the 60-second test profile.
- Produces HUD fields for health, XP, level, time, coins, boss warning, boss
  health, upgrade choices, results, retry, and save warning.

- [ ] **Step 1: Write failing scene contract tests**

Assert through `AssetDatabase`, prefab loading, and scene inspection:

```text
Gameplay scene has one orthographic camera
scene has one RunSessionController
player prefab references approved rookie constable sprite
enemy prefabs reference approved static sprites
all pooled prefab references are non-null
HUD is under SafeAreaRoot
floating joystick is inside lower safe area
static proof group is absent or inactive
no missing MonoBehaviour scripts
no building/wall/tree/collider terrain
```

- [ ] **Step 2: Run scene tests and confirm RED**

Expected: generated hierarchy and prefabs do not exist.

- [ ] **Step 3: Implement floating joystick and HUD**

`FloatingJoystick` implements pointer down/drag/up, follows the initial touch
within the configured lower safe region, clamps its handle radius, and writes
to the shared `MovementInputState`. Keyboard input and joystick input are
merged by maximum current magnitude, never added beyond 1.

`GameplayHudPresenter` subscribes in `OnEnable`, unsubscribes in `OnDisable`,
and performs event-driven updates rather than polling string values every frame.

- [ ] **Step 4: Implement the Editor generator**

Use `PrefabUtility`, `SerializedObject`, `EditorSceneManager`, and
`AssetDatabase`. Create directories if absent, generate prefabs, assign sprites
by exact asset path, create the flat field/camera/HUD/session hierarchy, set
references, save assets, and re-open them for validation. The method is
idempotent: a second run produces no duplicate roots or components.

- [ ] **Step 5: Generate prefabs and scene**

Run the generator through official Unity MCP or a batch Editor entry point.
Do not run it while another Unity instance owns the same project path. Re-open
the scene and inspect all serialized references.

- [ ] **Step 6: Add the PlayMode test script**

`Tools/Unity/Test-PlayMode.ps1` mirrors `Test-Unity.ps1`, uses
`-testPlatform playmode`, writes `Logs/playmode-results.xml`, and forwards an
optional filter.

- [ ] **Step 7: Add accelerated end-to-end tests**

Tests inject an accelerated/manual test clock rather than waiting 60 real
seconds:

```text
scene loads without missing references
input moves and flips player
normal enemies spawn and pursue
hwando defeats at least one enemy
XP pickup opens three choices and a choice resumes
geumjul closure damages a contained enemy
warning occurs before boss
boss spawns at scheduled time
boss defeat opens victory results
player death opens defeat results
retry returns to a clean running session
```

- [ ] **Step 8: Run complete validation**

```powershell
& Tools/Unity/Test-Unity.ps1
& Tools/Unity/Test-PlayMode.ps1
```

Inspect `Logs/editmode-results.xml`, `Logs/playmode-results.xml`, and the Unity
Console. Expected: zero failures and zero new errors.

- [ ] **Step 9: Perform manual Editor play check**

Open `Bootstrap` and `Gameplay` independently. Verify keyboard movement,
joystick drag with mouse, portrait framing, combat readability, upgrade input,
geumjul, boss, results, and retry. Capture one gameplay screenshot and record
observed FPS/object counts in the verification document.

- [ ] **Step 10: Final diff review**

Confirm:

```text
no unrelated user scene or ProjectSettings changes staged
no hand-edited scene/prefab YAML
no per-frame Find APIs
all poolable objects reset
all event subscriptions balance
test and production schedules are both covered
```

- [ ] **Step 11: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/Gameplay `
  Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs `
  Assets/JoseonHunter/Scenes/Gameplay.unity `
  Assets/JoseonHunter/Prefabs/Gameplay `
  Assets/JoseonHunter/Tests/EditMode/FirstPlayableSceneContractTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/FirstPlayableEndToEndTests.cs `
  Tools/Unity/Test-PlayMode.ps1 `
  Docs/Verification/2026-07-27-first-playable-run.md
git commit -m "feat: deliver first playable test run"
```

---

## Completion Gate

The plan is complete only when:

- all seven task reviews approve;
- full EditMode and PlayMode suites pass on the integrated commit;
- Unity Console has no new errors;
- the user can press Play and complete or lose the 60-second loop without
  Inspector setup;
- the production 5/10/15-minute schedule is present and tested;
- a gameplay screenshot and verification record exist;
- unrelated user files remain untouched.
