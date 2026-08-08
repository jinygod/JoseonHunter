# Premium Lobby Fidelity Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the oversized architectural lobby chrome with a thin, modular PixelLab UI kit and rebuild research, patrol, training, and pause layouts to closely match the approved mobile mockup.

**Architecture:** Existing presenters retain ownership of data, events, navigation, audio, and save behavior. `PremiumPixelUiSkin` becomes the single semantic mapping from UI roles to the new PixelLab sprites, while each presenter owns only its layout and content hierarchy. `LobbySceneBuilder` serializes the final runtime composition into the production prefab and scene.

**Tech Stack:** Unity 6000.5.5f1, C# MonoBehaviours, uGUI, TextMeshPro, NUnit/Unity Test Framework, PixelLab MCP UI asset generation, Git on `master`.

## Global Constraints

- Work directly on `D:\UnityProjects\JoseonHunter` branch `master` and push every completed task to `origin/master`.
- Generate every new bitmap UI asset with PixelLab; do not use ImageGen or another raster generator.
- Do not bake Korean text into images.
- Use only black, ink brown, antique gold, muted crimson, and limited jade accents.
- Do not use white outlines, bright gray borders, gradients, dense decorative pixels, or oversized architectural frames.
- Preserve all existing public presenter behavior, save formats, progression balance, and button events.
- Use Point filtering, Sprite Single, alpha transparency, mipmaps disabled, and uncompressed texture import.
- Run Unity processes sequentially and never launch a second editor while one is active.
- Do not stage existing unrelated `Artifacts/**`, font mutations, or Unity-rewritten unrelated `.meta` files.

## File Map

- `Assets/JoseonHunter/Resources/UI/PremiumJoseon/`: replacement PixelLab sprites consumed at runtime.
- `Assets/JoseonHunter/Scripts/Editor/AssetProduction/PremiumLobbyUiAssetImporter.cs`: deterministic import and 9-slice borders.
- `Assets/JoseonHunter/Scripts/Presentation/UI/PremiumPixelUiSkin.cs`: semantic frame, action, navigation, and difficulty styling.
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`: common header, content bounds, settings, and bottom navigation shell.
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`: patrol-only hierarchy and anchors.
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs`: research-only hierarchy and anchors.
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`: training-only hierarchy and anchors.
- `Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs`: pause-only hierarchy and actions.
- `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`: production prefab/scene generation and captures.
- `Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab`: generated production shell.
- `Assets/JoseonHunter/Scenes/Lobby.unity`: generated production scene.
- `Docs/Verification/2026-08-08-premium-lobby-fidelity-rework.md`: final evidence record.

---

### Task 1: Generate the Thin Modular PixelLab Kit

**Files:**
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/thin_outer_frame.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/header_bar.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/stage_title_plate.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/content_backplate.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/difficulty_idle.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/difficulty_selected.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/difficulty_locked.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/weapon_selector_frame.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/primary_red_button.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/secondary_dark_button.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/tab_idle.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/tab_selected.png`
- Create or replace: `Assets/JoseonHunter/Resources/UI/PremiumJoseon/small_item_frame.png`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/PremiumLobbyUiAssetImporter.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/PremiumLobbyUiAssetContractTests.cs`

**Interfaces:**
- Consumes: PixelLab `create_ui_asset` and `get_ui_asset` jobs.
- Produces: thirteen transparent runtime sprites in `Resources/UI/PremiumJoseon`, using the exact filenames in this task.

- [ ] **Step 1: Replace the asset contract with exact new names and import requirements**

Add this parameterized contract:

