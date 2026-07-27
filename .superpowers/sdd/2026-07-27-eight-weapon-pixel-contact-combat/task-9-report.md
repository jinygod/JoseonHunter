# Task 9 report

- Added a pooled `LinearProjectileExecutor` for straight-line position integration, lifetime expiry, mask-confirmed contacts, bounded impacts, and pool returns.
- Added distinct Gakgung boss → elite → threat score → runtime ID selection with captured, non-homing arrows; level five launches one three-impact lead arrow and two split arrows.
- Added distinct Singijeon densest 30-degree bucket selection with configured lanes and three rows at level five; every rocket has one bounded impact.
- Extended `ICombatTarget` and all runtime/test implementations with `IsBoss`, `IsElite`, and `ThreatScore`.
- Added EditMode mechanics tests for bow priority/moved-target misses and Singijeon direction/lane distinction.

Validation: scoped `git diff --check` passed. Unity tests were intentionally not run per Task 9 implementation instruction.
