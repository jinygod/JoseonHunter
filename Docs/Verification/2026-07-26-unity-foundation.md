# Unity foundation verification — 2026-07-26

## Scope and baseline

- Foundation baseline: `55c1a40fb8b520cce9741295d89e93b6978e807d` on `agent/unity-foundation-asset-pipeline`.
- Unity: `6000.5.5f1` (`d16e074b49fd`); active target: Android.
- Enabled build scenes: Bootstrap, Lobby, Gameplay, in that order.
- URP `17.5.0` is active with the checked-in 2D renderer; Input System `1.20.0` is enabled exclusively.

## Repository commands

| Command | Exit code | Result / artifact |
| --- | ---: | --- |
| `git status --short` | 0 | Clean at post-test documentation baseline. A transient Unity-authored Android scripting-symbol change and temporary audio fixtures were observed during validation and had cleared before documentation changes. |
| `git diff --check` | 0 | No whitespace errors. Unity serialized YAML was not rewritten. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AssetMigration/Test-SyncFlutterAssets.ps1` | 0 | PASS: invalid-input rejection, dry-run safety, failure aggregation, JSON reporting, SHA idempotency. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1` | 0 | EditMode 26/26 passed, 0 failed/skipped; `Logs/editmode-results.xml`, duration `5.2266208s`. |

The EditMode XML records 7/7 scene-scaffold tests and validates the scene order, dirty-scene protection, URP 2D/Input System-only configuration, Android landscape intent, required first-party assemblies, asset profiles, and migration policy.

## Connected Unity MCP evidence

- Official Unity bridge/client: connected and approved; no second provider present.
- `Unity_RunCommand` compiled and executed C# calling `EditorApplication.ExecuteMenuItem("JoseonHunter/Setup/Generate Foundation Scenes")`: `success: true`, compilation/execution succeeded, `executionId: 1`, empty compilation logs.
- `Unity_GetConsoleLogs` succeeded with `errorCount: 0`.
- Console warnings were package/environment warnings: URP shader conversion, historical revoked MCP attempts, and account API/signature messages. None were first-party compilation errors.

## Acceptance matrix

| Requirement | Status | Evidence / limitation |
| --- | --- | --- |
| Android Build Support, SDK, NDK, OpenJDK | Passed | Installed and verified before this checkpoint. |
| Official MCP only, connected and approved | Passed | Connected bridge plus successful `Unity_RunCommand`; no second provider. |
| URP 2D and Input System-only | Passed | Actual settings and passing `ProjectUsesUrp2DAndInputSystemOnly` test. |
| Six first-party and two test assemblies compile | Passed | Batch Editor compilation and 26 passing EditMode tests. |
| Synchronizer is allowlisted and passes isolated tests | Passed | Harness exit 0/PASS. |
| First-slice assets/import/rights validation | Passed | Hash inventory plus importer and validator tests; temporary audio/fonts remain intentionally excluded. |
| Bootstrap/Lobby/Gameplay exist and are enabled in order | Passed | Serialized build settings plus 7/7 scene-scaffold tests. |
| No Unity Console compile errors | Passed | MCP Console `errorCount: 0`. |
| All batch EditMode tests pass | Passed | 26/26 in `Logs/editmode-results.xml`. |
| Android player build/device smoke test | Not run | Not required by this foundation milestone; no APK/AAB artifact or device validation. |

## Limitations

This is a compile/configuration/EditMode checkpoint, not a gameplay or target-device release validation. No PlayMode smoke test, Android Gradle build, device installation, performance measurement, or visual acceptance was performed. Warnings remain classified as Unity/package/environment messages and should be re-triaged in a future validation run.
