# Eight-Weapon Pixel-Contact Combat Design

Date: 2026-07-27  
Status: user-approved design

## Goal

Replace the loose prototype combat inherited from the Flutter version with
eight mechanically distinct Unity weapons whose visible motion, contact timing,
damage resolution, and feedback agree.

The player must be able to understand why an enemy took damage by looking at
the attack. A weapon cannot deal damage merely because its presentation
started. Projectiles, moving blades, boundaries, and expanding effects deal
damage only when their active hit pixels reach a valid enemy hurt area.

## Launch Weapon Roster

The launch roster contains exactly these eight weapons:

1. `hwando_flying_blade` — 환도 비검
2. `gakgung_shot` — 각궁 사격
3. `talisman_throw` — 부적 투척
4. `thunder_crash_bomb` — 벽력진천뢰
5. `jangseung_ward` — 장승 결계
6. `singijeon_volley` — 신기전 일제사격
7. `frost_flask` — 서리 호리병
8. `wind_thunder_fan` — 풍뢰 부채

The earlier large melee arc concept for the hwando is removed. It obscured the
screen, made contact timing ambiguous, and overlapped with the fan's close-range
area role. Han Yeonhwa instead controls a small occult flying hwando that spins
outward and returns.

## Weapon Identity Matrix

| Weapon | Targeting | Geometry | Timing | Primary role | Deliberate weakness |
| --- | --- | --- | --- | --- | --- |
| 환도 비검 | nearest valid enemy | outbound and returning curved path | fast repeated contact | reliable general-purpose damage | limited simultaneous coverage |
| 각궁 사격 | highest-threat target, then stable distance and ID | narrow high-speed line | immediate precision shot | elite and boss damage | can miss moving targets and weak against flanks |
| 부적 투척 | nearest unmarked target | sequential target-to-target hops | attach, seal, transfer | tracking scattered enemies | delayed damage and limited burst |
| 벽력진천뢰 | predicted densest enemy center | lob plus expanding circular blast | telegraph, fuse, explosion | clustered wave burst and knockback | enemies can leave before detonation |
| 장승 결계 | placement around player | finite ward boundaries | damage when an enemy crosses a line | defensive control and safe space | weak while continually relocating |
| 신기전 일제사격 | densest direction | multiple non-homing lanes | directional volley | clearing a crowded corridor | low single-target efficiency |
| 서리 호리병 | predicted crowd center | persistent circular field | impact, ticks, freeze threshold | slow and area denial | low immediate damage |
| 풍뢰 부채 | most dangerous surrounding sector | wind cone followed by linked lightning | push, mark, simultaneous echo | escape from encirclement | long recovery and low sustained damage |

No two weapons share the same combination of target selection, geometry,
timing, and battlefield purpose.

## Weapon Mechanics

### 환도 비검

- Launches a compact spinning hwando toward the nearest valid enemy.
- Curves back to the player after reaching its target distance or first edge
  contact.
- Deals damage only when active blade pixels overlap an enemy hurt mask.
- Each attack instance can hit the same enemy at most once outbound and once
  inbound.
- A returning blade is collected by proximity to the player; it cannot remain
  indefinitely if the player teleports or the target disappears.
- Growth increases damage, travel distance, speed, and controlled blade count.
- Master form launches a short staggered set of three blades with distinct
  return curves; it does not create a full-screen slash.

### 각궁 사격

- Selects the highest-threat target, prioritizing bosses and elites before
  health and stable runtime ID.
- Shows a brief narrow aim cue, then launches one fast, non-homing arrow.
- The arrow can miss after release.
- Damage occurs on arrow hit-pixel contact, with later levels adding
  penetration and critical precision.
- Master form delivers one armor-piercing lead arrow followed by two smaller
  split arrows behind the first target.

### 부적 투척

- Flies to the nearest enemy that is not already reserved by the same cast.
- Attaches only after visible contact.
- After a short seal delay, deals damage and transfers to the nearest legal
  unmarked target.
- If no transfer target exists, it bursts once on the attached target and
  ends.
- Growth increases hop count, simultaneous talismans, search radius, and seal
  speed.
- Master form leaves several seals active and detonates them together in a
  five-color binding burst.

### 벽력진천뢰

- Predicts the densest enemy center and lobs a bomb along a visible arc.
- Displays a short landing marker that communicates the expected impact area.
- The bomb bounces no more than once, burns a fuse, then produces an expanding
  blast.
- Enemies take damage when the active blast ring reaches their hurt area, not
  at fuse completion regardless of position.
- Growth increases blast reach, knockback, count, and reduces fuse duration.
- Master form adds one secondary outward shockwave after the center blast.

### 장승 결계

- Places compact ward posts near the player and links them with visible finite
  boundary segments.
- An enemy is damaged and pushed when crossing an active boundary.
- A crossing direction and per-enemy re-entry cooldown prevent repeated damage
  while the enemy remains on the line.
- Repositioning replaces the oldest ward set rather than accumulating
  unlimited boundaries.
- Growth increases post count, boundary length, refresh rate, and debuff
  strength.
