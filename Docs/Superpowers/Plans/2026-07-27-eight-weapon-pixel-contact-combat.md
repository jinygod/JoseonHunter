# Eight-Weapon Pixel-Contact Combat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build all eight launch weapons with distinct mechanics, visible pixel-contact damage, pooled contact-point damage numbers, and a tightly budgeted PixelLab-to-Unity asset pipeline.

**Architecture:** Pure Domain types own weapon identity, targeting, hit policy, and resolved damage. Unity Runtime components schedule and move attack instances, use Physics 2D for broad-phase candidates and a binary pixel-mask service for narrow-phase contact, then publish confirmed damage events. Content ScriptableObjects bind level data and approved sprites; Presentation consumes confirmed events for pooled TextMeshPro numbers and never mutates health.

**Tech Stack:** Unity `6000.5.5f1`, C# assemblies (`JoseonHunter.Domain`, `Content`, `Runtime`, `Presentation`, `Editor`), Unity Physics 2D, TextMeshPro, NUnit EditMode/PlayMode tests, official Unity MCP, PixelLab MCP fast Pixen/PixFlux models.

## Global Constraints

- Android landscape remains the first player target; no new package is added.
- Launch roster is exactly `hwando_flying_blade`, `gakgung_shot`, `talisman_throw`, `thunder_crash_bomb`, `jangseung_ward`, `singijeon_volley`, `frost_flask`, and `wind_thunder_fan`.
- No large full-screen melee slash is used; the hwando is a compact outbound-and-returning occult flying blade.
- Physics overlap alone cannot award damage. An active hit pixel must overlap an enemy hurt mask.
- Decorative glow, smoke, trails, telegraphs, and sparks cannot deal damage.
- Presentation cannot mutate health or invent damage values.
- PixelLab baseline is `2,000` monthly generations with `0` used at plan creation.
- Standard one-generation Pixen/PixFlux calls are the default. Pro calls require a documented failed representative and explicit cost review.
- Existing dirty scene, imported `.meta`, art-source, and project-setting changes belong to the user and must not be overwritten or included in task commits.
- Every production-code task follows RED → verify failure → GREEN → verify pass → commit.

---

## File Structure

### Domain

- `Assets/JoseonHunter/Scripts/Domain/Combat/WeaponId.cs` — closed eight-ID value type and roster.
- `Assets/JoseonHunter/Scripts/Domain/Combat/WeaponMechanics.cs` — targeting, geometry, contact-phase, element, and repeat-policy enums.
- `Assets/JoseonHunter/Scripts/Domain/Combat/AttackInstance.cs` — stable instance ID and per-target hit memory.
- `Assets/JoseonHunter/Scripts/Domain/Combat/CombatTargetSnapshot.cs` — Unity-free target data and stable selection rules.
- `Assets/JoseonHunter/Scripts/Domain/Combat/ConfirmedDamageEvent.cs` — authoritative resolved hit envelope.
- `Assets/JoseonHunter/Scripts/Domain/Combat/DamageNumberAccumulator.cs` — presentation aggregation policy without Unity APIs.

### Content

- `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponLevelData.cs` — serializable level values.
- `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponDefinitionAsset.cs` — ScriptableObject identity, level, sprite, mask, and timing references.
- `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponCatalogAsset.cs` — validates and retrieves exactly eight definitions.
- `Assets/JoseonHunter/Content/Weapons/*.asset` — one definition per weapon.
- `Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset` — launch catalog.

### Runtime

- `Assets/JoseonHunter/Scripts/Runtime/Combat/ICombatTarget.cs` — runtime target contract.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatTargetRegistry.cs` — maintained active-target collection.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/PixelHitMask.cs` — immutable runtime mask data.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/PixelMaskContactService.cs` — transform-aware narrow-phase overlap and contact point.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs` — sole health-mutation entry point and confirmed-event publisher.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponRuntimeController.cs` — cooldowns and executor ownership.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs` — 환도 비검.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/LinearProjectileExecutor.cs` — shared arrow/rocket movement only.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs` — 각궁 policies.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs` — 신기전 policies.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs` — attach/seal/transfer.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs` — lob/fuse/expanding blast.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs` — ward placement and crossing.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs` — persistent slow/tick/freeze field.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs` — wind-mark-lightning sequence.

### Presentation and Editor

- `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs` — one pooled number animation.
- `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPool.cs` — bounded TextMeshPro pool and event subscription.
- `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetContract.cs` — source sprite and hit-mask validation.
- `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetImporter.cs` — deterministic nearest-neighbor import and mask derivation.
- `ArtSource/Pixel/Weapons/style-lock/` — shared approved style references and provenance.
- `ArtSource/Pixel/Weapons/<weapon-id>/` — source parts, prompts, masks, provenance, and review previews.
- `Docs/Assets/pixellab-weapon-generation-ledger.csv` — job, prompt, cost, decision, and remaining balance.

### Tests

