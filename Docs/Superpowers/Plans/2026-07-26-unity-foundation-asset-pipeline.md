# Unity Foundation and Asset Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a connected, testable Unity 6000.5 mobile foundation with the official Unity MCP bridge, URP 2D, the Input System, clean assembly boundaries, repeatable Flutter-asset synchronization, validated import settings, and empty Bootstrap/Lobby/Gameplay scenes ready for the first combat vertical slice.

**Architecture:** Unity packages and project settings establish the Android-first runtime. First-party code lives under `Assets/JoseonHunter` in one-way assemblies, while source-owned Flutter assets are copied through a checked-in manifest and PowerShell synchronizer instead of ad-hoc file copying. Editor tests validate project settings, assembly boundaries, assets, imports, licenses, and build scenes before gameplay work begins.

**Tech Stack:** Unity 6000.5.5f1, official Unity MCP from `com.unity.ai.assistant` 2.16.0-pre.1, URP 2D, Input System 1.17-compatible package, uGUI, TextMeshPro, NUnit/Unity Test Framework, PowerShell 7 or Windows PowerShell 5.1, Git/GitHub.

## Global Constraints

- Unity project root is exactly `D:\UnityProjects\JoseonHunter`.
- Flutter reference root is supplied through `JOSEON_FLUTTER_ROOT`; local default is `C:\Users\전성진\Documents\뱀서라이크게임`.
- Unity Editor version remains exactly 6000.5.5f1.
- Target platform is landscape Android; Windows Editor remains the local development host.
- Use only the official Unity MCP provider already bundled with `com.unity.ai.assistant`; do not add a community MCP.
- Configure the MCP relay with `--mcp --project-path D:\UnityProjects\JoseonHunter`.
- Keep Unity MCP batch-mode auto-approval disabled.
- Convert the empty project to URP with a 2D Renderer before creating materials or gameplay prefabs.
- Use the Input System only (`activeInputHandler: 1`), not legacy or Both mode.
- Runtime UI uses uGUI and TextMeshPro.
- Do not read or migrate Flutter SharedPreferences. Unity save schema starts at version 1 in a later vertical-slice plan.
- Never copy Flutter generated directories, build output, `.dart_tool`, platform runner output, or unlisted files.
- Preserve asset and audio rights ledgers and font license files.
- Do not hand-edit `.unity`, `.prefab`, `.asset`, `.mat`, `.controller`, or `.anim` YAML; create and modify them through Unity Editor APIs or Unity MCP.
- Each task ends with a focused test and a Git commit.

---

## Planned File Structure

```text
Assets/JoseonHunter/
  Art/
    Characters/
    Enemies/
    Bosses/
    Weapons/
    VFX/
    Stages/
    UI/
    Fonts/
  Audio/
    Music/
    SFX/
    UI/
  Data/
  Prefabs/
  Scenes/
  Scripts/
    Domain/
    Content/
    Runtime/
    Presentation/
    Infrastructure/
    Editor/
  Tests/
    EditMode/
    PlayMode/
Docs/
  AI/
  Assets/
  Superpowers/
Tools/
  AssetMigration/
  Unity/
```

Assembly responsibilities:

- `JoseonHunter.Domain`: pure C# rules and value types; no UnityEngine reference.
- `JoseonHunter.Content`: ScriptableObject authoring types and validation; references Domain.
- `JoseonHunter.Runtime`: gameplay orchestration; references Domain, Content, and Input System.
- `JoseonHunter.Presentation`: UI, sprites, audio, VFX, and haptics; references Domain, Content, Runtime, Input System, and URP.
- `JoseonHunter.Infrastructure`: save and online adapters; references Domain.
- `JoseonHunter.Editor`: editor-only setup, migration, and validation tools.
- `JoseonHunter.EditModeTests` and `JoseonHunter.PlayModeTests`: test assemblies only.

---

### Task 1: Install Android Support and Connect Official Unity MCP

**Files:**
- Modify outside repository: `C:\Users\전성진\.codex\config.toml`
- Modify through Unity: `ProjectSettings/Packages/com.unity.ai.assistant/Settings.json`
- Modify: `Docs/AI/UnityProjectContext.md`

**Interfaces:**
- Consumes: installed Unity Editor 6000.5.5f1 and relay `C:\Users\전성진\.unity\relay\relay_win.exe`.
- Produces: Android playback engine, approved `unity_mcp` Codex server, and verified low-risk Unity console/scene access.

- [ ] **Step 1: Record the missing Android module baseline**

Run:

```powershell
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor'
Test-Path "$editor\Data\PlaybackEngines\AndroidPlayer"
```

