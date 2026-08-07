# Background Music Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add six legally verified CC0 music roles and a persistent, crossfading music system for the lobby, three 15-minute combat phases, mid-bosses, and the final boss.

**Architecture:** Keep long-form music separate from the existing pooled SFX director. A persistent `GameMusicDirector` owns exactly two streaming `AudioSource` components and resolves clips through a serialized catalog. Gameplay publishes phase and encounter events; a small state object resolves overrides so crossing the 5- or 10-minute boundary during a mid-boss does not interrupt boss music.

**Tech Stack:** Unity 6000.5.5f1, C#, Unity AudioSource/AudioImporter, NUnit EditMode and PlayMode tests, FFmpeg for source normalization, PowerShell build scripts.

## Global Constraints

- Use only CC0 or public-domain music whose source page explicitly permits commercial use and modification without attribution.
- Import only six music clips for the current lobby and playable `귀곡 들판` stage.
- Do not add music for `도깨비 고개`, `월식 왕릉`, or difficulty-specific variants.
- Use OGG Vorbis, stereo, streaming load, background loading, and disabled preload for every music clip.
- Use exactly two persistent music sources and a two-second unscaled-time crossfade.
- Music load failures must never block scene loading or gameplay.
- Preserve unrelated local `.meta` changes and stage only files named by each task.
- Run Unity work sequentially with BelowNormal priority and restricted processor affinity to avoid saturating the workstation.

---

### Task 1: Curate and import the six CC0 tracks

**Files:**
- Create: `Assets/JoseonHunter/Audio/Music/CC0/lobby_yoiyami.ogg`
- Create: `Assets/JoseonHunter/Audio/Music/CC0/gwigok_early_asianoriental.ogg`
- Create: `Assets/JoseonHunter/Audio/Music/CC0/gwigok_mid_frozen_desert.ogg`
- Create: `Assets/JoseonHunter/Audio/Music/CC0/gwigok_late_hope.ogg`
- Create: `Assets/JoseonHunter/Audio/Music/CC0/midboss_determined_pursuit.ogg`
- Create: `Assets/JoseonHunter/Audio/Music/CC0/finalboss_epic_battle.ogg`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Modify: `Docs/ThirdPartyAudio/free-audio-source-manifest.md`
- Modify: `Docs/Assets/audio-rights-ledger.csv`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameMusicAssetContractTests.cs`

**Interfaces:**
- Consumes: OpenGameArt source pages and downloadable original audio files.
- Produces: six Unity `AudioClip` assets with stable paths and verified mobile import settings.

- [ ] **Step 1: Download and validate the source candidates**

Use these source pages and reject any downloaded file whose page no longer says `License(s): CC0`:

```text
Lobby:      https://opengameart.org/content/yoiyami-core-theme-%E2%80%93-deep-blue-ambient-piano
Early:      https://opengameart.org/content/asianoriental1
Mid:        https://opengameart.org/content/frozen-desert-112
Late:       https://opengameart.org/content/hopeorchestral-battle-music
Mid-boss:   https://opengameart.org/content/determined-pursuit-epic-orchestra-loop
Final boss: https://opengameart.org/content/boss-battle-music
```

Store originals under the git-ignored `ExternalAssets/Audio/Music/OpenGameArt/`. Record SHA-256, duration, channels, sample rate, and peak level. Reject vocals, speech, corrupted files, files shorter than 30 seconds, and files with more than one second of leading silence.

- [ ] **Step 2: Normalize game copies to OGG**

For WAV sources, preserve stereo and sample rate while encoding with Vorbis quality 5:

```powershell
ffmpeg -y -i source.wav -map_metadata -1 -c:a libvorbis -q:a 5 destination.ogg
```

For existing OGG sources, use the original OGG if it has no corrupt packets. Do not time-stretch or pitch-shift music merely to match a nominal BPM. Put only the six approved game copies under `Assets/JoseonHunter/Audio/Music/CC0/`.

- [ ] **Step 3: Write the failing import contract test**

Create tests that load all six exact paths and assert:

```csharp
Assert.That(clip, Is.Not.Null);
Assert.That(clip.length, Is.GreaterThanOrEqualTo(30f));
Assert.That(importer.forceToMono, Is.False);
Assert.That(importer.loadInBackground, Is.True);
Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.False);
Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis));
```

Also assert that `Assets/JoseonHunter/Audio/Music/CC0` contains exactly six `AudioClip` assets.

- [ ] **Step 4: Run the focused test to verify RED**

Run:

```powershell
Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameMusicAssetContractTests
```

Expected: FAIL because `loadInBackground` and the explicit music compression quality are not yet configured.

- [ ] **Step 5: Implement the music importer profile**

Update `OnPreprocessAudio` so `Assets/JoseonHunter/Audio/Music/` receives:

```csharp
audio.forceToMono = false;
audio.loadInBackground = true;
settings.loadType = AudioClipLoadType.Streaming;
settings.preloadAudioData = false;
settings.compressionFormat = AudioCompressionFormat.Vorbis;
settings.quality = .55f;
```

Keep the existing short-SFX profile unchanged.

- [ ] **Step 6: Update rights records**

Add one approved row per imported clip to `audio-rights-ledger.csv`, including local path, original filename, creator, OpenGameArt page, CC0-1.0, and SHA-256 in the accompanying manifest. State that the file was normalized to Vorbis quality 5 when applicable.

- [ ] **Step 7: Run the focused test to verify GREEN**

Run the Task 1 test command again. Expected: all six cases and the folder-count test pass.

- [ ] **Step 8: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Audio/Music/CC0 Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs Assets/JoseonHunter/Tests/EditMode/GameMusicAssetContractTests.cs Docs/ThirdPartyAudio/free-audio-source-manifest.md Docs/Assets/audio-rights-ledger.csv
git commit -m "feat: import cc0 background music"
git push origin master
```

