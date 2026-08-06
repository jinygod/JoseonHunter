# Audio Coverage and Weapon Frame Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fill the highest-value missing sound events without noisy normal-enemy death playback, and replace the weapon HUD's bead-like border with a readable hollow quality frame.

**Architecture:** Extend the existing `GameAudioDirector` cue catalog and the existing `FirstPlayableController` event surface; do not add a second audio manager or per-enemy `AudioSource`. Keep the weapon slot in `WeaponRackPresenter`, building its hollow frame from non-raycasting uGUI edge images so the center remains transparent and the quality color reads around the full icon.

**Tech Stack:** Unity 6000.5.5f1, C# 9, uGUI, TextMeshPro, Unity Test Framework, CC0 OGG/WAV audio, Git on `master`.

## Global Constraints

- Work directly on `master`; the user explicitly approved this workflow.
- Commit and push every meaningful completed task to `origin/master`.
- Do not stage or alter the pre-existing CombatChoices and `gakgung_shot/ui-icon.png.meta` changes.
- Do not add a normal-monster death cue; only elite and boss deaths receive dedicated playback.
- Preserve the existing 12-source pooled audio director and its playback budget.
- Import only selected CC0 clips, not complete demo scenes, packages, or unused audio libraries.
- Keep the weapon slot center transparent; stars remain the level indicator, frame color remains the affix-quality indicator, and potential icons move to compact top-right badges.
- Run Unity jobs sequentially at BelowNormal priority with four-core affinity.

---

### Task 1: Hollow weapon quality frame

**Files:**
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`

**Interfaces:**
- Consumes: `WeaponAffixQuality.BandFor(...)`, `WeaponRackPresenter.ColorFor(...)`, and existing `WeaponSlotView` data.
- Produces: child images named `Frame Shadow Top/Bottom/Left/Right`, `Quality Frame Top/Bottom/Left/Right`, four `Quality Corner` accents, and top-right `Potential Cell 0..2` badges.

- [ ] **Step 1: Write the failing PlayMode test**

  Extend `Weapon_rack_shows_level_stars_affix_quality_and_potential_icons` to assert that the four quality-frame edges and four corner accents exist, all quality-frame parts use the expected blue quality color, the slot root image is transparent, and `Potential Cell 0` is anchored at `(1,1)` with a negative X position.

- [ ] **Step 2: Run the focused test to verify RED**

  Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests.Weapon_rack_shows_level_stars_affix_quality_and_potential_icons`.

  Expected: FAIL because `Quality Frame Top` and the other hollow-frame children do not exist.

- [ ] **Step 3: Implement the minimal hollow frame**

  In `WeaponRackPresenter`, replace the filled/sliced `Quality Border` image with a transparent button target and code-built shadow, quality edge, and corner images. Store the quality images in the slot model, recolor all of them in `PopulateSlot`, pulse the frame container, and position the three potential cells vertically at the top-right.

- [ ] **Step 4: Run the focused rack tests to verify GREEN**

  Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests.Weapon_rack`.

  Expected: all weapon-rack tests PASS with no new Console errors.

- [ ] **Step 5: Commit and push**

  Stage only the two files above, commit as `feat: clarify weapon quality frames`, and push `master`.

### Task 2: High-value audio cue contract

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/GameAudioPlaybackBudgetTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/GameAudioAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Audio/GameAudioCueId.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Combat/GameAudioPlaybackBudget.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameAudioClipCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameAudioDirector.cs`

**Interfaces:**
- Produces cues `PlayerHurt`, `PlayerDefeat`, `EliteDefeat`, `WaveWarning`, `EliteAppear`, `BossSlam`, `BossCharge`, `BossVolley`, `TreasureAppear`, `TreasureOpen`, `PauseOpen`, `AppraisalTick`, and `AppraisalReveal`.
- Preserves: no `NormalEnemyDefeat` cue and no per-enemy `AudioSource`.

- [ ] **Step 1: Write failing cue and budget tests**

  Add literal assertions that player defeat and boss attacks have high priority, rapid player-hurt/appraisal-tick requests are throttled, and the enum contains no normal-enemy death cue. Update the asset contract's approved path table with the exact selected clip paths.

