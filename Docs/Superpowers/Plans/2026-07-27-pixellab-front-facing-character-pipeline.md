# PixelLab Front-Facing Character Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the PixelLab free-trial MCP securely and produce one approved, original, front-facing two-head-tall rookie constable with a four-frame cute walk and a Unity-ready 12-frame sheet.

**Architecture:** PixelLab produces the base pixel art and animation frames under a six-generation pilot budget. A new front-facing asset contract and runtime folder coexist with the legacy 38-frame directional assets, so rejected work remains unused without breaking the existing project. Deterministic tooling validates and packs approved PixelLab frames; no generated asset enters gameplay before an explicit user approval gate.

**Tech Stack:** PixelLab streamable HTTP MCP, Codex MCP configuration, PowerShell 7, Unity 6 Editor scripting, C#, NUnit EditMode tests, RGBA PNG, Git

## Global Constraints

- Character view is front-facing for movement up, down, left, and right.
- Character proportion is approximately two heads tall; the head occupies about 50% of the standing silhouette.
- Runtime cells are 64 x 64 RGBA PNG at 32 PPU with bottom-center pivot `(0.5, 0.125)`.
- Animation order is idle 2 frames, move 4 frames, death 6 frames; no hit or character attack frames.
- Move frames use alternating feet, one-to-two-pixel horizontal sway, and a small vertical bounce.
- Weapons and attack effects are independent runtime objects.
- The supplied SPUM screenshot is a proportion/readability reference only. Do not copy, redistribute, trace, or commit it as production art.
- PixelLab's API token must never appear in the repository, reports, prompts, screenshots, shell output, or Git history.
- Use no more than six PixelLab fast generations for the rookie-constable pilot without renewed user approval.
- Stop for explicit user approval after the base sprite and again after the four-frame movement preview.
- Preserve rejected legacy character assets as `pending`; do not use them in gameplay.

## File Structure

- `C:/Users/전성진/.codex/config.toml` — local MCP registration only; contains the environment-variable name, never the token.
- User environment variable `PIXELLAB_API_TOKEN` — local secret storage outside Git.
- `Docs/Assets/pixellab-connection.md` — non-secret connection and free-budget verification record.
- `Assets/JoseonHunter/Scripts/Editor/AssetProduction/FrontFacingCharacterSheetContract.cs` — validates the 12-frame production contract.
- `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs` — imports approved front-facing sheets as 12 sprites while preserving legacy 38-sprite imports.
- `Assets/JoseonHunter/Tests/EditMode/FrontFacingCharacterSheetContractTests.cs` — contract and negative tests.
- `Assets/JoseonHunter/Tests/EditMode/AssetImportProfileTests.cs` — front-facing importer tests.
- `Tools/Assets/Pack-FrontFacingCharacterSheet.ps1` — packs approved frame PNGs into a deterministic 256 x 192 sheet.
- `Tools/Assets/Test-FrontFacingCharacter.ps1` — runs file-level checks before Unity tests.
- `ArtSource/Pixel/Characters/front-facing/rookie-constable/` — PixelLab source frames, palette, manifest, provenance, and review images.
- `Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png` — Unity runtime sheet created only after approval.
- `Docs/Assets/review/rookie-constable-pixellab.md` — two-stage visual approval record.
- `Docs/Assets/review/rookie-constable-pixellab-board.png` — light/dark review contact sheet.
- `Docs/Assets/production-asset-manifest.json` — pending/approved state, dimensions, hash, prompt revision, and provenance pointer.

---

### Task 1: Secure PixelLab MCP Connection

**Files:**
- Modify outside Git: `C:/Users/전성진/.codex/config.toml`
- Create outside Git: user environment variable `PIXELLAB_API_TOKEN`
- Create: `Docs/Assets/pixellab-connection.md`

**Interfaces:**
- Consumes: PixelLab account page's existing `Copy API key` action.
- Produces: MCP server named `pixellab` at `https://api.pixellab.ai/mcp`, authenticated through `PIXELLAB_API_TOKEN`.

- [ ] **Step 1: Copy the PixelLab token without printing it**

In the user-authorized Chrome PixelLab account tab, click the unique `Copy API key` button. Do not read the token into a model-visible response and do not include it in a screenshot.

