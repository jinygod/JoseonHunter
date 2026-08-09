# Task 5 — Authored Training Page Binding

## Implemented

- Added `LobbyTrainingRowView` and `TrainingPageView` with explicit serialized row, icon, effect, capacity, action, and feedback bindings.
- Added the `TrainingRow` lobby module prefab through `LobbyModulePrefabBuilder`.
- Added strict `CommonTrainingPresenter.InitializeAuthored` validation and owned `UnityAction` teardown/rebinding. Existing external button listeners are preserved.
- Retained the explicitly documented legacy `Initialize`/`Build` adapter for the pre-Task-7 Lobby shell only. It does not make the authored page path permissive.
- Kept the established training capacity, cost, purchase, maximum-rank, refund, and Korean preview-copy semantics.

## Validation

- RED: the initial authored-row assertion failed against the old grid-card shell.
- GREEN: `CommonTrainingLobbyPlayModeTests` passed 7/7.
- Regression: `CommonTrainingProgressionTests` and `AccountProgressionTests` passed 33/33 in EditMode.

## Self-review

- Reviewed only Task 5 files; no changes were made to `Lobby.unity`, `LobbyShell.prefab`, `LobbyBootstrap`, `LobbySceneBuilder`, `PremiumPixelUiSkin`, or `LockSlashConstraint`.
- No `Resources.Load` use was introduced.
- The exact six icon slots are serialized on `TrainingPageView` in required enum order. Task 7 owns their authored Lobby scene-instance assignment when it replaces the transitional shell.
- `git diff --check` is required before commit.

## Review fix round 1

- `TrainingIconSet.asset` now owns the exact six Task 3 sprite references in enum order.
- Duplicate authored rows/buttons are rejected before listener unbinding; the focused contract preserves prior listener behavior and row identity.
