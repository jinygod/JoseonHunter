# Gameplay Visual Prefab Authoring Design

## Goal

Preserve every gameplay rule and public contract while moving the visual structures that `FirstPlayableController` currently assembles with `new GameObject()` into editable Unity Prefabs. The real Gameplay scene remains runtime-driven; a separate editor-only preview scene exposes representative instances for visual authoring.

## Baseline

- Branch: `master`, three local commits ahead of `origin/master` before this work.
- Existing unrelated Lobby/art/font changes are dirty and must be preserved.
- EditMode baseline: 924/924 passed.
- PlayMode baseline: 320/321 passed. The sole pre-existing failure is `LobbyPatrolPlayModeTests.PatrolUsesStageArrowsPremiumCardsAndHeroFrame` (`difficulty_selected` expected, `difficulty_idle` actual).
- Gameplay scene roots must remain `Main Camera`, `FirstPlayable`, and `EventSystem`.

## Scope Boundaries

This change owns only visual construction and authoring. It does not alter combat balance, weapons, enemy AI, waves, bosses, collision, damage, experience, upgrades, saves, routing, audio, or current public events/APIs.

The following continue to be dynamic runtime data:

- sprites and animation frames;
- sorting order and flip direction;
- combatant role, scale multiplier, health, shield and status;
- pickup kind, value, tier colour and attraction state;
- hit, guard and death presentation state.

## Chosen Reference Model

Use a `GameplayVisualPrefabLibrary` ScriptableObject stored under `Resources/Gameplay` and also serialized into `FirstPlayableController` in the Gameplay scene.

Resolution order:

1. the controller's serialized library reference;
2. `Resources.Load` for controllers created directly by tests or tools;
3. the existing code-created visual fallback, with an explicit Editor/Development warning naming the missing prefab.

This is safer than seven independent controller fields, makes the dependency visible in one asset, and keeps direct `AddComponent<FirstPlayableController>()` tests working. Fixed Resources paths for every prefab were rejected because they hide dependencies and make validation fragmented.

## Runtime Components

### `CombatantVisualView`

Reference-only component for:

- Visual Pivot and body SpriteRenderer;
- Soft Shadow, Silhouette Outline and optional Player Aura renderers;
- HealthBarAnchor and optional ShieldBarAnchor.

It contains no combat rules. `CombatantVisualRig.Bind` consumes it and injects sprite, sorting orders, colours, role and motion data without rebuilding or duplicating children.

### `WorldBarView`

References Background and Fill renderers/transforms and records the prefab-authored full fill scale and position. Runtime updates only the normalized fill ratio, preserving prefab height and vertical placement.

### `PickupVisualView`

References the Visual SpriteRenderer, optional root TrailRenderer and editable base scale. Runtime still injects sprite, colour, value, tier and attraction animation.

## Prefab Assets

`Assets/JoseonHunter/Prefabs/Gameplay/`

- `PlayerVisual.prefab`
- `EnemyVisual.prefab`
- `WorldHealthBar.prefab`
- `WorldShieldBar.prefab`
- `ExperiencePickup.prefab`
- `YeopjeonPickup.prefab`
- `MagnetPickup.prefab`
- `GameplayAuthoringPreview.prefab`

Expected combatant hierarchy:

```text
Root
├─ Soft Shadow
├─ Silhouette Outline
├─ Player Aura                 (player only)
├─ Visual Pivot                (body SpriteRenderer)
├─ HealthBarAnchor
└─ ShieldBarAnchor             (enemy only)
```

Bars keep the existing `Root/Background` and `Root/Fill` contract. Experience keeps a `TrailRenderer` on the pickup root because existing runtime and tests access it there.

## Runtime Integration

- `CreateCombatantObject` instantiates the role-appropriate prefab, applies the existing runtime root name/parent/world position and calls `CombatantVisualRig.Bind`.
- Existing `CombatantVisualRig.Create` remains as the compatibility fallback and for isolated tests.
- `CreateHealthBar` and `CreateShieldBar` instantiate bar prefabs under authored anchors and return the same Fill transform contract used by current code.
- `UpdateBarFill` delegates to `WorldBarView` when available, otherwise retains the legacy width/height calculation.
- `SpawnPickup` keeps the current merge cap and inactive-object pool. Only first creation changes to prefab instantiation; reuse, names, values and collection behaviour stay unchanged.
- Generic sprite objects such as projectiles, treasure chests and flashes remain code-created because they are outside this migration.

## Editor Generation and Safety

`GameplayVisualPrefabBuilder` creates only missing assets through Unity Editor APIs, fills only missing library references, validates required children/components, and wires the library into the Gameplay scene after refusing to overwrite a dirty open Gameplay scene. It never rewrites a customized valid prefab.

`GameplayVisualPreviewBuilder` creates the authoring preview prefab and `GameplayVisualPreview.unity` from the production prefabs. It refuses dirty loaded scenes, is repeatable, and never adds the preview scene to Build Settings.

The preview contains no `FirstPlayableController`, save session, routing or live combat flow.

## Preview Layout

The preview displays:

- player with health bar;
- normal enemy with health bar;
- enlarged elite/boss comparison with health and shield bars;
- experience, coin and magnet pickups;
- a portrait-camera guide.

Editing production prefabs rather than disconnected mock objects ensures saved Prefab changes appear in Play Mode on the next spawn/reset.

## Test Strategy

1. EditMode asset contracts begin RED while prefabs/library/preview are absent.
2. View binding tests protect exact child references, no duplicate SpriteRenderers, authored bar geometry and fallback compatibility.
3. PlayMode tests verify real Gameplay instances originate from prefabs, player/enemy/midboss/boss visuals bind correctly, bars update and pickups still pool/trail correctly.
4. Existing pickup range, rig motion/hit/death, scene root, game-over/routing and save tests remain unchanged.
5. Full EditMode and PlayMode suites run at the end. The known Lobby baseline failure is reported separately if it persists unchanged.

## Risks and Mitigations

- **Prefab missing or invalid:** explicit named warning plus legacy fallback; contract tests fail in production assets.
- **Duplicate visual children:** bind existing view references; PlayMode assertions count renderers and bars.
- **Authored values overwritten:** runtime changes only dynamic sprite/order/colour/ratio/role scale; base offsets and bar dimensions remain prefab-owned.
- **Pooling regression:** keep `PickupState`, pool search, deactivation and TrailRenderer root contract intact.
- **Scene overwrite:** editor builders reject dirty open scenes and Preview stays outside Build Settings.
- **Serialization regression:** existing serialized fields and public APIs remain untouched; one additive library field is introduced.
