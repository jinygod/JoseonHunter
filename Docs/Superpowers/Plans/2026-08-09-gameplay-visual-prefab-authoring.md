# Gameplay Visual Prefab Authoring Implementation Plan

> Execute this plan continuously. The user explicitly approved implementation and requested no plan-only stop.

**Goal:** Replace code-assembled player, enemy, world-bar and pickup visual structures with editable Unity Prefabs while preserving all gameplay logic and contracts.

**Architecture:** A Resources-backed `GameplayVisualPrefabLibrary` supplies seven production prefabs. Small view components expose authored children, `CombatantVisualRig` binds them, and `FirstPlayableController` retains legacy creation as an explicit compatibility fallback. A separate editor-only preview scene instantiates the production prefabs.

**Tech Stack:** Unity 6000.5.5f1, C#, NUnit/Unity Test Framework, UnityEditor PrefabUtility/AssetDatabase/EditorSceneManager.

---

## Task 1: Lock Asset and Reference Contracts

**Files:**
- Create: `Assets/JoseonHunter/Tests/EditMode/GameplayVisualPrefabContractTests.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayVisualPrefabLibrary.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

1. Add RED EditMode tests for the library path, all seven prefab references, required hierarchy/components, preview absence from Build Settings, and Gameplay scene serialized library reference.
2. Run the focused fixture and capture the expected failures.
3. Add the library type and controller serialized/Resources resolution without changing runtime construction yet.

## Task 2: Prefab-Backed World Bars

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/WorldBarView.cs`
- Create/modify tests: `Assets/JoseonHunter/Tests/PlayMode/GameplayVisualPrefabPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

1. Add RED tests proving a bar instance uses the prefab-authored hierarchy and ratio changes preserve authored height/offset.
2. Implement `WorldBarView`, prefab instantiation and legacy fallback.
3. Run focused bar tests and existing health/shield gameplay tests.

## Task 3: Prefab-Backed Combatant Visuals

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify tests: `CombatantVisualRigPlayModeTests.cs`, `GameplayVisualPrefabPlayModeTests.cs`

1. Add RED binding tests for player/enemy hierarchy, no duplicate renderers, logical-root stability, hit/death/motion and role layers.
2. Implement `CombatantVisualRig.Bind` and instantiate player/enemy prefabs in the existing helper.
3. Preserve runtime role scale and special-enemy/boss data injection.
4. Run focused combatant and presentation fixtures.

## Task 4: Prefab-Backed Pickups Without Pool Changes

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/PickupVisualView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify tests: `GameplayVisualPrefabPlayModeTests.cs`

1. Add RED tests for prefab origin, exact names, root TrailRenderer, base scale and same-instance pool reuse.
2. Instantiate the per-kind prefab only when the existing pool has no inactive candidate.
3. Keep merge, attraction, colour/tier, deactivation and collection logic unchanged.
4. Run the new fixture plus all existing pickup-range tests.

## Task 5: Generate Assets and Preview Through Unity Editor APIs

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/GameplayVisualPrefabBuilder.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/GameplayVisualPreviewBuilder.cs`
- Generate: seven production prefabs, the prefab library asset, preview prefab and preview scene
- Modify: `FirstPlayableSceneGenerator.cs`

1. Implement repeatable builders that create only missing assets, validate existing assets and refuse dirty scene overwrite.
2. Add batch entry points with explicit exit codes.
3. Run the production builder through Unity, then rerun asset contract tests GREEN.
4. Run the preview builder, confirm the scene is excluded from Build Settings and inspect its hierarchy.

## Task 6: Beginner Authoring Guide

**Files:**
- Create: `Docs/GameplayPrefabAuthoring.md`

Document opening Prefabs/Preview, Prefab Mode, saved versus Play Mode edits, safe size/offset/sprite changes, bars/pickups, and scripts/runtime references that must not be removed.

## Task 7: Regression and Platform Validation

1. Run focused EditMode/PlayMode fixtures after each phase.
2. Run full EditMode and full PlayMode suites and compare with the recorded baseline.
3. Validate missing scripts/prefabs, Build Settings, preview exclusion, duplicate renderers/bars, and Resources fallback.
4. Run the project's available Android compile/build validation if safe and proportionate.
5. Record commands, XML/log artifacts, automated evidence, manual-only checks and remaining limitations.

## Task 8: Review, Commit and Push

1. Review the exact diff without touching unrelated dirty files.
2. Request a final read-only code review.
3. Commit only intended gameplay-prefab files and generated Unity assets.
4. Push only after confirming the existing local unapproved Lobby commits do not create an unintended remote publication; otherwise report the precise blocker instead of silently pushing them.