- [ ] **Step 2: Validate and store the clipboard token in the user environment**

Run this PowerShell without echoing `$token`:

```powershell
$token = (Get-Clipboard -Raw).Trim()
if ($token -notmatch '^[0-9a-fA-F-]{36}$') {
  throw 'PixelLab token format was not recognized.'
}
[Environment]::SetEnvironmentVariable('PIXELLAB_API_TOKEN', $token, 'User')
$token = $null
[GC]::Collect()
Write-Output 'PIXELLAB_API_TOKEN stored for the current Windows user.'
```

Expected: only the final non-secret confirmation line.

- [ ] **Step 3: Register the streamable HTTP MCP**

First run `codex mcp list`. If no `pixellab` row exists, run:

```powershell
codex mcp add pixellab `
  --url https://api.pixellab.ai/mcp `
  --bearer-token-env-var PIXELLAB_API_TOKEN
```

Expected: the MCP is added without embedding a bearer value in `config.toml`.

- [ ] **Step 4: Verify the local configuration is secret-free**

Run:

```powershell
$config = Get-Content -Raw 'C:\Users\전성진\.codex\config.toml'
if ($config -notmatch '\[mcp_servers\.pixellab\]') { throw 'Missing pixellab MCP section.' }
if ($config -notmatch 'bearer_token_env_var\s*=\s*"PIXELLAB_API_TOKEN"') { throw 'Missing bearer env binding.' }
if ($config -match '[0-9a-fA-F]{8}-[0-9a-fA-F-]{27,}') { throw 'A token-like value was written to config.toml.' }
Write-Output 'PixelLab MCP config references only PIXELLAB_API_TOKEN.'
```

- [ ] **Step 5: Record the non-secret connection contract**

Create `Docs/Assets/pixellab-connection.md` with:

```markdown
# PixelLab connection

- MCP name: `pixellab`
- Endpoint: `https://api.pixellab.ai/mcp`
- Authentication: user-scoped `PIXELLAB_API_TOKEN`
- Token stored in repository: no
- Trial observed on 2026-07-27: 40 fast generations, then 5 slower daily generations
- Pilot budget: maximum 6 fast generations
- Restart required after initial MCP registration: yes
```

- [ ] **Step 6: Commit the non-secret record**

```powershell
git add Docs/Assets/pixellab-connection.md
git commit -m "docs: record PixelLab MCP connection contract"
```

- [ ] **Step 7: Restart gate**

Restart Codex so the desktop process inherits `PIXELLAB_API_TOKEN` and loads the new MCP. After restart, search available tools for `pixellab`; require at least one authenticated PixelLab tool call that reads account balance without consuming a generation.

Expected: trial balance reports 40 available fast generations or the current lower value if a pilot call has already run.

---

### Task 2: Front-Facing 12-Frame Unity Contract

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/FrontFacingCharacterSheetContract.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/FrontFacingCharacterSheetContract.cs.meta`
- Create: `Assets/JoseonHunter/Tests/EditMode/FrontFacingCharacterSheetContractTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/FrontFacingCharacterSheetContractTests.cs.meta`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/AssetImportProfileTests.cs`
- Create: `Tools/Assets/Pack-FrontFacingCharacterSheet.ps1`
- Create: `Tools/Assets/Test-FrontFacingCharacter.ps1`

**Interfaces:**
- Consumes: `manifest.json`, `palette.png`, and `flattened.png` under a front-facing source directory.
- Produces: `FrontFacingCharacterSheetContract.Validate(string sourceRoot, string runtimePath)`, a 12-sprite import under `Runtime/FrontFacing`, and deterministic 256 x 192 packing.

- [ ] **Step 1: Write failing contract tests**

Create fixtures with this manifest:

```json
{
  "id": "rookie-constable",
  "cellSize": [64, 64],
  "sheetSize": [256, 192],
  "footAnchor": [32, 56],
  "pivot": [0.5, 0.125],
  "pixelsPerUnit": 32,
  "view": "front",
  "directions": ["front"],
  "headHeightRatio": 0.5,
  "promptRevision": "pixellab-rookie-v1",
  "animations": [
    {"name": "idle", "start": 0, "frames": 2, "fps": 4},
    {"name": "move", "start": 2, "frames": 4, "fps": 8},
    {"name": "death", "start": 6, "frames": 6, "fps": 8}
  ]
}
```

Tests must assert:

```csharp
Assert.That(result.Errors, Is.Empty);
Assert.That(result.FrameCount, Is.EqualTo(12));
Assert.That(result.CellSize, Is.EqualTo(new Vector2Int(64, 64)));
Assert.That(result.SheetSize, Is.EqualTo(new Vector2Int(256, 192)));
Assert.That(result.HeadHeightRatio, Is.InRange(0.45f, 0.55f));
Assert.That(FrontFacingCharacterSheetContract.HasAnimationVariation(root, 2, 4), Is.True);
```

Negative cases must reject `directions` other than `["front"]`, a 38-frame manifest, an `attack` animation, an opaque background corner, semi-transparent pixels, identical move frames, a head ratio outside `0.45..0.55`, wrong dimensions, colors outside `palette.png`, and runtime/source mismatch.

- [ ] **Step 2: Run the focused tests and verify failure**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.FrontFacingCharacterSheetContractTests
```

