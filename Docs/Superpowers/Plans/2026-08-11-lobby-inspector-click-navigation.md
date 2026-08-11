# Lobby Inspector-Editable Click Navigation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore pointer clicks for the Training and Research home cards while preserving every saved visual override, then make the lobby navigation wiring explicit, validated, and understandable from the Unity Inspector.

**Architecture:** Keep the existing authored `Lobby.unity` + typed View/Presenter structure. Reuse the existing prefab-authored `HomeMenuCard/Button/Body` Image as the explicit click surface, keep all page transitions in `LobbyNavigationPresenter`, and strengthen serialized-reference validation without introducing a manager, router, reflection binder, or runtime UI construction. Static labels and visual styling remain authored; presenters keep only data/state behavior.

**Tech Stack:** Unity 6000.5.5f1, uGUI 2.5.0, Input System UI 1.20.0, NUnit Unity Test Framework 1.7.0, C#.

## Global Constraints

- Preserve the user's current `Lobby.unity` RectTransforms, anchors, pivots, positions, sizes, scales, sprites, materials, colors, alpha, fonts, TMP settings, canvas order, and hierarchy layout.
- Do not use PixelLab, image generation, Asset Store downloads, new sprites, or visual redesign.
- Do not reset, restore, regenerate, or wholesale rewrite the scene/prefabs.
- Runtime lobby code may bind events, toggle page/state objects, and render data; it must not author layout or fixed visual styling.
- Use explicit serialized references and owned listener removal; never use `RemoveAllListeners`.
- Current Deployment is audit-only in this task; do not change its object, sprite, color, alpha, activation, or geometry.
- Run one Unity process at a time, preserve unrelated dirty files, and stage only exact task files.

---

### Task 1: Preserve the manual lobby scene state

**Files:**
- Preserve: `Assets/JoseonHunter/Scenes/Lobby.unity`

**Interfaces:**
- Consumes: current saved scene YAML and prefab-instance overrides.
- Produces: checkpoint commit `chore: checkpoint manual lobby ui edits` containing only `Lobby.unity`.

- [x] **Step 1: Record baseline**

Run `git status --short --branch`, `git diff --stat`, `git diff --name-status`, Unity version, and missing-script scan.

- [x] **Step 2: Inspect scene-only semantic diff**

Run `git diff --ignore-space-at-eol -- Assets/JoseonHunter/Scenes/Lobby.unity` and identify functional overrides separately from visual overrides.

- [x] **Step 3: Commit only the manual scene**

Run `git add -- Assets/JoseonHunter/Scenes/Lobby.unity`, verify the cached name list is exactly one file, then commit.

### Task 2: Reproduce the real pointer-click failure

**Files:**
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`

**Interfaces:**
- Consumes: `LobbyRootView`, `LobbyHomeView`, `LobbyNavigationPresenter`, the production `GraphicRaycaster`, and `EventSystem.current`.
- Produces: a scene-backed regression that clicks through the actual graphic raycast path and verifies exactly one active page.

- [x] **Step 1: Run the existing focused baseline**

Run `Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests`. Expected before the new regression: existing fixture passes because it invokes `Button.onClick` directly.

- [x] **Step 2: Add a behavior test that catches the break**

Add a `UnityTest` that loads `Lobby`, obtains the authored `LobbyRootView`, computes each button center, asks the production `GraphicRaycaster` for results, sends `ExecuteEvents.pointerClickHandler` to the hit hierarchy, and verifies Training/Patrol/Research plus each Back button. Repeat the full round trip ten times and assert exactly one of Home/Training/Patrol/Research is active after every transition.

- [x] **Step 3: Run RED**

Run only `LobbyNavigationPlayModeTests`. Expected: Training and Research fail because their visual Button Image prefab overrides are disabled and no independent raycast surface exists; Patrol remains hittable.

### Task 3: Decouple click input from visual images and harden navigation wiring

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyMenuCardView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyHomeView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyRootView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyNavigationPresenter.cs`
- Modify: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/HomeMenuCard.prefab`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbyModulePrefabBuilder.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/LobbyModulePrefabContractTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`

**Interfaces:**
- Consumes: existing `LobbyMenuCardView.Button`, card prefab hierarchy, serialized page roots, menu buttons, and back buttons.
- Produces: `LobbyMenuCardView.InputSurface`, `LobbyMenuCardView.HasRequiredBindings`, `LobbyNavigationPresenter.HasRequiredBindings`, explicit `OpenTrainingPage`, `OpenPatrolPage`, `OpenResearchPage`, and `ReturnToHomePage` handlers.

- [x] **Step 1: Add prefab-contract RED coverage**

Load `HomeMenuCard.prefab` and assert the existing direct `Button/Body` Image is enabled, raycastable, serialized by `LobbyMenuCardView`, and assigned as `Button.targetGraphic`. The test must also assert `Icon`, `Title`, and optional `Description` remain decoration-only and preserve their authored properties.

- [x] **Step 2: Author the minimum functional click slot**

Reuse the existing child named `Body` beneath `Button` as the functional input surface. Assign it to `Button.targetGraphic` and the new serialized `inputSurface` field without changing its authored color, sprite, material, alpha, or RectTransform. Do not re-enable the manually disabled decorative root Image overrides in `Lobby.unity` and do not add another visual child.

- [x] **Step 3: Make required bindings explicit**

`LobbyMenuCardView.HasRequiredBindings` must require Button, Title, Icon, and the enabled/raycastable input surface; Description stays optional because the user removed it intentionally. `LobbyHomeView.HasRequiredBindings` must validate all three cards. `LobbyRootView.HasRequiredBindings` must validate both Home and Navigation. Missing bindings must produce a message containing the component/hierarchy owner and missing serialized field.

- [x] **Step 4: Make event ownership readable**