```csharp
[TestCase("thin_outer_frame", true)]
[TestCase("header_bar", true)]
[TestCase("stage_title_plate", true)]
[TestCase("content_backplate", true)]
[TestCase("difficulty_idle", true)]
[TestCase("difficulty_selected", true)]
[TestCase("difficulty_locked", true)]
[TestCase("weapon_selector_frame", true)]
[TestCase("primary_red_button", true)]
[TestCase("secondary_dark_button", true)]
[TestCase("tab_idle", true)]
[TestCase("tab_selected", true)]
[TestCase("small_item_frame", true)]
public void FidelitySpriteUsesDeterministicPixelImport(string name, bool sliced)
{
    const string root = "Assets/JoseonHunter/Resources/UI/PremiumJoseon/";
    var path = root + name + ".png";
    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
    Assert.That(sprite, Is.Not.Null, path);
    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
    Assert.That(importer.mipmapEnabled, Is.False);
    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
    Assert.That(importer.spriteBorder.sqrMagnitude, sliced ? Is.GreaterThan(0f) : Is.EqualTo(0f));
}
```

- [ ] **Step 2: Run the asset contract and record the expected missing-asset failures**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.PremiumLobbyUiAssetContractTests' -testResults 'Artifacts\fidelity-assets-red.xml' -logFile 'Artifacts\fidelity-assets-red.log'
```

Expected: the thirteen new sprite cases fail before generation.

- [ ] **Step 3: Queue thirteen PixelLab UI jobs**

Use `mcp__pixellab__create_ui_asset` with transparent backgrounds. Keep every prompt explicit about thin borders and forbidden content. Start with these calls:

```json
{
  "name": "thin_outer_frame",
  "description": "very thin modular Joseon mobile game UI border, empty transparent center, 4 to 8 pixel antique gold outer line, ink black inner line, tiny muted crimson corner knots only, no building, no windows, no roof, no text, no white outline, no gradient, sparse clean pixels",
  "color_palette": "ink black, antique gold, muted crimson, tiny jade accent",
  "width": 384,
  "height": 688,
  "no_background": true,
  "elements": ["panel"]
}
```

```json
{
  "name": "difficulty_selected",
  "description": "small horizontal Joseon mobile game selection card, transparent center, thin bright antique gold double border and one muted crimson inner line, simple square corners, no jewel, no text, no white outline, no gradient, sparse pixels",
  "color_palette": "ink black, antique gold, muted crimson",
  "width": 600,
  "height": 448,
  "no_background": true,
  "elements": ["button"]
}
```

Create the remaining jobs with these explicit role-specific descriptions while retaining the forbidden-content clauses from the two calls above:

- `header_bar`: low horizontal black bar, one thin gold lower rule, no corner tower.
- `stage_title_plate`: narrow dark plaque, tiny gold end caps, no ribbon and no center jewel.
- `content_backplate`: nearly solid ink rectangle, 85% opaque appearance, one thin antique-gold border.
- `difficulty_idle`: dark thin border with muted gold.
- `difficulty_locked`: dark desaturated border without a baked slash or lock.
- `weapon_selector_frame`: low horizontal dark card with a small icon bay on the left.
- `primary_red_button`: muted crimson center, antique-gold 4-pixel border.
- `secondary_dark_button`: ink center, muted-gold 3-pixel border.
- `tab_idle`: square dark tab with thin muted-gold edge.
- `tab_selected`: square dark tab with gold double edge and crimson bottom accent.
- `small_item_frame`: compact dark card with thin gold edge and no central decoration.

- [ ] **Step 4: Poll jobs, inspect every preview, and reject decorative drift**

Poll every exact UUID returned by Step 3 through `mcp__pixellab__get_ui_asset` with `include_preview: true`. Record the UUID beside its target filename before polling so parallel results cannot be swapped.

Reject and regenerate any asset containing a building, roof, window lattice, ribbon, center gem, baked letters, white halo, dense texture noise, or corner ornament larger than 16 pixels.

- [ ] **Step 5: Download accepted PNGs to exact runtime paths**

For each completed job, copy the exact download URL returned by `get_ui_asset` into a task-specific PowerShell variable and download it to its recorded target filename. Example for the recorded `thin_outer_frame` job:

```powershell
$thinOuterDownloadUrl = $completedThinOuterJobDownloadUrl
Invoke-WebRequest -Uri $thinOuterDownloadUrl -OutFile 'D:\UnityProjects\JoseonHunter\Assets\JoseonHunter\Resources\UI\PremiumJoseon\thin_outer_frame.png'
```

Set `$completedThinOuterJobDownloadUrl` to the literal URL returned by the completed PixelLab result before executing the command. Repeat with separately named variables and the exact output names listed in this task.

- [ ] **Step 6: Update deterministic importer and apply it**

Replace the sliced name list with the thirteen new frame names and retain Point, no mipmaps, uncompressed, Sprite Single, alpha transparency, PPU 100. Use a 12-pixel border for compact cards/buttons and a 16-pixel border for `thin_outer_frame`.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -executeMethod JoseonHunter.Editor.AssetProduction.PremiumLobbyUiAssetImporter.Apply -logFile 'Artifacts\fidelity-assets-import.log' -quit
```