Expected: FAIL because `FrontFacingCharacterSheetContract` does not exist.

- [ ] **Step 3: Implement the front-facing contract**

Create `FrontFacingCharacterSheetContract` with these public constants and result:

```csharp
public const int Columns = 4;
public const int Rows = 3;
public const int Frames = 12;
public static readonly Vector2Int CellSize = new(64, 64);
public static readonly Vector2Int SheetSize = new(256, 192);

public sealed record FrontFacingCharacterSheetValidationResult(
    IReadOnlyList<string> Errors,
    Vector2Int CellSize,
    Vector2Int SheetSize,
    Vector2Int FootAnchor,
    Vector2 Pivot,
    int FrameCount,
    float HeadHeightRatio);
```

Use `(frame % Columns) * 64` and `(frame / Columns) * 64` for frame origins. Require exactly the three animation records and values from Step 1. Require alpha `0` or `255`, transparent four sheet corners, exact palette membership for opaque pixels, differing move signatures, and byte-for-byte equality between `flattened.png` and the runtime PNG.

- [ ] **Step 4: Write failing 12-sprite importer tests**

Add `FrontFacingRuntimeUsesTwelveCustomPivotSlices` to `AssetImportProfileTests.cs`. Create a fixture at:

`Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/import_profile_test.png`

Assert 12 named sprites, 64 x 64 rects, point filtering, no mipmaps, 32 PPU, and pivot `(0.5, 0.125)`. Assert the existing legacy mannequin remains 38 slices.

- [ ] **Step 5: Implement the importer branch**

In `JoseonAssetPostprocessor.cs`, add:

```csharp
private const string FrontFacingCharacterRuntimeRoot =
    "Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/";
```

When `assetPath` starts with that root, assign `CharacterSprites(characterId, 12, 4)`. Keep legacy runtime sheets on `CharacterSprites(characterId, 38, 6)`. Change the helper signature to:

```csharp
private static SpriteMetaData[] CharacterSprites(
    string characterId,
    int frameCount,
    int columns)
```

- [ ] **Step 6: Add deterministic pack and preflight scripts**

`Pack-FrontFacingCharacterSheet.ps1` accepts `-IdleFrames`, `-MoveFrames`, `-DeathFrames`, and `-OutputPath`; it requires counts `2`, `4`, and `6`, checks every input is 64 x 64 RGBA, and packs frames in row-major order into 256 x 192.

`Test-FrontFacingCharacter.ps1` checks required files, dimensions, exact
12-frame metadata, the absence of token-like UUID values in provenance files,
and then invokes the focused Unity tests. Its production invocation is:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-FrontFacingCharacter.ps1 `
  -SourceRoot ArtSource/Pixel/Characters/front-facing/rookie-constable `
  -RuntimePath Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png
```

- [ ] **Step 7: Run focused and full tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.FrontFacingCharacterSheetContractTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Filter JoseonHunter.Tests.EditMode.AssetImportProfileTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1
```

Expected: all three invocations PASS; legacy 38-frame tests remain green.

