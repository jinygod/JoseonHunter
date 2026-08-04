# Lobby Deployment Focus Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the lobby home into a character-led deployment screen with a compact weapon picker, concise training labels, and an icon-led coin balance.

**Architecture:** Extend the existing runtime-built `LobbyBootstrap`, `PatrolPresenter`, and `CommonTrainingPresenter` rather than adding a new lobby system. `LobbySceneBuilder` remains the single editor seam that assigns authored sprites and rebuilds the Lobby scene/prefab.

**Tech Stack:** Unity 6, uGUI, TextMeshPro, NUnit Unity Test Framework, existing pixel sprites and 9-slice lobby assets

## Global Constraints

- Work directly on `master`, preserve unrelated dirty files, and push completed commits to `origin/master`.
- Do not add new image assets; reuse existing transparent pixel sprites and weapon catalog icons.
- Do not use a rectangular portrait background, white outline, or non-pixel character art in the lobby.
- Display the currency as coin icon plus bare number; do not display `엽전` or `냥` beside the number.
- Run Unity processes sequentially at BelowNormal priority with processor affinity `0xF`.

---

### Task 1: Define the new lobby labels and header contract

**Files:**
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs`

**Interfaces:**
- Consumes: existing Lobby scene shell
- Produces: observable label and header requirements

- [ ] **Step 1: Write failing navigation and training copy assertions**

Assert bottom labels equal `무기 연구`, `출전`, `수련`; assert `Training Title` equals `수련`; assert `Training Description` equals `수련 효과는 모든 출전에 적용되며, 항목별 최대치는 10%입니다.`.

- [ ] **Step 2: Write failing header assertions**

Set saved coins to 155, load Lobby, and assert `Coin Icon` has a non-null sprite while `Coin Text` equals `155` and contains neither `엽전` nor `냥`.

- [ ] **Step 3: Run focused tests and verify RED**

Run the three lobby fixtures. Expected failures: old `공통 수련` labels, missing `Coin Icon`, and old `엽전 155` text.

### Task 2: Implement concise training copy and icon-led currency

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`

**Interfaces:**
- Consumes: `MetaGameSession.Data.Coins`, existing coin sprite
- Produces: `Coin Icon`, bare-number `Coin Text`, and visible `수련` copy

- [ ] **Step 1: Add the header coin icon and reflow the number**

Create `Coin Icon` as a 42px square Image to the left of `Coin Text`. Change refresh copy to `$"{Coins:N0}"` and keep the group right-aligned.

- [ ] **Step 2: Assign the existing coin sprite in the scene builder**

Load `Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/coin.png` and assign it to `Coin Icon` with `preserveAspect = true`.

- [ ] **Step 3: Rename visible training copy**

Change only user-visible labels to `수련` and the approved description. Keep internal GameObject and save identifiers unchanged for compatibility.

- [ ] **Step 4: Run the focused tests and verify GREEN**

Expected: navigation, training copy, and header tests pass.

### Task 3: Define the character-led patrol flow with failing tests

**Files:**
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs`

**Interfaces:**
- Consumes: existing patrol save behavior and weapon catalog
- Produces: hero/slot/modal behavior contract

- [ ] **Step 1: Assert the new home hierarchy**

Assert `Patrol Hero`, `Patrol Hero Shadow`, `Starting Weapon Selector`, and `Start Patrol` exist. Assert `Previous Weapon`, `Next Weapon`, and the old oversized `Current Weapon Icon` do not exist.

- [ ] **Step 2: Assert transparent character integration**

Assert `Patrol Hero` has a non-null sprite, preserves aspect, and has no Image parent named hero frame/card/viewport. Assert `Patrol Hero Shadow` is a translucent sibling behind the hero.

- [ ] **Step 3: Assert weapon picker behavior**

Click `Starting Weapon Selector`, verify `Weapon Selection Overlay` becomes active, click the `gakgung_shot` option, then assert the active loadout saves `gakgung_shot` and the overlay closes.

- [ ] **Step 4: Run focused patrol tests and verify RED**

Expected failures: missing hero, selector, and selection overlay; old arrows still exist.

### Task 4: Implement the character-led patrol home and weapon picker

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`

**Interfaces:**
- Consumes: `WeaponRoster.All`, `WeaponCatalogAsset`, active `PatrolLoadout`
- Produces: `OpenWeaponSelection()`, `SelectWeapon(WeaponId)`, compact selector rendering

- [ ] **Step 1: Replace the oversized carousel hierarchy**

Remove runtime creation of arrow buttons and the central weapon-only image. Create a translucent oval `Patrol Hero Shadow`, transparent `Patrol Hero`, compact `Starting Weapon Selector`, and retain the existing primary `Start Patrol` button.

- [ ] **Step 2: Build the weapon selection overlay**

Create an inactive opaque overlay with title `시작 무기 선택`, close button, and a two-column grid. Each weapon option uses the existing secondary frame, weapon icon, and Korean name.

- [ ] **Step 3: Bind selection and immediate save**

Opening the selector shows the overlay. Selecting an option updates `selectedWeapon`, calls the existing save seam, refreshes the compact slot, and closes only when save succeeds.

- [ ] **Step 4: Assign the existing Han Yeonhwa sprite**

Load `Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png` in `LobbySceneBuilder` and assign it to `Patrol Hero`. Do not create a hero background sprite.

- [ ] **Step 5: Run focused patrol tests and verify GREEN**

Expected: the hierarchy, save behavior, overlay closing, and removed-carousel assertions pass.

### Task 5: Rebuild, capture, and regress

**Files:**
- Modify generated authored assets: `Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab`, `Assets/JoseonHunter/Scenes/Lobby.unity`
- Generate local evidence only: `Artifacts/LobbyPremium/*.png`

**Interfaces:**
- Consumes: updated builders and presenters
- Produces: authored Lobby scene/prefab and verified portrait captures

- [ ] **Step 1: Rebuild the Lobby scene and prefab**

Run `LobbySceneBuilder.BuildInBatchMode` with the project closed in the editor. Confirm the builder exits zero and no unrelated scenes are modified.

- [ ] **Step 2: Capture 720×1280 and 1080×2340 previews**

Run `LobbySceneBuilder.CapturePreviewInBatchMode`. Inspect that the transparent pixel hero blends with the courtyard, the coin icon/number fit the header, the compact weapon selector is readable, and no rectangle surrounds the hero.

- [ ] **Step 3: Run full EditMode tests**

Expected: zero failures.

- [ ] **Step 4: Run full PlayMode tests**

Expected: zero failures.

- [ ] **Step 5: Commit and push only scoped files**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/UI/Lobby Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs Assets/JoseonHunter/Tests Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab Assets/JoseonHunter/Scenes/Lobby.unity Docs/Superpowers
git commit -m "feat: refocus lobby deployment screen"
git push origin master
```

