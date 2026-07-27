# Static Sprite Launch Review

## PENDING BATCH APPROVAL

The consolidated board contains all twelve pending launch sources: three
heroes, five normal enemies, one boss, and three pickups. Every asset is shown
at literal native `64x64` size and as an exact `8x` nearest-neighbor (`512x512`)
check on both light and dark grounds. Runtime copies, production manifest
records, prefabs, and scene bindings are intentionally absent until explicit
batch approval.

## Source preflight

All twelve entries pass the static-source contract: `64x64` RGBA, hard alpha,
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

## Fallen general fix round 1

No generation was used. The existing normalized sprite received a targeted,
deterministic pixel edit only: 23 exposed warm face pixels were changed from
`#E9B6A3` to cool desaturated `#B2AEB0`, and three visible yellow eye pixels at
`(23,12)`, `(24,12)`, and `(29,12)` were changed from `#F2CC5B` to high-contrast
pale-cream `#F4F1EF`. This preserves the silhouette, armor, weapon, hard alpha,
approved palette, center, anchor, and color-count constraints. At native and
8x board inspection, the face reads cool undead and the eyes read as pale glow.

## Board inspection

`static-sprite-launch-board.png` is `4200x4850` pixels. It was inspected at
full width and overview scale: role rows, card labels, native sprites, light /
dark checks, and the dedicated non-overlapping source-preflight/accounting
footer are present without clipping. Visible technical labels use ASCII-only
`64x64`, `8x LIGHT CHECK`, `8x DARK CHECK`, and hyphen separators. The board is
ready for visual review.