- `Assets/JoseonHunter/Tests/EditMode/WeaponRosterTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/TargetSelectionTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/AttackInstanceTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/PixelMaskContactTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/CombatDamageServiceTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/DamageNumberAccumulatorTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/WeaponPixelAssetContractTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/DamageNumberPoolPlayModeTests.cs`

---

### Task 1: Lock the eight-weapon Domain roster

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/WeaponId.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/WeaponMechanics.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponRosterTests.cs`

**Interfaces:**
- Produces: `readonly struct WeaponId`, `WeaponRoster.All`, `WeaponTargeting`, `WeaponGeometry`, `ContactPhase`, `DamageElement`, and `RepeatHitPolicy`.
- Consumes: no new interfaces.

- [ ] **Step 1: Write the failing roster test**

```csharp
[Test]
public void LaunchRosterContainsExactlyEightDistinctWeapons()
{
    Assert.That(WeaponRoster.All.Select(id => id.Value).Distinct().Count(), Is.EqualTo(8));
    Assert.That(WeaponRoster.All, Is.EquivalentTo(new[]
    {
        WeaponId.HwandoFlyingBlade, WeaponId.GakgungShot,
        WeaponId.TalismanThrow, WeaponId.ThunderCrashBomb,
        WeaponId.JangseungWard, WeaponId.SingijeonVolley,
        WeaponId.FrostFlask, WeaponId.WindThunderFan
    }));
}
```

- [ ] **Step 2: Run the test and verify RED**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.WeaponRosterTests
```

Expected: compilation failure because `WeaponId` and `WeaponRoster` do not exist.

- [ ] **Step 3: Implement the closed value type and mechanic enums**

```csharp
public readonly struct WeaponId : IEquatable<WeaponId>
{
    public static readonly WeaponId HwandoFlyingBlade = new("hwando_flying_blade");
    public static readonly WeaponId GakgungShot = new("gakgung_shot");
    public static readonly WeaponId TalismanThrow = new("talisman_throw");
    public static readonly WeaponId ThunderCrashBomb = new("thunder_crash_bomb");
    public static readonly WeaponId JangseungWard = new("jangseung_ward");
    public static readonly WeaponId SingijeonVolley = new("singijeon_volley");
    public static readonly WeaponId FrostFlask = new("frost_flask");
    public static readonly WeaponId WindThunderFan = new("wind_thunder_fan");

    public WeaponId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Weapon ID is required.", nameof(value));
        Value = value;
    }

    public string Value { get; }
    public bool Equals(WeaponId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => obj is WeaponId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
}
```

Define the enum values explicitly:

```csharp
public enum WeaponTargeting { Nearest, HighestThreat, NearestUnmarked, DensestCenter, PlayerBoundary, DensestDirection, PredictedCrowd, DangerousSector }
public enum WeaponGeometry { ReturningPath, NarrowLine, SequentialHop, ExpandingCircle, Boundary, MultiLane, PersistentCircle, ConeThenLinks }
public enum ContactPhase { Outbound, Inbound, Direct, Attach, Seal, Blast, BoundaryCrossing, Tick, Wind, Lightning }
public enum DamageElement { Physical, Magic, Fire, Ice, Lightning }
public enum RepeatHitPolicy { OncePerInstance, OncePerPhase, TimedTicks, BoundaryReentry }
```

- [ ] **Step 4: Verify GREEN**

Run the Task 1 filter and then the complete EditMode suite. Expected: PASS with no new first-party Console error.

- [ ] **Step 5: Commit only Task 1 files**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Combat/WeaponId.cs Assets/JoseonHunter/Scripts/Domain/Combat/WeaponId.cs.meta Assets/JoseonHunter/Scripts/Domain/Combat/WeaponMechanics.cs Assets/JoseonHunter/Scripts/Domain/Combat/WeaponMechanics.cs.meta Assets/JoseonHunter/Tests/EditMode/WeaponRosterTests.cs Assets/JoseonHunter/Tests/EditMode/WeaponRosterTests.cs.meta
git commit -m "feat: define eight weapon roster"
```

### Task 2: Add deterministic target selection and hit memory

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/CombatTargetSnapshot.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/AttackInstance.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/TargetSelectionTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/AttackInstanceTests.cs`

**Interfaces:**
- Consumes: `WeaponTargeting`, `ContactPhase`, `RepeatHitPolicy`, and the existing `JoseonHunter.Domain.Geumjul.Float2`.
- Produces: `CombatTargetSelector.Select(...)`, `AttackInstance.TryRecordHit(...)`, and `AttackInstance.Reset()`.

- [ ] **Step 1: Write failing deterministic selection tests**

```csharp
[Test]
public void HighestThreatBreaksTiesByStableRuntimeId()
{
    var targets = new[]
    {
        new CombatTargetSnapshot(9, 25f, 5f, false, false, new Float2(2f, 0f)),
        new CombatTargetSnapshot(4, 25f, 5f, false, false, new Float2(-2f, 0f))
    };
    Assert.That(CombatTargetSelector.Select(WeaponTargeting.HighestThreat, new Float2(0f, 0f), targets).RuntimeId, Is.EqualTo(4));
}
```