Expected: `False`.

- [ ] **Step 2: Install Android Build Support with child modules**

Run from an elevated PowerShell only if Unity Hub requests elevation:

```powershell
& 'C:\Program Files\Unity Hub\Unity Hub.exe' -- --headless install-modules `
  --version 6000.5.5f1 `
  --module android `
  --childModules
```

Expected: exit code `0`; the AndroidPlayer directory, SDK/NDK, and OpenJDK appear. If the installed Hub no longer supports the deprecated headless command, use Hub → Installs → 6000.5.5f1 → Manage → Add modules and select Android Build Support, Android SDK & NDK Tools, and OpenJDK.

- [ ] **Step 3: Verify all Android components**

Run:

```powershell
$android = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Data\PlaybackEngines\AndroidPlayer'
@(
  "$android\UnityEditor.Android.Extensions.dll",
  "$android\SDK",
  "$android\NDK",
  "$android\OpenJDK"
) | ForEach-Object { [pscustomobject]@{ Path = $_; Exists = Test-Path $_ } }
```

Expected: every `Exists` value is `True`.

- [ ] **Step 4: Add the official relay to Codex configuration**

Append this exact TOML section only if `[mcp_servers.unity_mcp]` is absent:

```toml
[mcp_servers.unity_mcp]
command = "C:\\Users\\전성진\\.unity\\relay\\relay_win.exe"
args = ["--mcp", "--project-path", "D:\\UnityProjects\\JoseonHunter"]
enabled = true
```

Do not expose or modify unrelated MCP server entries.

- [ ] **Step 5: Start and approve the bridge**

Open `D:\UnityProjects\JoseonHunter` in Unity 6000.5.5f1. Go to:

```text
Edit > Project Settings > AI > Unity MCP Server
```

Confirm `Unity Bridge` is `Running`, `Auto-approve in Batch Mode` is off, and validation is `Standard`. Start Codex's Unity MCP connection and have the user select `Allow` for the pending Codex client.

- [ ] **Step 6: Run low-risk connection probes**

Use the connected Unity tools to:

1. read Unity version;
2. read the Console without clearing it;
3. list scenes without opening or saving them.

Expected: version `6000.5.5f1`, zero first-party scenes, and no compile errors.

- [ ] **Step 7: Update and commit the context record**

Update `Docs/AI/UnityProjectContext.md`:

```markdown
- Android Build Support: available (SDK, NDK, OpenJDK verified)
- Official Unity MCP: available and approved for Codex
```

Run:

```powershell
git add Docs/AI/UnityProjectContext.md
git commit -m "chore: connect Unity development tooling"
```

---

### Task 2: Install Packages and Configure the Android URP 2D Project

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `Packages/packages-lock.json`
- Create through Unity: `Assets/JoseonHunter/Settings/Rendering/JoseonHunterUniversalRenderPipeline.asset`
- Create through Unity: `Assets/JoseonHunter/Settings/Rendering/JoseonHunterRenderer2D.asset`
- Modify through Unity: `ProjectSettings/GraphicsSettings.asset`
- Modify through Unity: `ProjectSettings/QualitySettings.asset`
- Modify through Unity: `ProjectSettings/ProjectSettings.asset`
- Create: `Assets/JoseonHunter/Tests/EditMode/ProjectFoundationTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/JoseonHunter.EditModeTests.asmdef`

**Interfaces:**
- Consumes: Unity Editor, Android module, official MCP.
- Produces: URP 2D render pipeline, Input System-only mode, landscape Android settings, and a direct test dependency.

- [ ] **Step 1: Install direct package dependencies**

In Unity Package Manager, install compatible released versions of:

```text
com.unity.render-pipelines.universal
com.unity.inputsystem
com.unity.ugui
com.unity.test-framework
```

Let Unity 6000.5 resolve compatible versions and pin them in `Packages/manifest.json` and `Packages/packages-lock.json`. Do not manually guess a URP version. Confirm Input System resolves to the Unity 6000-compatible 1.17 release line.

- [ ] **Step 2: Create the failing project foundation test**

Create `Assets/JoseonHunter/Tests/EditMode/ProjectFoundationTests.cs`:

```csharp
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class ProjectFoundationTests
    {
        [Test]
        public void ProjectUsesUrp2DAndInputSystemOnly()
        {
            Assert.That(
                GraphicsSettings.defaultRenderPipeline,
                Is.InstanceOf<UniversalRenderPipelineAsset>());

            var settings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");
            StringAssert.Contains("activeInputHandler: 1", settings);
        }

        [Test]
        public void AndroidPlayerIsLandscapeOnly()
        {
            Assert.That(
                PlayerSettings.defaultInterfaceOrientation,
                Is.EqualTo(UIOrientation.LandscapeLeft));
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
        }
    }
}
```

Create `JoseonHunter.EditModeTests.asmdef` with:

```json
{
  "name": "JoseonHunter.EditModeTests",
  "rootNamespace": "JoseonHunter.Tests.EditMode",
  "references": [
    "Unity.RenderPipelines.Universal.Runtime"
  ],
  "includePlatforms": ["Editor"],
  "optionalUnityReferences": ["TestAssemblies"],
  "autoReferenced": false
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' `
  -runTests -testPlatform editmode `
  -testFilter 'JoseonHunter.Tests.EditMode.ProjectFoundationTests' `
  -testResults 'D:\UnityProjects\JoseonHunter\Logs\project-foundation-tests.xml' `
  -logFile 'D:\UnityProjects\JoseonHunter\Logs\project-foundation-tests.log' -quit
```

Expected: FAIL because the project is still Built-in and uses legacy input.

- [ ] **Step 4: Configure URP 2D through the Editor**

Use Unity's `Assets > Create > Rendering > URP Asset (with 2D Renderer)` command. Save and rename the generated assets to the exact paths listed above. Assign the URP asset in Graphics Settings and every active Quality level.

Set:

```text
Color Space: Linear
Active Input Handling: Input System Package (New)
Default Orientation: Landscape Left
Allowed Orientations for Auto Rotation: Landscape Left + Landscape Right only
Run In Background: off
Application Identifier (Android): com.jinygod.joseonhunter
Version: 0.1.0
Bundle Version Code: 1
Scripting Backend (Android): IL2CPP
Target Architectures: ARM64 only
```

Switch the active build target to Android after the module is installed.

- [ ] **Step 5: Run the foundation tests**

Repeat Step 3.

Expected: PASS with zero compile errors.

- [ ] **Step 6: Commit**

Run:

```powershell
git add Packages ProjectSettings Assets/JoseonHunter/Settings Assets/JoseonHunter/Tests/EditMode
git commit -m "chore: configure Unity mobile foundation"
```

---

### Task 3: Create First-Party Assembly Boundaries

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/JoseonHunter.Domain.asmdef`
- Create: `Assets/JoseonHunter/Scripts/Domain/ProjectIdentity.cs`
- Create: `Assets/JoseonHunter/Scripts/Content/JoseonHunter.Content.asmdef`
- Create: `Assets/JoseonHunter/Scripts/Content/AssemblyMarker.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/JoseonHunter.Runtime.asmdef`
- Create: `Assets/JoseonHunter/Scripts/Runtime/AssemblyMarker.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/JoseonHunter.Presentation.asmdef`
- Create: `Assets/JoseonHunter/Scripts/Presentation/AssemblyMarker.cs`
- Create: `Assets/JoseonHunter/Scripts/Infrastructure/JoseonHunter.Infrastructure.asmdef`
- Create: `Assets/JoseonHunter/Scripts/Infrastructure/AssemblyMarker.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/JoseonHunter.Editor.asmdef`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssemblyMarker.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/JoseonHunter.EditModeTests.asmdef`
- Create: `Assets/JoseonHunter/Tests/EditMode/AssemblyBoundaryTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/JoseonHunter.PlayModeTests.asmdef`
- Create: `Assets/JoseonHunter/Tests/PlayMode/AssemblyPresencePlayModeTests.cs`

**Interfaces:**
- Produces: `JoseonHunter.Domain.ProjectIdentity.ProductName`, `.UnityVersion`, and `.SaveSchemaVersion`.
- Produces: named assemblies used by all later plans.

- [ ] **Step 1: Write the failing assembly test**

Create `AssemblyBoundaryTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class AssemblyBoundaryTests
    {
        [Test]
        public void RequiredFirstPartyAssembliesAreLoaded()
        {
            var names = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName().Name)
                .ToHashSet();

            Assert.That(names, Does.Contain("JoseonHunter.Domain"));
            Assert.That(names, Does.Contain("JoseonHunter.Content"));
            Assert.That(names, Does.Contain("JoseonHunter.Runtime"));
            Assert.That(names, Does.Contain("JoseonHunter.Presentation"));
            Assert.That(names, Does.Contain("JoseonHunter.Infrastructure"));
            Assert.That(names, Does.Contain("JoseonHunter.Editor"));
        }
    }
}
```

Add all six assembly names to the EditMode test asmdef references.

- [ ] **Step 2: Run the test to verify it fails**

Run the EditMode batch command from Task 2 with:

```text
-testFilter JoseonHunter.Tests.EditMode.AssemblyBoundaryTests
```

Expected: compile failure or assertion failure because the assemblies do not exist.

- [ ] **Step 3: Create the assembly definitions**

Use this Domain definition:

```json
{
  "name": "JoseonHunter.Domain",
  "rootNamespace": "JoseonHunter.Domain",
  "references": [],
  "noEngineReferences": true,
  "autoReferenced": false
}
```

Use these reference lists:

```text
JoseonHunter.Content       -> JoseonHunter.Domain
JoseonHunter.Runtime       -> JoseonHunter.Domain, JoseonHunter.Content, Unity.InputSystem
JoseonHunter.Presentation  -> JoseonHunter.Domain, JoseonHunter.Content, JoseonHunter.Runtime, Unity.InputSystem, Unity.RenderPipelines.Universal.Runtime
JoseonHunter.Infrastructure-> JoseonHunter.Domain
JoseonHunter.Editor        -> all five first-party runtime assemblies; includePlatforms Editor
```

All definitions use `autoReferenced: false`. The Editor definition uses:

```json
"includePlatforms": ["Editor"]
```

- [ ] **Step 4: Add concrete types to every production assembly**

Create `ProjectIdentity.cs`:

```csharp
namespace JoseonHunter.Domain
{
    public static class ProjectIdentity
    {
        public const string ProductName = "JoseonHunter";
        public const string UnityVersion = "6000.5.5f1";
        public const int SaveSchemaVersion = 1;
    }
}
```

Create one public marker in each remaining assembly, changing only the namespace:

```csharp
namespace JoseonHunter.Content
{
    public static class AssemblyMarker { }
}
```

Use namespaces `JoseonHunter.Runtime`, `JoseonHunter.Presentation`,
`JoseonHunter.Infrastructure`, and `JoseonHunter.Editor` for their respective
files. These markers prevent otherwise empty asmdefs from disappearing and
provide stable targets for assembly-boundary tests.

- [ ] **Step 5: Create and verify the PlayMode test assembly**

Create `JoseonHunter.PlayModeTests.asmdef`:

```json
{
  "name": "JoseonHunter.PlayModeTests",
  "rootNamespace": "JoseonHunter.Tests.PlayMode",
  "references": [
    "JoseonHunter.Domain",
    "JoseonHunter.Content",
    "JoseonHunter.Runtime",
    "JoseonHunter.Presentation",
    "JoseonHunter.Infrastructure"
  ],
  "optionalUnityReferences": ["TestAssemblies"],
  "autoReferenced": false
}
```

Create `AssemblyPresencePlayModeTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class AssemblyPresencePlayModeTests
    {
        [UnityTest]
        public IEnumerator ProductionAssembliesAreResolvable()
        {
            Assert.That(JoseonHunter.Domain.ProjectIdentity.ProductName,
                Is.EqualTo("JoseonHunter"));
            Assert.That(typeof(JoseonHunter.Runtime.AssemblyMarker), Is.Not.Null);
            yield return null;
        }
    }
}
```

Run the Unity batch command with `-testPlatform playmode` and
`-testFilter JoseonHunter.Tests.PlayMode.AssemblyPresencePlayModeTests`.

Expected: PASS.

- [ ] **Step 6: Run all EditMode tests**

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add Assets/JoseonHunter/Scripts Assets/JoseonHunter/Tests
git commit -m "chore: establish Unity assembly boundaries"
```

---

### Task 4: Build a Manifest-Driven Asset Synchronizer

**Files:**
- Create: `Tools/AssetMigration/asset-migration-manifest.json`
- Create: `Tools/AssetMigration/Sync-FlutterAssets.ps1`
- Create: `Tools/AssetMigration/Test-SyncFlutterAssets.ps1`
- Create: `Docs/Assets/asset-migration-policy.md`

**Interfaces:**
- Consumes: `JOSEON_FLUTTER_ROOT` and JSON entries with `source`, `destination`, `profile`, and `licenseStatus`.
- Produces: copied Unity assets or documentation, deterministic SHA-256 report, non-zero exit on missing/unapproved sources.

- [ ] **Step 1: Create a failing synchronizer test**

`Test-SyncFlutterAssets.ps1` creates temporary `source`, `unity`, and manifest paths, then verifies:

```powershell
$ErrorActionPreference = 'Stop'
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('joseon-assets-' + [guid]::NewGuid())
$source = Join-Path $sandbox 'source'
$unity = Join-Path $sandbox 'unity'
New-Item -ItemType Directory -Path (Join-Path $source 'assets\images') -Force | Out-Null
New-Item -ItemType Directory -Path $unity -Force | Out-Null
Set-Content -LiteralPath (Join-Path $source 'assets\images\hero.png') -Value 'fixture'

$manifest = @{
  version = 1
  entries = @(@{
    source = 'assets/images/hero.png'
    destination = 'Assets/JoseonHunter/Art/Characters/hero.png'
    profile = 'pixel'
    licenseStatus = 'approved'
  })
} | ConvertTo-Json -Depth 5
$manifestPath = Join-Path $sandbox 'manifest.json'
Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding UTF8

& "$PSScriptRoot\Sync-FlutterAssets.ps1" `
  -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
if ($LASTEXITCODE -ne 0) { throw "sync failed: $LASTEXITCODE" }

$copied = Join-Path $unity 'Assets\JoseonHunter\Art\Characters\hero.png'
if (-not (Test-Path $copied)) { throw 'approved asset was not copied' }

$bad = $manifest -replace '"approved"', '"unresolved"'
Set-Content -LiteralPath $manifestPath -Value $bad -Encoding UTF8
& "$PSScriptRoot\Sync-FlutterAssets.ps1" `
  -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
if ($LASTEXITCODE -eq 0) { throw 'unresolved license did not block sync' }

Remove-Item -LiteralPath $sandbox -Recurse -Force
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AssetMigration/Test-SyncFlutterAssets.ps1
```

Expected: FAIL because `Sync-FlutterAssets.ps1` does not exist.

- [ ] **Step 3: Implement the synchronizer**

`Sync-FlutterAssets.ps1` accepts:

```powershell
param(
  [Parameter(Mandatory)][string]$SourceRoot,
  [Parameter(Mandatory)][string]$UnityRoot,
  [Parameter(Mandatory)][string]$ManifestPath,
  [switch]$DryRun
)
```

For each entry:

1. require `licenseStatus -eq 'approved'`;
2. resolve source beneath `$SourceRoot`;
3. resolve the project-relative destination beneath `$UnityRoot`;
4. allow only normalized destinations beneath `$UnityRoot\Assets\JoseonHunter`
   or `$UnityRoot\Docs\Assets`, rejecting every other prefix and all path traversal;
5. fail if source is missing;
6. create the destination directory;
7. copy only when SHA-256 differs;
8. print one object containing source, destination, profile, hash, and action;
9. exit `1` if any entry failed, otherwise `0`.

Use `Get-FileHash -Algorithm SHA256`, `Copy-Item -LiteralPath`, and
`ConvertFrom-Json`. Never delete destination files automatically.

- [ ] **Step 4: Add the checked-in first-slice manifest**

The initial manifest includes exact mappings for:

```text
assets/images/player/rookie_constable_player_32.png -> Assets/JoseonHunter/Art/Characters/rookie_constable_player_32.png
assets/images/characters/lobby/rookie_constable.png -> Assets/JoseonHunter/Art/Characters/Lobby/rookie_constable.png
assets/images/monsters/plague_rat_swarm_128.png -> Assets/JoseonHunter/Art/Enemies/plague_rat_swarm_128.png
assets/images/monsters/bandit_128.png -> Assets/JoseonHunter/Art/Enemies/bandit_128.png
assets/images/monsters/vengeful_spirit_128.png -> Assets/JoseonHunter/Art/Enemies/vengeful_spirit_128.png
assets/images/monsters/dokkaebi_128.png -> Assets/JoseonHunter/Art/Enemies/dokkaebi_128.png
assets/images/monsters/fallen_general_64.png -> Assets/JoseonHunter/Art/Bosses/fallen_general_64.png
assets/images/tiles/moonlit_office_tiles_128.png -> Assets/JoseonHunter/Art/Stages/moonlit_office_tiles_128.png
assets/images/props/moonlit_office_props_128.png -> Assets/JoseonHunter/Art/Stages/moonlit_office_props_128.png
assets/images/vfx/hwando/hwando_windup_128.png -> Assets/JoseonHunter/Art/VFX/Hwando/hwando_windup_128.png
assets/images/vfx/hwando/hwando_strike_128.png -> Assets/JoseonHunter/Art/VFX/Hwando/hwando_strike_128.png
assets/images/vfx/hwando/hwando_recovery_128.png -> Assets/JoseonHunter/Art/VFX/Hwando/hwando_recovery_128.png
assets/images/vfx/hwando/hwando_contact_128.png -> Assets/JoseonHunter/Art/VFX/Hwando/hwando_contact_128.png
assets/audio/music/menu.ogg -> Assets/JoseonHunter/Audio/Music/menu.ogg
assets/audio/music/battle.ogg -> Assets/JoseonHunter/Audio/Music/battle.ogg
assets/audio/music/boss.ogg -> Assets/JoseonHunter/Audio/Music/boss.ogg
assets/audio/music/victory.ogg -> Assets/JoseonHunter/Audio/Music/victory.ogg
assets/audio/music/defeat.ogg -> Assets/JoseonHunter/Audio/Music/defeat.ogg
assets/audio/sfx/hwando.ogg -> Assets/JoseonHunter/Audio/SFX/hwando.ogg
assets/audio/sfx/experience.ogg -> Assets/JoseonHunter/Audio/SFX/experience.ogg
assets/audio/sfx/enemy_death.ogg -> Assets/JoseonHunter/Audio/SFX/enemy_death.ogg
assets/audio/sfx/player_hit.ogg -> Assets/JoseonHunter/Audio/SFX/player_hit.ogg
assets/audio/sfx/level_up.ogg -> Assets/JoseonHunter/Audio/SFX/level_up.ogg
assets/audio/sfx/boss_warning.ogg -> Assets/JoseonHunter/Audio/SFX/boss_warning.ogg
assets/audio/ui/confirm.ogg -> Assets/JoseonHunter/Audio/UI/confirm.ogg
assets/audio/ui/back.ogg -> Assets/JoseonHunter/Audio/UI/back.ogg
assets/fonts/SongMyung-Regular.ttf -> Assets/JoseonHunter/Art/Fonts/SongMyung-Regular.ttf
assets/fonts/GowunBatang-Regular.ttf -> Assets/JoseonHunter/Art/Fonts/GowunBatang-Regular.ttf
assets/fonts/GowunBatang-Bold.ttf -> Assets/JoseonHunter/Art/Fonts/GowunBatang-Bold.ttf
assets/fonts/licenses/SongMyung-OFL.txt -> Assets/JoseonHunter/Art/Fonts/Licenses/SongMyung-OFL.txt
assets/fonts/licenses/GowunBatang-OFL.txt -> Assets/JoseonHunter/Art/Fonts/Licenses/GowunBatang-OFL.txt
docs/assets/asset-rights-ledger.csv -> Docs/Assets/asset-rights-ledger.csv
docs/assets/audio-rights-ledger.csv -> Docs/Assets/audio-rights-ledger.csv
```

Runtime PNG entries use `profile: pixel`; lobby art uses `ui`; music uses
`music`; short audio uses `sfx`; fonts and ledgers use `raw`. Every listed
entry starts with `licenseStatus: approved` only if the source ledger confirms
it; otherwise omit it from the runnable manifest and record it in the policy
document as blocked.

- [ ] **Step 5: Run the synchronizer tests and a dry run**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AssetMigration/Test-SyncFlutterAssets.ps1
$env:JOSEON_FLUTTER_ROOT = 'C:\Users\전성진\Documents\뱀서라이크게임'
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AssetMigration/Sync-FlutterAssets.ps1 `
  -SourceRoot $env:JOSEON_FLUTTER_ROOT `
  -UnityRoot 'D:\UnityProjects\JoseonHunter' `
  -ManifestPath 'D:\UnityProjects\JoseonHunter\Tools\AssetMigration\asset-migration-manifest.json' `
  -DryRun
```

Expected: tests PASS; dry run reports only approved, existing sources and does
not create files.

- [ ] **Step 6: Commit**

```powershell
git add Tools/AssetMigration Docs/Assets
git commit -m "feat: add manifest driven asset migration"
```

---

### Task 5: Enforce Unity Import Profiles

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/AssetImportProfileTests.cs`

**Interfaces:**
- Consumes: asset destination folders from Task 4.
- Produces: deterministic sprite, audio, and font importer settings.

- [ ] **Step 1: Write failing importer tests**

Create test cases that load importers for one synchronized pixel sprite, music
clip, and SFX clip and assert:

```csharp
Assert.That(texture.textureType, Is.EqualTo(TextureImporterType.Sprite));
Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
Assert.That(texture.mipmapEnabled, Is.False);
Assert.That(texture.spritePixelsPerUnit, Is.EqualTo(32f));

Assert.That(music.defaultSampleSettings.loadType,
    Is.EqualTo(AudioClipLoadType.Streaming));
Assert.That(sfx.defaultSampleSettings.loadType,
    Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
```

For Android texture settings assert:

```csharp
var android = texture.GetPlatformTextureSettings("Android");
Assert.That(android.overridden, Is.True);
Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_6x6));
```

Large UI/background profiles use Bilinear filtering; pixel profiles use Point.

- [ ] **Step 2: Run the importer tests to verify failure**

Expected: FAIL because default Unity import settings do not match.

- [ ] **Step 3: Implement `JoseonAssetPostprocessor`**

Dispatch strictly by project-relative prefix:

```csharp
private const string PixelRoot = "Assets/JoseonHunter/Art/";
private const string MusicRoot = "Assets/JoseonHunter/Audio/Music/";
private const string SfxRoot = "Assets/JoseonHunter/Audio/SFX/";
private const string UiAudioRoot = "Assets/JoseonHunter/Audio/UI/";
```

`OnPreprocessTexture` sets Sprite, Point, no mipmap, 32 PPU, alpha transparency,
and Android ASTC 6x6 for gameplay art. Assets under `Art/UI` and
`Art/Characters/Lobby` use Bilinear instead of Point.

`OnPreprocessAudio` sets music to Streaming and SFX/UI to DecompressOnLoad,
Vorbis, mono only when the source is mono, and preserves sample rate.

- [ ] **Step 4: Synchronize and import the first-slice assets**

Run the non-dry synchronizer from Task 4, then call
`AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport)` through Unity
MCP or the Editor.

- [ ] **Step 5: Run importer tests**

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Art Assets/JoseonHunter/Audio `
  Assets/JoseonHunter/Scripts/Editor/AssetImport `
  Assets/JoseonHunter/Tests/EditMode/AssetImportProfileTests.cs `
  Docs/Assets