- [ ] **Step 7: Run the asset contract to green**

Run the Step 2 command with `fidelity-assets-green.xml`. Expected: all thirteen cases pass.

- [ ] **Step 8: Commit and push the accepted kit**

```powershell
git add -- 'Assets/JoseonHunter/Resources/UI/PremiumJoseon' 'Assets/JoseonHunter/Scripts/Editor/AssetProduction/PremiumLobbyUiAssetImporter.cs' 'Assets/JoseonHunter/Tests/EditMode/PremiumLobbyUiAssetContractTests.cs'
git commit -m "art: replace oversized lobby chrome with thin pixel kit"
git push origin master
```

---

### Task 2: Replace Semantic Skin Mappings

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/PremiumPixelUiSkin.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/JoseonButtonSkin.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbySelectionChrome.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PremiumPixelUiSkinPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/JoseonButtonSkinPlayModeTests.cs`

**Interfaces:**
- Consumes: the thirteen new PixelLab sprites.
- Produces: `PremiumFrame`, `PremiumActionStyle`, `ApplyFrame`, `ApplyAction`, `ApplyNavigation`, and `ApplyDifficulty`.

- [ ] **Step 1: Write failing semantic skin tests**

Add tests with these exact expectations:

```csharp
Assert.That(ApplyFrameAndReturnSprite(PremiumFrame.ThinOuter).name, Is.EqualTo("thin_outer_frame"));
Assert.That(ApplyFrameAndReturnSprite(PremiumFrame.HeaderBar).name, Is.EqualTo("header_bar"));
Assert.That(ApplyFrameAndReturnSprite(PremiumFrame.ContentBackplate).name, Is.EqualTo("content_backplate"));

PremiumPixelUiSkin.ApplyAction(button, PremiumActionStyle.Primary);
Assert.That(((Image)button.targetGraphic).sprite.name, Is.EqualTo("primary_red_button"));