```csharp
[Test]
public void FlyingBladeAllowsOneHitPerOutboundAndInboundPhase()
{
    var attack = new AttackInstance(31, RepeatHitPolicy.OncePerPhase, 0.5f);
    Assert.That(attack.TryRecordHit(7, ContactPhase.Outbound, 0f), Is.True);
    Assert.That(attack.TryRecordHit(7, ContactPhase.Outbound, 0.01f), Is.False);
    Assert.That(attack.TryRecordHit(7, ContactPhase.Inbound, 0.2f), Is.True);
}
```

- [ ] **Step 2: Run both filters and verify RED**

Expected: compilation failure for the missing selector and attack instance.

- [ ] **Step 3: Implement Unity-free target snapshots and policies**

`CombatTargetSnapshot` contains `RuntimeId`, `Health`, `Threat`, `IsElite`,
`IsBoss`, and `Float2 Position`. `CombatTargetSelector.Select` implements all
eight targeting values and rejects an empty candidate list with a `null`
nullable result. Every equality tie ends with ascending `RuntimeId`.

`AttackInstance` owns a dictionary keyed by `(targetId, phase)` plus a timed
dictionary for tick and re-entry policies. `TryRecordHit` is the only method
that mutates hit memory.

- [ ] **Step 4: Verify GREEN**

Run both new filters and complete EditMode. Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Combat/CombatTargetSnapshot.cs* Assets/JoseonHunter/Scripts/Domain/Combat/AttackInstance.cs* Assets/JoseonHunter/Tests/EditMode/TargetSelectionTests.cs* Assets/JoseonHunter/Tests/EditMode/AttackInstanceTests.cs*
git commit -m "feat: add weapon targeting and hit memory"
```

### Task 3: Create weapon content definitions and catalog validation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponLevelData.cs`
- Create: `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponDefinitionAsset.cs`
- Create: `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponCatalogAsset.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`

**Interfaces:**
- Consumes: `WeaponId`, mechanic enums, existing `UpgradeOffer`.
- Produces: `WeaponCatalogAsset.TryGet(WeaponId, out WeaponDefinitionAsset)` and exactly eight five-level launch definitions.

- [ ] **Step 1: Write the failing catalog test**

```csharp
[Test]
public void CatalogRejectsMissingDuplicateOrMechanicallyIdenticalLaunchDefinitions()
{
    var catalog = ScriptableObject.CreateInstance<WeaponCatalogAsset>();
    catalog.SetDefinitionsForTests(TestWeaponFactory.CreateLaunchDefinitions());
    Assert.That(catalog.ValidateLaunchRoster(), Is.Empty);

    catalog.SetDefinitionsForTests(TestWeaponFactory.CreateLaunchDefinitions().Take(7).ToArray());
    Assert.That(catalog.ValidateLaunchRoster(), Does.Contain("launch catalog must contain exactly eight weapons"));
}
```

- [ ] **Step 2: Run and verify RED**

Expected: compilation failure for missing content types.

- [ ] **Step 3: Implement the ScriptableObject contracts**

`WeaponLevelData` fields are `baseDamage`, `cooldownSeconds`, `range`,
`projectileCount`, `speed`, `durationSeconds`, `pierce`, `chainCount`,
`knockback`, `slowFraction`, and `criticalChance`. Validation requires five
levels, finite non-negative values, cooldown greater than zero, and level IDs
matching the owning definition.

`WeaponDefinitionAsset` stores serialized string ID but exposes parsed
`WeaponId Id`; it also stores the four mechanic enums, element, level array,
presentation sprite list, `Texture2D[]` binary mask source references, active
frame windows, and pool capacity. Runtime converts each validated mask texture
to immutable `PixelHitMask` data once during catalog loading.

- [ ] **Step 4: Replace the old three-ID progression array**

Change `UpgradeSelector` to consume the eight canonical string values from
`WeaponRoster.All`. Preserve the existing maximum level at five and the
existing guaranteed-owned-weapon offer behavior. Keep the hwando evolution
mapping but point it to `hwando_flying_blade`.

- [ ] **Step 5: Verify GREEN and commit**

Run `WeaponContentTests`, `CombatRuleTests`, and the complete EditMode suite.
Commit only Task 3 files:

```powershell
git add Assets/JoseonHunter/Scripts/Content/Weapons Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs* Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs*
git commit -m "feat: add eight weapon content catalog"
```

