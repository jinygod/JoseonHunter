# Weapon Affix Micro-Slot Polish Design

## Goal

Replace the current stretched ornament flash with a readable, short three-line micro-slot reveal that plays only after acquiring or upgrading a weapon. Ordinary support upgrades and evolutions keep their existing fast reward presentation.

## Problem

`WeaponAffixRevealPresenter` displays the final roll immediately, fades the entire root in and out, and only adds a small sine scale for rare results. The imported `reel_frame.png` is a tiny circular ornament rather than a slot frame, while the other slot-part images are unrelated small decorations. Enlarging them across a 760 x 360 panel produces the observed blurry flash and never communicates spinning, stopping, or a jackpot.

## Experience Contract

- The affix result is rolled exactly once by progression code before presentation begins.
- Presentation never rerolls, replaces, or mutates the result.
- Only weapon acquisition and weapon level-up trigger the slot reveal.
- Standard results finish in about `0.86s`.
- High and Perfect results finish in about `1.08s` and `1.28s`.
- One-, two-, and three-line potential jackpots finish in about `1.38s`, `1.66s`, and `1.96s`.
- The panel becomes opaque during the opening and remains readable until the closing phase.
- A skip request accelerates the current reveal but preserves a minimum stop/read window and remains idempotent.
- All clocks use unscaled time because upgrade choice pauses gameplay.

## Visual Design

The panel is a compact Joseon occult mechanism made from dark iron, warm brass, jade light, and a restrained red talisman accent. It contains one primary affix window and three smaller potential windows. Pixel art is imported point-filtered and uncompressed; each PNG contains one asset with transparent background and no pseudo-text.

Required replacement assets:

1. `slot_machine_shell.png` - the complete empty machine body and outer frame.
2. `reel_window.png` - a reusable inset window behind each rolling symbol.
3. `locked_potential_slot.png` - the inactive potential line seal.
4. `reel_symbol_stat.png` - neutral stat symbol used while spinning.
5. `reel_symbol_rarity.png` - rare-result anticipation symbol.
6. `reel_symbol_potential.png` - potential/jackpot anticipation symbol.
7. `reel_stop_flash.png` - short stop-impact highlight.
8. `jackpot_burst_1.png`, `jackpot_burst_2.png`, `jackpot_burst_3.png` - escalating backgrounds for opened potential line counts.

Existing affix rarity and weapon-specific potential sprites remain the final result symbols. This avoids generating 24 redundant icons and keeps the result tied to its actual weapon mechanic.

## Motion Design

The reveal is a phase machine:

1. **Open** - `0.10s`: panel scales from `0.94` to `1.0`, background dims, alpha eases to one.
2. **Spin** - `0.30-0.48s`: anticipation symbols cycle vertically inside clipped windows. No final result is visible.
3. **Stop** - `0.12s` per awarded row: the affix stops first; awarded potential rows unlock in order with a small overshoot and stop flash. Locked rows remain visibly sealed.
4. **Read** - `0.20-0.34s`: final title and values remain steady. High/Perfect uses one restrained pulse; potential results add a larger burst based on line count.
5. **Close** - `0.12s`: panel moves down a few pixels and fades smoothly. The result never disappears during the primary read window.

Screen shake is limited to potential jackpots and uses small UI-local displacement rather than moving the gameplay camera. Standard results have no tension pulse.

## Layout and Mobile Readability

The reveal is centered within the safe area and uses a maximum width based on the canvas, rather than stretching a tiny sprite. The primary affix occupies the top window; the three potential cells form a single row below it. Text is rendered by TMP outside generated images so Korean and numbers stay crisp. At 1080 x 1920, the panel should occupy roughly 70-78% of safe-area width and no more than 24% of height.

## Code Structure

- `WeaponAffixRevealTimeline` is a pure timing/value helper that exposes duration, phase boundaries, and skip cap.
- `WeaponAffixRevealPresenter` owns runtime UI, phase progression, symbol cycling, final locking, and completion.
- `WeaponAffixPresentationCatalogAsset` exposes the new individual slot sprites.
- `WeaponAffixPixelAssetImporter` imports each file as a point-filtered single sprite and updates the Resources catalog.
- Existing controller sequencing and support/evolution routing remain unchanged.

## Verification

- EditMode tests cover exact duration/phase boundaries and skip caps.
- PlayMode tests prove final symbols are hidden during spin, rows stop in order, standard results do not pulse, jackpot results do, skip is idempotent, and completion returns the original result instance.
- Unity console must contain no new errors after asset import and script compilation.
- A 1080 x 1920 Game view capture verifies safe-area placement, readable final state, and absence of stretched sprites.

