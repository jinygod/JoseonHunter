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
- The first-playable lifecycle creates and binds a `DamageNumberPool` to its combat service, and explicitly unbinds it on reset/destruction.
