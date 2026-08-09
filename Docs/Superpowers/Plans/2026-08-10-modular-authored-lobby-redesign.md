# Modular Authored Lobby Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the runtime-built lobby shell with a Scene-visible, modular pixel-UI composition containing a Home hub and authored Training, Patrol, and Research pages without changing progression or save behavior.

**Architecture:** Small connected UI prefabs provide reusable header, page-header, card, row, progress, action, and lock components. `Lobby.unity` owns the static hierarchy and connected module instances; runtime presenters bind session data and button behavior only. A guarded Editor builder creates missing production modules and performs the explicit one-time scene composition, while normal play never deletes or rebuilds the authored hierarchy.

**Tech Stack:** Unity 6000.5.5f1, C#/.NET, Unity UI, TextMeshPro, Input System, NUnit EditMode/PlayMode tests, PixelLab pixel-art generation, Git.

## Global Constraints

- Preserve `MetaGameSession`, save schemas, account progression, training costs/effects, stage unlock rules, weapon mastery, style unlock/equip behavior, and Gameplay routing.
- Default lobby page is `Home`; Home has exactly three large menu actions: `수련`, `출전`, `연구`.
- Detailed pages return to Home through a back button; the old bottom navigation is removed.
- Home shows only current stage, difficulty, and starting weapon. It does not show `환도 비검 연구`, research tiers, or research achievement requirements.
- The normal runtime path must bind authored objects and must not create or replace the lobby UI hierarchy.
- UI images contain no baked text. All Korean copy is valid UTF-8 TextMeshPro text.
- Reuse existing `PremiumJoseon` 9-slice sprites. Generate missing pixel icons only with PixelLab, never ImageGen.
- Reference resolution is 1080×1920; validate 720×1280, 1080×1920, and 1080×2340 portrait layouts.
- Run one Unity process at a time. Set Unity and its active children to `BelowNormal` priority and processor affinity mask `255` (maximum eight logical cores).
- Preserve unrelated dirty files. Stage only the exact task files; never use `git add -A`.
- Every completed task receives implementation review, then a local commit and `git push origin master`.

---

### Task 1: Reusable Lobby Module Views and Prefab Contracts

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyPageId.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyHeaderView.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyPageHeaderView.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyProgressBarView.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyMenuCardView.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyDifficultyCardView.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbyModulePrefabBuilder.cs`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/CommonHeader.prefab`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/PageHeader.prefab`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/HomeMenuCard.prefab`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/InfoStrip.prefab`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/ProgressBar.prefab`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/DifficultyCard.prefab`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/PrimaryActionButton.prefab`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/SecondaryActionButton.prefab`
- Test: `Assets/JoseonHunter/Tests/EditMode/LobbyModulePrefabContractTests.cs`

**Interfaces:**
- Produces: `LobbyPageId { Home, Training, Patrol, Research }`.
- Produces: `LobbyHeaderView.Render(AccountLevelState account, int coins)` and `HasRequiredBindings`.
- Produces: `LobbyPageHeaderView.BackButton`, `Title`, and `Icon` accessors.
- Produces: `LobbyProgressBarView.Render(float normalized, string label)`.
- Produces: `LobbyMenuCardView.Button`, `Title`, `Description`, and `Icon` accessors.
- Produces: `LobbyDifficultyCardView.Render(string label, bool selected, bool locked)`.
- Produces: `LobbyModulePrefabBuilder.CreateOrValidateProductionModules()` and `BuildInBatchMode()`.
- Consumes: existing `PremiumPixelUiSkin`, `LobbyUiFactory`, TextMeshPro, and Unity UI.

- [ ] **Step 1: Record the mixed-worktree baseline without changing it**

Run:

```powershell
git status --short | Set-Content Artifacts/modular-lobby-worktree-baseline.txt
git diff -- Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab `
  Assets/JoseonHunter/Scenes/Lobby.unity `
  Assets/JoseonHunter/Scripts/Presentation/UI/PremiumPixelUiSkin.cs `
  Assets/JoseonHunter/Tests/EditMode/PremiumLobbyAssetContractTests.cs `
  | Set-Content Artifacts/modular-lobby-existing-dirty.diff
