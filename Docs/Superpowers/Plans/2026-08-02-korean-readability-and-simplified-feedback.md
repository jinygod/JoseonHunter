# Korean Readability and Simplified Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Replace mismatched high-detail UI/combat visuals with readable low-color presentation, enlarge combat feedback, and remove player-facing English from the HUD and run result.

**Architecture:** Keep combat rules in FirstPlayableController and weapon executors, but give UI icons, potential icons, and world presentation separate asset channels. Add a Canvas-owned run-result presenter, extend the existing UI state with read-only result flags, and keep transient world visuals pooled or bounded. Preserve runtime composition and avoid scene/prefab edits.

**Tech Stack:** Unity 6000.5.5f1, C#, uGUI, TextMeshPro, URP 2D, NUnit EditMode/PlayMode tests, built-in image generation and nearest-neighbor PNG post-processing.

## Global Constraints

- Work directly on master; commit and push each completed task to origin/master.
- Do not stage the user-owned changes in Gameplay.unity, ProjectSettings.asset, the two MaruBuri dynamic SDF assets, or .utmp/.
- Do not change combat damage, attraction radius 0.58, collection distance 0.42, potential hit masks, or weapon presentation frame indices.
- Do not use white outlines, high-detail metal trim, multicolor gradients, or more than five colors in the new world/UI sprites.
- Do not add packages, Mecanim controllers, scene wiring, or per-frame managed allocations.
- All player-facing runtime labels introduced or touched by this plan must be Korean.
- Android sprites remain point-filtered, uncompressed RGBA32, mipmaps off, and 32 pixels per unit.

---

### Task 1: Separate UI icons and author simplified sprites

**Files:**
- Create: Assets/JoseonHunter/Art/Weapons/Runtime/gakgung_shot/ui-icon.png
- Create: Assets/JoseonHunter/Art/Weapons/Runtime/gakgung_shot/ui-icon.png.meta
- Create: Assets/JoseonHunter/Art/VFX/JangseungGeumjul/jangseung_guardian_descent.png
- Create: Assets/JoseonHunter/Art/VFX/JangseungGeumjul/jangseung_guardian_descent.png.meta
- Modify: Assets/JoseonHunter/Scripts/Content/Weapons/WeaponDefinitionAsset.cs
- Modify: Assets/JoseonHunter/Content/Weapons/GakgungShot.asset
- Modify: Assets/JoseonHunter/Scripts/Runtime/Gameplay/JangseungGeumjulVisualLibrary.cs
- Modify: Assets/JoseonHunter/Scripts/Editor/AssetProduction/JangseungGeumjulAssetImporter.cs
- Modify: Assets/JoseonHunter/Resources/Presentation/JangseungGeumjulVisualLibrary.asset
- Modify: Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs
- Test: Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs
- Test: Assets/JoseonHunter/Tests/EditMode/JangseungGeumjulAssetTests.cs

**Interfaces:**
- Produces WeaponDefinitionAsset.UiIcon : Sprite.
- Produces JangseungGeumjulVisualLibrary.GuardianDescentSprite : Sprite.
- ResolveWeaponSprite returns UiIcon first and preserves the presentation-frame fallback.

- [ ] **Step 1: Write failing asset-contract tests**

Use SerializedObject so the red test compiles before the new typed properties exist:

~~~csharp
var gakgung = LoadDefinition(WeaponId.GakgungShot);
var uiIconProperty = new SerializedObject(gakgung).FindProperty("uiIcon");
Assert.That(uiIconProperty, Is.Not.Null);
Assert.That(uiIconProperty.objectReferenceValue, Is.Not.Null);
Assert.That(uiIconProperty.objectReferenceValue, Is.Not.SameAs(gakgung.PresentationSprites[0]));
Assert.That(AssetDatabase.GetAssetPath(uiIconProperty.objectReferenceValue),
    Does.EndWith("gakgung_shot/ui-icon.png"));

var library = AssetDatabase.LoadAssetAtPath<JangseungGeumjulVisualLibrary>(
    JangseungGeumjulAssetImporter.LibraryPath);
var guardianProperty = new SerializedObject(library).FindProperty("guardianDescentSprite");
Assert.That(guardianProperty, Is.Not.Null);
Assert.That(AssetDatabase.GetAssetPath(guardianProperty.objectReferenceValue),
    Does.EndWith("jangseung_guardian_descent.png"));
~~~

