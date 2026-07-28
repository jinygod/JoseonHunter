# Combat UI final fix report

## Scope and implementation

Base before this fix wave: `c6bc121`.

- `ConfirmedDamageEvent` now captures immutable `IsBossTarget` from `ICombatTarget.IsBoss` before resolved damage can remove the target.
- The classification flows through `DamageNumberAccumulator` and `DamageNumberDisplay`; `DamageNumberPool` styles the number from that snapshot rather than querying the mutable controller target list.
- `CombatFeedbackDirector` uses the event snapshot and resolves a fatal boss contact to intensity 100. The contact flash has a distinct size/color for that tier.
- Added policy/event tests for boss classification, delayed number propagation, intensity 100, the confirmed-event PlayMode path, and bootstrap `RunReset` closure.

## Unity validation

Unity executable: `C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe`.

1. `Start-Process ... -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter\.worktrees\combat-ui-evolutions' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.FirstPlayableUiStateTests|JoseonHunter.Tests.EditMode.FeedbackBudgetTests|JoseonHunter.Tests.EditMode.CombatDamageServiceTests|JoseonHunter.Tests.EditMode.DamageNumberAccumulatorTests' -testResults '...\Temp\combat-ui-editmode.xml' -logFile '...\Temp\combat-ui-editmode.log' -quit`, waiting on the returned Unity PID 84024.
   - Exit code: 0.
   - Unity compiled `JoseonHunter.EditModeTests.dll` and `JoseonHunter.PlayModeTests.dll`; no C# errors were reported. Existing obsolete-API warnings remained.
   - XML: not produced. The first launch completed an import/compile and exited before Test Runner results were emitted.
2. `Start-Process ... -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter\.worktrees\combat-ui-evolutions' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.FeedbackBudgetTests' -testResults '...\Temp\feedback-budget-editmode.xml' -logFile '...\Temp\feedback-budget-editmode.log' -quit`, waiting on Unity PID 88292.
   - Exit code: 0.
   - XML: not produced.
   - Blocking log evidence: `Failed to handshake to channel: "LicenseClient-전성진"`, `Access token is unavailable`, and entitlement `404` errors.

Therefore successful Test Runner XML totals are unavailable: EditMode `0 run / 0 failures recorded`; PlayMode `not run / no XML`. The source compiled in the initial launch, but this is not a replacement for test execution.

## Portrait acceptance (1080x1920)

Not run. The batch Unity entitlement failure above prevented entering a reliable Unity test/editor session, and no screenshot was captured. Screenshot path: none. Static checks only confirm the existing 1080x1920 CanvasScaler and safe-area container; they do not establish safe-area visuals, center clearance, card reachability/readability, one-click reward behavior, contact number placement, or reward-strength appearance.

## Working tree and self-review

- `git diff --check` passes.
- Unity-generated art `.meta`, UI `.meta`, and `ProjectSettings` changes were restored after validation. The eight originally untracked weapon-directory `.meta` files remain untouched and unstaged.
- The changed presentation path contains no post-event controller boss lookup; only the alive predicate remains, intentionally used to identify the fatal event after damage is applied.
- No new committed asset requires a `.meta` file.

## Remaining concerns

1. Restore a valid Unity entitlement and rerun the focused EditMode and PlayMode UI suites, retaining their XML result paths and totals.
2. With an interactive/licensed editor, complete the required 1080x1920 portrait visual acceptance and retain an inspected screenshot outside source control.