### Task 2: Add music roles, policy, and serialized catalog

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Audio/GameMusicRole.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Audio/GameMusicPolicy.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameMusicCatalogAsset.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/Audio/GameMusicCatalogBuilder.cs`
- Create via builder: `Assets/JoseonHunter/Resources/Audio/GameMusicCatalog.asset`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameMusicPolicyTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameMusicCatalogTests.cs`

**Interfaces:**
- Produces: `GameMusicRole`, `CombatMusicPhase`, `GameMusicPolicy.PhaseAt(float)`, `GameMusicPolicy.RoleFor(CombatMusicPhase)`, and `GameMusicCatalogAsset.TryGet(GameMusicRole, out AudioClip, out float)`.
- Consumes: the six audio paths created by Task 1.

- [ ] **Step 1: Write failing policy tests**

Cover exact boundaries:

```csharp
[TestCase(0f, CombatMusicPhase.Early)]
[TestCase(299.99f, CombatMusicPhase.Early)]
[TestCase(300f, CombatMusicPhase.Mid)]
[TestCase(599.99f, CombatMusicPhase.Mid)]
[TestCase(600f, CombatMusicPhase.Late)]
public void PhaseAtUsesFiveMinuteBoundaries(float elapsed, CombatMusicPhase expected)
```

Assert mappings `Early -> CombatEarly`, `Mid -> CombatMid`, and `Late -> CombatLate`.

- [ ] **Step 2: Run policy tests to verify RED**

Expected: compilation failure because the role and policy types do not exist.

- [ ] **Step 3: Implement the role and policy types**

Define:

```csharp
public enum GameMusicRole { None, Lobby, CombatEarly, CombatMid, CombatLate, MidBoss, FinalBoss }
public enum CombatMusicPhase { Early, Mid, Late }
public static CombatMusicPhase PhaseAt(float elapsedSeconds)
public static GameMusicRole RoleFor(CombatMusicPhase phase)
```