```

Expected: the baseline contains the known Lobby prefab/scene/skin changes plus unrelated art `.meta`, font, and capture changes. Do not stage either artifact.

- [ ] **Step 2: Write the failing module prefab contract**

Create `LobbyModulePrefabContractTests.cs` with explicit paths and assertions:

```csharp
[TestCase("CommonHeader", typeof(LobbyHeaderView))]
[TestCase("PageHeader", typeof(LobbyPageHeaderView))]
[TestCase("HomeMenuCard", typeof(LobbyMenuCardView))]
[TestCase("ProgressBar", typeof(LobbyProgressBarView))]
[TestCase("DifficultyCard", typeof(LobbyDifficultyCardView))]
public void ProductionModuleHasRequiredViewAndNoMissingScripts(string name, Type viewType)
{
    var path = $"Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/{name}.prefab";
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    Assert.That(prefab, Is.Not.Null, path);
    Assert.That(prefab.GetComponent(viewType), Is.Not.Null);
    Assert.That(prefab.GetComponentsInChildren<Transform>(true)
        .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)), Is.Zero);
}

[Test]
public void CreateOrValidateDoesNotOverwriteValidModules()
{
    LobbyModulePrefabBuilder.CreateOrValidateProductionModules();
    var before = File.ReadAllBytes(CommonHeaderPath);
    LobbyModulePrefabBuilder.CreateOrValidateProductionModules();
    Assert.That(File.ReadAllBytes(CommonHeaderPath), Is.EqualTo(before));
}
```

Also assert that each prefab root is `RectTransform`, required text/images/buttons are direct named descendants, all framed `Image` components use `Image.Type.Sliced`, and buttons expose Normal/Highlighted/Pressed/Disabled colors.

- [ ] **Step 3: Run the contract and capture RED**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform editmode `
  -Filter JoseonHunter.Tests.EditMode.LobbyModulePrefabContractTests
```

Expected: compile failure for missing view types or asset failures for missing module prefabs.

- [ ] **Step 4: Implement minimal view components**

Use serialized references and explicit binding validation. The progress component must preserve its authored Y/Z geometry:

```csharp
public void Render(float normalized, string label)
{
    normalized = Mathf.Clamp01(normalized);
    fill.fillAmount = normalized;
    valueText.text = label ?? string.Empty;
}

public bool HasRequiredBindings => fill != null && valueText != null;
```

`LobbyDifficultyCardView.Render` must call `PremiumPixelUiSkin.ApplyDifficulty(button, selected, locked)`, set the Korean label, keep the button visible for locked states, and set `button.interactable = !locked`.

- [ ] **Step 5: Implement the production module builder**

`CreateOrValidateProductionModules()` must:

1. create `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules` when absent;
2. load and strictly validate an existing prefab without saving it;
3. create only missing prefabs with the exact named child bindings;
4. use existing premium frames and actions;
5. save each new prefab through `PrefabUtility.SaveAsPrefabAsset`;
6. expose `BuildInBatchMode()` with exit code 0/1;
7. never open or modify `Lobby.unity`.

Use a table instead of duplicated path logic:

```csharp
private static readonly ModuleDefinition[] Definitions =
{
    new("CommonHeader", BuildCommonHeader, ValidateCommonHeader),
    new("PageHeader", BuildPageHeader, ValidatePageHeader),
    new("HomeMenuCard", BuildHomeMenuCard, ValidateHomeMenuCard),
    new("InfoStrip", BuildInfoStrip, ValidateInfoStrip),
    new("ProgressBar", BuildProgressBar, ValidateProgressBar),
    new("DifficultyCard", BuildDifficultyCard, ValidateDifficultyCard),
    new("PrimaryActionButton", BuildPrimaryActionButton, ValidateActionButton),
    new("SecondaryActionButton", BuildSecondaryActionButton, ValidateActionButton)
};
```

- [ ] **Step 6: Run the builder and GREEN contract**

Run the Editor builder once, then the focused test. Launch Unity sequentially with `BelowNormal` priority and affinity `255`.

Expected: builder exit 0; `LobbyModulePrefabContractTests` passes; a second builder invocation does not change any valid prefab hash.

- [ ] **Step 7: Review, commit, and push the task**

Stage only the new view scripts, their `.meta` files, builder, test, and eight module prefabs/metas.

```powershell
git diff --cached --check
git commit -m "feat: add reusable lobby UI modules"
git push origin master
```

---

### Task 2: Home Hub and Four-State Navigation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyHomeView.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyHomePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyNavigationPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyHomePlayModeTests.cs`

