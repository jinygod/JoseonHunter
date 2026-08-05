# Project Health and Cleanup Report — 2026-08-06

## Status

**Ready with limitations.** The audited source compiles without C# warnings, all automated Unity tests pass, and an Android ARM64 IL2CPP development APK builds successfully. A physical Android device run, visual regression pass, and measured performance budget were not part of this cleanup validation.

## Scope and baseline

- Project: `D:\UnityProjects\JoseonHunter`
- Branch: `master`
- Unity: `6000.5.5f1 (d16e074b49fd)`
- Rendering/input/UI: URP 2D, Input System, uGUI/TMP
- Enabled build scenes, in order: `Bootstrap`, `Lobby`, `Gameplay`
- First-party assembly boundaries retained: Domain, Content, Runtime, Presentation, Infrastructure, Editor, EditModeTests, PlayModeTests
- Final tracked inventory before this report: 2,380 files, including 308 C# files and 838 PNG files
- Pre-existing local state preserved: 33 PNG `.meta` files under CombatChoices and `gakgung_shot/ui-icon.png.meta`. They were already dirty before the audit and were not staged by cleanup commits.

## Audit method

Deletion candidates were checked against all of the following before removal:

- serialized GUID/file-ID references in scenes, prefabs, assets, and metadata
- C# type, member, path, and name references
- `Resources.Load` and concatenated/dynamic path construction
- Unity callbacks, editor menu attributes, asset postprocessors, build entry points, tests, and reflection risk
- generator manifests, source-art provenance, licenses, and visual verification documents
- exact binary hashes for duplicate assets

Filename-only or single-reference heuristics were never used as deletion proof.

## Removed content

All removed repository files were tracked by Git and are recoverable from history.

| Category | Removed | Evidence |
|---|---:|---|
| Obsolete repository reports and structure | 5 old root task reports and 1 empty folder meta | No repository references; superseded by maintained plans/verification documents |
| Dead code | `LobbyHeroMotion.cs` and its `.meta` | No type, GUID, runtime, editor, or test reference |
| Superseded weapon art | 26 PNG/meta pairs (52 files) | Zero GUID/path/generator/test references; canonical runtime art remains |
| Exact duplicate premium art | 2 PNG/meta pairs plus 1 empty folder meta (5 files) | Byte-identical canonical copies already live under `Resources` and are runtime-loaded there |

Total tracked deletion: **65 files / 2,573,156 bytes**.

Breakdown by cleanup commit:

- `fc95bf7`: 8 files / 9,304 bytes
- `e1ace66`: 52 files / 141,102 bytes
- `dd8b1fa`: 5 files / 2,422,750 bytes

## Package cleanup

Removed 17 direct dependencies whose code, assemblies, serialized data, and workflows were unused:

- Services/tools: AI Assistant, AI Inference, Multiplayer Center
- Engine modules: Cloth, Director, Terrain Physics, Vehicles, Video, Wind, XR, Unity Analytics, base UnityWebRequest, and its AssetBundle/Audio/Texture/WWW modules

Promoted or added only dependencies with proven direct use:

- `com.unity.nuget.newtonsoft-json` for existing JSON code
- `com.unity.2d.sprite` for Unity 6 `ISpriteEditorDataProvider` sprite-sheet metadata access

Unity resolved the final manifest/lock graph and compiled all runtime, editor, and test assemblies successfully.

## Code hygiene

- Replaced obsolete object lookup calls with `FindAnyObjectByType` where ordering was irrelevant.
- Replaced obsolete TMP wrapping properties with `textWrappingMode`.
- Migrated Android named-target settings to `NamedBuildTarget.Android`.
- Replaced obsolete `TextureImporter.spritesheet` access with a tested `ISpriteEditorDataProvider` adapter.
- The sprite metadata adapter avoids writes when slices are unchanged and preserves existing sprite IDs when updates are necessary.
- Repeated compiler diagnostics fell from 358 warning lines in the baseline compile log to **0 C# warnings**.
- No speculative gameplay architecture rewrite was performed.

## Retained content and rationale