- Master form maintains four mobile cardinal posts that reposition in bounded
  steps around the player.
- This remains distinct from geumjul: geumjul is a player-drawn closed loop
  with one area seal; jangseung is an automatically placed crossing defense.

### 신기전 일제사격

- Selects the direction containing the greatest number of valid enemies.
- Launches several non-homing rockets in a readable fan of narrow lanes.
- Each rocket uses its own contact mask and creates a small impact burst at
  contact or maximum travel.
- Growth increases lane count, fan width, light penetration, and impact size.
- Master form launches three visually separated rows rather than a single
  opaque wall of effects.

### 서리 호리병

- Throws a flask toward a predicted crowd center.
- The flask shatters on ground contact and creates a persistent cold field.
- Enemies are slowed immediately on entering, take bounded periodic damage,
  and briefly freeze after remaining inside for the configured threshold.
- Slow decays after leaving instead of ending on the same frame.
- Only a bounded number of fields may exist; the oldest expires first.
- Growth increases duration, radius, slow, field count, and freeze reliability.
- Master form periodically raises small ice spikes inside the field.

### 풍뢰 부채

- Finds the most dangerous surrounding sector using enemy distance and threat.
- Emits one broad but short wind cone that pushes enemies it visibly touches.
- Wind-hit enemies receive a temporary lightning mark.
- After a brief readable pause, marked enemies are struck simultaneously by
  linked lightning.
- Talisman chains move sequentially from target to target; fan lightning is a
  simultaneous echo on targets already contacted by the cone.
- Growth increases cone coverage, push, marked-target cap, and echo strength.
- Master form uses four short directional gusts followed by one bounded
  lightning resolution.

## Contact Architecture

### Broad Phase

Unity Physics 2D performs inexpensive candidate collection with non-triggering
query shapes. Each attack type supplies its own conservative query bounds.
Queries use explicit enemy layers and non-allocating buffers sized from the
combat population budget.

### Pixel-Mask Narrow Phase

Only broad-phase candidates enter the narrow phase.

- Every attack visual has a separate binary hit mask.
- Every enemy archetype has a compact binary hurt mask or an approved
  simplified hurt silhouette.
- Transparent glow, smoke, motion trails, telegraphs, and decorative sparks
  never enter hit masks.
- Mask coordinates are transformed through the same position, rotation, flip,
  and scale used by the rendered sprite.
- Overlap returns a world-space contact point used by damage feedback.
- Rotated or scaled attacks use deterministic nearest-neighbor sampling so
  visual pixels and mask pixels remain aligned.

Physics overlap alone cannot award damage. The pixel-mask check must confirm
contact while the attack's contact window is active.

### Attack Instances And Hit Memory

Every cast receives a stable attack-instance ID. Its hit memory records the
target ID, contact phase, and allowed repeat policy.

- single-hit attacks reject all repeated contact;
- the flying hwando permits one outbound and one inbound hit;
- persistent fields use deterministic tick windows;
- ward boundaries use crossing and re-entry windows;
- multi-projectile volleys keep hit memory per projectile;
- destroyed or pooled attacks clear all hit memory before reuse.

## Damage Pipeline

All weapons submit a shared immutable damage request containing:

- attack-instance ID;
- weapon ID and level;
- source and target runtime IDs;
- raw damage and element;
- critical state;
- knockback;
- world-space contact point;
- contact phase;
- frame or simulation tick.

The authoritative damage resolver validates the request, applies defense and
modifiers once, mutates health once, then publishes one confirmed damage event.
Presentation cannot change health and cannot invent unconfirmed numbers.

The current `FirstPlayableController` prototype's immediate `DamageEnemy`
calls and line-renderer-only hwando are migration targets, not extension
points. Weapon runtime behavior moves into focused components and pure combat
rules instead of growing the existing controller.

## Damage Number Presentation

A pooled TextMeshPro world-space presenter consumes confirmed damage events.

- Numbers originate at the pixel contact point with a small deterministic
  offset.
- Normal damage is light neutral text.
- Critical damage is larger gold text with one short scale punch.
- Elemental or seal damage may use a restrained weapon accent color.
- Boss numbers remain visible slightly longer but do not use a different
  damage value.
- Repeated damage-over-time events from one source and target are aggregated
  over a 0.25-second display window.
- Simultaneous multi-hit events may aggregate only for presentation; combat
  telemetry retains each authoritative hit.
- Presenters return to a bounded pool and never allocate one object per hit in
  sustained combat.

## PixelLab Asset Plan

PixelLab creates reusable source parts, not complete gameplay screenshots.
Unity owns transforms, timing, tint, repetition, and composition.

### Shared Style Lock

Before weapon production, approve one compact weapon contact sheet containing:

- near-black one-pixel outline;
- Joseon occult-fantasy indigo, ivory, crimson, gold, cyan, and ember accents;
- hard pixel edges and transparent background;
- no text, gradients, anti-aliased edges, or baked bloom;
- scale examples beside the approved Han Yeonhwa combat sprite.

