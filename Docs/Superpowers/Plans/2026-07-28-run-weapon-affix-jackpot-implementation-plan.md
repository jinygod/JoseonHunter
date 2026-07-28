# Run Weapon Affix Jackpot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add deterministic run-only random weapon stat rolls, progressive one-to-three-line potential jackpots, 24 weapon-specific mechanics, and a fast PixelLab-backed reveal UI without weakening pixel-contact damage rules.

**Architecture:** A pure domain roller owns deterministic selection and a run-scoped profile per weapon. `FirstPlayableController` applies one roll after every new weapon or weapon-level choice, passes an immutable `WeaponRuntimeModifiers` snapshot into rebuilt executors, and publishes the result to presentation. Shared delayed statuses live in one runtime service; executor-specific potentials remain inside the executor whose state machine they extend.

**Tech Stack:** Unity 6.0.5 (`6000.5.5f1`), C#/.NET, Unity Input System/uGUI/TMP, NUnit EditMode and PlayMode tests, existing pixel-mask combat services, PixelLab MCP/API.

## Global Constraints

- All affixes and potentials exist for the current run only and reset with `FirstPlayableController.ResetRun`.
- Every new weapon acquisition and level 2–5 weapon upgrade grants exactly one compatible general affix.
- General ranges are damage `+10–30%`, cooldown `-5–12%`, area `+8–20%`, projectile speed `+10–30%`, and duration `+10–25%`.
- Potential jackpot chances are `5%` at zero lines, `2%` at one line, `0.5%` at two lines, and disabled at three lines.
- After one potential succeeds, same-roll continuation chances are `8%` for the next line and `1%` for the third line.
- Potential IDs never duplicate within one weapon profile; at most three lines may be stored.
- General results auto-close in about `0.95s`; one-, two-, and three-line jackpots cap at `1.3s`, `1.6s`, and `1.9s`.
- Pointer input may shorten presentation to about `0.3s` for general results and no more than `0.7s` for a three-line jackpot, without changing the result.
- High and perfect general rolls alone receive tension buildup; ordinary rolls must remain fast.
- All new damage or status effects originate from a confirmed pixel-mask contact and use explicit `AttackInstance`/`RepeatHitPolicy` rules.
- Decorative glow, smoke, trails, telegraphs, and UI pixels must never enter a damage hit mask.
- Final evolutions inherit the same run affix profile; evolution and executor rebuilds must not reroll or clear it.
- Use PixelLab for final missing slot, rarity, status, and potential visuals. Do not ship placeholder boxes or glyphs.
- Do not touch the eight pre-existing untracked weapon-directory `.meta` files.
- Do not interact with or terminate the original Unity Editor processes; validation targets only the isolated worktree.

---

### Task 1: Deterministic Run Affix Domain

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponAffixTypes.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponAffixCatalog.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponAffixRoller.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixRollerTests.cs`

**Interfaces:**
- Produces: `WeaponAffixStat`, `WeaponAffixTier`, `WeaponPotentialId`, `WeaponAffixRoll`, sealed reference type `WeaponAffixRollResult`, `WeaponRunAffixProfile`, `WeaponRunAffixState`.
- Produces: `IAffixRandom.NextUnit()`, `IAffixRandom.NextIndex(int)`, and `SeededAffixRandom`.
- Produces: `WeaponAffixRoller.RollAndApply(WeaponRunAffixState, WeaponId, IAffixRandom)`.
- Produces: `WeaponAffixCatalog.CompatibleStats(WeaponId)` and `CompatiblePotentials(WeaponId)`.

- [ ] **Step 1: Write failing deterministic and boundary tests**

```csharp
[Test]
public void New_weapon_roll_adds_one_compatible_general_affix()
{
    var state = new WeaponRunAffixState();
    var result = WeaponAffixRoller.RollAndApply(
        state, WeaponId.JangseungWard,
        new SequenceAffixRandom(new[] { .10, .99 }, new[] { 0 }));

    Assert.That(result.General.Stat, Is.Not.EqualTo(WeaponAffixStat.ProjectileSpeed));
    Assert.That(state.ProfileFor(WeaponId.JangseungWard).GeneralRolls.Count, Is.EqualTo(1));
}

