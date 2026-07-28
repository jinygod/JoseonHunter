# Eight Weapon Evolution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add unlockable final evolutions for all eight weapons so each retains its original identity while changing at least two dimensions of attack rhythm, geometry, target response, or payoff.

**Architecture:** Domain progression owns evolution eligibility and acquisition by weapon ID. Runtime weapon construction selects a normal or evolved execution profile without changing slots or resetting levels. Each executor keeps confirmed pixel contact as the only damage gateway, while presentation consumes evolution and damage events for visuals.

**Tech Stack:** Unity 6000.5.5f1, C# 9, existing weapon executor framework, pixel hit masks, NUnit EditMode/PlayMode tests

## Global Constraints

- Evolutions occur only after the matching weapon reaches level 5 and its evolution ID is unlocked.
- Evolution does not create a ninth slot or reset the weapon's accumulated level.
- Every evolved hit still goes through `CombatDamageService.TryApply` with confirmed pixel contact.
- Each evolution changes at least two of rhythm, path/geometry, enemy response, or payoff.
- Evolution presentation may reach intensity 100 only for reveal and the first evolved cast; later casts return to combat intensity 70–80.
- Existing icon and representative sprites are reused. PixelLab is used only when a missing evolved silhouette or secondary pixel part cannot be composed from existing assets.
- Preserve unrelated dirty `.meta`, scene, character, VFX, and `ProjectSettings` files.

---

## File Structure

- Create `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponEvolutionCatalog.cs`: all eight evolution IDs, requirements, display data.
- Modify `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`: offer eligible evolutions with priority.
- Modify `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`: persist acquired evolution IDs, apply evolution offers, rebuild evolved executors.
- Modify all eight files under `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/`: accept and execute evolved profiles.
- Create `Assets/JoseonHunter/Tests/EditMode/WeaponEvolutionCatalogTests.cs`
- Create `Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs`
- Create `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`
- Create `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponTestRig.cs`: shared registry, targets, tick loop, and evolution telemetry for the eight focused tests.

---

### Task 1: Define all evolution IDs and selection rules

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponEvolutionCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponEvolutionCatalogTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs`

**Interfaces:**
- Produces: `WeaponEvolutionDefinition`
- Produces: `WeaponEvolutionCatalog.All`, `TryGet(string id, out WeaponEvolutionDefinition definition)`
- Consumes: `WeaponId`, `UpgradeState`

- [ ] **Step 1: Write catalog and eligibility tests**

```csharp
[Test]
public void Catalog_contains_one_evolution_for_every_weapon()
{
    Assert.That(WeaponEvolutionCatalog.All.Count, Is.EqualTo(WeaponRoster.All.Count));
    CollectionAssert.AreEquivalent(
        WeaponRoster.All.Select(id => id.Value),
        WeaponEvolutionCatalog.All.Select(value => value.RequiredWeaponId.Value));
}

