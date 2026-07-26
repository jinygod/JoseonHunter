# Joseon Hunter Portrait Mobile Vertical Slice Design

**Status:** Approved in collaborative design review on 2026-07-26

**Target:** Android portrait mobile game

**Project:** `D:\UnityProjects\JoseonHunter`

**Release package:** `com.jinygod.joseonhunter`

## 1. Purpose

Build a release-shaped, offline-first vertical slice of Joseon Hunter as a
portrait mobile survivors-like game. The slice must be a complete repeatable
product loop rather than a technology demo:

```text
Shop or improve equipment
  -> choose a patrol
  -> survive for three minutes
  -> fight the boss
  -> receive coins, equipment progress, and investigation clues
  -> unlock progression
  -> patrol again
```

The game uses familiar mobile-game navigation so a player can understand it
without instruction. Its identity comes from the Joseon folk-fantasy setting,
the geumjul sealing mechanic, the investigation record, and the original
modular chibi pixel-art system.

This specification supersedes the landscape and five-minute product direction
in `2026-07-26-flutter-to-unity-migration-design.md`. The completed Unity
foundation, assembly boundaries, asset rights controls, official Unity MCP
connection, and repository structure remain valid.

## 2. Product Constraints

- Portrait-only Android is the first player platform.
- Reference resolution is 360 x 640. Layouts support phone and tablet safe
  areas and aspect ratios from 19.5:9 through 4:3.
- A boss appears at 3:00. A complete run, including the boss, ends by 4:00.
- Gameplay is offline. The first release has no account, cloud save, ads,
  in-app purchases, or required network connection.
- There is no energy system or limit on patrol attempts.
- Consumption currency is limited to one type, coins (`엽전`).
- Investigation clues are collection progress, not a spendable currency.
- The first release uses a new Unity save and does not import Flutter save data.
- SPUM code, packages, and assets are not used.
- AI-generated art must be original, must follow the rights ledger, and must
  not reproduce SPUM characters, logos, layouts, or source structure.
- The Flutter project is a source of reviewed balance behavior, content IDs,
  and approved source assets only. Dart and Flame runtime architecture are not
  ported line by line.

## 3. Migration Strategy

Unity gameplay is implemented natively. The following Flutter information may
be reused after review:

- stable content identifiers;
- the basic roles of the rookie constable, shaman, and mountain hunter;
- the progression shape of hwando, talisman, and horn bow;
- readable boss telegraph timings;
- the fallen general's charge, cone sweep, and summon pattern;
- tested asset-rights records;
- approved sprites that remain useful as temporary references.

The following are replaced:

- landscape UI and navigation;
- the five-minute wave schedule;
- Flame components and Flutter widgets;
- the twelve-weapon first-release scope;
- complex backend, account, purchase, telemetry, and cloud-save systems;
- authored combat buildings and collision-heavy stage layouts.

The Unity version becomes the release source. Flutter remains a behavioral
reference until the Unity slice passes its own tests and device validation.

## 4. Visual Direction

### 4.1 Style

Use original modular chibi pixel art with:

- a large head and compact body;
- clean, readable silhouettes;
- limited color ramps and hard nearest-neighbor pixel edges;
- Joseon-inspired hats, uniforms, ritual clothing, weapons, and accessories;
- cute but clearly hostile Korean-folklore monsters;
- ink navy, hanji cream, vermilion, muted jade, lantern amber, ghost cyan, and
  sealing gold;
- approachable presentation without becoming childish;
- no Japanese torii, samurai, ninja, Chinese imperial dragon motifs, European
  fantasy armor, photorealism, or copied SPUM art.

Combat terrain is a bright, unobstructed plain. It uses subtle grass and packed
earth, worn paths, flowers, pebbles, and paper scraps as non-colliding
decoration. Buildings, walls, fences, trees, and other path-blocking scenery
are excluded from the combat field.

### 4.2 Character Production Template

Every authored character part uses a transparent 64 x 64 pixel cell.