[Test]
public void Jackpot_can_fill_three_distinct_lines_in_one_roll()
{
    var state = new WeaponRunAffixState();
    var result = WeaponAffixRoller.RollAndApply(
        state, WeaponId.HwandoFlyingBlade,
        new SequenceAffixRandom(
            new[] { .99, .01, .01, .001 },
            new[] { 0, 0, 1, 2 }));

    Assert.That(result.NewPotentials.Count, Is.EqualTo(3));
    Assert.That(result.NewPotentials.Distinct().Count(), Is.EqualTo(3));
    Assert.That(state.ProfileFor(WeaponId.HwandoFlyingBlade).PotentialIds.Count, Is.EqualTo(3));
}
```

`SequenceAffixRandom(IEnumerable<double> units, IEnumerable<int> indices)` is a private test fake with separate queues for unit and index values; it throws when a test under-specifies randomness so probability branches remain explicit.

- [ ] **Step 2: Run the focused EditMode test and verify the missing types fail**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter\.worktrees\combat-ui-evolutions' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.WeaponAffixRollerTests' -testResults 'Temp\affix-task1.xml' -logFile 'Temp\affix-task1.log' -quit
```

Expected: compile failure naming `WeaponRunAffixState` or a focused failing result before implementation.

- [ ] **Step 3: Implement immutable value types and mutable run state**

```csharp
public sealed class WeaponRunAffixProfile
{
    private readonly List<WeaponAffixRoll> generalRolls = new();
    private readonly List<WeaponPotentialId> potentialIds = new();
    public IReadOnlyList<WeaponAffixRoll> GeneralRolls => generalRolls;
    public IReadOnlyList<WeaponPotentialId> PotentialIds => potentialIds;
    internal void AddGeneral(WeaponAffixRoll roll) => generalRolls.Add(roll);
    internal bool AddPotential(WeaponPotentialId id)
    {
        if (potentialIds.Count >= 3 || potentialIds.Contains(id)) return false;
        potentialIds.Add(id);
        return true;
    }
}

public sealed class WeaponRunAffixState
{
    private readonly Dictionary<WeaponId, WeaponRunAffixProfile> profiles = new();
    public WeaponRunAffixProfile ProfileFor(WeaponId id) =>
        profiles.TryGetValue(id, out var profile) ? profile : profiles[id] = new WeaponRunAffixProfile();
    public void Clear() => profiles.Clear();
}
```

Use `WeaponPotentialId` constants for all 24 approved IDs rather than free-form strings.

- [ ] **Step 4: Implement compatibility catalogs and exact probabilities**

`CompatibleStats` excludes projectile speed for `thunder_crash_bomb`, `jangseung_ward`, `frost_flask`, and `wind_thunder_fan`; it excludes duration for `hwando_flying_blade`, `gakgung_shot`, `talisman_throw`, and `singijeon_volley`. Every weapon retains damage, cooldown, and area.

The potential catalog maps exactly three unique IDs to each `WeaponRoster.All` entry and validates:

```csharp
if (potentialMap.Count != WeaponRoster.All.Count ||
    potentialMap.Any(pair => pair.Value.Count != 3 || pair.Value.Distinct().Count() != 3))
    throw new InvalidOperationException("Every launch weapon requires three distinct potentials.");
```

- [ ] **Step 5: Implement the roller and tier calculation**

The roller must consume randomness in this order: stat index, value unit, initial jackpot unit, potential index, second-line continuation unit/index, third-line continuation unit/index. Stop consuming potential values once the profile reaches three lines.

```csharp
var tier = valueUnit >= .95 ? WeaponAffixTier.Perfect :
           valueUnit >= .75 ? WeaponAffixTier.High :
           WeaponAffixTier.Standard;
var jackpotChance = profile.PotentialIds.Count == 0 ? .05 :
                    profile.PotentialIds.Count == 1 ? .02 :
                    profile.PotentialIds.Count == 2 ? .005 : 0.0;
```

