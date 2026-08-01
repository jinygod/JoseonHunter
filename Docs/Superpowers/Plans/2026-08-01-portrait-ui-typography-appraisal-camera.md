# Portrait Typography, Appraisal, and Camera Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply licensed Joseon-themed fonts, readable Korean UI copy, a scalable appraisal panel with an animated 0-to-result reveal, and a 2.5× wider portrait combat view without changing HUD scale or wave rules.

**Architecture:** Keep gameplay authority and reward data unchanged. Add one role-based font catalog to the Presentation assembly, one shared affix display formatter in Runtime, deterministic appraisal timing helpers, and a portrait scale profile that separates the rendered camera from the combat spawn bounds. Existing presenters remain the owners of their UI and animation lifecycles.

**Tech Stack:** Unity 6000.5.5f1, C# 9, uGUI 2.5.0, TextMeshPro, NUnit EditMode/PlayMode tests, URP 2D, PowerShell Unity test/capture scripts.

## Global Constraints

- Work only in `D:\UnityProjects\JoseonHunter` on `master`.
- Preserve the user's uncommitted `ProjectSettings/ProjectSettings.asset`; never stage it.
- Commit and push each completed task to `origin/master`.
- Android portrait reference resolution remains 1080×1920; Screen Space HUD/modal scale must not change.
- Use only official font downloads: Chosun Gungseo, Naver Maru Buri, and Google Fonts Black And White Picture.
- Include font license/source records with the shipped font files.
- Do not change affix probabilities, affix values, potential effects, active enemy cap, wave composition, or monster variety.
- Do not perform the deferred combat optimization work.
- Runtime modal animation uses `Time.unscaledDeltaTime`; gameplay remains on scaled time.
- No production behavior is written before its failing test has been observed.

---

### Task 1: Licensed Font Assets and Deterministic TMP Generation

