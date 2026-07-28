# Task 5 report — evolved ward and Singijeon

## Delivered

- Evolved Jangseung wards activate their four posts sequentially, then mark only targets inside the completed polygon.  Only those marked targets can receive a confirmed boundary-crossing pulse.
- Evolved Singijeon now fires exactly three spread scout rockets, records the selected dense target position, waits 0.35 seconds, and fires eight focused rockets around that position.  Rockets retain separate `OncePerInstance` attack identities.
- Added PlayMode coverage for enclosed marking, confirmed marked-boundary pulse, scout/focus cadence and per-attack-instance contact uniqueness.  The test telemetry now exposes the evolved volley counts and kind order.

## Validation

One requested Unity PlayMode invocation was run. It stopped at compilation with CS0819 in the new polygon loop (`var` used with two declarators). The loop was corrected to explicit `int` declarations after that single attempt. Per task instruction, the Unity run was not repeated.

## Scope notes

- `EvolvedExecutorFactory.cs` and `EvolvedWeaponTestRig.cs` are the minimal test seams changed to expose the new volley telemetry and represent the completed guardian mark.
- The eight pre-existing untracked weapon `.meta` files were left untouched.

## Review round 1

- Reworked evolved Singijeon ticking to consume simulation time at the exact scout-to-focus boundary.  A large tick now simulates scouts only to 0.35 seconds, launches focus there, then applies only the residual time to focus rockets and the following cooldown.
- Dense target selection now uses ascending bucket tie-breaks and the centroid of all valid targets in the selected bucket.
- Added coverage for split-versus-large ticks, focus carry, tied multi-cluster centroid selection, non-vacuous rocket contact uniqueness, sequential ward activation, pre-completion/non-marked/stationary ward exclusions, and normal-form preservation.
- One Unity PlayMode command was issued for this round. The workspace's pre-existing Unity processes prevented it from reaching test execution or producing result XML during the command window; it was not repeated per instruction.