**Interfaces:**
- Consumes: `LobbyPageId`, three `LobbyMenuCardView` instances, `MetaGameSession.ActiveStageSelection`, `MetaGameSession.ActiveLoadout`, `StageCatalog`, and `WeaponCatalogAsset`.
- Produces: `LobbyNavigationPresenter.CurrentPage`, `Show(LobbyPageId page)`, and `ShowHome()`.
- Produces: `LobbyHomePresenter.Initialize(MetaGameSession session, WeaponCatalogAsset catalog)` and `Refresh()`.
- Produces: `LobbyViewModels.DifficultyName(StageDifficulty difficulty)` with `보통`, `흉조`, `대흉`.

- [ ] **Step 1: Replace the old navigation expectations with RED Home contracts**

The test fixture must create four pages and six navigation controls (three Home menu buttons and three Back buttons), then call the new initializer:

```csharp
presenter.Initialize(
    homePage, trainingPage, patrolPage, researchPage,
    trainingMenuButton, patrolMenuButton, researchMenuButton,
    trainingBackButton, patrolBackButton, researchBackButton);

Assert.That(presenter.CurrentPage, Is.EqualTo(LobbyPageId.Home));
Assert.That(homePage.activeSelf, Is.True);
Assert.That(new[] { trainingPage, patrolPage, researchPage }.Count(page => page.activeSelf), Is.Zero);
```

Click each Home menu button, assert exactly one corresponding page is active, click its Back button, and assert Home is the only active page. Call `Initialize` twice and verify one click causes one transition.

In `LobbyHomePlayModeTests`, assert the Home page has exactly three visible buttons with text `수련`, `출전`, `연구`, contains no `환도 비검 연구`, and renders selected stage/difficulty/starting weapon from the session.

- [ ] **Step 2: Run the two fixtures and capture RED**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode `
  -Filter JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode `
  -Filter JoseonHunter.Tests.PlayMode.LobbyHomePlayModeTests
```

Expected: missing Home types/signatures and old default-Patrol assertions fail.

- [ ] **Step 3: Implement the navigation state machine**

Use explicit state and listener cleanup:

```csharp
public void Show(LobbyPageId page)
{
    currentPage = page;
    homePage.SetActive(page == LobbyPageId.Home);
    trainingPage.SetActive(page == LobbyPageId.Training);
    patrolPage.SetActive(page == LobbyPageId.Patrol);
    researchPage.SetActive(page == LobbyPageId.Research);
}

public void ShowHome() => Show(LobbyPageId.Home);
```

`Bind()` must remove all listeners it owns before adding new ones. Do not apply old tab frames and do not create a compatibility `Bottom Navigation` object.

- [ ] **Step 4: Implement Home summary binding**

`LobbyHomePresenter.Refresh()` must read the active stage selection and loadout, then render:

```csharp
view.StageText.text = StageCatalog.TryGet(selection.StageId, out var stage)
    ? stage.DisplayName
    : "알 수 없는 지역";
view.DifficultyText.text = LobbyViewModels.DifficultyName(selection.Difficulty);
view.StartingWeaponText.text = LobbyViewModels.WeaponName(loadout.StartingWeapon);
view.StartingWeaponIcon.sprite = weaponCatalog != null &&
    weaponCatalog.TryGet(loadout.StartingWeapon, out var weapon)
        ? weapon.UiIcon != null ? weapon.UiIcon : weapon.PresentationSprites.FirstOrDefault()
        : null;
```

The Home presenter must not read or render style/research unlock state.

- [ ] **Step 5: Run GREEN tests and regression-check domain-free navigation**

Expected: both fixtures pass; repeated initialization does not duplicate listeners; no session data changes when navigating.

- [ ] **Step 6: Review, commit, and push the task**

```powershell
git diff --cached --check
git commit -m "feat: add lobby home navigation"
git push origin master
```

---

### Task 3: Pixel Icon Set and Semantic Skin Completion

**Files:**
- Create with PixelLab: `Assets/JoseonHunter/Art/UI/Lobby/Training/training_vitality.png`
- Create with PixelLab: `Assets/JoseonHunter/Art/UI/Lobby/Training/training_power.png`
- Create with PixelLab: `Assets/JoseonHunter/Art/UI/Lobby/Training/training_footwork.png`
- Create with PixelLab: `Assets/JoseonHunter/Art/UI/Lobby/Training/training_learning.png`
- Create with PixelLab: `Assets/JoseonHunter/Art/UI/Lobby/Training/training_guard.png`
- Create with PixelLab: `Assets/JoseonHunter/Art/UI/Lobby/Training/training_resonance.png`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/PremiumPixelUiSkin.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/LockSlashConstraint.cs`
- Preserve: `Assets/JoseonHunter/Scripts/Presentation/UI/LockSlashConstraint.cs.meta`
- Modify: `Assets/JoseonHunter/Tests/EditMode/PremiumLobbyAssetContractTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/LobbyTrainingIconAssetContractTests.cs`