git commit -m "feat: import first slice source assets"
```

---

### Task 6: Add Asset and License Validation

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetImport/AssetMigrationManifest.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetImport/AssetMigrationValidator.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/AssetMigrationValidatorTests.cs`
- Create: `Tools/Unity/Test-Unity.ps1`

**Interfaces:**
- Produces: `AssetMigrationValidator.Validate(string manifestPath) : IReadOnlyList<string>`.
- Produces: `Test-Unity.ps1` as the canonical local EditMode test entrypoint.

- [ ] **Step 1: Write validator tests**

Test these exact failures:

```text
duplicate destination
destination outside Assets/JoseonHunter or Docs/Assets
missing destination file
licenseStatus other than approved
pixel profile imported with mipmaps
missing SongMyung or GowunBatang license
```

Also assert the checked-in manifest returns an empty error list after Task 5.

- [ ] **Step 2: Run tests to verify failure**

Expected: compile failure because the validator types do not exist.

- [ ] **Step 3: Implement manifest models and validator**

Use `JsonUtility` serializable types:

```csharp
[System.Serializable]
public sealed class AssetMigrationManifest
{
    public int version;
    public AssetMigrationEntry[] entries;
}

[System.Serializable]
public sealed class AssetMigrationEntry
{
    public string source;
    public string destination;
    public string profile;
    public string licenseStatus;
}
```

