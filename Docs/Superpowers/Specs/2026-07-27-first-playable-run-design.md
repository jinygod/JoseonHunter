# JoseonHunter First Playable Run Design

## Goal

Create the first Unity build that can be played repeatedly while the combat
feel, pacing, rewards, and difficulty are developed. The first milestone is a
60-second test run. The same runtime architecture must later support the
15-minute production run without replacing the core systems.

## Approved Run Formats

### 60-Second Test Run

- The run begins immediately after entering the gameplay scene.
- Normal enemies spawn and pursue the player from the start.
- A warning appears before the test boss.
- One test boss appears at 50 seconds.
- The run ends in victory when the boss is defeated, or at 60 seconds if the
  boss remains alive.
- Player death ends the run immediately.
- Victory and defeat both show a result screen with an immediate retry action.

### 15-Minute Production Run

- The first boss appears at 5:00.
- The second, stronger boss appears at 10:00.
- The final and most difficult boss appears at 15:00.
- Defeating the first and second bosses continues the same run.
- Defeating the final boss completes the run.
- Boss schedules and run duration are data-driven. Test and production modes
  use the same runtime systems with different schedule definitions.

## Player Experience

The first playable loop is:

```text
enter test run
  -> move and evade
  -> automatic hwando attacks
  -> defeat pursuing enemies
  -> collect experience flames and coins
  -> choose one of three upgrades on level-up
  -> close local geumjul loops for area damage
  -> fight the test boss
  -> view results
  -> retry immediately
```

The player character remains a front-facing static 64x64 sprite. Movement uses
procedural bobbing and horizontal sprite flipping. No character attack
animation is required; weapons and effects communicate attacks.

## Controls

- Desktop validation supports WASD and arrow keys.
- Mobile supports a floating drag joystick in the lower safe area.
- Both inputs produce the same normalized movement vector.
- Releasing input stops movement immediately.
- Input is disabled during upgrade selection, pause, and results.

## Runtime Architecture

### Domain

Existing deterministic Domain rules remain authoritative for:

- run time and phases;
- damage resolution;
- experience thresholds;
- three-choice upgrade offers;
- geumjul trail, loop detection, mastery, and seal resolution.

New pure Domain rules may be added only when behavior cannot be expressed by
the existing contracts. They remain independent of Unity APIs.

### Runtime

`JoseonHunter.Runtime` owns:

- run session orchestration;
- player movement and health;
- enemy and boss spawning;
- pursuit and contact damage;
- target acquisition;
- automatic weapon timing;
- projectile and pickup pooling;
- XP, coin, death, victory, and retry state transitions.

Runtime components receive explicit configuration. The first version avoids a
global service locator and avoids per-frame scene-wide searches.

### Presentation

`JoseonHunter.Presentation` owns:

- sprite movement feedback and horizontal flipping;
- health, XP, time, coin, boss warning, and boss health HUD;
- floating joystick;
- upgrade selection overlay;
- damage, pickup, geumjul, victory, and defeat feedback.

### Content

The first run uses the approved static launch sprites:

- rookie constable;
- plague rat and one additional normal enemy;
- Fallen General as the temporary test boss;
- experience spirit flame and coin.

The temporary ground is a clean, collider-free flat field. Pending final stage,
UI, weapon, and VFX art can replace placeholders without changing gameplay
contracts.

## Combat Rules

- Enemies move directly toward the player using deterministic speed values.
- Contact damage uses a cooldown so overlapping enemies cannot deal damage
  every rendered frame.
- The hwando automatically targets the nearest valid enemy within range.
- Target ties resolve by stable runtime ID.
- The first implementation uses a pooled, visible weapon strike or projectile
  and never character attack animation.
- Enemies drop XP deterministically. Coins use an explicit configured drop
  rule.
- Level-up pauses combat and presents exactly three legal upgrade choices.
- Geumjul records the recent player trail and applies existing Domain seal
  rules when a valid local loop closes.

## Test Boss

- Fallen General spawns once at 50 seconds after a warning.
- The test boss has more health, size, speed pressure, and contact damage than
  normal enemies.
- The first playable requires a readable health bar and a clear defeat event.
- Complex charge, cone, summon, and enrage patterns are deferred to the
  production boss task.
- The test schedule maps to the first production boss contract without
  hard-coding the 50-second value inside the boss component.

## Scene And Bootstrap

- `Bootstrap` remains the build entry scene.
- `Gameplay` receives a generated first-playable hierarchy through Unity Editor
  APIs, not hand-edited YAML.
- The hierarchy contains camera, world, pools, player, spawners, session
  controller, and HUD roots.
- The existing inactive static-sprite proof group remains separate from live
  gameplay and can be removed only through the scene generator.
- Retry resets the session without duplicating persistent objects or event
  subscriptions.

## Failure Handling

- Missing required content fails with one clear startup error and does not
  start a partially wired run.
- Pools use bounded growth and skip a spawn safely if capacity is exhausted.
- Invalid or non-finite movement and timing input is rejected before state
  mutation.
- A failed save after results does not block retry; the player receives a
  visible local-save warning.

## Performance Targets

- No `FindObjectOfType`, `GameObject.Find`, or scene-wide component scans occur
  in per-frame code.
- Normal enemies, weapon effects, and pickups use pooling.
- Target acquisition uses a maintained active-enemy collection.
- The 60-second test must remain responsive with at least 80 simultaneous
  normal enemies in the Editor test profile.
- Mobile production validation later targets the existing release performance
  budget and representative devices.

## Testing

### EditMode

- movement normalization and clamping;
- deterministic target selection;
- contact-damage cooldown;
- spawn schedule and active cap;
- XP and coin application;
- upgrade pause and legal three-choice resolution;
- test and production boss schedules;
- victory, timeout, death, and retry transitions.

### PlayMode

- Gameplay scene starts without missing references;
- keyboard movement changes player position and sprite facing;
- enemies spawn, pursue, and damage the player;
- hwando attacks and defeats an enemy;
- XP pickup opens an upgrade choice;
- a valid geumjul loop damages contained enemies;
- the test boss appears at 50 seconds through an accelerated test clock;
- death and victory both reach results and retry cleanly;
- pooled objects reset without leaked subscriptions.

### Manual Play Check

The user can open the project, enter Play Mode from Bootstrap or Gameplay, move
immediately, survive and upgrade, fight the test boss, see results, and retry
without Editor setup.

## Acceptance Criteria

- The first run is meaningfully playable in the Unity Editor.
- WASD, arrow keys, and mobile-style drag input work through one movement path.
- Combat, XP, upgrades, geumjul, boss, results, and retry form one complete
  60-second loop.
- The 5:00, 10:00, and 15:00 production boss schedule is represented as data
  and covered by tests.
- No new Console errors appear.
- Relevant EditMode and PlayMode tests pass.
- Existing user-created scenes and unrelated working-tree files remain
  untouched.