| Rule | Value |
| --- | --- |
| Character visible height | approximately 48 px |
| Foot anchor in top-left image coordinates | `(32, 56)` |
| Unity normalized pivot | `(0.5, 0.125)` |
| Pixels Per Unit | `32` |
| Authored directions | down, right, up |
| Left direction | mirrored from right |
| Texture filtering | Point |
| Mipmaps | disabled |
| Runtime compression | uncompressed RGBA32 for critical actor sheets |

The common layer order is:

1. shadow;
2. back equipment;
3. body and skin;
4. back hair;
5. lower clothing;
6. upper clothing;
7. armor;
8. face and eyes;
9. front hair;
10. Joseon hat or headwear;
11. left-hand weapon;
12. right-hand prop;
13. front overlay.

Every layer shares the same canvas, frame layout, foot anchor, and palette
slots. Palette slots cover skin, primary cloth, secondary cloth, accent, metal,
and outline ramps.

This is an asset-production specification, not a runtime character creator.
When a new character is requested, the approved parts are aligned and flattened
into a final transparent sprite sheet. Unity renders the flattened character
to minimize draw calls. Source layers and a JSON manifest remain available for
future revisions.

### 4.3 Character File Contract

Each character deliverable contains:

```text
ArtSource/Pixel/Characters/<character-id>/
  manifest.json
  palette.png
  layers/
    body.png
    face.png
    back-hair.png
    lower-clothing.png
    upper-clothing.png
    armor.png
    front-hair.png
    headwear.png
    left-weapon.png
    right-prop.png

Assets/JoseonHunter/Art/Characters/Runtime/<character-id>/
  <character-id>.png
  <character-id>.png.meta
```

The manifest records the character ID, cell size, pivot, authored directions,
animation ranges, exact palette, source prompt revision, and rights-ledger
entry.

## 5. Animation Contract

Characters do not perform attack animations. Their bodies remain in idle or
move state while weapons, projectiles, orbitals, trails, and impact effects
attack independently. This supports simultaneous automatic weapons without
interrupting movement.

Player hit feedback also requires no sprite frames. It uses:

1. an 0.08-second white flash;
2. an 0.10-second horizontal squash;
3. a short knockback;
4. 0.35 seconds of invulnerability blinking.

Boss-strength hits may additionally use reduced, optional camera shake and a
red screen-edge pulse.

### 5.1 Player Frames

| Animation | Frames | Directions | FPS | Total frames |
| --- | ---: | ---: | ---: | ---: |
| Idle | 4 | 3 | 6 | 12 |
| Move | 6 | 3 | 10 | 18 |
| Death | 8 | 1 | 10 | 8 |
| **Total per hero** | | | | **38** |

The final death frame holds. Attack and hit sheets are prohibited in the first
release unless a later approved special ability explicitly requires one.

### 5.2 Enemy Frames

Normal contact enemies use idle, move, and death. Hit feedback is procedural.
Ranged enemies use separate telegraph, projectile, and impact VFX. The boss
uses body motion only when its silhouette must communicate a pattern; damage
timing is still owned by pattern data, never by an animation event alone.

## 6. Navigation And Screen Structure

The lobby follows a conventional portrait mobile-game information hierarchy.
The persistent bottom navigation has five tabs:

1. **Shop (`상점`)**
2. **Equipment (`장비`)**
3. **Patrol (`출전`)** — larger center action
4. **Investigation Record (`수사록`)**
5. **Evolution (`진화`)**

Familiar navigation is intentional. The game does not copy another game's
graphics, exact layout, item economy, iconography, copy, or proprietary
content.

### 6.1 Shop

The `순라 보급소` displays:

- one daily free supply worth 50 coins;
- directly purchasable fragments for a chosen equipment slot;
- cosmetic clothing colors;
- complete price and item disclosure before purchase.

There are no random boxes, fake discounts, forced advertisements, or real-money
items in the first release.

### 6.2 Equipment

The screen shows the selected hero and four slots:

- weapon;
- clothing;
- hopae;
- shoes.

Equipment can be equipped and trained. Each action previews the resulting stat
change before confirmation.

### 6.3 Patrol

This is the default and center tab. It shows:

- selected hero;
- current stage and case;
- difficulty;
- best record;
- prominent patrol button.

### 6.4 Investigation Record

The investigation record contains:

- active supernatural case;
- clue progress;
- witnesses, traces, and relics;
- monster compendium;
- milestone rewards and discovered boss weaknesses.

### 6.5 Evolution

The evolution screen contains the twelve-node permanent training board. It
uses coins, previews stat changes, and supports free reset.

## 7. Run Flow

### 7.1 Timeline

| Time | Content |
| --- | --- |
| 0:00-0:45 | plague rat spirits and first weapon level |
| 0:45-1:30 | paper ghosts, first elite clue opportunity |
| 1:30-2:15 | dokkaebi and lantern spirits, mixed pressure |
| 2:15-2:45 | maximum normal-wave pressure |
| 2:45-3:00 | elite, boss warning, remaining-enemy cleanup |
| 3:00-4:00 maximum | fallen general boss fight |

The target is 8-10 level-up choices per successful run. The player can hold
three weapons and three support disciplines. Selection pauses combat.

### 7.2 Player Characters

| Hero | HP | Move speed | Starting weapon | Passive |
| --- | ---: | ---: | --- | --- |
| Rookie Constable | 105 | 4.0 | Hwando | contact damage -12% |
| Shaman | 85 | 3.7 | Talisman | talisman and seal damage +15% |
| Mountain Hunter | 90 | 4.4 | Horn Bow | critical chance +10 percentage points |

Move speed is expressed in Unity world units per second.

### 7.3 Experience

The first playtest curve is:

| Level reached | Required XP | Cumulative XP |
| --- | ---: | ---: |
| 2 | 5 | 5 |
| 3 | 8 | 13 |
| 4 | 12 | 25 |
| 5 | 18 | 43 |
| 6 | 26 | 69 |
| 7 | 36 | 105 |
| 8 | 48 | 153 |
| 9 | 62 | 215 |

If an owned weapon is not at maximum level, at least one of the three choices
must improve an owned weapon. Maximum-level entries and unavailable evolutions
must not appear.

## 8. Weapon Model

Player body animation and weapon behavior are independent. Every weapon owns
its cooldown, targeting, spawned presentation, collision, and damage data.

### 8.1 Hwando

The hwando creates a fast blade trail toward the closest target.

| Level | Change |
| --- | --- |
| 1 | damage 8, cooldown 0.72 s |
| 2 | damage and range increase |
| 3 | cooldown 0.60 s |
| 4 | damage 15 and wider arc |
| 5 | alternating left/right two-hit sequence |
| Evolution | `금문난무`, sequential five-direction slash |

Evolution requires Hwando level 5 and `금줄비법` level 2. Each attack can hit
one target only once even when multiple visual arcs overlap.

### 8.2 Talisman

The talisman seeks a target, attaches, and chains.

| Level | Change |
| --- | --- |
| 1 | damage 8, one chain |
| 2 | two chains |
| 3 | damage 12, three chains |
| 4 | four chains and more acquisition range |
| 5 | two talismans, five chains |
| Evolution | `오방귀마진`, up to three talismans and six chains |

Evolution requires Talisman level 5 and `신령먹` level 2. The final target
receives a 30% slow field.

### 8.3 Horn Bow

The horn bow fires toward the densest enemy line.

| Level | Change |
| --- | --- |
| 1 | damage 7, cooldown 1.05 s |
| 2 | damage 9, cooldown 0.95 s |
| 3 | one pierce |
| 4 | two arrows |
| 5 | two pierces |
| Evolution | `관귀천궁`, primary arrow plus two secondary arrows |

Evolution requires Horn Bow level 5 and `매의눈` level 2.

### 8.4 Support Disciplines

- `금줄비법`: geumjul damage and maximum length;
- `신령먹`: talisman count and persistent effects;
- `매의눈`: critical chance and piercing;
- `조식법`: all weapon cooldowns;
- `가벼운 짚신`: move speed;
- `넓은 호패`: pickup radius.

## 9. Signature Mechanic: Geumjul Sealing

