# Portrait Stabilization Vertical Slice Design

**Status:** Approved on 2026-07-31

**Project:** JoseonHunter

**Target:** Unity 6.5, Android, portrait-only mobile

**Supersedes for this stabilization pass:** landscape-oriented runtime layout and scattered pause ownership

## 1. Purpose

Stop expanding content and turn the current combat build into a polished, measurable vertical prototype. The pass focuses on five player-visible problems:

1. the game is configured and laid out as landscape content inside a portrait window;
2. level-up and weapon appraisal flows do not have one authoritative pause owner;
3. enemies converge into the same point and become unreadable;
4. performance work lacks profiler evidence and repeatable load scenarios;
5. existing animation and VFX assets are not audited before new assets are generated.

The result must be a coherent portrait combat loop, not a collection of additional content.

## 2. Confirmed Baseline

The design is based on the repository state at commit `22e37ca`.

- Unity version: `6000.5.5f1`.
- Build target: Android with URP 2D and the Input System.
- Build scenes: Bootstrap, Lobby, Gameplay.
- Player settings still select `LandscapeLeft`; portrait orientations are disabled.
- `FirstPlayableUiBootstrap` uses a `1920 x 1080` Canvas reference resolution.
- combat HUD, weapon rack, upgrade choice, and appraisal presenters contain landscape-sized offsets.
- `UpgradeChoicePresenter`, `WeaponAffixRevealPresenter`, `CombatFeedbackDirector`, and `FirstPlayableController` all manipulate `Time.timeScale` independently.
- enemy movement seeks the player directly and has no local separation step.
- the camera follows with unscaled delta time, so it can continue moving during modal pauses.
- enemy and pickup lifecycles still contain repeated `Instantiate` and `Destroy` paths.
- there are no repeatable development-build profiler captures or subsystem profiler markers.
- a portrait runtime capture demonstrates clipped level-up cards and very small combat actors.
- the repository already contains substantial character walk/idle animation and per-weapon polish frames.
- the first EditMode baseline is 490 tests: 477 pass and 13 fail. The failures mix real configuration defects with stale test contracts.
- PixelLab starts this pass with 1,512 generations remaining. No generation is authorized until the runtime asset audit identifies a concrete gap.

## 3. Goals and Non-goals

### Goals

- Support portrait-only play at 720x1280, 1080x1920, 1080x2340, 1170x2532, and 1440x3200.
- Keep all interactive UI inside the device safe area.
- Make one service authoritative for game flow and time scale.
- Freeze combat, spawning, pickups, player input, weapon simulation, and camera movement throughout modal flows.
- Keep reveal animation responsive with unscaled UI time while gameplay is frozen.
- Keep groups of 30, 50, and 100 enemies readable without full Rigidbody-based crowd simulation.
- Add repeatable profiler evidence before choosing pooling or other optimizations.
- Reuse integrated art first and generate only reviewed, bounded gaps.
- Leave a concise Unity/C# handoff for a Java backend developer.

### Non-goals

- adding characters, weapons, stages, enemies, progression systems, or live-service features;
- replacing the entire `FirstPlayableController` in one rewrite;
- building a general-purpose ECS crowd framework;
- introducing a real-time blur pipeline for modal backgrounds;
- mass-generating replacement art;
- optimizing solely from Editor frame rate or memory usage;
- changing combat balance except where necessary for the three-minute prototype and validation scenarios.

## 4. Delivery Strategy

Use an incremental stabilization pass. Existing systems remain playable after each slice, and each slice adds tests before production changes.

1. make baseline contracts truthful and lock portrait player settings;
2. introduce centralized game flow and pause ownership;
3. rebuild runtime layout around a safe-area portrait root;
4. connect upgrade and appraisal flows to the state coordinator;
5. add viewport-aware camera and spawn behavior;
6. add allocation-free local enemy separation;
7. add performance instrumentation and optimize only measured hotspots;
8. audit and polish the existing animation/VFX bindings;
9. validate all target resolutions and Android development behavior;
10. document the resulting architecture and workflow.

This approach was selected over a full controller/UI rewrite and over isolated minimal patches. It contains regression risk while still removing the architectural causes of the visible bugs.

## 5. Game Flow and Time Ownership

### 5.1 State model

Introduce a single `GameFlowState` model with these required states:

