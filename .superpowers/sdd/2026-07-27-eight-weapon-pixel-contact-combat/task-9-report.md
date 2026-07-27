# Task 9 report

- Added a pooled `LinearProjectileExecutor` for straight-line position integration, lifetime expiry, mask-confirmed contacts, bounded impacts, and pool returns.
- Added distinct Gakgung boss → elite → threat score → runtime ID selection with captured, non-homing arrows; level five launches one three-impact lead arrow and two split arrows.
- Added distinct Singijeon densest 30-degree bucket selection with configured lanes and three rows at level five; every rocket has one bounded impact.
- Extended `ICombatTarget` and all runtime/test implementations with `IsBoss`, `IsElite`, and `ThreatScore`.
- Added EditMode mechanics tests for bow priority/moved-target misses and Singijeon direction/lane distinction.

Validation: scoped `git diff --check` passed. Unity tests were intentionally not run per Task 9 implementation instruction.

## Review fix round 1

- Moved attack-instance allocation to `WeaponRuntimeController`, so FlyingBlade, Gakgung, Singijeon, and future executors sharing a runtime cannot collide.
- Added deterministic swept mask-contact samples at half an attack-mask pixel, with a 64-step safe travel bound per tick.
- Normalized Singijeon angular buckets cyclically across the ±180-degree seam.
- Capped lanes at 6, active and pooled projectiles at 32 each, and per-projectile impacts at 3; level five remains exactly three rows.
- Added focused mechanics tests for ID isolation, tunneling, angular-wrap density, and level-five caps.

## Review fix round 2

- Replaced clipped high-speed travel with a per-projectile simulation-time debt. Each tick processes at most the 64-sample budget, carries the remaining time forward, and decrements lifetime only for the swept segment.
- Added full-range high-speed, all-three-executor ID, competing-cluster seam, and explicit active/pool/impact cap tests.