- [ ] **Step 6: Add tests for ranges, no duplicates, three-line cap, no dead rolls, repeatable seeds, and state reset**

Assert all five numeric ranges at both `0.0` and `0.999999`, and loop every roster weapon through 100 deterministic seeds to prove only compatible stats/potentials appear.

- [ ] **Step 7: Run focused tests or, if Unity licensing blocks execution, compile Domain and EditMode test assemblies once**

Expected: zero C# compile errors. Record missing XML as infrastructure evidence rather than a pass.

- [ ] **Step 8: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Progression Assets/JoseonHunter/Tests/EditMode/WeaponAffixRollerTests.cs*
git commit -m "feat: add deterministic run weapon affixes"
```

---

### Task 2: Runtime Modifiers and Shared Status Service

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponRuntimeModifiers.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponAffixStatusService.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponRuntimeController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/CombatDamageService.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/WeaponMechanics.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/CombatDamageServiceTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponRuntimeModifierTests.cs`

**Interfaces:**
- Consumes: `WeaponRunAffixProfile`.
- Produces: `WeaponRuntimeModifiers.From(WeaponRunAffixProfile)`, `ScaleDamage`, `ScaleCooldown`, `ScaleArea`, `ScaleSpeed`, `ScaleDuration`, and `HasPotential`.
- Produces: `WeaponAffixStatusService.ApplyPeriodic`, `ApplyVulnerability`, `Tick`, `ClearTarget`, and `Reset`.
- Produces: `WeaponRuntimeController.AffixStatuses`.

- [ ] **Step 1: Write failing modifier aggregation tests**

```csharp
[Test]
public void Repeated_damage_rolls_stack_additively_before_scaling()
{
    var profile = Profile(
        Roll(WeaponAffixStat.Damage, .10f),
        Roll(WeaponAffixStat.Damage, .30f),
        Roll(WeaponAffixStat.Cooldown, .12f));

    var modifiers = WeaponRuntimeModifiers.From(profile);
    Assert.That(modifiers.ScaleDamage(100f), Is.EqualTo(140f).Within(.001f));
    Assert.That(modifiers.ScaleCooldown(2f), Is.EqualTo(1.76f).Within(.001f));
}
```

- [ ] **Step 2: Write failing periodic damage and vulnerability tests**

Build a registered target, apply poison only after a confirmed seed contact, tick `.49f` then `.01f`, and assert damage occurs only at the boundary. Add a pargap vulnerability test asserting unrelated later weapon damage is multiplied by `1.2` for `2.0s`, then returns to normal.

- [ ] **Step 3: Implement identity-safe modifiers**

`default(WeaponRuntimeModifiers)` must behave as identity, preserving every existing direct constructor call in tests.

```csharp
public float ScaleCooldown(float value) =>
    Mathf.Max(.01f, value * (1f - Mathf.Clamp(CooldownReduction, 0f, .75f)));
public bool HasPotential(WeaponPotentialId id) => potentialIds != null && potentialIds.Contains(id);
```

- [ ] **Step 4: Implement status requests and contact provenance**

`PeriodicEffectRequest` contains source weapon, target runtime ID, stored confirmed contact point, damage per tick, `0.5s` interval, remaining ticks, and its own attack instance. Status application APIs reject `confirmedContact == false`, missing/dead targets, non-finite points, and non-positive durations.

Add `ContactPhase.Poison`, `Burn`, `Bleed`, `PotentialBlast`, and `PotentialChain`.

- [ ] **Step 5: Integrate status ticking and incoming vulnerability**

`WeaponRuntimeController.Tick` advances `AffixStatuses` before executors. `CombatDamageService` asks the service for a target multiplier before creating the final `DamageResult`; periodic-effect phases do not recursively reapply vulnerability or seed new statuses.

- [ ] **Step 6: Run modifier/status tests and existing combat damage tests**

Expected: old damage events keep the same values under identity modifiers; status ticks retain weapon ID, event-time boss metadata, contact point, and unique attack identity.