- `Playing`
- `LevelUpSelection`
- `AugmentResult`
- `Paused`
- `GameOver`

`GameFlowCoordinator` is the only runtime component allowed to set the global gameplay time scale. Presenters request transitions; they do not write `Time.timeScale`.

Expected primary flow:

```text
Playing -> LevelUpSelection -> AugmentResult -> Playing
Playing <-> Paused
Playing/LevelUpSelection/AugmentResult/Paused -> GameOver
```

Invalid transitions are rejected and logged with enough context to diagnose the caller. Re-entering the current state is idempotent.

### 5.2 Pause semantics

- `Playing` uses gameplay time scale 1, except for coordinator-owned transient hit stop.
- `LevelUpSelection`, `AugmentResult`, `Paused`, and `GameOver` use gameplay time scale 0.
- hit stop is an internal transient request, not a public flow state, and is accepted only while `Playing`.
- a hard modal state always wins over hit stop.
- restoring a modal state never restores a stale time scale captured by a presenter.
- On disable, scene unload, and test teardown, the coordinator restores a known time scale of 1.

### 5.3 Simulation gates

Gameplay systems use `IsGameplayRunning` rather than duplicating state checks. When false, all of the following stop:

- player movement and touch combat input;
- enemy movement, contact, death progression, and spawning;
- weapon attacks and transient combat simulation;
- pickup attraction and collection;
- battlefield progression and timers;
- camera follow and shake.

Modal UI reveal animation uses unscaled time. Only the active modal receives input.

## 6. Portrait Runtime Layout

### 6.1 Player and Canvas settings

- default orientation is `Portrait`;
- autorotation and both landscape orientations are disabled;
- Canvas mode remains `ScreenSpaceOverlay` unless profiling demonstrates a reason to change it;
- Canvas Scaler uses `ScaleWithScreenSize`, reference resolution `1080 x 1920`, and a balanced width/height match;
- the Safe Area root converts `Screen.safeArea` to normalized anchors whenever resolution or safe area changes;
- gameplay UI is parented under the Safe Area root unless it is an intentional full-screen dimmer.

### 6.2 Layout regions

- top safe region: health, experience, stage clock, kill/boss information;
- upper/middle combat region: unobstructed battlefield and readable actor scale;
- lower safe region: weapon rack and touch movement affordance;
- center modal region: upgrade selection and weapon appraisal scroll;
- bottom modal region: explicit confirm action within thumb reach.

The layout uses anchors, layout groups, and bounded content sizes. Hard-coded coordinates tied to 1920x1080 are removed from portrait paths.

### 6.3 Modal presentation

The level-up and appraisal flows share a modal shell:

- full-screen dark scrim with mobile-cheap visual treatment;
- centered parchment/scroll content constrained to the Safe Area;
- vertically readable choices and details;
- no background raycasts or combat touch input;
- result screen remains until explicit confirmation;
- all reveal stages use unscaled time and remain testable with deterministic timing profiles.

## 7. Camera and Spawn Geometry

Create a portrait combat visual profile instead of continuing to use `MobileLandscape` values.

- actor scale and orthographic size are tuned together using the 1080x1920 reference capture;
- the player remains readable near the visual center while leaving useful space above for incoming enemies;
- spawn positions derive from the current camera viewport expanded by a world-space margin;
- a spawn is selected from a perimeter side rather than a fixed circular radius;
- all spawn points begin outside the visible viewport and inside the active simulation envelope;
- camera follow, impulse, and shake stop whenever gameplay is not running.

Viewport-derived geometry prevents enemies from appearing visibly on the narrow sides of tall devices.

## 8. Enemy Separation

Use a lightweight spatial hash rebuilt from active enemies during the enemy simulation step.

- cell size is close to the normal enemy contact diameter;
- each enemy checks only its current and adjacent cells;
- chase velocity is blended with a capped soft separation vector;
- exact overlaps use a deterministic fallback direction so two enemies cannot remain coincident;
- maximum correction and neighbor work are bounded;
- buffers and cell collections are reused to avoid steady-state managed allocations;
- bosses and elites use profile-specific radii and weights;
- separation never pushes enemies away strongly enough to stop pursuit.

The load harness exercises 30, 50, and 100 active enemies and records minimum spacing, frame time, and allocations. Rigidbody collision solving is not introduced for normal crowd motion.