- [ ] **Step 8: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Editor/AssetProduction/FrontFacingCharacterSheetContract.cs `
  Assets/JoseonHunter/Scripts/Editor/AssetProduction/FrontFacingCharacterSheetContract.cs.meta `
  Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs `
  Assets/JoseonHunter/Tests/EditMode/FrontFacingCharacterSheetContractTests.cs `
  Assets/JoseonHunter/Tests/EditMode/FrontFacingCharacterSheetContractTests.cs.meta `
  Assets/JoseonHunter/Tests/EditMode/AssetImportProfileTests.cs `
  Tools/Assets/Pack-FrontFacingCharacterSheet.ps1 `
  Tools/Assets/Test-FrontFacingCharacter.ps1
git commit -m "feat: add front-facing character sheet contract"
```

---

### Task 3: PixelLab Rookie Constable Base Pilot

**Files:**
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/base.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/palette.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/prompt.md`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/provenance.json`
- Create: `Docs/Assets/review/rookie-constable-pixellab.md`
- Create: `Docs/Assets/review/rookie-constable-pixellab-board.png`

**Interfaces:**
- Consumes: authenticated PixelLab MCP, the user-supplied screenshot as a non-production proportion reference, and the locked prompt below.
- Produces: one pending 64 x 64 transparent base PNG and a user-facing review board.

- [ ] **Step 1: Read the free balance**

Use the authenticated PixelLab balance/status tool. Record only numeric generation counts in `provenance.json`; never record the token.

- [ ] **Step 2: Save the exact generation prompt**

Write this prompt to `prompt.md`:

```text
Original front-facing Joseon folk-fantasy rookie constable for a portrait
mobile survivor game. True crisp pixel art, transparent background, exactly
64x64 canvas. Cute two-head-tall super-deformed proportions: head is about half
the standing height, very short compact torso, tiny hands and feet. Large simple
dark eyes, tiny mouth, firm 1-pixel near-black silhouette outline, deliberate
pixel clusters, no anti-aliasing. Oversized black Joseon patrol hat, navy patrol
uniform, red waist accent, small wooden hopae, compact sheathed hwando. Neutral
front-facing standing pose, centered at bottom anchor (32,56), readable at native
scale. Use the supplied image only for broad proportion, outline weight, and
pixel-density reference. Create a new character; do not reproduce any reference
character, costume, weapon, logo, text, or source pixels. No side view, no back
view, no attack pose, no background, no shadow outside the sprite.
```

- [ ] **Step 3: Generate one base sprite**

Use PixelLab's MCP image-generation tool that supports a 64 x 64 transparent output and a style/reference image. Prefer the documented style-capable Bitforge operation; use the basic Pixflux operation only if Bitforge is unavailable on the free trial.

Save the returned PNG as `base.png`. Record provider, MCP tool name, model/operation, job ID, dimensions, creation time, prompt SHA-256, output SHA-256, and generation count `1` in `provenance.json`.

- [ ] **Step 4: Run base preflight**

Verify 64 x 64 dimensions, RGBA, transparent corners, hard alpha, bottom-center bounds, and no more than 48 opaque palette colors. Generate `palette.png` from exact opaque colors.

Expected: PASS. If it fails, count the failed result against the six-generation budget and retry with the approved base prompt plus the exact failed constraint.

- [ ] **Step 5: Build the base review board**

The board shows native 64 x 64, 8x nearest-neighbor enlargement, light background, dark background, palette strip, alpha checkerboard, current generation usage, and `PENDING BASE APPROVAL`.

`rookie-constable-pixellab.md` records:

```markdown
- Gate: base sprite
- Status: pending
- PixelLab generations used: 1 of 6 pilot maximum
- Runtime imported: no
- Next action after approval: four-frame front-facing walk
```

- [ ] **Step 6: Commit and stop for user approval**

```powershell
git add ArtSource/Pixel/Characters/front-facing/rookie-constable `
  Docs/Assets/review/rookie-constable-pixellab.md `
  Docs/Assets/review/rookie-constable-pixellab-board.png
git commit -m "art: prepare PixelLab constable base review"
```

Return the board to the user. Do not generate movement until the user explicitly approves the base.

---

### Task 4: Four-Frame Front-Facing Walk Pilot

