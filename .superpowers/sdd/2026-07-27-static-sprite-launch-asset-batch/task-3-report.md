## Status

Complete and stopped before runtime import. Six new pending source directories
were created for five normal enemies and the fallen-general boss; the manifest,
consolidated 12-asset review board, and review notes are ready for explicit
batch approval. No runtime files, production manifest records, prefabs, or
scene bindings were created.

## Operations and authoritative accounting

Before: `32 used / 8 remaining`. Exactly six sequential PixelLab Pixen
operations, each `64×64`, transparent, and quoted at one generation:

| Asset | Pixen job ID | Cost | Visual identity inspection |
| --- | --- | --- | --- |
| plague_rat | `27135cd5-687b-4ad2-9d7d-3cd1e368520f` | 1 | Hunched grey-brown rat; oversized ears, teeth, olive tag, and four feet readable. |
| vengeful_spirit | `4266ece7-8679-4b9e-b2af-6afd253675fe` | 1 | Pale blue-white floating spirit; dark hair, angry eyes, burial clothing, trailing lower body. |
| sakkat_specter | `be34969c-cf14-48af-9944-ac44317b7159` | 1 | Oversized straw sakkat, shadowed face, dark robe, forward yellow charm. |
| dokkaebi | `4a397c8c-00bd-42d4-a63f-da2b1ce656ee` | 1 | Compact teal-blue horned dokkaebi with cheeky hostile face, waistcloth, and low club. |
| bandit | `81a1d9c6-5ad2-4def-ad47-244e5b0f08a7` | 1 | Joseon bandit with dark mask, red headband, patched brown clothes, and low short blade. |
| fallen_general | `058da3f5-098d-43c7-b4e6-0c7ee1849a9e` | 1 | Larger armored undead commander with red sash, crested helmet, pale eyes, and broken polearm. |

After: `38 used / 2 remaining`. No Pixflux call was made. No retry was made.
No fallback was needed: each single candidate retained its intended subject
identity. Every result was normalized only by hard-alpha thresholding at 128,
nearest-color mapping to the approved rookie-constable palette, and integer
translation to bounds center `x=32` and top-origin opaque maximum `y=56`.
Raw provider output, exact prompt, selected palette, hashes, and non-secret
provenance are preserved per source.

## Validation

New-source direct validation passed for all six assets:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-StaticSpriteAssetValidation.ps1 -AssetId <id> -SourceDirectory ArtSource/Pixel/StaticSprites/<id>
```

Result: `PASS` for plague_rat, vengeful_spirit, sakkat_specter, dokkaebi,
bandit, and fallen_general. The six Task 2 sources had already passed their
direct route (6/6).

The required full preflight initially exposed three Task 2 provenance strings:
the contract permits UUIDs only in `jobId` fields, while pickup chronology text
repeated the old Pixflux UUID. The chronology wording now refers to the
already-recorded attempt, preserving the UUID in the corresponding `jobId`
record. Re-run result:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-StaticSpriteBatch.ps1 -ManifestPath ArtSource/Pixel/StaticSprites/static-sprite-batch.json -SourceRoot ArtSource/Pixel/StaticSprites -RuntimeRoot ArtSource/Pixel/StaticSprites
```

`Logs/static-sprite-batch-preflight.log`: `Static sprite batch preflight passed.`
The explicit existing `-RuntimeRoot` argument avoids the helper's PowerShell
empty-argument limitation when runtime validation is not requested; no runtime
content is required or consumed.

## Consolidated review board

`Docs/Assets/review/static-sprite-launch-board.png` is `4200×4850` pixels.
It has labeled Heroes, Normal Enemies, Boss, and Pickups rows; every card has a
literal native `64×64` rendering plus separate exact `8×` (`512×512`)
nearest-neighbor light and dark panels. A dedicated footer records all twelve
selected source/provider/cost lines and the authoritative accounting, without
overlapping image cards. Inspection at full width and overview scale found no
clipping or overlap. Board label: `PENDING BATCH APPROVAL`.

## Self-review and concerns

The six candidates are visually distinct at native and enlarged scale, use the
approved outline/palette direction, and meet the source contract. The boss
uses visibly more of its canvas than normal enemies. The board intentionally
does not imply approval; user visual approval remains the only next gate.

Concern: `Test-StaticSpriteBatch.ps1` cannot pass an empty optional
`RuntimeRoot` through `Start-Process` in this environment. This did not affect
source-only validation: supplying the existing source root lets the same helper
run its no-runtime route and the contract reported the explicit full-batch PASS.

## Commit

Pending at report write time; Task 3 source, board, review, and report files
are to be committed as `art: prepare static launch batch review`.