- [ ] **Step 2: Run focused EditMode tests to verify RED**

  Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameAudio`.

  Expected: FAIL because the new cue identifiers and audio assets are not present.

- [ ] **Step 3: Add cue mappings and playback policy**

  Add the cue identifiers, catalog mappings, volume bands, combat-cue classification, priority, and minimum intervals to the existing audio classes. Reuse the source pool and reserve high priority for player death, boss attacks, and elite/boss defeat.

- [ ] **Step 4: Import only selected CC0 clips**

  Copy a small curated set from the approved Kenney/OpenGameArt source packs and, where suitable, the approved Unity Asset Store `FREE Casual Game SFX Pack` into `Assets/JoseonHunter/Resources/Audio/CC0/{UI,Combat,Events}`. Let Unity generate metadata and keep clips short, mono, and compressed for mobile.

- [ ] **Step 5: Run focused EditMode tests to verify GREEN**

  Re-run the focused `GameAudio` tests. Expected: PASS and every new catalog path resolves to one or more real clips.

- [ ] **Step 6: Commit and push**

  Stage only the cue code, tests, selected audio, and generated metadata; commit as `feat: expand gameplay sound palette`, and push `master`.

### Task 3: Gameplay and UI audio event wiring

**Files:**
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableAudioPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`

**Interfaces:**
- Consumes: the Task 2 cue identifiers and `GameAudioDirector.TryPlay(GameAudioCueId)`.
- Produces: controller events for player hurt, elite defeat, wave/elite warnings, boss attack kinds, and treasure spawn/open; appraisal presenter events for count ticks and final reveal.

- [ ] **Step 1: Write failing event-integration tests**

  Add PlayMode tests that trigger player damage, an elite death, a boss attack execution, chest spawn/open, pause open, and appraisal progression, then assert the expected cue request count. Add a negative test proving a normal enemy death does not request `EliteDefeat` or any separate death cue.

- [ ] **Step 2: Run the focused PlayMode tests to verify RED**

  Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.FirstPlayableAudioPlayModeTests`.

  Expected: FAIL because the new gameplay and appraisal events are not wired.

- [ ] **Step 3: Wire events at their authoritative transition points**

  Invoke controller events only when health actually decreases, an elite actually dies, a wave announcement starts, a boss attack enters execute, and a chest is created/opened. Subscribe/unsubscribe in `FirstPlayableUiBootstrap`; map `BossAttackKind` to one cue. Emit appraisal ticks only when the displayed integer advances and emit one reveal cue when the tier becomes visible.

- [ ] **Step 4: Run the focused PlayMode tests to verify GREEN**

  Re-run `FirstPlayableAudioPlayModeTests`. Expected: PASS, including the normal-enemy no-death-sound case.

- [ ] **Step 5: Commit and push**

  Stage only the four files above, commit as `feat: wire combat and appraisal audio events`, and push `master`.

### Task 4: Source ledger and full validation

**Files:**
- Modify: `Docs/ThirdPartyAudio/free-audio-source-manifest.md`
- Modify: `Docs/Assets/audio-rights-ledger.csv`
- Create: `Docs/Verification/2026-08-07-audio-coverage-and-weapon-frame.md`

**Interfaces:**
- Consumes: the final imported clip list and Unity validation results.
- Produces: reproducible licensing provenance and verification evidence.

- [ ] **Step 1: Record exact audio provenance**

  For each imported clip, record Unity path, source filename, pack, creator, source URL, license, and approved status. Record the Asset Store package identifier/version if any of its clips are retained.

- [ ] **Step 2: Run full EditMode tests**

  Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode` at BelowNormal priority/four-core affinity. Expected: all tests PASS.

- [ ] **Step 3: Run full PlayMode tests**

  Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode` at BelowNormal priority/four-core affinity. Expected: all tests PASS.

- [ ] **Step 4: Build Android development player**

  Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1` at BelowNormal priority/four-core affinity. Expected: successful ARM64 IL2CPP APK build with no new first-party errors.

- [ ] **Step 5: Review the final diff and write verification evidence**

  Confirm no pre-existing `.meta` changes are staged, no complete third-party package/demo content is added, and document exact test/build counts plus any remaining device-only audio/visual checks.

- [ ] **Step 6: Commit and push**

  Stage only the two ledgers and verification document, commit as `docs: verify audio and weapon frame polish`, and push `master`.

