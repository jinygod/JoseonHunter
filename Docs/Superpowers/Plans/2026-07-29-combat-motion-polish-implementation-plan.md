# Combat Motion Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace sliding single-frame combatants with readable multi-frame movement, weighted hit reactions, clean camera motion, and distinct motion timing for all eight weapons.

**Architecture:** Keep gameplay authority on stable logical roots and place animation on child visual pivots. A lightweight sprite-frame player and deterministic motion state provide reusable presentation behavior without Animator Controller proliferation; a single `CombatMotionLibrary` ScriptableObject owns authored frame sets. Weapon executors retain their current damage/contact authority and receive only trajectory and presentation timing changes.

**Tech Stack:** Unity 6000.5.5f1, C# MonoBehaviours/plain state, URP 2D, Pixel Perfect Camera, PixelLab frame generation, Unity Test Framework.

## Global Constraints

- Runtime art remains one transparent PNG per animation frame; no runtime sprite sheets or contact sheets.
- All pixel sprites keep Point filtering, no mipmaps, no lossy compression, PPU 64.
- Logical position and pixel-contact authority must not inherit visual bob, squash, recoil, or death motion.
- Existing eight-weapon IDs, attack instances, damage service, affixes, evolution rules, and upgrade flow remain unchanged.
- Target baseline is 360 x 800 portrait and Android with up to 48 active enemies.
- No per-frame LINQ, hierarchy search, material creation, or collection allocation in motion hot paths.

---

### Task 1: Deterministic Motion State

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantMotionState.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/CombatantMotionStateTests.cs`

**Interfaces:**
- Produces: `CombatantMotionState.Step(Vector2 desiredVelocity, float deltaTime, MotionWeight weight)`
- Produces: `CombatantMotionPose` with `VisualOffset`, `TiltDegrees`, `Scale`, `FacingLeft`, `NormalizedSpeed`, and `FootstepPulse`
- Produces: `CombatantMotionState.Hit(Vector2 incomingDirection, float strength)` and `Kill()`

- [ ] **Step 1: Write failing movement tests**

```csharp
[Test]
public void Step_AcceleratesAndSettlesWithoutMovingLogicalPosition()
{
    var state = new CombatantMotionState(0f);
    var moving = state.Step(Vector2.right * 2f, 0.05f, MotionWeight.Light);
    var stopped = state.Step(Vector2.zero, 0.20f, MotionWeight.Light);
    Assert.That(moving.NormalizedSpeed, Is.GreaterThan(0f));
    Assert.That(Mathf.Abs(stopped.TiltDegrees), Is.LessThan(Mathf.Abs(moving.TiltDegrees)));
}
```

- [ ] **Step 2: Run the focused test and confirm it fails**

Run: `Tools/Unity/Test-Unity.ps1 -TestFilter CombatantMotionStateTests`

Expected: compilation failure because `CombatantMotionState` does not exist.

- [ ] **Step 3: Implement allocation-free motion state**

Implement exponential acceleration/settling, phase-based footstep bounce, weight-specific amplitude, hit recoil decay, and a death progress state. Clamp visual offset below `0.12` world units and tilt below `4` degrees.

- [ ] **Step 4: Add hit and weight tests**

Verify heavy motion has slower cadence and smaller idle bob, hit recoil decays to zero, direction flip uses a dead zone, and no pose field becomes NaN for zero delta.

- [ ] **Step 5: Run focused EditMode tests**

Run: `Tools/Unity/Test-Unity.ps1 -TestFilter CombatantMotionStateTests`

Expected: all motion-state tests pass.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantMotionState.cs Assets/JoseonHunter/Tests/EditMode/CombatantMotionStateTests.cs
git commit -m "feat: add deterministic combatant motion state"
```

---

### Task 2: Frame Library and Visual Rig

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatMotionLibrary.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/CombatMotionLibraryTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Consumes: `CombatantMotionPose`
- Produces: `CombatMotionLibrary.Find(Sprite referenceSprite)`
- Produces: `CombatantVisualRig.Create(GameObject logicalRoot, Sprite initialSprite, int sortingOrder, CombatMotionSet motionSet, MotionWeight weight)`
- Produces: `CombatantVisualRig.Tick(Vector2 desiredVelocity, float deltaTime)`
- Produces: `CombatantVisualRig.ShowHit(Vector2 incomingDirection, float strength)` and `PlayDeath()`
- Produces: stable `PixelMaskTransform CollisionTransform(Float2 logicalPosition)`

- [ ] **Step 1: Write failing catalog tests**