Clamp negative elapsed time to the early phase.

- [ ] **Step 4: Write failing catalog tests**

Assert that the default resource catalog exists, contains exactly six unique non-null clips, uses a volume in `[0.2, 0.8]`, and resolves every role except `None`.

- [ ] **Step 5: Implement catalog asset and editor builder**

Use a serializable entry:

```csharp
[Serializable]
public struct GameMusicEntry
{
    public GameMusicRole Role;
    public AudioClip Clip;
    [Range(0f, 1f)] public float Volume;
}
```

`GameMusicCatalogBuilder.Rebuild()` must create or update the resource asset using exact Task 1 paths and volumes: lobby `.34`, early `.38`, mid `.40`, late `.42`, mid-boss `.44`, final boss `.46`.

- [ ] **Step 6: Rebuild the catalog and run focused tests**

Run Unity in batch mode with `-executeMethod JoseonHunter.Editor.Audio.GameMusicCatalogBuilder.Rebuild`, then run both new EditMode fixtures. Expected: all pass.

- [ ] **Step 7: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Audio/GameMusicRole.cs Assets/JoseonHunter/Scripts/Runtime/Audio/GameMusicPolicy.cs Assets/JoseonHunter/Scripts/Presentation/Audio/GameMusicCatalogAsset.cs Assets/JoseonHunter/Scripts/Editor/Audio/GameMusicCatalogBuilder.cs Assets/JoseonHunter/Resources/Audio/GameMusicCatalog.asset Assets/JoseonHunter/Tests/EditMode/GameMusicPolicyTests.cs Assets/JoseonHunter/Tests/EditMode/GameMusicCatalogTests.cs
git commit -m "feat: add background music catalog"
git push origin master
```

### Task 3: Implement the persistent two-source music director

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameMusicDirector.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GameMusicDirectorPlayModeTests.cs`

**Interfaces:**
- Consumes: `GameMusicCatalogAsset.LoadDefault()` and `GameMusicRole`.
- Produces: `GameMusicDirector.EnsureExists()`, `Request(GameMusicRole, float fadeSeconds = 2f)`, `FadeOut(float fadeSeconds = 0.8f)`, `CurrentRole`, and test-only source state accessors.

- [ ] **Step 1: Write failing lifecycle and transition tests**

Test that:

- two `AudioSource` components are created exactly once;
- duplicate singleton objects destroy themselves;
- requesting the current role does not restart the active source;
- requesting a new role crossfades to the other source;
- `FadeOut` reaches `None` and stops both sources;
- a missing catalog entry returns false without throwing.

- [ ] **Step 2: Run the focused fixture to verify RED**

Expected: compilation failure because `GameMusicDirector` does not exist.

- [ ] **Step 3: Implement the director**

Use `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`, `DontDestroyOnLoad`, two non-spatial looping sources, and a single replaceable crossfade coroutine. Crossfade with `Time.unscaledDeltaTime`; stop and clear the outgoing clip after the fade. Ignore duplicate requests for `CurrentRole` when a clip is already active.

- [ ] **Step 4: Run the focused fixture to verify GREEN**

Expected: all lifecycle and crossfade tests pass.

- [ ] **Step 5: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/Audio/GameMusicDirector.cs Assets/JoseonHunter/Tests/PlayMode/GameMusicDirectorPlayModeTests.cs
git commit -m "feat: add persistent music crossfades"
git push origin master
```

### Task 4: Integrate lobby, combat phases, mid-boss overrides, and run endings

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameplayMusicState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameplayMusicStateTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/GameAudioIntegrationPlayModeTests.cs`

**Interfaces:**
- `GameplayMusicState.Reset()` sets early combat.
- `SetPhase(CombatMusicPhase)`, `EnterMidBoss()`, `ExitMidBoss()`, `EnterFinalBoss()`, and `EndRun()` update `CurrentRole`.
- `FirstPlayableController` publishes `CombatMusicPhaseChanged`, `MidBossAppeared`, and `MidBossDefeated`.

