# Lobby Selection and Action Button Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the selected lobby difficulty/navigation unmistakable, present Great Omen as a visual lock, remove redundant stage-status copy, and replace unfinished solid action buttons with reusable Joseon pixel frames and semantic icons.

**Architecture:** Keep the existing runtime-built uGUI hierarchy. Add one shared `JoseonButtonSkin` for opt-in action-button frames/icons and one lobby-only `LobbySelectionChrome` for idempotent border and lock decoration; presenters explicitly apply these helpers so information/choice cards retain their current layout. Store generated transparent PNGs in `Resources`, validate their import contract, and test the resulting real runtime hierarchy in PlayMode.

**Tech Stack:** Unity 6.0.5 (`6000.5.5f1`), C#, uGUI, TextMeshPro, Unity Test Framework, PixelLab-generated PNG sprites, Git/GitHub.

## Global Constraints

- Work in `D:\UnityProjects\JoseonHunter` on `master`; commit and push every completed task to `origin/master`.
- Run Unity and PixelLab jobs sequentially so the project does not saturate CPU or memory.
- Preserve all pre-existing unrelated local `.meta` and font-asset changes; stage only files named by this plan.
- Use Korean text through TMP; do not bake Korean words into PNGs.
- New pixel sprites use transparent backgrounds, a restrained Joseon palette, Point filtering, no mipmaps, no white outline, and no anti-aliased fringe.
- Do not apply action-button chrome to upgrade cards, legacy cards, weapon-replacement choices, weapon-selection choices, or other information cards.
- Keep stage unlock rules, save data, stage ordering, combat balance, and routing behavior unchanged.
- The Great Omen button remains clickable so existing lock-reason feedback continues to work.

---

## File Map

- `Assets/JoseonHunter/Resources/UI/Buttons/button_primary_frame.png`: sliced bright-gold primary action frame.
- `Assets/JoseonHunter/Resources/UI/Buttons/button_secondary_frame.png`: sliced muted-gold secondary action frame.
- `Assets/JoseonHunter/Resources/UI/Buttons/icon_continue.png`: small gold play/continue glyph.
- `Assets/JoseonHunter/Resources/UI/Buttons/icon_lobby.png`: small simplified Joseon tiled-gate glyph.
- `Assets/JoseonHunter/Resources/Lobby/icon_lock.png`: compact gold padlock for the Great Omen seal.
- Corresponding `.meta` files: sprite import settings and 8-pixel sliced borders on the two frames.
- `Assets/JoseonHunter/Scripts/Presentation/UI/JoseonButtonSkin.cs`: opt-in action frame and semantic-icon application.
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbySelectionChrome.cs`: reusable outer/inner border and diagonal-lock hierarchy.
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`: selected difficulty, Great Omen lock, hidden status copy, and start-button skin.
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyNavigationPresenter.cs`: selected bottom-tab border.
- `Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs`: primary continue and secondary lobby-return buttons with icons.
- `Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs`: secondary lobby-return action with lobby icon.
- `Assets/JoseonHunter/Scripts/Presentation/UI/AudioSettingsPresenter.cs`: secondary close action.
- `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`: primary confirm action.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`: primary confirm action.
- `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponReplacementPresenter.cs`: secondary cancel action while leaving replacement-choice cards unchanged.
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`: primary purchase and secondary reset actions.
- `Assets/JoseonHunter/Tests/EditMode/JoseonButtonAssetContractTests.cs`: resource presence and sprite-import contract.
- `Assets/JoseonHunter/Tests/PlayMode/JoseonButtonSkinPlayModeTests.cs`: common frame/icon behavior and requested pause/result integrations.
- `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`: difficulty selection, status removal, lock seal, and start-button assertions.
- `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`: selected bottom-tab frame movement.
- `Docs/Verification/2026-08-08-lobby-selection-and-action-buttons.md`: exact test/capture evidence.

---

### Task 1: Pixel Button and Lock Asset Contract

**Files:**
- Create: `Assets/JoseonHunter/Tests/EditMode/JoseonButtonAssetContractTests.cs`
- Create: `Assets/JoseonHunter/Resources/UI/Buttons/button_primary_frame.png`
- Create: `Assets/JoseonHunter/Resources/UI/Buttons/button_secondary_frame.png`
- Create: `Assets/JoseonHunter/Resources/UI/Buttons/icon_continue.png`
- Create: `Assets/JoseonHunter/Resources/UI/Buttons/icon_lobby.png`
- Create: `Assets/JoseonHunter/Resources/Lobby/icon_lock.png`
- Create: all Unity `.meta` files generated for the preceding assets and any new directories

**Interfaces:**
- Consumes: Unity `Resources.Load<Sprite>(string)` and `TextureImporter`.
- Produces: resource paths `UI/Buttons/button_primary_frame`, `UI/Buttons/button_secondary_frame`, `UI/Buttons/icon_continue`, `UI/Buttons/icon_lobby`, and `Lobby/icon_lock`.

- [ ] **Step 1: Write the failing asset-contract test**

```csharp
[TestCase("UI/Buttons/button_primary_frame", true)]
[TestCase("UI/Buttons/button_secondary_frame", true)]
[TestCase("UI/Buttons/icon_continue", false)]
[TestCase("UI/Buttons/icon_lobby", false)]
[TestCase("Lobby/icon_lock", false)]
public void ButtonResourceUsesCrispSpriteImport(string resourcePath, bool sliced)
{
    var sprite = Resources.Load<Sprite>(resourcePath);
    Assert.That(sprite, Is.Not.Null, resourcePath);
    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
    Assert.That(importer.mipmapEnabled, Is.False);
    Assert.That(importer.alphaIsTransparency, Is.True);
    if (sliced) Assert.That(sprite.border.x, Is.GreaterThan(0f));
}
```

- [ ] **Step 2: Run the focused EditMode test and verify RED**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.JoseonButtonAssetContractTests' -testResults 'Artifacts\lobby-buttons-assets-red.xml' -logFile 'Artifacts\lobby-buttons-assets-red.log'
```