- `ArtSource`: retained as asset-generation provenance, not runtime duplication.
- `Artifacts`: retained because tracked screenshots are linked from verification documents; future output remains ignored.
- `SlotParts` jackpot bursts: retained because paths are assembled dynamically and covered by asset contract tests.
- Font license text: retained to preserve redistribution/license obligations.
- Similar animation frames and static/animated copies: retained where serialized slots or distinct presentation roles reference them.
- Unity callbacks, editor generators, command-line build methods, tests, and internal type collections: retained even when ordinary source-reference counts were low.

## Structural health findings

| Severity | Finding | Evidence / disposition |
|---|---|---|
| Pass | No missing scripts | 0 serialized `m_Script: {fileID: 0}` markers |
| Pass | Metadata integrity | 0 orphan tracked `.meta` files and 0 assets missing `.meta` after validation cleanup |
| Pass | Build scene flow | `Bootstrap`, `Lobby`, and `Gameplay` exist and are enabled in the intended order |
| Pass | Assembly/package boundaries | Runtime assemblies have no accidental UnityEditor dependency; final package graph resolves |
| Medium, environment | Windows Gradle Prefab cannot encode the Korean user profile in its generated batch file | First build failed with a corrupted `C:\Users\전성진\.gradle` classpath. Setting `GRADLE_USER_HOME=C:\COB1ED~1\.gradle` produced a successful build without source changes. Document this in Windows build automation. |
| Medium, unmeasured | No device performance budget was measured | Automated performance tests exist, but this audit does not claim frame-time, memory, allocation, or thermal compliance on target hardware. |
| Low | Much scene content is created by runtime bootstraps | Intentional current architecture; tests cover startup/navigation. A future prefab-first authoring migration would be a product/editor-workflow project, not unused-content cleanup. |

## Validation evidence

### Focused checks

- Lobby scene contract: 1/1 passed
- Superseded weapon asset contracts: 37/37 passed
- Premium lobby asset contracts: 4/4 passed
- Sprite/audio import profile contracts after Unity 6 migration: 11/11 passed
- Unity package resolution and script compile: exit 0, 0 C# errors, 0 C# warnings

### Full suites

- EditMode: **685/685 passed**, 0 failed, 0 skipped, 0 inconclusive; 36.2 seconds
- PlayMode: **244/244 passed**, 0 failed, 0 skipped, 0 inconclusive; 135.0 seconds
- Logs/results: ignored files under `D:\UnityProjects\JoseonHunter\Logs`

### Android target build

- Target: Android development APK, ARM64, IL2CPP
- Options: Development, Connect With Profiler, Allow Debugging
- Application ID: `com.jinygod.joseonhunter`
- Version: `0.1.0` / code 1
- Minimum/target SDK: 26 / 36
- Result: succeeded, exit 0, 0 C# warnings
- Artifact: `D:\UnityProjects\JoseonHunter\Builds\Android\JoseonHunter-development.apk`
- Size: **173,111,062 bytes**
- SHA-256: `7F90A1F5935F77169BFB7F4DEE6CE87E4B0F1E948CE4E437E1C2B75D286845E3`
- Previous ignored APK replaced: 173,444,358 bytes, SHA-256 `697735AA827B5A130630864C78D72FF6BB6F289945DBDDAEDFBFED116495149C`; it was not tracked by Git and is not recoverable from repository history.

The first build attempt intentionally remains documented: it completed C#/IL2CPP work but failed in Gradle Prefab because the generated batch file corrupted the Korean profile path. The successful retry used the same project commit and an ASCII Gradle-home alias.

## Recovery

Every source, asset, metadata, and document deletion in this cleanup is recoverable through Git. For a specific deleted path, inspect the deleting commit and restore from its parent, for example:

```powershell
git show <deleting-commit>^:<path>
git restore --source=<deleting-commit>^ -- <path>
```

The ignored APK replacement is the only material overwrite not recoverable through Git.

## Remaining validation limitations

- No APK installation or smoke run on a physical Android device
- No visual comparison of every UI/VFX state after cleanup
- No profiler capture with a defined frame-time, memory, GC, or thermal budget
- No iOS build in this Windows workspace

These limitations do not indicate a discovered regression, but they prevent labeling this cleanup as a release-candidate device validation.