- [ ] **Step 2: Run the focused tests and verify failure**

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter "JoseonHunter.Tests.EditMode.WeaponContentTests|JoseonHunter.Tests.EditMode.JangseungGeumjulAssetTests"
~~~

Expected: FAIL because UiIcon and GuardianDescentSprite do not exist.

- [ ] **Step 3: Generate the Gakgung UI sprite**

Use built-in image generation on flat #00ff00 with this prompt:

~~~text
Use case: stylized-concept
Asset type: 48x48 Unity pixel-art weapon UI icon
Primary request: a simplified Joseon gakgung horn bow, instantly readable as a C-shaped traditional bow
Style/medium: crisp animation-like pixel art with deliberately large pixel clusters
Composition/framing: one centered bow, diagonal three-quarter orientation, generous padding
Color palette: exactly four subject colors—dark ink brown outline, ochre wood, muted vermilion grip, pale tan string
Constraints: no circular aiming reticle, no arrow, no metal ornament, no white outline, no texture noise, no text, no watermark
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background with no shadow, gradient, or lighting variation
~~~

Remove chroma with the installed helper, downscale to 48x48 using nearest-neighbor sampling, and save it at the exact project path.

- [ ] **Step 4: Generate the coherent guardian sprite**

~~~text
Use case: stylized-concept
Asset type: 48x64 Unity pixel-art combat sprite
Primary request: one complete Joseon jangseung guardian spirit descending upright to crush an enemy
Style/medium: simple animation-like pixel art with a single strong wooden totem silhouette and large pixel clusters
Composition/framing: one full uncut guardian body, centered, front-facing, fully inside the canvas
Color palette: exactly five subject colors—dark ink brown outline, deep brown wood, ochre planes, muted vermilion face marks, pale tan highlight
Constraints: no separated fragments, no floating heads, no rocks, no dust, no white outline, no glow, no text, no watermark
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background with no shadow, gradient, or lighting variation
~~~

Remove chroma, downscale to 48x64 with nearest-neighbor sampling, and save it at the exact VFX path.

- [ ] **Step 5: Add serialized channels and fallback behavior**

~~~csharp
[SerializeField] private Sprite uiIcon;
public Sprite UiIcon => uiIcon;

[SerializeField] private Sprite guardianDescentSprite;
public Sprite GuardianDescentSprite => guardianDescentSprite;
~~~

Extend ConfigureForImport and JangseungGeumjulAssetImporter.Rebuild to bind the guardian sprite. Update GakgungShot.asset to reference ui-icon.png. Resolve UI sprites with this order:

~~~csharp
return definition.UiIcon != null
    ? definition.UiIcon
    : definition.PresentationSprites.Count > 0
        ? definition.PresentationSprites[0]
        : solidSprite;
~~~

- [ ] **Step 6: Import, rebuild, and rerun tests**

Run the focused EditMode command to create metadata, invoke JoseonHunter/Assets/Rebuild Jangseung Geumjul Visual Library, then rerun the two fixtures. Expected: PASS and both importers satisfy the point/uncompressed contract.

- [ ] **Step 7: Commit and push**

~~~powershell
git add -- Assets/JoseonHunter/Art/Weapons/Runtime/gakgung_shot/ui-icon.png Assets/JoseonHunter/Art/Weapons/Runtime/gakgung_shot/ui-icon.png.meta Assets/JoseonHunter/Art/VFX/JangseungGeumjul/jangseung_guardian_descent.png Assets/JoseonHunter/Art/VFX/JangseungGeumjul/jangseung_guardian_descent.png.meta Assets/JoseonHunter/Scripts/Content/Weapons/WeaponDefinitionAsset.cs Assets/JoseonHunter/Content/Weapons/GakgungShot.asset Assets/JoseonHunter/Scripts/Runtime/Gameplay/JangseungGeumjulVisualLibrary.cs Assets/JoseonHunter/Scripts/Editor/AssetProduction/JangseungGeumjulAssetImporter.cs Assets/JoseonHunter/Resources/Presentation/JangseungGeumjulVisualLibrary.asset Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs Assets/JoseonHunter/Tests/EditMode/JangseungGeumjulAssetTests.cs
git commit -m "feat: separate simplified weapon presentation assets"
git push origin master
~~~

### Task 2: Restyle the appraisal sheet as warm paper-and-ink UI

**Files:**
- Modify: Assets/JoseonHunter/Scripts/Presentation/UI/JoseonUiPalette.cs
- Modify: Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs
- Test: Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs
- Test: Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs

**Interfaces:**
- Tests observe named Image and TextMeshProUGUI children in the real presenter hierarchy; no test-only production API is added.
- Preserves reveal/count-up timing, slot state, confirmation behavior, ShowDetails, and Play.

- [ ] **Step 1: Write failing presentation tests**

~~~csharp
presenter.PreviewAtForEditor(result, WeaponAffixRevealTimeline.For(result).ReadStartsAt + .1f);
Assert.That(ImageNamed(presenter, "Reel Window 0").sprite, Is.Null);
Assert.That(ImageNamed(presenter, "Reel Window 0").color, Is.EqualTo(JoseonUiPalette.AppraisalResult));
Assert.That(ImageNamed(presenter, "Reel Window 1").color, Is.EqualTo(JoseonUiPalette.AppraisalInset));
Assert.That(TextNamed(presenter, "Rarity Seal Label").text, Is.EqualTo("일반"));
~~~

Also assert that locked labels are dark ink on a light inset and the confirmation label equals 확인.

- [ ] **Step 2: Run the appraisal fixtures and verify failure**

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter "JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests|JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests.ReadOnlyWeaponDetailsDoNotOwnGameTime"
~~~

- [ ] **Step 3: Add exact warm palette colors**

~~~csharp
public static readonly Color AppraisalResult = new(.22f, .14f, .09f, 1f);
public static readonly Color AppraisalInset = new(.82f, .74f, .57f, 1f);
public static readonly Color AppraisalBorder = new(.18f, .12f, .08f, 1f);
public static readonly Color AppraisalAccent = new(.72f, .25f, .12f, 1f);
~~~

- [ ] **Step 4: Replace ornate rows with flat nested images**

Clear reelWindows sprites, lockedSlots sprites, confirmButton sprite, and decorative stop-flash sprites. Apply AppraisalResult to row 0, AppraisalInset to rows 1–3, a two-pixel dark border, dark locked text, and a small integrated flat seal. Preserve positions and timeline.

- [ ] **Step 5: Rerun tests**

Expected: PASS; no reel_window, locked_potential_slot, or rarity-frame sprite is enabled during reading, and no text uses pure white.

- [ ] **Step 6: Commit and push**

~~~powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/JoseonUiPalette.cs Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs
git commit -m "feat: harmonize appraisal paper UI"
git push origin master
~~~

### Task 3: Enlarge experience flames and damage numbers

**Files:**
- Modify: Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs
- Modify: Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs
- Test: Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePickupRangePlayModeTests.cs
- Test: Assets/JoseonHunter/Tests/PlayMode/DamageNumberPoolPlayModeTests.cs

**Interfaces:**
- Tests create a real experience pickup by killing SpawnEnemyForTests output, matching the existing pickup-range fixture.
- Keeps StartingPickupRadius 0.58 and collection distance 0.42.
- Sets normal damage size 4.9, boss size 5.9, normal lifetime 0.62, boss bonus 0.14.

- [ ] **Step 1: Write failing size and range tests**

~~~csharp
var setup = LoadPickupAt(new Vector2(1f, 0f));
while (setup.MoveNext()) yield return setup.Current;
var pickup = GameObject.Find("Experience Flame");
Assert.That(pickup.transform.localScale.x, Is.InRange(.289f, .311f));
var before = pickup.transform.position;
controller.TickGameplayIfRunningForTests(.05f);
Assert.That(pickup.transform.position, Is.EqualTo(before));

presenter.Play(display, false, Color.white, _ => { });
var text = presenter.GetComponent<TextMeshPro>();
Assert.That(text.fontSize, Is.EqualTo(4.9f).Within(.01f));
Assert.That(text.outlineColor.r, Is.LessThan(.8f));
Assert.That(text.outlineColor.g, Is.LessThan(.8f));
Assert.That(text.outlineColor.b, Is.LessThan(.8f));
yield return new WaitForSeconds(.50f);
Assert.That(presenter.IsActive, Is.True);
yield return new WaitForSeconds(.15f);
Assert.That(presenter.IsActive, Is.False);
~~~

- [ ] **Step 2: Run tests and verify failure**

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter "JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests|JoseonHunter.Tests.PlayMode.DamageNumberPoolPlayModeTests"
~~~

- [ ] **Step 3: Implement bounded pickup pulse**