[Test]
public void Max_level_unlocked_weapon_offers_its_evolution()
{
    var state = new UpgradeState(
        new Dictionary<string, int> { [WeaponId.FrostFlask.Value] = 5 },
        new Dictionary<string, int>(),
        new HashSet<string> { "frost_bloom_evolution" },
        new HashSet<string>());

    var offers = UpgradeSelector.Select(state, 27);
    Assert.That(offers, Has.Some.Matches<UpgradeOffer>(
        offer => offer.Kind == UpgradeKind.Evolution && offer.Id == "frost_bloom_evolution"));
}
```

- [ ] **Step 2: Add exact catalog entries**

```csharp
public static readonly IReadOnlyList<WeaponEvolutionDefinition> All =
    Array.AsReadOnly(new[]
    {
        new("hwando_moon_eclipse", WeaponId.HwandoFlyingBlade, "환도·월식", "귀환 교차점에 월식 폭발", EvolutionDimension.Geometry, EvolutionDimension.Payoff),
        new("gakgung_sun_piercer", WeaponId.GakgungShot, "각궁·관일", "일정 사격마다 거대 관통 화살", EvolutionDimension.Rhythm, EvolutionDimension.Payoff),
        new("talisman_heaven_chain", WeaponId.TalismanThrow, "천쇄부진", "연결된 봉인망 완성 시 동시 폭발", EvolutionDimension.Geometry, EvolutionDimension.Payoff),
        new("thunder_prison", WeaponId.ThunderCrashBomb, "벽력탄·뇌옥", "끌어모은 뒤 압축 낙뢰 폭발", EvolutionDimension.EnemyResponse, EvolutionDimension.Rhythm),
        new("twelve_guardians", WeaponId.JangseungWard, "십이지신 장승진", "완성된 진 안의 적을 낙인", EvolutionDimension.Geometry, EvolutionDimension.EnemyResponse),
        new("fire_dragon_barrage", WeaponId.SingijeonVolley, "신기전·화룡포", "표식 지점에 지연 집중 포격", EvolutionDimension.Rhythm, EvolutionDimension.Geometry),
        new("frost_bloom_evolution", WeaponId.FrostFlask, "서리병·빙화원", "축적한 빙결을 연쇄 파쇄", EvolutionDimension.EnemyResponse, EvolutionDimension.Payoff),
        new("returning_heaven_thunder", WeaponId.WindThunderFan, "풍뢰선·천뢰귀환", "모은 표식 사이를 낙뢰가 왕복", EvolutionDimension.Geometry, EvolutionDimension.Rhythm)
    });

public enum EvolutionDimension { Rhythm, Geometry, EnemyResponse, Payoff }

public sealed class WeaponEvolutionDefinition
{
    public WeaponEvolutionDefinition(
        string id, WeaponId requiredWeaponId, string displayName, string summary,
        params EvolutionDimension[] changedDimensions)
    {
        Id = id;
        RequiredWeaponId = requiredWeaponId;
        DisplayName = displayName;
        Summary = summary;
        ChangedDimensions = Array.AsReadOnly(changedDimensions.ToArray());
    }

    public string Id { get; }
    public WeaponId RequiredWeaponId { get; }
    public string DisplayName { get; }
    public string Summary { get; }
    public IReadOnlyList<EvolutionDimension> ChangedDimensions { get; }
}
```

- [ ] **Step 3: Prioritize an eligible evolution without breaking three-card uniqueness**

`UpgradeSelector.Select` must place one eligible unacquired evolution first, then preserve the existing owned-weapon and unowned-weapon guarantees while removing duplicate IDs.

```csharp
var evolutions = eligible.Where(offer => offer.Kind == UpgradeKind.Evolution).ToList();
if (evolutions.Count > 0)
{
    offers.Add(evolutions[random.Next(evolutions.Count)]);
    eligible.RemoveAll(offer => offers.Any(selected => selected.Id == offer.Id));
}
```

- [ ] **Step 4: Run tests and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.WeaponEvolutionCatalogTests' -testResults 'Temp\evolution-catalog.xml' -logFile 'Temp\evolution-catalog.log' -quit
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.UpgradeEvolutionTests' -testResults 'Temp\evolution-offers.xml' -logFile 'Temp\evolution-offers.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Domain/Progression/WeaponEvolutionCatalog.cs' 'Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs' 'Assets/JoseonHunter/Tests/EditMode/WeaponEvolutionCatalogTests.cs' 'Assets/JoseonHunter/Tests/EditMode/UpgradeEvolutionTests.cs'
git commit -m 'feat: define eight weapon evolutions'
```

---

### Task 2: Persist acquired evolutions and build evolved executors

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponEvolutionState.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponTestRig.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Produces: `WeaponEvolutionState.IsEvolved(WeaponId id)`
- Produces: `FirstPlayableController.AcquiredEvolutionIds`
- Consumes: `WeaponEvolutionCatalog`

- [ ] **Step 1: Write acquisition persistence test**

