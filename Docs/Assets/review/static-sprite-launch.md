# Static Sprite Launch Review

## PENDING BATCH APPROVAL

The consolidated board contains all twelve pending launch sources: three
heroes, five normal enemies, one boss, and three pickups. Every asset is shown
at literal native `64×64` size and as an exact `8×` nearest-neighbor (`512×512`)
check on both light and dark grounds. Runtime copies, production manifest
records, prefabs, and scene bindings are intentionally absent until explicit
batch approval.

## Source preflight

All twelve entries pass the static-source contract: `64×64` RGBA, hard alpha,
transparent corners, at most 48 opaque colors, centered silhouette, and the
top-origin maximum opaque `y=56` foot anchor. New enemy/boss sources passed
direct per-asset validation; the full twelve-entry preflight passed afterward.

## Generation accounting

The authoritative trial balance before Task 3 was `32 used / 8 remaining`.
Task 3 used exactly six sequential one-generation PixelLab Pixen operations,
one for each enemy/boss candidate, and did not call Pixflux. The resulting
balance is `38 used / 2 remaining`. The board's bottom table preserves each
asset's selected provider and cost; detailed non-secret job identifiers are
stored with the six new sources.

The older pre-ruling Pixflux timeout remains documented in the pickup attempt
records. Its UUID was removed from free-text chronology only because the source
contract deliberately rejects UUID-like values outside `jobId` fields; the
attempt record still retains the actual job identifier.

## Board inspection

`static-sprite-launch-board.png` is `4200×4850` pixels. It was inspected at
full width and overview scale: role rows, card labels, native sprites, light /
dark checks, and the dedicated non-overlapping source-preflight/accounting
footer are present without clipping. The board is ready for visual review.
