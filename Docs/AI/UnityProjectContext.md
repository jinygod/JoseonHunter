# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `D:\UnityProjects\JoseonHunter`
- Purpose: Unity destination for migrating the existing Flutter + Flame project at `C:\Users\전성진\Documents\뱀서라이크게임`
- Current state: Empty Unity project; no first-party assets, scripts, scenes, or build scenes exist yet.
- Last analyzed: 2026-07-26
- Last analyzed commit: Pre-initialization state; the repository was initialized after this onboarding pass.

## Confirmed Environment

- Unity version: 6000.5.5f1, revision `d16e074b49fd`
- Android Build Support: available (SDK, NDK, OpenJDK verified)
- Official Unity MCP: relay configured for Codex; Unity Editor approval and low-risk connection probes remain pending.
- Render pipeline: Built-in Render Pipeline
- Input system: Legacy Input Manager (`activeInputHandler: 0`); no Input System package is installed.
- Target platforms: Not yet configured. The source product targets landscape Android first.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Unity AI | Assistant 2.16.0-pre.1 and Inference 2.6.1 are direct dependencies. | Confirmed | `Packages/manifest.json` |
| Multiplayer | Multiplayer Center 1.0.1 is present, but there is no gameplay networking implementation. | Confirmed | `Packages/manifest.json`, empty `Assets/` |
| Rendering | No URP or HDRP package or render-pipeline asset is configured. | Confirmed | `Packages/manifest.json`, `ProjectSettings/GraphicsSettings.asset` |
| Testing | Unity Test Framework is present only as a transitive dependency; no first-party test assemblies or tests exist. | Confirmed | `Packages/packages-lock.json`, empty `Assets/` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/` | Empty destination for first-party game content. | Confirmed | Filesystem inspection |
| `Packages/` | Unity package manifest and resolved lock file. | Confirmed | `Packages/manifest.json`, `Packages/packages-lock.json` |
| `ProjectSettings/` | Unity 6000.5 project settings. | Confirmed | `ProjectSettings/ProjectVersion.txt` |
| `Library/`, `Temp/`, `Logs/` | Generated Unity Editor state; do not treat as source. | Confirmed | Unity project layout |

## Assembly Boundaries

No `.asmdef` or `.asmref` files exist. Assembly boundaries have not been established.

## Scenes And Startup Flow

- Build scenes: None.
- Likely startup scene: None.
- Scene loading flow: Not implemented.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Runtime architecture | Not established. | Confirmed | Empty `Assets/` |
| Data architecture | Not established; Flutter source currently owns content definitions, save data, settings, telemetry, and progression behavior. | Confirmed | Source-project structure and empty Unity project |
| Presentation | Not established; Flutter source currently uses Material widgets plus Flame. | Confirmed | Source-project `lib/app/`, `lib/game/` |

## Coding Conventions

- Namespace style: Not established.
- Serialized fields: Not established.
- Async: Not established.
- Comments/docs: Not established.

## Testing And Validation

- EditMode tests: None.
- PlayMode tests: None.
- CI/build validation: None.
- Migration validation should compare deterministic Unity domain behavior against the existing Flutter tests before replacing the source application.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| `unity.connection.status` | unavailable | No Unity MCP capability is exposed in the current Codex session. |
| `unity.editor.version` | unavailable | Read from project files instead. |
| `unity.console.read` | unavailable | No Unity MCP capability is exposed. |
| `unity.scene.list` | unavailable | Read from serialized assets and Build Settings instead. |
| `unity.scene.inspect` | unavailable | No Unity MCP capability is exposed. |
| `unity.buildsettings.read` | unavailable | Read from `EditorBuildSettings.asset` instead. |
| `unity.gameobject.inspect` | unavailable | No Unity MCP capability is exposed. |
| `unity.asset.search` | unavailable | Filesystem inspection is available. |
| `unity.package.read` | unavailable | Read from package files instead. |
| `unity.tests.list` | unavailable | Filesystem inspection is available. |
| `unity.tests.run` | unavailable | Unity batch-mode execution remains available through the installed editor if configured later. |
| `unity.playmode.read` | unavailable | No Unity MCP capability is exposed. |
| `unity.profiler.read` | unavailable | No Unity MCP capability is exposed. |

## Important Constraints

- Preserve the Flutter project as the behavioral reference until Unity reaches feature parity.
- Do not copy Flutter-generated folders or platform build output into Unity.
- Import only source-owned assets and retain licensing ledgers and attribution.
- Preserve pixel-art sampling, sprite geometry, audio loop behavior, and landscape Android intent during asset import.
- Track only source-owned Unity files; generated directories are excluded by the repository `.gitignore`.
- Do not infer multiplayer support from the Multiplayer Center package.

## Unknowns And Confidence

- The final Unity render pipeline, input package, scene architecture, and assembly boundaries require an approved migration design.
- Android player settings, orientation, package identifier, signing, and backend integrations are not configured.
- Save compatibility strategy between Flutter `SharedPreferences` data and Unity persistence is not yet approved.
- No automated Unity baseline can run until first-party tests and assemblies are created.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- Flutter source `README.md`
- Flutter source `pubspec.yaml`
- Flutter source directory layout under `lib/` and `assets/`

<!-- unity-onboarding:generated:end -->