```csharp
[UnityTest]
public IEnumerator Choosing_evolution_keeps_weapon_level_and_rebuilds_evolved_executor()
{
    SceneManager.LoadScene("Gameplay");
    yield return null;
    var controller = Object.FindFirstObjectByType<FirstPlayableController>();
    controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
    controller.UnlockEvolutionForTests("hwando_moon_eclipse");
    controller.OpenUpgradeForTests(seed: 7);

    var index = controller.CurrentOffers
        .Select((offer, i) => (offer, i))
        .Single(pair => pair.offer.Id == "hwando_moon_eclipse").i;
    Assert.That(controller.TryChooseUpgrade(index), Is.True);

    Assert.That(controller.WeaponLevelForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(5));
    Assert.That(controller.AcquiredEvolutionIds, Contains.Item("hwando_moon_eclipse"));
    Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.True);
    yield return null;
}
```

- [ ] **Step 2: Add evolution state mapping**

```csharp
public sealed class WeaponEvolutionState
{
    private readonly HashSet<string> evolvedWeaponIds = new();

    public void SetEvolved(WeaponId weaponId) => evolvedWeaponIds.Add(weaponId.Value);
    public bool IsEvolved(WeaponId weaponId) => evolvedWeaponIds.Contains(weaponId.Value);
    public void Clear() => evolvedWeaponIds.Clear();
}
```

- [ ] **Step 3: Apply evolution offers in the controller**

```csharp
else if (offer.Kind == UpgradeKind.Evolution &&
         WeaponEvolutionCatalog.TryGet(offer.Id, out var evolution))
{
    acquiredEvolutionIds.Add(offer.Id);
    evolutionState.SetEvolved(evolution.RequiredWeaponId);
    RebuildWeaponExecutorsForLevel();
    reward = new ProgressionRewardEvent(
        offer.Id, evolution.RequiredWeaponId.Value, 5, ProgressionRewardKind.Evolution,
        evolution.DisplayName, evolution.Summary, ResolveWeaponSprite(evolution.RequiredWeaponId));
}
```

`ResetRun()` clears acquired evolutions and evolution state. `RegisterCatalogWeapons()` passes `evolutionState.IsEvolved(id)` into each executor constructor.
For the current first-playable build, `ResetRun()` also adds every
`WeaponEvolutionCatalog.All` ID to `unlockedUpgradeIds`; later meta progression
can replace this bootstrap without changing eligibility rules.

- [ ] **Step 4: Add the shared evolved-weapon test rig**

The rig owns the same real registry, damage service, runtime controller, and
1-pixel masks used by the production executors. It records confirmed events;
executor-specific public read-only telemetry is adapted into a single snapshot.

