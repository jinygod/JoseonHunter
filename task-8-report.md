# Task 8 — Flying Hwando vertical slice

Implemented the prototype migration from the instantaneous hwando slash to `WeaponRuntimeController` and `FlyingBladeExecutor`.

- The blade follows a bounded quadratic outbound curve and a direct return to the owner.
- Damage is attempted only after `PixelMaskContactService` confirms active blade/hurt-mask overlap.
- Each blade owns an `AttackInstance` with `OncePerPhase`, allowing one outbound and one inbound hit per target.
- Levels scale damage, cooldown, range, and speed; level five launches three staggered pooled blades.
- The first-playable prototype adapts spawned enemies into `ICombatTarget` and registers/unregisters them with the combat registry.

Validation was static diff and source inspection only; Unity was not launched by request. The prototype currently supplies the weapon values directly because no launch weapon catalog asset is present in this worktree. The controller/executor seam is ready for a later `WeaponDefinitionAsset` / `WeaponLevelData` binding.

## Review round 1

- Added deterministic `WeaponMechanicTests` seams for pre-contact damage, phase-limited hits, range, pooling, and level-five staggering.
- Pixel-mask transforms now use the same sprite rect/pivot/PPU and renderer world scale, rotation, and horizontal flip as their visuals. Non-readable prototype textures fall back to an opaque mask with the same sprite rect.
- Staggered blades are positioned at their launch point before their delay begins.
- `FirstPlayableDamageNumberBootstrap` owns the `DamageNumberPool`, finds the gameplay controller, and binds/unbinds against its read-only combat event source without adding a Runtime → Presentation dependency.
## 2026-07-31 Task 8 follow-up: bounded enemy separation

### RED / GREEN evidence

- RED grid: `Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.EnemySeparationGridTests` failed during fresh compilation because `EnemySeparationAgent` and `EnemySeparationGrid` did not exist.
- RED integration: `Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.FirstPlayableLoadPlayModeTests` failed during fresh compilation because the controller had no separation load-test API.
- GREEN grid: the focused EditMode command passed **8/8**. It covers exact overlap/opposite response, stable coincident IDs, 30/50/100 dense loads, maximum-eight neighbors, constructor validation, and warmed repeated `Rebuild`/`Resolve` allocation.
- GREEN load: the focused PlayMode command passed **3/3** at 30, 50, and 100 living enemies. Each starts exactly coincident, runs 80 deterministic resolver ticks, excludes a treasure chest, has no exact remaining position pairs, and still reduces mean distance to the player.
- Representative regression: `StagePacingPlayModeTests;PortraitUiLayoutPlayModeTests;CombatHudPlayModeTests;FirstPlayableLoadPlayModeTests` passed **17/17**.
- Full EditMode: `JoseonHunter.Tests.EditMode` passed **521/521**. Fresh XML and logs were inspected.

### Implementation and allocation evidence

- Added a reusable `EnemySeparationGrid` with reusable dictionary buckets, bucket stack, occupied-key list, stable input-index bucket order, bounded direct cell lookup, penetration-weighted displacement, ID-derived coincident direction, magnitude clamping, and `LastNeighborCount`.
- `EnemySeparationGridTests.WarmedRebuildAndResolveAllocateNoManagedBytes` warms four full 100-agent rebuild/resolve passes, then measures eight passes using `GC.GetAllocatedBytesForCurrentThread`; the steady-state result was **0 managed bytes**.
- `UpdateEnemies` prunes destroyed states before rebuilding once from reusable parallel living non-treasure state/agent lists. It uses Task 7 contact radii, resolves at most eight neighbors, and blends `chase + separate * .72f` while retaining existing rank speed, movement multiplier, visuals, hit checks, and GameFlow behavior.
- No Task 9 profiler markers or counters were added. Unity reimported one unrelated Hwando meta during validation; it was restored. The seven pre-existing concurrent sprite-meta edits remain untouched.

### Files

- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemySeparationGrid.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- `Assets/JoseonHunter/Tests/EditMode/EnemySeparationGridTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableLoadPlayModeTests.cs`

### Commit and upstream

- Implementation commit: `76bd345 feat: add bounded enemy separation`.
- `git push origin agent/portrait-stabilization-vertical-slice` completed successfully. The final report commit and a fresh fetch compare are recorded with the task handoff.
