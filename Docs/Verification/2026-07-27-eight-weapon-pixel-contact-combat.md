# Eight-weapon pixel-contact combat verification

Date: 2026-07-27
Unity: 6000.5.5f1
Branch: `agent/eight-weapon-pixel-contact`

## Verified in this checkpoint

- Unity compilation completed for runtime and test assemblies.
- `WeaponMechanicTests`: 33 total, 33 passed, 0 failed.
- `EightWeaponCombatPlayModeTests`: 1 total, 1 passed, 0 failed.
- Full EditMode regression: 273 total, 270 passed, 3 failed.
- The three full-suite failures are the same pre-existing Gameplay scene scaffold/static-proof failures recorded before this feature:
  - `SceneScaffoldTests.EachFoundationSceneHasOnlyTheSceneRoot(Gameplay)`
  - `SceneScaffoldTests.GameplaySceneRootContainsWorldAndUi`
  - `StaticSpriteContentTests.GameplaySceneContainsInactiveStaticSpriteLaunchProofLineup`
- PixelLab ledger reconciles to 30 generations used and 1,970 remaining.

## Corrections found by fresh Unity validation

- Read sprite sub-rect pixels through the Unity 6-compatible full texture buffer.
- Avoided Unity fake-null component handling when creating pooled projectile renderers.
- Assigned controller-owned monotonic combat target IDs instead of the obsolete `GetInstanceID`.
- Confirmed direct contact after a talisman transfers to a replacement target.
- Centered stretched Jangseung boundary masks on a real pixel row for both odd and even heights.
- Gated Thunder Bomb ring contact to the frame in which the expanding radius reaches the target center, then retained pixel-mask confirmation.

## Intentionally deferred

Per the implementation-first request, the following broader presentation work is deferred:

- 80-enemy stress/balance capture.
- Per-weapon long-running PlayMode fire/hit/damage-number evidence.
- Runtime before-contact/first-hit screenshot board.

These are presentation and stress verification follow-ups, not compilation blockers.