Return deterministic, sorted strings from:

```csharp
public static IReadOnlyList<string> Validate(string manifestPath)
```

The validator reads the manifest, checks paths and licenses, resolves Unity
importers with `AssetImporter.GetAtPath`, and never modifies assets.

- [ ] **Step 4: Create the canonical Unity test script**

`Tools/Unity/Test-Unity.ps1` accepts `-Filter` and runs:

```powershell
param([string]$Filter = 'JoseonHunter.Tests.EditMode')
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$logs = Join-Path $root 'Logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
& $unity -batchmode -nographics -projectPath $root `
  -runTests -testPlatform editmode -testFilter $Filter `
  -testResults (Join-Path $logs 'editmode-results.xml') `
  -logFile (Join-Path $logs 'editmode.log') -quit
exit $LASTEXITCODE
```

- [ ] **Step 5: Run all EditMode tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Editor/AssetImport `
  Assets/JoseonHunter/Tests/EditMode Tools/Unity
git commit -m "test: validate migrated Unity assets"
```

---

### Task 7: Generate Bootstrap, Lobby, and Gameplay Scene Scaffolds

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/SceneScaffoldGenerator.cs`
- Create through Editor API: `Assets/JoseonHunter/Scenes/Bootstrap.unity`
- Create through Editor API: `Assets/JoseonHunter/Scenes/Lobby.unity`
- Create through Editor API: `Assets/JoseonHunter/Scenes/Gameplay.unity`
- Modify through Editor API: `ProjectSettings/EditorBuildSettings.asset`
- Create: `Assets/JoseonHunter/Tests/EditMode/SceneScaffoldTests.cs`

