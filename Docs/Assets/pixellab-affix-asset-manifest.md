# Affix Jackpot PixelLab Atlas Manifest

This manifest is the spend lock for the run-only weapon-affix presentation set.  Each approved atlas is exactly 256 x 128 pixels, RGBA with only 0/255 alpha, hard one-pixel edges, transparent outside the documented cells, and no readable text, humanoids, panels, or UI-shaped weapon parts.  The palette and outline language are locked to `ArtSource/Pixel/Weapons/style-lock/style-lock.png` and `ArtSource/Pixel/Palettes/joseon-hunter-master.png`.

## Shared constraints

- 32 pixels per Unity unit; point filter, no mipmaps, uncompressed, readable.
- Cells are `(x, y, width, height)` measured from the atlas lower-left in Unity texture coordinates.
- A `damage-active` cell has an explicit authored binary mask. `decorative-only` cells never have a gameplay mask.
- Mask pixels are strictly a subset of the source's opaque alpha: never copy source alpha wholesale. Glow, smoke, trails, telegraphs, poison droplets, rarity bursts, sparks, and all UI cells are excluded.

## `slot-kit.png` — UI-only, 4 x 2 cells of 64 x 64

| Cell | Bounds | Use | Contact |
| --- | --- | --- | --- |
| reel_frame | `(0,64,64,64)` | empty slot-machine reel surround | decorative-only |
| standard_frame | `(64,64,64,64)` | standard affix border | decorative-only |
| high_frame | `(128,64,64,64)` | high affix border | decorative-only |
| perfect_frame | `(192,64,64,64)` | perfect affix border | decorative-only |
| jackpot_burst_1 | `(0,0,64,64)` | one-line reveal burst | decorative-only |
| jackpot_burst_2 | `(64,0,64,64)` | two-line reveal burst | decorative-only |
| jackpot_burst_3 | `(128,0,64,64)` | three-line reveal burst | decorative-only |
| rarity_flash | `(192,0,64,64)` | final rarity flash | decorative-only |

## `status-symbols.png` — UI-only, 4 x 2 cells of 64 x 64

| Cell | Bounds | Use | Contact |
| --- | --- | --- | --- |
| poison | `(0,64,64,64)` | poison status icon | decorative-only |
| burn | `(64,64,64,64)` | burn status icon | decorative-only |
| frost | `(128,64,64,64)` | frost status icon | decorative-only |
| bleed | `(192,64,64,64)` | bleed status icon | decorative-only |
| armor_break | `(0,0,64,64)` | armor-break status icon | decorative-only |
| seal_transfer | `(64,0,64,64)` | seal-transfer status icon | decorative-only |
| lightning_mark | `(128,0,64,64)` | lightning-mark status icon | decorative-only |
| experience | `(192,0,64,64)` | experience status icon | decorative-only |

## `potential-parts-a.png` — gameplay, 4 x 3 cells of 64 x 32

| Potential ID | Bounds | Visible part | Contact |
| --- | --- | --- | --- |
| hwando_venom_fang | `(0,96,64,32)` | exposed curved blade with a tiny poison droplet | damage-active: blade shadow body only; droplet excluded |
| hwando_returning_afterimage | `(64,96,64,32)` | exposed flying blade and separate violet afterimage | damage-active: blade body only; afterimage excluded |
| hwando_flying_blade_dance | `(128,96,64,32)` | three distinct exposed blades | damage-active: blade bodies only; sparks excluded |
| gakgung_armor_break_arrowhead | `(192,96,64,32)` | split-arrow iron arrowhead | damage-active: arrowhead and shaft body only |
| gakgung_split_fletching | `(0,64,64,32)` | three separated split arrows | damage-active: arrow bodies only |
| gakgung_full_draw | `(64,64,64,32)` | fast arrow with compact wind ring | damage-active: arrow body only; ring excluded |
| talisman_five_element_cycle | `(128,64,64,32)` | talisman with five tiny elemental motes | damage-active: talisman paper body only; motes excluded |
| talisman_seal_transfer | `(192,64,64,32)` | talisman and a seal mark | damage-active: talisman paper body only; mark excluded |
| talisman_vengeful_ghost_burst | `(0,32,64,32)` | talisman and small ghost-flame body | damage-active: ghost-flame core only; smoke excluded |
| thunder_earth_current | `(64,32,64,32)` | ground crack with electric core | damage-active: ground-crack lightning core only |
| thunder_overcharged_core | `(128,32,64,32)` | bomb core with outer charge glow | damage-active: core body only; glow excluded |
| thunder_lightning_rod | `(192,32,64,32)` | lightning bolt striking rod mark | damage-active: bolt core only; rod mark excluded |

## `potential-parts-b.png` — gameplay, 4 x 3 cells of 64 x 32

| Potential ID | Bounds | Visible part | Contact |
| --- | --- | --- | --- |
| jangseung_ghost_face | `(0,96,64,32)` | rotating ward edge with ghost face inset | damage-active: rotating ward edge only |
| jangseung_four_direction_barrier | `(64,96,64,32)` | four ward edges | damage-active: each ward edge only |
| jangseung_guardian_descent | `(128,96,64,32)` | ward edge and descending guardian seal | damage-active: rotating ward edge only; seal excluded |
| singijeon_powder_trail | `(192,96,64,32)` | rocket body with powder trail | damage-active: rocket body only; trail excluded |
| singijeon_submunition_split | `(0,64,64,32)` | three submunition rockets | damage-active: submunition bodies only |
| singijeon_chain_ignition | `(64,64,64,32)` | rocket body and ignition spark | damage-active: rocket body only; spark excluded |
| frost_crack_mark | `(128,64,64,32)` | frost field core with crack mark | damage-active: frost spread core only; mist excluded |
| frost_spread | `(192,64,64,32)` | expanding frost core | damage-active: frost spread core only |
| frost_mist | `(0,32,64,32)` | frost core under a mist halo | damage-active: frost core only; mist excluded |
| fan_vacuum_edge | `(64,32,64,32)` | fan wind edge and bleed droplets | damage-active: wind edge only; droplets excluded |
| fan_distant_thunder | `(128,32,64,32)` | fan wind edge with chain lightning | damage-active: chain-lightning core only; wind excluded |
| fan_returning_chain | `(192,32,64,32)` | returning wind edge and chain arc | damage-active: chain-lightning core only; arc sparks excluded |

## Prompt records

The checked-in `prompt.md` beside each approved atlas is the authoritative request. Each provenance record has the PixelLab job ID, requested model, timestamp, cost, review decision, dimensions, alpha inspection, and approved destination. No prompt or source atlas may be replaced after approval without a rejected-atlas record and its single permitted targeted retry.
