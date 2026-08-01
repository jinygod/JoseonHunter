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

## Portrait vertical-slice handoff (2026-08-01)

- Android portrait contract: `com.jinygod.joseonhunter`, version `0.1.0` (code `1`), min SDK 26, target SDK 36, ARM64, IL2CPP. The enabled build scenes are Bootstrap, Lobby, and Gameplay in that order. URP 2D is active; the serialized graphics-API list is empty, so the exact Android graphics API must be read from a successful player build rather than inferred.
- Reproducible commands: full EditMode `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode`; full PlayMode uses the same command with `-Platform playmode -Filter JoseonHunter.Tests.PlayMode`; Android development build uses `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1`. The build method applies the portrait contract, uses enabled EditorBuildSettings scenes, and requests Development, ConnectWithProfiler, and AllowDebugging.
- Java-to-Unity lifecycle mapping: `MonoBehaviour.Awake` is composition-time initialization, `Update` is the frame callback, and `OnDisable` is the cleanup/unsubscribe callback. Keep the ownership and teardown semantics explicit; do not treat Unity callbacks as ordinary Java constructors/finalizers.
- Treat ScriptableObject assets as versioned configuration data, not service locators. Inspector references are explicit application wiring/DI edges. Coroutines are Unity-frame-scheduled routines; do not equate them with `Task`, and use cancellation tokens only for task-based async operations that own them.
- `GameFlowCoordinator` is the sole production `Time.timeScale` owner. Gameplay uses scaled time; modal animation uses unscaled time, and modal confirmation controls the transition back to gameplay.
- Portrait visual capture command: run Unity batchmode with `-executeMethod JoseonHunter.Editor.Scenes.PortraitStateValidationCapture.CaptureInBatchMode` and **omit `-nographics`** because it synchronously calls `Camera.Render`. It captures 720x1280, 1080x1920, 1080x2340, 1170x2532, and 1440x3200 for real Gameplay, level-up, appraisal, and resumed combat, with persistent reload recovery and temporary reversible routing of the production overlay canvas through the gameplay camera. Do not use synthetic overlays.
- PixelLab ledger: starting balance 1,512; ending balance 1,512; accepted assets: existing imported combat frames and weapon presentation assets; rejected/generated assets: none; generation cost: 0. Task 11 established that existing assets were sufficient.

## Portrait typography and appraisal handoff (2026-08-01)

- Runtime font roles are explicit: `ChosunGs-Dynamic SDF` for clean Gungseo-style titles, `MaruBuri-Regular/SemiBold-Dynamic SDF` for UI and item text, and `BlackAndWhitePicture-Dynamic SDF` for large damage numbers. Source font licenses are checked in beside the fonts under `Assets/JoseonHunter/Art/Fonts/Licenses/`; dynamic atlas data must be cleared after validation and must not be committed.
- Weapon-affix display strings are centralized through `WeaponAffixDisplayFormatter`. Player-facing affix labels are Korean, the top tier is `최대 추가옵션`, and the appraisal confirmation label is `확인`.
- The appraisal presenter owns the reel stop, post-stop 0-to-result count-up, tick pulse, final punch, and confirmation lock. Standard results count for 0.75 s; high/perfect results count for 0.90 s. Text over the dark reel uses the high-contrast panel palette, not hanji ink.
- The portrait gameplay camera uses orthographic size 18 (formerly 7.25), while the independent spawn camera profile uses 8.5. Treat these as separate authored profiles.
- Latest verification record: `Docs/Verification/2026-08-01-portrait-typography-appraisal-camera.md`. Full EditMode passed 544/544, focused appraisal PlayMode passed 28/28, the Android development APK built successfully, and all 20 portrait state captures were visually inspected. The complete PlayMode suite still has 79 deferred weapon-potential combat failures, so the project-wide test status is not green.

## Combat performance and pickup handoff (2026-08-01)

- Projectile performance uses two conservative broad phases before exact pixel contact: active-pixel world bounds in `PixelMaskContactService`, followed by a once-per-tick swept candidate list in `LinearProjectileExecutor`. Never remove the exact final pixel test or change target order when optimizing this path.
- The final eight-level-five-weapon/100-target direct CPU measurement is 3.3120 ms average per 0.05-second simulation tick with 0 B managed allocation. Gakgung fell from 271.1035 to 1.6694 ms in the isolated harness; Singijeon fell from 60.8499 to 0.8888 ms.
- Starting pickup attraction radius is 0.58 world units; final collection remains 0.42. Warding bell still adds 0.7.
- Visual assets are checked in on `master`; runtime combat animation is PNG-frame/code-driven rather than Mecanim `.anim`/`.controller` assets.
- First and repeated Gameplay loads measured 376.874 and 375.783 ms after Editor compilation/import. A static font-atlas experiment was slower and was rejected. Treat immediate post-compile Editor delay separately from runtime combat performance.
- Latest evidence: `Docs/Verification/2026-08-01-combat-performance-and-pickup-range.md`. Full EditMode 544/544, performance 3/3, pickup 2/2, load 9/9, eight-weapon behavior 9/9, and Android development build passed. Device profiling remains outstanding.
