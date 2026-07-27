- Gate: movement review
- Status: PENDING MOVEMENT APPROVAL
- PixelLab generations used: 3 of 6 pilot maximum
- Runtime imported: no
- Next action after approval: idle/death production review; do not import runtime assets before the complete approval gate.

Preflight: PASS — normalized 64 × 64 RGBA PNG, transparent corners, hard alpha, opaque foot-contact row exactly y=56, horizontally centered opaque bounds (11,3)-(52,56), and 38 opaque colors (maximum 48). The original provider output is preserved as `base-pixellab-raw.png`; `base.png` is the deterministic nearest-neighbor normalized review candidate.

The first PixelLab base result was rejected because it contained two characters. The selected second result remains the approved normalized production reference.

## Front-facing walk (pending approval)

`move/move-00.png` through `move/move-03.png` are a four-frame front-facing walking-in-place loop generated from approved `base.png` with PixelLab `animate_image` job `054aca37-6359-4a13-bca7-e3fbc3065bc6` (one generation). The review board shows each native frame, nearest-neighbor enlarged light presentation, numbered strip on a dark background, and temporal metrics.

Deterministic post-processing translated each complete frame vertically to the approved foot anchor (opaque bottom y=56) and mapped opaque colors to the exact approved-base palette. It did not redraw, add, or otherwise invent pixels. Validation passed: all frames are 64 x 64 `Format32bppArgb`, hard-alpha, transparent-corner PNGs; 37-38 opaque colors; bounds `(11,0..2)-(52,56)`; silhouette center x `31.5` for every frame; four distinct SHA-256 hashes; and no colors outside the base palette. Hat-top span is two pixels, within the two-pixel limit; visual eye registration is within one pixel across the ordered strip.

An attempted reference-edit job `585c01c7-233d-4c9b-9f98-7481904946d0` quoted approximately 20 generations and was abandoned without polling, downloading, or using results. The authoritative balance remained 3 used / 37 remaining, so it is not counted as consumed.