**Files:**
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/move/move-00.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/move/move-01.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/move/move-02.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/move/move-03.png`
- Modify: `ArtSource/Pixel/Characters/front-facing/rookie-constable/provenance.json`
- Modify: `Docs/Assets/review/rookie-constable-pixellab.md`
- Modify: `Docs/Assets/review/rookie-constable-pixellab-board.png`

**Interfaces:**
- Consumes: user-approved `base.png` and the same PixelLab character/reference identity.
- Produces: four coherent pending movement frames.

- [ ] **Step 1: Generate the movement animation**

Use PixelLab's text animation operation with `base.png` as the starting image, 64 x 64 output, four frames, transparent background, and:

```text
Front-facing cute walking-in-place loop. Keep the face, hat, body, clothing,
hopae, sword, palette, outline, scale, and camera exactly consistent with the
input. The character never turns. Alternate the tiny feet, sway horizontally by
only 1 to 2 pixels, and bob vertically by only 1 pixel. Loop frame 4 back to
frame 1. No attack, no arm swing that changes equipment, no camera motion.
```

- [ ] **Step 2: Validate temporal consistency**

Require all four frames to remain 64 x 64 RGBA with hard alpha. Reject more than one-pixel eye drift, more than two-pixel hat-bound drift, palette colors outside the base palette tolerance, identical frame hashes, or a silhouette center shift above two pixels.

- [ ] **Step 3: Correct within the pilot budget**

If Step 2 fails, use PixelLab's animation edit operation with the approved base as the reference and the exact failed metric. Stop when total base-plus-walk generations reaches six.

- [ ] **Step 4: Update the review board**

Add the four native/enlarged frames, a looping GIF or numbered strip, light/dark backgrounds, frame-difference metrics, generations used, and `PENDING MOVEMENT APPROVAL`.

- [ ] **Step 5: Commit and stop for user approval**

```powershell
git add ArtSource/Pixel/Characters/front-facing/rookie-constable `
  Docs/Assets/review/rookie-constable-pixellab.md `
  Docs/Assets/review/rookie-constable-pixellab-board.png
git commit -m "art: prepare PixelLab constable walk review"
```

Do not generate idle or death until the user explicitly approves the walk.

---

### Task 5: Complete and Validate the 12-Frame Pilot Sheet

**Files:**
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/idle/idle-00.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/idle/idle-01.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/death/death-00.png` through `death-05.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/flattened.png`
- Create: `ArtSource/Pixel/Characters/front-facing/rookie-constable/manifest.json`
- Create: `Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png`
- Modify: `Docs/Assets/review/rookie-constable-pixellab-board.png`
- Modify: `Docs/Assets/review/rookie-constable-pixellab.md`
- Modify: `Docs/Assets/production-asset-manifest.json`
- Modify: `Docs/Assets/asset-rights-ledger.csv`

**Interfaces:**
- Consumes: approved base and move frames.
- Produces: one pending, validated Unity-ready 12-frame sheet.

- [ ] **Step 1: Create the two idle frames**

Use the approved base for `idle-00.png`. Create `idle-01.png` by changing only the eye pixels into a blink and moving no silhouette pixel.

- [ ] **Step 2: Generate six death frames**

Use PixelLab animation from the approved base:

```text
Six-frame front-facing defeat animation. Preserve the exact character identity,
palette, equipment, pixel outline, and camera. The character squashes downward
and settles on the ground; do not rotate to a side or back view. Frame 1 starts
standing and frame 6 is a compact collapsed front-facing pose. Transparent
background, no particles, no text.
```

If the free budget is exhausted, create death frames by pixel-preserving vertical squash, downward translation, and one-pixel cleanup from the approved base; do not invent a different character drawing.