### Task 4: Build binary pixel-mask import and contact math

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/PixelHitMask.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/PixelMaskContactService.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetContract.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetImporter.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/PixelMaskContactTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponPixelAssetContractTests.cs`

**Interfaces:**
- Consumes: approved RGBA sprite source and importer metadata.
- Produces: `PixelHitMask`, `PixelMaskTransform`, and `PixelMaskContactService.TryFindContact(...)`.

- [ ] **Step 1: Write failing transparent-pixel and transform tests**

```csharp
[Test]
public void TransparentGlowCannotConfirmContact()
{
    var attack = PixelHitMask.FromRows("0000", "0100", "0000");
    var enemy = PixelHitMask.FromRows("1");
    Assert.That(PixelMaskContactService.TryFindContact(
        attack, PixelMaskTransform.Identity,
        enemy, PixelMaskTransform.Translation(0f, 0f),
        out _), Is.False);
}

[Test]
public void ActivePixelConfirmsContactAfterFlipAndRotation()
{
    var attack = PixelHitMask.FromRows("001", "000", "000");
    var enemy = PixelHitMask.FromRows("1");
    var transform = new PixelMaskTransform(new Float2(2f, 0f), 90, true, 1);
    Assert.That(PixelMaskContactService.TryFindContact(
        attack, transform, enemy,
        PixelMaskTransform.Translation(2f, -2f), out var point), Is.True);
    Assert.That(point, Is.EqualTo(new Float2(2f, -2f)));
}
```

- [ ] **Step 2: Run and verify RED**

Expected: missing mask/contact types.

- [ ] **Step 3: Implement immutable masks and deterministic sampling**

Store width, height, pivot pixel, pixels-per-unit, and a packed `uint[]` bitset.
Map active attack pixels to world space with integer quarter-turn fast paths
and a deterministic nearest-neighbor fallback for arbitrary rotations.
Return the first overlapping world pixel in stable row-major order.

- [ ] **Step 4: Implement asset preflight and mask derivation**

The editor contract rejects anti-aliased alpha (`alpha` must be `0` or `255`),
wrong PPU, compression, mipmaps, non-point filtering, mask/sprite dimension
mismatch, and active pixels outside opaque source pixels. The importer derives
the initial mask from alpha, then applies a checked-in exclusion PNG for glow,
trail, or telegraph pixels.

- [ ] **Step 5: Verify GREEN and commit**

Run both Task 4 filters and complete EditMode. Expected: PASS. Commit with:

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/PixelHitMask.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/PixelMaskContactService.cs* Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetContract.cs* Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetImporter.cs* Assets/JoseonHunter/Tests/EditMode/PixelMaskContactTests.cs* Assets/JoseonHunter/Tests/EditMode/WeaponPixelAssetContractTests.cs*
git commit -m "feat: add pixel contact masks"
```

### Task 5: Make confirmed damage the only health-mutation path

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/CombatTypes.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/DamageResolver.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/ConfirmedDamageEvent.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/ICombatTarget.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatTargetRegistry.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/CombatDamageServiceTests.cs`

**Interfaces:**
- Consumes: `WeaponId`, `AttackInstance`, `DamageResolver`, pixel contact point.
- Produces: `CombatDamageService.TryApply(in WeaponDamageRequest, out ConfirmedDamageEvent)` and `event Action<ConfirmedDamageEvent> DamageConfirmed`.

- [ ] **Step 1: Write the failing authority test**

```csharp
[Test]
public void ConfirmedHitMutatesHealthOnceAndPublishesExactResolvedDamage()
{
    var target = new FakeCombatTarget(runtimeId: 7, health: 40);
    var service = new CombatDamageService();
    ConfirmedDamageEvent published = default;
    service.DamageConfirmed += value => published = value;

    var request = WeaponDamageRequest.Create(
        attackInstanceId: 12, WeaponId.HwandoFlyingBlade, target,
        baseDamage: 9, critical: false, contactPoint: new Float2(3f, 4f),
        phase: ContactPhase.Outbound, simulationTick: 44);

    Assert.That(service.TryApply(request, out var confirmed), Is.True);
    Assert.That(target.Health, Is.EqualTo(31));
    Assert.That(confirmed, Is.EqualTo(published));
    Assert.That(confirmed.FinalDamage, Is.EqualTo(9));
    Assert.That(confirmed.ContactPoint, Is.EqualTo(new Float2(3f, 4f)));
}
```

- [ ] **Step 2: Run and verify RED**

Expected: missing runtime damage contracts.

- [ ] **Step 3: Implement target registry and damage service**

`ICombatTarget` exposes `RuntimeId`, `IsAlive`, `Health`, `WorldPosition`,
`HurtMask`, `HurtMaskTransform`, `ApplyResolvedDamage(int)`, and
`ApplyKnockback(Float2, float)`.

`CombatDamageService` rejects dead targets, invalid numbers, rejected hit
memory, and absent confirmed contact. It resolves once, mutates once, then
publishes the immutable event.

- [ ] **Step 4: Verify GREEN and commit**

Run `CombatDamageServiceTests`, existing `CombatRuleTests`, and complete
EditMode. Commit:

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Combat/CombatTypes.cs Assets/JoseonHunter/Scripts/Domain/Combat/DamageResolver.cs Assets/JoseonHunter/Scripts/Domain/Combat/ConfirmedDamageEvent.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/ICombatTarget.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/CombatTargetRegistry.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs* Assets/JoseonHunter/Tests/EditMode/CombatDamageServiceTests.cs*
git commit -m "feat: centralize confirmed weapon damage"
```