The moving player leaves a short, luminous geumjul trail. A seal activates when
the current trail closes a valid local loop.

### 9.1 Geometry Rules

- Only the most recent 7 m or 4 seconds of trail is active.
- A valid loop has a minimum perimeter of 2.5 m.
- The maximum valid enclosed area is approximately a 3 m radius.
- Closure is detected when the current segment intersects or approaches a
  valid prior segment.
- Old points fade and stop participating in closure tests.
- A loop that exceeds the length, duration, or area limit dissipates.
- Map boundaries never close a loop.
- Only enemies inside the validated polygon are affected.

### 9.2 Base Effect

- base seal damage: 20;
- normal-enemy bind: 1.2 seconds;
- boss damage multiplier: 35%;
- bosses cannot be bound.

### 9.3 Use-Based Mastery

Geumjul mastery does not consume a normal level-up choice.

| Successful closures | Reward |
| ---: | --- |
| 0 | damage 20, 7 m trail, 1.2 s bind |
| 3 | damage 26 and more forgiving closure |
| 8 | 8.5 m maximum length and +15% area |
| 14 | choose Fire Mark or Ice Bind |
| 20 | Five-Color Barrier, 40-damage chain explosion |

This makes movement skill a separate source of in-run growth.

## 10. Enemy And Boss Baseline

| Enemy | HP | Speed | Damage | XP | Role |
| --- | ---: | ---: | ---: | ---: | --- |
| Plague Rat Spirit | 8 | 1.8 | 6 | 1 | one-hit swarm |
| Paper Ghost | 18 | 1.9 | 8 | 1 | direct chase |
| Straw Effigy | 38 | 1.1 | 13 | 3 | slow shield |
| Lantern Spirit | 25 | 1.3 | 9 | 3 | ranged, 0.7 s warning |
| Dokkaebi | 22 | 1.4 | 10 | 2 | dash, 0.65 s warning |
| Fallen General | 900 | 0.8 | 20 | boss | charge, cone, summon |

Before the boss, active-enemy caps progress through 28, 36, 48, and 64. The
boss encounter reduces the cap to 36.

Safety targets:

- the level-one hwando kills a plague rat spirit in one hit;
- continuously moving non-dash enemies remain below 50% of player speed;
- uninterrupted contact takes at least four seconds to defeat the player;
- all dangerous attacks have at least 0.6 seconds of visual warning;
- gameplay outcomes do not depend on optional VFX pool capacity.

### 10.1 Fallen General

- 900 HP;
- slow pursuit;
- cavalry charge with a 0.75-second line warning;
- commander's sweep with a 0.60-second 90-degree cone warning;
- one vengeful-spirit summon at 40% HP;
- enrage after 25 seconds, increasing movement and pattern frequency by 25%.

## 11. Equipment And Economy

### 11.1 Equipment

There are four slots and twelve first-release items:

| Slot | Items | Primary purpose |
| --- | --- | --- |
| Weapon | Constable Hwando, Shaman Talisman, Mountain Horn Bow | starting weapon |
| Clothing | Patrol Uniform, Hemp Robe, Mountain Hunting Garb | defense, cooldown, speed |
| Hopae | Patrol Hopae, Requiem Hopae, Tracking Hopae | coins, seal, critical |
| Shoes | Straw Shoes, Leather Shoes, Unhye | movement, knockback, evasion |

Quality tiers are:

1. common (`평범`);
2. tempered (`단련`);
3. masterwork (`명품`);
4. spirit-bound (`영물`).

The game never requires three identical items to be merged. A selected item's
fragments raise its quality; coins raise its level. Spirit-bound quality adds a
small unique trait and a cosmetic effect.

### 11.2 Coin Rewards

| Reward source | Victory | Defeat |
| --- | ---: | ---: |
| Patrol wage | 40 | 20 |
| Enemy defeats | 30-50 | 10-35 |
| Boss seal | 100 | 0 |
| First solution bonus | 50 | 0 |
| Expected total | 170-220 | 40-90 |