- [ ] **Step 3: Pack the sheet**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Pack-FrontFacingCharacterSheet.ps1 `
  -IdleFrames ArtSource/Pixel/Characters/front-facing/rookie-constable/idle/*.png `
  -MoveFrames ArtSource/Pixel/Characters/front-facing/rookie-constable/move/*.png `
  -DeathFrames ArtSource/Pixel/Characters/front-facing/rookie-constable/death/*.png `
  -OutputPath ArtSource/Pixel/Characters/front-facing/rookie-constable/flattened.png
```

Copy the approved packed bytes to `Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png`.

- [ ] **Step 4: Write manifest and provenance**

Use the exact manifest from Task 2 with `promptRevision` set to `pixellab-rookie-v1`. Update provenance with all job IDs, hashes, tool names, generation counts, and the fallback flag; include no token.

- [ ] **Step 5: Update production records as pending**

Add `hero_rookie_constable_front_facing_runtime` with the actual runtime hash:

```powershell
$manifestPath = 'Docs/Assets/production-asset-manifest.json'
$runtimePath = 'Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png'
$document = Get-Content -Raw $manifestPath | ConvertFrom-Json
$entry = [pscustomobject][ordered]@{
  id = 'hero_rookie_constable_front_facing_runtime'
  batch = 'characters'
  kind = 'sprite_sheet'
  sourcePath = 'ArtSource/Pixel/Characters/front-facing/rookie-constable/flattened.png'
  runtimePath = $runtimePath
  width = 256
  height = 192
  frameCount = 12
  pivotX = 0.5
  pivotY = 0.125
  pixelsPerUnit = 32
  sha256 = (Get-FileHash -Algorithm SHA256 $runtimePath).Hash.ToLowerInvariant()
  licenseStatus = 'approved'
  approvalStatus = 'pending'
  promptRevision = 'pixellab-rookie-v1'
}
$document.assets += $entry
$document | ConvertTo-Json -Depth 6 | Set-Content -Encoding utf8 $manifestPath
```

Add PixelLab commercial-generation terms and the provenance location to the
rights ledger.

- [ ] **Step 6: Run preflight and Unity tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-FrontFacingCharacter.ps1 `
  -SourceRoot ArtSource/Pixel/Characters/front-facing/rookie-constable `
  -RuntimePath Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1
```

Expected: PASS with 12 runtime slices and no secret scan findings.

- [ ] **Step 7: Commit and stop for final sheet approval**

```powershell
git add ArtSource/Pixel/Characters/front-facing/rookie-constable `
  Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing `
  Docs/Assets/review/rookie-constable-pixellab.md `
  Docs/Assets/review/rookie-constable-pixellab-board.png `
  Docs/Assets/production-asset-manifest.json `
  Docs/Assets/asset-rights-ledger.csv
git commit -m "art: prepare front-facing constable sheet review"
```

---

### Task 6: Approve the Pilot and Make It the Master Style Reference

**Files:**
- Modify: `ArtSource/Pixel/Characters/front-facing/rookie-constable/manifest.json`
- Modify: `Docs/Assets/review/rookie-constable-pixellab.md`
- Modify: `Docs/Assets/production-asset-manifest.json`
- Create: `Docs/Assets/pixellab-master-style.md`

**Interfaces:**
- Consumes: explicit user approval of the full 12-frame review board.
- Produces: approved rookie constable and a reusable PixelLab style contract for later heroes and enemies.

- [ ] **Step 1: Record approval**

Set only the new front-facing runtime entry to `approvalStatus: "approved"`. Keep legacy rejected character entries `pending`.

- [ ] **Step 2: Write the master style contract**

Record the approved base SHA-256, palette SHA-256, prompt revision, two-head ratio, outline rule, front-facing movement rule, and the instruction to reuse the approved base as PixelLab's style reference. Include no token and no copy of the SPUM screenshot.

- [ ] **Step 3: Re-run validation**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File Tools/Assets/Test-FrontFacingCharacter.ps1 `
  -SourceRoot ArtSource/Pixel/Characters/front-facing/rookie-constable `
  -RuntimePath Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1
git diff --check
```

Expected: all PASS and no whitespace errors.

- [ ] **Step 4: Commit**

```powershell
git add ArtSource/Pixel/Characters/front-facing/rookie-constable/manifest.json `
  Docs/Assets/review/rookie-constable-pixellab.md `
  Docs/Assets/production-asset-manifest.json `
  Docs/Assets/pixellab-master-style.md
git commit -m "art: approve PixelLab constable master style"
```

- [ ] **Step 5: Return to the asset-batch plan**

Use the approved master style for shaman, mountain hunter, and the subsequent enemy batch. Preserve one explicit visual approval gate per batch.