Expected: FAIL because at least `UI/Buttons/button_primary_frame` cannot be loaded.

- [ ] **Step 3: Generate the five approved PNGs with PixelLab, sequentially**

Use these exact visual briefs:

```text
button_primary_frame: transparent 64x32 pixel-art 9-slice frame, thick antique gold outer rail, one-pixel jade inner accent, deep dark-crimson center, squared Joseon joinery corners, maximum five colors, no text, no shadow, no white outline
button_secondary_frame: transparent 64x32 pixel-art 9-slice frame, muted antique gold outer rail, dark ink-brown center, restrained corner joints, maximum four colors, no text, no shadow, no white outline
icon_continue: transparent 24x24 pixel-art right-facing play arrow, antique gold body with one dark-brown shadow tone, no circle, no text, no white outline
icon_lobby: transparent 24x24 pixel-art simplified Joseon tiled gate, antique gold roof and dark-brown pillars, maximum four colors, no text, no white outline
icon_lock: transparent 24x24 pixel-art closed padlock, antique gold body and dark-brown keyhole/shadow, maximum three colors, no text, no white outline
```

Place final images at the resource paths in the file map. Do not retain generation previews inside `Assets`.

- [ ] **Step 4: Configure deterministic sprite import metadata**

Set all assets to `spriteMode: 1`, `filterMode: 0`, `enableMipMap: 0`, `alphaIsTransparency: 1`, `textureCompression: 0`, and a consistent pixels-per-unit value. Set both frame sprites to an 8-pixel border:

```yaml
spriteBorder: {x: 8, y: 8, z: 8, w: 8}
```

- [ ] **Step 5: Run the focused EditMode test and verify GREEN**

Run the Step 2 command with output files `lobby-buttons-assets-green.xml` and `lobby-buttons-assets-green.log`.

Expected: all five cases PASS.

- [ ] **Step 6: Commit and push the asset slice**

```powershell
git add -- 'Assets/JoseonHunter/Tests/EditMode/JoseonButtonAssetContractTests.cs' 'Assets/JoseonHunter/Tests/EditMode/JoseonButtonAssetContractTests.cs.meta' 'Assets/JoseonHunter/Resources/UI/Buttons' 'Assets/JoseonHunter/Resources/Lobby/icon_lock.png' 'Assets/JoseonHunter/Resources/Lobby/icon_lock.png.meta'
git commit -m 'art: add joseon action button sprites'
git push origin master
```