Create an in-memory library with one entry and assert lookup by reference sprite returns that entry, missing sprites return the static fallback, and empty frame arrays never throw.

- [ ] **Step 2: Run focused tests and confirm failure**

Run: `Tools/Unity/Test-Unity.ps1 -TestFilter CombatMotionLibraryTests`

Expected: compilation failure because the library does not exist.

- [ ] **Step 3: Implement the library**

Use serializable `CombatMotionSet` entries containing reference sprite, idle frames, move frames, idle FPS, move FPS, and `MotionWeight`. Cache lookup in a dictionary during `OnEnable`; do not search asset paths at runtime.

- [ ] **Step 4: Implement the visual rig**

Create `Visual Pivot` and its `SpriteRenderer` below the logical root. Advance frames from speed and unscaled phase seed, apply pose only to the pivot, preserve the renderer's base color, and expose stable collision scale/flip independent of pivot animation.

- [ ] **Step 5: Adapt controller object creation**

Return the root plus its rig for player and enemy creation. Leave pickups and treasure static. Store the rig in `EnemyState`; stop calling `GetComponent<SpriteRenderer>()` on moving roots.

- [ ] **Step 6: Run catalog and existing pixel-contact tests**

Run: `Tools/Unity/Test-Unity.ps1 -TestFilter "CombatMotionLibraryTests|PixelMaskContactServiceTests|MobilePixelArtImportTests"`

Expected: all selected tests pass and collision scale remains unchanged during visual squash.

