# Flutter to Unity Migration Design

> **Superseded for product direction and the first playable release.**
> The validated Unity foundation remains applicable, but portrait orientation,
> the three-minute run, original modular pixel art, menus, progression, and
> release scope are now defined by
> `2026-07-26-portrait-mobile-vertical-slice-design.md`.

## Summary

Migrate the existing Flutter + Flame game, Joseon Dynasty Survival, into the
Unity project at `D:\UnityProjects\JoseonHunter`.

The Flutter project remains the behavioral reference until Unity reaches
feature parity. Migration proceeds through a playable vertical slice rather
than by porting every subsystem independently. The first goal is to reproduce
the established rules and five-minute game loop, while Unity-native
presentation, input, profiling, and asset workflows raise the mobile-game
quality.

The Unity version starts with a new save. It does not read the Flutter
application's local SharedPreferences data.

## Product Direction

- Preserve the Flutter version's game rules, balance, content IDs, and main
  navigation flow before making design changes.
- Improve touch response, combat readability, animation, VFX, audio mixing,
  haptics, frame pacing, and Android performance through Unity-native systems.
- Keep the Flutter project available as an executable and testable reference
  until the corresponding Unity milestones pass.
- Target landscape Android first. Other platforms remain follow-up work.

## Repository And Tooling

`D:\UnityProjects\JoseonHunter` becomes an independent Git repository. Unity
generated directories, IDE files, and build outputs are ignored. Source assets,
their `.meta` files, project settings, packages, code, tests, and documentation
are tracked.

Use Unity 6000.5.5f1. The installed `com.unity.ai.assistant` package already
contains the official Unity MCP bridge, and the Windows relay is present at
`C:\Users\전성진\.unity\relay\relay_win.exe`. Do not add a second community MCP
provider.

Configure Codex to launch the relay with:

```text
--mcp --project-path D:\UnityProjects\JoseonHunter
```

The user approves the first pending Codex connection in:

```text
Edit > Project Settings > AI > Unity MCP Server
```

Keep batch-mode auto-approval disabled. Validate the connection first by
reading the Unity console and scene list.

## Unity Foundation

### Rendering

Convert the empty project from the Built-in Render Pipeline to URP with a 2D
Renderer. The switch happens before gameplay assets or materials depend on the
old pipeline.

Pixel-art actors, enemies, projectiles, and most combat VFX use Point filtering,
disabled mipmaps, and lossless or low-loss compression. Large UI art and
backgrounds use separate mobile compression presets. Sorting layers and pixel
scale rules are established before prefabs are assembled.

### Input

Use Unity's Input System package rather than the legacy Input Manager. One
gameplay action map covers:

- touch virtual joystick movement;
- keyboard movement for editor testing;
- gamepad movement;
- pause and UI navigation.

Touch controls must respect Safe Area and remain usable on supported landscape
aspect ratios.

### UI

Use uGUI and TextMeshPro for runtime UI. The UI covers the landscape lobby,
selection screens, combat HUD, choice modal, pause/settings, results, and
diagnostics. SongMyung and GowunBatang are converted to TextMeshPro SDF assets
with their license files retained.

### Scenes

Start with three scenes:

1. `Bootstrap` initializes services, validated content, settings, and save data.
2. `Lobby` owns navigation, character/stage selection, and meta presentation.
3. `Gameplay` owns the combat world and its HUD. Pause, level-up choices, and
   results are UI states rather than separate scenes.

## Code Architecture

Use focused assemblies with one-way dependencies:

```text
Domain
  <- Content
  <- Runtime
  <- Presentation

Domain
  <- Infrastructure

Tests reference the smallest required production assembly.
```

### Domain

Pure C# types and deterministic rules with no UnityEngine dependency where
practical. This includes combat stats, damage calculations, weapons, waves,
level progression, rewards, unlock evaluation, and save models.

### Content

ScriptableObject authoring assets for characters, weapons, augments, enemies,
bosses, stages, waves, and presentation references. At startup they are
validated and converted to immutable runtime definitions.

### Runtime

Unity-facing gameplay orchestration: game clock, spawning, targeting, movement,
collision queries, projectiles, pickups, object pools, pause state, and run
lifecycle.

### Presentation

Sprite renderers, animation, cameras, particles, combat telegraphs, audio,
haptics, damage numbers, HUD, lobby, choices, and results. Presentation reacts
to domain/runtime events and does not own balance rules.

### Infrastructure

Unity save persistence first. Supabase identity/cloud progress, telemetry, and
Google Play purchase integrations are added after local feature parity.

## Data Flow

The main flow is:

```text
ScriptableObject authoring data
  -> content validation
  -> immutable runtime definitions
  -> pure domain/runtime systems
  -> typed gameplay events
  -> UI, sprites, audio, VFX, and haptics
```

The new Unity save begins at schema version 1. Writes use a temporary file and
atomic replacement where the target platform permits it. Invalid or damaged
saves fall back to defaults and emit a diagnostic; the game must not silently
retain partially corrupt progress.