Early equipment levels cost 80-180 coins. Later first-release equipment levels
cost 220-400. Evolution nodes cost 100-450. Cosmetic clothing colors cost 600.
The target is one meaningful improvement every one or two runs and a cosmetic
unlock every three or four successful runs.

An abandoned run still grants time- and kill-proportional rewards.

### 11.3 Permanent Evolution

The twelve-node board upgrades health, damage, movement, experience gain, and
pickup radius. Each numerical category is capped near a 10% final increase.
The board can be reset for free.

Numerical progression remains deliberately light. Major progress comes from
new heroes, weapons, evolutions, cases, difficulties, and appearances.

## 12. Investigation Record

The first case is `월하 폐관의 망령`.

It has nine unique clues split across witness, trace, and relic categories.
Clue rewards prefer an undiscovered clue; random duplicates are prohibited
until the case is complete. The first patrol guarantees one new clue. Elites
and the boss provide additional opportunities.

| Progress | Reward |
| ---: | --- |
| 3/9 | first fallen-general weakness and expanded compendium entry |
| 6/9 | hwando evolution recipe and a selectable investigation policy |
| 9/9 | case solved, Shaman unlocked, hard difficulty opened |

Investigation is the game's lore and unlock layer. It does not replace the
familiar shop, equipment, patrol, and evolution screens.

## 13. Tutorial And Accessibility

The first-run tutorial completes within 60 seconds:

1. move using a floating joystick;
2. enclose three training spirits with geumjul;
3. choose a level-up card;
4. leave a red telegraph.

There is no mandatory tutorial video or long text page. Completing the action
advances the lesson. Later runs skip it automatically; Settings can replay it.

Accessibility settings include:

- UI scale: 100%, 115%, 130%;
- joystick scale: 80%, 100%, 120%;
- screen shake: 0%, 50%, 100%;
- reduced flashing;
- high-contrast enemy outlines;
- geumjul states distinguished by pattern as well as color;
- separate vibration, music, and SFX controls;
- 30 FPS battery mode and 60 FPS default mode;
- floating joystick that can begin anywhere in the lower play region;
- safe-area layout;
- minimum 48 dp touch targets.

## 14. Save Data And Failure Recovery

The versioned local save contains:

- selected hero and equipment;
- equipment levels, quality, and fragments;
- coins;
- evolution board;
- case clues and compendium;
- unlocked content;
- best results;
- settings and tutorial state.

Writes use:

```text
serialize in memory
  -> write temporary file
  -> validate checksum and schema
  -> replace current save
  -> retain previous valid save as backup
```

Autosave occurs after a run, equipment or evolution purchase, setting change,
and application pause.

Load order is current save, backup save, then safe defaults. A corrupt save
must never crash the game. Recovery displays a concise notice. Reset Progress
requires two explicit confirmations.

Failure handling:

- missing sprite: use an approved silhouette fallback and log the content ID;
- missing audio: continue through a silent backend;
- invalid content item: exclude it and guarantee the default hwando;
- scene-load failure: return to Bootstrap with an error code;
- insufficient storage: cancel the state-changing action and preserve the
  previous save;
- application background: pause combat and save.

## 15. Architecture

Keep the established assemblies and one-way dependencies:

```text
Domain <- Content <- Runtime <- Presentation
Domain <- Infrastructure
```

### 15.1 Domain

Pure C# rules:

- stats and damage;
- run clock;
- experience;
- weapon level and evolution eligibility;
- geumjul polygon validation and affected-target selection;
- rewards;
- equipment;
- investigation;
- save schema and migration decisions.

### 15.2 Content

ScriptableObject authoring assets validated into immutable runtime definitions:

- characters;
- weapons and support disciplines;
- enemy and boss patterns;
- wave phases;
- equipment;
- evolution nodes;
- cases and clues;
- rewards;
- visual and audio references.

Balance values must not be embedded in MonoBehaviours.

### 15.3 Runtime

Unity-facing orchestration:

- input and movement;
- weapon cooldowns and targeting;
- geumjul trail sampling;
- spatial queries;
- spawning and waves;
- object pools;
- run lifecycle;
- pause and focus handling.

### 15.4 Presentation