- [ ] **Step 1: Write failing override-state tests**

Verify that entering a mid-boss changes the role to `MidBoss`, changing phase during the encounter does not replace the override, exiting returns to the newest underlying phase, final boss always wins over mid-boss, and ending the run resolves to `None`.

- [ ] **Step 2: Run state tests to verify RED**

Expected: compilation failure because `GameplayMusicState` does not exist.

- [ ] **Step 3: Implement the state object**

Store `CombatMusicPhase phase`, `int activeMidBosses`, `bool finalBoss`, and `bool ended`. Compute `CurrentRole` in priority order: ended, final boss, active mid-boss, combat phase.

- [ ] **Step 4: Write failing integration tests**

Load Lobby and assert `GameMusicDirector.CurrentRole == Lobby`. Load Gameplay, reset the run, cross 300 and 600 seconds, spawn and defeat each mid-boss, spawn the final boss, and end the run. Assert the role sequence:

```text
CombatEarly -> CombatMid -> MidBoss -> CombatMid -> CombatLate -> FinalBoss -> None
```

- [ ] **Step 5: Publish gameplay music events**

In `FirstPlayableController`:

- invoke `CombatMusicPhaseChanged` only when elapsed time crosses 300 or 600 seconds;
- invoke `MidBossAppeared` after a mid-boss is registered;
- invoke `MidBossDefeated` only when the defeated state was a mid-boss;
- keep the existing final `BossAppeared` and `BossDefeated` events;
- do not emit music events for elites or ordinary monsters.

- [ ] **Step 6: Bind scene presentation**

`LobbyBootstrap.Awake()` requests `Lobby`. `FirstPlayableUiBootstrap` owns one `GameplayMusicState`, subscribes and unsubscribes the new events, requests the resolved role after every state transition, and fades out on player defeat, final boss defeat, or abandoned run. `RunReset` requests early combat.

- [ ] **Step 7: Run focused state and integration fixtures**

Expected: all new and existing music/audio integration tests pass.

- [ ] **Step 8: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/Audio/GameplayMusicState.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs Assets/JoseonHunter/Tests/EditMode/GameplayMusicStateTests.cs Assets/JoseonHunter/Tests/PlayMode/GameAudioIntegrationPlayModeTests.cs
git commit -m "feat: integrate adaptive stage music"
git push origin master
```

### Task 5: Validate the complete music feature

**Files:**
- Create: `Docs/Verification/2026-08-07-background-music.md`
- Modify: `Docs/AI/UnityProjectContext.md`

**Interfaces:**
- Consumes: completed Tasks 1-4.
- Produces: build and test evidence plus project-context documentation.

- [ ] **Step 1: Run scoped music tests**

Run every new EditMode and PlayMode music fixture. Record exact pass counts.

- [ ] **Step 2: Run the complete automated suite**

```powershell
Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode
```

Expected: zero failures and zero skipped tests.

- [ ] **Step 3: Build Android development APK**

```powershell
Tools/Unity/Build-AndroidDevelopment.ps1
```

Expected: `Build Finished, Result: Success` and a fresh `Builds/Android/JoseonHunter-development.apk`.

- [ ] **Step 4: Inspect final repository and runtime assets**

Confirm exactly one persistent music director, exactly six music clips, two music sources, clean scoped diffs, and no staged unrelated `.meta` files.

- [ ] **Step 5: Write verification and context docs**

Record source hashes, role mapping, exact test totals, APK timestamp/size, intentional exclusions, and the manual device-listening checklist. Update `UnityProjectContext.md` with the director, catalog, phase thresholds, and asset paths.

- [ ] **Step 6: Commit and push**

```powershell
git add -- Docs/Verification/2026-08-07-background-music.md Docs/AI/UnityProjectContext.md
git commit -m "docs: verify background music integration"
git push origin master
```