Replace anonymous transition lambdas with named methods, keep owned `UnityAction` references, keep `Unbind()` before `Bind()`, and add `OnDestroy => Unbind()`. Preserve outside/Inspector listeners and all existing page behavior.

- [x] **Step 5: Keep the optional editor generator consistent**

Update only `BuildHomeMenuCard`/`ValidateHomeMenuCard` so a future explicit module rebuild binds and validates `Button/Body` as the input surface. Do not invoke the full module rebuild and do not modify its layout or authored styling.

- [x] **Step 6: Run GREEN**

Run sequentially: `LobbyModulePrefabContractTests`, `LobbyNavigationPlayModeTests`, and `LobbyHomePlayModeTests`. Expected: all pass, ten pointer round trips work, and existing external-listener preservation remains green.

- [x] **Step 7: Commit the functional fix**

Stage only the exact runtime View/Navigation files, `HomeMenuCard.prefab`, builder consistency change, and focused tests. Commit as `fix: restore authored lobby menu clicks`.

### Task 4: Remove safe runtime design overrides without changing visuals

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyHomePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyHomePlayModeTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`

**Interfaces:**
- Consumes: already-authored labels/sprites/colors in `Lobby.unity` and module prefabs.
- Produces: presenters that bind data/events without rewriting fixed labels or action-button skin on initialization.

- [x] **Step 1: Add preservation tests**

Capture authored label, sprite, color, alpha, font, and RectTransform values before presenter initialization; initialize twice; assert those fixed design values and object identities remain unchanged while data text/interactable state updates still work.

- [x] **Step 2: Run RED against current design-writing behavior**

Expected failures must identify `PremiumPixelUiSkin.ApplyAction`, fixed label replacement, or same-object fallback binding—not generic object lookup failures.

- [x] **Step 3: Remove only safe fixed-design writes**

Remove the `LobbyHomePresenter.Awake` fallback `GetComponent` and rely on `LobbyBootstrap.ConfigureView`. Remove Training/Patrol initialization-time fixed label and action-skin rewrites only after tests prove the authored scene already supplies them. Keep data values, selected/locked state, routing, save logic, and weapon icon binding unchanged.

- [x] **Step 4: Run focused presenter suites**

Run `LobbyHomePlayModeTests`, `CommonTrainingLobbyPlayModeTests`, and `LobbyPatrolPlayModeTests` sequentially.

### Task 5: Document Inspector editing and report remaining UI debt

**Files:**
- Create: `Docs/UI_EDITING_GUIDE.md`
- Create: `Docs/Verification/2026-08-11-lobby-inspector-navigation.md`

**Interfaces:**
- Consumes: final authored hierarchy, serialized field ownership, editor menus, Current Deployment audit, project-wide pattern scan, and test evidence.
- Produces: a Korean editing guide and evidence-backed three-tier refactor inventory.

- [x] **Step 1: Write the Korean editing guide**

Document scene/module locations; page activation for editing; why not to edit in Play Mode; Button/Body/Icon/Title responsibilities; raycast rules; freely editable Inspector fields; required components/serialized refs; sprite and RectTransform editing; adding a page/card; code/UI boundary; prefab overrides; saving; and exact dangerous rebuild commands.

- [x] **Step 2: Record Current Deployment without changing it**

Report exact hierarchy `Safe Area/Home Page/Current Deployment/Starting Weapon Icon`, null Source Image, white RGBA, raycastTarget state, `LobbyHomePresenter.Refresh()` ownership, and placeholder/functional-slot conclusion.

- [x] **Step 3: Record automatic-generation risks**

List `LobbySceneBuilder`, `LobbyModulePrefabBuilder`, and `LobbyEditingTools`, their menu paths, execution conditions, affected assets, dirty-scene safeguards, and which rebuild menus must not be run after manual editing without a checkpoint.

- [x] **Step 4: Record three-tier project UI audit**

Classify safe lobby-local fixes, follow-up refactors (notably Patrol weapon-option string paths and selection chrome design values), and high-risk builder/runtime-factory migrations. Do not expand implementation scope.

- [x] **Step 5: Commit structure/docs**

Stage only the safe presenter simplification, tests, and the two documents. Commit as `refactor: separate authored lobby ui from behavior`.

### Task 6: Final verification and review

**Files:**
- Verify only; do not edit unrelated art, font, capture, gameplay, save, combat, or build-setting assets.

**Interfaces:**
- Consumes: all task commits.
- Produces: verified final report with exact counts and commit hashes.

- [x] **Step 1: Focused validation**

Run sequentially: `LobbyModulePrefabContractTests`, `LobbyModularSceneContractTests`, `LobbySceneContractTests`, `LobbyNavigationPlayModeTests`, `LobbyHomePlayModeTests`, `CommonTrainingLobbyPlayModeTests`, `LobbyPatrolPlayModeTests`, and `WeaponResearchLobbyPlayModeTests`.

- [x] **Step 2: Full suites**

Run full EditMode and full PlayMode only after focused green. Record exact total/pass/fail/skip counts and compare against the 1014/1014 and 364/364 last-known-green baseline.

- [x] **Step 3: Static integrity checks**

Scan modified scenes/prefabs for missing scripts, verify Build Settings order Bootstrap -> Lobby -> Gameplay, verify no task code writes RectTransform/design properties, verify scene visual-property diff equals the checkpoint, and inspect `git diff --check` plus exact staged file lists.

- [x] **Step 4: Final read-only code review**

Review the integrated diff once for P0/P1 issues, listener lifetime, serialized reference safety, and accidental visual changes; fix only confirmed findings and rerun affected tests.

- [ ] **Step 5: Push exact commits**

Push only after all required validation and final review complete; preserve all unrelated dirty files untouched.