**Interfaces:**
- Consumes: existing `PremiumFrame`, `PremiumActionStyle`, and `PremiumIcon` mappings.
- Produces: six 32×32 transparent, point-filtered, uncompressed training icons.
- Produces: standalone serializable `JoseonHunter.Presentation.UI.LockSlashConstraint`.

- [ ] **Step 1: Write RED icon/import and standalone lock contracts**

For every training icon assert:

```csharp
Assert.That(texture.width, Is.EqualTo(32));
Assert.That(texture.height, Is.EqualTo(32));
Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
Assert.That(importer.mipmapEnabled, Is.False);
Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
```

Retain the existing standalone `LockSlashConstraint` `MonoScript.GetClass()` contract, and add a bounds test at 720×1280 and 1080×2340 ensuring the slash and lock stay within the difficulty card rectangle.

- [ ] **Step 2: Run focused EditMode RED**

Expected: missing six icon assets; standalone lock script contract fails if the split files are not staged together.

- [ ] **Step 3: Generate the six icons in PixelLab**

Use `mcp__pixellab__create_map_object` with `width=32`, `height=32`, `view="side"`, `detail="low detail"`, `shading="flat shading"`, `outline="selective outline"`.

Use the shared style phrase in every description:

```text
single centered Joseon folk-fantasy mobile UI icon, antique gold and muted amber only,
dark brown selective outline, flat colors, no white outline, no text, no background,
clear silhouette readable at 32 pixels
```

Append one subject per icon: red-ginseng heart for vitality, clenched training fist for power, straw sandal with motion mark for footwork, open classical book for learning, round guardian shield for guard, small ritual bell with two resonance arcs for resonance.

Poll each job, inspect the original result, reject extra text/multiple objects/white halo/noisy shading, and regenerate only rejected subjects. Save accepted PNGs at the exact paths above.

- [ ] **Step 4: Import assets and finish semantic skin behavior**

Apply the approved importer settings synchronously. Keep `LockSlashConstraint` as one standalone file and remove the nested duplicate from `PremiumPixelUiSkin.cs`. Preserve existing `ApplyAction`, `ApplyDifficulty`, and `ApplyIcon` mappings; only add helper behavior needed by module views.

- [ ] **Step 5: Run focused GREEN and visually inspect icons at 1× and 4× nearest-neighbor scale**

Expected: contracts pass; all six icons use the same palette/outline density; no icon has a white border or unreadable internal noise.

- [ ] **Step 6: Review, commit, and push the task**

Stage only the six PNG/meta pairs, skin split, and two contract files.

```powershell
git diff --cached --check
git commit -m "feat: add modular lobby icon skin"
git push origin master
```

---

### Task 4: Authored Patrol Page Binding

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/PatrolPageView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs:18-529`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbyModulePrefabBuilder.cs`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/WeaponSelectorCard.prefab`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`

**Interfaces:**
- Consumes: three `LobbyDifficultyCardView` instances, `LobbyPageHeaderView`, `WeaponCatalogAsset`, `MetaGameSession`, and existing stage/loadout domain APIs.
- Produces: `PatrolPageView.HasRequiredBindings` and direct typed accessors for stage arrows, hero, difficulty cards, starting weapon selector, feedback, overlay, and `출전 시작` button.
- Preserves: `ConfigureCatalog`, `SelectStartingWeaponForTests`, stage browsing, difficulty selection, unlock feedback, and routing behavior.

- [ ] **Step 1: Add RED authored-identity and behavior assertions**

Load Lobby, cache `PatrolPageView`, its three difficulty cards, weapon selector, hero, and action button IDs. Initialize the presenter twice and assert the IDs do not change and no named control duplicates appear.

Retain behavior assertions for stage arrows, difficulty unlock, weapon save, and routing. Update visual assertions:

```csharp
Assert.That(selected.Background.sprite.name, Is.EqualTo("difficulty_selected"));
Assert.That(idle.Background.sprite.name, Is.EqualTo("difficulty_idle"));
Assert.That(locked.Background.sprite.name, Is.EqualTo("difficulty_locked"));
Assert.That(startButton.targetGraphic.GetComponent<Image>().sprite.name,
    Is.EqualTo("primary_red_button"));
```

Assert all three difficulty cards have equal width/height, locked overlays stay inside their card, and the visible action label is `출전 시작`.

