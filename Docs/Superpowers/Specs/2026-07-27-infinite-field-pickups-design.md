# JoseonHunter Infinite Field And Reward Pickups Design

## Goal

Remove visible or mechanical map boundaries from the 15-minute survivor run
and create two satisfying reward moments: a rare experience magnet and
breakable treasure chests that are the only combat source of yeopjeon.

## Infinite Field

- Player, camera, and enemy spawning have no fixed world-coordinate clamps.
- The clean grass field and grid follow the camera in two-unit snapped steps,
  creating a seamless effectively infinite battlefield.
- Gameplay objects keep normal world coordinates. No floating-origin reset is
  needed for a maximum 15-minute run.
- Enemies spawn 7.5 to 9.5 world units around the current player position.
- Geumjul trail points remain in world coordinates and keep their current loop
  behavior.

## Normal Enemy Rewards

- Every defeated normal enemy drops exactly one experience spirit.
- Normal enemies never drop yeopjeon.
- Each defeated normal enemy has a 1 percent chance to additionally drop one
  experience magnet.
- Boss reward rules are separate and do not add random normal-enemy coins.

## Experience Magnet

- The temporary launch visual uses the existing treasure-like pickup sprite
  with a distinct cyan tint until a dedicated magnet asset is approved.
- Collecting the magnet marks every experience spirit currently on the field
  for forced collection.
- Marked experience spirits fly toward the player rapidly regardless of normal
  pickup radius and award experience only when they reach the player.
- The magnet does not pull yeopjeon, treasure chests, or later-created
  experience spirits.
- A short `혼령 대회수!` message is shown for 1.2 seconds.

## Treasure Chests And Yeopjeon

- The 60-second test run spawns its first chest at 18 seconds.
- Later chests spawn after a random 40-to-60-second interval.
- At most two unopened chests may exist.
- A chest appears 7 to 10 world units from the current player and remains
  stationary.
- Automatic hwando attacks may target a chest. It has 75 health, takes normal
  weapon damage, deals no contact damage, and flashes when hit.
- Breaking a chest scatters 6 to 10 yeopjeon pickups. Each pickup is worth
  1 to 3 yeopjeon.
- Yeopjeon uses normal proximity collection and is not affected by the
  experience magnet.
- Chest destruction does not count as a monster kill and does not drop
  experience.

## Presentation

- The infinite field stays flat and collider-free.
- Chest, magnet, experience, and yeopjeon remain visually smaller than the
  player and are readable through color and motion.
- Chest breaking uses a brief scale pop and radial coin scatter.
- No new final art is required for this implementation pass; existing approved
  static sprites are temporary launch visuals.

## Validation

- Walking continuously in every direction never exposes a field edge.
- Camera, ground, and enemy spawns follow the player beyond the old bounds.
- Normal enemy defeat produces experience and never yeopjeon.
- Magnet collection pulls only experience already present at collection time.
- Chest timing, maximum active count, damage, break reward, and coin values
  follow the values above.
- Unity compiles without new errors; a short Play-mode smoke run validates the
  original edge reproduction and both reward paths.
