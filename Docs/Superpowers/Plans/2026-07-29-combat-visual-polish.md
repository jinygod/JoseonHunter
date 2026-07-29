# Combat Visual Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a small-scale, broad-view, contact-faithful and mobile-readable first-playable combat presentation.

**Architecture:** Add one gameplay-owned visual scale profile, extend existing combatant and weapon
presentation paths, replace the snapping battlefield presenter, and lengthen the existing affix
reveal state machine. Existing combat executors, pixel-mask damage authority, runtime UI bootstrap
and pooling remain intact.

**Tech Stack:** Unity 6000.5.5f1, URP 2D, C#, uGUI/TMP, NUnit EditMode/PlayMode, PixelLab MCP.

## Global Constraints

- Android landscape composition target is 1920x1080.
- Do not globally reimport all source sprites or change the render pipeline.
- Preserve existing scene references, GUIDs, weapon catalog IDs and pixel-mask damage authority.
- Keep runtime collections, spawned visuals and enemy population bounded.
- Preserve all unrelated dirty user files and metadata.

---

### Task 1: Combat scale profile and battlefield

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatVisualScaleProfile.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BattlefieldTilePresenter.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/CombatVisualScaleProfileTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/BattlefieldTilePresenterPlayModeTests.cs`

**Interfaces:**
- Produces: `CombatVisualScaleProfile.MobileLandscape`, rank scale/contact helpers and stable
  battlefield construction.
- Consumes: existing player/enemy creation and serialized battlefield sprites.

- [ ] **Step 1: Add failing scale-profile tests**

Assert camera area is at least 2.5 times baseline, player screen ratio is between 0.30 and 0.42,
normal/player scale ratio is near one, elite is larger than normal and boss is largest.

- [ ] **Step 2: Run focused EditMode tests and confirm failure**

Run `Tools/Unity/Test-Unity.ps1` with the new fixture filter and expect missing
`CombatVisualScaleProfile`.

- [ ] **Step 3: Implement `CombatVisualScaleProfile`**

Add immutable values for baseline/new camera size, player/normal/elite/boss world scales, contact
radii, spawn annulus, health bars, pickup scale, trail width and weapon presentation multiplier.

- [ ] **Step 4: Apply profile in `FirstPlayableController`**

Replace camera, combatant, chest, health-bar, contact-distance, spawn-distance and geumjul width
constants with profile values. Keep movement units per second stable initially.

- [ ] **Step 5: Replace field snapping**

Build one large world-anchored tiled surface with a muted tint. Disable alternate high-contrast
overlay and cap deterministic sparse decals. `UpdateField` must no longer move the field every two
units.

- [ ] **Step 6: Run focused tests and commit**

Run the scale and battlefield fixtures; commit only Task 1 files.

### Task 2: Combatant readability and collision agreement

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/CombatantVisualRigPlayModeTests.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/EnemyRankProfileTests.cs`

**Interfaces:**
- Consumes: `CombatVisualScaleProfile`.
- Produces: rank-aware visual roots, shadows, player aura and scale-correct collision transform.

- [ ] **Step 1: Add failing hierarchy and collision tests**

Verify shadow/main/aura roles, stable logical root, elite/boss hierarchy and that
`CollisionTransform.Scale` equals the visible logical scale.

- [ ] **Step 2: Run focused tests and confirm failure**

Expect missing visual layers and the old rank scale behavior.

- [ ] **Step 3: Extend `CombatantVisualRig.Create`**

Accept a presentation role, create cached shadow and optional aura/outline children, and expose
read-only visual scale information for validation. Do not allocate in `Tick`.

- [ ] **Step 4: Integrate rank-aware sizes and bars**

Pass player/normal/elite/boss roles from the controller. Position and size health bars from the
profile and retain existing hit/death motion.

- [ ] **Step 5: Run focused tests and commit**

Run combatant and rank fixtures; commit only Task 2 files.

