# Combat Information and Audio Settings Verification

## Scope

- Per-weapon run damage and attributed kill totals
- One-affix-per-line weapon detail presentation with Korean grade labels
- Persistent split music and sound-effect volume controls in pause and lobby
- Width-driven health and experience bars
- Elapsed run clock in `경과 mm:ss` format

## Automated evidence

- Full EditMode: 898 passed, 0 failed
- Full PlayMode: 296 passed, 0 failed
- Save coverage includes legacy single-volume migration, split-volume round trip, and atomic clamped settings mutation.
- UI coverage includes weapon statistics/affix rows, pause sliders, lobby gear modal, HUD ratios, and elapsed clock.

Both Unity test runs used `BelowNormal` priority and four-core processor affinity.

## Remaining device checks

- Confirm slider drag comfort and gear-button hit target on a physical Android device.
- Confirm music/effect balance through phone speakers and headphones.
- Visually inspect five-affix weapon details at the smallest supported portrait resolution.