This sheet becomes the style reference for every paid call.

### Required Source Assets

| Weapon | Generated source parts |
| --- | --- |
| 환도 비검 | spinning hwando frames, return glint, icon |
| 각궁 사격 | bow/arrow icon, arrow projectile, small contact spark |
| 부적 투척 | flying talisman, attached seal, transfer streak, binding burst, icon |
| 벽력진천뢰 | bomb, fuse frames, expanding blast, secondary shockwave, icon |
| 장승 결계 | post, repeatable boundary segment, crossing flash, debuff seal, icon |
| 신기전 일제사격 | rocket variants, smoke-less travel accent, impact burst, icon |
| 서리 호리병 | flask, shatter, repeatable frost patch, ice spike, icon |
| 풍뢰 부채 | fan, wind contact edge, lightning segments, lightning impact, icon |

Hit masks are derived deterministically from approved source artwork and then
reviewed. PixelLab generations are not spent on masks that tooling can create
from alpha and a small explicit inclusion/exclusion map.

### Generation Budget Policy

- Account baseline: Tier 1, 2,000 total monthly generations, 0 used when this
  design was written.
- No paid generation starts before the style-lock prompt and output dimensions
  are recorded.
- Begin with one style-lock batch and one representative asset per attack
  family: flying blade, projectile, attached mark, explosion, boundary,
  volley projectile, ground field, and lightning.
- Review representatives before generating secondary frames or icons.
- Reuse approved assets through rotation, flipping, tinting, segmented
  repetition, and nearest-neighbor scale.
- Use inexpensive standard or v3 generation where suitable. Pro generation is
  reserved for a failed representative that cannot be corrected from the
  approved base.
- Keep a per-job ledger with job ID, prompt revision, cost, result status, and
  remaining balance.
- Stop a weapon family after two rejected attempts and revise its reference or
  geometry before spending again.

## Runtime Boundaries

### Domain

Pure rules define weapon stats, target-selection priorities, attack-instance
hit policy, damage requests, and deterministic repeat windows. They do not
depend on Unity APIs.

### Content

ScriptableObject definitions bind weapon identity, levels, asset references,
collision-mask metadata, element, and presentation timings. Content files do
not mutate runtime state.

### Runtime

Focused weapon executors schedule attacks and move attack instances. A shared
contact service performs broad and narrow phase checks. A shared damage service
resolves confirmed hits. Pools own transient attacks and damage-number
presenters.

### Presentation

Sprite renderers, animation timing, audio cues, camera feedback, and damage
numbers consume runtime events. Presentation is not damage authority.

## Performance And Readability Limits

- At most a configured bounded number of each persistent field, boundary set,
  and projectile family may remain active.
- No full-screen opaque slash is used.
- Telegraphs are dimmer than active contact pixels.
- Active hit pixels use a consistent brightness hierarchy.
- Lightning links and ward lines have maximum segment counts.
- Contact checks begin with Physics 2D broad-phase filtering.
- Pixel-mask checks run only for candidates and active contact frames.
- Damage-number aggregation and pooling protect the mobile frame budget.
- Weapon VFX remain readable with 80 normal enemies in the Editor test profile.

## Validation

### EditMode

- all eight IDs are unique and have complete level data;
- each weapon has a distinct targeting, geometry, timing, and role contract;
- stable target tie-breaking;
- hit-mask transformation under rotation, flip, and scale;
- transparent visual pixels cannot deal damage;
- active hit pixels deal damage at the first overlapping tick;
- one-hit and permitted repeat-hit policies;
- ward crossing and field tick windows;
- deterministic damage resolution and critical handling;
- damage-number aggregation without changing combat totals;
- pool reset clears hit history.

### PlayMode

- every weapon can acquire, attack, damage, and kill a target;
- the flying hwando visibly contacts before outbound or inbound damage;
- arrows and rockets can miss;
- attached talismans transfer correctly;
- bomb damage follows the expanding blast;
- ward damage requires a boundary crossing;
- frost slow enters, ticks, freezes, and decays;
- fan wind precedes lightning;
- damage numbers originate near contact and return to the pool;
- no missing asset references or new Console errors;
- the 80-enemy profile remains responsive.

### Visual Review Gates

1. Shared weapon style lock.
2. One representative asset for each of the eight attack families.
3. Complete source-part contact sheet on light and dark backgrounds.
4. In-game contact capture showing visual overlap and damage on the same tick.
5. Full-roster readability capture under an 80-enemy stress scene.

No generated asset becomes production-approved before its applicable review
gate passes.

## Completion Criteria

- All eight weapons are mechanically distinct and data-driven.
- The old prototype line-renderer hwando is removed from damage authority.
- Every damaging attack uses an explicit active contact window.
- Projectile, boundary, and expanding-effect damage requires confirmed visible
  contact.
- Confirmed hits produce accurate pooled damage numbers.
- PixelLab source parts and their provenance are recorded without secrets.
- Automated tests and PlayMode checks pass without new Console errors.
- Existing user-created scenes and unrelated working-tree changes remain
  untouched.