```csharp
internal sealed class EvolvedWeaponRig : IDisposable
{
    private readonly GameObject root = new("Evolved Weapon Test Root");
    private readonly CombatTargetRegistry registry = new();
    private readonly List<ConfirmedDamageEvent> events = new();
    private readonly List<TestTarget> targets = new();
    private readonly IWeaponExecutor executor;
    private int tick;

    private EvolvedWeaponRig(WeaponId id)
    {
        var mask = PixelHitMask.FromRows("1");
        Damage = new CombatDamageService(registry);
        Runtime = new WeaponRuntimeController(registry, Damage, mask);
        executor = EvolvedExecutorFactory.CreateForTests(id, Runtime);
        Runtime.Register(executor);
        Damage.DamageConfirmed += events.Add;
    }

    public static EvolvedWeaponRig For(WeaponId id, bool evolved = true)
    {
        if (!evolved) throw new ArgumentException("This rig is reserved for evolved profiles.", nameof(evolved));
        return new EvolvedWeaponRig(id);
    }
    public CombatDamageService Damage { get; }
    public WeaponRuntimeController Runtime { get; }
    public IReadOnlyList<ContactPhase> ContactPhases => events.Select(value => value.Phase).ToArray();
    public IReadOnlyList<ContactPhase> DistinctPhaseOrder => events.Select(value => value.Phase).Distinct().ToArray();
    public int Count(ContactPhase phase) => events.Count(value => value.Phase == phase);
    public int UniqueDamagedTargets => events.Select(value => value.TargetRuntimeId).Distinct().Count();
    public EvolutionTelemetry Telemetry => EvolvedExecutorFactory.ReadTelemetry(executor);

    public TestTarget AddTarget(Vector2 position)
    {
        var target = new TestTarget(targets.Count + 1, new Float2(position.x, position.y), PixelHitMask.FromRows("1"));
        targets.Add(target);
        registry.Register(target);
        return target;
    }

    public void AddTargets(int count)
    {
        for (var index = 0; index < count; index++)
            AddTarget(new Vector2(1f + index * 0.2f, 0f));
    }

    public void AddTargets(int count, bool insideField)
    {
        var distance = insideField ? 0.4f : 8f;
        for (var index = 0; index < count; index++)
            AddTarget(new Vector2(distance + index * 0.1f, 0f));
    }

    public IEnumerator AdvanceSeconds(float seconds)
    {
        var elapsed = 0f;
        while (elapsed < seconds)
        {
            const float delta = 0.05f;
            executor.Tick(delta, new WeaponExecutionContext(default, root.transform, null, 0, ++tick));
            elapsed += delta;
            yield return null;
        }
    }

    public IEnumerator AdvanceCasts(int count)
    {
        for (var index = 0; index < count; index++)
        {
            executor.Tick(0.01f, new WeaponExecutionContext(default, root.transform, null, 0, ++tick));
            executor.Tick(0.2f, new WeaponExecutionContext(default, root.transform, null, 0, ++tick));
            yield return null;
        }
    }

    public void Dispose()
    {
        Damage.DamageConfirmed -= events.Add;
        Runtime.Dispose();
        UnityEngine.Object.DestroyImmediate(root);
    }
}

internal readonly struct EvolutionTelemetry
{
    public EvolutionTelemetry(
        int lastProjectileMaximumImpacts, float lastProjectileScale,
        IReadOnlyList<string> stateOrder, IReadOnlyList<string> volleyKinds,
        int scoutProjectileCount, int focusProjectileCount, float fieldDuration,
        bool allStoredTargetsResolvedOnce)
    {
        LastProjectileMaximumImpacts = lastProjectileMaximumImpacts;
        LastProjectileScale = lastProjectileScale;
        StateOrder = stateOrder;
        VolleyKinds = volleyKinds;
        ScoutProjectileCount = scoutProjectileCount;
        FocusProjectileCount = focusProjectileCount;
        FieldDuration = fieldDuration;
        AllStoredTargetsResolvedOnce = allStoredTargetsResolvedOnce;
    }

    public int LastProjectileMaximumImpacts { get; }
    public float LastProjectileScale { get; }
    public IReadOnlyList<string> StateOrder { get; }
    public IReadOnlyList<string> VolleyKinds { get; }
    public int ScoutProjectileCount { get; }
    public int FocusProjectileCount { get; }
    public float FieldDuration { get; }
    public bool AllStoredTargetsResolvedOnce { get; }
}
```

`EvolvedExecutorFactory.CreateForTests` is added in
`WeaponEvolutionState.cs` under `#if UNITY_INCLUDE_TESTS`; it constructs the
requested executor at level 5 with `evolved: true` and deterministic common
values (`baseDamage: 10`, `cooldownSeconds: 0.1f`, `range: 4f`, `speed: 8f`).
`EvolvedExecutorFactory.ReadTelemetry(IWeaponExecutor executor)` switches on the
eight concrete executor types and returns their public read-only counters in the
`EvolutionTelemetry` fields defined above.
`TestTarget` implements `ICombatTarget`, `IFrostStatusTarget`, and
`IJangseungWardStatusTarget`, stores status strings in `Statuses`, and exposes
its mutable `Position`.

