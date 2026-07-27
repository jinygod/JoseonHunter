# Task 14 report

Implemented catalog-driven weapon registration in `FirstPlayableController` and scene-generator catalog assignment. The controller creates the shared target registry, damage service, weapon runtime, and pooled damage-number presentation root, then registers the canonical roster once each.

`WeaponExecutionContext` now accepts an optional per-weapon sprite resolver. Flying blades and linear projectiles use it, preserving the previous generic sprite as a fallback.

Static validation performed: inspected catalog YAML level counts, searched controller for removed `UpdateAttack` prototype reconfiguration, and checked direct `DamageEnemy` calls. Unity and PlayMode tests were intentionally not launched per task instruction.

All eight launch definitions now have five conservative progression rows and the catalog references each definition once. Representative sprites have deterministic point/no-mipmap/uncompressed importer metadata and each definition references its own representative. The PlayMode test covers generated-scene registration and pool bootstrap; confirmed-hit coverage remains a Unity-run validation item.

Review round 1: fixed the projectile visual-spec compile error, serialized the Gameplay catalog reference, reset the initial level before catalog registration, and rebuild executors cleanly when experience raises a level. Presentation remains the sole damage-number-pool owner. Every definition now references its approved binary mask and the controller loads the runtime mask catalog before executor registration.

Review round 2: restored the legacy five-argument execution-context constructor and added catalog mask resolution to the execution context. Flying blades and linear projectiles now prefer the immutable approved mask for their weapon and only derive a sprite mask for isolated prototype/test contexts.