- [ ] **Step 2: Run Patrol RED**

Expected: missing `PatrolPageView`, runtime Build creates/repairs controls, and old action text/geometry assertions fail.

- [ ] **Step 3: Move view references out of runtime construction**

Replace unconditional `Build()` in `Initialize()` with strict authored binding:

```csharp
public void Initialize(MetaGameSession value, Action onChanged)
{
    if (view == null || !view.HasRequiredBindings)
        throw new InvalidOperationException("PatrolPageView is incomplete.");
    session = value;
    refreshChrome = onChanged;
    BindListeners();
    LoadCurrentWeapon();
    LoadCurrentStage();
    Refresh();
}
```

Do not call `LobbyUiFactory` from the normal Patrol runtime path. The Editor builder owns construction. Preserve public test methods and exact domain command ordering.

- [ ] **Step 4: Add the reusable weapon selector module**

The module must expose icon, caption, weapon name, chevron, and a sliced button frame. Existing weapon overlay option names remain `Weapon Option {weaponId}` so current catalog binding remains valid.

- [ ] **Step 5: Normalize Patrol Korean copy**

Use `출전`, `출전 시작`, `시작 무기`, `보통`, `흉조`, `대흉`, `아직 준비 중인 지역입니다`, `출전 정보를 저장하지 못했습니다`, and `무기를 저장하지 못했습니다. 다시 시도해 주세요.`. Remove mojibake literals and update assertions to valid UTF-8.

- [ ] **Step 6: Run Patrol GREEN and regression fixture**

Run `LobbyPatrolPlayModeTests` and `StagePacingPlayModeTests`. Expected: both pass, selected difficulty uses the selected frame, and the known selected-vs-idle baseline failure is eliminated.

- [ ] **Step 7: Review, commit, and push the task**

```powershell
git diff --cached --check
git commit -m "refactor: bind authored patrol lobby page"
git push origin master
```

---

### Task 5: Authored Training Page Binding

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyTrainingRowView.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/TrainingPageView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs:12-234`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbyModulePrefabBuilder.cs`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/TrainingRow.prefab`
- Test: `Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes: six `LobbyTrainingRowView` instances keyed by `CommonTrainingId` and the six Task 3 icons.
- Produces: `TrainingPageView.HasRequiredBindings`, `Rows`, `CurrentEffectText`, `NextEffectText`, `CostText`, `PurchaseButton`, `ResetButton`, and `FeedbackText`.
- Preserves: `SelectForTests`, `PurchaseForTests`, `ResetForTests`, rank/cost/capacity/refund behavior.

- [ ] **Step 1: Write RED training row and identity tests**

Assert exactly six rows and this order: Vitality, Power, Footwork, Learning, Guard, Resonance. Every row must have one icon, Korean name, progress fill, and `rank / 20`. Assert purchase/reset and selected row state after repeated `Initialize` preserve instance IDs and one listener per button.

Keep domain assertions for account-level capacity, cost, effect preview, purchase, insufficient coins, maximum rank, and full refund.

- [ ] **Step 2: Run Training RED**

Expected: missing row/page views and old grid-card layout failures.

- [ ] **Step 3: Implement authored row rendering**

```csharp
public void Render(string label, Sprite icon, int rank, int maximum, bool selected)
{
    nameText.text = label;
    iconImage.sprite = icon;
    rankText.text = $"{rank} / {maximum}";
    progress.Render(maximum <= 0 ? 0f : (float)rank / maximum, string.Empty);
    LobbySelectionChrome.Apply(button, selected);
}
```

Refactor `CommonTrainingPresenter.Initialize` to require `TrainingPageView` and bind existing rows. It must not call `Build()` or archive children at runtime.

- [ ] **Step 4: Normalize training copy and bind icons**

Use names `활력`, `완력`, `보법`, `학습`, `수호`, `공명`; effect labels from `LobbyViewModels`; `현재 효과`, `강화 후 효과`, `필요 엽전`, `수련하기`, `전체 초기화`. Use the exact account-level capacity feedback already defined by the domain design.

- [ ] **Step 5: Run Training GREEN and progression regression**

Run `CommonTrainingLobbyPlayModeTests`, `CommonTrainingProgressionTests`, and `AccountProgressionTests`. Expected: all pass and scene object identities remain stable.

- [ ] **Step 6: Review, commit, and push the task**

```powershell
git diff --cached --check
git commit -m "refactor: bind authored training lobby page"
git push origin master
```

---

