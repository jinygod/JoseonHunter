# Task 5 report — confirmed weapon damage authority

## Implemented

- Added immutable `ConfirmedDamageEvent` with the resolved damage, weapon and attack identity, target identity, contact point, phase, and simulation tick.
- Added `ICombatTarget`, an identity-safe `CombatTargetRegistry`, and `CombatDamageService` as the Runtime health-mutation boundary.
- `CombatDamageService.TryApply` rejects absent pixel confirmation, invalid contact coordinates or damage values, dead/empty targets, unregistered targets when a registry is supplied, and duplicate attack-instance contacts. It resolves once, applies that resolved value once, then publishes the exact event.
- Added focused EditMode coverage for the requested authority path, duplicate hit rejection, and rejected unregistered/unconfirmed requests.

## Validation

- Reviewed every Task 5 production and test file plus all existing `DamageResolver` and `AttackInstance` call sites.
- `git diff --check` reports only pre-existing user-owned imported sprite `.meta` whitespace; no Task 5 path is reported.
- Unity test execution was intentionally skipped because Unity editor processes were already active; no Editor process was started or interrupted.

## Compatibility

- Existing `DamageResolver.Resolve` behaviour is unchanged. `TryResolve` is the validating entry point used by the authoritative Runtime service.
- The no-registry constructor supports the plan's minimal authority example. Production wiring supplies `CombatTargetRegistry`, which requires the request target to be the currently registered instance.