### Task 6: Add pooled contact-point damage numbers

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/DamageNumberAccumulator.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPool.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/JoseonHunter.Presentation.asmdef`
- Create: `Assets/JoseonHunter/Tests/EditMode/DamageNumberAccumulatorTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/DamageNumberPoolPlayModeTests.cs`

**Interfaces:**
- Consumes: `ConfirmedDamageEvent`.
- Produces: `DamageNumberAccumulator.Add(...)`, `FlushReady(...)`, and pooled `DamageNumberPool.Bind(CombatDamageService)`.

- [ ] **Step 1: Write failing aggregation tests**

```csharp
[Test]
public void SameSourceTargetAndWeaponAggregateInsideQuarterSecondWindow()
{
    var accumulator = new DamageNumberAccumulator(0.25f);
    accumulator.Add(Event(source: 1, target: 2, damage: 4, time: 1.00f));
    accumulator.Add(Event(source: 1, target: 2, damage: 6, time: 1.20f));
    Assert.That(accumulator.FlushReady(1.24f), Is.Empty);
    Assert.That(accumulator.FlushReady(1.26f).Single().DisplayedDamage, Is.EqualTo(10));
}
```

- [ ] **Step 2: Run and verify RED**

Expected: missing accumulator.

- [ ] **Step 3: Implement accumulator and TextMeshPro pool**

Add the `Unity.TextMeshPro` assembly reference. `DamageNumberPresenter` owns one
`TextMeshPro` component, moves upward for `0.55` seconds, applies gold color and
one `1.2` scale punch for critical hits, and returns itself through a callback.
`DamageNumberPool` prewarms `48`, grows to `96`, and aggregates DOT events before
renting presenters. Normal damage is light neutral; fire, ice, lightning,
magic/seal, and physical events use restrained ember, cyan, violet, gold, and
ivory accents respectively. Boss hits remain visible `0.15` seconds longer
without changing their combat value.

- [ ] **Step 4: Write and run the PlayMode pool reset test**

The test publishes 120 confirmed events, advances time, and asserts that active
presenters return to zero, total instances never exceed 96, and the next rent
contains no prior text or critical styling.

- [ ] **Step 5: Verify GREEN and commit**

Run both Task 6 filters and complete EditMode/targeted PlayMode. Commit:

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Combat/DamageNumberAccumulator.cs* Assets/JoseonHunter/Scripts/Presentation/Combat Assets/JoseonHunter/Scripts/Presentation/JoseonHunter.Presentation.asmdef Assets/JoseonHunter/Tests/EditMode/DamageNumberAccumulatorTests.cs* Assets/JoseonHunter/Tests/PlayMode/DamageNumberPoolPlayModeTests.cs*
git commit -m "feat: show pooled contact damage numbers"
```

### Task 7: Produce and approve the PixelLab shared style lock

**Files:**
- Create: `ArtSource/Pixel/Weapons/style-lock/prompt.md`
- Create: `ArtSource/Pixel/Weapons/style-lock/style-lock.png`
- Create: `ArtSource/Pixel/Weapons/style-lock/style-lock-preview-8x.png`
- Create: `ArtSource/Pixel/Weapons/style-lock/provenance.json`
- Create: `Docs/Assets/pixellab-weapon-generation-ledger.csv`

**Interfaces:**
- Consumes: approved Han Yeonhwa combat sprite and master palette.
- Produces: one approved style reference used by every subsequent PixelLab call.

- [ ] **Step 1: Record the balance before generation**

Call `get_balance` and add the ledger header:

```csv
timestamp,job_id,tool,prompt_revision,cost,status,remaining_generations,asset_path
```

Expected baseline: `generations_remaining=2000`, `generations_used=0`.

- [ ] **Step 2: Generate one 128×128 transparent style board with Pixen**

Use `create_image_pixen` at a cost of one generation:

```json
{
  "width": 128,
  "height": 128,
  "no_background": true,
  "detail": "medium detail",
  "outline": "single color outline",
  "view": "high top-down",
  "description": "A compact coherent Joseon occult fantasy pixel-art weapon style board containing exactly four separated objects: a small indigo and gold hwando, an ivory paper talisman with crimson ink, a wooden jangseung ward post, and a cyan lightning impact. Hard pixel edges, one-pixel near-black outline, limited indigo ivory crimson gold cyan and ember palette, transparent background, no text, no frame, no glow haze, no anti-aliasing."
}
```

- [ ] **Step 3: Poll, save, validate, and record**

Poll `get_image(job_id)`. Save the original image and a nearest-neighbor 8×
review preview. Run the weapon art preflight. Record job ID, exact cost,
remaining balance, prompt revision `weapon-style-lock-v1`, and validation
result without authentication data.

- [ ] **Step 4: Apply the review gate**

