# Gameplay Hybrid Scene Refactor Design

## Status

Approved in conversation on 2026-08-09. The user selected the hybrid approach: stable gameplay composition is authored in the Unity Scene, while high-volume combat objects remain runtime-created or pooled.

## Goal

Make the production `Gameplay` scene useful for normal Unity authoring. A developer must be able to open the scene, see Han Yeonhwa, the camera framing, the battlefield root, the runtime/spawn structure, and the UI root, then move or visually edit stable objects without waiting for Play Mode.

The refactor must preserve current combat, balance, progression, save data, stage pacing, pooling, public test seams, and Android behavior.

## Current Problem

`FirstPlayableController` is an early vertical-slice controller of roughly 4,000 lines. Its `Awake()` method resolves or creates a camera, creates shared render assets, and calls `ResetRun()`. `ResetRun()` destroys and recreates `RuntimeObjects`, creates the player and world bar, creates runtime presentation helpers and pools, and conditionally rebuilds the field. `CreateField()` destroys and recreates `FlatField`.

Consequences:

- the production Scene contains only `Main Camera`, `FirstPlayable`, and `EventSystem`;
- Han Yeonhwa, field content, and the UI do not exist until Play Mode;
- changing an object in the live Play hierarchy is temporary;
- repeated reset is coupled to hierarchy destruction;
- scene composition, visual construction, gameplay rules, and run lifecycle are mixed in one controller;
- the existing scene generator erases every root and would destroy manual scene authoring.

## Considered Approaches

### 1. Fully scene-authored combat

Author the player, field, enemy samples, projectiles, pickups, UI, and every gameplay object in the Scene.

This is easy to inspect but is rejected because survivors-style waves, multiple stage presentations, object pooling, and a mobile active-enemy ceiling require dynamic ownership. It would also create many inactive scene objects and duplicate runtime state.

### 2. Hybrid stable composition and transient runtime content

Author the stable composition in the Scene and keep high-volume or stage-dependent content dynamic. This is the selected approach.

It provides direct Scene editing without changing the runtime ownership model that already supports waves, pooling, and stage selection.

### 3. Keep runtime composition and improve only the preview scene

This has the smallest runtime risk but is rejected because moving objects in the preview does not change the production starting composition.

## Authored Production Hierarchy

The production `Gameplay` scene will retain its existing root names where practical and add a stable hierarchy under `FirstPlayable`:

```text
Gameplay
├── Main Camera
│   └── Camera component
├── FirstPlayable
│   ├── FirstPlayableController
│   ├── GameFlowCoordinator
│   ├── GameplaySceneComposition
│   ├── FlatField
│   │   └── stage-specific runtime chunks and boundaries
│   ├── RuntimeObjects
│   │   ├── Han Yeonhwa (connected PlayerVisual prefab instance)
│   │   │   └── authored WorldHealthBar prefab instance
│   │   └── runtime enemies, pickups, treasure, and attack objects
│   ├── RuntimeSystems
│   │   └── reset-scoped presenters and pools
│   └── Spawn Guides
│       └── editor gizmo representation of the viewport spawn perimeter
├── First Playable UI
│   └── FirstPlayableUiBootstrap
└── EventSystem
```

`RuntimeObjects/Han Yeonhwa` remains the compatibility path used by existing PlayMode tests and presentation code. The container itself and Han Yeonhwa are scene-owned and are never destroyed by `ResetRun()`. Other children are reset-scoped.

## Component Responsibilities

### `GameplaySceneComposition`

A runtime `MonoBehaviour` on `FirstPlayable` owns serialized references to:

- the production camera;
- `FlatField`;
- `RuntimeObjects`;
- `RuntimeSystems`;
- `Spawn Guides`;
- the authored player `CombatantVisualView`;
- the scene-authored UI bootstrap.

It validates that every owned object belongs to the same Scene and expected hierarchy. It captures the player's authored local position, rotation, scale, and active state once before gameplay mutates them.

It exposes narrow operations:

- resolve the camera and stable roots;
- restore the authored player pose;
- remove or deactivate reset-scoped children without deleting stable roots;
- return the authored player for runtime binding;
- report whether the authored composition is complete.

Missing or invalid composition is not fatal. Development and Editor builds emit one clear warning, then the controller uses its legacy runtime-created fallback so direct component tests and older scenes remain supported.

### `GameplayBattlefieldHost`

A runtime `MonoBehaviour` on `FlatField` owns the stage-dependent battlefield presentation currently implemented by `FirstPlayableController.CreateField()` and `UpdateField()`.

It keeps the `FlatField` transform stable while rebuilding only generated chunk, decoration, and boundary children when the stage identity changes. It owns the active `BattlefieldTilePresenter` or `BoundedBattlefieldPresenter` reference and exposes bounds/tracking data needed by the controller. It does not own stage selection or balance rules.

### `GameplayVisualFactory`

A plain runtime collaborator owns the prefab-or-fallback construction currently concentrated at the bottom of `FirstPlayableController`:

- bind the authored player visual;
- instantiate enemy visuals;
- instantiate pickup visuals;
- create or reuse authored health/shield bars;
- preserve `GameplayVisualPrefabLibrary` validation and one-time fallback warnings.

Runtime sprites, roles, sorting orders, motion bindings, status presentation, damage logic, and pickup values remain injected by `FirstPlayableController`. This factory does not own gameplay state.

### `FirstPlayableController`

The controller remains the authoritative owner of run state, input, combat services, wave pacing, enemies, pickups, weapons, upgrades, settlement, and public events.

The refactor changes only composition-facing code:

