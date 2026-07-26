# Unity Project Context

## Foundation milestone — validated 2026-07-26

- Project root: `D:\UnityProjects\JoseonHunter`
- Implementation baseline: `55c1a40fb8b520cce9741295d89e93b6978e807d` (the verified Unity code/configuration state).
- Verified documentation and published milestone commit: `6d7c143f01a58960e13f666a7301d69ac3e63c87` (`docs: verify Unity foundation milestone`). Subsequent docs-only correction commits update recorded evidence only; they do not alter the verified code/configuration state.
- Unity: `6000.5.5f1` (`d16e074b49fd`)
- Intended first player target: Android, landscape only; identifier `com.jinygod.joseonhunter`, version `0.1.0` (code `1`), minimum SDK `26`, ARM64, IL2CPP.
- Android Build Support, SDK, NDK, and OpenJDK were verified installed for this project. An Android player build/device run is not part of this foundation checkpoint.

## Project pipeline and input

- Universal Render Pipeline is active through `Assets/JoseonHunter/Settings/Rendering/JoseonHunterUniversalRenderPipeline.asset`; its renderer type is 2D (`m_RendererType: 1`).
- `com.unity.render-pipelines.universal` resolves to `17.5.0`; the Input System-only setting is active (`activeInputHandler: 1`) and `com.unity.inputsystem` resolves to `1.20.0`.
- Direct package versions in `Packages/manifest.json`: Assistant `2.16.0-pre.1`, Inference `2.6.1`, Input System `1.20.0`, Multiplayer Center `1.0.1`, URP `17.5.0`, Test Framework `1.7.0`, and uGUI `2.5.0`.
- The existing Flutter project remains the behavioral reference. The migration synchronizer uses the checked-in allowlist `Tools/AssetMigration/asset-migration-manifest.json`; it copies only approved entries and never deletes Unity assets.

## Assemblies, scenes, and assets

- First-party production assemblies: `JoseonHunter.Domain`, `JoseonHunter.Content`, `JoseonHunter.Runtime`, `JoseonHunter.Presentation`, `JoseonHunter.Infrastructure`, and editor-only `JoseonHunter.Editor`.
- Test assemblies: `JoseonHunter.EditModeTests` and `JoseonHunter.PlayModeTests`.
- Enabled build scenes, in required navigation order: `Assets/JoseonHunter/Scenes/Bootstrap.unity`, `Lobby.unity`, then `Gameplay.unity`.
- The scene scaffold generator is available at **JoseonHunter/Setup/Generate Foundation Scenes**. It refuses to replace an open dirty foundation or non-foundation scene.
- Approved first-slice migrated assets and their hashes are inventoried in `Docs/Assets/first-slice-asset-inventory.md`. Production audio and fonts remain excluded until their source-ledger approval is recorded.

## Testing and validation

- Canonical EditMode entry point: `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1`.
- The 2026-07-26 foundation run passed all 26 EditMode tests; XML artifact: `Logs/editmode-results.xml` (generated and ignored). It includes seven scene-scaffold tests, package/settings tests, asset importer tests, migration-validator tests, and assembly-boundary coverage.
- The isolated synchronizer harness is `Tools/AssetMigration/Test-SyncFlutterAssets.ps1`; the foundation run passed.
- Official Unity MCP is the only approved provider. Its connected bridge successfully compiled and executed C# invoking `EditorApplication.ExecuteMenuItem("JoseonHunter/Setup/Generate Foundation Scenes")` (execution ID `1`, no compilation logs), and a Console read reported `errorCount: 0`.
- Console warnings observed through MCP were Unity/package/environment warnings (URP shader conversion, historical revoked MCP attempts, account API/signature), not first-party compile errors. Preserve and re-triage warnings instead of clearing them.

## Current capabilities and limitations

- The foundation has compilation and batch EditMode evidence, scene/build-settings evidence, and connected-MCP Editor command evidence.
- There is no Android Gradle artifact, device installation, PlayMode gameplay smoke test, performance capture, or visual-quality approval in this checkpoint. Those must be performed by the vertical-slice/release validation that requires them.
- Do not infer gameplay networking from the Multiplayer Center package; no gameplay networking is implemented.