**Interfaces:**
- Produces: `SceneScaffoldGenerator.Generate()` and the three stable scene paths.
- Produces build order: Bootstrap index 0, Lobby index 1, Gameplay index 2.

- [ ] **Step 1: Write the failing scene test**

Assert:

```csharp
var expected = new[]
{
    "Assets/JoseonHunter/Scenes/Bootstrap.unity",
    "Assets/JoseonHunter/Scenes/Lobby.unity",
    "Assets/JoseonHunter/Scenes/Gameplay.unity"
};
CollectionAssert.AreEqual(
    expected,
    EditorBuildSettings.scenes.Where(scene => scene.enabled)
        .Select(scene => scene.path).ToArray());
```

Open each scene additively in EditMode and assert exactly one root named
`SceneRoot`; Gameplay also contains root children `World` and `UI`.

- [ ] **Step 2: Run scene tests to verify failure**

Expected: FAIL because no scenes are registered.

- [ ] **Step 3: Implement the Editor generator**

`Generate()` uses `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene)`,
creates the required roots, saves with `EditorSceneManager.SaveScene`, and
assigns the exact enabled `EditorBuildSettingsScene[]` order. It refuses to
overwrite a dirty open scene and logs every generated path.

Add menu item:

```csharp
[MenuItem("JoseonHunter/Setup/Generate Foundation Scenes")]
```

