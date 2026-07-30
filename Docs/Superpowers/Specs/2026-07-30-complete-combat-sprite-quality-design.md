# Complete Combat Sprite Quality Design

## Goal

Remove every legacy-looking sprite that is visible during the current combat
loop. All replacements follow the approved simplified Joseon mobile-survivor
style: a dark outline, large color masses, restrained detail, and stable
animation silhouettes.

## Scope

The current runtime references define the boundary.

- Normal enemies: dokkaebi, sakkat specter, vengeful spirit
- Elite: dokkaebi captain
- Boss: fallen general
- Pickups: coin, experience spirit flame, treasure chest
- Weapons: Fan, Frost, Gakgung, Jangseung, Singijeon, Talisman, Thunder
- Existing approved pack retained: Han Yeonhwa, bandit, plague rat, Hwando

Unused concepts, lobby illustrations, portraits, potential icons, masks, and
future heroes are outside this combat-pass boundary.

## Asset contracts

- Normal combat sprites and weapon frames: transparent 96×96 PNG, PPU 64
- Elite frames: transparent 112×112 PNG, PPU 64
- Boss frames: transparent 128×128 PNG, PPU 64
- Pickups: transparent 64×64 PNG, PPU 64
- Point filtering, mipmaps disabled, uncompressed texture data
- One frame per PNG with a bottom-centered pivot for combatants
- Weapon effects use centered pivots
- 2–3 px dark outline at 96 px; 3–4 px at 112–128 px
- Four to six dominant colors per material group
- No noisy one-pixel decoration that disappears at the gameplay camera

## Animation contracts

- Dokkaebi, sakkat specter, vengeful spirit: six walk frames each
- Dokkaebi captain: four idle and six walk frames
- Fallen general: four idle and eight walk frames
- Pickups remain static until the runtime gains a pickup animation owner
- Weapon prefixes keep their existing exact frame counts and filenames

Generated animation frames must preserve the same object identity. Rigid
projectiles use one PixelLab-authored base repeated across frames when Unity
already supplies rotation or motion; this prevents AI morphing.

## Visual identities

- Dokkaebi: teal skin, dark indigo trousers, one horn pair, compact club
- Sakkat specter: straw hat, charcoal robe, pale cyan ghost edge
- Vengeful spirit: pale face, black hair mass, faded plum-and-white hanbok
- Dokkaebi captain: broader teal body, red commander sash, iron club
- Fallen general: dark lamellar armor, burnt orange plates, blue ghost flame
- Pickups: gold coin, emerald-cyan spirit flame, red lacquer treasure chest

Each weapon keeps a unique visual grammar:

- Fan: pale wind crescents plus violet-gold lightning
- Frost: cyan flask, expanding ice growth, angular white-blue shatter
- Gakgung: compact arrow, gold aim glint, tan-white impact splinters
- Jangseung: brown-red guardian post, ground rise, heavy orange strike
- Singijeon: dark rocket with red flame, ember trail, orange-red explosion
- Talisman: yellow paper seal, red ink, binding ribbon, gold seal pulse
- Thunder: navy bomb, yellow warning, blue ground current, white-blue blast

## Production workflow

PixelLab generates one approved base for each character, pickup, and weapon
prefix. Animation jobs derive only deformable motion from those bases.
Rejected candidates remain out of runtime folders. Accepted job ids, prompts,
and seeds are recorded under `ArtSource/Pixel/SimplifiedQuality/CompletePack`.

Generation is performed in bounded batches of at most four jobs. Completed
outputs are downloaded before the next batch so the workstation never holds a
large generation payload in memory.

## Acceptance criteria

- No current combat reference points at a legacy 64×64 normal enemy.
- Every motion-library combatant meets its canvas and frame-count contract.
- Every weapon-polish prefix preserves its current exact frame count.
- Normal enemies read at approximately player height; elites read 1.2–1.3×
  normal height; the boss reads 1.7–1.9× normal height.
- Character identities remain stable across animation frames.
- Rigid projectiles do not bend or change design between frames.
- Focused EditMode and PlayMode tests pass.
- A fresh 720×1280 gameplay capture shows no obvious legacy-style combatant
  among the current runtime roster.