- [ ] **Step 5: Run the focused test and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EvolvedWeaponCombatPlayModeTests.Choosing_evolution_keeps_weapon_level_and_rebuilds_evolved_executor' -testResults 'Temp\evolution-state.xml' -logFile 'Temp\evolution-state.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs' 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponEvolutionState.cs' 'Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponTestRig.cs' 'Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs'
git commit -m 'feat: persist evolved weapon state'
```

---

### Task 3: Evolve 환도 and 각궁

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`

**Interfaces:**
- `FlyingBladeExecutor(..., int projectileCount, bool evolved)`
- `GakgungExecutor(..., int level, bool evolved)`
- Produces confirmed `ContactPhase.Return` and `ContactPhase.Blast` for 월식
- Produces a periodic high-pierce 관일 shot

- [ ] **Step 1: Add paired behavior tests**

```csharp
[UnityTest]
public IEnumerator Moon_eclipse_keeps_outbound_and_return_contact_then_blasts_at_crossing()
{
    var rig = EvolvedWeaponRig.For(WeaponId.HwandoFlyingBlade, evolved: true);
    rig.AddTarget(new Vector2(2f, 0f));
    rig.AddTarget(new Vector2(0.2f, 0f));
    yield return rig.AdvanceSeconds(2f);

    CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Direct);
    CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Inbound);
    CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Blast);
}

[UnityTest]
public IEnumerator Sun_piercer_fires_one_high_pierce_shot_on_cadence()
{
    var rig = EvolvedWeaponRig.For(WeaponId.GakgungShot, evolved: true);
    yield return rig.AdvanceCasts(4);
    Assert.That(rig.Telemetry.LastProjectileMaximumImpacts, Is.GreaterThanOrEqualTo(6));
    Assert.That(rig.Telemetry.LastProjectileScale, Is.GreaterThan(1f));
}
```

- [ ] **Step 2: Implement 월식 as return-crossing payoff**

Keep existing outbound and return blades. In evolved mode launch four radial blades. Track the first crossing of two returning segments and create one blast `AttackInstance` at the crossing position. Apply blast damage only to targets whose hurt mask confirms overlap with the blast mask.

```csharp
if (evolved && !cast.MoonBlastResolved && TryFindReturnCrossing(cast, out var crossing))
{
    cast.MoonBlastResolved = true;
    ResolveMoonBlast(crossing, context);
}
```

- [ ] **Step 3: Implement 관일 cadence**

Increment a private `shotSequence` for every cast. Every fourth evolved cast uses `damage * 3`, `maxImpacts = 8`, `scale = 1.75f`, and a slower launch beat. Other shots retain normal 각궁 behavior.

```csharp
var sunPiercer = evolved && ++shotSequence % 4 == 0;
var impacts = sunPiercer ? 8 : 1 + Level;
var damage = Mathf.CeilToInt(BaseDamage * (sunPiercer ? 3f : 1f));
```

- [ ] **Step 4: Run paired tests and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EvolvedWeaponCombatPlayModeTests' -testResults 'Temp\evolved-ranged-a.xml' -logFile 'Temp\evolved-ranged-a.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs' 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs' 'Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs'
git commit -m 'feat: evolve hwando and gakgung'
```

---

### Task 4: Evolve 주술 부적 and 벽력탄

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`

**Interfaces:**
- `TalismanExecutor(..., int level, bool evolved)`
- `ThunderBombExecutor(..., int level, bool evolved)`
- Produces one seal-net blast per completed unique chain
- Produces pull phase before compressed lightning blast

- [ ] **Step 1: Add chain-completion and delayed-compression tests**