---

### Task 2: Reusable Action Button Skin and Presenter Integrations

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/JoseonButtonSkin.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/JoseonButtonSkinPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/AudioSettingsPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponReplacementPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`

**Interfaces:**
- Produces: `JoseonButtonStyle { Primary, Secondary }`, `JoseonButtonIcon { None, Continue, Lobby }`, and `JoseonButtonSkin.Apply(Button button, JoseonButtonStyle style, JoseonButtonIcon icon = JoseonButtonIcon.None)`.
- Guarantees: `button.targetGraphic` is a sliced `Image`; repeated application reuses one child named `Action Icon`; choice-card presenters are not modified.

- [ ] **Step 1: Write failing PlayMode tests for common skin and requested screens**

```csharp
[UnityTest]
public IEnumerator ApplyCreatesOneSlicedFrameAndOneSemanticIconIdempotently()
{
    var root = new GameObject("Root", typeof(RectTransform));
    var button = RuntimeUiFactory.Button("Action", root.transform, Color.black);
    JoseonButtonSkin.Apply(button, JoseonButtonStyle.Primary, JoseonButtonIcon.Continue);
    JoseonButtonSkin.Apply(button, JoseonButtonStyle.Primary, JoseonButtonIcon.Continue);
    Assert.That((button.targetGraphic as Image).type, Is.EqualTo(Image.Type.Sliced));
    Assert.That(button.transform.Cast<Transform>().Count(t => t.name == "Action Icon"), Is.EqualTo(1));
    Assert.That(button.transform.Find("Action Icon").GetComponent<Image>().sprite.name,
        Is.EqualTo("icon_continue"));
    Object.Destroy(root);
    yield return null;
}

[UnityTest]
public IEnumerator PauseAndResultButtonsUseFinishedFramesAndSemanticIcons()
{
    SceneManager.LoadScene("Gameplay");
    yield return null;
    yield return null;
    GameObject.Find("Pause Button").GetComponent<Button>().onClick.Invoke();
    yield return null;
    AssertAction("Continue Combat Button", "button_primary_frame", "icon_continue");
    AssertAction("Confirm Return Button", "button_secondary_frame", "icon_lobby");
}
```

Add a result-state assertion using the existing `FirstPlayableController.EndRunForTests(false)` setup and verify `Lobby Return Button` uses `button_secondary_frame` and `icon_lobby`.

- [ ] **Step 2: Run the focused PlayMode test and verify RED**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.JoseonButtonSkinPlayModeTests' -testResults 'Artifacts\button-skin-red.xml' -logFile 'Artifacts\button-skin-red.log'
```

Expected: compile/test FAIL because `JoseonButtonSkin` does not exist.

- [ ] **Step 3: Implement the minimal reusable skin**

Implement the public interface from this task. Load sprites once using `Resources.Load<Sprite>`, set the target image to `Image.Type.Sliced` and `Color.white`, configure bright but restrained `ColorBlock` states, and create/reuse the optional `Action Icon` child with `raycastTarget = false`, `preserveAspect = true`, a 28-pixel square, and a left inset. When an icon is present, inset the existing TMP label by 44 pixels on the left so icon and text never overlap.

Core dispatch must be explicit:

```csharp
var framePath = style == JoseonButtonStyle.Primary
    ? "UI/Buttons/button_primary_frame"
    : "UI/Buttons/button_secondary_frame";
var iconPath = icon == JoseonButtonIcon.Continue
    ? "UI/Buttons/icon_continue"
    : icon == JoseonButtonIcon.Lobby
        ? "UI/Buttons/icon_lobby"
        : null;
```

- [ ] **Step 4: Apply the skin only to action buttons**

Apply these mappings immediately after each button is created:

```text
Continue Combat Button       Primary   Continue
Confirm Return Button        Secondary Lobby
Lobby Return Button          Secondary Lobby
Close Audio Settings         Secondary None
Confirm Reward               Primary   None
Confirm Result               Primary   None
Cancel Replacement           Secondary None
Purchase Training            Primary   None
Reset Training               Secondary None
```

