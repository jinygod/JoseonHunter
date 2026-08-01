# Infinite Battlefield, Wave Roster, and Loading Flow Design

## Goal

Replace the finite 72-by-48 battlefield presentation with an endless, deterministic pixel battlefield; make enemy density and species visibly change by wave; and turn the empty Bootstrap scene into a real loading transition. The implementation must preserve the optimized combat path and remain suitable for the portrait Android target.

## Confirmed player experience

- The player can travel in any direction without reaching a boundary or revealing the camera clear color.
- The ground uses the approved **A: Joseon folk field** direction: muted mugwort-green soil, sparse grass, small stones, pale flowers, and discarded ward-paper fragments.
- The opening wave contains plague rats only.
- Later waves introduce new species in recognizable groups instead of selecting every normal enemy sprite uniformly at random.
- Enemy pressure rises quickly enough that the battlefield feels populated, while active normal combatants never exceed the mobile limit of 140.
- The application shows a real loading presentation before Gameplay becomes visible.

## Current causes

`BattlefieldTilePresenter` currently builds one world-anchored 72-by-48 ground renderer. `FirstPlayableController` lets the player and camera move without bounds, so eventually the camera sees its solid-color clear background.

The domain already defines species IDs per phase in `WaveSchedule`, but `FirstPlayableController.ChooseNormalEnemySprite` ignores that schedule and randomly selects from the entire normal-enemy sprite array. Spawn pacing and wave composition are therefore disconnected. The spawn geometry also uses the independent 8.5-unit spawn profile while the visible portrait camera has an orthographic size of 18, so enemies are not consistently placed beyond the actual visible viewport.

Bootstrap and Lobby contain only placeholder scene roots. Gameplay contains the main camera, `FirstPlayableController`, and EventSystem. The field, player, enemies, weapons, pickups, and most UI are composed at runtime, which explains why the edit-time scene hierarchy looks mostly empty.

## Chosen architecture

### 1. Deterministic 3-by-3 battlefield chunks

Create a focused battlefield chunk presenter owned by the Gameplay runtime. It maintains exactly nine reusable 32-by-32-world-unit chunks centered on the player's current integer chunk coordinate.

When the player crosses a chunk boundary, only chunks whose required coordinates changed are reassigned and rebuilt. No world-sized history is retained. The chunk coordinate is combined with a fixed battlefield seed to select decoration placement, rotation, and mirroring, so returning to a coordinate reconstructs the same appearance.

Each chunk contains:

- one base ground renderer using a seamless or visually seam-safe pixel tile;
- a small, bounded number of pooled decoration renderers;
- no gameplay colliders, navigation data, or persistent mutable state.

The base ground must remain low contrast. Decoration density is intentionally sparse so enemies, experience drops, weapon effects, and Geumjul remain readable. Chunk seams must not be visible at the supported portrait aspect ratios.

The chunk prefab belongs under `Assets/JoseonHunter/Prefabs/World`. Runtime ownership remains explicit: Gameplay creates the nine instances once and recycles them. It must not instantiate or destroy chunk objects every frame.

### 2. Pixel ground asset

Create a new top-down pixel-art ground asset following the approved Joseon folk-field direction. It must be orthographic, contain no perspective horizon, contain no characters or large landmarks, and avoid baked lighting that makes repetition obvious.

The imported texture uses the project's pixel-art import profile: point filtering, no mipmaps, no compression that blurs pixels, and no unintended alpha gaps. Small grass, stone, flower, and ward-paper details can be separate decoration sprites when that produces cleaner repetition than baking them into the base tile.

The existing occult battlefield asset remains available for reference and rollback; it is not overwritten.

### 3. Wave roster director

Introduce a deterministic, testable wave-roster component in the Domain/Runtime boundary instead of adding more random branches to `FirstPlayableController`. It consumes elapsed run time and returns:

- current phase;
- active-enemy cap;
- weighted normal species roster;
- continuous spawn batch size and interval;
- optional pack event definition;
- elite probability.

Approved phase contract for the 180-second prototype:

| Time | Composition | Active cap | Pack behavior |
|---|---|---:|---|
| 0-45 s | 100% plague rat | 72 | Rat groups build pressure quickly; no other normal species |
| 45-90 s | 65% plague rat, 35% vengeful spirit | 104 | A 10-14 spirit pack enters at a bounded, seed-varied interval |
| 90-135 s | 20% plague rat, 45% vengeful spirit, 35% sakkat specter | 128 | Alternating spirit/specter packs |
| 135-165 s | sakkat specter, dokkaebi, and bandit mixture | 140 | Larger mixed packs with bounded directional variation |
| 165-180 s | boss warning | reduced normal pressure | No new pack event; preserve warning readability |
| 180 s onward | final boss | boss-owned pressure | Normal spawning follows the existing boss-state contract |

Pack timing and entry side vary inside authored ranges using the run seed. Species weights, minimum/maximum pack sizes, phase boundaries, and the active cap are fixed rules. This creates a pattern that is recognizable but not identical every run.

Continuous spawns and pack spawns share the same active-cap budget. Treasure chests do not determine species selection. Boss and mid-boss rules remain separate from the normal roster.

Normal enemy IDs map explicitly to the existing five normal enemy sprites:

- `plague_rat`
- `bandit`
- `dokkaebi`
- `sakkat_specter`
- `vengeful_spirit`

Unknown or missing IDs fall back to the plague-rat sprite in player builds and produce a development warning once per missing ID. A malformed roster must not silently select from every sprite.

### 4. Viewport-safe spawning

Spawn positions use the real Gameplay camera viewport converted to world bounds, expanded by an authored margin that also accounts for the selected enemy renderer bounds. This replaces the current mismatch between the 18-unit visible camera and the separate 8.5-unit spawn profile.

