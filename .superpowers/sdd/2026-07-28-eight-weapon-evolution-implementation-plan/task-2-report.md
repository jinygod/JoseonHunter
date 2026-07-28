# Task 2 report

Implemented persisted run-scoped evolution acquisition. Reset now clears evolution state, then bootstraps all catalog evolution IDs for the first-playable run. Evolution offers retain the owning weapon at level five, rebuild runtime executors with an evolved profile, and emit the catalog's canonical reward name, summary, and icon.

Each launch-roster executor accepts and exposes an `IsEvolved` profile flag without changing normal-mode behavior. The runtime keeps the weapon-to-executor registration needed for the focused PlayMode assertion.

Validation: `git diff --check` passed. One focused Unity PlayMode invocation was made before implementation as the required red check. Unity reached test-assembly compilation but failed against the stale runtime assembly because the new test helper APIs did not yet exist; no retry was made per the task constraint. No post-change Unity test result was produced.

## Fix Round 1

Added the runtime-only `EvolvedWeaponTestRig` and the test-only `EvolvedExecutorFactory`. The factory constructs all eight level-five evolved executors using real registry, damage service, and runtime controller dependencies. Focused coverage now verifies every roster weapon has one unique, queryable evolved executor.

The controller evolution test now captures the normal pre-choice runtime and executor, verifies that selection replaces both, checks the retired runtime's disposal/cleared registration signal, and asserts one evolved replacement registration without a duplicate slot. Unity test infrastructure was not retried.

## Fix Round 2

`WeaponRuntimeController.Register(WeaponId, ...)` now rejects duplicate weapon IDs before modifying its executor list. Test-only slot counts expose the actual executor collection, and coverage verifies a rejected duplicate is neither ticked nor disposed as a registered slot.

Completed `EvolvedWeaponTestRig` with a real registry, damage service, runtime, root object, combat targets, confirmed-event observations, cast/time advancement, and cleanup. Added test-only unified `EvolutionTelemetry` and the all-eight factory reader. Default telemetry remains intentionally neutral until the executor-specific evolution tasks add their real counters; it reports the real evolved profile flag without synthesizing combat events. Unity test infrastructure was not retried.

## Fix Round 3

`ReadTelemetry` is now an eight-type adapter over real executor state: live launch/active/contact/field/ward counters, state, duration/range values, and target-selection direction are copied into a stable snapshot. Each snapshot also identifies the concrete executor and canonical weapon ID, plus the actual evolved profile. Fields whose corresponding future evolution mechanic does not exist remain neutral rather than fabricated.

The all-eight rig test now adds a registered legal target, advances the actual executor, and verifies the resulting live telemetry discriminator, evolved flag, state, and an observed count or non-idle state for every weapon. Unity test infrastructure was not retried.
