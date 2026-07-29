# Combat Visual Polish Design

## Goal

Rebuild the first-playable combat presentation so Android landscape gameplay shows a broad,
readable battlefield with small characters, contact-faithful weapon attacks, a quiet background,
and an understandable upgrade/affix reveal.

## Confirmed visual target

- The visible battlefield should cover roughly three times the current area.
- The player and normal enemies should occupy about one third of their current screen height.
- A normal enemy should be close to the player's size, an elite about 1.25 times the normal
  silhouette, and the boss about 1.8 times the normal silhouette.
- Player, enemy, weapon, pickup, shadow, health-bar, damage-number and VFX sizes must be coordinated.
- The player remains readable through silhouette separation, a soft shadow, a restrained aura and
  hit flash. Effects must not obscure enemy groups.
- The shipped reference view is 1920x1080 landscape. Portrait/free-aspect Editor views must remain
  usable, but are not the composition target.

## Architecture

### Combat scale profile

`CombatVisualScaleProfile` is a small runtime value type owned by gameplay presentation. It defines
camera size, player/normal/elite/boss display scales, world-bar sizes, contact radii and common
weapon/VFX scale multipliers. `FirstPlayableController` applies this profile when creating and
updating runtime objects instead of scattering unrelated magic numbers.

Display and collision remain separate concerns but share the same authoritative scale. Character
pixel hurt masks use the logical root's scale. Weapon presentation and weapon hit-mask transforms
receive matching per-weapon scale values. Damage continues to be authoritative in the existing
executors and `PixelMaskContactService`; animation never applies damage by itself.

### Camera and battlefield

The camera orthographic size increases from 6.25 to a landscape-tuned value near 10.5. Combined
with a character world scale near 0.62, this produces approximately 0.37 of the former screen
height while showing about 2.8 times the former world area.

The current battlefield root snaps every two world units, visibly changing the floor under the
camera. It is replaced by a stable, world-anchored, seamless low-saturation ground. The center is
kept clean; sparse decorations are deterministic and far apart. No crack, fissure or high-contrast
branching motif is allowed.

### Combatant readability

`CombatantVisualRig` owns visual-only child layers:

- soft elliptical shadow below the feet;
- restrained player aura only for the player;
- main animated sprite;
- optional one-pixel-style silhouette duplicate behind the main sprite.

The logical root remains the movement and collision owner. Hit flashes briefly brighten the main
sprite without permanently replacing palette colors. World-space health bars use rank-aware
dimensions and offsets.

### Weapon presentation

The existing eight executors and pixel-mask contact system remain authoritative. Each weapon gets a
presentation scale and a distinct motion contract:

- Hwando flying blade: short launch anticipation, rotating curved outbound flight, contact flash,
  rebound and faster rotating return.
- Gakgung: compact draw glint, narrow arrow flight, contact spark and short target recoil.
- Talisman: fluttering chained hops with small seal pops.
- Thunder bomb: small arcing bomb, warning marker and compact blast.
- Jangseung ward: planted markers with restrained boundary pulses.
- Singijeon: compact volley with thin powder traces and clustered detonation.
- Frost flask: rotating lob, shatter and low-opacity ground frost.
- Wind-thunder fan: small spinning fan/gust and targeted lightning rather than a screen-filling arc.

Sprite bounds are normalized at presentation time so 32 PPU weapon sources cannot appear twice the
world size of 64 PPU combatants. Visible sprite transforms and hit-mask transforms use the same
scale. Existing checked-in multi-frame weapon polish assets are reused first. PixelLab generation
is reserved for motion stages that remain visibly incomplete after the first in-game capture.

### Oversized “7” diagnosis

Repository search found no large runtime text that renders a bare `7`. The captured purple shape is
therefore treated as an oversized transient weapon/VFX until runtime hierarchy inspection proves
otherwise. Runtime diagnostics will identify the renderer, sprite bounds, PPU and owner executor.
The source scale is fixed; the symptom is not hidden by clipping or an overlay.

### Upgrade and affix presentation

The choice screen retains three immediately selectable cards but reduces card height and empty
space so the player can scan all choices quickly. Selection closes cleanly before the affix reveal.

The affix micro-slot uses an explicit timeline:

- entrance and acceleration: 0.35 seconds;
- fast spin: 0.9 seconds;
- eased deceleration and sequential stops: 0.9-1.25 seconds;
- final hesitation and lock: 0.25-0.45 seconds;
- readable result: at least 1.5 seconds and then an explicit confirm button.

Normal rolls use the shorter end. High/perfect affixes and one-to-three new potentials extend the
sequential stop and result emphasis. Clicking during spin may accelerate to the earliest readable
stop state, but cannot skip the final readable result. A second click or confirm button closes only
after the result has been exposed.

## Asset rules

- One logical sprite frame per PNG.
- Transparent background for combatants, weapons and VFX.
- Point filtering, no mipmaps, no lossy compression for source sprites.
- Character sources remain 64 PPU; weapon/VFX import and runtime normalization must produce the
  same effective world-pixel density.
- Canvas, pivot and foot/contact anchors stay stable between animation frames.
- PixelLab jobs are defined from a motion-stage list before generation and imported through the
  existing project asset-production conventions.

## Performance and memory

- No per-frame sprite or material creation.
- Existing transient weapon pooling remains in use.
- Combatant shadow/outline objects are created once with the combatant and destroyed with it.
- Enemy cap may increase only after the wider view is verified, and must remain bounded.
- Unity is launched with reduced worker counts for validation. Play Mode is stopped between capture
  stages, and available system memory is checked before each launch.

## Validation

- EditMode tests cover scale values, rank hierarchy, camera-area ratio, slot timing and asset
  contracts.
- PlayMode tests cover combatant visual hierarchy, visible/hit-mask scale agreement, upgrade
  open/close, affix confirm gating and pooled cleanup.
- A 1920x1080 Gameplay capture verifies battlefield density, background stability, weapon
  silhouettes, the absence of the large purple shape and UI readability.
- Hwando contact is verified by correlating its visible transform, pixel-mask contact and confirmed
  damage event.
- Unity Console must contain no new first-party errors.

## Non-goals

- No new render pipeline, dependency, global service framework or physics rewrite.
- No global reimport of every sprite PPU.
- No gameplay balance overhaul beyond spacing, contact radii and perception-preserving speed
  adjustments required by the new visual scale.