Every newly spawned normal enemy must begin fully outside the camera viewport, then move inward. Pack members use an arc or lane around one selected perimeter side with separation-safe spacing. A pack never teleports onto the player or overlaps the loading transition.

### 5. Bootstrap loading presentation

Bootstrap becomes the real application entry scene. A small loading prefab under `Assets/JoseonHunter/Prefabs/UI` contains:

- game title;
- a subtle spirit-flame or ink-pulse animation;
- a thin brush-stroke progress bar driven by Unity's actual asynchronous scene-load progress;
- a full-screen opaque background that prevents partially composed Gameplay from being visible.

The loader persists across the scene transition, starts `Gameplay` asynchronously, waits until the Gameplay controller has completed its initial composition and at least one rendered frame has elapsed, then fades out using unscaled time. A minimum presentation time of 0.35 seconds prevents a single-frame flash; no fake numeric percentage is shown.

The currently empty Lobby scene is not inserted into this first playable flow. It remains reserved for a future menu rather than adding a redundant transition.

Gameplay must still be runnable directly in the Unity Editor for focused iteration and PlayMode tests. Direct Gameplay entry bypasses the Bootstrap overlay but uses the same ready-state contract.

## Scene and prefab ownership

Stable, inspectable presentation objects introduced by this work become prefabs: battlefield chunk and loading UI. High-volume or short-lived entities remain runtime-created or pooled: enemies, projectiles, damage numbers, pickups, and weapon effects.

This preserves the project's existing assembly-oriented folder organization:

- `Art`: source and runtime visual assets;
- `Content`: authored gameplay data;
- `Prefabs`: stable Unity object compositions;
- `Resources`: explicitly runtime-loaded shared assets;
- `Scenes`: navigation and scene-level composition;
- `Scripts`: Domain, Runtime, Presentation, Infrastructure, and Editor code;
- `Tests`: EditMode and PlayMode coverage.

The work does not reorganize unrelated files or convert the code-driven PNG-frame animation system to Mecanim.

## Runtime data flow

1. Bootstrap creates the opaque loading presentation and begins async Gameplay loading.
2. Gameplay loads and `FirstPlayableController` initializes combat state.
3. The battlefield presenter builds nine chunks around coordinate `(0, 0)`.
4. The controller publishes readiness after player, field, weapon runtime, and required presentation roots exist.
5. The loader waits one rendered frame, fades, and destroys its persistent root.
6. During play, the battlefield presenter derives the player's chunk coordinate and recycles only changed chunks.
7. The wave director samples elapsed time, supplies roster and density rules, and schedules bounded pack events.
8. The spawn path resolves an explicit enemy ID and places the complete renderer outside the real viewport.

## Performance constraints

- Exactly nine ground chunks remain active.
- Chunk-coordinate changes may rebuild bounded decorations; ordinary movement inside a chunk allocates no managed memory.
- No per-frame procedural texture generation is allowed.
- Normal enemies, packs, elites, and bosses share the existing 140-active-enemy mobile ceiling.
- The optimized exact pixel-contact path and target registration order remain unchanged.
- The existing 100-target combat regression remains green, and a representative 140-enemy Gameplay measurement is added or updated.

## Failure handling and fallback

- Missing base ground art falls back to the existing solid ground color without exposing the camera clear color.
- Missing decoration art produces an undecorated chunk, not a load failure.
- Invalid chunk size or seed configuration uses validated defaults.
- Missing enemy content IDs use the plague-rat fallback and emit one development warning per ID.
- A failed asynchronous Gameplay load keeps the opaque loading screen visible and shows a concise retry message instead of revealing a black or partially composed scene.

## Test strategy

Follow test-first implementation.

EditMode coverage:

- world position to chunk-coordinate conversion, including negative coordinates;
- required 3-by-3 coordinate set around a center;
- deterministic decoration output for the same coordinate and different output for representative different coordinates;
- exact wave boundaries, caps, species weights, and pack-size ranges;
- first-wave roster contains only `plague_rat`;
- all rosters resolve to known sprite IDs;
- pack scheduling remains within authored timing bounds.

PlayMode coverage:

- nine chunks cover the portrait camera at origin and after large movement in all four directions;
- returning to a prior coordinate restores the same decoration signature;
- no chunk GameObject churn while moving inside one chunk;
- all normal spawns begin fully outside the actual camera viewport;
- first-wave live enemies are rats only;
- second-wave simulation includes both rats and spirits and produces a spirit pack;
- active enemies never exceed 140 during continuous plus pack spawning;
- direct Gameplay entry remains functional;
- Bootstrap loads Gameplay, waits for readiness, and removes the opaque loading overlay;
- 140-enemy runtime profiling records frame timing and managed allocation.

Final validation includes the relevant EditMode and PlayMode suites, Console inspection, visual inspection of chunk seams and loading transition at representative portrait resolutions, and an Android development build.

## Compatibility and rollback

The design avoids modifying weapon behavior, save data, package versions, input bindings, or render-pipeline settings. The existing finite battlefield asset and presenter remain recoverable through Git history. The Gameplay scene's existing serialized sprite references are preserved unless a new explicit prefab reference is required; any scene edit must be narrow because the local Gameplay scene currently contains user-owned uncommitted changes.

## Out of scope

- Persistent open-world objects, buildings, quests, or revisitable enemy state;
- biome transitions beyond sparse wave-related decoration accents;
- a finished Lobby/menu flow;
- conversion of all runtime-created combat objects to prefabs;
- changing weapon attack patterns as part of this feature;
- physical Android-device thermal and GPU profiling, which remains a separate validation task.