## Asset Migration

Organize first-party assets under:

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
    Characters/
    Weapons/
    Enemies/
    Stages/
    Waves/
  Prefabs/
    Actors/
    Combat/
    UI/
  Scenes/
  Scripts/
    Domain/
    Content/
    Runtime/
    Presentation/
    Infrastructure/
  Tests/
```

Before copying, generate a migration inventory from the Flutter runtime assets
and asset catalogs. Every item is classified as:

- approved runtime asset;
- temporary or placeholder asset;
- duplicate;
- unused;
- source-only material;
- missing or unresolved license/provenance.

Copy source-owned runtime assets rather than Flutter build output. Preserve the
asset and audio rights ledgers under `Docs/Assets/`. Maintain a mapping from the
Flutter source path and content ID to the Unity destination. Unity `.meta`
files become the stable identity after import.

Folder-aware import automation applies:

- Point filtering and no mipmaps for pixel gameplay sprites;
- appropriate sprite slicing and pivot conventions;
- Android ASTC settings for large backgrounds and UI where visual inspection
  approves the result;
- Streaming for music;
- memory-resident or ADPCM settings for frequent short effects;
- validation for oversized, duplicate, missing, or incorrectly configured
  assets.

## First Vertical Slice

The first playable slice includes:

- character: `rookie_constable`;
- starting weapon: `hwando_slash`;
- stage: `moonlit_abandoned_office`;
- five-minute target with boss arrival at 270 seconds;
- plague rat swarm, bandit, vengeful spirit, dokkaebi, and fallen general;
- movement, automatic attack, damage, death, experience pickups, leveling,
  upgrade choices, boss victory, defeat, and results;
- Bootstrap, lobby, deployment, gameplay, pause/settings, results, return to
  lobby, and restart;
- new local save for selected character, best record, earned currency, and
  settings;
- music, essential combat/UI audio, screen impulse, damage numbers, telegraphs,
  haptics, and object pools.

The first slice deliberately excludes the remaining roster, production
Supabase integration, cloud save, and real-money purchases.

## Mobile Quality Targets

- Landscape-first layout with Safe Area support.
- Touch virtual joystick with fast press-to-move response and no stuck input.
- Keyboard and gamepad parity for development and accessibility.
- A 60 FPS target on representative Android hardware.
- Object pooling for enemies, projectiles, pickups, damage numbers, and common
  VFX.
- Explicit caps and graceful degradation for transient presentation effects.
- Attack warnings that remain legible under heavy combat load.
- Channel-based audio mixing and priority rules for simultaneous effects.
- Optional vibration, screen shake, damage numbers, and UI scale.
- Pause and resume behavior that survives focus loss and Android lifecycle
  transitions.

## Error Handling

- Duplicate IDs, invalid stats, broken references, and missing required assets
  fail content validation and tests.
- Missing presentation assets show a clear fallback and log the content ID and
  expected path.
- Pool exhaustion may expand within a budget or skip nonessential presentation,
  but must not change combat outcomes.
- Service failures remain isolated from the offline gameplay loop.
- Async scene or service initialization failures return to a recoverable state
  with a user-readable message and diagnostic log.

## Testing And Parity

### EditMode

Export or hand-author reviewed JSON fixtures from the Flutter definitions for
characters, weapons, waves, progression, and results. Test pure C# systems
against those fixtures. Do not attempt to execute Dart inside Unity.

### PlayMode

Cover scene boot, dependency composition, input, spawning, pause, level-up
choice, victory/defeat, results, save/reload, and broken prefab/reference
detection.

### Android

Validate touch, Safe Area, focus loss, background/foreground restore, audio
interruption, vibration settings, frame time, memory, temperature, and IL2CPP
build behavior on real devices.

## Full Migration Stages

1. Repository, official MCP, URP 2D, Input System, TextMeshPro, assemblies,
   tests, and Android landscape settings.
2. Asset inventory, rights validation, copying, import automation, and asset
   validation.
3. First vertical slice through a five-minute boss run and local persistence.
4. All three characters, the complete weapon and augment roster, normal and
   elite enemies, bosses, both stages, unlocks, records, and compendium.
5. Tutorial, telemetry, Supabase account/cloud progress, and Google Play
   purchase integration.
6. Android performance, lifecycle, build, signing, and release-readiness
   validation.

Each stage must leave a runnable, tested project. The Flutter version is not
retired until the corresponding Unity behaviors and content have passed parity
checks.

## Completion Criteria

The migration is complete when:

- the complete intended Flutter content and main flow exist in Unity;
- approved runtime assets and licensing records are organized and validated;
- core deterministic behavior has parity coverage;
- the game is controllable and readable on landscape Android devices;
- local save, settings, records, unlocks, and required online services work;
- Android IL2CPP builds pass automated and device validation;
- the Unity project, not Flutter, is the maintained release source.
