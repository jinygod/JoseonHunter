# Joseon Hunter Static Sprite Launch Asset Batch Design

**Date:** 2026-07-27  
**Status:** Approved
**Supersedes:** The requirement to ship per-character walk, idle, and death
sprite frames in the PixelLab front-facing character pilot

## Goal

Create the complete minimum visual cast for the first playable release while
keeping asset production and runtime animation simple. Each hero, enemy, boss,
and pickup uses one transparent PNG. Unity supplies direction and motion at
runtime.

This batch establishes the approved rookie constable as the master proportion
and pixel-density reference. Later assets must look like members of the same
game without copying SPUM characters or other commercial-game source art.

## Chosen Approach

Use one static `64 x 64` RGBA sprite per entity, authored facing generally
right in a front-biased three-quarter pose. Set `SpriteRenderer.flipX` when an
entity moves left. Preserve a neutral readable silhouette so the same source
sprite works while moving up or down.

Runtime motion supplies:

- a one-pixel vertical bob while moving;
- a subtle direction-relative tilt;
- a brief white damage flash and squash;
- a shrink, settle, and fade death;
- no character attack animation, because weapons and effects own attacks.

The completed four-frame constable walk pilot remains source-only reference
material and is not imported into the runtime or used as the launch animation
contract.

## Alternatives Considered

1. **One static sprite plus procedural motion — chosen.** Lowest art cost,
   easiest to keep consistent, and sufficient for a mobile survivor game.
2. **Two hand-authored movement frames per entity.** More organic but doubles
   the batch and introduces identity drift.
3. **Full idle, move, hit, and death sheets.** Highest animation quality, but
   too slow and expensive for the launch schedule.

## Launch Asset Roster

### Heroes

1. Rookie constable — approved navy-and-red patrol uniform, black patrol hat,
   hopae, and sheathed hwando.
2. Shaman — ivory and muted red ritual clothing, compact ritual fan and charm,
   friendly but capable silhouette.
3. Mountain hunter — practical brown-and-forest clothing, small horn bow and
   quiver, rugged but cute silhouette.

### Normal enemies

1. Plague rat — hunched grey-brown rat with sickly green accent.
2. Vengeful spirit — pale blue-white floating spirit with dark hair and
   trailing lower body.
3. Sakkat specter — straw-hat ghost with a readable paper charm.
4. Dokkaebi — compact horned teal or blue goblin with a small wooden club.
5. Bandit — masked Joseon-era human with a red headband and short blade.

### Boss

1. Fallen general — larger-looking armored undead commander with a crested
   helmet and broken polearm. It still occupies a `64 x 64` file but uses more
   of the canvas than normal enemies.

### Pickups

1. Coin (`엽전`) — round brass coin with a square hole, readable at small scale.
2. Experience spirit flame — cyan-blue compact flame with a bright core.
3. Treasure chest — small dark-wood chest with brass trim and a red seal.

## Art Contract

Every entity file must:

- be exactly `64 x 64` RGBA;
- use only alpha `0` or `255`;
- have transparent corners and no baked background or external shadow;
- use crisp pixel clusters with no anti-aliasing;
- use a near-black one-pixel outline;
- stay readable at native scale on light and dark ground;
- fit within a common bottom anchor at `(32, 56)`;
- use no more than 48 opaque colors;
- preserve roughly two-head-tall proportions for humanoids;
- preserve the approved constable's outline weight, face scale, and pixel
  density as the master style reference.

Monsters may vary their silhouette, but their eye size, outline weight, and
rendering density must remain compatible with the heroes. Pickups may use a
smaller opaque footprint while retaining the same canvas and anchor convention.

## PixelLab Production

Generate one candidate per roster item with the approved constable as the style
reference where the operation supports it. A candidate failing dimensions,
alpha, palette size, anchor, or recognizability is not imported.

Persist:

- the final source PNG;
- the exact prompt;
- provider operation and non-secret job identifier;
- output SHA-256;
- generation count;
- deterministic normalization notes.

Never persist an API key or token. Avoid PixelLab operations whose quoted cost
is disproportionate to a single static `64 x 64` sprite. Fast one-generation
image operations are preferred.

## Review and Approval

Create one batch review board containing:

- every sprite at native size;
- an 8x nearest-neighbor enlargement;
- light and dark background checks;
- stable asset names;
- PixelLab generation usage;
- `PENDING BATCH APPROVAL`.

The user approves the lineup as a batch. Individual candidates may be replaced
without blocking candidates already accepted. No candidate enters the runtime
manifest before technical validation; the batch enters runtime only after
visual approval.

## Unity Integration

Import approved images under category-specific runtime folders as single
sprites with:

- point filtering;
- no mipmaps;
- no compression;
- `32` pixels per unit;
- custom pivot `(0.5, 0.125)`.

A small reusable presentation component reads horizontal velocity:

- positive velocity: `flipX = false`;
- negative velocity: `flipX = true`;
- zero horizontal velocity: keep the last facing direction.

The component owns procedural bob, tilt, hit flash, and death presentation. It
does not own combat logic, movement authority, health, or drops.

## Validation

Automated asset preflight rejects:

- incorrect canvas or color mode;
- semi-transparent pixels;
- opaque corners;
- excess colors;
- off-center or out-of-bounds silhouettes;
- duplicate runtime identifiers;
- source/runtime byte mismatches;
- token-like secrets in provenance files.

Unity EditMode tests verify importer settings, pivot, single-sprite mode, and
left/right facing behavior. PlayMode validation verifies that bobbing stops
when idle, the last direction is retained at zero horizontal velocity, hit
presentation resets, and death presentation does not modify gameplay state.

## Completion Criteria

The batch is complete when all twelve static assets pass preflight, the review
board receives explicit visual approval, runtime copies and records match their
source hashes, Unity imports them as single sprites, automated tests pass, and
the first-stage scene visibly uses the approved assets.
