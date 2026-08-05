# Stage One Fifteen-Minute Boss and XP Verification

## Verified scope

- Production Gameplay duration is 900 seconds.
- Wave roster windows, packs, role announcements, special introductions, two midboss milestones, final warning, and final boss all use the same canonical time coordinate.
- Maximum player level is 35 with a monotonic experience curve and an explicit cap guard.
- First and second midboss silhouettes use 1.7x and 1.9x normal scale; Fallen General uses 2.3x normal scale. Contact radii follow the same multipliers.
- Boss combat uses deterministic telegraph, execute, and recovery states. Runtime presentation supports suppression circles, charge corridors, and spirit-volley rings without white outlines.
- Active experience pickups are capped at 180. Overflow merges into the nearest active XP pickup and preserves total value.
- XP tiers use cyan, violet, and magenta presentation. Attraction radius remains 0.58 world units.
- Pickup objects and XP trails are reused; trail components are no longer added at attraction time. Magnet attraction begins at most 24 XP pickups per frame.

## Automated evidence

- Full EditMode: 665 total, 665 passed, 0 failed, 0 skipped.
- Full PlayMode: 243 total, 243 passed, 0 failed, 0 skipped.
- Focused suites passed for:
  - `RunRuleTests`
  - `WaveSpawnDirectorTests`
  - `CombatRuleTests`
  - `RunLoadoutRulesTests`
  - `BossAttackPatternTests`
  - `BossScaleProfileTests`
  - `ExperiencePickupBudgetTests`
  - `StagePacingPlayModeTests`
  - `WaveRosterPlayModeTests`
  - `FirstPlayablePickupRangePlayModeTests`
  - the maximum-level `FirstPlayableUiStatePlayModeTests` case
- Android ARM64 IL2CPP development build completed successfully.
- APK: `Builds/Android/JoseonHunter-development.apk`, 173,409,550 bytes (165.38 MiB), generated 2026-08-05 22:25 KST.
- Unity jobs ran sequentially at BelowNormal priority with processor affinity mask `15`.

## Observed and remaining limits

- The Unity MCP relay and official AI Assistant package are installed locally, but Unity MCP tools were not attached to this Codex task. Validation therefore used batch EditMode, PlayMode, and Android build workflows.
- No physical Android device profiling was performed. The 140-enemy and 180-pickup hard budgets are covered by deterministic rules and automated tests, but thermal behavior, GPU overdraw, and device frame pacing still require a device pass.
- No new PixelLab asset was generated. The warnings use low-color runtime geometry because it is sharper and cheaper than scaling a bitmap; PixelLab remains optional only if device capture shows the small spirit projectile needs a bespoke sprite.
- A live visual review of the new boss warning palette and exact on-device apparent size remains recommended after Unity MCP is connected or the project is opened interactively.