- [ ] **Step 7: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat Assets/JoseonHunter/Scripts/Domain/Combat/WeaponMechanics.cs Assets/JoseonHunter/Tests/EditMode
git commit -m "feat: add weapon affix runtime modifiers"
```

---

### Task 3: Progression, Run Lifecycle, and Executor Rebuild Integration

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixProgressionPlayModeTests.cs`

**Interfaces:**
- Consumes: Task 1 state/roller and Task 2 modifiers.
- Produces: `ProgressionRewardEvent.AffixResult`.
- Produces: `WeaponSlotView.GeneralAffixSummary`, `WeaponSlotView.PotentialIds`.
- Appends optional `WeaponRuntimeModifiers modifiers = default` to every executor constructor.

- [ ] **Step 1: Write failing new-weapon, level-up, evolution, and reset tests**

```csharp
[UnityTest]
public IEnumerator Every_weapon_choice_rolls_once_but_evolution_does_not_reroll()
{
    var controller = BuildControllerWithDeterministicAffixRandom();
    controller.OpenUpgradeForTests();
    ChooseWeapon(controller, WeaponId.GakgungShot);
    Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(1));

    controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 5);
    controller.AcquireEvolutionForTests("gakgung_sun_piercer");
    Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(1));
    yield return null;
}
```

Add a reset test that stores three potential lines, calls `ResetRunForTests`, and asserts no profile remains.

- [ ] **Step 2: Inject deterministic roll creation without exposing production mutable state**

Production uses `SeededAffixRandom(WeaponAffixRoller.StableSeed(weaponId, level, kills, affixRollOrdinal++))`. Under `UNITY_INCLUDE_TESTS`, expose a setter for an `IAffixRandom` factory and read-only profile snapshots.

- [ ] **Step 3: Roll after every `UpgradeKind.Weapon` application**

Apply the base level first, then call the roller, then rebuild once. Extend `ProgressionRewardEvent` with a nullable/reference `WeaponAffixRollResult AffixResult`; support and evolution rewards pass `null`.

- [ ] **Step 4: Build immutable UI snapshots**

`WeaponSlotView` copies potential IDs and formats general totals such as `공격 +17% · 범위 +12%`. Add potential count and tier to `WeaponSignature` so the rack rerenders after a roll even if level/icon are unchanged.

- [ ] **Step 5: Pass modifiers into all eight executors**

Each constructor scales only the values it owns:

```csharp
BaseDamage = modifiers.ScaleDamage(baseDamage);
CooldownSeconds = modifiers.ScaleCooldown(cooldownSeconds);
Range = modifiers.ScaleArea(range);
Speed = modifiers.ScaleSpeed(speed);
Duration = modifiers.ScaleDuration(duration);
Potentials = modifiers;
```

Do not change existing level/evolution multipliers. Default modifiers must leave all old tests unchanged.

- [ ] **Step 6: Prove rebuild persistence and no duplicate registration**

After five forced rolls and one evolution, assert the old runtime is disposed, exactly eight or the owned subset of executors is registered once, the new executor exposes the same potential set, and no roll count changes during rebuild.

- [ ] **Step 7: Run Runtime + full PlayModeTests response-file compilation and focused progression tests**

Expected: both assemblies exit `0`; if Test Runner XML is absent, record compile success only.