### Task 6: Authored Weapon Research Page Binding

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyResearchRowView.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/ResearchPageView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs:16-260`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbyModulePrefabBuilder.cs`
- Create through Editor API: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/ResearchRow.prefab`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes: existing eight-weapon catalog, three style definitions, `MetaGameSession`, and `LobbyProgressBarView`.
- Produces: `ResearchPageView.HasRequiredBindings`, eight weapon-select buttons, selected weapon summary, mastery progress, three `LobbyResearchRowView` instances, and page feedback.
- Preserves: `ConfigureCatalog`, `SelectedStyleStateForTests`, `ActivateStyleForTests`, `SelectWeaponForTests`, sequential unlock, purchase, equip, and save rules.

- [ ] **Step 1: Write RED research hierarchy and behavior tests**

Assert exactly eight weapon selectors and three research rows. Each row must show stage name, status, concise effect, cost/requirement, action, and an internal lock overlay. Cache row/button/summary IDs, initialize twice, and assert no duplicates.

Retain tests for starter Hwando styles, insufficient mastery, insufficient coins, sequential unlock, successful purchase, and equip persistence.

- [ ] **Step 2: Run Research RED**

Expected: missing page/row views and old large style-card geometry failures.

- [ ] **Step 3: Refactor the presenter to authored binding**

`Initialize` must reject incomplete `ResearchPageView`, bind the eight selectors and three rows once, then render. Do not call `Build()`, `ArchiveLegacyLayoutIfPresent`, or `LobbyUiFactory` at runtime.

Build the effect and requirement from the actual `WeaponMasteryStyleDefinition` fields before rendering. Do not introduce a second source of unlock rules:

```csharp
var effect = $"{style.Benefit} / {style.Tradeoff}";
var requirement = style.IsBase
    ? "처음부터 사용 가능"
    : style.IsStarterUnlocked
        ? "처음부터 해금"
        : $"숙련도 {mastery:N0}/{style.RequiredMastery:N0} · 엽전 {style.CoinCost:N0}";

row.Render(
    style.DisplayName,
    StateFor(index),
    effect,
    requirement,
    actionLabel,
    locked,
    canAct);
```

- [ ] **Step 4: Normalize research copy**

Use `무기 연구`, `숙련도`, `기본식`, `연구 중`, `해금 가능`, `해금 완료`, `장착 중`, `해금`, `장착`, and exact Korean sequential-lock feedback `2단계 연구 완료 시 해금`. Remove every mojibake preview/runtime literal in the touched presenter and view model.

- [ ] **Step 5: Run Research GREEN and mastery regression**

Run `WeaponResearchLobbyPlayModeTests`, `WeaponMasteryProgressionTests`, and weapon legacy/loadout EditMode tests referenced by the existing research fixture. Expected: all pass and data mutations match the pre-refactor behavior.

- [ ] **Step 6: Review, commit, and push the task**

```powershell
git diff --cached --check
git commit -m "refactor: bind authored weapon research page"
git push origin master
```

---

### Task 7: Compose and Validate the Scene-Authored Modular Lobby

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyRootView.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs:15-420`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs:16-356`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbyEditingTools.cs`
- Modify through Editor API: `Assets/JoseonHunter/Scenes/Lobby.unity`
- Delete after zero-reference audit: `Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab`
- Delete after zero-reference audit: `Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab.meta`
- Modify: `Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/LobbyModularSceneContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`

**Interfaces:**
- Consumes: Task 1 modules and Tasks 2/4/5/6 page views/presenters.
- Produces: direct `Lobby.unity` roots `Lobby Camera`, `Lobby Canvas`, `EventSystem` and the exact static Safe Area hierarchy in the design spec.
- Produces: `LobbyBootstrap.BindAuthoredView(MetaGameSession session)` and bind-only normal `Awake()`.
- Produces: `LobbySceneBuilder.BuildInBatchMode`, `ValidateInBatchMode`, and capture entry points.
- Produces: Editor menus to open Lobby scene, open a selected module prefab, validate modules, and explicitly rebuild Lobby.

- [ ] **Step 1: Write RED authored-scene contracts**

Open `Lobby.unity` additively and assert exactly one of every root/component. Under `Lobby Canvas/Safe Area` require:

```text
Background
Common Header
Home Page
Training Page
Patrol Page
Research Page
Settings Overlay
```

Assert `Bottom Navigation` is absent, Home contains exactly three `LobbyMenuCardView` instances, no Home TMP text contains `환도 비검 연구`, all detailed pages have one `LobbyPageHeaderView`, and all module instances retain a connected prefab source below `Prefabs/UI/Lobby/Modules`.

