# Task 5: Deterministic Combat, XP, and Run Rules

## Delivery

Implemented engine-free Domain combat damage resolution, run timing/wave schedule,
experience thresholds, and seeded upgrade selection. All new Domain code remains in
the `JoseonHunter.Domain` assembly, whose asmdef has `noEngineReferences: true`.

## TDD evidence

1. RED tests were added first in `CombatRuleTests` and `RunRuleTests`.
2. RED command:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.CombatRuleTests
   ```

   The first compilation failed as expected because the required combat, progression,
   and run types did not exist (including `CS0246` for `UpgradeState` and `RunPhase`).
   The compiler stops before NUnit can report a test count.
3. GREEN focused commands and results:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.CombatRuleTests
   # Passed: 13/13

   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.RunRuleTests
   # Passed: 13/13
   ```
4. Full EditMode regression:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode
   # Passed: 131/131, failed: 0, skipped: 0
   ```

## API and edge decisions

- `DamageResolver.Resolve(in DamageRequest)` sums base and flat damage, applies the
  supplied multiplier, rounds midpoint values away from zero, and clamps final damage
  to one. The supplied critical flag is preserved in the result; critical scaling is
  represented by the supplied multiplier.
- `RunClock` accumulates non-negative elapsed time and transitions at exactly 45, 90,
  135, 165, 180, and 240 seconds. `WaveSchedule` supplies caps 28, 36, 48, 64, and
  36 for the four waves and boss, and uses only existing launch content IDs.
- `ExperienceCurve.GetThresholdForNextLevel` uses exactly `5, 8, 12, 18, 26, 36, 48,
  62` and rejects levels outside that defined range.
- `UpgradeSelector.Select` uses `System.Random(seed)`, never Unity random. It selects
  exactly three unique offers, reserves one slot for an owned non-max weapon when one
  exists, excludes maxed entries, and only offers an evolution after its ID is in the
  unlocked set and its required weapon is maxed.

## Unity/C# compatibility deviation

The brief's `readonly record struct` declarations cannot compile in this project:
Unity's compiler reports `CS8773` (record structs require C# 10 while this project is
C# 9) and `CS0518` (`System.Runtime.CompilerServices.IsExternalInit` is unavailable).
Changing ProjectSettings is outside Task 5 scope. The implementation therefore uses
readonly structs with the same public constructor/property names and explicit
structural equality, and an immutable-property sealed class for `UpgradeState`.

Likewise, Unity exposes `System.Collections.Generic.IReadOnlySet<T>` as inaccessible.
To avoid shadowing a future BCL type, the narrow Domain-local `IUpgradeIdSet` is used
only for upgrade IDs, with an `ISet<string>` constructor overload for ordinary
`HashSet<string>` callers.

## Files

- `Assets/JoseonHunter/Scripts/Domain/Combat/CombatTypes.cs`
- `Assets/JoseonHunter/Scripts/Domain/Combat/DamageResolver.cs`
- `Assets/JoseonHunter/Scripts/Domain/Runs/RunClock.cs`
- `Assets/JoseonHunter/Scripts/Domain/Runs/WaveSchedule.cs`
- `Assets/JoseonHunter/Scripts/Domain/Progression/ExperienceCurve.cs`
- `Assets/JoseonHunter/Scripts/Domain/Progression/ProgressionTypes.cs`
- `Assets/JoseonHunter/Scripts/Domain/Progression/UpgradeSelector.cs`
- `Assets/JoseonHunter/Tests/EditMode/CombatRuleTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/RunRuleTests.cs`
- Unity `.meta` files for every new file and folder

## Self-review and risks

- Reviewed the scoped diff and confirmed no `UnityEngine`, `UnityEditor`, or Unity
  random references appear in the new Domain code.
- Reviewed deterministic ordering and offer uniqueness; the selector uses only the
  injected integer seed.
- Unity import modified unrelated art metas and ProjectSettings in this shared
  worktree; none of those files are staged or included in the Task 5 commit.
- The C#9 substitutions are deliberately observable-contract compatible, but are a
  review point if the project later moves to C#10 and can adopt the requested record
  syntax and BCL `IReadOnlySet<T>` directly.

## Commit

Initial delivery commit: `feat: add deterministic patrol combat rules`. The exact
current commit hash is provided by the task handoff, rather than self-referentially
rewriting this tracked report.

## Review fix round 1

### RED/GREEN evidence

New tests were added before implementation and run with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.CombatRuleTests
```

RED was a compile failure for the intentionally absent API: `UpgradeState` lacked
the four-argument acquired-evolution constructor and `AcquiredEvolutionIds`, while
`DamageRequest` lacked `Deconstruct`, `==`, and `!=` (`CS1729`, `CS1061`, `CS8129`,
and `CS0019`). After the minimal implementation, the same focused suite was GREEN:
18/18 passed.

Additional verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.RunRuleTests
# Passed: 13/13

powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode
# Passed: 136/136, failed: 0, skipped: 0
```

### Changes

- Upgrade selection now throws `InvalidOperationException` with the stable diagnostic
  `At least three distinct eligible upgrades are required.` whenever fewer than three
  distinct valid candidates exist. This treats an exhausted offer state as invalid;
  it never returns a partial offer list.
- `UpgradeState` accepts an acquired-evolution ID snapshot in a new four-argument
  overload, retaining the previous three-argument overload with an empty acquired
  set. Acquired evolutions are excluded even if unlocked and otherwise eligible.
- All input dictionaries and sets are copied at construction. Public properties expose
  read-only dictionary/set interfaces backed by private snapshots, so callers cannot
  mutate the source collections after construction to change selection behavior.
- `DamageRequest` now has C#9-compatible structural value behavior (`IEquatable`,
  object equality, hash code, equality operators, and four-value deconstruction).
  `UpgradeState` intentionally remains reference-equality only: its role is an
  immutable snapshot input object, and structural equality across four collections
  would add API/ordering policy not required by this first-release contract.