- [ ] **Step 8: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime Assets/JoseonHunter/Tests/PlayMode/WeaponAffixProgressionPlayModeTests.cs*
git commit -m "feat: integrate run affixes with weapon progression"
```

---

### Task 4: PixelLab Asset Batch and Contact-Mask Contracts

**Files:**
- Create: `Docs/Assets/pixellab-affix-generation-ledger.csv`
- Create: `Docs/Assets/pixellab-affix-asset-manifest.md`
- Create: `ArtSource/Pixel/UI/AffixJackpot/slot-kit.png`
- Create: `ArtSource/Pixel/UI/AffixJackpot/status-symbols.png`
- Create: `ArtSource/Pixel/Weapons/Potentials/potential-parts-a.png`
- Create: `ArtSource/Pixel/Weapons/Potentials/potential-parts-b.png`
- Create: matching `prompt.md` and `provenance.json` files beside each approved source
- Create: `ArtSource/Pixel/Weapons/Potentials/potential-parts-a-hit-mask.png`
- Create: `ArtSource/Pixel/Weapons/Potentials/potential-parts-b-hit-mask.png`
- Create/import: `Assets/JoseonHunter/Art/UI/AffixJackpot/*`
- Create/import: `Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/*`
- Create: `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponAffixPresentationCatalogAsset.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponAffixPixelAssetImporter.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixPixelAssetContractTests.cs`

**Interfaces:**
- Produces: `WeaponAffixPresentationCatalogAsset.SpriteForAffix`, `SpriteForPotential`, `MaskForPotential`, and rarity frame sprites.
- Produces four approved PixelLab source atlases with no readable text or human figure.

- [ ] **Step 1: Write the exact four-atlas manifest before spending generations**

The manifest fixes:

- `slot-kit.png`: 256×128, transparent; reel frame, standard/high/perfect borders, 1/2/3-line jackpot bursts, no text.
- `status-symbols.png`: 256×128; poison, burn, frost, bleed, armor-break, seal-transfer, lightning-mark, experience symbols.
- `potential-parts-a.png`: 256×128; Hwando, Gakgung, Talisman, Thunder potential parts.
- `potential-parts-b.png`: 256×128; Jangseung, Singijeon, Frost, Fan potential parts.

Each cell is documented with pixel bounds and whether it is decorative-only or damage-active.

- [ ] **Step 2: Discover PixelLab tools, check balance once, and record the baseline**

Use tool discovery for PixelLab image generation/status/balance. Append the returned balance and timestamp to `pixellab-affix-generation-ledger.csv`. Do not generate until all four prompts are committed in the working tree.

- [ ] **Step 3: Generate the four atlases as one controlled batch**

Use the existing Joseon weapon style lock, transparent background, hard 1-pixel edges, master palette, no antialiasing, no panels containing text, no human silhouettes. Allow at most one targeted retry per rejected atlas; never reroll an approved atlas for cosmetic preference.

- [ ] **Step 4: Inspect every source at original resolution**

Reject any atlas with opaque background, readable pseudo-text, panel-like weapon parts, human figures, blended alpha, or ambiguous cell boundaries. Record job ID, prompt revision, cost, status, remaining balance, and approved destination.

- [ ] **Step 5: Author explicit active hit masks**

Damage-active cells include only blade shadow bodies, split arrows, ghost-flame body, ground-crack lightning core, rotating ward edge, submunitions, frost spread core, and chain-lightning core. Exclude poison droplets, glow, smoke, trails, telegraphs, rarity bursts, and every UI cell.

- [ ] **Step 6: Import point-filtered sprites and build the presentation catalog**

Use multiple-sprite import with manifest cell rectangles, alpha transparency, no compression, and project-consistent pixels-per-unit. The catalog is stored at `Assets/JoseonHunter/Resources/WeaponAffixPresentationCatalog.asset` so runtime bootstrap needs no scene reference migration.

- [ ] **Step 7: Write and run asset contract tests**

Tests assert exact atlas dimensions, alpha values only `0/255`, mask pixels are a subset of source alpha, UI masks are absent, every one of 24 potential IDs resolves a sprite, and every damage-active potential resolves a nonempty mask.

- [ ] **Step 8: Commit**

```powershell
git add Docs/Assets ArtSource/Pixel/UI/AffixJackpot ArtSource/Pixel/Weapons/Potentials Assets/JoseonHunter/Art/UI/AffixJackpot Assets/JoseonHunter/Art/Weapons/Runtime/Potentials Assets/JoseonHunter/Resources Assets/JoseonHunter/Scripts/Content/Weapons Assets/JoseonHunter/Scripts/Editor/AssetProduction Assets/JoseonHunter/Tests/EditMode/WeaponAffixPixelAssetContractTests.cs*
git commit -m "feat: add PixelLab affix jackpot assets"
```

---

### Task 5: Fast Slot Reveal and HUD Potential Lines

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs`

**Interfaces:**
- Consumes: `ProgressionRewardEvent.AffixResult`, `WeaponSlotView` summaries, Task 4 presentation catalog.
- Produces: `WeaponAffixRevealPresenter.Play`, `Skip`, `HideImmediately`, `RevealCompleted`, `IsRevealing`, and the test-visible read-only `LastCompletedResult`.

- [ ] **Step 1: Write failing timing, skip, and sequencing tests**

```csharp
[UnityTest]
public IEnumerator Standard_roll_auto_closes_within_one_second()
{
    presenter.Play(Result(WeaponAffixTier.Standard, potentialCount: 0));
    yield return AdvanceUnscaled(.96f);
    Assert.That(presenter.IsRevealing, Is.False);
}

[UnityTest]
public IEnumerator Three_line_jackpot_can_skip_without_changing_result()
{
    var result = Result(WeaponAffixTier.Perfect, potentialCount: 3);
    presenter.Play(result);
    presenter.Skip();
    yield return AdvanceUnscaled(.7f);
    Assert.That(presenter.LastCompletedResult, Is.EqualTo(result));
}
```

- [ ] **Step 2: Build the presenter from PixelLab sprites**

The presenter uses the slot frame, clipped reel symbols, rarity border, three potential rows, and jackpot burst sprites. Text labels display the localized stat/potential names, but no temporary glyph replaces a missing sprite.

- [ ] **Step 3: Implement exact unscaled timing**

Use one unscaled phase clock. Standard duration is `.95f`; High is `1.15f`; Perfect is `1.35f`; potentials override to `1.3f`, `1.6f`, or `1.9f`. Tension begins only for High, Perfect, or any potential result. Skip remaps remaining phase durations but never calls the roller.

- [ ] **Step 4: Sequence upgrade close and reveal without adding confirmation input**

For weapon rewards, `FirstPlayableUiBootstrap` waits for choice-close and affix-reveal completion. The old generic weapon-level reveal is suppressed to avoid playing two consecutive reward panels; support and evolution retain their existing reveal. Queued upgrades open immediately after completion.

- [ ] **Step 5: Add HUD summaries**

Each rack slot displays one compact totals line and three 18×18 potential cells. Locked cells use the PixelLab empty-line frame; unlocked cells show their potential sprite. A newly opened line pulses only its cell and weapon accent.

- [ ] **Step 6: Run PlayMode UI tests and compile Presentation + PlayModeTests**

Assert no duplicate EventSystem, no double notification, `Time.timeScale` restoration, safe-area containment at 1080×1920, skip idempotence, and run-reset cancellation.

- [ ] **Step 7: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/UI Assets/JoseonHunter/Tests/PlayMode
git commit -m "feat: add fast weapon affix jackpot reveal"
```

---

### Task 6: Hwando, Gakgung, Talisman, and Thunder Potentials

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatAPlayModeTests.cs`

**Interfaces:**
- Consumes: modifiers, shared status service, Task 4 potential sprites/masks.
- Produces telemetry under the existing PlayMode test assembly for spawned potential attacks, target order, status ticks, and attack IDs.

- [ ] **Step 1: Write failing contact-negative tests for all 12 potentials**

Every potential test first supplies overlapping bounds with non-overlapping masks and asserts zero extra damage/status/spawn, then moves to confirmed overlap and asserts the defined effect.

- [ ] **Step 2: Implement Hwando potentials**

- `독아`: confirmed blade contact schedules three poison ticks, each `20%` of modified base damage at `0.5s`; recontact refreshes remaining ticks but does not create parallel poison stacks.
- `잔영회수`: confirmed inbound contact spawns one same-path shadow after `0.12s`, dealing `55%` modified base damage once per target with a new attack ID.
- `비검연무`: within one cast, each newly confirmed distinct target adds `+15%` damage to subsequent blade contacts, capped at `+60%`; outbound and inbound share the cast counter.

- [ ] **Step 3: Implement Gakgung potentials**

- `파갑촉`: the first confirmed target per primary arrow receives `+20%` incoming damage for `2.0s`; side arrows cannot apply it.
- `갈래깃`: each primary confirmed impact spawns two ±25° arrows at `45%` damage, `65%` range, one impact each; children never split.
- `만력장궁`: damage scales linearly from `1.0×` to `1.6×` over the first `80%` of allowed travel and projectile scale from `1.0×` to `1.35×`.

- [ ] **Step 4: Implement Talisman potentials**

- `오행순환`: casts rotate fire, ice, lightning. Fire schedules three `15%` burn ticks; ice applies the existing frost slow for `1.2s`; lightning chains once to the nearest other live target within `2.5` world units for `60%` damage after confirmed seal contact.
- `봉인전이`: when a sealed target dies, transfer once to the nearest live unsealed target within `4.0` units; no contact damage occurs during transfer.
- `역귀폭부`: sealed-target death spawns a ghost flame that seeks the nearest live target for `0.6s`; only its Task 4 mask contact deals `75%` damage once.

- [ ] **Step 5: Implement Thunder potentials**

- `지맥잔뢰`: after the main blast, wait `0.35s`, then strike the original blast center for `65%` damage using the crack-lightning mask.
- `뇌심과충전`: count unique live targets actually moved during Pull; add `+8%` compressed-blast damage per target, capped at `+80%`.
- `피뢰표식`: mark the highest `ThreatScore` target touched by the main blast; after `0.45s`, a vertical mask strike deals `90%` damage if the same target remains live.

- [ ] **Step 6: Prove normal/evolved compatibility and attack identity**

Run each potential once on normal and evolved constructors. Assert child attacks cannot recurse, delayed attacks skip dead/unregistered targets, and each repeated-hit rule matches its description.

- [ ] **Step 7: Run focused tests or compile Runtime + full PlayModeTests once**

Expected: zero C# errors. Do not loop on LicenseClient/XML failures.

- [ ] **Step 8: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatAPlayModeTests.cs*
git commit -m "feat: add first twelve weapon potentials"
```

---

### Task 7: Jangseung, Singijeon, Frost, and Fan Potentials

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatBPlayModeTests.cs`

**Interfaces:**
- Consumes: modifiers, status service, Task 4 sprites/masks.
- Produces telemetry for potential state order, contact phases, target IDs, and delayed times.

- [ ] **Step 1: Write failing contact-negative and frame-split tests for all 12 potentials**

For delayed and rotating effects, compare one large tick with split ticks and assert identical attack order/timestamps.

- [ ] **Step 2: Implement Jangseung potentials**

- `귀면장승`: each marked target crossing pulse applies knockback away from ward center with force `1.25`; no confirmed pulse means no movement.
- `사방결계`: after the fourth post completes, rotate one finite boundary segment through 360° over `0.8s`; a target can take `70%` damage once per full rotation only on mask-confirmed segment contact.
- `수호신강림`: ward completion spawns one guardian for `1.2s`; it selects the highest-threat marked live target and performs one `110%` mask-confirmed strike, then retires.

- [ ] **Step 3: Implement Singijeon potentials**

- `화약궤적`: each rocket leaves finite trail cells for `0.6s`; a target takes `15%` burn damage per `0.3s` at most twice per trail, only after crossing an active trail mask.
- `자탄분열`: a focus rocket's first confirmed hit spawns three submunitions at -30°, 0°, +30°, each `35%` damage, `55%` range, and one impact; scouts and children do not split.
- `연쇄점화`: when focus damage kills a target, unlaunched focus rockets retarget once to the next deterministic densest centroid; already launched rockets keep their path.

- [ ] **Step 4: Implement Frost potentials**

- `균열표식`: confirmed field residence adds one vulnerability stack each `0.5s`, max three; expiry spike gains `+25%` damage per stack and consumes stacks.
- `서리전염`: a confirmed expiry spike emits a `1.5`-unit frost pulse that starts residence at `0.25s` on nearby live targets; it cannot recursively infect from the same attack instance.
- `빙무`: field radius grows linearly from `1.0×` to `1.5×` over its duration; visuals and contact mask use the same scale.

- [ ] **Step 5: Implement Fan potentials**

- `진공인`: confirmed wind contact schedules four bleed ticks at `15%` modified base damage every `0.4s`; repeated wind refreshes, not stacks.
- `원뢰증폭`: outward lightning multiplier is `1.0 + clamp(projection/range,0,1) * .75`; inbound keeps its existing reduced multiplier after applying this distance term.
- `회천연쇄`: an inbound kill schedules one nearest surviving marked target within `3.0` units after `0.08s` for `50%` damage; one chain maximum per cast.

- [ ] **Step 6: Validate skip, recursion, and frame independence**

Assert missing targets do not stall a sequence, potential children do not create grandchildren, normal and evolved identities remain intact, and 0.08/0.3/0.4/0.5/0.6/0.8-second boundaries carry residual delta exactly once.

- [ ] **Step 7: Run focused tests or compile Runtime + full PlayModeTests once**

Expected: zero C# errors and no source-level test contradictions under static review.

- [ ] **Step 8: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatBPlayModeTests.cs*
git commit -m "feat: add remaining weapon potentials"
```

---

### Task 8: Full Integration, Balance Invariants, and Handoff

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatRuleTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableUiStatePlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixVerticalSlicePlayModeTests.cs`
- Modify: `Docs/Superpowers/Specs/2026-07-28-run-weapon-affix-jackpot-design.md` only if implementation discovered a concrete corrected invariant.

**Interfaces:**
- Consumes every preceding task.
- Produces one end-to-end proof from weapon card selection through roll reveal, executor rebuild, confirmed potential damage, evolution inheritance, and run reset.

- [ ] **Step 1: Add catalog invariants**

Assert eight weapons, three distinct potentials each, exact stat ranges, exact jackpot thresholds, every potential presentation sprite, required damage masks, and no potential ID assigned to more than one weapon.

- [ ] **Step 2: Add a deterministic vertical slice**

Force a Perfect damage roll plus three Hwando potentials, choose the card through `IPointerClickHandler`, skip the reveal, hit registered production-like targets with actual masks, evolve to Moon Eclipse, and assert:

- the same three potential IDs survive rebuild/evolution;
- poison, shadow, and distinct-target ramp produce confirmed events with distinct attack identities;
- rack shows three filled potential cells;
- queued upgrade opens only after reveal completion;
- reset clears every roll, delayed status, presentation pulse, and potential child.

- [ ] **Step 3: Run static and compile verification**

```powershell
git diff --check 526de47..HEAD
```

Compile current Unity response files for `JoseonHunter.Runtime`, `JoseonHunter.Presentation`, `JoseonHunter.EditModeTests`, and `JoseonHunter.PlayModeTests`. All must exit `0`; obsolete Unity API warnings may be recorded but no new warnings from affix code are accepted.

- [ ] **Step 4: Attempt the relevant Unity suites once**

Run EditMode filters for affix/catalog/mask tests and PlayMode filters for reveal, progression, potential A/B, and vertical slice. If LicenseClient prevents XML, do not claim passes and do not repeatedly invoke the blocked runner.

- [ ] **Step 5: Perform one integrated read-only review**

Review `526de47..HEAD` against the design and this plan. Block on unconfirmed-contact damage, duplicate attack identities, rerolls during rebuild/evolution, results exceeding three lines, ordinary reveal duration above one second, missing PixelLab assets, or generated report artifacts.

- [ ] **Step 6: Apply one focused fix wave and rerun compile verification**

Only address review findings inside this feature scope. Restore Unity-imported unrelated art `.meta` and `ProjectSettings` changes. Preserve the eight known untracked weapon-directory metas.

- [ ] **Step 7: Commit final fixes**

```powershell
git add Assets/JoseonHunter ArtSource/Pixel Docs/Assets Docs/Superpowers
git commit -m "fix: close weapon affix jackpot review findings"
```

- [ ] **Step 8: Report evidence honestly**

List implemented mechanics, PixelLab generations/cost/remaining balance, compilation exits, executed test counts if XML exists, missing runtime evidence if it does not, branch name, worktree path, and any required manual 1080×1920 visual check.