Do not modify `UpgradeChoicePresenter`, `WeaponLegacyChoicePresenter`, replacement-choice creation, weapon research choices, or patrol weapon choices.

- [ ] **Step 5: Run the focused PlayMode test and related presenter suites**

```powershell
$filter = 'JoseonHunter.Tests.PlayMode.JoseonButtonSkinPlayModeTests|JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests|JoseonHunter.Tests.PlayMode.RewardRevealPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponReplacementFlowPlayModeTests|JoseonHunter.Tests.PlayMode.RunSettlementLobbyPlayModeTests'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter $filter -testResults 'Artifacts\button-skin-green.xml' -logFile 'Artifacts\button-skin-green.log'
```

Expected: all selected suites PASS and no duplicate `Action Icon` exists.

- [ ] **Step 6: Commit and push the action-button slice**

Stage only the files listed in this task and their new `.meta` files, then:

```powershell
git commit -m 'feat: skin joseon action buttons'
git push origin master
```

---

### Task 3: Lobby Selection Borders, Great Omen Seal, and Status Removal

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbySelectionChrome.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyNavigationPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`

**Interfaces:**
- Produces: `LobbySelectionChrome.Apply(Button button, bool selected, bool locked = false)`.
- Hierarchy contract: `Selection Outer Border`, `Selection Inner Border`, `Lock Slash`, and `Lock Icon` are named, idempotent, raycast-transparent child objects.
- Consumes: `JoseonButtonSkin.Apply(patrolButton, JoseonButtonStyle.Primary)` for the large start action.

- [ ] **Step 1: Add failing lobby PlayMode assertions**

Extend the existing tests with these exact runtime contracts:

```csharp
Assert.That(GameObject.Find("Stage Status").activeSelf, Is.False);
Assert.That(FindIncludingInactive("Difficulty Normal").transform.Find("Selection Outer Border").gameObject.activeSelf, Is.True);
Assert.That(FindIncludingInactive("Difficulty Omen").transform.Find("Selection Outer Border").gameObject.activeSelf, Is.False);
Assert.That(GameObject.Find("Difficulty Great Omen").GetComponentInChildren<TMP_Text>().text, Is.EqualTo("대흉"));
Assert.That(FindIncludingInactive("Difficulty Great Omen").transform.Find("Lock Slash").gameObject.activeSelf, Is.True);
Assert.That(FindIncludingInactive("Difficulty Great Omen").transform.Find("Lock Icon").GetComponent<Image>().sprite.name, Is.EqualTo("icon_lock"));
Assert.That((GameObject.Find("Start Patrol").GetComponent<Button>().targetGraphic as Image).sprite.name,
    Is.EqualTo("button_primary_frame"));