```csharp
[UnityTest]
public IEnumerator Heaven_chain_bursts_once_after_unique_target_chain_completes()
{
    var rig = EvolvedWeaponRig.For(WeaponId.TalismanThrow, evolved: true);
    rig.AddTargets(4);
    yield return rig.AdvanceSeconds(3f);
    Assert.That(rig.Count(ContactPhase.Blast), Is.EqualTo(4));
    Assert.That(rig.UniqueDamagedTargets, Is.EqualTo(4));
}

[UnityTest]
public IEnumerator Thunder_prison_pulls_before_secondary_blast()
{
    var rig = EvolvedWeaponRig.For(WeaponId.ThunderCrashBomb, evolved: true);
    var target = rig.AddTarget(new Vector2(2f, 0f));
    yield return rig.AdvanceSeconds(1f);
    Assert.That(target.Position.x, Is.LessThan(2f));
    Assert.That(rig.Telemetry.StateOrder, Is.EqualTo(new[] { "Pull", "CompressionDelay", "CompressedBlast" }));
    CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Blast);
}
```

- [ ] **Step 2: Implement 천쇄부진 completion**

Record unique successfully contacted targets in a cast-owned ordered list. When the hop limit completes with at least three targets, resolve one same-tick blast per linked target using a shared attack instance. Failed contacts never join the chain.

- [ ] **Step 3: Implement 뇌옥 pull, silence, and compressed blast**

Add evolved states `Pull`, `CompressionDelay`, and `CompressedBlast`. Pull eligible target positions toward the landing point for 0.25 seconds without dealing damage, wait 0.12 seconds, then confirm pixel-mask overlap for the blast damage.

```csharp
case ThunderBombState.Pull:
    PullTargets(deltaTime);
    if (bomb.Elapsed >= 0.25f) Transition(bomb, ThunderBombState.CompressionDelay);
    break;
case ThunderBombState.CompressionDelay:
    if (bomb.Elapsed >= 0.12f) Transition(bomb, ThunderBombState.CompressedBlast);
    break;
```

- [ ] **Step 4: Run tests and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EvolvedWeaponCombatPlayModeTests' -testResults 'Temp\evolved-control-a.xml' -logFile 'Temp\evolved-control-a.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs' 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs' 'Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs'
git commit -m 'feat: evolve talisman and thunder bomb'
```

---

### Task 5: Evolve 장승진 and 신기전

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`

**Interfaces:**
- `JangseungWardExecutor(..., int level, bool evolved)`
- `SingijeonExecutor(..., int level, bool evolved)`
- Produces completed-ward mark payoff
- Produces scout volley followed by delayed focus barrage

- [ ] **Step 1: Add ward-completion and focus-barrage tests**

```csharp
[UnityTest]
public IEnumerator Twelve_guardians_marks_only_targets_inside_completed_ward()
{
    var rig = EvolvedWeaponRig.For(WeaponId.JangseungWard, evolved: true);
    var inside = rig.AddTarget(Vector2.zero);
    var outside = rig.AddTarget(new Vector2(9f, 0f));
    yield return rig.AdvanceSeconds(2f);
    Assert.That(inside.Statuses, Contains.Item("guardian_mark"));
    Assert.That(outside.Statuses, Does.Not.Contain("guardian_mark"));
}

[UnityTest]
public IEnumerator Fire_dragon_barrage_scouts_then_focuses_marked_position()
{
    var rig = EvolvedWeaponRig.For(WeaponId.SingijeonVolley, evolved: true);
    yield return rig.AdvanceSeconds(2f);
    Assert.That(rig.Telemetry.VolleyKinds, Is.EqualTo(new[] { "scout", "focus" }));
    Assert.That(rig.Telemetry.FocusProjectileCount, Is.GreaterThan(rig.Telemetry.ScoutProjectileCount));
}
```

- [ ] **Step 2: Implement 십이지신 장승진 completion**

Spawn evolved posts sequentially around the normal ward radius. Only after the last post activates does the ward mark currently enclosed targets. Marked targets take an additional confirmed-contact pulse when crossing a ward segment; targets outside the completed polygon are not marked.

- [ ] **Step 3: Implement 화룡포 scout/focus rhythm**

The first volley uses three spread rockets and records the densest valid target position. After 0.35 seconds, fire at least eight rockets toward offsets around that recorded point. Each rocket remains a normal pixel-contact projectile and damages at most once.

