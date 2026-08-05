# Upgrade UI, Weapon Identity, and Stage Difficulty Design

## Goal

Make upgrade selection return to combat quickly, make weapon information understandable at a glance, and restore meaningful weapon and enemy identities across the fifteen-minute first stage.

The work covers eight connected concerns:

- support upgrades should apply without a redundant confirmation modal;
- weapon appraisal and read-only details should list real accumulated effects instead of vague progression labels;
- the combat weapon rack should communicate weapon level and affix quality;
- Frost Flask should be a control and damage-over-time weapon rather than a burst nuke;
- Gakgung should feel like a deliberate long-range sniper weapon;
- normal enemies should stop remaining one-shot targets throughout the stage;
- weapon-detail text must not wrap into neighboring controls;
- the existing run loadout limit must remain explicit and stable.

## Confirmed Current Causes

### Support confirmation flow

`FirstPlayableController.CompleteUpgrade` sends every upgrade into `AugmentResult`. `FirstPlayableUiBootstrap` then routes support rewards through `RewardRevealPresenter`, whose coroutine waits for the explicit `확인` button. The support stat is already applied before this modal appears, so the modal adds no decision or information that affects gameplay.

### Appraisal and detail ambiguity

`WeaponAffixRevealPresenter` hardcodes three lower rows as `선택한 성장`, `현재 상태`, and `다음 강화`. These rows describe the legacy-path state machine, but they do not explain the actual modifiers currently affecting the weapon. The presenter already receives the accumulated affix summary and potential IDs, so the useful data exists but is not organized for players.

### Detail text overlap

The affix value uses a 600 x 62 text box at font size 42. A combined modifier string can wrap to a second line, while the accumulated summary and the first lower row occupy nearby fixed Y positions. The overlapping fixed rectangles cause the broken text shown in the report.

### Combat weapon slot readability

`WeaponRackPresenter` already loads `compact_weapon_slot`, but the 112-pixel slot is darkened with a level tint and has no explicit level marks. At the distant portrait camera scale the border blends into the field, leaving the weapon icon visually isolated. Affix quality is not represented at all.

### Frost Flask damage and presentation

Frost Flask currently deals 100% base damage on landing and 50% base damage every 0.25 seconds for 1.4-2.2 seconds. Level-five spikes add more burst damage. Its persistent field also displays `frost_growth` crystal sprites scaled to the full gameplay radius, producing the oversized ice formation. Both mechanics contradict its intended control role.

### Gakgung and enemy durability

Gakgung currently deals `15 / 19 / 24 / 30 / 38` damage at `0.72-0.60` second cooldowns. It fires frequently but does not deliver the large single hit expected from a sniper weapon.

Normal enemy base health still interpolates from 18 to 42, but the interpolation denominator was extended from the old three-minute run to the new fifteen-minute run. At three minutes the resulting base health is only 22.8 instead of 42. Weapons therefore outgrow most normal enemies very early.

## Approved Design

### 1. Immediate support application

- Choosing a support upgrade applies the stat immediately, closes the choice presentation, and returns to gameplay.
- Support rewards do not open `RewardRevealPresenter` and do not require a confirmation click.
- The existing one-second grace interval between queued level-ups remains intact. A magnet pickup cannot reopen the next queued choice before that playable interval.
- Weapon acquisition, weapon level-up appraisal, weapon legacy selection, weapon replacement, and evolution presentation retain their existing modal ownership.
- The controller remains the source of truth for the applied support level; the presentation layer only changes sequencing.

### 2. Weapon appraisal information hierarchy

The appraisal result keeps the existing value roll, grade reveal, and confirmation because it creates tension and communicates the random result. The three generic legacy rows are removed.

Below the newly rolled affix, one section titled `적용 후 누적 효과` presents concrete information:

1. `추가옵션` - each aggregated modifier on its own line, such as `피해량 +22%` and `재사용 대기시간 -5%`;
2. `성장 방식` - the selected legacy name and its current stage in plain Korean, or `선택 전`;
3. `잠재 능력` - the Korean names of all unlocked potentials, or `없음`.

