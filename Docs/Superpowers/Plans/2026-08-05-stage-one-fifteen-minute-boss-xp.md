# Stage One Fifteen-Minute Boss and XP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a fifteen-minute first stage with authored wave progression, oversized telegraphed bosses, a natural level-35 cap, and bounded high-value experience pickups.

**Architecture:** Consolidate time-dependent rules in Domain classes and keep Unity object ownership in the existing `FirstPlayableController`. Add small deterministic state machines for boss attacks and pickup aggregation, then let runtime presentation consume those decisions through pooled sprite geometry and existing combat interfaces.

**Tech Stack:** Unity 6000.5.5f1, C#, URP 2D, Unity Test Framework/NUnit, runtime-created SpriteRenderer presentation

## Global Constraints

- Production stage duration is exactly 900 seconds; final boss combat is untimed and normal waves stop at 15:00.
- Active enemies never exceed 140; active experience pickups never exceed 180.
- Midboss scales are approximately 1.7 and 1.9 times normal; final boss scale is approximately 2.3 times normal.
- Boss warnings use dark crimson and transparent muted red with no white outline.
- Pickup attraction radius remains 0.58 world units.
- Maximum player level is 35 and every level-up before the cap offers meaningful eligible choices.
- Preserve unrelated dirty `.meta` whitespace changes.
- Run Unity sequentially at BelowNormal priority with processor affinity mask `15`.

---

### Task 1: Make the fifteen-minute stage timeline authoritative

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/RunRuleTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WaveSpawnDirectorTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/RunClock.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSpawnDirector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Produces: `RunClock.PhaseAt(float seconds, float maximumSeconds)`
- Produces: `WaveSchedule.NormalEntriesAt(float elapsedSeconds)` on the 900-second coordinate system
- Consumes: `StagePacingTimeline.CanonicalDurationSeconds`

- [ ] Write failing tests for phase boundaries at 120, 300, 420, 600, 720, 840, and 900 seconds, rat-only opening, delayed normal introductions, and no packs during the boss warning/fight.
- [ ] Run focused EditMode tests and verify they fail against the current 180-second rules.
- [ ] Replace duplicated 45/90/135/165/180-second constants with the canonical 900-second phase and wave windows; keep preview compression only through explicit duration-aware APIs.
- [ ] Set `FirstPlayableController` production duration to `StagePacingTimeline.CanonicalDurationSeconds` and route all phase/pack/introduction lookups through the same timeline.
- [ ] Re-run focused tests and verify they pass.

### Task 2: Cap progression at 35 and tune the curve

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/RunLoadoutRulesTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/ExperienceProgressionTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/RunLoadoutRules.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/ExperienceCurve.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Produces: `RunLoadoutRules.MaximumPlayerLevel = 35`
- Produces: monotonic `ExperienceCurve.GetThresholdForNextLevel(int level)` values calibrated for the milestone bands

- [ ] Write failing tests proving level 35 is the cap, the curve is monotonic, representative accumulated XP reaches the authored milestone bands, and experience at cap cannot enqueue another upgrade.
- [ ] Run the focused tests and verify RED.
- [ ] Implement the smallest curve/cap changes, guard `AddExperience` and upgrade opening, and stop XP creation at the cap.
- [ ] Re-run focused EditMode and level-up PlayMode tests and verify GREEN.

### Task 3: Add deterministic boss attack decisions

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/BossAttackPattern.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/BossAttackPatternTests.cs`

**Interfaces:**
- Produces: `BossAttackController.Tick(float deltaSeconds, Float2 bossPosition, Float2 playerPosition, float healthFraction)`
- Produces: immutable snapshots containing state, attack kind, locked target, warning duration, and one-shot execution events

- [ ] Write separate failing tests for target locking, no early damage, one-shot execution, recovery, phase-two cadence, and attack rotation.
- [ ] Run the focused test and verify RED because the controller does not exist.
- [ ] Implement the `Chase -> Telegraph -> Execute -> Recover` state machine without Unity dependencies.
- [ ] Re-run the focused test and verify GREEN.

### Task 4: Present oversized midbosses and final boss

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/BossScaleProfile.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/BossScaleProfileTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/StagePacingPlayModeTests.cs`

**Interfaces:**
- Produces: scale multipliers `1.7`, `1.9`, and `2.3`
- Consumes: existing `CombatVisualScaleProfile`, pixel mask, health bar, and spawn-clearance setup

- [ ] Write failing EditMode and PlayMode tests that compare normal, midboss tier 1, midboss tier 2, and final boss rendered/physical footprints.
- [ ] Run tests and verify RED against the existing near-boss scales.
- [ ] Apply scale profiles while preserving sprite aspect ratio and update health-bar offsets and spawn clearance from the resolved footprint.
- [ ] Re-run focused tests and verify GREEN.

### Task 5: Integrate boss telegraphs and attacks

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/BossTelegraphPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/BossCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: `BossAttackController` snapshots
- Produces: pooled circle, corridor, ring, and projectile renderers plus one-shot player damage

- [ ] Write failing PlayMode tests for visible warning geometry, locked safe zones, single-hit charge, slam resolution, projectile gaps, cleanup, pause, and boss death.
- [ ] Run focused PlayMode tests and verify RED.
- [ ] Implement pooled runtime SpriteRenderer geometry below combatants, bridge execution to existing player damage, and disable generic chase while an attack owns movement.
- [ ] Give tier-one midboss only slam, tier-two only charge, and Fallen General all three patterns with phase-two cadence.
- [ ] Re-run focused PlayMode tests and verify GREEN.

### Task 6: Bound and merge experience pickups

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/ExperiencePickupBudget.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/ExperiencePickupBudgetTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePickupRangePlayModeTests.cs`

**Interfaces:**
- Produces: tier classification for values `1-4`, `5-19`, and `20+`
- Produces: merge decisions that preserve total XP with maximum active count `180`

- [ ] Write failing tests for tier boundaries, exact value conservation, nearest bounded merge choice, active cap, and unchanged attraction radius.
- [ ] Run focused tests and verify RED.
- [ ] Add pooled pickup lifecycle, pre-created trails, tier tint/scale, and bounded merge behavior; remove attraction-time `AddComponent<TrailRenderer>` and per-collection destruction.
- [ ] Batch magnet attraction starts and replace the per-kill one-percent magnet roll with milestone-oriented rare drops.
- [ ] Re-run focused EditMode and PlayMode tests and verify GREEN.

### Task 7: Integration, performance, visual validation, and delivery

**Files:**
- Modify: `Docs/Verification/2026-08-05-stage-one-fifteen-minute-boss-xp.md`
- Review all files changed in Tasks 1-6

**Interfaces:**
- Validates the complete stage, boss, progression, and pickup contracts

- [ ] Run focused EditMode suites for timeline, waves, progression, boss state, scale, and pickup budget; require zero failures.
- [ ] Run focused PlayMode suites for stage milestones, boss combat, pickups, UI state, and modal flow; require zero failures.
- [ ] Run the full EditMode suite, then the full PlayMode suite sequentially.
- [ ] Run an accelerated 900-second gameplay simulation and record active enemies, active pickups, boss transitions, maximum level, and managed allocation observations.
- [ ] Capture representative portrait gameplay and boss warnings at 720x1280 and 1080x2340 and inspect readability, scale, clipping, and palette.
- [ ] Build the Android development APK using `Tools/Unity/Build-AndroidDevelopment.ps1`.
- [ ] Review the diff, exclude unrelated whitespace-only `.meta` files, record exact evidence in the verification document, commit coherent checkpoints, push `master`, and verify local/remote hashes match.