- `Awake()` resolves the scene composition before setup;
- camera setup prefers the authored camera and preserves its Inspector configuration;
- `ResetRun()` clears transient children but preserves the authored roots and player identity;
- field operations delegate to `GameplayBattlefieldHost`;
- visual construction delegates to `GameplayVisualFactory`;
- existing public APIs and `UNITY_INCLUDE_TESTS` seams remain unchanged.

No combat formula, spawn budget, stage timeline, save schema, weapon behavior, or progression value changes in this work.

### `FirstPlayableUiBootstrap`

The bootstrap component becomes scene-authored on the `First Playable UI` root. Its existing runtime initializer remains a fallback for old/direct-load scenes and first checks for the scene-authored component, preventing duplicate canvases or event systems.

The existing presenters continue building their detailed HUD hierarchy at runtime. This design authors UI ownership and placement, not a second UI implementation.

## Camera and Spawn Behavior

The `Main Camera` Inspector values become authoritative. The controller no longer overwrites orthographic size, clear flags, background color, or initial X/Y placement when a valid authored composition exists. Camera follow continues using the current smoothing and player target.

Enemy spawn positions continue to use the camera viewport perimeter and bounded-stage rules. `Spawn Guides` visualize that perimeter in the Scene and expose authoring guidance; they do not replace the moving-camera spawn calculation with fixed world positions. This preserves off-screen spawning and infinite-map behavior.

## Reset and Lifetime Rules

On initial load and every run reset:

1. preserve `Main Camera`, `FirstPlayable`, `FlatField`, `RuntimeObjects`, `RuntimeSystems`, `Spawn Guides`, `Han Yeonhwa`, `First Playable UI`, and `EventSystem` identities;
2. clear reset-scoped enemies, projectiles, stage hazards, pickups, treasure, presenters, and generated field children;
3. restore Han Yeonhwa's authored transform and active state;
4. bind the existing player visual to current sprite, motion, sorting, health, combat target, and weapon services;
5. reuse the authored health bar when valid, otherwise use the existing prefab/fallback path;
6. rebuild stage presentation under the stable field root;
7. mark `GameplayReadySignal` only after composition and runtime services are ready.

`OnDestroy()` retains current service disposal and render-resource cleanup.

## Editor Workflow

`FirstPlayableSceneGenerator` becomes non-destructive and idempotent:

- it refuses to modify a dirty loaded Gameplay scene;
- it creates missing stable objects and serialized references;
- it preserves valid existing objects, transforms, nested prefab links, and user edits;
- it never deletes all Scene roots;
- it ensures `Gameplay` remains the third enabled Build Settings scene;
- it uses Unity Editor APIs to create or connect scene objects and prefab instances.

Menus:

- `JoseonHunter/Gameplay Editing/Open Authored Gameplay Scene`
- `JoseonHunter/Gameplay Editing/Create or Validate Authored Gameplay Scene`

The existing `GameplayVisualPreview` remains useful for side-by-side visual checks. The production `Gameplay` scene becomes the source for starting composition and Play Mode iteration.

## Compatibility and Failure Handling

- Existing scenes or tests with only `FirstPlayableController` continue through legacy runtime composition.
- Missing individual visual prefabs continue through existing visual fallbacks.
- Invalid authored references log once in Editor/Development builds and do not crash a release player.
- Serialized field names already in use are preserved. New serialized references are additive.
- Scene and prefab GUIDs remain stable. New assets receive Unity-generated metadata.
- The Preview scene stays excluded from Build Settings.
- Unrelated Lobby and art changes already present in the working tree are not modified or committed with this feature.

## Testing Strategy

### EditMode

- production Gameplay scene contains exactly one complete authored composition;
- stable hierarchy names, serialized references, prefab connections, and component ownership are valid;
- the player is a connected `PlayerVisual` instance and the health bar is a connected `WorldHealthBar` instance;
- generator validation is idempotent and preserves authored transforms and production prefab hashes;
- dirty Gameplay scenes are refused without mutation;
- Build Settings order remains Bootstrap, Lobby, Gameplay;
- no Missing Script references exist in new or modified assets.

### PlayMode

- Gameplay loads with one camera, player, UI bootstrap, field root, runtime root, and event system;
- repeated `ResetRunForTests()` preserves the instance IDs and authored transforms of stable scene objects;
- no duplicate player renderer, bar, canvas, UI bootstrap, or event system is created;
- spawned enemies and pickups remain transient children under `RuntimeObjects`;
- pickup pooling still reuses instances and experience trails still work;
- camera follow and field tracking still respond to player movement;
- Resources and invalid-prefab fallbacks retain the stable scene composition;
- existing combat, presentation, pickup, stage, and UI tests remain green except documented pre-existing failures.

### Build and Manual Validation

- compile/import with no new first-party Console errors;
- focused EditMode and PlayMode suites;
- full EditMode and full PlayMode suites with baseline comparison;
- Android development APK build;
- manually open the production Gameplay scene, move Han Yeonhwa, press Play, and verify the authored start position is used;
- verify reset restores that authored position and does not duplicate objects;
- verify camera framing, field tiles, HUD, enemy waves, pickups, and pause flow in portrait view.

## Acceptance Criteria

- Han Yeonhwa is visible and movable in the production Gameplay Scene before Play Mode.
- Moving Han Yeonhwa in the Scene changes the next Play Mode starting position.
- Main Camera Inspector framing is visible before Play and is not overwritten during initialization.
- Stable scene object identities survive repeated run resets.
- Enemies, projectiles, hazards, treasure, and pickups remain dynamic/pool-compatible.
- Current gameplay, balance, save, and progression behavior is unchanged.
- `FirstPlayableController` no longer owns scene hierarchy construction, battlefield presentation construction, or low-level visual prefab construction directly.
- Editor generation is non-destructive and refuses dirty-scene overwrite.
- Focused tests, full suites, and Android build are reported with exact evidence and any pre-existing failure is distinguished from regressions.