## 9. Performance Evidence and Optimization

Add named profiler markers around:

- run update and state gating;
- enemy grid rebuild, neighbor query, and movement;
- spawning and lifecycle operations;
- weapon execution and transient visuals;
- pickup update;
- HUD refresh and modal reveal.

Validation distinguishes Editor results from an Android development build. The target is a stable 60 FPS frame budget (16.67 ms) on the available reference device, with no recurring managed allocation in steady enemy movement or modal-idle frames.

Pooling is evidence-driven:

- keep existing transient weapon and damage-number pools;
- add enemy or pickup pooling only if captures show lifecycle work or garbage collection as a meaningful hotspot;
- prewarm to the validated load tier and keep pool growth bounded;
- report before/after captures for every optimization retained.

The current machine's 16 GB RAM is sufficient for this stabilization pass. A hardware upgrade is not a substitute for runtime profiling.

## 10. Art and Animation Policy

The current runtime already contains usable art:

- Han Yeonhwa idle and walk frames;
- normal enemy walk sets;
- elite and boss idle/walk sets;
- per-weapon VFX and polish sequences;
- an existing combat motion rig with bob, recoil, hit, and death treatments.

The first task is to verify imports, `CombatMotionLibrary` bindings, scale, cadence, and actual runtime visibility. Static fallbacks are retained only for safe recovery.

PixelLab generation requires all of the following:

1. a named runtime gap visible in the approved portrait capture;
2. an exact asset list, dimensions, frame count, and palette/style reference;
3. confirmation that no existing source or runtime asset satisfies the need;
4. a ledger entry containing prompt, output path, selection decision, and credit delta;
5. import and runtime validation before another batch is requested.

Likely candidates, only if the audit proves them missing, are player death readability, selected enemy idle readability, or missing flying-blade launch/hit/return phases. The initial generation budget for the stabilization pass is zero.

## 11. Baseline Failure Policy

The 13 failing EditMode tests are handled before new completion claims:

- production-setting failures such as Android orientation are fixed in production code/settings;
- stale contracts such as landscape scale, old animation frame counts, old PPU assumptions, and obsolete scene-root expectations are updated to the approved portrait architecture;
- import and alpha-mask failures are investigated against the actual runtime asset contract;
- domain behavior failures such as affix cooldown range handling receive focused regression tests;
- a test is not weakened merely to turn the suite green.

The updated suite must explain the new contract in its test names.

## 12. Verification and Acceptance

### Automated

- EditMode tests for state transitions, time ownership, portrait settings, safe-area math, viewport spawn geometry, separation, and content contracts;
- PlayMode tests proving all gameplay and camera movement stop during selection/result states;
- PlayMode tests proving reveal UI continues with unscaled time and requires confirmation;
- deterministic 30/50/100-enemy load scenarios;
- full EditMode and PlayMode regression runs with no unexpected failures;
- Android development build validation.

### Visual

Capture the gameplay HUD, level-up choice, appraisal reveal, and resumed combat at:

- 720x1280
- 1080x1920
- 1080x2340
- 1170x2532
- 1440x3200

Acceptance requires no clipped text or controls, no interaction outside the safe area, readable player/enemy scale, no modal background movement, and no obvious enemy stacking at validated load tiers.

### Performance

- record Editor and Android development-build captures separately;
- include frame-time and GC evidence for 30, 50, and 100 enemies;
- document retained optimizations and rejected speculative ones;
- report device/model limitations when a physical Android reference device is unavailable.

## 13. Documentation and Handoff

The final handoff explains the implementation for a Java backend developer using direct mappings:

- `MonoBehaviour` lifecycle versus a managed service lifecycle;
- ScriptableObject content versus immutable/configuration DTOs;
- serialized Unity references versus dependency injection/wiring;
- coroutines versus asynchronous workflows, including their different cancellation semantics;
- Unity frame update, scaled time, and unscaled time;
- EditMode versus PlayMode tests;
- how to reproduce the portrait captures and performance scenarios.

The handoff also includes the final PixelLab starting/ending balance and a list of generated, accepted, and rejected assets.

## 14. Completion Boundary

The stabilization pass is complete only when the portrait combat loop can be launched, played through upgrade/appraisal/resume, and validated without relying on undocumented Editor state. Any additional content idea is recorded separately and does not enter this vertical slice.