The read-only weapon detail opened from the combat rack uses the same section, but its heading is `현재 적용 효과` and it does not replay the appraisal animation.

The current roll remains visually dominant. The accumulated section uses smaller body text, a stable two-column label/value layout, and sufficient height for the maximum supported content. It must never concatenate unrelated values into one oversized line.

### 3. Combat weapon rack

Each owned weapon uses one 124 x 124 pixel-art slot:

- the weapon icon remains centered;
- one to five small stars below the icon show weapon level;
- up to three small potential glyphs remain visible inside the slot;
- the outer frame color shows the overall quality of accumulated general affixes;
- tapping the slot opens the read-only detail described above.

Quality uses a normalized average of the actual affix values, not only the coarse `Standard`, `High`, and `Perfect` labels. The five presentation bands are:

Each roll is normalized with `(absolute value - that stat's minimum) / (that stat's maximum - minimum)`, clamped to 0-1. The weapon score is the arithmetic mean of its normalized rolls. A weapon with no rolls uses score zero. Potentials and weapon level do not alter this score because they are communicated separately.

| Score | Frame |
|---|---|
| no rolls or below 0.30 | ash gray |
| 0.30-0.49 | green |
| 0.50-0.69 | blue |
| 0.70-0.89 | restrained crimson |
| 0.90-1.00 | gold |

Black is not used because it disappears against the combat HUD. The source frame remains visible at every quality; color multiplies only the bright ornamental pixels. Level pulse animation affects the whole slot uniformly and returns to scale one.

### 4. Frost Flask control identity

Frost Flask becomes a persistent control field:

- landing damage: 20% of authored base damage;
- field damage: 20% of authored base damage every 0.5 seconds, with the first tick after 0.5 seconds rather than on landing;
- slow: applies continuously while a target remains in the field and uses the authored 35-55% strength;
- freeze: a target that remains for 0.75 seconds receives a short 0.3-second freeze;
- slow removal retains the existing short decay so overlapping fields do not snap movement speed;
- a target that remains for the complete standard level-five field receives at most 100% of Frost Flask authored base damage in total: 20% landing plus four 20% ticks. This is 10 damage before modifiers, versus Thunder Crash Bomb's level-five authored 24-damage burst before its extra bomb count;
- Frost Shatter legacy and evolved behavior retain their distinct shatter bonuses, but standard fields cannot inherit the old full landing burst.

The persistent field no longer displays `frost_growth` crystals. It cycles existing restrained `frost_shatter_04` and `frost_shatter_05` snowflake/fleck frames at low alpha, with a flattened ground-facing scale. Landing uses one short small flake burst. Large crystals are reserved for a future explicit shatter or evolution moment and are not used as the normal field.

No new image asset is required for this pass.

### 5. Gakgung sniper identity

Authored progression becomes:

| Level | Base damage | Cooldown |
|---|---:|---:|
| 1 | 28 | 1.35 s |
| 2 | 40 | 1.30 s |
| 3 | 54 | 1.25 s |
| 4 | 72 | 1.20 s |
| 5 | 96 | 1.15 s |

- Range and projectile speed retain the current long-range progression.
- Target acquisition remains restricted to the current gameplay viewport.
- Priority remains boss, elite, threat, distance, then stable runtime ID.
- Level-five side arrows deal 50% of the primary arrow's damage instead of 100%.
- The primary shot does not gain a generic splash explosion. High single-target impact is its defining advantage.
- Existing legacy and potential multipliers continue to apply after the authored base values.

### 6. Fifteen-minute enemy durability curve

Normal-enemy base health uses a piecewise linear curve:

| Time | Base health |
|---|---:|
| 0:00 | 18 |
| 3:00 | 42 |
| 6:00 | 68 |
| 10:00 | 105 |
| 15:00 | 155 |

The curve restores the original opening ramp and then continues scaling for the longer stage. Existing rank and archetype multipliers apply after this base value:

