# Joseon Hunter Portrait Mobile Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce an Android portrait release candidate with approved original
pixel art, a complete three-minute patrol and boss fight, geumjul sealing,
equipment and investigation progression, local recovery, and Google Play
submission material.

**Architecture:** Preserve the existing one-way Domain, Content, Runtime,
Presentation, Infrastructure, and Editor assembly boundaries. Complete and
approve the entire production-asset library before gameplay implementation;
after that gate, build deterministic pure-C# rules first, connect them to
pooled Unity runtime objects, and finish with presentation, persistence, and
Android release validation.

**Tech Stack:** Unity 6000.5.5f1, C# 9-compatible Unity runtime, URP 2D,
Input System, uGUI, TextMeshPro, NUnit/Unity Test Framework, official Unity
MCP, PowerShell, Android IL2CPP ARM64, Git/GitHub.

## Global Constraints

- Project root is exactly `D:\UnityProjects\JoseonHunter`.
- Design authority is
  `Docs/Superpowers/Specs/2026-07-26-portrait-mobile-vertical-slice-design.md`.
- Portrait-only Android is the first player platform.
- Reference resolution is 360 x 640; support safe areas from 19.5:9 through
  4:3.
- A boss appears at 3:00 and the complete run ends by 4:00.
- Gameplay is offline with no account, cloud save, ads, in-app purchases,
  energy, required network, telemetry SDK, or unused service interface.
- Consumption currency is limited to coins (`엽전`).
- Start with Unity save schema version 1 and do not import Flutter save data.
- Do not use SPUM code, packages, assets, logos, layouts, or source structure.
- Every AI-generated asset must be original and have provenance recorded.
- Character cells are 64 x 64 px, approximately 48 px visible height, foot
  anchor `(32, 56)`, normalized pivot `(0.5, 0.125)`, and 32 PPU.
- Player sheets contain Idle 4 x 3, Move 6 x 3, and Death 8 x 1: exactly 38
  frames; no attack or hit sheets.
- Combat terrain contains no buildings, walls, fences, trees, or colliders.
- Do not begin Task 5 or any later gameplay task until Task 4 records explicit
  user approval for every asset batch and all asset validation tests pass.
- Do not hand-edit Unity `.unity`, `.prefab`, `.asset`, `.controller`, `.anim`,
  or `.meta` YAML. Use Editor APIs or the official Unity MCP.
- Use test-first red/green cycles and a focused commit at the end of every
  task.
- Never stage or overwrite unrelated working-tree changes.
- Android release settings are minimum API 26, target API 36, IL2CPP, ARM64,
  AAB, Play App Signing, and 16 KB native-page compatibility.

---

## Planned File Structure

```text
ArtSource/
  Pixel/
    Palettes/
    Characters/<id>/{manifest.json,palette.png,layers/,preview.png}
    Enemies/<id>/{manifest.json,palette.png,layers/,preview.png}
    Weapons/
    VFX/
    Stage/
    UI/
  Audio/
  Store/
Assets/JoseonHunter/
  Art/{Characters/Runtime,Enemies,Bosses,Weapons,VFX,Stages,UI,Store}
  Audio/{Music,SFX,UI}
  Data/{Heroes,Enemies,Weapons,Equipment,Progression,Waves}
  Prefabs/{App,Gameplay,Actors,Weapons,VFX,UI}
  Scripts/
    Domain/{Combat,Geumjul,Progression,Runs,Save}
    Content/{Definitions,Validation}
    Runtime/{App,Actors,Combat,Geumjul,Pooling,Runs}
    Presentation/{Gameplay,Lobby,Tutorial}
    Infrastructure/{Save,Diagnostics}
    Editor/{AssetProduction,Build,Content,Prefabs,Scenes}
  Tests/{EditMode,PlayMode}
Docs/
  Assets/{production-asset-manifest.json,asset-approval.md,rights-ledgers}
  Release/
Tools/
  Assets/Test-ProductionAssets.ps1
  Unity/{Test-Unity.ps1,Test-PlayMode.ps1,Build-Android.ps1}
```

Assembly ownership remains:

- `JoseonHunter.Domain`: deterministic rules and immutable values; no
  `UnityEngine` reference.
- `JoseonHunter.Content`: ScriptableObject definitions and validation;
  references Domain.
- `JoseonHunter.Runtime`: scene orchestration, actors, pools, and input;
  references Domain and Content.
- `JoseonHunter.Presentation`: UI, sprites, audio, VFX, haptics, tutorials;
  references Domain, Content, and Runtime.
- `JoseonHunter.Infrastructure`: local save and diagnostics; references Domain.
- `JoseonHunter.Editor`: asset, prefab, scene, content, and build generators.

---

### Task 1: Lock the Portrait Release and Production-Asset Contract

**Files:**
- Create: `Docs/Assets/production-asset-manifest.json`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/ProductionAssetManifest.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/ProductionAssetValidator.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/ProductionAssetContractTests.cs`
- Modify through Unity:
  `ProjectSettings/ProjectSettings.asset`

**Interfaces:**
- Produces:
  `ProductionAssetValidator.Validate(string manifestPath) : IReadOnlyList<string>`.
- Produces manifest batches `characters`, `enemies`, `weapons_vfx`, `stage`,
  `ui`, `audio`, and `store`.
- Produces portrait/API settings consumed by the Android build task.

- [ ] **Step 1: Write the failing project and manifest tests**

Create tests with these exact assertions:

```csharp
[Test]
public void AndroidReleaseContractIsPortraitApi36Arm64()
{
    Assert.That(PlayerSettings.defaultInterfaceOrientation,
        Is.EqualTo(UIOrientation.Portrait));
    Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.False);
    Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.False);
    Assert.That(PlayerSettings.Android.minSdkVersion,
        Is.EqualTo(AndroidSdkVersions.AndroidApiLevel26));
    Assert.That(PlayerSettings.Android.targetSdkVersion,
        Is.EqualTo(AndroidSdkVersions.AndroidApiLevel36));
    Assert.That(PlayerSettings.Android.targetArchitectures,
        Is.EqualTo(AndroidArchitecture.ARM64));
    Assert.That(PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android),
        Is.EqualTo(ScriptingImplementation.IL2CPP));
}

[Test]
public void ManifestDeclaresEveryRequiredApprovalBatch()
{
    var errors = ProductionAssetValidator.Validate(
        "Docs/Assets/production-asset-manifest.json");
    Assert.That(errors, Is.Empty);
}
```

The validator test fixture must also prove these failures:

```text
duplicate asset id
unknown batch
missing source path
missing runtime path
license other than approved
approval status other than pending or approved
missing dimensions, frame count, pivot, PPU, SHA-256, or prompt revision
runtime path outside Assets/JoseonHunter
source path outside ArtSource
```

- [ ] **Step 2: Run the focused tests and confirm red**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.ProductionAssetContractTests
```