- sprites and animation;
- weapon trails, projectiles, telegraphs, impacts, and sealing VFX;
- camera and pixel-perfect presentation;
- HUD and five-tab menus;
- audio, haptics, flashes, and shake;
- tutorials and accessibility rendering.

### 15.5 Infrastructure

- local save repository;
- settings repository;
- Android lifecycle adapter;
- diagnostic log.

No purchase, entitlement, advertisement, telemetry, account, cloud-save, or
other unused third-party service interface or SDK is shipped in the first
release.

## 16. Asset Inventory

### 16.1 Characters

- three 38-frame playable-character sheets;
- three portraits;
- three locked silhouettes;
- source layer and palette sets for each;
- four cosmetic palette variants for the rookie constable.

### 16.2 Enemies

- five normal-enemy sheets;
- fallen-general boss sheet;
- elite palette and size variants where validated;
- six compendium portraits.

### 16.3 Weapons And VFX

- hwando trail and contact;
- talisman flight, attach, chain, and explosion;
- horn-bow arrow, pierce, contact, and arrow-rain evolution;
- geumjul draw, fade, close, burst, Fire Mark, Ice Bind, and Five-Color
  Barrier;
- enemy death, experience pickup, level up, boss warning, boss charge, cone,
  and summon;
- damage feedback and optional reduced-flash variants.

### 16.4 Stage

- sixteen 32 px grass and earth base tiles;
- twelve worn-path and transition variants;
- twenty-four non-colliding ground decals;
- three boss-area ground marks;
- one edge-fog set;
- no movement-blocking environment art.

### 16.5 UI

- combat HUD;
- floating joystick;
- three-choice level-up modal;
- boss health and warning;
- pause and Settings;
- four tutorial prompts;
- victory and defeat results;
- five persistent navigation tabs;
- shop, equipment, patrol, investigation, evolution screens;
- equipment slot, quality, coin, clue, and stat icons;
- safe-area frames and scale variants.

### 16.6 Audio

- lobby, combat, and boss music;
- ten weapon, seal, impact, and enemy SFX;
- five UI SFX;
- victory, defeat, evolution, and boss-arrival cues;
- a silent fallback implementation;
- rights-ledger record for every shipped file.

### 16.7 Store Assets

- 512 x 512 app icon;
- 1024 x 500 feature graphic;
- six portrait screenshots;
- splash image;
- Korean and English logo;
- credits and privacy screen.

### 16.8 Asset Approval Gate

Gameplay implementation does not begin until:

- the complete release asset manifest exists;
- every launch-critical character, enemy, weapon, VFX, stage, UI, audio, and
  store-art batch has a reviewable output;
- the user has approved every batch;
- dimensions, frame counts, pivots, palettes, licenses, and destination paths
  pass automated validation.

The implementation plan may be written before this gate, but execution remains
blocked at the asset-production tasks until the gate passes. Pure geometry,
balance, and save rules may use deterministic test data in the plan; gameplay
scenes do not adopt unapproved visual assets.

## 17. Performance Targets

- 60 FPS target on representative mid-range Android hardware;
- 30 FPS floor and explicit battery mode;
- maximum 64 active enemies before the boss;
- maximum 36 active enemies during the boss;
- pools for enemies, projectiles, pickups, damage indicators, and repeated VFX;
- zero per-frame LINQ and avoidable allocations in active combat loops;
- spatial partitioning for targeting and geumjul affected-target queries;
- optional VFX degrade before combat logic;
- three consecutive complete device runs without increasing retained memory;
- app download target below 150 MB.

## 18. Android Release Requirements

Build settings:

- portrait orientation;
- minimum API 26;
- target API 36;
- ARM64;
- IL2CPP;
- Android App Bundle;
- Play App Signing;
- 16 KB native-page compatibility;
- no unnecessary dangerous permissions.

As of 2026-07-26, Google Play requires API 36 for new apps and updates starting
2026-08-31:

<https://support.google.com/googleplay/android-developer/answer/11926878?hl=en-GB_ALL>

New and updated apps targeting Android 15 or later must support 16 KB page
sizes:

<https://developer.android.com/guide/practices/page-sizes>

Google Play uses App Bundles and reports a 200 MB maximum compressed download
for the generated APK for one device:

<https://support.google.com/googleplay/android-developer/answer/9859152?hl=en>

The release listing requires:

- app name and descriptions;
- screenshots and feature art;
- content rating;
- audience and advertising declarations;
- Data safety form;
- a publicly accessible privacy policy;
- in-app privacy-policy access;
- complete rights records.

Even a game that collects no user data must complete Data safety and provide a
privacy policy:

<https://support.google.com/googleplay/android-developer/answer/10787469?hl=en>

### 18.1 Publishing Schedule Constraint

For a personal Play developer account created after 2023-11-13, Google requires
at least twelve opted-in closed testers for fourteen consecutive days before
production access can be requested:

<https://support.google.com/googleplay/android-developer/answer/14151465>

Therefore:

- one week can produce a release-candidate AAB, internal testing release,
  listing material, and policy forms;
- public production submission in week one is possible only if the Play
  account is already eligible;
- public availability cannot be guaranteed because Google review is external
  and may take additional time.

## 19. Testing

### 19.1 EditMode

- damage and cooldown calculations;
- experience curve and choice filtering;
- weapon evolution recipes;
- geumjul trail expiry, loop validity, area cap, polygon inclusion, boss
  modifier, and mastery thresholds;
- wave interpolation and active caps;
- boss pattern sequencing and warnings;
- reward calculations;
- equipment training and quality;
- investigation non-duplication and milestones;
- save migration, checksum, backup, and fallback;
- content and asset-reference validation.

### 19.2 PlayMode

- Bootstrap to lobby;
- five-tab navigation;
- equipment changes;
- patrol start;
- touch and keyboard movement;
- independent simultaneous weapons;
- level-up pause and resume;
- geumjul visual-to-domain integration;
- boss arrival and victory/defeat;
- results and save/reload;
- app focus pause;
- missing sprite and audio fallback;
- tutorial completion and replay;
- accessibility settings.

### 19.3 Android

- Android 8/API 26, Android 12, Android 15, and Android 16 coverage;
- representative phone and tablet aspect ratios;
- touch, safe area, focus loss, suspend/resume, audio interruption;
- 16 KB page-size verification using bundletool and a compatible environment;
- signed AAB internal-test installation;
- three consecutive complete runs with frame-time, memory, and temperature
  capture;
- Play pre-launch report;
- store listing and Data safety consistency.

## 20. Acceptance Criteria

The vertical slice is release-shaped when:

- a new user completes the tutorial and starts a patrol without outside help;
- the game is portrait-only and all primary controls fit supported safe areas;
- three heroes, three weapon families, five normal enemies, and the fallen
  general exist with approved art;
- the player can close local geumjul loops and cannot exploit map boundaries
  or expired trail segments;
- at least one weapon can evolve in a normal successful run;
- the boss appears at 3:00 and the run ends by 4:00;
- victory and defeat both award valid progress;
- the five menu tabs, equipment, shop, investigation, and evolution loop work;
- save corruption recovers without a crash;
- all required EditMode and PlayMode tests pass;
- a signed API-36 ARM64 IL2CPP AAB passes 16 KB and internal-test validation;
- representative device runs meet the performance floor;
- shipped assets have recorded provenance;
- store and privacy materials are ready;
- any Play-account or review limitation is reported as an external release
  gate rather than hidden.

## 21. Seven-Day Execution Target

This is an aggressive engineering target, not a guarantee of public store
availability:

| Day | Target |
| --- | --- |
| 1 | final specification and production-asset approval |
| 2 | movement, input, weapons, waves |
| 3 | geumjul, boss, run lifecycle |
| 4 | menus, equipment, progression, save |
| 5 | content integration, balance, audio |
| 6 | device, performance, recovery, and accessibility QA |
| 7 | signed AAB, internal test, listing and submission preparation |

Scope is protected by excluding online accounts, cloud save, real-money
purchases, advertisements, multiple stages, and additional bosses from the
first release.