- [ ] **Step 4: Generate scenes through Unity MCP**

Invoke the menu command with the connected Editor. Do not edit scene YAML.

- [ ] **Step 5: Run all EditMode tests and inspect Unity Console**

Expected: all tests PASS and Console contains no error or exception entries.

- [ ] **Step 6: Commit**

```powershell
git add Assets/JoseonHunter/Scenes `
  Assets/JoseonHunter/Scripts/Editor/Scenes `
  Assets/JoseonHunter/Tests/EditMode/SceneScaffoldTests.cs `
  ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: scaffold Unity application scenes"
```

---

### Task 8: Verify, Document, and Push the Foundation Milestone

**Files:**
- Modify: `Docs/AI/UnityProjectContext.md`
- Create: `Docs/Assets/first-slice-asset-inventory.md`
- Create: `Docs/Verification/2026-07-26-unity-foundation.md`

**Interfaces:**
- Consumes all earlier tasks.
- Produces a clean, reproducible foundation checkpoint for the combat vertical-slice plan.

- [ ] **Step 1: Run repository checks**

```powershell
git status --short
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AssetMigration/Test-SyncFlutterAssets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1
```

Expected: clean intended status before docs, asset sync tests PASS, all EditMode
tests PASS. Ignore Unity-authored trailing whitespace already present in
serialized ProjectSettings when evaluating `git diff --check`; do not rewrite
Unity YAML solely to remove it.

- [ ] **Step 2: Verify through Unity MCP**

Read and record:

```text
Unity version
bridge/client connection status
Console errors and warnings
enabled build scene order
active build target
URP asset type
loaded package versions
EditMode test summary
```

Expected: Unity 6000.5.5f1, official bridge connected, zero compile errors,
Bootstrap/Lobby/Gameplay enabled in order, Android active, URP 2D assigned, and
all EditMode tests passing.

- [ ] **Step 3: Update persistent documentation**

Update `UnityProjectContext.md` with the now-confirmed pipeline, input, Android,
assemblies, scenes, tests, MCP capabilities, and current commit.

`first-slice-asset-inventory.md` lists every migrated asset with source path,
destination, profile, SHA-256, license source, and placeholder status.

`2026-07-26-unity-foundation.md` records commands, exit codes, test-result
paths, Console summary, unresolved warnings, and Android module state.

- [ ] **Step 4: Commit documentation**

```powershell
git add Docs
git commit -m "docs: verify Unity foundation milestone"
```

- [ ] **Step 5: Push**

```powershell
git push origin master
```

Expected: `origin/master` advances and the working tree is clean.

---

## Plan Completion Gate

Do not start combat gameplay implementation until all of these are true:

- Android Build Support, SDK, NDK, and OpenJDK are installed.
- Official Unity MCP is connected and approved; no second provider is present.
- URP 2D and Input System-only settings pass EditMode validation.
- Six first-party assemblies and two test assemblies compile.
- The manifest synchronizer passes isolated tests and copies only approved assets.
- Imported first-slice assets pass texture, audio, font, and license validation.
- Bootstrap, Lobby, and Gameplay scenes exist and are enabled in the correct order.
- The Unity Console has no compile errors.
- All EditMode tests pass in batch mode.
- The milestone is committed and pushed to `jinygod/JoseonHunter`.

The next plan begins the playable vertical slice: Domain combat rules, Input
Actions and touch joystick, player/enemy runtime, Hwando attack, experience and
level-up choices, wave clock, Fallen General boss, HUD, results, and new Unity
save schema v1.
