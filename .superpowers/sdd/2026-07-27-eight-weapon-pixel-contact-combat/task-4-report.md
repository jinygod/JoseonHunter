# Task 4 Report

- Added immutable packed `PixelHitMask` data with dimensions, pivot pixels, PPU, texture conversion, and deterministic row fixtures.
- Added stable row-major pixel contact with flip, integer quarter-turn fast paths, and nearest-neighbor inverse sampling for other rotations.
- Added `WeaponPixelMaskCatalog`; it validates the weapon catalog on load and converts every distinct source `Texture2D` to a runtime-owned mask only once.
- Added editor preflight for binary alpha, PPU, uncompressed/readable point-filtered non-mipmapped textures, dimensional parity, and mask containment. The importer derives a mask from opaque source alpha and applies an exclusion PNG.
- Added focused EditMode coverage for contact transformations, mask immutability, alpha/containment validation, and exclusions.

## Validation

- Focused `git diff --check` for Task 4 paths: passed.
- Unity test launch intentionally skipped: two pre-existing Unity Editor processes are active, and Task direction prohibits a new launch.

## Review fix round 1

- Validation now accepts distinct source and binary-mask importers and applies the PPU, compression, mipmap, filtering, and readability contract to both.
- Pixel reads are guarded by both importer and loaded texture readability, so invalid unreadable assets return contract errors without `GetPixels32`.