```

For an account with Stage 1 Normal cleared, click Omen and assert the active outer/inner border moves from Normal to Omen. In `LobbyNavigationPlayModeTests`, assert the Patrol tab starts with an active outer border, click Weapon Research, and assert the same border moves to the Research tab.

- [ ] **Step 2: Run lobby tests and verify RED**

```powershell
$filter = 'JoseonHunter.Tests.PlayMode.LobbyPatrolPlayModeTests|JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter $filter -testResults 'Artifacts\lobby-selection-red.xml' -logFile 'Artifacts\lobby-selection-red.log'
```

Expected: FAIL because the named border/lock children are absent and status is active.

- [ ] **Step 3: Implement `LobbySelectionChrome`**

Create/reuse border roots by name. Each root contains four solid `Image` rails so the center remains transparent. Use these exact visual states:

```csharp
private static readonly Color SelectedGold = new(1f, .69f, .12f, 1f);
private static readonly Color SelectedJade = new(.12f, .80f, .68f, 1f);
private static readonly Color IdleBrown = new(.30f, .20f, .14f, 1f);
```

Selected: 5-pixel outer gold rails and 2-pixel inner jade rails. Unselected: disable both selected roots and show a 2-pixel idle border. Locked: create/reuse a 5-pixel diagonal seal line and a centered 24x24 `icon_lock`; keep both active while locked. All decoration images set `raycastTarget = false`.

- [ ] **Step 4: Update patrol refresh semantics**

In `PatrolPresenter.Refresh()`:

```csharp
stageStatusText.text = string.Empty;
stageStatusText.gameObject.SetActive(false);
JoseonButtonSkin.Apply(patrolButton, JoseonButtonStyle.Primary);
```

In `RefreshDifficultyButton`, keep `button.interactable = true`, set the label to `StageDifficultyNames.DisplayName(difficulty)` for both locked and unlocked states, and call:

```csharp
LobbySelectionChrome.Apply(button, selected, !unlocked);
```

Retain existing feedback text for a click on a locked difficulty.

- [ ] **Step 5: Update bottom-navigation selection semantics**

Keep current panel switching and label colors, then finish `ApplySelection` with:

```csharp
LobbySelectionChrome.Apply(button, selected);
```

The selected Patrol tab therefore uses the same bright outer/inner border as selected difficulty; unselected tabs retain only the thin idle border.

- [ ] **Step 6: Run lobby tests and verify GREEN**

Run the Step 2 command with output files `lobby-selection-green.xml` and `lobby-selection-green.log`.

Expected: all lobby patrol and navigation tests PASS.

- [ ] **Step 7: Commit and push the lobby-selection slice**

Stage only the files listed in this task and their new `.meta` files, then:

```powershell
git commit -m 'feat: clarify lobby selection and locks'
git push origin master
```

---

### Task 4: Regression Verification and Mobile Visual Evidence

**Files:**
- Modify if needed: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbyPremiumCapture.cs`
- Modify if needed: `Assets/JoseonHunter/Scripts/Editor/Scenes/PortraitStateValidationCapture.cs`
- Create: `Docs/Verification/2026-08-08-lobby-selection-and-action-buttons.md`

**Interfaces:**
- Consumes: production Lobby and Gameplay scenes and all presenter contracts from Tasks 1-3.
- Produces: 720x1280 lobby capture, portrait pause capture, test XML/log paths, and a concise verification record.

- [ ] **Step 1: Run the complete EditMode suite sequentially**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testResults 'Artifacts\lobby-buttons-full-editmode.xml' -logFile 'Artifacts\lobby-buttons-full-editmode.log'
```

Expected: zero failures/errors.

- [ ] **Step 2: Run the complete PlayMode suite sequentially**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testResults 'Artifacts\lobby-buttons-full-playmode.xml' -logFile 'Artifacts\lobby-buttons-full-playmode.log'
```

Expected: zero failures/errors.

- [ ] **Step 3: Capture real production lobby and pause states**

Run the existing production capture entry points without `-nographics`. If a current capture entry point does not expose the required state, make the smallest deterministic editor-only extension and test its policy before capturing.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -projectPath 'D:\UnityProjects\JoseonHunter' -executeMethod JoseonHunter.Editor.Scenes.LobbyPremiumCapture.CaptureInBatchMode -logFile 'Artifacts\lobby-selection-capture.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -projectPath 'D:\UnityProjects\JoseonHunter' -executeMethod JoseonHunter.Editor.Scenes.PortraitStateValidationCapture.CaptureInBatchMode -logFile 'Artifacts\pause-buttons-capture.log'
```

Inspect the 720x1280 lobby frame and the pause frame at original resolution. Verify: selected Omen/Normal and selected Patrol are obvious at phone size; Great Omen shows `대흉` plus slash and lock; no status line remains; primary/secondary frames remain crisp; icons do not overlap Korean labels; no white fringe or unintended anti-aliasing appears.

- [ ] **Step 4: Record exact evidence and final diff review**

Write the verification document with test totals, timestamps/result paths, capture paths/dimensions, and visual findings. Run:

```powershell
git diff --check
git status --short
git diff --stat HEAD
```

Confirm pre-existing unrelated `.meta` and font changes are not staged.

- [ ] **Step 5: Commit and push verification or final corrections**

```powershell
git add -- 'Docs/Verification/2026-08-08-lobby-selection-and-action-buttons.md'
git commit -m 'test: verify lobby and action button polish'
git push origin master
```

