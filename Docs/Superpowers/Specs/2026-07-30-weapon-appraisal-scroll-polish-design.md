# Weapon Appraisal Scroll Polish Design

## Goal

Turn the current weapon appraisal panel into a pacing-aware Joseon fantasy
scroll reveal. New weapons receive a memorable full reveal, ordinary repeat
upgrades resolve quickly, and only valuable results interrupt the run with
additional tension.

## Player-facing flow

### First weapon acquisition

- The overlay darkens while the top and bottom scroll rods separate.
- The appraisal content is revealed through a vertical mask as the scroll
  expands from the center.
- Weapon identity appears first, then the new general affix counts from zero.
- The next eligible potential slot shakes and either settles quietly or
  reveals a jackpot.
- The result remains readable until explicit confirmation.

### Repeat weapon upgrade

- The weapon level is already authoritative before presentation begins.
- The scroll opens from a partially expanded state and reaches full size
  quickly.
- One new general affix is appended to the run profile. It never replaces a
  previous roll; repeated stat types add together in combat.
- The appraisal emphasizes the newly gained roll while a compact total line
  shows the accumulated weapon modifiers.
- Every repeat selection attempts the next potential slot while fewer than
  three distinct potentials are owned.

### Valuable result escalation

- A High or Perfect general affix slows the final count briefly and stamps the
  result with the PixelLab appraisal seal.
- A potential jackpot adds a violet ritual ring, localized shake, and
  sequential slot reveals.
- Standard results without a potential use the shortest presentation.
- The existing skip and confirm behavior remains idempotent.

## Pacing

| Result | Target automatic motion |
| --- | ---: |
| Repeat standard, no potential | 0.90 s |
| First acquisition, standard | 1.30 s |
| High or Perfect, no potential | 1.45–1.60 s |
| One potential | 2.10 s |
| Two potentials | 2.28 s |
| Three potentials | 2.40 s |

The first acquisition may use a longer opening phase without increasing the
jackpot cap. Repeat standard upgrades must not replay the full ceremonial
opening.

## Visual assets

PixelLab produces one PNG per final asset:

- `appraisal_scroll.png`: complete transparent scroll frame and paper.
- `appraisal_roller.png`: independent rod used above and below the reveal mask.
- `potential_ritual_seal.png`: violet jackpot overlay.
- `rare_appraisal_stamp.png`: High/Perfect result stamp.

The scroll animation uses one source image plus a Unity `RectMask2D`. It does
not store hundreds of near-identical frames, preserving memory while retaining
smooth motion at mobile resolution. All generated art is imported as crisp,
uncompressed, mip-free single sprites.

## Architecture

- `WeaponAppraisalViewModel` declares whether the result is a new acquisition
  or repeat upgrade and supplies the accumulated affix summary.
- `WeaponAppraisalPresentation` owns deterministic reveal-profile selection
  and opening-mask interpolation.
- `WeaponAffixRevealTimeline` accepts the reveal profile while preserving the
  current jackpot caps.
- `WeaponAffixRevealPresenter` remains the single runtime owner of the overlay,
  coroutine, skip, confirmation, and read-only detail mode.
- `WeaponAffixPresentationCatalogAsset` gains optional scroll-polish sprites.
  Legacy micro-slot sprites remain as safe fallback references.
- `WeaponAffixPixelAssetImporter` imports and wires the new PixelLab assets.

## Acceptance criteria

- A level-one weapon visibly unfurls from the center.
- A level-two-or-higher standard result opens faster than a new weapon.
- High, Perfect, or potential results use the ceremonial emphasis.
- The appraisal distinguishes the new affix from the accumulated total.
- Existing potentials remain visible and only the next eligible slot shakes.
- No potential duplicates are introduced and the three-slot cap remains.
- Missing new art falls back safely without blocking reward completion.
- Focused EditMode and PlayMode tests cover profile selection, opening
  interpolation, repeat-upgrade context, skip, and potential reveal order.