Present native, 8×, light-background, dark-background, and Han Yeonhwa scale
comparisons. If rejected, change one prompt dimension at a time and spend at
most three additional one-generation attempts. Do not invoke Pro generation.

- [ ] **Step 5: Commit only the approved style lock**

```powershell
git add ArtSource/Pixel/Weapons/style-lock Docs/Assets/pixellab-weapon-generation-ledger.csv
git commit -m "art: lock pixel weapon style"
```

### Task 8: Implement the 환도 비검 vertical slice

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponRuntimeController.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Consumes: catalog, target registry, pixel contact service, damage service.
- Produces: first complete `IWeaponExecutor` lifecycle and the migration seam used by the other seven weapons.

- [ ] **Step 1: Write failing flying-blade tests**

Test that the blade:

```csharp
Assert.Multiple(() =>
{
    Assert.That(result.ContactPhases, Is.EqualTo(new[] { ContactPhase.Outbound, ContactPhase.Inbound }));
    Assert.That(result.HitsFor(targetId: 5, ContactPhase.Outbound), Is.EqualTo(1));
    Assert.That(result.HitsFor(targetId: 5, ContactPhase.Inbound), Is.EqualTo(1));
    Assert.That(result.MaximumDistanceFromOwner, Is.LessThanOrEqualTo(level.Range));
    Assert.That(result.ReturnedToPool, Is.True);
});
```

Also assert zero damage before the first mask overlap.

- [ ] **Step 2: Run and verify RED**

Expected: missing executor/controller.

- [ ] **Step 3: Implement the executor and bridge**

`IWeaponExecutor.Tick(float delta, in WeaponExecutionContext context)` schedules
and advances attacks. `FlyingBladeExecutor` uses a quadratic outbound curve and
a direct bounded return curve. It delegates all damage to the contact and damage
services. Levels increase damage, range, speed, and blade count; level five
launches three staggered blades with separate return curves.

Replace `FirstPlayableController.UpdateAttack` and `ShowHwandoStrike` authority
with one `WeaponRuntimeController.Tick`. Do not modify movement, spawning,
geumjul, results, or unrelated HUD code in this task.

- [ ] **Step 4: Verify GREEN in EditMode and PlayMode**

Run `WeaponMechanicTests`, the first-playable tests, and a PlayMode capture that
shows the blade touching before each damage number.

- [ ] **Step 5: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponRuntimeController.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs* Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs* Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs
git commit -m "feat: replace slash with flying hwando"
```

### Task 9: Implement 각궁 and 신기전 as distinct projectile weapons

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/LinearProjectileExecutor.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`

**Interfaces:**
- Consumes: `IWeaponExecutor`, targeting, contact, damage, pools.
- Produces: non-homing precision arrow and multi-lane directional volley.

- [ ] **Step 1: Add failing distinction tests**

Assert that the bow selects the boss over a closer normal enemy, fires one
narrow projectile, and can miss after target movement. Assert that singijeon
selects the densest direction, creates the configured number of non-homing
lanes, and never reuses the bow's highest-threat policy.

- [ ] **Step 2: Verify RED**

Expected: missing executors.

- [ ] **Step 3: Implement shared movement without shared weapon policy**

`LinearProjectileExecutor` owns only position integration, lifetime, broad
phase, mask contact, penetration count, and pool return. Gakgung and Singijeon
own different selection, count, spread, speed, damage, and impact policies.
At level five the bow uses one armor-piercing lead arrow plus two split arrows;
Singijeon uses three separated volley rows with bounded lane and impact counts.

- [ ] **Step 4: Verify GREEN and commit**

Run mechanic tests and targeted PlayMode. Commit:

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/LinearProjectileExecutor.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs* Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs
git commit -m "feat: add bow and singijeon weapons"
```

### Task 10: Implement 부적 투척 and 풍뢰 부채

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`

**Interfaces:**
- Consumes: executor context, target reservations, confirmed damage service.
- Produces: sequential attach/seal/transfer and cone-contact/simultaneous lightning.

- [ ] **Step 1: Add failing sequencing tests**

For talismans, assert `Direct → Attach → Seal` precedes transfer, each hop uses
a different legal target, and no target causes a final burst. For the fan,
assert wind contact and knockback occur before lightning, only wind-marked
targets receive the echo, and lightning resolution is simultaneous by tick.

- [ ] **Step 2: Verify RED**

Expected: missing executors.

- [ ] **Step 3: Implement both state machines**

Talisman state is `Flying`, `Attached`, `Sealing`, `Transferring`, `Complete`.
Fan state is `WindActive`, `EchoDelay`, `LightningResolve`, `Complete`.
Keep separate target-selection and repeat-hit memory. At level five, talismans
hold several seals and resolve one five-color binding burst; the fan emits four
short cardinal gusts and then one bounded simultaneous lightning resolution.

- [ ] **Step 4: Verify GREEN and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs* Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs
git commit -m "feat: add talisman and wind thunder weapons"
```

### Task 11: Implement 벽력진천뢰 and 서리 호리병

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`