### Task 3: Eight-weapon presentation normalization and Hwando contact motion

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponPresentationScale.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Modify: remaining seven executors under `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponVisualCue.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponVisualCueTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Produces: `WeaponPresentationScale.For(WeaponId, WeaponVisualStage, Sprite)` and consistent
  visible/mask scale application.
- Consumes: existing catalog presentation sprites, pixel hit masks and transient visual pool.

- [ ] **Step 1: Add failing scale and Hwando phase tests**

Verify normalized weapon world bounds, compact VFX caps, Hwando outbound rotation, rebound/return
speed and damage only after pixel contact.

- [ ] **Step 2: Run focused tests and confirm failure**

Expect oversized 32 PPU presentation and missing exposed motion evidence.

- [ ] **Step 3: Implement presentation normalization**

Derive a bounded scale from sprite bounds plus weapon/stage target size. Apply it at acquisition and
transient cue creation without changing catalog assets or IDs.

- [ ] **Step 4: Polish Hwando**

Offset launch slightly toward the target, rotate continuously from velocity, use the checked-in
flight/trail frames, add a short contact flash and recoil, then accelerate the inbound leg. The
pixel mask follows the same position, rotation and scale as the visible blade.

- [ ] **Step 5: Apply bounded profiles to the other seven weapons**

Keep their mechanics intact while limiting projectile/field/impact screen coverage and preserving
their distinct motion signatures.

- [ ] **Step 6: Diagnose the purple “7” in a memory-safe runtime capture**

Log active renderer name, sprite, bounds and owner for any visual exceeding the configured maximum.
Fix the owning executor/profile and remove diagnostic logging after verification.

- [ ] **Step 7: Generate only missing frames with PixelLab**

If capture still shows static or unstable motion, generate the exact approved motion-stage frames
at stable canvas/pivot, save one frame per PNG, and import through the existing asset pipeline.

- [ ] **Step 8: Run weapon fixtures and commit**

Run `WeaponVisualCueTests`, `EightWeaponCombatPlayModeTests`, pixel contact tests and asset-contract
tests; commit Task 3 code/assets only.

### Task 4: Upgrade cards and readable affix slot timeline

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixRevealTimelineTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`

**Interfaces:**
- Produces: a 2.8-4.2 second tier-aware timeline, explicit result confirmation, and compact cards.
- Consumes: existing `WeaponAffixRollResult`, PixelLab slot catalog and UI bootstrap completion event.

- [ ] **Step 1: Add failing timing and confirmation tests**

Assert entrance, spin, deceleration, sequential stops, 1.5-second readable hold, click-to-accelerate
and confirm-to-close semantics for normal/high/perfect and zero-to-three potentials.

- [ ] **Step 2: Run focused tests and confirm failure**

Expect current 0.86-1.96 second automatic completion to fail.

- [ ] **Step 3: Implement the expanded timeline**

Expose phase boundaries and minimum-readable time. A click before reading accelerates to the
earliest valid stop; it never closes the result. A click or confirm button after the hold closes.

- [ ] **Step 4: Polish slot motion and card layout**

Use eased symbol velocity, sequential reel bounce, final hesitation, rarity frame/burst and a
clearly visible confirm button. Reduce card height and spacing without reducing touch targets below
mobile-safe dimensions.

- [ ] **Step 5: Run UI fixtures and commit**

Run timeline, reveal, choice and UI state fixtures; commit Task 4 files only.

### Task 5: Integrated validation and final tuning

**Files:**
- Modify only files proven necessary by capture evidence.
- Add verification record:
  `Docs/Verification/2026-07-29-combat-visual-polish.md`

**Interfaces:**
- Consumes: all prior tasks.
- Produces: verified Gameplay scene, captures and exact console/test evidence.

- [ ] **Step 1: Launch Unity with reduced worker counts**

Use the project Unity version and explicit low worker counts. Confirm at least 3 GB free RAM before
Play Mode.

- [ ] **Step 2: Compile and inspect Console**

Record baseline package warnings separately; fix every new first-party error.

- [ ] **Step 3: Capture 1920x1080 gameplay checkpoints**

Capture base combat, dense enemies, each weapon, the choice screen, slot spin, final result, elite
and boss. Compare object ratios and background stability against the design.

- [ ] **Step 4: Verify contact and timing**

Correlate visible Hwando contact with confirmed damage and verify the slot phases with unscaled
timestamps.

- [ ] **Step 5: Run the relevant EditMode and PlayMode suites**

Run all changed-system fixtures, then the existing 44-test PlayMode and 51-test weapon-mechanics
baselines if memory remains safe.

- [ ] **Step 6: Review diff and commit verification**

Confirm no unrelated dirty files are staged, document exact results/limitations, and commit the
verification record with any evidence-driven final tuning.

