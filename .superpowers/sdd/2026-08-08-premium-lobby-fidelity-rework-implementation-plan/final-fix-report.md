# Premium lobby final fix wave report

## Completed code and test fixes

- Restored `thin_outer_frame` on all active lobby content panels without restoring an oversized architectural rail.
- Added the thin-content-frame PlayMode contract and exact pause primary/secondary action sprite assertions.
- Added deterministic primary/secondary action highlight, pressed, selected, and disabled `ColorBlock` behaviour with a direct PlayMode assertion.
- Removed obsolete first-pass difficulty anchors that were immediately overwritten by the approved anchor table.
- Restored the patrol production-resolution test's screen resolution in a `finally` block.
- Added an EditMode source-alpha rendered-bounds contract for the four padded controls plus `thin_outer_frame`.

## TDD evidence

- `Artifacts/fidelity-final-fix-red.xml`: 14/18 passed, 4 failed as expected for padded `difficulty_selected`, `difficulty_idle`, `primary_red_button`, and `secondary_dark_button`.
- `Artifacts/fidelity-content-frame-red.xml`: 8/9 passed, 1 failed as expected because the patrol content panel had no `thin_outer_frame`.
- `Artifacts/fidelity-final-fix-code-green.xml`: 12/12 passed after the code fixes (`LobbyPatrolPlayModeTests`, `PremiumPauseUiPlayModeTests`, `JoseonButtonSkinPlayModeTests`).

## Blocking PixelLab evidence

PixelLab was used exclusively for attempted bitmap regeneration. Both inspected output batches were rejected; no generated image was copied into production:

- Batch 1 IDs: `7d21d5ca-c9ca-4823-b8fc-7164d45b7628`, `32571766-da1e-49dd-9fd2-910bdc9602aa`, `4c31c663-b47c-41c4-9294-6439220d9e7b`, `8bd40e86-823b-4b21-9112-8e2501fd4251`, `b90570d1-3956-4322-9ca5-7f1a59546a29`, `37e8b103-38e7-4f0f-8c44-c1453edfef38`. Rejected for large transparent margins; `thin_outer_frame` also had non-thin ornate/textured treatment and `tab_idle` had non-plain treatment.
- Batch 2 full-canvas-piece IDs: `f357f8bd-39da-4b46-b23b-35e6437a4eb1`, `d3fff0f2-467d-442f-80e7-25aea9eff0a6`, `91514c71-600c-4899-bda3-c795296d07e6`, `577398f3-516a-4318-8a4c-a6a27c90cc38`, `b00bbd07-3f27-4b89-a24d-3a5901c2f5d4`, `38f9729b-2165-4b85-a0ba-267af43f77b0`. Although their alpha bounds fill their canvases, original-resolution inspection showed forbidden output: menu text/buildings for selected/idle, a multi-control sprite sheet for primary, transparent-margin controls for secondary/thin, and transparent-margin tab artwork. They cannot be accepted under the final-review constraints.

Because no compliant PixelLab bitmap is available and cropping/drawing with a different raster tool is prohibited, the required asset replacement, production rebuild, full suites, and capture acceptance were not run. This report records the actual stopping point rather than claiming final acceptance.