PremiumPixelUiSkin.ApplyDifficulty(button, selected: true, locked: false);
Assert.That(((Image)button.targetGraphic).sprite.name, Is.EqualTo("difficulty_selected"));
```

Add this test helper to the fixture:

```csharp
private static Sprite ApplyFrameAndReturnSprite(PremiumFrame frame)
{
    var root = new GameObject("Frame Test", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
    var image = root.GetComponent<Image>();
    PremiumPixelUiSkin.ApplyFrame(image, frame);
    var sprite = image.sprite;
    Object.DestroyImmediate(root);
    return sprite;
}
```

Also verify locked difficulty uses `difficulty_locked`, the runtime-created slash and lock remain inside the button rect, and navigation maps to `tab_idle`/`tab_selected`.

- [ ] **Step 2: Run focused skin tests and verify old mappings fail**

```powershell
$filter='JoseonHunter.Tests.PlayMode.PremiumPixelUiSkinPlayModeTests|JoseonHunter.Tests.PlayMode.JoseonButtonSkinPlayModeTests'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter $filter -testResults 'Artifacts\fidelity-skin-red.xml' -logFile 'Artifacts\fidelity-skin-red.log'
```

- [ ] **Step 3: Define exact semantic enums**

```csharp
public enum PremiumFrame
{
    ThinOuter,
    HeaderBar,
    StageTitlePlate,
    ContentBackplate,
    DifficultyIdle,
    DifficultySelected,
    DifficultyLocked,
    WeaponSelector,
    TabIdle,
    TabSelected,
    SmallItem,
    HeroOval
}

public enum PremiumActionStyle
{
    Primary,
    Secondary
}
```

- [ ] **Step 4: Implement one-to-one sprite mapping and action styling**

Add:

```csharp
public static void ApplyAction(Button button, PremiumActionStyle style)
{
    var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
    LoadAndApply(image, style == PremiumActionStyle.Primary
        ? "primary_red_button"
        : "secondary_dark_button", sliced: true);
    button.targetGraphic = image;
    button.transition = Selectable.Transition.ColorTint;
}
```

Update `ApplyDifficulty` and `ApplyNavigation` to select distinct sprite files instead of tinting duplicated frames. Clamp `Lock Slash` anchors to `.12f` and `.88f` and lock icon size to 30% of card height.

- [ ] **Step 5: Keep `JoseonButtonSkin` as a compatibility facade**

Map existing `JoseonButtonStyle.Primary` and `Secondary` calls to `PremiumPixelUiSkin.ApplyAction`. Preserve existing icon child creation and button click behavior.

- [ ] **Step 6: Run focused skin tests to green**

Run the Step 2 command with `fidelity-skin-green.xml`. Expected: all focused tests pass.

- [ ] **Step 7: Commit and push semantic skin replacement**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/PremiumPixelUiSkin.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/JoseonButtonSkin.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbySelectionChrome.cs' 'Assets/JoseonHunter/Tests/PlayMode/PremiumPixelUiSkinPlayModeTests.cs' 'Assets/JoseonHunter/Tests/PlayMode/JoseonButtonSkinPlayModeTests.cs'
git commit -m "feat: map lobby controls to thin semantic frames"
git push origin master
```

---

### Task 3: Rebuild the Common Lobby Shell

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyNavigationPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs`

**Interfaces:**
- Consumes: `PremiumFrame.HeaderBar`, `PremiumFrame.ThinOuter`, and tab semantic styles.
- Produces: the shared 1080×1920-safe shell used by all three lobby panels.

- [ ] **Step 1: Write failing common-shell layout tests**

Assert these anchors with tolerance `.005f`:

```csharp
AssertAnchors("Header", new Vector2(.025f, .91f), new Vector2(.975f, .985f));
AssertAnchors("Stage Content", new Vector2(.04f, .105f), new Vector2(.96f, .895f));
AssertAnchors("Bottom Navigation", new Vector2(.04f, .02f), new Vector2(.96f, .095f));
Assert.That(ImageNamed("Header").sprite.name, Is.EqualTo("header_bar"));
Assert.That(ImageNamed("Patrol Panel").sprite.name, Is.EqualTo("thin_outer_frame"));
Assert.That(VisibleNavigationLabels(), Is.Empty);
```

Add these helpers to `LobbyNavigationPlayModeTests`:

```csharp
private static RectTransform RectNamed(string name) =>
    GameObject.Find(name).GetComponent<RectTransform>();

private static Image ImageNamed(string name) =>
    GameObject.Find(name).GetComponent<Image>();

private static string[] VisibleNavigationLabels() =>
    GameObject.Find("Bottom Navigation").GetComponentsInChildren<TMP_Text>(false)
        .Where(label => label.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(label.text))
        .Select(label => label.text).ToArray();

private static void AssertAnchors(string name, Vector2 minimum, Vector2 maximum)
{
    var rect = RectNamed(name);
    Assert.That(rect.anchorMin.x, Is.EqualTo(minimum.x).Within(.005f), name + " min x");
    Assert.That(rect.anchorMin.y, Is.EqualTo(minimum.y).Within(.005f), name + " min y");
    Assert.That(rect.anchorMax.x, Is.EqualTo(maximum.x).Within(.005f), name + " max x");
    Assert.That(rect.anchorMax.y, Is.EqualTo(maximum.y).Within(.005f), name + " max y");
}
```

- [ ] **Step 2: Run lobby navigation and scene contract fixtures to verify failure**

```powershell
$filter='JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter $filter -testResults 'Artifacts\fidelity-shell-red.xml' -logFile 'Artifacts\fidelity-shell-red.log'
```

- [ ] **Step 3: Apply the approved common anchor map**

Update shell anchors to the values from Step 1. Use `HeaderBar` only on the header and `ThinOuter` only on the active content panel. Keep the pixel courtyard full bleed. Do not create any inner architectural frame.

- [ ] **Step 4: Rebuild the header hierarchy**

Retain the existing account badge, name, experience fill, coin icon, coin text, and settings button. Place account information in the left 62% and currency/settings in the right 34%. Apply `secondary_dark_button` to the settings button with the existing gear icon centered.

- [ ] **Step 5: Rebuild bottom navigation as three equal icon-only tabs**

Keep `HorizontalLayoutGroup`, set spacing to 6 reference pixels, and apply `tab_selected` only to the active tab. Hide or remove every TMP label under the three navigation buttons.

- [ ] **Step 6: Run navigation and shell contracts to green**

Run the PlayMode command from Step 2 and the EditMode `LobbySceneContractTests` fixture. Expected: zero failures.

- [ ] **Step 7: Commit and push the common shell**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyNavigationPresenter.cs' 'Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs' 'Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs'
git commit -m "feat: rebuild lobby shell to approved proportions"
git push origin master
```

---

### Task 4: Rebuild the Patrol Screen to Match the Mockup

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`

**Interfaces:**
- Consumes: stage plaque, difficulty, weapon selector, action, tab, and hero frame semantic styles.
- Produces: mockup-order patrol hierarchy without overlap.

- [ ] **Step 1: Write failing anchor and state tests**

Assert the following reference anchors:

```text
Stage Plaque:          (.18,.875) to (.82,.95)
Previous Stage:        (.04,.875) to (.16,.95)
Next Stage:            (.84,.875) to (.96,.95)
Patrol Hero Frame:     (.30,.55)  to (.70,.84)
Difficulty Normal:     (.055,.43) to (.35,.535)
Difficulty Omen:       (.352,.43) to (.648,.535)
Difficulty Great Omen: (.65,.43)  to (.945,.535)
Starting Weapon:       (.12,.285) to (.88,.405)
Start Patrol:          (.20,.09)  to (.80,.235)
```

Assert exact sprite names `stage_title_plate`, `difficulty_selected`, `difficulty_idle`, `difficulty_locked`, `weapon_selector_frame`, and `primary_red_button`.

- [ ] **Step 2: Run patrol tests and confirm anchor/sprite failures**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.LobbyPatrolPlayModeTests' -testResults 'Artifacts\fidelity-patrol-red.xml' -logFile 'Artifacts\fidelity-patrol-red.log'
```

- [ ] **Step 3: Recompose `Build` and `EnsureStageControls` around one anchor table**

Extract a private helper:

```csharp
private static void Anchor(RectTransform rect, Vector2 min, Vector2 max) =>
    LobbyUiFactory.Anchor(rect, min, max, Vector2.zero, Vector2.zero);
```

Apply the exact values from Step 1 in both generated and existing-view paths. Remove the oversized hero shadow and use only a small oval shadow behind the character.

- [ ] **Step 4: Apply explicit state frames**

Use `DifficultySelected`, `DifficultyIdle`, and `DifficultyLocked` without additional whole-card tinting. Keep label text high-contrast Hanji. Keep `대흉` text unchanged and overlay the runtime slash and lock only when locked.

- [ ] **Step 5: Simplify the starting weapon card**

Use one left icon bay, one small `시작 무기` caption, one weapon name, and one right chevron. Remove every decorative child not used by those four elements.

- [ ] **Step 6: Run patrol and navigation regression tests**

Run `LobbyPatrolPlayModeTests|LobbyNavigationPlayModeTests`. Expected: all pass.

- [ ] **Step 7: Commit and push the patrol screen**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs' 'Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs'
git commit -m "feat: match patrol lobby to approved mockup"
git push origin master
```

---

### Task 5: Rebuild Research and Training as Independent Screens

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes: `ContentBackplate`, `SmallItem`, and action semantic styles.
- Produces: compact research and training layouts that fit one portrait screen.

- [ ] **Step 1: Write failing research composition tests**

Require these named objects and roles:

```csharp
Assert.That(ImageNamed("Research Progress Backplate").sprite.name, Is.EqualTo("content_backplate"));
Assert.That(ButtonsUnder("Weapon Grid"), Has.Count.EqualTo(8));
Assert.That(ImageNamed("Style Card 0").sprite.name, Is.EqualTo("content_backplate"));
Assert.That(ImageNamed("Style Card 1").sprite.name, Is.EqualTo("content_backplate"));
Assert.That(ImageNamed("Style Card 2").sprite.name, Is.EqualTo("content_backplate"));
AssertNoOverlap("Weapon Grid", "Style Card 0", "Style Card 1", "Style Card 2");
```

Add these helpers to `WeaponResearchLobbyPlayModeTests`:

```csharp
private static Image ImageNamed(string name) => GameObject.Find(name).GetComponent<Image>();

private static Button[] ButtonsUnder(string name) =>
    GameObject.Find(name).GetComponentsInChildren<Button>(true);

private static Rect WorldRect(RectTransform rect)
{
    var corners = new Vector3[4];
    rect.GetWorldCorners(corners);
    return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
}

private static void AssertNoOverlap(params string[] names)
{
    var rects = names.Select(name => (name, rect: WorldRect(GameObject.Find(name).GetComponent<RectTransform>())))
        .ToArray();
    for (var left = 0; left < rects.Length; left++)
    for (var right = left + 1; right < rects.Length; right++)
        Assert.That(rects[left].rect.Overlaps(rects[right].rect), Is.False,
            rects[left].name + " overlaps " + rects[right].name);
}
```

The mastery label must use `연구 중 {current:N0} / {required:N0}` and each style card must contain no more than three visible text lines.

- [ ] **Step 2: Write failing training composition tests**

Require six `small_item_frame` stat cards in a 3×2 grid, one `Training Summary Backplate`, one primary `Purchase Training`, and one secondary `Reset Training`. Assert both buttons remain inside the content panel and do not overlap.

- [ ] **Step 3: Run both fixtures and verify composition failures**

```powershell
$filter='JoseonHunter.Tests.PlayMode.WeaponResearchLobbyPlayModeTests|JoseonHunter.Tests.PlayMode.CommonTrainingLobbyPlayModeTests'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter $filter -testResults 'Artifacts\fidelity-meta-red.xml' -logFile 'Artifacts\fidelity-meta-red.log'
```

- [ ] **Step 4: Recompose research layout**

Use this vertical map:

```text
Title and weapon icon: .86-.96
Mastery backplate/bar: .77-.855
Weapon grid 4×2:       .57-.755
Style card 0:          .42-.555
Style card 1:          .275-.41
Style card 2:          .13-.265
```

Use `GridLayoutGroup` with four fixed columns. Keep every weapon-selection click listener and unlock rule unchanged.

- [ ] **Step 5: Recompose training layout**

Use this vertical map:

```text
Title and cap copy: .84-.96
Stat grid 3×2:      .58-.82
Summary backplate:  .31-.56
Purchase button:    .12-.27, left 58%
Reset button:       .12-.27, right 34%
```

Keep training selection, cost calculation, purchase, reset, account-level cap, and feedback behavior unchanged.

- [ ] **Step 6: Run research and training fixtures to green**

Run the Step 3 command with `fidelity-meta-green.xml`. Expected: zero failures.

- [ ] **Step 7: Commit and push both independent menu layouts**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs' 'Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs' 'Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs'
git commit -m "feat: compact research and training lobby layouts"
git push origin master
```

---

### Task 6: Rebuild Pause with the Thin Frame

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PremiumPauseUiPlayModeTests.cs`

**Interfaces:**
- Consumes: `ThinOuter`, `ContentBackplate`, and primary/secondary action styles.
- Produces: a compact pause settings modal with no redundant settings button.

- [ ] **Step 1: Replace pause visual assertions**

Assert `Abandon Panel` uses `thin_outer_frame`, a child `Pause Backplate` uses `content_backplate`, title and message bounds are within the panel, two sliders exist, and the only buttons are `Continue Combat Button` and `Confirm Return Button`.

- [ ] **Step 2: Run the pause fixture and verify old frame failure**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.PremiumPauseUiPlayModeTests' -testResults 'Artifacts\fidelity-pause-red.xml' -logFile 'Artifacts\fidelity-pause-red.log'
```

- [ ] **Step 3: Recompose the modal**

Retain the existing 936×840 logical size. Place a dark `Pause Backplate` inside the thin outer frame with 24 reference-pixel margins. Keep the proven final vertical positions for title, message, divider, audio settings, continue, and return, but use new action sprites and ensure the backplate sits behind every interactive child.

- [ ] **Step 4: Run pause, audio, and modal flow regression tests**

```powershell
$filter='JoseonHunter.Tests.PlayMode.PremiumPauseUiPlayModeTests|JoseonHunter.Tests.PlayMode.GameAudioIntegrationPlayModeTests|JoseonHunter.Tests.PlayMode.ModalGameFlowPlayModeTests'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter $filter -testResults 'Artifacts\fidelity-pause-green.xml' -logFile 'Artifacts\fidelity-pause-green.log'
```

- [ ] **Step 5: Commit and push pause fidelity changes**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs' 'Assets/JoseonHunter/Tests/PlayMode/PremiumPauseUiPlayModeTests.cs'
git commit -m "feat: apply thin premium frame to pause settings"
git push origin master
```

---

### Task 7: Rebuild Production Assets and Perform Side-by-Side Acceptance

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`
- Modify: `Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab`
- Modify: `Assets/JoseonHunter/Scenes/Lobby.unity`
- Create: `Docs/Verification/2026-08-08-premium-lobby-fidelity-rework.md`
- Delete after reference audit: obsolete oversized frame PNGs under `Assets/JoseonHunter/Resources/UI/PremiumJoseon/`

**Interfaces:**
- Consumes: all completed semantic skins and presenter layouts.
- Produces: production scene/prefab, full-suite evidence, and inspected portrait captures.

- [ ] **Step 1: Update editor semantic assignments**

Remove every reference to `panel_frame`, `card_idle_frame`, `card_selected_frame`, `stage_plaque_frame`, `nav_idle_frame`, and `nav_selected_frame`. Apply only the new semantic skin functions; retain direct AssetDatabase assignment for courtyard, coin, heroine, and weapon catalog.

- [ ] **Step 2: Rebuild Lobby prefab and scene**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -executeMethod JoseonHunter.Editor.Scenes.LobbySceneBuilder.BuildInBatchMode -logFile 'Artifacts\fidelity-lobby-build.log'
```

Expected log: `JoseonHunter Lobby presentation built.`

- [ ] **Step 3: Run complete EditMode suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testResults 'Artifacts\fidelity-full-editmode.xml' -logFile 'Artifacts\fidelity-full-editmode.log'
```

Expected: zero failures.

- [ ] **Step 4: Run complete PlayMode suite**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testResults 'Artifacts\fidelity-full-playmode.xml' -logFile 'Artifacts\fidelity-full-playmode.log'
```

Expected: zero failures.

- [ ] **Step 5: Capture all required screens sequentially with graphics enabled**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -projectPath 'D:\UnityProjects\JoseonHunter' -executeMethod JoseonHunter.Editor.Scenes.LobbySceneBuilder.CapturePreviewInBatchMode -logFile 'Artifacts\fidelity-lobby-capture.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -projectPath 'D:\UnityProjects\JoseonHunter' -executeMethod JoseonHunter.Editor.Scenes.PortraitStateValidationCapture.CaptureInBatchMode -logFile 'Artifacts\fidelity-pause-capture.log'
```

- [ ] **Step 6: Inspect six original-resolution captures**

Inspect:

- `Artifacts/LobbyPremium/720x1280-patrol.png`
- `Artifacts/LobbyPremium/720x1280-research-ready.png`
- `Artifacts/LobbyPremium/720x1280-training.png`
- `Artifacts/LobbyPremium/1080x2340-patrol.png`
- `Artifacts/LobbyPremium/1080x2340-research-ready.png`
- `Artifacts/PortraitValidation/720x1280/04-pause.png`

Compare patrol directly with the approved mock. Reject the implementation if any oversized architectural rail remains, if card text touches borders, if the lock slash leaves its card, if selected state is unclear, or if bottom labels appear.

- [ ] **Step 7: Audit and delete only unreferenced obsolete UI sprites**

```powershell
rg -n "panel_frame|card_idle_frame|card_selected_frame|stage_plaque_frame|nav_idle_frame|nav_selected_frame" Assets/JoseonHunter
```

Delete an obsolete PNG and its `.meta` only when the search returns no production or test reference after the semantic migration. Keep shared icons and `hero_oval_frame` when still referenced.

- [ ] **Step 8: Write final verification record**

Record Unity version, exact commands, EditMode and PlayMode totals, inspected paths, side-by-side acceptance results, deleted obsolete assets, and non-blocking warnings in `Docs/Verification/2026-08-08-premium-lobby-fidelity-rework.md`.

- [ ] **Step 9: Commit and push production scene and evidence**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs' 'Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab' 'Assets/JoseonHunter/Scenes/Lobby.unity' 'Assets/JoseonHunter/Resources/UI/PremiumJoseon' 'Docs/Verification/2026-08-08-premium-lobby-fidelity-rework.md'
git diff --cached --name-only
git commit -m "test: verify high-fidelity premium lobby rework"
git push origin master
```

Do not stage `Artifacts/**`, unrelated font assets, or unrelated Unity-rewritten `.meta` files.

---

## Completion Checklist

- [ ] The center content no longer uses an architectural building frame.
- [ ] Every new bitmap came from PixelLab and contains no baked text.
- [ ] Patrol matches the approved mock's order, spacing, and hierarchy.
- [ ] Research and training use compact independent cards rather than one giant shared frame.
- [ ] Difficulty and bottom-tab selected states use distinct sprites.
- [ ] Locked difficulty slash and lock remain inside their card.
- [ ] Pause uses the thin frame and contains only the two required action buttons.
- [ ] Existing progression, save, navigation, audio, and button behavior remains unchanged.
- [ ] Complete EditMode and PlayMode suites have zero failures.
- [ ] All six captures were inspected at original resolution before completion.
- [ ] Every task commit was pushed to `origin/master` without unrelated staged files.