**Interfaces:**
- Consumes: predicted crowd center, area pools, contact masks, damage service.
- Produces: expanding one-shot blast and bounded persistent slow field.

- [ ] **Step 1: Add failing area timing tests**

Assert the bomb deals no damage at fuse completion until its expanding ring
reaches each target. Assert the frost field slows on entry, ticks at configured
windows, freezes only after threshold residence, decays slow after exit, and
expires the oldest field when capacity is exceeded.

- [ ] **Step 2: Verify RED**

- [ ] **Step 3: Implement lob, blast, and field states**

Use deterministic parabolic interpolation for both thrown objects. Bomb blast
radius expands over fixed simulation ticks and runs pixel-mask narrow phase.
Frost uses a persistent mask with timed hit policy rather than per-frame
damage. At level five the bomb adds one outward secondary shockwave; the frost
field raises bounded periodic ice spikes that use their own active masks.

- [ ] **Step 4: Verify GREEN and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs* Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs* Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs
git commit -m "feat: add bomb and frost field weapons"
```

### Task 12: Implement 장승 결계

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`

**Interfaces:**
- Consumes: player position, ward segment mask, target previous/current position.
- Produces: bounded post sets and direction-aware boundary crossing events.

- [ ] **Step 1: Add failing boundary tests**

Assert no damage while an enemy remains on one side, one hit when the movement
segment crosses the ward, no repeated hit while touching the boundary, and a
second hit only after leaving and satisfying the re-entry interval.

- [ ] **Step 2: Verify RED**

- [ ] **Step 3: Implement finite ward sets**

Place posts in deterministic cardinal order, build finite segment transforms,
test movement-segment intersection before pixel-mask confirmation, and replace
the oldest set at capacity. Do not reuse geumjul loop completion or area seal
damage. At level five maintain four mobile cardinal posts that reposition only
at bounded intervals, so enemies can still cross readable finite boundaries.