Assert one `LobbyBootstrap`, one `LobbyRootView`, one `LobbyNavigationPresenter`, one `EventSystem`, one `InputSystemUIInputModule`, no missing scripts, and no duplicate AudioListener.

Add a dirty-scene behavior test: mark an opened Lobby scene dirty with a marker, call explicit Build, expect `InvalidOperationException`, and assert marker/dirty state/instance IDs remain unchanged.

- [ ] **Step 2: Run scene-contract RED**

Expected: Home missing, old Bottom Navigation present, old LobbyShell connection present, and scene builder still destroys roots.

- [ ] **Step 3: Implement LobbyRootView and bind-only bootstrap**

`LobbyRootView` serializes header, Home presenter/view, navigation, three page presenters, settings overlay, and audio settings presenter. `HasRequiredBindings` must validate all refs.

Normal `LobbyBootstrap.Awake()` must:

```csharp
if (rootView == null || !rootView.HasRequiredBindings)
{
    Debug.LogError("Lobby authored view is incomplete. Runtime UI construction was skipped.");
    enabled = false;
    return;
}

var session = MetaGameSession.EnsureExists();
BindAuthoredView(session);
ApplySafeArea();
```

Delete runtime calls that construct Header, Stage Content, panels, Bottom Navigation, or settings. Keep safe-area calculation, music, audio settings binding, and header refresh.

- [ ] **Step 4: Replace destructive scene building with explicit modular composition**

The explicit builder may compose a new scene only after `RefuseDirtyLobby()` and module validation. It must instantiate module prefabs through `PrefabUtility.InstantiatePrefab`, wire serialized references with `SerializedObject`, and save a direct authored scene. It must not save a giant LobbyShell prefab.

`Validate()` must be read-only and reject duplicate roots, missing modules, disconnected module instances, missing input components, or an incomplete `LobbyRootView` before any mutation.

- [ ] **Step 5: Build the production scene and run GREEN contracts**

Run the module builder, explicit Lobby scene builder, `LobbyModulePrefabContractTests`, `LobbyModularSceneContractTests`, and `SceneScaffoldTests` sequentially.

Expected: all pass; Build Settings remain Bootstrap → Lobby → Gameplay; opening `Lobby.unity` shows the full UI hierarchy without entering Play Mode.

- [ ] **Step 6: Prove Home navigation and settings on the production scene**

Run `LobbyNavigationPlayModeTests` and audio settings tests. Assert Header and Settings object IDs remain constant across Home → page → Home transitions, only one page is active, and settings opens/closes from every page.

- [ ] **Step 7: Audit and remove the legacy shell**

Run:

```powershell
rg -n "LobbyShell|f32" Assets/JoseonHunter `
  --glob '!Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab' `
  --glob '!Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab.meta'
```

Resolve the actual prefab GUID from its `.meta` and repeat the search with that GUID. Delete only when both name and GUID production/test references are zero. Keep the legacy files if any legitimate reference remains and record the exact blocker instead of deleting them.

- [ ] **Step 8: Review, commit, and push the task**

Stage only the authored Lobby scene, bootstrap/builder/editing/view/test changes, and the verified legacy deletion. Exclude combat `.meta`, font SDF, and capture files.

```powershell
git diff --cached --check
git commit -m "feat: author modular lobby scene"
git push origin master
```

---

