# Combat Information and Audio Settings Implementation Plan

1. Add failing EditMode tests for run weapon damage/kill attribution and save-data audio migration.
2. Extend `RunWeaponKillLedger`, `FirstPlayableController`, and `WeaponSlotView` with run damage and kill totals.
3. Add failing PlayMode assertions for per-affix grade rows and weapon combat statistics, then update the detail layout.
4. Add failing PlayMode/EditMode audio-setting tests, extend persistent save DTOs, and expose a settings mutation through `MetaGameSession`.
5. Add master-volume controls to the existing music and pooled SFX directors.
6. Implement the shared audio settings UI in the pause panel and a new lobby gear modal.
7. Add failing HUD tests for visual ratios and elapsed `mm:ss`, then replace sprite-less filled images with anchored-width fills.
8. Run focused tests, full EditMode, full PlayMode, inspect the final diff, commit only task files, and push `master`.
