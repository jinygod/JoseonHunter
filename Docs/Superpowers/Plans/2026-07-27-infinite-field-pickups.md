# JoseonHunter Infinite Field And Reward Pickups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the prototype's mismatched finite clamps with an effectively infinite field and add experience-only monster rewards, rare experience magnets, and breakable yeopjeon treasure chests.

**Architecture:** Keep the rapid playable in its current single runtime controller for this iteration. Move the visual field in snapped increments around the camera while all gameplay positions remain in world space; extend the existing pickup and damageable state with explicit kinds so reward rules cannot overlap.

**Tech Stack:** Unity 6000.5.5f1, C# 9, URP 2D, Unity Input System, Unity MCP.

## Global Constraints

- Player, camera, and enemy spawning have no fixed world-coordinate clamps.
- The field follows the camera in two-unit snapped steps.
- Normal enemies drop exactly one experience spirit and never yeopjeon.
- Normal enemies have a 1 percent experience-magnet drop chance.
- A magnet pulls only experience spirits present when it is collected.
- The first chest spawns at 18 seconds; later intervals are 40 to 60 seconds.
- At most two unopened chests exist.
- Chests have 75 health and scatter 6 to 10 pickups worth 1 to 3 yeopjeon.
- Per the user's speed preference, run one focused Play-mode smoke check now and defer the broad integrated suite.
- Preserve unrelated user scenes, static-sprite meta changes, PixelLab candidates, and ProjectSettings changes.

---

### Task 1: Make The Runtime Field Effectively Infinite

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- `UpdateField()` snaps `FlatField` to two-unit increments around the camera.
- `UpdatePlayer`, `UpdateCamera`, and `SpawnEnemy` stop clamping world coordinates.

- [ ] Remove player, camera, and enemy-position clamp statements.
- [ ] Store the generated `FlatField` transform and call `UpdateField` after camera movement.
- [ ] Snap field position with:

```csharp
var cameraPosition = gameplayCamera.transform.position;
flatField.position = new Vector3(
    Mathf.Round(cameraPosition.x / 2f) * 2f,
    Mathf.Round(cameraPosition.y / 2f) * 2f,
    0f);
```

- [ ] In Play mode, move the player beyond the old `(9.5, 14.5)` bounds and verify the camera, field, and player continue together.
- [ ] Commit with `feat: make playable field effectively infinite`.

---

### Task 2: Add Explicit Reward Kinds, Magnet Collection, And Treasure Chests

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- Modify through Editor API: `Assets/JoseonHunter/Scenes/Gameplay.unity`

**Interfaces:**
- Add `PickupKind { Experience, Yeopjeon, Magnet }`.
- `SpawnPickup(Vector2 position, PickupKind kind, int value)` owns pickup visuals and values.
- `SpawnTreasureChest()` creates a stationary 75-health damageable.
- `CollectMagnet()` snapshots existing experience pickups into a forced-collection set.

- [ ] Replace `PickupState.IsCoin` with `PickupKind Kind` and `bool ForceCollect`.
- [ ] On normal-enemy death, spawn one `Experience` and independently roll `Random.value < 0.01f` for `Magnet`; remove the old coin roll.
- [ ] Assign the existing treasure-chest sprite to both a serialized `treasureChestSprite` and temporary tinted magnet visual.
- [ ] Schedule the first chest at 18 seconds, then reset its timer with `Random.Range(40f, 60f)`, respecting a maximum of two active chests.
- [ ] Represent a chest as a stationary damageable with 75 health, no contact damage, no kill count, and no experience reward.
- [ ] On chest break, scatter `Random.Range(6, 11)` yeopjeon pickups around its position, each with `Random.Range(1, 4)` value.
- [ ] On magnet collection, mark only currently existing `Experience` pickups with `ForceCollect = true`; forced pickups move toward the player at 24 world units per second regardless of pickup radius.
- [ ] Show `혼령 대회수!` for 1.2 seconds and leave yeopjeon unaffected.
- [ ] Regenerate the `Gameplay` scene through `FirstPlayableSceneGenerator.Generate()`.
- [ ] Run a focused Play-mode smoke check confirming: no field edge beyond old bounds, enemy reward has no coin, forced experience collection works, chest breaks into 6-to-10 yeopjeon pickups, and Unity reports no new gameplay exception.
- [ ] Commit with `feat: add magnet and treasure rewards`.

---

## Completion Gate

- The player can travel beyond the old bounds without exposing a field edge.
- Normal enemies never produce yeopjeon.
- Magnet and treasure behaviors match the exact values in the approved spec.
- The saved `Gameplay` scene opens with an orthographic camera and the updated sprite references.