Expected: compile failure because the production manifest types do not exist,
or assertion failure because orientation is not portrait.

- [ ] **Step 3: Implement the manifest model and validator**

Use serializable fields:

```csharp
[Serializable]
public sealed class ProductionAssetManifest
{
    public int schemaVersion;
    public ProductionAssetEntry[] assets;
}

[Serializable]
public sealed class ProductionAssetEntry
{
    public string id;
    public string batch;
    public string kind;
    public string sourcePath;
    public string runtimePath;
    public int width;
    public int height;
    public int frameCount;
    public float pivotX;
    public float pivotY;
    public int pixelsPerUnit;
    public string sha256;
    public string licenseStatus;
    public string approvalStatus;
    public string promptRevision;
}
```

`Validate` returns sorted messages and never changes files. Valid batches are:

```csharp
private static readonly HashSet<string> ValidBatches = new()
{
    "characters", "enemies", "weapons_vfx", "stage", "ui", "audio", "store"
};
```

For audio, `width`, `height`, `frameCount`, `pivotX`, `pivotY`, and
`pixelsPerUnit` are zero; all other entries require positive dimensions.

- [ ] **Step 4: Create the complete pending manifest**

Declare every deliverable from sections 16.1 through 16.7 of the design spec.
Use stable IDs such as:

```json
{
  "id": "hero_rookie_constable_runtime",
  "batch": "characters",
  "kind": "sprite_sheet",
  "sourcePath": "ArtSource/Pixel/Characters/rookie-constable/preview.png",
  "runtimePath": "Assets/JoseonHunter/Art/Characters/Runtime/rookie_constable.png",
  "width": 384,
  "height": 448,
  "frameCount": 38,
  "pivotX": 0.5,
  "pivotY": 0.125,
  "pixelsPerUnit": 32,
  "sha256": "",
  "licenseStatus": "approved",
  "approvalStatus": "pending",
  "promptRevision": "character-v1"
}
```

The validator accepts an empty hash only while `approvalStatus` is `pending`;
an approved entry requires a 64-character lowercase SHA-256.

- [ ] **Step 5: Apply release settings through Unity**

Through the official Unity MCP or an Editor menu command, set:

```text
Default Orientation: Portrait
Allowed Auto Rotation: Portrait only
Reference UI resolution: 360 x 640
Minimum API: 26
Target API: 36
Scripting Backend: IL2CPP
Architecture: ARM64 only
Application Identifier: com.jinygod.joseonhunter
Version: 0.1.0
Bundle Version Code: 1
```

Do not edit serialized project YAML directly.

- [ ] **Step 6: Re-run tests and commit**

Expected: focused tests pass. Pending approval entries are valid, but the
separate gate in Task 4 still fails until every entry is approved.

```powershell
git add Docs/Assets/production-asset-manifest.json `
  Assets/JoseonHunter/Scripts/Editor/AssetProduction `
  Assets/JoseonHunter/Tests/EditMode/ProductionAssetContractTests.cs `
  ProjectSettings/ProjectSettings.asset
