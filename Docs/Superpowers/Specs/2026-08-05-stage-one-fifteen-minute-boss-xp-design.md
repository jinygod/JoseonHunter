# Stage One Fifteen-Minute Boss and XP Design

## Goal

Turn the current three-minute prototype into a complete fifteen-minute Stage 1 with readable enemy introductions, visibly oversized bosses, avoidable telegraphed boss attacks, controlled level pacing, and bounded pickup cost on mobile.

## Stage contract

- Stage duration is 900 seconds. Normal spawning stops at 15:00 and the final boss fight remains untimed until victory or player death.
- The single canonical timeline owns phase boundaries, wave composition, packs, introductions, warnings, and boss milestones. Runtime systems must not keep separate 180-second constants.
- Authored beats are:
  - 0:00-2:00: plague rats establish the baseline.
  - 2:00-5:00: vengeful spirits are introduced, then mixed with rats.
  - 5:00: first midboss.
  - 5:00-7:00: dokkaebi are introduced as slow durable enemies.
  - 7:00-10:00: learned families and special enemies mix.
  - 10:00: second midboss.
  - 10:00-12:00: mixed pressure.
  - 12:00-14:00: final surge.
  - 14:00-15:00: final warning and preparation.
  - 15:00: final boss; normal waves stop.
- Active enemies remain capped at 140 for the mobile target.

## Boss identity and scale

- Normal enemy scale remains unchanged.
- First midboss uses approximately 1.7 times the normal rendered size.
- Second midboss uses approximately 1.9 times the normal rendered size.
- The final Fallen General uses approximately 2.3 times the normal rendered size.
- Health bars, collision masks, spawn clearance, telegraph radius, and contact spacing follow the scaled body so the image and gameplay footprint agree.
- Boss rendering uses the existing Fallen General and captain animation frames. New detailed character art is not required.

## Boss combat

Boss behavior is an explicit `Chase -> Telegraph -> Execute -> Recover` state machine. Telegraphs lock their target when they begin, use scaled time, and never deal damage before the warning completes.

### First midboss: suppression slam

- A dark-crimson circle appears at the player's locked position for 1.1 seconds.
- The circle pulses once and resolves as an area hit for 16 damage.
- The player escapes by leaving the circle before resolution.

### Second midboss: blood charge

- A dark-crimson corridor from boss to the player's locked position appears for 0.95 seconds.
- The boss then charges along that fixed corridor and deals 20 damage at most once per charge.
- The player escapes by moving out of the corridor.

### Final boss: Fallen General

- Blood charge: 0.95-second corridor warning, fast fixed-direction charge, 22 damage at most once.
- General's suppression: 1.1-second circle warning at the locked player position, 18 damage on resolution.
- Spirit volley: 0.8-second ring warning around the boss, followed by eight directional projectiles with deliberate angular gaps, 11 damage per projectile.
- Below 50% health, recovery shortens and the volley uses ten projectiles. Warning durations never fall below 0.7 seconds and attacks do not overlap.
- Slow effects apply at 35% of their normal strength to bosses. Freeze becomes a short 0.25-second stagger with an internal cooldown so the boss cannot be permanently locked.

## Telegraph visual language

- Ground markers are pooled runtime geometry rather than large imported bitmaps: dark crimson outline, transparent muted red fill, two to three colors, no white outline, and a restrained opacity pulse.
- The corridor, target circle, and boss ring use the same palette and are always below characters but above the field.
- Existing sprites are sufficient for the first implementation. PixelLab is only used later if play capture proves the small spirit projectile unreadable; no new asset is generated speculatively.

## Progression and level cap

- Maximum player level is 35 for this content set. Four weapon slots and three support slots provide exactly 34 meaningful selections after the starting level.
- Reaching level 35 stops further experience drops and level-up requests. It must never call `UpgradeSelector` with fewer than three eligible offers.
- Target milestones are level 7-9 at 2:00, 14-17 at 5:00, 24-27 at 10:00, and 32-35 at 15:00.
- Early choices arrive quickly; late choices should normally be 40-60 seconds apart.
- The curve is validated with deterministic expected-kill simulations and adjusted without adding meaningless repeatable stat filler.

## Pickup performance and readability

- The attraction radius remains 0.58 world units and final collection behavior remains unchanged.
- Experience value tiers use one existing spirit-flame sprite with limited palette and scale variation:
  - 1-4 XP: cyan.
  - 5-19 XP: violet.
  - 20+ XP: magenta.
- At most 180 active experience pickups may exist. When the budget is full, new XP merges into the nearest pickup in a bounded spatial search and promotes its visible tier. Total XP value is preserved exactly.
- Experience, coin, and magnet objects use reusable pools. Pooled state resets attraction, force-collect, value, color, scale, and trail state.
- Trail components are prepared when pooled objects are created, not added during attraction.
- Magnet collection begins pickups in bounded batches so one frame does not activate every trail and transform at once.
- Random magnet frequency is drastically reduced from the current per-kill rate; deterministic reward drops around the long-run milestones provide the intended sweep moments.

## Architecture

- Plain Domain C# owns stage timing, boss attack decisions, level cap rules, pickup merge accounting, and tier classification.
- `FirstPlayableController` remains the composition owner and bridges domain decisions to transforms, sprite renderers, player damage, and existing combat targets.
- New presentation objects are runtime-created and pooled. No scene or prefab save is required.
- Existing public contracts remain stable unless a duration-aware overload is necessary; old short preview tests continue through explicit duration parameters.

## Acceptance criteria

- Stage 1 reaches its authored milestones at 300, 600, 840, and 900 seconds.
- No enemy family appears before its introduction window.
- Midbosses and final boss are visually and physically larger than normal enemies.
- Each boss attack displays a readable red warning, permits escape, resolves once, and enters recovery.
- Normal spawning stops for the final boss.
- Level never exceeds 35 and no empty upgrade modal is opened.
- Pickup radius is unchanged, active XP pickups never exceed 180, and merging preserves value.
- Focused and full Unity tests pass, the Android development build succeeds, and portrait captures are inspected at representative phone sizes.