Add ExperiencePickupScale .30, ExperiencePulseAmplitude .035, and ExperiencePulseSpeed 4.5. Store a deterministic PulsePhase in PickupState. In UpdatePickups update only the experience object's scale with sine pulse; do not include scale in distance or attraction calculations.

- [ ] **Step 4: Implement readable damage text**

Use the exact sizes/lifetimes above, make normal numbers bold, and configure once in Awake:

~~~csharp
textMesh.outlineWidth = .18f;
textMesh.outlineColor = new Color32(38, 28, 22, 220);
~~~

The test rejects any outline whose RGB channels are all above 0.8.

- [ ] **Step 5: Rerun tests and confirm pool reset**

Expected: PASS; the existing 120-hit bounded-pool test returns every presenter with no persistent scale/text state.

- [ ] **Step 6: Commit and push**

~~~powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePickupRangePlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/DamageNumberPoolPlayModeTests.cs
git commit -m "feat: improve pickup and damage readability"
git push origin master
~~~

### Task 4: Replace the fragmented guardian icon with a coherent descent

**Files:**
- Create: Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungGuardianDescentPresenter.cs
- Create: Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungGuardianDescentPresenter.cs.meta
- Modify: Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs
- Test: Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatBPlayModeTests.cs
- Test: Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePresentationPlayModeTests.cs

**Interfaces:**
- Produces Play(int ownerId, Sprite sprite, Vector2 contact, int sortingOrder).
- Produces Tick(float deltaTime), Cancel(int ownerId), Clear(), Dispose().
- Tests observe the real child renderers and stable root child count; no test-only lifecycle API is added.

- [ ] **Step 1: Write failing presentation tests**