git commit -m "chore: lock portrait asset production contract"
```

---

### Task 2: Create the Modular Pixel Production Kit

**Files:**
- Create: `ArtSource/Pixel/Palettes/joseon-hunter-master.png`
- Create: `ArtSource/Pixel/Characters/mannequin/manifest.json`
- Create: `ArtSource/Pixel/Characters/mannequin/palette.png`
- Create: `ArtSource/Pixel/Characters/mannequin/layers/*.png`
- Create: `ArtSource/Pixel/Characters/mannequin/preview.png`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/CharacterSheetContract.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/CharacterSheetContractTests.cs`
- Create: `Tools/Assets/Test-ProductionAssets.ps1`
- Modify: `Docs/Assets/asset-rights-ledger.csv`

**Interfaces:**
- Produces:
  `CharacterSheetContract.Validate(string sourceRoot, string runtimePath) :
  CharacterSheetValidationResult`.
- Produces the canonical mannequin used by the three heroes and six enemies.
- Produces a PowerShell preflight that rejects malformed PNG or manifest data
  before Unity import.

- [ ] **Step 1: Write failing mannequin contract tests**

Assert:

```csharp
var result = CharacterSheetContract.Validate(
    "ArtSource/Pixel/Characters/mannequin",
    "Assets/JoseonHunter/Art/Characters/Runtime/mannequin.png");
Assert.That(result.Errors, Is.Empty);
Assert.That(result.CellSize, Is.EqualTo(new Vector2Int(64, 64)));
Assert.That(result.FootAnchor, Is.EqualTo(new Vector2Int(32, 56)));
Assert.That(result.Pivot, Is.EqualTo(new Vector2(0.5f, 0.125f)));
Assert.That(result.FrameCount, Is.EqualTo(38));
```

Add negative fixtures for wrong canvas size, semi-transparent stray pixels,
more than the declared palette, missing layer, mismatched layer dimensions,
and a non-transparent background.

- [ ] **Step 2: Run the tests and confirm red**

Use `Tools/Unity/Test-Unity.ps1` filtered to
`CharacterSheetContractTests`. Expected: compile failure.

- [ ] **Step 3: Implement the source manifest contract**

Use this JSON shape:

```json
{
  "id": "mannequin",
  "cellSize": [64, 64],
  "footAnchor": [32, 56],
  "pivot": [0.5, 0.125],
  "pixelsPerUnit": 32,
  "directions": ["down", "right", "up"],
  "mirrorLeftFrom": "right",
  "animations": [
    {"name": "idle", "start": 0, "frames": 12, "fps": 6},
    {"name": "move", "start": 12, "frames": 18, "fps": 10},
    {"name": "death", "start": 30, "frames": 8, "fps": 10}
  ],
  "layers": [
    "shadow", "back-equipment", "body", "back-hair", "lower-clothing",
    "upper-clothing", "armor", "face", "front-hair", "headwear",
    "left-weapon", "right-prop", "front-overlay"
  ],
  "paletteSlots": [
    "skin", "primary-cloth", "secondary-cloth", "accent", "metal", "outline"
  ],
  "promptRevision": "mannequin-v1"
}
```

The flattened sheet layout is six 64 px columns by seven 64 px rows. Frames
0-37 are read row-major; unused cells 38-41 remain fully transparent.

Return:

```csharp
public sealed record CharacterSheetValidationResult(
    IReadOnlyList<string> Errors,
    Vector2Int CellSize,
    Vector2Int FootAnchor,
    Vector2 Pivot,
    int FrameCount);
```

- [ ] **Step 4: Produce and inspect the mannequin kit**

Generate original pixel art, then pixel-correct it so every layer:

```text
uses the shared 384 x 448 sheet
contains no anti-aliased edge pixels
places both feet around y=56 within each cell
uses only approved master-palette ramps
preserves the same silhouette across matching frames
contains no attack or hit frames
```

Create `preview.png` as a 4x nearest-neighbor composite showing down idle,
right move, up idle, and death final frame on both light and dark checks.

- [ ] **Step 5: Implement the PowerShell preflight**

`Test-ProductionAssets.ps1` accepts `-ManifestPath` and optional `-Batch`,
parses the production manifest, confirms every selected file exists, computes
SHA-256, and invokes Unity validation. Omitting `-Batch` always validates the
complete manifest:

```powershell
param(
  [string]$ManifestPath =
    'D:\UnityProjects\JoseonHunter\Docs\Assets\production-asset-manifest.json',
  [ValidateSet(
    'characters', 'enemies', 'weapons_vfx', 'stage', 'ui', 'audio', 'store')]
  [string]$Batch
)

$root = 'D:\UnityProjects\JoseonHunter'
$manifest = Get-Content -Raw -LiteralPath $ManifestPath | ConvertFrom-Json
$errors = [Collections.Generic.List[string]]::new()
$selected = if ($Batch) {
  @($manifest.assets | Where-Object batch -eq $Batch)
} else {
  @($manifest.assets)
}
foreach ($asset in $selected) {
  $source = Join-Path $root $asset.sourcePath
  if (-not (Test-Path -LiteralPath $source)) {
    $errors.Add("missing source: $($asset.id)")
  }
}
if ($errors.Count -gt 0) {
  $errors | Write-Error
  exit 1
}
& "$root\Tools\Unity\Test-Unity.ps1" `
  -Filter JoseonHunter.Tests.EditMode.ProductionAsset
exit $LASTEXITCODE
```

- [ ] **Step 6: Record provenance, verify, and commit**

The ledger row includes asset ID, prompt revision, generation date, generator,
human edits, source path, runtime path, and confirmation `original-no-SPUM`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.CharacterSheetContractTests
git add ArtSource/Pixel/Palettes ArtSource/Pixel/Characters/mannequin `
  Assets/JoseonHunter/Scripts/Editor/AssetProduction `
  Assets/JoseonHunter/Tests/EditMode/CharacterSheetContractTests.cs `
  Tools/Assets Docs/Assets/asset-rights-ledger.csv
git commit -m "feat: add modular pixel production kit"
```

---

### Task 3: Produce Every Release Asset Batch

**Files:**
- Create: `ArtSource/Pixel/Characters/{rookie-constable,shaman,mountain-hunter}/**`
- Create: `ArtSource/Pixel/Enemies/{plague-rat-spirit,paper-ghost,straw-effigy,lantern-spirit,dokkaebi,fallen-general}/**`
- Create: `ArtSource/Pixel/{Weapons,VFX,Stage,UI}/**`
- Create: `ArtSource/Audio/**`
- Create: `ArtSource/Store/**`
- Create: `Assets/JoseonHunter/Art/{Characters/Runtime,Enemies,Bosses,Weapons,VFX,Stages,UI,Store}/**`
- Create: `Assets/JoseonHunter/Audio/{Music,SFX,UI}/**`
- Create: `Docs/Assets/review/{characters,enemies,weapons-vfx,stage,ui,audio,store}.md`
- Modify: `Docs/Assets/production-asset-manifest.json`
- Modify: `Docs/Assets/asset-rights-ledger.csv`
- Modify: `Docs/Assets/audio-rights-ledger.csv`

**Interfaces:**
- Consumes the mannequin, master palette, and manifest validator.
- Produces seven complete review batches and all runtime-ready files.
- Produces no gameplay prefabs or scenes.

- [ ] **Step 1: Produce and review the characters batch**

Create three heroes with 38 frames each, three portraits, three locked
silhouettes, and four constable palette variants. Required readable traits:

```text
rookie constable: navy patrol uniform, black gat, hopae, hwando silhouette
shaman: cream and vermilion ritual robe, jade accent, talisman bundle
mountain hunter: muted green hunting garb, fur accent, horn bow silhouette
```

Each `characters.md` review sheet embeds:

```text
4x preview
idle/move/death contact sheet
light/dark background check
palette swatches
64x64 bounds, anchor, pivot, and 38-frame validator output
SPUM non-reproduction declaration
```

Present the batch to the user. Record only `approved` or exact requested
revisions; do not infer approval from silence.

- [ ] **Step 2: Produce and review the enemies batch**

Create the five normal enemies and Fallen General. Each sheet contains idle,
move, and death; Lantern Spirit also receives projectile/impact VFX, Dokkaebi
receives dash telegraph VFX, and the boss receives charge, cone, summon, and
enrage presentation. Use palette/size variants for elites without counting
them as new species.

The boss silhouette must read at the reference 360 x 640 resolution and must
not use European plate armor, samurai armor, or Chinese imperial motifs.
Present `enemies.md` and obtain explicit user approval.

- [ ] **Step 3: Produce and review weapons and VFX**

Create:

```text
Hwando: blade trail, contact, two-hit alternation, five-direction evolution
Talisman: flight, attach, chain, explosion, slow field, evolved formation
Horn Bow: arrow, pierce, contact, secondary arrows, evolved arrow rain
Geumjul: draw, age/fade, closure, burst, Fire Mark, Ice Bind, Five-Color Barrier
Shared: death, XP pickup, level-up, boss warning/charge/cone/summon
Accessibility: reduced-flash variants for every full-screen or rapid effect
```

Contact shapes and damage timing are data-driven; art contains no authoritative
hitbox. Present `weapons-vfx.md` and obtain explicit approval.

- [ ] **Step 4: Produce and review the flat stage**

Create exactly:

```text
16 grass/earth base tiles at 32 x 32 px
12 path/transition tiles
24 non-colliding decals
3 boss-area ground marks
1 edge-fog set
```

Assemble a 360 x 640 and 720 x 1280 nearest-neighbor mock gameplay field.
Confirm there are no buildings, walls, fences, trees, or navigation blockers.
Present `stage.md` and obtain explicit approval.

- [ ] **Step 5: Produce and review the UI batch**

Create the combat HUD, joystick, three-choice level modal, boss UI, pause and
settings, four tutorial prompts, results, five-tab navigation, and Shop,
Equipment, Patrol, Investigation, and Evolution screens. Also create equipment
slot, four quality, coin, clue, and stat icons.

Review at:

```text
360 x 640 with representative Android safe area
390 x 844 phone
800 x 1280 4:3 tablet
normal and reduced-flash modes
Korean labels at 100%, 115%, and 130% UI scale
```

Present `ui.md` and obtain explicit approval.

- [ ] **Step 6: Produce and review audio**

Create three seamless music tracks (lobby, combat, boss), ten combat SFX, five
UI SFX, and victory, defeat, evolution, and boss-arrival cues. Normalize music
to a consistent integrated loudness and leave combat effects with headroom.
Record duration, channels, sample rate, loop points, generator/source, license,
and SHA-256 in `audio-rights-ledger.csv`.

Create `audio.md` with a filename-to-event table and loudness notes. Obtain
explicit approval for the batch.

- [ ] **Step 7: Produce and review store assets**

Create:

```text
512 x 512 app icon
1024 x 500 feature graphic
6 portrait screenshots
splash image
Korean logo
English logo
credits screen
privacy screen
```

Screenshots must use only approved in-game art and may not show unimplemented
features, ratings, prices, or reward claims. Present `store.md` and obtain
explicit approval.

- [ ] **Step 8: Import, hash, validate, and commit each approved batch**

After each approval, set its entries to `approved`, compute lowercase SHA-256,
and import through Unity. Actor sprites use Point, no mipmaps, 32 PPU, RGBA32;
large UI may use Bilinear; music streams; short SFX decompress on load.

Run after every batch:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-ProductionAssets.ps1 -Batch characters
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-ProductionAssets.ps1 -Batch enemies
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-ProductionAssets.ps1 -Batch weapons_vfx
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-ProductionAssets.ps1 -Batch stage
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-ProductionAssets.ps1 -Batch ui
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-ProductionAssets.ps1 -Batch audio
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-ProductionAssets.ps1 -Batch store
```

Run the matching command immediately after its batch. Task 4 is the only
pre-gameplay step that runs the script without `-Batch`.

Use focused commits:

```text
art: approve launch character assets
art: approve launch enemy assets
art: approve weapon and effect assets
art: approve flat stage assets
art: approve portrait interface assets
audio: approve launch sound library
art: approve store presentation assets
```

---

### Task 4: Close the Asset Approval Gate

**Files:**
- Create: `Assets/JoseonHunter/Tests/EditMode/AssetApprovalGateTests.cs`
- Create: `Docs/Assets/asset-approval.md`
- Modify: `Docs/Assets/production-asset-manifest.json`

**Interfaces:**
- Produces:
  `ProductionAssetValidator.ValidateApprovalGate(string manifestPath) :
  IReadOnlyList<string>`.
- Produces the sole authorization record that allows Task 5 to begin.

- [ ] **Step 1: Write the failing approval-gate test**

```csharp
[Test]
public void EveryProductionAssetIsApprovedAndImported()
{
    var errors = ProductionAssetValidator.ValidateApprovalGate(
        "Docs/Assets/production-asset-manifest.json");
    Assert.That(errors, Is.Empty);
}
```

The method must reject pending entries, missing hashes, missing runtime files,
wrong frame counts, wrong pivots, wrong PPU, invalid importer settings, duplicate
IDs, missing ledger rows, and any of the seven missing batch approvals.

- [ ] **Step 2: Run the gate test and confirm red**

Expected: FAIL until every batch in Task 3 has been explicitly approved.

- [ ] **Step 3: Implement strict gate validation**

Require:

```csharp
entry.approvalStatus == "approved"
entry.licenseStatus == "approved"
entry.sha256.Length == 64
File.Exists(entry.sourcePath)
AssetDatabase.LoadMainAssetAtPath(entry.runtimePath) != null
```

For character entries, also call `CharacterSheetContract.Validate`.
No command-line switch, environment variable, test fixture, or fallback may
bypass the production gate.

- [ ] **Step 4: Record the approval receipt**

`asset-approval.md` lists:

```text
manifest commit
approval date
seven batch review-document paths
approved asset count per batch
validator test result path
user approval statement
known non-blocking notes, or "none"
```

The user must approve this consolidated receipt after reviewing the seven
batches. If changes are requested, return to the relevant Task 3 step.

- [ ] **Step 5: Run the complete asset suite and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-ProductionAssets.ps1
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.Asset
git add Assets/JoseonHunter/Tests/EditMode/AssetApprovalGateTests.cs `
  Assets/JoseonHunter/Scripts/Editor/AssetProduction `
  Docs/Assets
git commit -m "docs: close production asset approval gate"
```

Expected: all asset tests pass. Only after this commit may Task 5 start.

---

### Task 5: Implement Deterministic Combat, XP, and Run Rules

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/CombatTypes.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Combat/DamageResolver.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/RunClock.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/ExperienceCurve.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/ProgressionTypes.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/CombatRuleTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/RunRuleTests.cs`

**Interfaces:**
- Produces:
  `DamageResolver.Resolve(in DamageRequest) : DamageResult`.
- Produces:
  `RunClock.Advance(float deltaSeconds) : RunPhase`.
- Produces:
  `UpgradeSelector.Select(UpgradeState state, int seed) :
  IReadOnlyList<UpgradeOffer>`.

- [ ] **Step 1: Write failing combat and run tests**

Cover:

```csharp
[TestCase(0f, RunPhase.WaveOne)]
[TestCase(45f, RunPhase.WaveTwo)]
[TestCase(90f, RunPhase.WaveThree)]
[TestCase(135f, RunPhase.Peak)]
[TestCase(165f, RunPhase.BossWarning)]
[TestCase(180f, RunPhase.Boss)]
[TestCase(240f, RunPhase.Expired)]
public void ClockUsesApprovedBoundaries(float seconds, RunPhase expected) { }

[Test]
public void LevelOneHwandoOneShotsRat()
{
    var result = DamageResolver.Resolve(new DamageRequest(8, 0, false, 1f));
    Assert.That(result.FinalDamage, Is.EqualTo(8));
}

[Test]
public void OwnedNonMaxWeaponAppearsInThreeOffers() { }

[Test]
public void MaxedAndLockedEvolutionsNeverAppear() { }
```

Use the exact XP thresholds `5, 8, 12, 18, 26, 36, 48, 62`.

- [ ] **Step 2: Run tests and confirm red**

Expected: compile failure because types do not exist.

- [ ] **Step 3: Implement immutable domain types**

```csharp
public readonly record struct DamageRequest(
    int BaseDamage, int FlatBonus, bool IsCritical, float Multiplier);

public readonly record struct DamageResult(int FinalDamage, bool IsCritical);

public enum RunPhase
{
    WaveOne, WaveTwo, WaveThree, Peak, BossWarning, Boss, Expired
}

public readonly record struct UpgradeOffer(
    string Id, UpgradeKind Kind, int NextLevel);

public enum UpgradeKind { Weapon, Support, Evolution }

public sealed record UpgradeState(
    IReadOnlyDictionary<string, int> WeaponLevels,
    IReadOnlyDictionary<string, int> SupportLevels,
    IReadOnlySet<string> UnlockedIds);
```

Round damage away from zero after multiplying and clamp it to at least 1.
Use an injected integer seed for offer ordering; never call Unity random from
Domain.

- [ ] **Step 4: Implement the approved schedule and offers**

`WaveSchedule` returns active caps `28, 36, 48, 64, 36` and content IDs for
each phase. `UpgradeSelector` guarantees one owned non-max weapon when such a
weapon exists, fills remaining unique slots from eligible supports/new
weapons, and emits exactly three offers.

- [ ] **Step 5: Run all Domain tests and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode
git add Assets/JoseonHunter/Scripts/Domain `
  Assets/JoseonHunter/Tests/EditMode
git commit -m "feat: add deterministic patrol combat rules"
```

---

### Task 6: Implement Geumjul Geometry and Mastery Rules

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Geumjul/TrailPoint.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Geumjul/GeumjulTrail.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Geumjul/LoopDetector.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Geumjul/SealResolver.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Geumjul/GeumjulMastery.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GeumjulRuleTests.cs`

**Interfaces:**
- Produces:
  `GeumjulTrail.Add(TrailPoint point)`.
- Produces:
  `LoopDetector.TryClose(IReadOnlyList<TrailPoint> points) : LoopResult`.
- Produces:
  `SealResolver.Resolve(LoopResult loop, IReadOnlyList<TargetPoint> targets) :
  IReadOnlyList<SealHit>`.

- [ ] **Step 1: Write failing geometry tests**

Test:

```text
points older than 4.0 seconds are discarded
trail is trimmed to its most recent 7.0 metres
perimeter below 2.5 metres is invalid
self-intersection closes a polygon
near-first-segment closure uses mastery tolerance
area above pi * 3 * 3 is invalid
map boundary is never a segment
only polygon-contained targets are selected
normal bind is 1.2 seconds
boss damage is floor(20 * 0.35) and bind is zero
mastery changes at 3, 8, 14, and 20 closures
```

- [ ] **Step 2: Run tests and confirm red**

Expected: compile failure.

- [ ] **Step 3: Implement trail trimming and loop detection**

Use Domain-owned `Float2`:

```csharp
public readonly record struct Float2(float X, float Y);
public readonly record struct TrailPoint(Float2 Position, float Time);
public readonly record struct TargetPoint(
    int TargetId, Float2 Position, bool IsBoss);
public readonly record struct LoopResult(
    bool IsValid, IReadOnlyList<Float2> Polygon, float Perimeter, float Area);
public enum SealBranch { None, FireMark, IceBind, FiveColorBarrier }
```

Use segment intersection plus squared-distance closure tolerance. Compute
polygon area with the shoelace formula. Reject repeated zero-length segments
and polygons with fewer than three unique vertices.

- [ ] **Step 4: Implement seal and mastery output**

```csharp
public readonly record struct SealHit(
    int TargetId, int Damage, float BindSeconds, SealBranch Branch);
```

At 14 closures expose exactly one branch choice, Fire Mark or Ice Bind. At 20
closures return Five-Color Barrier chain-explosion data with 40 base damage.

- [ ] **Step 5: Run tests and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.GeumjulRuleTests
git add Assets/JoseonHunter/Scripts/Domain/Geumjul `
  Assets/JoseonHunter/Tests/EditMode/GeumjulRuleTests.cs
git commit -m "feat: implement local geumjul sealing rules"
```

---

### Task 7: Author and Validate Launch Content

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Content/Definitions/*.cs`
- Create: `Assets/JoseonHunter/Scripts/Content/Validation/ContentValidator.cs`
- Create through Editor:
  `Assets/JoseonHunter/Data/{Heroes,Enemies,Weapons,Equipment,Progression,Waves}/*.asset`
- Create: `Assets/JoseonHunter/Scripts/Editor/Content/LaunchContentGenerator.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/LaunchContentTests.cs`

**Interfaces:**
- Produces `HeroDefinition`, `EnemyDefinition`, `WeaponDefinition`,
  `EquipmentDefinition`, `EvolutionNodeDefinition`, `CaseDefinition`, and
  `WaveDefinition`.
- Produces stable IDs consumed by runtime, save, menus, and tests.

- [ ] **Step 1: Write failing content validation tests**

Assert exactly:

```text
3 heroes
5 normal enemies and 1 boss
3 weapons with levels 1-5 and one evolution each
6 support disciplines
12 equipment items across 4 slots
4 qualities: common, tempered, masterwork, spirit-bound
spirit-bound items add one small trait and one approved cosmetic effect
equipment level costs stay within 80-400 coins
12 permanent evolution nodes
evolution node costs stay within 100-450 coins
cosmetic colour cost is 600 coins
the approved wave schedule
all sprite/audio references point to approved manifest entries
all IDs are unique and lowercase snake_case
```

- [ ] **Step 2: Run tests and confirm red**

Expected: compile failure or missing content assets.

- [ ] **Step 3: Implement focused ScriptableObject definitions**

Example:

```csharp
[CreateAssetMenu(menuName = "JoseonHunter/Content/Hero")]
public sealed class HeroDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private int maxHealth;
    [SerializeField] private float moveSpeed;
    [SerializeField] private WeaponDefinition startingWeapon;
    [SerializeField] private HeroPassive passive;
    [SerializeField] private Sprite portrait;
    [SerializeField] private RuntimeAnimatorController animator;
    public string Id => id;
}
```

Keep behavior in Domain/Runtime; definitions contain authored data only.

- [ ] **Step 4: Generate all launch assets through Editor APIs**

`LaunchContentGenerator.Generate()` creates or updates assets by stable ID,
assigns the exact numbers from the spec, and refuses to bind any production
asset whose manifest approval is not `approved`.

- [ ] **Step 5: Validate, run tests, and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.LaunchContentTests
git add Assets/JoseonHunter/Scripts/Content `
  Assets/JoseonHunter/Scripts/Editor/Content `
  Assets/JoseonHunter/Data `
  Assets/JoseonHunter/Tests/EditMode/LaunchContentTests.cs
git commit -m "feat: author validated launch content"
```

---

### Task 8: Build the Pooled Gameplay Runtime

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/App/GameplayCompositionRoot.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Actors/{PlayerController,EnemyController,Health}.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Pooling/{Pool,PoolRegistry}.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Runs/{RunController,WaveSpawner}.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/{TargetIndex,ContactDamage}.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Prefabs/GameplayPrefabGenerator.cs`
- Create through Editor: `Assets/JoseonHunter/Prefabs/Gameplay/*.prefab`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GameplayRuntimeTests.cs`

**Interfaces:**
- Produces:
  `PlayerController.SetMoveInput(Vector2 input)`.
- Produces:
  `PoolRegistry.Spawn(string id, Vector3 position) : GameObject`.
- Produces:
  `RunController.StateChanged : event Action<RunState>`.

- [ ] **Step 1: Write failing PlayMode tests**

Test:

```text
floating input moves the player at the hero's configured speed
diagonal input is normalized
player stays controllable while weapons fire
64 active-enemy cap is enforced before boss
36 cap is enforced during boss
despawned enemy and XP objects return to pools
continuous normal contact cannot defeat the constable before 4 seconds
pause and focus loss stop domain time
```

- [ ] **Step 2: Run PlayMode tests and confirm red**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-PlayMode.ps1 `
  -Filter JoseonHunter.Tests.PlayMode.GameplayRuntimeTests
```

Expected: compile failure.

- [ ] **Step 3: Implement movement, health, targeting, and pools**

Use `Rigidbody2D.MovePosition` in fixed time, squared-distance targeting, and
prewarmed generic pools. `TargetIndex` uses a fixed-cell spatial hash; it
returns reusable caller-provided lists to avoid per-frame allocation.

Use:

```csharp
public enum RunState { Loading, Playing, LevelChoice, Paused, Victory, Defeat }
public event Action<RunState> StateChanged;
```

- [ ] **Step 4: Implement waves and lifecycle**

`RunController` advances the Domain `RunClock`, emits phase transitions once,
cleans normal enemies at 2:45, spawns Fallen General at 3:00, and ends with
timeout defeat at 4:00 if the boss remains alive.

- [ ] **Step 5: Generate prefabs and connect Gameplay scene**

Use `GameplayPrefabGenerator` and Editor APIs. Required scene roots:

```text
World/Stage
World/Actors
World/Projectiles
World/Pickups
World/VFX
Systems
UI
```

No gameplay collider belongs to stage decoration.

- [ ] **Step 6: Run tests, inspect allocations, and commit**

The PlayMode profiler test advances a representative 60-second simulation and
asserts zero managed allocations in steady-state targeting/spawn ticks after
warm-up.

```powershell
git add Assets/JoseonHunter/Scripts/Runtime `
  Assets/JoseonHunter/Scripts/Editor/Prefabs `
  Assets/JoseonHunter/Prefabs/Gameplay `
  Assets/JoseonHunter/Scenes/Gameplay.unity `
  Assets/JoseonHunter/Tests/PlayMode/GameplayRuntimeTests.cs
git commit -m "feat: build pooled portrait gameplay runtime"
```

---

### Task 9: Implement Three Independent Weapons, XP, and Geumjul Presentation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/*.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Progression/{ExperiencePickup,RunUpgradeController}.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Geumjul/GeumjulController.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/{ActorView,WeaponView,GeumjulView}.cs`
- Create through Editor: `Assets/JoseonHunter/Prefabs/{Weapons,VFX}/*.prefab`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponAndGeumjulTests.cs`

**Interfaces:**
- Produces:
  `IWeaponRuntime.Tick(float deltaSeconds, TargetIndex targets)`.
- Produces:
  `RunUpgradeController.Choose(string offerId)`.
- Consumes `LoopDetector` and emits approved Geumjul VFX.

- [ ] **Step 1: Write failing weapon and seal tests**

Cover:

```text
each weapon owns an independent cooldown
Hwando overlap damages one target once per attack
Talisman chain does not revisit a target
Horn Bow selects the densest line and respects pierce count
each level changes the approved statistic
each evolution requires weapon level 5 and support level 2
XP opens exactly three valid offers and pauses combat
closing a loop damages only contained enemies
boss gets 35% seal damage and no bind
player body remains in idle/move during all attacks
```

- [ ] **Step 2: Run tests and confirm red**

Expected: compile failure.

- [ ] **Step 3: Implement weapon runtimes**

Use:

```csharp
public interface IWeaponRuntime
{
    string Id { get; }
    int Level { get; }
    void Tick(float deltaSeconds, TargetIndex targets);
    void Upgrade(WeaponUpgrade upgrade);
}

public readonly record struct WeaponUpgrade(
    string Id, int Level, bool IsEvolution);
```

Weapon logic emits spawn/damage commands; Presentation consumes commands and
plays approved art/audio. No damage is triggered solely by animation events.

- [ ] **Step 4: Implement XP selection and evolution**

XP pickup feeds `ExperienceCurve`. On level, set `Time.timeScale = 0` only in
the presentation adapter while Domain state remains explicit. Apply exactly
one chosen offer, close the modal, and restore the previous pause state.

- [ ] **Step 5: Connect trail sampling and approved seal VFX**

Sample only after movement exceeds 0.08 world units or 0.08 seconds. Render
the active trail with pooled segments, age it visually with Domain timestamps,
and pass world targets through the pure loop resolver.

- [ ] **Step 6: Run tests and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Combat `
  Assets/JoseonHunter/Scripts/Runtime/Progression `
  Assets/JoseonHunter/Scripts/Runtime/Geumjul `
  Assets/JoseonHunter/Scripts/Presentation/Gameplay `
  Assets/JoseonHunter/Prefabs/Weapons Assets/JoseonHunter/Prefabs/VFX `
  Assets/JoseonHunter/Tests/PlayMode/WeaponAndGeumjulTests.cs
git commit -m "feat: add weapons progression and geumjul combat"
```

---

### Task 10: Implement Fallen General and Complete Run Results

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Actors/FallenGeneralController.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/RewardCalculator.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Gameplay/{BossView,ResultsView}.cs`
- Create through Editor: `Assets/JoseonHunter/Prefabs/Actors/fallen_general.prefab`
- Create: `Assets/JoseonHunter/Tests/EditMode/RewardCalculatorTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/BossRunTests.cs`

**Interfaces:**
- Produces boss states `Pursue`, `ChargeWarn`, `Charge`, `ConeWarn`, `Cone`,
  `Summon`, `Enraged`, `Dead`.
- Produces:
  `RewardCalculator.Calculate(RunSummary summary) : RunRewards`.

- [ ] **Step 1: Write failing boss and reward tests**

Assert 900 HP, 0.75-second charge warning, 0.60-second cone warning, one summon
at 40% HP, enrage after 25 seconds, and 25% pattern-frequency increase.

Reward fixtures:

```text
victory total remains within 170-220
defeat total remains within 40-90
abandon reward is proportional to time and kills
boss seal pays 100 only on victory
first solution pays 50 once
```

- [ ] **Step 2: Run tests and confirm red**

Expected: compile failure.

- [ ] **Step 3: Implement the deterministic pattern state machine**

Warnings own their full durations before damage starts. Summon can occur once.
At enrage, multiply pursuit speed and inverse cooldown by 1.25 without reducing
warning durations below the approved values.

- [ ] **Step 4: Implement results and rewards**

`RunRewards` contains coins, new clue IDs, equipment fragment progress, outcome,
duration, kills, and first-solution flag. The view only formats this result.

```csharp
public enum RunOutcome { Victory, Defeat, Abandoned }

public readonly record struct RunSummary(
    RunOutcome Outcome,
    float DurationSeconds,
    int Kills,
    bool BossSealed,
    bool IsFirstSolution);

public readonly record struct RunRewards(
    int Coins,
    IReadOnlyList<string> NewClueIds,
    IReadOnlyDictionary<string, int> FragmentProgress,
    RunOutcome Outcome,
    float DurationSeconds,
    int Kills,
    bool FirstSolution);
```

- [ ] **Step 5: Run tests and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Actors `
  Assets/JoseonHunter/Scripts/Domain/Runs `
  Assets/JoseonHunter/Scripts/Presentation/Gameplay `
  Assets/JoseonHunter/Prefabs/Actors `
  Assets/JoseonHunter/Tests
git commit -m "feat: complete boss encounter and run rewards"
```

---

### Task 11: Implement Save Recovery and Meta Progression

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Save/SaveDataV1.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Save/ISaveRepository.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/{EquipmentProgression,InvestigationCase,EvolutionBoard}.cs`
- Create: `Assets/JoseonHunter/Scripts/Infrastructure/Save/{JsonSaveRepository,SaveChecksum}.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/{SaveRecoveryTests,MetaProgressionTests}.cs`

**Interfaces:**
- Produces:
  `ISaveRepository.Load() : LoadResult`.
- Produces:
  `ISaveRepository.Save(SaveDataV1 data) : SaveResult`.
- Produces equipment, investigation, and evolution transactions.

- [ ] **Step 1: Write failing save and progression tests**

Cover:

```text
new install creates schema 1 defaults
save writes temporary file, backup, then current atomically
checksum mismatch loads backup
current and backup corruption loads safe defaults without crash
coins never become negative
quality uses selected fragments, never three-item merge
equipment has four slots and twelve items
evolution has twelve nodes and free reset refunds all spent coins
first patrol guarantees a unique clue
duplicates are forbidden before 9/9
3/9, 6/9, and 9/9 milestones fire once
6/9 unlocks the Hwando evolution recipe and one selectable investigation policy
9/9 unlocks Shaman and hard difficulty
insufficient storage preserves the previous valid save
run result, equipment/evolution purchase, settings change, and app pause autosave
```

- [ ] **Step 2: Run tests and confirm red**

Expected: compile failure.

- [ ] **Step 3: Implement save schema and repository**

`SaveDataV1` stores:

```text
schemaVersion
coins
owned/equipped hero
equipment levels, qualities, and fragments
evolution node ranks
investigation clues and claimed milestones
monster compendium entries
unlocked heroes, difficulties, recipes, and appearances
best patrol results
tutorial completion
accessibility and audio settings
first-solution flags
```

Define the repository boundary in Domain:

```csharp
public interface ISaveRepository
{
    LoadResult Load();
    SaveResult Save(SaveDataV1 data);
}

public readonly record struct LoadResult(
    SaveDataV1 Data, LoadSource Source, SaveError Error);

public readonly record struct SaveResult(bool Success, SaveError Error);
public enum LoadSource { Current, Backup, Defaults }
public enum SaveError { None, Corrupt, InsufficientStorage, IoFailure }
```

Use UTF-8 JSON, SHA-256 over canonical payload, `.tmp`, `.bak`, and `.json`
paths under `Application.persistentDataPath`. Log only error codes, never user
filesystem contents.

- [ ] **Step 4: Implement transactional progression**

Every transaction returns success or a stable error enum. Perform validation
on a copy, then commit once. Investigation selection draws only from
undiscovered clue IDs until the nine-clue case is complete.

- [ ] **Step 5: Run tests and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Domain/Save `
  Assets/JoseonHunter/Scripts/Domain/Progression `
  Assets/JoseonHunter/Scripts/Infrastructure/Save `
  Assets/JoseonHunter/Tests/EditMode
git commit -m "feat: add recoverable local progression"
```

---

### Task 12: Build the Five-Tab Lobby and Tutorial

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/Lobby/*.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Tutorial/*.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Common/{SafeAreaFitter,TextScaleController,FlashSettings}.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneGenerator.cs`
- Create through Editor: `Assets/JoseonHunter/Prefabs/UI/*.prefab`
- Modify through Editor: `Assets/JoseonHunter/Scenes/Lobby.unity`
- Create: `Assets/JoseonHunter/Tests/PlayMode/{LobbyFlowTests,TutorialTests}.cs`

**Interfaces:**
- Produces tabs `Shop`, `Equipment`, `Patrol`, `Investigation`, `Evolution`.
- Produces a 60-second first-run tutorial state machine.
- Consumes progression transactions and the approved UI assets.

- [ ] **Step 1: Write failing lobby and tutorial tests**

Cover:

```text
five tabs exist in approved order and Patrol is the centre action
shop has no random box, ad, IAP, or fake-discount control
shop grants one 50-coin daily supply and cannot grant it twice in one local day
shop sells disclosed fragments for a selected slot and cosmetic colours directly
equipment shows four slots and selected-fragment training
equipment and evolution changes preview their exact stat result before confirmation
patrol can start with the selected hero/equipment
investigation shows 0/9 through 9/9 and milestone rewards
evolution previews stats and free reset
all primary controls remain inside tested safe areas
UI scales to 130% without clipping primary actions
tutorial teaches floating-joystick movement, enclosing three training spirits,
XP choice, and leaving a red telegraph
tutorial completes within 60 simulated seconds and can be replayed
Reset Progress requires two explicit confirmations
```

- [ ] **Step 2: Run tests and confirm red**

Expected: compile failure or missing hierarchy.

- [ ] **Step 3: Implement presenter/view boundaries**

Views expose user intent events and render immutable view models. Presenters
own transactions and navigation. No view reads or writes save JSON directly.

- [ ] **Step 4: Generate the Lobby hierarchy**

Use Editor APIs:

```text
LobbyRoot
  SafeArea
    Header
    ContentHost
    BottomNavigation
      Shop
      Equipment
      Patrol
      Investigation
      Evolution
  ModalLayer
  ToastLayer
```

Use Canvas Scaler `Scale With Screen Size`, 360 x 640, match 0.5.

- [ ] **Step 5: Implement tutorial and accessibility**

Tutorial advances on the actual action, never on a fixed delay. Settings
include UI scale 100/115/130%, joystick scale 80/100/120%, screen shake
0/50/100%, reduced flashing, high-contrast enemy outlines, patterned geumjul
states, separate vibration/music/SFX controls, and 30/60 FPS modes. The
floating joystick may begin anywhere in the lower play region, primary targets
are at least 48 dp, and defaults avoid full-screen rapid white flashes.

- [ ] **Step 6: Run PlayMode tests and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation `
  Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneGenerator.cs `
  Assets/JoseonHunter/Prefabs/UI Assets/JoseonHunter/Scenes/Lobby.unity `
  Assets/JoseonHunter/Tests/PlayMode
git commit -m "feat: build mobile lobby and first-run tutorial"
```

---

### Task 13: Compose Bootstrap, Audio, Lifecycle, and Diagnostics

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/App/{AppBootstrap,SceneNavigator}.cs`
- Create: `Assets/JoseonHunter/Scripts/Infrastructure/Diagnostics/DiagnosticLog.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Common/{AudioDirector,Haptics}.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/BootstrapSceneGenerator.cs`
- Modify through Editor: `Assets/JoseonHunter/Scenes/Bootstrap.unity`
- Create: `Assets/JoseonHunter/Tests/PlayMode/AppLifecycleTests.cs`

**Interfaces:**
- Produces deterministic Bootstrap -> Lobby -> Gameplay -> Results navigation.
- Produces silent audio and no-op haptics fallbacks.

- [ ] **Step 1: Write failing lifecycle tests**

Test missing/corrupt save, missing optional audio, missing sprite, invalid
content, scene load failure, focus loss, suspend/resume, repeated
Lobby/Gameplay transitions, and app quit during save. Missing sprites use the
approved silhouette and log the content ID; invalid content is excluded while
the default Hwando remains available. Every other case produces a stable
fallback or Bootstrap error code, never an unhandled exception.

- [ ] **Step 2: Run tests and confirm red**

Expected: compile failure.

- [ ] **Step 3: Implement composition and fallbacks**

`AppBootstrap` creates repositories once, loads settings/save, configures
audio/haptics, and navigates to Lobby. `SceneNavigator` serializes requests and
rejects duplicate transitions. Missing clips use `SilentAudioHandle`.

- [ ] **Step 4: Generate Bootstrap scene and verify build order**

Build order remains:

```text
0 Assets/JoseonHunter/Scenes/Bootstrap.unity
1 Assets/JoseonHunter/Scenes/Lobby.unity
2 Assets/JoseonHunter/Scenes/Gameplay.unity
```

- [ ] **Step 5: Run tests and commit**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/App `
  Assets/JoseonHunter/Scripts/Infrastructure/Diagnostics `
  Assets/JoseonHunter/Scripts/Presentation/Common `
  Assets/JoseonHunter/Scripts/Editor/Scenes/BootstrapSceneGenerator.cs `
  Assets/JoseonHunter/Scenes/Bootstrap.unity `
  Assets/JoseonHunter/Tests/PlayMode/AppLifecycleTests.cs
git commit -m "feat: compose offline application lifecycle"
```

---

### Task 14: Validate Performance and Build the Android Release Candidate

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/Build/AndroidReleaseBuilder.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/PerformanceBudgetTests.cs`
- Create: `Tools/Unity/Build-Android.ps1`
- Create: `Docs/Release/android-release-checklist.md`
- Create: `Docs/Release/privacy-policy.md`
- Create: `Docs/Release/data-safety.md`
- Create: `Docs/Verification/2026-07-26-portrait-vertical-slice.md`

**Interfaces:**
- Produces:
  `AndroidReleaseBuilder.Build(string outputPath)`.
- Produces a signed or local-upload-key AAB and reproducible verification
  report without committing secrets.

- [ ] **Step 1: Write failing build-contract and performance tests**

Assert project settings for portrait, API 26/36, IL2CPP, ARM64, AAB, package
ID, version, build code, and build scenes. Performance scenario runs the
64-enemy peak and 36-enemy boss with all three weapons and geumjul; after
warm-up it records frame time, managed allocations, pool expansions, and
retained memory.

- [ ] **Step 2: Run tests and establish the device baseline**

Expected initially: failures for missing release builder or unmet budgets.
Record the actual representative device model and Android version; do not
invent device measurements.

- [ ] **Step 3: Implement the release builder**

The builder:

```text
validates the asset approval gate
runs ContentValidator
sets Android target
uses IL2CPP and ARM64
builds an App Bundle
reads keystore secrets only from environment variables
writes Builds/Android/JoseonHunter-0.1.0-1.aab
returns non-zero on any validation or build failure
```

`Build-Android.ps1` requires `JOSEON_KEYSTORE_PATH`,
`JOSEON_KEYSTORE_PASS`, `JOSEON_KEY_ALIAS`, and `JOSEON_KEY_ALIAS_PASS`.
Never print or commit their values.

- [ ] **Step 4: Meet runtime budgets**

Verify:

```text
60 FPS target on a named representative mid-range Android device
30 FPS floor
no steady-state per-frame managed allocations in active combat systems
no unbounded retained-memory growth over three complete runs
64/36 enemy caps
download target below 150 MB
VFX degrades before combat logic
```

If a budget fails, profile the named subsystem and make a focused fix with a
regression test before continuing.

- [ ] **Step 5: Validate Android packaging**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Build-Android.ps1
```

Then use current Android SDK/bundletool tooling to verify manifest SDK levels,
ARM64 libraries, 16 KB page compatibility, generated download size, and
internal-test installation. Record exact tool versions and outputs.

- [ ] **Step 6: Complete store and policy records**

The privacy policy states local-only gameplay and the exact data behavior.
The Data safety document matches the shipped SDK and permissions. The release
checklist records content rating, audience, advertising declaration, screenshots,
feature art, icon, privacy URL, credits, and asset provenance.

Also record whether the Play account is subject to the twelve-testers-for-
fourteen-days production-access rule. Report that as an external gate, never
as a product defect.

- [ ] **Step 7: Run complete verification**

```powershell
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-ProductionAssets.ps1
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-PlayMode.ps1
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Build-Android.ps1
```

Inspect the Unity Console and require zero compile errors or unhandled
exceptions. Complete three device runs covering tutorial, victory, defeat,
save/reload, focus loss, and recovery.

- [ ] **Step 8: Write verification evidence and commit**

The verification document records commands, exit codes, XML result paths,
test counts, device measurements, AAB hash/size, 16 KB result, permissions,
known external release gates, and zero hidden waivers.

```powershell
git add Assets/JoseonHunter/Scripts/Editor/Build `
  Assets/JoseonHunter/Tests/PlayMode/PerformanceBudgetTests.cs `
  Tools/Unity/Build-Android.ps1 Docs/Release Docs/Verification
git commit -m "release: verify portrait Android candidate"
```

---

## Final Acceptance Gate

Do not call the release candidate complete until fresh evidence confirms:

- all seven asset batches and the consolidated receipt are user-approved;
- all production assets pass dimensions, frames, pivots, palette, import,
  provenance, license, hash, and path validation;
- three heroes, three weapons, five normal enemies, and Fallen General use
  approved assets;
- the player can complete the tutorial without outside instruction;
- geumjul loops cannot use expired segments or map boundaries;
- the boss appears at 3:00 and every run ends by 4:00;
- victory, defeat, and abandonment award valid progress;
- Shop, Equipment, Patrol, Investigation, and Evolution work and persist;
- save corruption recovers without a crash;
- every EditMode and PlayMode test passes;
- three representative device runs meet the measured performance floor;
- the API-36 ARM64 IL2CPP AAB passes 16 KB and internal-test validation;
- store, privacy, Data safety, rights, and credits records match the binary;
- Play-account eligibility and Google review timing are reported separately
  from engineering completion.