### Task 8: Portrait Capture, Cleanup, Full Verification, and Android Build

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/PremiumLobbyAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PremiumPixelUiSkinPlayModeTests.cs`
- Regenerate: `Artifacts/LobbyPremium/720x1280-home.png`
- Regenerate: `Artifacts/LobbyPremium/720x1280-training.png`
- Regenerate: `Artifacts/LobbyPremium/720x1280-patrol.png`
- Regenerate: `Artifacts/LobbyPremium/720x1280-research-ready.png`
- Regenerate: `Artifacts/LobbyPremium/1080x1920-home.png`
- Regenerate: `Artifacts/LobbyPremium/1080x1920-training.png`
- Regenerate: `Artifacts/LobbyPremium/1080x1920-patrol.png`
- Regenerate: `Artifacts/LobbyPremium/1080x1920-research-ready.png`
- Regenerate: `Artifacts/LobbyPremium/1080x2340-home.png`
- Regenerate: `Artifacts/LobbyPremium/1080x2340-training.png`
- Regenerate: `Artifacts/LobbyPremium/1080x2340-patrol.png`
- Regenerate: `Artifacts/LobbyPremium/1080x2340-research-ready.png`
- Create: `Docs/Verification/2026-08-10-modular-authored-lobby-redesign.md`

**Interfaces:**
- Consumes: final production Lobby scene and all lobby fixtures.
- Produces: native-resolution visual evidence, full EditMode/PlayMode results, Android development APK metadata, final zero-reference audit, and verification report.

- [ ] **Step 1: Make the capture harness render authored pages and valid Korean**

`CapturePreview()` must load `Lobby.unity`, bind deterministic preview data without mutating production save files, and call `LobbyNavigationPresenter.Show(...)` for Home, Training, Patrol, and Research. Add 1080×1920 to the resolution table. Remove every mojibake preview literal.

- [ ] **Step 2: Run focused lobby suites**

Run each fixture sequentially. `Test-Unity.ps1` forwards one literal Unity test filter, so do not join fixture names with `|`:

```powershell
$editFixtures = @(
  "JoseonHunter.Tests.EditMode.LobbyModulePrefabContractTests",
  "JoseonHunter.Tests.EditMode.LobbyModularSceneContractTests",
  "JoseonHunter.Tests.EditMode.PremiumLobbyAssetContractTests"
)
foreach ($fixture in $editFixtures) {
  powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
    -Platform editmode -Filter $fixture
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$playFixtures = @(
  "JoseonHunter.Tests.PlayMode.LobbyHomePlayModeTests",
  "JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests",
  "JoseonHunter.Tests.PlayMode.LobbyPatrolPlayModeTests",
  "JoseonHunter.Tests.PlayMode.CommonTrainingLobbyPlayModeTests",
  "JoseonHunter.Tests.PlayMode.WeaponResearchLobbyPlayModeTests",
  "JoseonHunter.Tests.PlayMode.PremiumPixelUiSkinPlayModeTests"
)
foreach ($fixture in $playFixtures) {
  powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
    -Platform playmode -Filter $fixture
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
```

Expected: zero failures. The previous Patrol selected-vs-idle failure must be absent.

- [ ] **Step 3: Generate all twelve native captures**

Run `LobbySceneBuilder.CapturePreviewInBatchMode` with graphics enabled; omit `-nographics`. Inspect each original, not a resized preview.

Acceptance:

- Header account/XP/coin/settings is readable and identical on every page.
- Home has exactly three large cards and no research summary.
- There is no bottom navigation.
- One primary crimson action dominates each detailed page.
- Patrol has equal difficulty cards, a contained lock/slash, compact hero/title/weapon/action spacing, and no oversized black void.
- Training rows show icon/name/progress/rank without overlap.
- Research rows show state/effect/requirement/action within their borders.
- No Korean text is mojibake, clipped, or baked into a bitmap.

- [ ] **Step 4: Run full EditMode and PlayMode suites**

Run one suite at a time with CPU controls. Record totals, failures, durations, and result XML paths. Any new failure blocks completion. If an unrelated pre-existing failure remains, reproduce it against commit `9f21f59` before classifying it as baseline.

- [ ] **Step 5: Audit missing scripts, references, and unintended dirty files**

Run:

```powershell
rg -n "m_Script: \{fileID: 0\}" Assets/JoseonHunter/Scenes Assets/JoseonHunter/Prefabs
git status --short
git diff --name-only 9f21f59..HEAD
```

Expected: no missing scripts in production scenes/prefabs; task commits contain only lobby modules/runtime/editor/tests/assets/docs. Unrelated combat art `.meta`, font SDF, and gameplay capture changes remain unstaged.

- [ ] **Step 6: Build Android development APK**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1
Get-Item Builds/Android/JoseonHunter-development.apk | Select-Object FullName,Length,LastWriteTime
Get-FileHash Builds/Android/JoseonHunter-development.apk -Algorithm SHA256
```

Expected: exit 0, ARM64/IL2CPP development APK exists, and size/SHA256 are recorded.

- [ ] **Step 7: Write the verification report**

Record focused/full test counts, capture paths and native acceptance, missing-script and legacy-shell audits, Android path/size/SHA256, CPU-control method, final commit range, and any retained unrelated dirty files.

- [ ] **Step 8: Final implementation review, commit, and push**

Request a final read-only review of the integrated diff. Resolve every P0/P1 and rerun the affected focused fixture before staging. Stage only final capture evidence, capture harness/test updates, and verification documentation.

```powershell
git diff --cached --check
git commit -m "test: verify modular authored lobby redesign"
git push origin master
```