- [ ] **Step 7: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatMotionLibrary.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/EditMode/CombatMotionLibraryTests.cs
git commit -m "feat: separate combat logic roots from animated visuals"
```

---

### Task 3: PixelLab Character Frame Production

**Files:**
- Create: `Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa/Idle/*.png`
- Create: `Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa/Walk/*.png`
- Create: `Assets/JoseonHunter/Art/Animation/Enemies/<EnemyId>/Walk/*.png`
- Create: `Assets/JoseonHunter/Art/Animation/Elites/DokkaebiCaptain/*.png`
- Create: `Assets/JoseonHunter/Art/Animation/Bosses/FallenGeneral/*.png`
- Modify: `Docs/Assets/pixellab-mobile-polish-generation-ledger.csv`

**Interfaces:**
- Consumes: the approved reference PNG for each combatant.
- Produces: individually named frames `idle_00.png`, `walk_00.png`, and so on, all matching the reference canvas and palette.

- [ ] **Step 1: Queue PixelLab animation jobs**

Generate:

- Han Yeonhwa: 4-frame subtle idle and 8-frame in-place walk.
- Five normal enemies: one 4-frame walk loop each.
- Dokkaebi Captain: 4-frame heavy walk and 4-frame warning idle.
- Fallen General: 4-frame heavy walk and 4-frame breathing idle.

Use transparent backgrounds, the current sprite as first frame, and fixed per-character seeds.

- [ ] **Step 2: Poll jobs and inspect previews**

Reject sets with identity drift, canvas cropping, changing weapon/clothing identity, or opaque backgrounds. Retry only the affected character with a more constrained action description.

- [ ] **Step 3: Save frames as individual PNG files**

Keep index zero only when it closes the loop cleanly. Never save a combined strip or sheet under runtime `Assets/`.

- [ ] **Step 4: Import and validate**

Run Unity asset refresh and the single-PNG validator. Verify every frame uses Point filter, PPU 64, no mipmaps, transparent alpha, and one sprite per file.

- [ ] **Step 5: Update the PixelLab ledger**

Record job ID, prompt/action, seed, canvas, selected frame count, output path, and rejected/accepted status.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Art/Animation Docs/Assets/pixellab-mobile-polish-generation-ledger.csv
git commit -m "art: add multi-frame combatant animations"
```

---

### Task 4: Catalog Assembly and Combatant Integration

**Files:**
- Create: `Assets/JoseonHunter/Content/Motion/CombatMotionLibrary.asset`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- Modify: `Assets/JoseonHunter/Scenes/Gameplay.unity`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Consumes: `CombatMotionLibrary` and individual frame PNG sprites.
- Produces: populated scene reference `FirstPlayableController.motionLibrary`.

- [ ] **Step 1: Extend scene generation**

Add an editor-only helper that loads frames from exact approved directories, sorts by numeric suffix, builds/updates the one motion library asset, assigns reference sprites, and wires the Gameplay controller.

- [ ] **Step 2: Generate the catalog through Unity**

Run the existing First Playable scene generator or a narrow Unity command that invokes the new helper. Save assets and the Gameplay scene.

- [ ] **Step 3: Integrate player locomotion**

Feed actual velocity to the player rig, use motion-state facing rather than raw instantaneous input, and update the camera in `LateUpdate`.

- [ ] **Step 4: Integrate enemy locomotion**

Seed phase from runtime target ID, feed each enemy's real velocity, use weight profiles by rank, and keep health bars on the logical root.

- [ ] **Step 5: Integrate hit and death reactions**

On confirmed damage, derive recoil from contact point to logical position. Mark dead immediately for gameplay and rewards, then retain only a detached visual corpse for up to `0.30` seconds.

- [ ] **Step 6: Improve camera follow**

Replace frame-rate-dependent `Lerp` with `SmoothDamp`, add at most `0.35` world-unit look-ahead from movement, and retain render-scoped combat impulse.

- [ ] **Step 7: Validate scene references**

Inspect the saved scene and catalog to confirm no missing sprites, no broken GUIDs, and all seven monster ranks plus player resolve a motion set.

- [ ] **Step 8: Commit**

```powershell
git add Assets/JoseonHunter/Content/Motion Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs Assets/JoseonHunter/Scenes/Gameplay.unity Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs
git commit -m "feat: animate player and enemy combat motion"
```

---

### Task 5: Eight-Weapon Motion Pass

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponMotionCurves.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponMotionCurvesTests.cs`
- Modify: all eight executor files under `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/`

**Interfaces:**
- Produces: allocation-free `WeaponMotionCurves.EaseOutCubic`, `EaseInCubic`, `Arc`, `Pulse`, and `Stagger01`
- Consumes: current executor position/timing and returns the actual presentation/contact position.

- [ ] **Step 1: Write failing curve tests**

Assert endpoints remain exact, easing is monotonic, arc returns zero at both endpoints, and stagger values remain within `[0, 1]`.

- [ ] **Step 2: Implement curve helpers**

Use pure static math without animation curves or allocations.

- [ ] **Step 3: Polish the two primary weapons**

Keep the existing curved Hwando and Gakgung logic, add launch anticipation of at most `0.08` seconds, improve acceleration, and ensure the first damage-capable frame remains the first confirmed pixel contact.

- [ ] **Step 4: Polish the six remaining weapons**

Apply the motion identities specified in the design: floating talisman chain, ballistic thunder bomb, rising ward settle, staggered singijeon, spinning frost flask, and delayed fan/thunder propagation.

- [ ] **Step 5: Verify contact authority**

Run weapon executor tests and assert decoration objects never register damage, while actual curved positions are supplied to mask contact.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons Assets/JoseonHunter/Tests/EditMode/WeaponMotionCurvesTests.cs
git commit -m "feat: polish eight weapon motion identities"
```

---

### Task 6: Feedback Budget and Runtime Validation

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPool.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/CombatMotionSmokeTests.cs`

**Interfaces:**
- Consumes: `CombatDamageService.DamageConfirmed`.
- Produces: bounded impact flash, hit stop, camera impulse, and damage-number motion.

- [ ] **Step 1: Tune the feedback budget**

Normal hits get no hit stop or camera shake; critical/kill hits get `0.025-0.035` seconds; boss kill gets a capped stronger impulse. Prevent continuous weapons from retriggering hit stop every frame.

- [ ] **Step 2: Clean damage-number motion**

Give numbers a short upward ease with slight horizontal spread, merge bursts from the same target within a short window when necessary, and cap the active pool without allocations.

- [ ] **Step 3: Add a PlayMode smoke test**

Load Gameplay, assert the player visual pivot exists, spawn normal/elite/boss targets, advance frames, confirm their logical roots move while visual offsets remain bounded, and confirm no missing references.

- [ ] **Step 4: Compile and run focused tests**

Run the motion, pixel contact, weapon executor, import, and PlayMode smoke tests.

- [ ] **Step 5: Run an Editor Play Mode visual check**

Check idle, movement, direction changes, a 48-enemy crowd, elite and boss weight, all eight weapon attacks, confirmed-contact damage numbers, pause/upgrade transitions, and restart.

- [ ] **Step 6: Inspect performance**

Use Unity Profiler or Timeline during the 48-enemy check. Confirm the new motion loop performs no managed allocation per frame and does not create unbounded renderer/effect objects.

- [ ] **Step 7: Final diff review and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/Combat Assets/JoseonHunter/Tests/PlayMode/CombatMotionSmokeTests.cs
git commit -m "feat: finish clean combat motion feedback"
```