- [ ] **Step 4: Run tests and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EvolvedWeaponCombatPlayModeTests' -testResults 'Temp\evolved-zones.xml' -logFile 'Temp\evolved-zones.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs' 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs' 'Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs'
git commit -m 'feat: evolve ward and singijeon'
```

---

### Task 6: Evolve 서리병 and 풍뢰선, then validate all eight

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatRuleTests.cs`

**Interfaces:**
- `FrostFlaskExecutor(..., int level, bool evolved)`
- `WindThunderFanExecutor(..., int level, bool evolved)`
- Produces stored-freeze shatter payoff
- Produces outward and return lightning phases across marked targets

- [ ] **Step 1: Add freeze-storage and returning-lightning tests**

```csharp
[UnityTest]
public IEnumerator Frost_bloom_stores_frozen_targets_then_shatters_on_expiry()
{
    var rig = EvolvedWeaponRig.For(WeaponId.FrostFlask, evolved: true);
    rig.AddTargets(3, insideField: true);
    yield return rig.AdvanceSeconds(rig.Telemetry.FieldDuration + 0.1f);
    Assert.That(rig.Count(ContactPhase.Blast), Is.EqualTo(3));
    Assert.That(rig.Telemetry.AllStoredTargetsResolvedOnce, Is.True);
}

[UnityTest]
public IEnumerator Returning_heaven_thunder_hits_marked_targets_outward_and_back()
{
    var rig = EvolvedWeaponRig.For(WeaponId.WindThunderFan, evolved: true);
    rig.AddTargets(3);
    yield return rig.AdvanceSeconds(2f);
    CollectionAssert.AreEqual(
        new[] { ContactPhase.Wind, ContactPhase.Lightning, ContactPhase.Inbound },
        rig.DistinctPhaseOrder);
}
```

- [ ] **Step 2: Implement 빙화원 stored shatter**

Add frozen target IDs to a field-owned set only after the existing residence threshold is met. On field expiry, spawn one short-lived spike attack per still-live stored target and apply one `ContactPhase.Blast` after confirming the spike mask overlaps that target.

- [ ] **Step 3: Implement 천뢰귀환**

Keep the wind mark phase. Sort marked targets by projection along wind direction, resolve lightning in that order, pause 0.08 seconds, then resolve `ContactPhase.Inbound` in reverse order with reduced damage. A target missing at return time is skipped without retargeting.

- [ ] **Step 4: Add the eight-weapon invariant test**

```csharp
[Test]
public void Every_evolution_changes_two_or_more_mechanic_dimensions()
{
    foreach (var evolution in WeaponEvolutionCatalog.All)
    {
        Assert.That(evolution.ChangedDimensions.Distinct().Count(), Is.GreaterThanOrEqualTo(2),
            evolution.DisplayName);
    }
}
```

Populate `ChangedDimensions` with values from `EvolutionDimension.Rhythm`, `Geometry`, `EnemyResponse`, and `Payoff`.

- [ ] **Step 5: Run focused evolution suites**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.WeaponEvolutionCatalogTests' -testResults 'Temp\evolution-editmode.xml' -logFile 'Temp\evolution-editmode.log' -quit
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.EvolvedWeaponCombatPlayModeTests' -testResults 'Temp\evolution-playmode.xml' -logFile 'Temp\evolution-playmode.log' -quit
```

Expected: all eight evolved executors confirm contact-based damage and all evolution catalog invariants pass.

- [ ] **Step 6: Perform one manual all-weapons smoke pass and commit**

Use a development-only inspector or test hook to set each weapon to level 5 and choose its evolution. For every weapon verify:

- The normal weapon identity remains recognizable.
- The evolved cast changes at least two mechanic dimensions.
- Damage occurs only at confirmed contact.
- The reveal reaches intensity 100 once; repeated casts return to normal budget.
- No evolved attack hides the player for more than a brief contact/reveal moment.

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs' 'Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs' 'Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs' 'Assets/JoseonHunter/Tests/EditMode/CombatRuleTests.cs'
git commit -m 'feat: complete eight weapon evolutions'
```