~~~csharp
presenter.Play(7, guardianSprite, Vector2.zero, 12);
Assert.That(root.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(1));
var createdChildren = root.childCount;
presenter.Tick(.60f);
Assert.That(root.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
presenter.Play(8, guardianSprite, Vector2.one, 12);
Assert.That(root.childCount, Is.EqualTo(createdChildren));
~~~

Also retain the existing single guardian-damage assertion.

- [ ] **Step 2: Run focused guardian tests and verify failure**

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter "JoseonHunter.Tests.PlayMode.WeaponPotentialCombatBPlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests"
~~~

- [ ] **Step 3: Implement the bounded presenter**

Each pooled entry owns one guardian SpriteRenderer, one shadow ring, and one dust ring. Allocate 16 ring points in the entry constructor. Use exact phases: telegraph 0.10, descent 0.28, squash 0.36, lifetime 0.58 seconds. Move from contact plus 1.4 up to contact, squash Y to 0.78, expand dust from 0.25 to 0.9, and use only FlatWardVisualPalette colors.

- [ ] **Step 4: Wire the executor**

Keep WeaponPotentialVisuals only for the hit mask. Remove construction of the world object from the potential icon. On confirmed strike call:

~~~csharp
guardianDescentPresenter.Play(
    set.Attack.InstanceId,
    context.JangseungGeumjulVisualLibrary.GuardianDescentSprite,
    new Vector2(contact.X, contact.Y),
    context.SortingOrder + 3);
~~~

Tick and dispose the presenter with the executor lifecycle.

- [ ] **Step 5: Rerun tests**

Expected: PASS; damage is unchanged, no potential description icon is used in-world, and repeated strikes reuse bounded entries.

- [ ] **Step 6: Commit and push**

~~~powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungGuardianDescentPresenter.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/JangseungGuardianDescentPresenter.cs.meta Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs Assets/JoseonHunter/Tests/PlayMode/WeaponPotentialCombatBPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/FirstPlayablePresentationPlayModeTests.cs
git commit -m "feat: add coherent guardian descent"
git push origin master
~~~

### Task 5: Replace English IMGUI with Korean Canvas results

**Files:**
- Create: Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs
- Create: Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs.meta
- Modify: Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs
- Modify: Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs
- Modify: Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs
- Modify: Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs
- Test: Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs
- Test: Assets/JoseonHunter/Tests/PlayMode/FirstPlayableUiStatePlayModeTests.cs

**Interfaces:**
- Adds optional runEnded and victory parameters and read-only RunEnded/Victory properties.
- Adds FirstPlayableController.RestartRun(), a no-op unless the run ended.
- Produces RunResultPresenter.Render(FirstPlayableUiState) and RestartRequested event.

- [ ] **Step 1: Write failing Korean-copy and modal tests**

Assert exact prefixes 체력, 경험치, 엽전, 처치, 우두머리. Render a failed result and inspect named real children: Result Title equals 전투 종료, Result Summary contains 생존 시간/처치/도달 레벨/획득 엽전, Restart Label equals 다시 시작, and Result Panel Image alpha equals 1. Also reject HP, XP, COIN, KILLS, BOSS, Run, Restart, Survived, and Try again. Do not add test-only getters to RunResultPresenter.

- [ ] **Step 2: Run UI fixtures and verify failure**

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter "JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayableUiStatePlayModeTests"
~~~

- [ ] **Step 3: Extend state and restart entry point**

Append optional constructor parameters so existing call sites compile unchanged:

~~~csharp
bool runEnded = false, bool victory = false
~~~

Set them in BuildUiState. Add RestartRun that returns unless runEnded, then calls ResetRun. Keep the silent R-key path calling RestartRun.

- [ ] **Step 4: Build the opaque Canvas result presenter**

Build once: full-screen root, dark scrim, centered opaque Hanji panel with HanjiInk border, title in the title font role, four-line summary in body role, and one 다시 시작 button. Render updates text only when the state signature changes.

- [ ] **Step 5: Integrate and remove IMGUI**

Build under modalSafeAreaContainer, subscribe RestartRequested, update in the existing 0.1-second render loop, disable rack/HUD raycasts while open, include safe-area layout, and delete FirstPlayableController.OnGUI completely.

- [ ] **Step 6: Translate exact HUD copy**

Use 레벨, 체력, 경험치, 엽전, 처치, 강한 기운이 다가옵니다, and 우두머리. Preserve only the numeric timer.

- [ ] **Step 7: Rerun fixtures and exercise restart**

Expected: PASS; clicking 다시 시작 clears RunEnded, hides the modal, and restores raycasts.

- [ ] **Step 8: Commit and push**

~~~powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs.meta Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/FirstPlayableUiStatePlayModeTests.cs
git commit -m "feat: add Korean run result UI"
git push origin master
~~~

### Task 6: Integrated validation and evidence

**Files:**
- Create: Docs/Verification/2026-08-02-korean-readability-and-simplified-feedback.md
- Modify only if capture states need it: Assets/JoseonHunter/Scripts/Editor/Scenes/PortraitStateValidationCapture.cs

**Interfaces:**
- Produces a verification record with commit IDs, commands, pass counts, capture paths, limitations, and dirty-file preservation.

- [ ] **Step 1: Scan whitespace and player-facing English**

~~~powershell
git diff --check
rg -n 'Run failed|Run complete|Restart|Survived|Try again|KILLS|COIN|A DREADFUL|BOSS  |HP |XP ' Assets/JoseonHunter/Scripts/Runtime Assets/JoseonHunter/Scripts/Presentation
~~~

Expected: no player-facing matches.

- [ ] **Step 2: Run full EditMode**

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
~~~

Expected: all EditMode tests pass.

- [ ] **Step 3: Run integrated PlayMode surface**

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter "JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests|JoseonHunter.Tests.PlayMode.DamageNumberPoolPlayModeTests|JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayableUiStatePlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests|JoseonHunter.Tests.PlayMode.WeaponPotentialCombatBPlayModeTests"
~~~

Expected: all selected tests pass.

- [ ] **Step 4: Capture and inspect portrait states**

Run PortraitStateValidationCapture without -nographics. Capture 720x1280 and 1080x2340 for appraisal, ordinary combat with experience/damage, guardian impact, and run result. Confirm no white outlines, fragmented guardian icon, ornate dark-purple appraisal rows, transparent result panel, or English labels.

- [ ] **Step 5: Run Android development build**

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1
~~~

Expected: ARM64 IL2CPP development APK succeeds without new first-party errors.

- [ ] **Step 6: Write record and review diff**

~~~powershell
git status --short
git diff --stat da7eb1d..HEAD
git diff --check da7eb1d..HEAD
~~~

Record exact counts and capture paths. Confirm the four pre-existing modified assets and .utmp remain unstaged.

- [ ] **Step 7: Commit and push verification**

~~~powershell
git add -- Docs/Verification/2026-08-02-korean-readability-and-simplified-feedback.md Assets/JoseonHunter/Scripts/Editor/Scenes/PortraitStateValidationCapture.cs
git commit -m "docs: verify Korean readability polish"
git push origin master
~~~
