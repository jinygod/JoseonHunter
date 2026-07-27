# Static Sprite Launch Asset Batch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce, approve, import, and visibly exercise twelve coherent static
pixel-art assets for the first Joseon Hunter playable release.

**Architecture:** Each roster entry owns one source directory containing a
`64 x 64` transparent PNG, prompt, palette, and non-secret provenance. A shared
contract validates the source batch and byte-identical runtime copies. Unity
imports each approved PNG as one sprite, while a presentation-only component
uses velocity to flip and procedurally animate the static image.

**Tech Stack:** Unity 6, C# EditMode/PlayMode tests, Unity AssetPostprocessor,
PowerShell 7-compatible asset scripts, PixelLab MCP one-generation image tools,
PNG RGBA32, JSON manifests, NUnit

## Global Constraints

- The batch contains exactly twelve IDs: `rookie_constable`, `shaman`,
  `mountain_hunter`, `plague_rat`, `vengeful_spirit`, `sakkat_specter`,
  `dokkaebi`, `bandit`, `fallen_general`, `coin`, `experience_spirit_flame`,
  and `treasure_chest`.
- Every production sprite is exactly `64 x 64` RGBA with alpha only `0` or
  `255`, four transparent corners, no baked background, no external shadow,
  no anti-aliasing, and no more than 48 opaque colors.
- The common opaque bottom anchor is `(32, 56)`; horizontal opaque-bounds center
  must be within `30.0..34.0`, and the maximum opaque y must equal `56`.
- Humanoids use approximately two-head-tall proportions and all assets preserve
  the approved constable's one-pixel near-black outline weight, face scale, and
  pixel density.
- One PNG owns both directions: positive horizontal velocity uses
  `flipX = false`, negative uses `flipX = true`, and zero preserves the last
  facing value.
- No character attack animation is created. Weapons and effects own attacks.
- The abandoned four-frame constable walk pilot remains source-only and must
  not be copied to a runtime folder or referenced by the new catalog.
- PixelLab credentials never enter files, logs, prompts, commits, or reports.
- Use only PixelLab operations quoted at one generation per `64 x 64` image.
  Do not invoke any operation quoting 20 or more generations.
- The eleven new images may consume at most sixteen generations total,
  including failed candidates and retries.
- No static batch asset enters runtime before explicit user approval of the
  consolidated review board.
- Runtime sprites use point filtering, no mipmaps, no texture compression,
  `32` pixels per unit, `SpriteImportMode.Single`, and pivot `(0.5, 0.125)`.
- Preserve all legacy sprite-sheet assets and tests; the new static contract is
  additive.

---

### Task 1: Static Sprite Batch Contract and Import Profile

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/StaticSpriteBatchContract.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/StaticSpriteBatchContract.cs.meta`
- Create: `Assets/JoseonHunter/Tests/EditMode/StaticSpriteBatchContractTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StaticSpriteBatchContractTests.cs.meta`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/AssetImportProfileTests.cs`
- Create: `Tools/Assets/Test-StaticSpriteBatch.ps1`
- Create: `Tools/Assets/Test-StaticSpriteBatchScriptTests.ps1`

**Interfaces:**
- Consumes: a batch manifest, source directories, and an optional runtime root.
- Produces:
  `StaticSpriteBatchContract.Validate(string manifestPath, string sourceRoot,
  string runtimeRoot, bool requireRuntime)`,
  `StaticSpriteBatchContract.ValidateAsset(string assetId,
  string sourceDirectory)`, and
  `StaticSpriteBatchValidationResult(IReadOnlyList<string> Errors, int AssetCount)`.

- [ ] **Step 1: Write failing contract tests**

Create a temporary fixture containing this exact manifest shape:

```json
{
  "schemaVersion": 1,
  "promptRevision": "static-launch-v1",
  "assets": [
    {
      "id": "rookie_constable",
      "role": "hero",
      "sourcePath": "rookie_constable/sprite.png",
      "runtimePath": "Heroes/rookie_constable.png",
      "width": 64,
      "height": 64,
      "footAnchor": [32, 56],
      "pivot": [0.5, 0.125],
      "pixelsPerUnit": 32,
      "approvalStatus": "pending",
      "sha256": ""
    }
  ]
}
```

Test one valid twelve-entry fixture and negative fixtures for: missing ID,
duplicate ID, unexpected thirteenth ID, wrong dimensions, non-RGBA input,
semi-transparent pixel, opaque corner, more than 48 opaque colors,
`maxY != 56`, horizontal center outside `30.0..34.0`, missing prompt,
missing provenance, token-like provenance value, source hash mismatch, and
runtime byte mismatch when `requireRuntime` is true.

