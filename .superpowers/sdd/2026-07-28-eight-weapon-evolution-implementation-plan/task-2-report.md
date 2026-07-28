# Task 2 report

Implemented persisted run-scoped evolution acquisition. Reset now clears evolution state, then bootstraps all catalog evolution IDs for the first-playable run. Evolution offers retain the owning weapon at level five, rebuild runtime executors with an evolved profile, and emit the catalog's canonical reward name, summary, and icon.

Each launch-roster executor accepts and exposes an `IsEvolved` profile flag without changing normal-mode behavior. The runtime keeps the weapon-to-executor registration needed for the focused PlayMode assertion.

Validation: `git diff --check` passed. One focused Unity PlayMode invocation was made before implementation as the required red check. Unity reached test-assembly compilation but failed against the stale runtime assembly because the new test helper APIs did not yet exist; no retry was made per the task constraint. No post-change Unity test result was produced.

## Fix Round 1

Added the runtime-only `EvolvedWeaponTestRig` and the test-only `EvolvedExecutorFactory`. The factory constructs all eight level-five evolved executors using real registry, damage service, and runtime controller dependencies. Focused coverage now verifies every roster weapon has one unique, queryable evolved executor.

The controller evolution test now captures the normal pre-choice runtime and executor, verifies that selection replaces both, checks the retired runtime's disposal/cleared registration signal, and asserts one evolved replacement registration without a duplicate slot. Unity test infrastructure was not retried.