**Files:**
- Create: `Assets/JoseonHunter/Art/Fonts/ChosunGs.TTF`
- Create: `Assets/JoseonHunter/Art/Fonts/MaruBuri-Regular.ttf`
- Create: `Assets/JoseonHunter/Art/Fonts/MaruBuri-SemiBold.ttf`
- Create: `Assets/JoseonHunter/Art/Fonts/BlackAndWhitePicture-Regular.ttf`
- Create: `Assets/JoseonHunter/Art/Fonts/Licenses/Chosun-font-source.txt`
- Create: `Assets/JoseonHunter/Art/Fonts/Licenses/Naver-font-license.txt`
- Create: `Assets/JoseonHunter/Art/Fonts/Licenses/OFL-BlackAndWhitePicture.txt`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/RuntimeFontAssetGenerator.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/RuntimeFontAssetContractTests.cs`
- Generate: `Assets/JoseonHunter/Resources/Fonts/ChosunGs-Dynamic SDF.asset`
- Generate: `Assets/JoseonHunter/Resources/Fonts/MaruBuri-Regular-Dynamic SDF.asset`
- Generate: `Assets/JoseonHunter/Resources/Fonts/MaruBuri-SemiBold-Dynamic SDF.asset`
- Generate: `Assets/JoseonHunter/Resources/Fonts/BlackAndWhitePicture-Dynamic SDF.asset`

**Interfaces:**
- Consumes: official ZIP/font URLs and existing `NotoSansKR-Dynamic SDF` fallback.
- Produces: four `TMP_FontAsset` resources with dynamic atlases and Noto Sans KR fallback.

- [ ] **Step 1: Write the failing asset contract test**

```csharp
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class RuntimeFontAssetContractTests
    {
        [TestCase("Fonts/ChosunGs-Dynamic SDF", "ChosunGs-Dynamic SDF")]
        [TestCase("Fonts/MaruBuri-Regular-Dynamic SDF", "MaruBuri-Regular-Dynamic SDF")]
        [TestCase("Fonts/MaruBuri-SemiBold-Dynamic SDF", "MaruBuri-SemiBold-Dynamic SDF")]
        [TestCase("Fonts/BlackAndWhitePicture-Dynamic SDF", "BlackAndWhitePicture-Dynamic SDF")]
        public void LicensedRuntimeFontExistsWithDynamicAtlas(string path, string expectedName)
        {
            var font = Resources.Load<TMP_FontAsset>(path);
            Assert.That(font, Is.Not.Null, path);
            Assert.That(font.name, Is.EqualTo(expectedName));
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
            Assert.That(font.fallbackFontAssetTable, Does.Contain(
                Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR-Dynamic SDF")));
        }
    }
}
```

- [ ] **Step 2: Run the focused EditMode test and verify RED**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.RuntimeFontAssetContractTests
```

Expected: FAIL because the four new font resources do not exist.

- [ ] **Step 3: Download only the approved official font files**

Use exact sources:

```text
https://fontdown.chosun.com/100/ChosunGs.zip
https://hangeul.naver.com/hangeul_static/webfont/zips/maruburi.zip
https://raw.githubusercontent.com/google/fonts/main/ofl/blackandwhitepicture/BlackAndWhitePicture-Regular.ttf
https://raw.githubusercontent.com/google/fonts/main/ofl/blackandwhitepicture/OFL.txt
```

Extract `ChosunGs.TTF`, and from the nested `MaruBuriTTF.zip` extract only
`MaruBuri-Regular.ttf` and `MaruBuri-SemiBold.ttf`. Record the official source
and redistribution terms from `https://event.chosun.com/100/100font.html` and
`https://hangeul.naver.com/font`. Do not modify or rename the internal font
family names.

- [ ] **Step 4: Add the deterministic editor generator**

Implement `RuntimeFontAssetGenerator.GenerateAll()` as a public static editor
entry point and menu item. For each source/output pair, load `Font` with
`AssetDatabase.LoadAssetAtPath<Font>`, create a 1024×1024 SDFAA dynamic
`TMP_FontAsset`, name it exactly as the test expects, add its atlas texture and
material as sub-assets, add Noto Sans KR to `fallbackFontAssetTable`, then save
and refresh. Delete only the exact generated output path when regenerating.

```csharp
private static TMP_FontAsset Create(string sourcePath, string outputPath, string assetName,
    TMP_FontAsset fallback)
{
    var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
    if (source == null) throw new InvalidOperationException("Missing font: " + sourcePath);
    if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
        AssetDatabase.DeleteAsset(outputPath);
    var asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA,
        1024, 1024, AtlasPopulationMode.Dynamic, true);
    asset.name = assetName;
    asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
    AssetDatabase.CreateAsset(asset, outputPath);
    asset.atlasTextures[0].name = assetName + " Atlas";
    asset.material.name = assetName + " Material";
    AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
    AssetDatabase.AddObjectToAsset(asset.material, asset);
    EditorUtility.SetDirty(asset);
    return asset;
}
```

- [ ] **Step 5: Generate the assets and verify GREEN**

Run the generator from the active Unity Editor menu
`JoseonHunter/Assets/Generate Runtime Font Assets`. After import completes,
run the focused test again. Expected: all four cases PASS with no new Console
errors.

- [ ] **Step 6: Commit and push**

Stage only the new TTF, license, generator, generated TMP assets, `.meta` files,
and test. Commit:

```text
feat(ui): add licensed Joseon font assets
```

Push `master` to `origin`.

---

### Task 2: Role-Based Typography, Readable Colors, and Damage Font

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/RuntimeFontCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RuntimeUiFactory.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/JoseonUiPalette.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/DamageNumberPoolPlayModeTests.cs`

**Interfaces:**
- Consumes: generated TMP font assets from Task 1.
- Produces: `RuntimeFontRole`, `RuntimeFontCatalog.Get(RuntimeFontRole)`, and a role argument on `RuntimeUiFactory.Text`.

- [ ] **Step 1: Write failing role assignment tests**

Replace the old assertion that every UI label is Noto Sans KR with observable
role checks:

```csharp
var texts = bootstrap.GetComponentsInChildren<TextMeshProUGUI>(true);
var heading = System.Array.Find(texts, text => text.name == "Heading");
var weaponName = System.Array.Find(texts, text => text.name == "Weapon Name");
Assert.That(heading.font.name, Is.EqualTo("ChosunGs-Dynamic SDF"));
Assert.That(weaponName.font.name, Is.EqualTo("MaruBuri-SemiBold-Dynamic SDF"));
Assert.That(weaponName.color.grayscale, Is.LessThan(.45f));
```

Add to `DamageNumberPoolPlayModeTests`:

```csharp
var mesh = presenter.GetComponent<TextMeshPro>();
Assert.That(mesh.font.name, Is.EqualTo("BlackAndWhitePicture-Dynamic SDF"));
```

- [ ] **Step 2: Run the two focused PlayMode classes and verify RED**

Expected: FAIL because all UI still receives Noto Sans KR and the world-space
damage number uses the TMP default font.

- [ ] **Step 3: Implement font roles and palette tokens**

Create:

```csharp
public enum RuntimeFontRole { Body, BodyEmphasis, Title, Damage }

public static TMP_FontAsset Get(RuntimeFontRole role) => role switch
{
    RuntimeFontRole.Title => Load(ref title, "Fonts/ChosunGs-Dynamic SDF"),
    RuntimeFontRole.BodyEmphasis => Load(ref bodyEmphasis,
        "Fonts/MaruBuri-SemiBold-Dynamic SDF"),
    RuntimeFontRole.Damage => Load(ref damage,
        "Fonts/BlackAndWhitePicture-Dynamic SDF"),
    _ => Load(ref body, "Fonts/MaruBuri-Regular-Dynamic SDF")
};
```

Change `RuntimeUiFactory.Text` to accept
`RuntimeFontRole role = RuntimeFontRole.Body`; preserve all existing call sites.
Add palette colors matching the design: `HanjiInk`, `HanjiMutedInk`,
`DarkPanelText`, and `SealCrimson`.

- [ ] **Step 4: Assign semantic roles at the real consumers**

- Upgrade heading: `Title`.
- Upgrade category/behavior/delta: `Body`; name: `BodyEmphasis`.
- Appraisal title and tier seal: `Title`.
- Weapon name, result value, and confirm label: `BodyEmphasis`.
- Other appraisal copy: `Body`.
- DamageNumberPresenter `Awake`: assign `RuntimeFontCatalog.Get(Damage)`.

Use `HanjiInk` for primary text on light backgrounds and `HanjiMutedInk` for
secondary text. Do not change Canvas scale or rect sizes in this task.

- [ ] **Step 5: Run focused tests and full EditMode tests**

Expected: the two PlayMode classes PASS; full EditMode remains green; Unity
Console contains no new missing-font or missing-glyph errors.

- [ ] **Step 6: Commit and push**

Commit:

```text
feat(ui): apply semantic Joseon typography
```

Push `master`.

---

### Task 3: Shared Korean Affix Copy and Button Labels

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WeaponAffixDisplayFormatter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixValueFormatter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixPresentationTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Produces: `WeaponAffixDisplayFormatter.Describe(WeaponAffixRoll, int)` and `Describe(WeaponRuntimeModifiers)`.
- Consumers: appraisal result, weapon rack accumulated summary, reward/confirm UI.

- [ ] **Step 1: Change formatter expectations to Korean and add all stat cases**

```csharp
[TestCase(WeaponAffixStat.Damage, 24, "피해량 +24%")]
[TestCase(WeaponAffixStat.Cooldown, -8, "재사용 대기시간 -8%")]
[TestCase(WeaponAffixStat.Area, 20, "공격 범위 +20%")]
[TestCase(WeaponAffixStat.ProjectileSpeed, 15, "투사체 속도 +15%")]
[TestCase(WeaponAffixStat.Duration, 12, "지속 시간 +12%")]
public void AffixStatsUseKoreanPlayerFacingNames(
    WeaponAffixStat stat, int value, string expected)
{
    var roll = new WeaponAffixRoll(stat, WeaponAffixTier.Standard, value);
    Assert.That(WeaponAffixValueFormatter.Describe(roll, value), Is.EqualTo(expected));
}
```

Update PlayMode assertions to require `최대 추가옵션`, `확인`, and Korean
weapon-level copy.

- [ ] **Step 2: Run focused tests and verify RED**

Expected: FAIL with the current English enum names, `완벽한 추가옵션`,
`확인 · 계속`, and `LEVEL`.

- [ ] **Step 3: Implement one shared formatter**

Use an exhaustive switch:

```csharp
private static string StatName(WeaponAffixStat stat) => stat switch
{
    WeaponAffixStat.Damage => "피해량",
    WeaponAffixStat.Cooldown => "재사용 대기시간",
    WeaponAffixStat.Area => "공격 범위",
    WeaponAffixStat.ProjectileSpeed => "투사체 속도",
    WeaponAffixStat.Duration => "지속 시간",
    _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
};
```

`Describe(WeaponRuntimeModifiers)` emits the same five names and keeps cooldown
negative. Make `WeaponAffixValueFormatter` delegate to this formatter. Replace
`FirstPlayableController.GeneralAffixSummary` construction with the shared
method.

- [ ] **Step 4: Replace remaining player-facing strings**

- `완벽한 추가옵션` → `최대 추가옵션`.
- `확인  ·  계속` and `CONFIRM` → `확인`.
- `LEVEL N · 현재 무기` → `레벨 N · 현재 무기`.
- `LEVEL N · 신규 무기` → `레벨 N · 신규 무기`.
- `LEVEL N · 강화 감정` → `레벨 N · 강화 감정`.

Search all first-party C# for the five English affix labels and `CONFIRM` before
finishing this task.

- [ ] **Step 5: Run focused tests, then full EditMode**

Expected: all Korean formatting and modal copy tests PASS; no enum text leaks.

- [ ] **Step 6: Commit and push**

Commit:

```text
fix(ui): localize weapon affix copy
```

Push `master`.

---

### Task 4: Resolution-Independent Appraisal Panel and Tier Seal

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`

**Interfaces:**
- Consumes: `RuntimeFontRole.Title`, palette tokens, existing appraisal reel row sprites.
- Produces: a scalable procedural Hanji shell and `Affix Tier Seal` text badge.

- [ ] **Step 1: Write failing composition tests**

```csharp
var shell = ImageNamed(presenter, "Appraisal Hanji Surface");
Assert.That(shell.sprite, Is.Null);
Assert.That(shell.color.a, Is.EqualTo(1f));
Assert.That(ImageNamed(presenter, "Final Symbol 0").enabled, Is.False);
Assert.That(RectNamed(presenter, "Affix Tier Seal").gameObject.activeSelf, Is.True);
Assert.That(TextValue(RectNamed(presenter, "Affix Tier Seal Label")), Is.EqualTo("최대"));
```

Also assert the top, bottom, left, and right frame strips are present and fully
inside `Weapon Appraisal Panel` at all validation resolutions.

- [ ] **Step 2: Run focused PlayMode tests and verify RED**

Expected: FAIL because the shell is a cropped low-resolution sprite and the
left icon well still shows `ReelSymbolRarity`.

- [ ] **Step 3: Rebuild the shell without a stretched background sprite**

Rename the shell object to `Appraisal Hanji Surface`, leave `sprite = null`,
and use an opaque warm Hanji color. Add solid uGUI frame strips under content:

```text
Appraisal Outer Shadow
Appraisal Top Rail
Appraisal Bottom Rail
Appraisal Left Rail
Appraisal Right Rail
Appraisal Inner Border
```

Use anchors and fixed rail thickness; do not scale the old 405×210 scroll
content across the panel. Keep existing small reel row sprites at their current
row sizes. Remove cropped-sprite allocation and cleanup paths.

- [ ] **Step 4: Replace the coin-like symbol with a tier seal**

Create a crimson square `Image` in reel 0's icon well and a centered Title-role
label. Set text from a pure mapping: Standard=`일반`, High=`고급`,
Perfect=`최대`. Hide `Final Symbol 0` and `rarityFrame` for all general affix
results. Potential symbols in reels 1–3 remain unchanged.

- [ ] **Step 5: Run focused and safe-area PlayMode tests**

Expected: panel composition and every validation resolution PASS; no white
rectangle or detached decoration appears.

- [ ] **Step 6: Commit and push**

Commit:

```text
fix(ui): rebuild appraisal panel and tier seal
```

Push `master`.

---

### Task 5: Post-Stop 0-to-Result Count-Up

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalPresentation.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixRevealTimelineTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixPresentationTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Produces: `CountStartsAt`, `CountEndsAt`, and deterministic count pulse scale.
- Consumer: `WeaponAffixRevealPresenter.UpdateVisualState(float)`.

- [ ] **Step 1: Write failing timing and value tests**

```csharp
var timeline = WeaponAffixRevealTimeline.For(Result(WeaponAffixTier.Perfect, 0));
Assert.That(timeline.CountStartsAt, Is.EqualTo(timeline.AffixStopsAt));
Assert.That(timeline.CountEndsAt - timeline.CountStartsAt, Is.EqualTo(.90f).Within(.01f));
Assert.That(timeline.ReadStartsAt, Is.GreaterThanOrEqualTo(timeline.CountEndsAt));

var values = new List<int>();
for (var step = 0; step <= 100; step++)
    values.Add(WeaponAppraisalPresentation.DisplayValueAt(20d, step / 100f));
Assert.That(values[0], Is.Zero);
Assert.That(values[^1], Is.EqualTo(20));
Assert.That(values, Is.Ordered);
Assert.That(values, Does.Contain(1).And.Contain(19));
```

Add a PlayMode preview assertion that the value is `+0%` at `CountStartsAt`,
between 0 and target halfway through, and final at `CountEndsAt`.

- [ ] **Step 2: Run focused EditMode/PlayMode tests and verify RED**

Expected: FAIL because the count currently runs from opening to affix stop and
the timeline exposes no count window.

- [ ] **Step 3: Add count timing to the timeline**

Add constructor fields and public properties. Use a 0.75-second count for
Standard, 0.90 seconds for High/Perfect, and move potential stop/read times
after `CountEndsAt`. Preserve sequential potential stops at 0.18-second gaps.
Update total durations so confirmation never appears before the count and
potential reveals complete.

- [ ] **Step 4: Drive value and punch from the count window**

Use:

```csharp
var countProgress = Mathf.InverseLerp(timeline.CountStartsAt,
    timeline.CountEndsAt, time);
var displayed = WeaponAppraisalPresentation.DisplayValueAt(
    activeResult.General.Value, countProgress);
detail.text = WeaponAffixValueFormatter.Describe(activeResult.General, displayed);
detail.rectTransform.localScale = Vector3.one *
    WeaponAppraisalPresentation.CountPulseScaleAt(
        activeResult.General.Value, countProgress);
```

`CountPulseScaleAt` adds a small sinusoidal step pulse and one larger pulse in
the final 10% of progress, then returns exactly 1 at completion. Show the tier
seal and enable `확인` only after the final value has settled.

- [ ] **Step 5: Preserve compressed skip behavior**

`SkipFinishAt` must still spend at least 0.18 seconds moving through the final
count/punch state. Ensure repeated click/confirm produces one completion event.

- [ ] **Step 6: Run focused tests and full EditMode/PlayMode suites**

Expected: count tests, reveal tests, and full suites PASS; Console has no new
exceptions.

- [ ] **Step 7: Commit and push**

Commit:

```text
feat(ui): add staged affix count-up reveal
```

Push `master`.

---

### Task 6: 2.5× Portrait Camera Zoom-Out with Decoupled Spawn Bounds

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatVisualScaleProfile.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatVisualScaleProfileTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/StagePacingPlayModeTests.cs`

**Interfaces:**
- Produces: portrait `CameraOrthographicSize = 18f`, `SpawnOrthographicSize = 8.5f`, and `SpawnBounds(Vector2,float)`.
- Consumer: `FirstPlayableController.SpawnEnemy`.

- [ ] **Step 1: Write failing profile tests**

```csharp
var profile = CombatVisualScaleProfile.MobilePortrait;
Assert.That(profile.CameraOrthographicSize, Is.EqualTo(18f));
Assert.That(7.25f / profile.CameraOrthographicSize, Is.InRange(.33f, .50f));
Assert.That(profile.SpawnOrthographicSize, Is.EqualTo(8.5f));
var bounds = profile.SpawnBounds(new Vector2(2f, 3f), 9f / 16f);
Assert.That(bounds, Is.EqualTo(Rect.MinMaxRect(
    -2.78125f, -5.5f, 6.78125f, 11.5f)));
```

Change the PlayMode spawn contract: the spawn root must be outside the
independent engagement bounds but inside the larger rendered camera viewport.

- [ ] **Step 2: Run focused EditMode and PlayMode tests and verify RED**

Expected: FAIL because camera size is 7.25 and spawning uses the rendered
viewport perimeter.

- [ ] **Step 3: Add the independent spawn profile**

Extend the profile constructor and add:

```csharp
public Rect SpawnBounds(Vector2 center, float aspect)
{
    var halfHeight = SpawnOrthographicSize;
    var halfWidth = halfHeight * Mathf.Max(.01f, aspect);
    return Rect.MinMaxRect(center.x - halfWidth, center.y - halfHeight,
        center.x + halfWidth, center.y + halfHeight);
}
```

Set portrait camera to `18f` and spawn size to `8.5f`. Keep actor scale,
contact radii, movement speed, active cap, and wave timeline unchanged.

- [ ] **Step 4: Route spawning through engagement bounds**

Replace `CurrentViewportBounds()` with `CurrentSpawnBounds()` using the player
position and `VisualScale.SpawnBounds(center, gameplayCamera.aspect)`. Continue
to use `ViewportSpawnGeometry.PointOnExpandedPerimeter` with the existing
0.75–1.5 margins. Do not require renderers to remain outside the rendered
viewport because the design intentionally allows peripheral on-screen entry.

- [ ] **Step 5: Run camera/spawn tests and stage pacing tests**

Expected: camera is 18, spawns remain outside engagement bounds, no spawn is in
the central combat region, boss milestones and wave counts remain unchanged.

- [ ] **Step 6: Commit and push**

Commit:

```text
fix(combat): widen portrait camera view
```

Push `master`.

---

### Task 7: Visual Capture, Regression Verification, and Handoff

**Files:**
- Modify if required by changed object names only: `Assets/JoseonHunter/Scripts/Editor/Scenes/PortraitStateValidationCapture.cs`
- Generate ignored evidence: `Artifacts/PortraitValidation/**`
- Modify: `Docs/AI/UnityProjectContext.md`

**Interfaces:**
- Consumes: all completed tasks.
- Produces: final automated results, portrait screenshots, and current project handoff notes.

- [ ] **Step 1: Run full automated validation**

Run full EditMode and PlayMode suites with `Tools/Unity/Test-Unity.ps1`.
Expected: all tests PASS. Read `Logs/editmode.log`, `Logs/playmode.log`, and
Unity Console; record exact counts and any pre-existing warnings.

- [ ] **Step 2: Capture every supported portrait state**

Run `PortraitStateValidationCapture.CaptureInBatchMode` without `-nographics`.
Capture gameplay, upgrade selection, appraisal mid-count, appraisal final, and
resumed combat at 720×1280, 1080×1920, 1080×2340, 1170×2532, and 1440×3200.

- [ ] **Step 3: Inspect the captures against the user report**

Verify visually:

- primary and secondary text are readable on Hanji;
- upgrade and appraisal copy is Korean;
- title/body/damage font roles are visibly distinct;
- no blue/gold coin-like affix emblem remains;
- appraisal background has no enlarged pixel blocks or white rectangles;
- maximum result reads `최대 추가옵션` and button reads `확인`;
- count-up visibly passes through intermediate values;
- world actors are roughly 40% of their previous screen height;
- HUD and modal sizes did not shrink;
- the beginning of combat is not empty because of distant spawning.

- [ ] **Step 4: Update project context with exact evidence**

Add a dated handoff section recording font sources, font roles, camera 18.0,
spawn size 8.5, test counts, capture paths, and any limitation. Do not claim
device performance improvement; optimization was out of scope.

- [ ] **Step 5: Review the final diff**

Confirm `git diff --check`, no unrelated files, no generated `Library/`,
`Logs/`, or `Artifacts/` staged, and `ProjectSettings/ProjectSettings.asset`
remains unstaged.

- [ ] **Step 6: Commit and push the verification handoff**

Commit:

```text
docs: verify portrait typography and camera pass
```

Push `master` and report the exact commit hashes and validation results.
