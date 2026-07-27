# Task 3 Report

- Added `WeaponLevelData`, `WeaponDefinitionAsset`, and `WeaponCatalogAsset` contracts.
- Catalog validation requires the exact eight-ID roster, five valid levels per definition, unique IDs, and distinct targeting/geometry/contact/repeat-policy definitions.
- Added eight-definition dummy factory coverage and level ownership validation.
- Migrated `UpgradeSelector` and its rule coverage to `WeaponRoster.All`; hwando evolution now requires `hwando_flying_blade`.
- No production `.asset` instances or balance values were created; those remain deferred to Task 14.

## Validation

- `git diff --check` on Task 3 paths: passed.
- Unity test launch intentionally deferred at the parent agent's request. A prior targeted batch launch overlapped an existing Editor process and produced a stale result before compiling the RED test; no GREEN Unity result is claimed.