- [ ] **Step 4: Verify GREEN and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs* Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs
git commit -m "feat: add jangseung boundary weapon"
```

### Task 13: Generate the complete PixelLab weapon source-part batch

**Files:**
- Create: `ArtSource/Pixel/Weapons/<weapon-id>/prompt.md`
- Create: `ArtSource/Pixel/Weapons/<weapon-id>/provenance.json`
- Create: `ArtSource/Pixel/Weapons/<weapon-id>/*.png`
- Create: `ArtSource/Pixel/Weapons/review/weapon-source-parts-board.png`
- Modify: `Docs/Assets/pixellab-weapon-generation-ledger.csv`

**Interfaces:**
- Consumes: approved style lock and exact runtime dimensions from Tasks 8–12.
- Produces: approved sprites, deterministic masks, icons, and review board.

- [ ] **Step 1: Generate one representative per attack family**

Use eight one-generation PixFlux calls with the approved style board passed as
`color_image_base64`. Set `no_background=true`, `outline="single color outline"`,
`shading="flat shading"`, `detail="low detail"`, and these canvases:

```text
hwando_flying_blade 64x64
gakgung_shot        64x32
talisman_throw      48x48
thunder_crash_bomb  64x64
jangseung_ward      64x64
singijeon_volley    64x32
frost_flask         48x48
wind_thunder_fan    96x64
```

Each description names exactly one object, forbids text and baked glow, and
states the intended active contact portion. Poll all eight jobs, record each
cost, and stop if the balance delta differs from eight.

- [ ] **Step 2: Validate and review representatives**

Run `WeaponPixelAssetContractTests` plus the editor preflight. Present light,
dark, native, and 8× comparisons. Reject anatomical-looking hands, blurry
edges, mixed projection, clipped silhouettes, or style drift.

- [ ] **Step 3: Generate secondary source parts only for approved families**

Use one-generation PixFlux calls with the approved representative as
`init_image_base64` when identity continuity matters. Use
`init_image_strength=300` for controlled variants. Generate only the source
parts listed in the design spec; rotations, flips, tint variants, line
repetition, and scale changes remain Unity operations.

- [ ] **Step 4: Derive and review masks**

Run the deterministic importer, save binary mask PNGs, and manually exclude
telegraphs, smoke, glow, and trails. Produce an overlay board where active hit
pixels are magenta over the source sprites.

- [ ] **Step 5: Generate eight UI icons efficiently**

Use eight separate `48x48` Pixen calls with the approved weapon representative
described as the identity reference in the prompt. This costs eight generations
but avoids an inconsistent mixed icon sheet and allows per-weapon rejection.

- [ ] **Step 6: Enforce the budget**

The default full-batch ceiling is `60` total generations including the style
lock. If the ledger reaches `50`, stop and review remaining gaps. Any increase
above `60` requires an explicit new approval and an explanation by weapon and
asset.

- [ ] **Step 7: Commit the approved batch**

```powershell
git add ArtSource/Pixel/Weapons Docs/Assets/pixellab-weapon-generation-ledger.csv
git commit -m "art: add approved pixel weapon assets"
```

### Task 14: Import assets, create catalog assets, and wire Gameplay safely

**Files:**
- Create: `Assets/JoseonHunter/Art/Weapons/Runtime/<weapon-id>/*.png`
- Create: `Assets/JoseonHunter/Content/Weapons/*.asset`
- Create: `Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: approved source parts, masks, definitions, executors, pools.
- Produces: generated Gameplay wiring with all eight weapons available through one catalog.

- [ ] **Step 1: Write the failing generated-scene PlayMode test**

The test opens/generated-loads Gameplay, gets `WeaponRuntimeController`, asserts
all eight executors are registered exactly once, activates each at accelerated
cooldown, and requires at least one confirmed hit and one pooled damage number
per weapon.

- [ ] **Step 2: Run and verify RED**

Expected: catalog/wiring absent.

- [ ] **Step 3: Import and create ScriptableObjects through Editor APIs**

Use the importer to copy only approved sources, preserve point/no-mipmap/no-
compression settings, create mask sub-assets, create eight definitions and one
catalog, then save and re-read every reference.

- [ ] **Step 4: Update the scene generator**

Generate `CombatRoot`, target registry, contact service, damage service, weapon
controller, transient pools, and damage-number pool. Refuse to overwrite an
open dirty scene using the existing scene-generator safety policy.

- [ ] **Step 5: Remove prototype damage authority**

Delete `UpdateAttack`, `ShowHwandoStrike`, direct weapon `DamageEnemy` calls,
and the old three-choice string-prefix weapon mutations only after the new
catalog path is wired. Preserve non-weapon damage such as contact and geumjul
until separately migrated through the shared service.

- [ ] **Step 6: Verify GREEN and commit**

Run the new PlayMode filter, full EditMode suite, and existing first-playable
tests. Commit:

```powershell
git add Assets/JoseonHunter/Art/Weapons/Runtime Assets/JoseonHunter/Content/Weapons Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs*
git commit -m "feat: wire eight weapon combat"
```

### Task 15: Validate contact truth, readability, and performance

**Files:**
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`
- Create: `Docs/Verification/2026-07-27-eight-weapon-pixel-contact-combat.md`
- Create: `Docs/Assets/review/eight-weapon-contact-board.png`
- Modify: `Docs/Assets/pixellab-weapon-generation-ledger.csv`

**Interfaces:**
- Consumes: integrated eight-weapon runtime and approved assets.
- Produces: automated results, contact evidence, stress evidence, and final balance.

- [ ] **Step 1: Run complete automated validation**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1
```

Run targeted PlayMode through Unity Test Runner or the project PlayMode runner.
Expected: all tests PASS and Unity Console reports zero new first-party errors.

- [ ] **Step 2: Capture one contact proof per weapon**

For each weapon, capture the frame before contact and the first confirmed
damage frame. Overlay attack hit pixels, enemy hurt pixels, world contact
point, attack-instance ID, and final damage. Confirm no pre-contact health
change.

- [ ] **Step 3: Run the 80-enemy stress scene**

Exercise all eight weapons over a bounded capture, record average frame time,
maximum active attacks, broad-phase candidate count, narrow-phase mask checks,
active damage numbers, and pool high-water marks. Confirm no unbounded growth
and no full-screen readability loss.

- [ ] **Step 4: Reconcile PixelLab usage**

Call `get_balance`, compare actual usage to every ledger row, and record final
remaining generations. Resolve any delta before claiming the batch complete.

- [ ] **Step 5: Review the final diff**

```powershell
git status --short
git diff --check
git diff --stat HEAD~1
```

Verify that user-owned dirty files were not staged or overwritten.

- [ ] **Step 6: Commit verification evidence**

```powershell
git add Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs Docs/Verification/2026-07-27-eight-weapon-pixel-contact-combat.md Docs/Assets/review/eight-weapon-contact-board.png Docs/Assets/pixellab-weapon-generation-ledger.csv
git commit -m "docs: verify eight weapon pixel combat"
```

## Final Acceptance Checklist

- [ ] Eight unique weapon IDs and five level rows per weapon.
- [ ] Eight distinct targeting/geometry/timing/role combinations.
- [ ] 환도 uses a compact returning flying blade and no full-screen melee slash.
- [ ] Damage requires active visual-pixel and hurt-mask overlap.
- [ ] Flying blade, projectiles, expanding blast, ward crossing, persistent field, and wind/lightning timing tests pass.
- [ ] Confirmed events mutate health once and produce contact-point numbers.
- [ ] DOT display aggregation does not change combat totals.
- [ ] PixelLab ledger reconciles exactly with account usage and remains within the approved ceiling.
- [ ] Unity imports point-filtered, uncompressed, no-mipmap weapon assets and masks.
- [ ] Gameplay wiring contains all eight executors once.
- [ ] 80-enemy stress validation shows bounded pools and readable effects.
- [ ] Full EditMode and targeted PlayMode tests pass with no new first-party Console errors.
