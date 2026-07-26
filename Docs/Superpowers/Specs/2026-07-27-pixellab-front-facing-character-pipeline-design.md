# PixelLab Front-Facing Character Pipeline Design

Date: 2026-07-27  
Status: user-approved visual direction; written-spec review pending

## Goal

Replace the rejected character batch with original Joseon folk-fantasy pixel
characters that have the cute, compact readability of the user's supplied
reference without copying or redistributing any reference asset.

PixelLab is the primary pixel-art generator. Unity receives only approved,
validated PNG assets.

## Locked Visual Direction

- Every playable character is approximately two heads tall.
- The head occupies about half of the standing silhouette.
- Faces use large readable eyes, a tiny mouth, and minimal pixel clusters.
- Bodies, hands, and feet are short and compact.
- Silhouettes use a firm dark outline.
- Hats, hair, clothing, and carried equipment are exaggerated enough to remain
  identifiable at gameplay scale.
- Designs are original Joseon folk-fantasy designs. The supplied SPUM screenshot
  is a proportion and readability reference only; no source pixels, names,
  characters, or derived artwork enter the production repository.

## View and Animation Contract

Characters always face the camera. Moving up, down, left, or right does not swap
to side-facing or back-facing artwork.

The launch sheet contract is:

- cell size: 64 x 64 RGBA PNG;
- pivot: bottom center, normalized `(0.5, 0.125)`;
- pixels per unit: 32;
- idle: 2 frames;
- move: 4 frames;
- death: 6 frames;
- attack: no character animation;
- total: 12 frames, packed in animation order;
- movement style: alternating feet, one-to-two-pixel horizontal sway, and a
  small vertical bounce while the face remains front-facing;
- weapons and attack effects animate as independent objects.

Left-facing movement may mirror only independent equipment when necessary.
The character body itself remains the same front-facing art in every direction.

## PixelLab Production Architecture

PixelLab's official MCP endpoint is the preferred connection:

`https://api.pixellab.ai/mcp`

Authentication uses the PixelLab account's bearer token. The token must be kept
in local Codex configuration or an environment-backed secret and must never be
written to the repository, reports, prompts, screenshots, or test output.

The pipeline is:

1. Generate one transparent 64 x 64 front-facing base sprite.
2. Keep the same character identity, palette, outline, prompt contract, and
   reference assets for every related generation.
3. Generate a four-frame front-facing walk from the approved base sprite.
4. Generate idle and death only after the base and walk are visually approved.
5. Normalize the returned frames to the 12-frame Unity sheet contract.
6. Run pixel-grid, alpha, palette, silhouette, frame-difference, and importer
   checks.
7. Present a light/dark contact sheet for explicit user approval.
8. Import only approved assets into Unity.

PixelLab creates the source artwork. Deterministic tooling may crop, pack,
validate, remove stray alpha, and make minimal one-pixel cleanup corrections;
it must not redraw the character into a disconnected substitute.

## Free-Trial Pilot

The currently active PixelLab trial provides 40 fast generations, followed by
five slower daily generations. The pilot is intentionally limited to the rookie
constable:

- one base-sprite generation;
- one walk-animation generation;
- up to four visual correction attempts;
- maximum pilot budget: six fast generations unless the user approves more.

No shaman, mountain hunter, enemy, or environment generation starts before the
rookie constable base and walk are approved.

After approval, the rookie constable becomes the master style reference for the
remaining heroes and enemies.

## Character Identity Contract

The rookie constable must preserve these traits across all frames:

- oversized black Joseon patrol hat;
- navy patrol uniform;
- red waist accent;
- small wooden hopae;
- compact hwando silhouette;
- warm skin palette and dark hair;
- two-head-tall proportions.

Eyes, hat width, outline thickness, clothing colors, hopae placement, and sword
placement may not drift between frames beyond intentional movement.

## Review Gates

The character pilot has two separate approval gates:

1. base sprite approval;
2. four-frame movement approval.

Every review board must show:

- sprite at native scale and nearest-neighbor enlargement;
- light and dark backgrounds;
- all animation frames;
- palette strip;
- transparent-background proof;
- generation provenance without secrets;
- remaining free-generation budget.

Rejected assets remain `pending` and are not used by Unity gameplay.

## Validation

Automated validation must reject:

- dimensions other than 64 x 64 per frame;
- non-RGBA output or an opaque background;
- colors outside the locked palette tolerance;
- inconsistent silhouette bounds;
- identical move frames;
- head-height ratio outside the approved two-head-tall tolerance;
- more than one-pixel unintended facial-feature drift;
- unsupported side-facing, back-facing, hit, or attack clips;
- missing generation provenance or approval state.

Unity EditMode tests cover the new 12-frame importer contract and ensure the old
directional 38-frame contract is not accepted for launch characters.

## Failure Handling

- If MCP authentication fails, stop without exposing the token and repair the
  local connection.
- If a PixelLab job fails, preserve its job identifier and retry only within the
  approved pilot budget.
- If visual consistency fails twice, reuse the approved base as a stronger image
  reference instead of rewriting the character description.
- If PixelLab cannot produce a usable four-frame walk on the free tier, retain
  PixelLab's approved base sprite and create the four deterministic movement
  frames through pixel-preserving transforms and minimal manual cleanup.

## Out of Scope for the Pilot

- side or back character views;
- character attack animation;
- runtime character-customization UI;
- paid PixelLab subscription;
- batch generation of all heroes, enemies, or environments;
- changes to gameplay systems.