```csharp
var result = StaticSpriteBatchContract.Validate(
    manifestPath, sourceRoot, runtimeRoot, requireRuntime: false);
Assert.That(result.Errors, Is.Empty);
Assert.That(result.AssetCount, Is.EqualTo(12));
```

- [ ] **Step 2: Run the focused contract test and verify failure**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.StaticSpriteBatchContractTests
```

Expected: FAIL because `StaticSpriteBatchContract` does not exist.

- [ ] **Step 3: Implement the contract**

Define:

```csharp
public sealed record StaticSpriteBatchValidationResult(
    IReadOnlyList<string> Errors,
    int AssetCount);

public static class StaticSpriteBatchContract
{
    public const int CanvasSize = 64;
    public const int MaxOpaqueColors = 48;
    public static readonly Vector2Int FootAnchor = new(32, 56);
    public static readonly Vector2 Pivot = new(0.5f, 0.125f);

    public static StaticSpriteBatchValidationResult Validate(
        string manifestPath,
        string sourceRoot,
        string runtimeRoot,
        bool requireRuntime);

    public static IReadOnlyList<string> ValidateAsset(
        string assetId,
        string sourceDirectory);
}
```

Use `Texture2D.LoadImage`, inspect `Color32` values, compute inclusive opaque
bounds, compare lowercase SHA-256 values, and scan every JSON string value with
case-insensitive patterns `api[_-]?key`, `token`, `secret`, `bearer`, and UUID
values only when their property name is not `jobId`.

- [ ] **Step 4: Write the failing static importer test**

Add a `64 x 64` fixture at:

`Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/import_profile_test.png`

Assert:

```csharp
Assert.That(texture.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
Assert.That(texture.spriteAlignment, Is.EqualTo((int)SpriteAlignment.Custom));
Assert.That(texture.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.125f)));
Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
Assert.That(texture.mipmapEnabled, Is.False);
Assert.That(texture.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
Assert.That(texture.spritePixelsPerUnit, Is.EqualTo(32f));
```

Keep the existing 12-slice and 38-slice assertions unchanged.

- [ ] **Step 5: Implement the static importer branch**

Add:

```csharp
private const string StaticSpriteRuntimeRoot =
    "Assets/JoseonHunter/Art/StaticSprites/Runtime/";
```

Before the sheet branch, detect this root and set single-sprite custom pivot,
point filtering, no mipmaps, uncompressed texture, and `32f` PPU. Do not apply
the Android `ASTC_6x6` override to this root.

- [ ] **Step 6: Add PowerShell batch preflight**

`Test-StaticSpriteBatch.ps1` accepts:

```powershell
param(
  [Parameter(Mandatory=$true)][string]$ManifestPath,
  [Parameter(Mandatory=$true)][string]$SourceRoot,
  [string]$RuntimeRoot = '',
  [switch]$RequireRuntime
)
```

It resolves paths, rejects missing files before launching Unity, runs the
contract through a batchmode editor entry point, and returns non-zero on any
error. Script tests use temporary paths containing spaces and verify
`-RequireRuntime` forwarding.

- [ ] **Step 7: Run focused and full tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-StaticSpriteBatchScriptTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.StaticSpriteBatchContractTests
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.AssetImportProfileTests
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1
```

Expected: all PASS; legacy sprite-sheet tests remain green.

- [ ] **Step 8: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Editor/AssetProduction `
  Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs `
  Assets/JoseonHunter/Tests/EditMode `
  Tools/Assets/Test-StaticSpriteBatch.ps1 `
  Tools/Assets/Test-StaticSpriteBatchScriptTests.ps1
git commit -m "feat: add static sprite batch contract"
```

---

### Task 2: Hero and Pickup Static Sources

**Files:**
- Create: `ArtSource/Pixel/StaticSprites/static-sprite-batch.json`
- Create: `ArtSource/Pixel/StaticSprites/rookie_constable/`
- Create: `ArtSource/Pixel/StaticSprites/shaman/`
- Create: `ArtSource/Pixel/StaticSprites/mountain_hunter/`
- Create: `ArtSource/Pixel/StaticSprites/coin/`
- Create: `ArtSource/Pixel/StaticSprites/experience_spirit_flame/`
- Create: `ArtSource/Pixel/StaticSprites/treasure_chest/`
- Create: `Docs/Assets/review/static-sprite-launch-board.png`
- Create: `Docs/Assets/review/static-sprite-launch.md`

Each source directory contains `sprite.png`, `palette.png`, `prompt.md`, and
`provenance.json`.

**Interfaces:**
- Consumes: approved
  `ArtSource/Pixel/Characters/front-facing/rookie-constable/base.png` and the
  Task 1 preflight.
- Produces: six technically valid pending hero/pickup entries.

- [ ] **Step 1: Initialize the exact twelve-entry manifest**

Use the IDs and roles from Global Constraints. Set all `approvalStatus` values
to `"pending"` and all `sha256` values to the actual lowercase source hashes as
files are accepted. Use category runtime paths:

```text
Heroes/{id}.png
Enemies/{id}.png
Bosses/{id}.png
Pickups/{id}.png
```

- [ ] **Step 2: Reuse the approved constable**

Copy the approved normalized `base.png` bytes to
`StaticSprites/rookie_constable/sprite.png`. Copy its approved palette, record
the source hash and original provenance link, and state
`generationConsumedForStaticBatch: 0`.

- [ ] **Step 3: Save exact generation prompts**

Every new `prompt.md` consists of this exact common block followed by exactly
one subject block:

```text
Original game-ready pixel-art sprite for Joseon Hunter, matching the supplied
approved rookie constable only in proportion, one-pixel near-black outline,
face scale, palette restraint, and pixel density. Exactly 64x64 RGBA,
transparent background, hard alpha, no anti-aliasing, no external shadow.
Cute compact two-head-tall silhouette where humanoid, front-biased
three-quarter pose generally facing right, centered at bottom anchor (32,56).
Readable at native scale. Create a new design; do not reproduce any commercial
character, logo, costume, or source pixels. One subject only, no text, no
frame, no scenery, no attack pose.
```

Subject blocks:

```text
SHAMAN: Friendly but capable young Joseon shaman in ivory and muted-red ritual
clothing, small ritual fan and one paper charm, black hair tied simply.

MOUNTAIN HUNTER: Rugged cute Joseon mountain hunter in brown and forest-green
practical clothing, compact horn bow and small quiver, warm determined face.

COIN: One round brass Joseon yeopjeon coin with a clear square center hole,
slight three-quarter thickness, bold readable outline, compact footprint.

EXPERIENCE SPIRIT FLAME: One cyan-blue Korean spirit flame pickup with a bright
cream core, three compact flame lobes, friendly magical glow represented by
solid pixel clusters only.

TREASURE CHEST: One small dark-wood Joseon travel chest with brass trim and a
red paper seal, closed, sturdy, compact, readable latch.
```

- [ ] **Step 4: Generate and normalize five candidates**

Use a PixelLab one-generation `64 x 64` transparent image operation. Where an
init/style image is supported at the same one-generation cost, supply the
approved constable. Save raw output only when deterministic normalization is
needed. Allowed normalization is nearest-neighbor uniform scaling, integer
translation, hard-alpha thresholding, and exact palette extraction; do not
redraw anatomy or equipment.

- [ ] **Step 5: Validate after each accepted image**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-StaticSpriteBatch.ps1 `
  -ManifestPath ArtSource/Pixel/StaticSprites/static-sprite-batch.json `
  -SourceRoot ArtSource/Pixel/StaticSprites
```

During partial production, invoke `ValidateAsset` separately for every completed
ID. Do not run the twelve-entry batch result as a partial pass, and never
suppress image-format, anchor, hash, or secret findings.

- [ ] **Step 6: Create the partial review board**

Show the six native sprites, 8x nearest-neighbor enlargements, light/dark
checks, names, per-asset hashes, and PixelLab generation usage. Mark it
`HEROES AND PICKUPS COMPLETE — FULL BATCH PENDING`.

- [ ] **Step 7: Commit**

```powershell
git add ArtSource/Pixel/StaticSprites `
  Docs/Assets/review/static-sprite-launch-board.png `
  Docs/Assets/review/static-sprite-launch.md
git commit -m "art: create static heroes and pickups"
```

---

### Task 3: Enemy and Boss Static Sources

**Files:**
- Create: `ArtSource/Pixel/StaticSprites/plague_rat/`
- Create: `ArtSource/Pixel/StaticSprites/vengeful_spirit/`
- Create: `ArtSource/Pixel/StaticSprites/sakkat_specter/`
- Create: `ArtSource/Pixel/StaticSprites/dokkaebi/`
- Create: `ArtSource/Pixel/StaticSprites/bandit/`
- Create: `ArtSource/Pixel/StaticSprites/fallen_general/`
- Modify: `ArtSource/Pixel/StaticSprites/static-sprite-batch.json`
- Modify: `Docs/Assets/review/static-sprite-launch-board.png`
- Modify: `Docs/Assets/review/static-sprite-launch.md`

Each new source directory contains `sprite.png`, `palette.png`, `prompt.md`,
and `provenance.json`.

**Interfaces:**
- Consumes: Task 2 master style and Task 1 preflight.
- Produces: all twelve technically valid pending sources and the consolidated
  visual approval board.

- [ ] **Step 1: Save the enemy prompt files**

Use the exact common prompt from Task 2 and one subject block:

```text
PLAGUE RAT: One hunched grey-brown plague rat, oversized ears, tiny sharp
teeth, sickly olive-green cloth tag, hostile but cute, four feet readable.

VENGEFUL SPIRIT: One pale blue-white Joseon vengeful spirit, long dark hair,
small angry eyes, ragged white burial clothing, floating trailing lower body.

SAKKAT SPECTER: One compact ghost under an oversized straw sakkat, dark muted
robe, one readable yellow paper charm held forward, face mostly shadowed.

DOKKAEBI: One compact teal-blue Korean dokkaebi, two short horns, broad cheeky
hostile face, rough brown waistcloth, small wooden club held low.

BANDIT: One cute hostile Joseon mountain bandit, dark cloth mask, red headband,
brown patched clothing, compact short blade held low, no modern gear.

FALLEN GENERAL: One undead Joseon general boss, dark iron lamellar armor,
weathered red commander sash, crested helmet, glowing pale eyes, broken compact
polearm held low. Fill more of the 64x64 canvas than normal enemies while
keeping the same anchor and pixel density.
```

- [ ] **Step 2: Generate and normalize six candidates**

Use only one-generation PixelLab operations and the same allowed deterministic
normalization as Task 2. Record every attempted job and generation count,
including rejected outputs. Stop the entire task before the batch reaches
sixteen new generations.

- [ ] **Step 3: Run complete source preflight**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-StaticSpriteBatch.ps1 `
  -ManifestPath ArtSource/Pixel/StaticSprites/static-sprite-batch.json `
  -SourceRoot ArtSource/Pixel/StaticSprites
```

Expected: PASS for all twelve entries with no suppressed missing-entry errors.

- [ ] **Step 4: Build the consolidated review board**

Lay out heroes, normal enemies, boss, and pickups in labeled rows. Include
native size, 8x nearest-neighbor enlargement, light/dark checks, total
generation usage, and `PENDING BATCH APPROVAL`.

- [ ] **Step 5: Commit and stop for explicit visual approval**

```powershell
git add ArtSource/Pixel/StaticSprites `
  Docs/Assets/review/static-sprite-launch-board.png `
  Docs/Assets/review/static-sprite-launch.md
git commit -m "art: prepare static launch batch review"
```

Return the board as directly attached PNG data. Do not create runtime copies,
production manifest entries, prefabs, or scene bindings until the user
explicitly approves the consolidated board.

---

### Task 4: Approved Runtime Assets and Production Records

**Files:**
- Create: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/*.png`
- Create: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/*.png`
- Create: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png`
- Create: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/*.png`
- Modify: `ArtSource/Pixel/StaticSprites/static-sprite-batch.json`
- Modify: `Docs/Assets/production-asset-manifest.json`
- Modify: `Docs/Assets/asset-rights-ledger.csv`
- Modify: `Docs/Assets/review/static-sprite-launch.md`

**Interfaces:**
- Consumes: explicit user approval and twelve validated source PNGs.
- Produces: twelve byte-identical runtime PNGs and approved production records.

- [ ] **Step 1: Record visual approval**

Set every static batch manifest entry to `"approvalStatus": "approved"` and
record the approval date `2026-07-27` in the review document. Do not alter
legacy pending entries.

- [ ] **Step 2: Copy source bytes to runtime paths**

Use `Copy-Item -LiteralPath` for each exact manifest mapping. Do not resize,
re-encode, or repack the approved PNGs.

- [ ] **Step 3: Add production manifest entries**

Add twelve entries with:

```json
{
  "batch": "static_launch",
  "kind": "single_sprite",
  "width": 64,
  "height": 64,
  "frameCount": 1,
  "pivotX": 0.5,
  "pivotY": 0.125,
  "pixelsPerUnit": 32,
  "licenseStatus": "approved",
  "approvalStatus": "approved",
  "promptRevision": "static-launch-v1"
}
```

Use IDs `static_{id}_runtime`, exact source/runtime paths, and actual lowercase
runtime SHA-256 values.

- [ ] **Step 4: Add rights-ledger entries**

Record PixelLab as provider, PixelLab terms URL, prompt/provenance evidence,
source hash, `ai_generated`, deterministic normalization notes, no credit
requirement, and status `approved`. The reused constable entry references its
earlier approved generation and records no new generation.

- [ ] **Step 5: Validate runtime equality and import settings**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-StaticSpriteBatch.ps1 `
  -ManifestPath ArtSource/Pixel/StaticSprites/static-sprite-batch.json `
  -SourceRoot ArtSource/Pixel/StaticSprites `
  -RuntimeRoot Assets/JoseonHunter/Art/StaticSprites/Runtime `
  -RequireRuntime
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.AssetImportProfileTests
```

Expected: PASS with twelve byte-equal files and single-sprite custom pivots.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Art/StaticSprites `
  ArtSource/Pixel/StaticSprites/static-sprite-batch.json `
  Docs/Assets/production-asset-manifest.json `
  Docs/Assets/asset-rights-ledger.csv `
  Docs/Assets/review/static-sprite-launch.md
git commit -m "art: import approved static launch sprites"
```

---

### Task 5: Procedural Static Sprite Presentation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/StaticSpriteMotionPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/StaticSpriteMotionPresenter.cs.meta`
- Create: `Assets/JoseonHunter/Tests/EditMode/StaticSpriteMotionStateTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StaticSpriteMotionStateTests.cs.meta`
- Create: `Assets/JoseonHunter/Tests/PlayMode/StaticSpriteMotionPresenterTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/StaticSpriteMotionPresenterTests.cs.meta`

**Interfaces:**
- Consumes: `SpriteRenderer`, externally supplied velocity, hit events, and
  death events.
- Produces:
  `SetVelocity(Vector2 velocity)`, `ShowHit()`, `PlayDeath()`, and
  presentation-only transform/color changes.

- [ ] **Step 1: Write failing direction-state tests**

Define test expectations:

```csharp
var state = new StaticSpriteMotionState();
state.SetVelocity(new Vector2(1f, 0f));
Assert.That(state.FlipX, Is.False);
state.SetVelocity(new Vector2(-1f, 0f));
Assert.That(state.FlipX, Is.True);
state.SetVelocity(Vector2.zero);
Assert.That(state.FlipX, Is.True);
```

Also assert moving bob amplitude is `1f / 32f` world units, idle bob is zero,
maximum tilt is `2f` degrees, and `Reset()` restores zero offset/rotation
without changing last facing.

- [ ] **Step 2: Run the EditMode test and verify failure**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.StaticSpriteMotionStateTests
```

Expected: FAIL because the state and presenter do not exist.

- [ ] **Step 3: Implement state and presenter**

Implement a plain state type and MonoBehaviour in the same focused file:

```csharp
public sealed class StaticSpriteMotionState
{
    public bool FlipX { get; private set; }
    public float BobOffset { get; private set; }
    public float TiltDegrees { get; private set; }
    public void SetVelocity(Vector2 velocity);
    public void Step(float deltaTime);
    public void Reset();
}

public sealed class StaticSpriteMotionPresenter : MonoBehaviour
{
    public void SetVelocity(Vector2 velocity);
    public void ShowHit();
    public void PlayDeath();
}
```

Use a `6 Hz` sine wave, bob amplitude `1f / 32f`, tilt limit `2f`, hit duration
`0.08f`, and death duration `0.35f`. Cache original local position, rotation,
scale, and color in `Awake`. `PlayDeath()` only shrinks, settles by
`2f / 32f`, and fades; it never disables combat or destroys the GameObject.

- [ ] **Step 4: Write PlayMode behavior tests**

Create a GameObject with `SpriteRenderer` and the presenter. Verify right,
left, and zero direction; moving/idle offsets; hit color returns to the
original after `0.08f`; death reaches alpha zero and leaves the GameObject
active after `0.35f`.

- [ ] **Step 5: Run focused and full tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.StaticSpriteMotionStateTests
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.PlayMode.StaticSpriteMotionPresenterTests
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1
```

Expected: all PASS with no new Console errors.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/StaticSpriteMotionPresenter.cs `
  Assets/JoseonHunter/Scripts/Presentation/StaticSpriteMotionPresenter.cs.meta `
  Assets/JoseonHunter/Tests/EditMode/StaticSpriteMotionStateTests.cs `
  Assets/JoseonHunter/Tests/EditMode/StaticSpriteMotionStateTests.cs.meta `
  Assets/JoseonHunter/Tests/PlayMode/StaticSpriteMotionPresenterTests.cs `
  Assets/JoseonHunter/Tests/PlayMode/StaticSpriteMotionPresenterTests.cs.meta
git commit -m "feat: animate static sprites procedurally"
```

---

### Task 6: Static Sprite Catalog, Prefabs, and Gameplay Scene Proof

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Content/StaticSpriteCatalog.cs`
- Create: `Assets/JoseonHunter/Scripts/Content/StaticSpriteCatalog.cs.meta`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/StaticSpriteContentGenerator.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/StaticSpriteContentGenerator.cs.meta`
- Create: `Assets/JoseonHunter/Content/StaticSpriteCatalog.asset`
- Create: `Assets/JoseonHunter/Prefabs/StaticSprites/*.prefab`
- Modify: `Assets/JoseonHunter/Scenes/Gameplay.unity`
- Create: `Assets/JoseonHunter/Tests/EditMode/StaticSpriteContentTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StaticSpriteContentTests.cs.meta`

**Interfaces:**
- Consumes: approved runtime sprites and
  `StaticSpriteMotionPresenter`.
- Produces: a twelve-entry catalog, twelve reusable prefabs, and a disabled
  `StaticSpriteLaunchProof` lineup under `Gameplay/SceneRoot/World`.

- [ ] **Step 1: Write failing catalog and scene tests**

Assert the catalog contains exactly the twelve required IDs with unique,
non-null sprites and prefabs. Assert every prefab has exactly one
`SpriteRenderer` and one `StaticSpriteMotionPresenter`. Open `Gameplay.unity`
and assert `SceneRoot/World/StaticSpriteLaunchProof` exists, is inactive, and
has twelve children named by ID.

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.StaticSpriteContentTests
```

Expected: FAIL because the catalog and generated content do not exist.

- [ ] **Step 3: Implement the catalog**

```csharp
[CreateAssetMenu(menuName = "JoseonHunter/Static Sprite Catalog")]
public sealed class StaticSpriteCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public Sprite sprite;
        public GameObject prefab;
    }

    [SerializeField] private Entry[] entries;
    public IReadOnlyList<Entry> Entries => entries;
    public bool TryGet(string id, out Entry entry);
}
```

`TryGet` uses ordinal ID comparison and returns false with `entry = null` for
null, empty, or unknown IDs.

- [ ] **Step 4: Implement deterministic content generation**

Add menu item `JoseonHunter/Assets/Generate Static Launch Content`. Refuse to
overwrite a dirty open `Gameplay.unity`. Load sprites through exact manifest
runtime paths, create prefabs with a renderer and presenter, populate the
catalog, add the inactive proof lineup beneath `World`, save assets, and log
one summary line.

- [ ] **Step 5: Generate content and validate**

Run the editor method in batchmode, then:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.StaticSpriteContentTests
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-StaticSpriteBatch.ps1 `
  -ManifestPath ArtSource/Pixel/StaticSprites/static-sprite-batch.json `
  -SourceRoot ArtSource/Pixel/StaticSprites `
  -RuntimeRoot Assets/JoseonHunter/Art/StaticSprites/Runtime `
  -RequireRuntime
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1
git diff --check
```

Expected: all PASS, no new Console errors, no whitespace errors, and the
inactive proof lineup visibly contains the approved batch when enabled.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Content/StaticSpriteCatalog.cs `
  Assets/JoseonHunter/Scripts/Content/StaticSpriteCatalog.cs.meta `
  Assets/JoseonHunter/Scripts/Editor/AssetProduction/StaticSpriteContentGenerator.cs `
  Assets/JoseonHunter/Scripts/Editor/AssetProduction/StaticSpriteContentGenerator.cs.meta `
  Assets/JoseonHunter/Content `
  Assets/JoseonHunter/Prefabs/StaticSprites `
  Assets/JoseonHunter/Scenes/Gameplay.unity `
  Assets/JoseonHunter/Tests/EditMode/StaticSpriteContentTests.cs `
  Assets/JoseonHunter/Tests/EditMode/StaticSpriteContentTests.cs.meta
git commit -m "feat: bind static launch sprite content"
```
