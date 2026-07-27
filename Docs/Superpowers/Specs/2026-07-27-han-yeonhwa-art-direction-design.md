# Han Yeonhwa Art Direction And Gameplay Presentation Design

## Goal

Reframe JoseonHunter as a subculture-friendly Korean occult fantasy led by one
glamorous adult heroine, while keeping the readable front-facing chibi combat
style and the existing hwando, geumjul, and monster-hunting systems.

## Core Direction

- Keep the Joseon occult-fantasy identity as the game's differentiation.
- Prioritize attractive modern anime character design over strict historical
  reconstruction.
- Use polished adult heroine illustrations outside combat and cute two-head
  pixel characters during combat.
- The launch version has exactly one playable character and no character
  selection or locked-character screen.

## Heroine

The launch heroine is **Han Yeonhwa**, a clearly adult woman in her late
twenties.

- Role: exorcist swordswoman.
- Personality: confident, provocative, and openly enjoys combat.
- Signature systems: hwando automatic attacks and geumjul trail sealing.
- Hair: long black hair with a prominent braid and ornamental hairpin.
- Outfit: indigo, ivory, and crimson combat hanbok interpretation.
- Accessories: gold norigae details, talismans, and sword fittings.
- Historical accuracy is secondary to a coherent Korean occult-fantasy
  silhouette.

## Illustration Pose And Quality Rules

- The default key-art pose has Yeonhwa standing proudly with shoulders pulled
  back and her upper body held confidently forward.
- Prefer both hands behind her back, concealed inside long sleeves, or hidden
  behind a large prop.
- If a hand must be visible, expose only one hand in a simple pose.
- Every generated illustration must be inspected for finger count, joint
  direction, weapon grip, weapon continuity, facial asymmetry, and costume
  continuity.
- Failed anatomy is repaired through targeted inpainting. Images that still
  fail inspection are not used in the game.
- The character is sexy, glamorous, and cute without nudity or explicit sexual
  content. All marketing and store art must communicate that she is an adult.

## Screen Usage

1. Native application splash remains a short, lightweight logo screen.
2. The title screen uses the strongest full-body Han Yeonhwa illustration.
3. Loading screens rotate alternate crops, expressions, or poses with one short
   worldbuilding or gameplay sentence.
4. Lobby, equipment, shop, and progression screens use full-body or half-body
   Yeonhwa illustrations.
5. Combat uses a front-facing two-head pixel chibi derived from the same hair,
   indigo jacket, crimson ties, ornaments, and hwando.

Each production character set contains:

- one approved full-body key illustration;
- two reusable loading-screen crops or expression variants;
- one front-facing combat sprite;
- optional pixel weapon and effect sprites owned by the combat system rather
  than the character animation.

## Pixel Combat Assets

- Geumjul uses repeatable pixel rope segments, knot/talisman accents, and a
  separate closed-loop flash.
- Unity places the repeatable segments along the runtime trail so arbitrary
  loop shapes remain possible.
- Hwando, talisman, projectile, and impact effects are separate pixel assets.
- Character attack animation remains unnecessary; weapons and VFX communicate
  attacks.

## Immediate Gameplay Presentation Changes

- Player movement speed changes from `4.8` to `2.4`.
- Normal enemy speed changes from the `1.55` to `2.65` range to the `0.775` to
  `1.325` range.
- Test boss speed changes from `2.25` to `1.125`.
- Automatic attack interval and experience balance remain unchanged.
- Normal enemies have no health bars.
- Bosses have no world-space health bar. While a boss is alive, a named boss
  health bar appears at the top of the screen.
- The player keeps the world-space health bar under her feet.

## Validation

- Han Yeonhwa is the only playable/visible roster character at launch.
- The title, loading, lobby, and combat presentations clearly depict the same
  character.
- Illustration anatomy passes the hand, weapon, face, and costume checklist.
- Player, normal enemy, and test boss speeds use the exact values above.
- Normal enemies create no health-bar objects.
- The Fallen General displays one top-screen boss bar that follows its current
  health and disappears when defeated.
- Unity compiles without new errors and a short Play-mode smoke run verifies
  movement pace and boss UI activation.