- rats remain disposable but stop being universal one-hit targets after the opening;
- vengeful spirits remain fragile and fast;
- dokkaebi remain slow, visibly large, and durable;
- shield dokkaebi retain charge-based frontal mitigation;
- elites retain their current 4x health multiplier and existing distinct presentation.

This pass does not add a new recolored veteran class. The existing archetype and elite systems already communicate durability without adding more colors or screen noise. Enemy active caps, spawn cadence, experience rewards, separation, and pooling remain unchanged.

### 7. Loadout limit

The run keeps the existing limit of four weapons and three supports. The fifteen-minute extension does not change this limit. Four weapons provide enough build variety for the current level-35 progression without requiring additional HUD rows or more upgrade content.

## Data and Code Boundaries

- `FirstPlayableUiBootstrap` owns whether a selected reward needs presentation, but never applies gameplay stats.
- `FirstPlayableController` continues to own upgrades, accumulated affix data, enemy health construction, and the UI view state.
- A pure affix-quality helper converts actual rolls to a normalized score and presentation band. The presenter does not duplicate roll ranges.
- `WeaponSlotView` exposes structured accumulated effects required by the rack and detail view. Player-facing UI must not parse formatted strings back into gameplay data.
- `WeaponAffixRevealPresenter` renders a shared effect-summary model for both appraisal and read-only details.
- Frost and Gakgung numerical behavior remains inside their weapon content/executors and receives deterministic EditMode coverage.
- The enemy health curve is a pure helper so milestone values can be tested without spawning scene objects.

## Performance and Asset Safety

- Support upgrades remove a modal instead of adding runtime work.
- Weapon stars and potential glyphs are created only when a slot is created, not every frame.
- Affix quality is calculated when building UI state, not during HUD animation.
- Frost reuses checked-in sprites and the existing pooled transient visual path.
- Enemy durability changes values only; they do not increase the active enemy cap or add renderers.
- No scene, prefab, package, project setting, font atlas, or unrelated dirty PNG metadata is modified.

## Testing Strategy

### EditMode

- support reward routing identifies support as immediate and weapon/evolution flows as modal where appropriate;
- affix quality normalization covers empty, boundary, and maximum roll sets;
- the five frame bands map to the approved colors;
- Gakgung content values and level-five side-arrow multiplier match the approved progression;
- Frost standard landing/tick cadence and freeze residence match the approved control contract;
- Frost standard throughput remains below the Thunder comparison budget;
- the enemy health curve returns exactly 18, 42, 68, 105, and 155 at its milestones and interpolates monotonically between them;
- the loadout limit remains four weapons and three supports.

### PlayMode

- selecting a support closes the choice, opens no reward modal, resumes gameplay, and preserves queued-level grace;
- weapon appraisal still waits for confirmation after revealing the roll;
- appraisal and read-only details display separate accumulated effect rows without overlap;
- long two-modifier summaries fit at supported portrait resolutions;
- combat slots show the correct star count, quality frame, potential glyphs, and open read-only details;
- Frost uses the restrained field frames and never scales a crystal sprite as the persistent field;
- nearby regression coverage for weapon replacement, legacy selection, modal time ownership, and HUD taps remains green.

### Final validation

Run focused tests red then green, full EditMode, full PlayMode, and the Android ARM64 IL2CPP development build. Review representative portrait captures for the combat rack, appraisal, read-only details, and Frost field. Run Unity sequentially at BelowNormal priority with CPU affinity mask 15, and preserve all pre-existing unrelated metadata changes.

## Acceptance Criteria

- A support selection returns to combat without a second confirmation button.
- A player can state exactly which affixes, growth path, and potentials affect a weapon from either weapon screen.
- No Korean modifier string overlaps another UI element at supported portrait resolutions.
- Weapon level is readable from stars and affix quality from the frame without opening details.
- Standard Frost Flask controls a group over time and cannot erase it with its landing hit.
- Gakgung delivers a visibly stronger, slower primary shot and does not target off-screen enemies.
- The original three-minute enemy durability is restored and continues rising through fifteen minutes.
- The combat HUD supports exactly four weapons and three supports without layout regression.
