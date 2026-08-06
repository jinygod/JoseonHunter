# Conservative Game Audio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add restrained, clearly readable UI, pickup, weapon, hit, and boss sound effects without creating an audio performance problem during dense combat.

**Architecture:** A Unity-independent playback budget decides whether a cue may play, while one persistent `GameAudioDirector` owns a fixed pool of 12 2D `AudioSource` components and an explicit Resources catalog. Existing presentation components consume already-confirmed UI, pickup, and damage events; gameplay rules and save data remain audio-agnostic.

**Tech Stack:** Unity 6000.5.5f1, C# 9, uGUI/EventSystem, Unity AudioSource, Unity Test Framework, CC0 OGG/WAV assets.

## Global Constraints

- Work directly on `master`; commit and push every coherent task.
- Preserve all unrelated modified `.meta` files and never stage them.
- Do not add background music, character voices, mouse-hover sounds, or repeating ordinary-monster voices.
- Use a fixed pool of 12 `AudioSource` objects and never instantiate transient audio objects during play.
- Use `Time.unscaledTime`; UI remains audible while paused and combat sounds are rejected while gameplay is paused.
- Pitch variation stays within `0.96` to `1.04`.
- Missing clips fail silently after one warning and cannot interrupt play.
- Run Unity processes sequentially to avoid saturating CPU and memory.

---

### Task 1: Deterministic Playback Budget

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Audio/GameAudioCueId.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Audio/GameAudioPlaybackBudget.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameAudioPlaybackBudgetTests.cs`

**Interfaces:**
- Produces: `GameAudioCueId`, `GameAudioPriority`, and `GameAudioPlaybackBudget.TryReserve(GameAudioCueId cue, float now, int activeSources)`.
- Produces: `GameAudioPlaybackBudget.TryReserveWeapon(WeaponId weaponId, int attackInstanceId, float now, int activeSources)` for one representative cue per attack.

- [ ] **Step 1: Write the failing budget tests**

```csharp
[Test]
public void Experience_cues_inside_nine_hundredths_are_rejected()
{
    var budget = new GameAudioPlaybackBudget(12);
    Assert.That(budget.TryReserve(GameAudioCueId.ExperiencePickup, 10f, 0), Is.True);
    Assert.That(budget.TryReserve(GameAudioCueId.ExperiencePickup, 10.08f, 0), Is.False);
    Assert.That(budget.TryReserve(GameAudioCueId.ExperiencePickup, 10.09f, 0), Is.True);
}

[Test]
public void The_same_weapon_attack_only_reserves_once()
{
    var budget = new GameAudioPlaybackBudget(12);
    Assert.That(budget.TryReserveWeapon(WeaponId.GakgungShot, 71, 4f, 0), Is.True);
    Assert.That(budget.TryReserveWeapon(WeaponId.GakgungShot, 71, 4.2f, 0), Is.False);
    Assert.That(budget.TryReserveWeapon(WeaponId.GakgungShot, 72, 4.2f, 0), Is.True);
}

[Test]
public void A_full_pool_rejects_low_priority_but_allows_boss_warning_to_replace_one()
{
    var budget = new GameAudioPlaybackBudget(12);
    Assert.That(budget.TryReserve(GameAudioCueId.NormalHit, 1f, 12), Is.False);
    Assert.That(budget.TryReserve(GameAudioCueId.BossWarning, 1f, 12), Is.True);
}
```

- [ ] **Step 2: Run the test and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameAudioPlaybackBudgetTests
```

Expected: compile failure because `GameAudioPlaybackBudget` and cue types do not exist.

- [ ] **Step 3: Implement the minimal deterministic rules**

```csharp
public bool TryReserve(GameAudioCueId cue, float now, int activeSources)
{
    var priority = PriorityFor(cue);
    if (activeSources >= sourceCapacity && priority < GameAudioPriority.High) return false;
    var interval = MinimumIntervalFor(cue);
    if (lastPlayed.TryGetValue(cue, out var previous) && now - previous + .0001f < interval) return false;
    lastPlayed[cue] = now;
    return true;
}
```

Store at most 64 attack keys; clear the oldest generation when the bound is reached so the collection cannot grow indefinitely.

- [ ] **Step 4: Run the focused test and verify GREEN**

Use the command from Step 2. Expected: all budget tests pass.

- [ ] **Step 5: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Audio Assets/JoseonHunter/Tests/EditMode/GameAudioPlaybackBudgetTests.cs*
git commit -m "feat: add bounded game audio playback rules"
git push origin master
```

### Task 2: Persistent Audio Director and Explicit Catalog

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameAudioClipCatalog.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/Audio/GameAudioDirector.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GameAudioDirectorPlayModeTests.cs`

**Interfaces:**
- Consumes: Task 1 cue IDs and playback budget.
- Produces: `GameAudioDirector.TryPlay(GameAudioCueId cue)`, `TryPlayWeapon(WeaponId weaponId, int attackInstanceId)`, and `SetCombatEnabled(bool enabled)`.
- Produces: one persistent object named `Game Audio` containing exactly 12 pooled 2D sources.
- Test builds expose `RequestCountForTests(GameAudioCueId cue)` and `ResetRequestCountsForTests()` so integration tests can verify forwarding even before the CC0 assets are imported; these members are wrapped in `UNITY_INCLUDE_TESTS`.

- [ ] **Step 1: Write failing lifecycle and budget integration tests**

```csharp
[UnityTest]
public IEnumerator Ensure_created_twice_keeps_one_director_and_twelve_sources()
{
    GameAudioDirector.EnsureExists();
    GameAudioDirector.EnsureExists();
    yield return null;
    Assert.That(Object.FindObjectsByType<GameAudioDirector>(FindObjectsInactive.Include), Has.Length.EqualTo(1));
    Assert.That(GameAudioDirector.Instance.SourceCount, Is.EqualTo(12));
}

[UnityTest]
public IEnumerator Missing_optional_clip_returns_false_without_throwing()
{
    GameAudioDirector.EnsureExists();
    yield return null;
    Assert.DoesNotThrow(() => GameAudioDirector.Instance.TryPlay(GameAudioCueId.None));
    Assert.That(GameAudioDirector.Instance.TryPlay(GameAudioCueId.None), Is.False);
}
```

- [ ] **Step 2: Run the test and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.GameAudioDirectorPlayModeTests
```

Expected: compile failure because the director does not exist.

- [ ] **Step 3: Implement the director**

Use `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`, `DontDestroyOnLoad`, duplicate destruction in `Awake`, one-time explicit `Resources.Load<AudioClip>` calls, and a circular scan of the pre-created source pool. `TryPlay` returns whether a clip was accepted and started. High-priority cues may stop the quietest low-priority source; low-priority cues are dropped when the pool is full.

- [ ] **Step 4: Run the focused PlayMode test and verify GREEN**

Use the command from Step 2. Expected: all director tests pass with no Console errors.

- [ ] **Step 5: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/Audio Assets/JoseonHunter/Tests/PlayMode/GameAudioDirectorPlayModeTests.cs*
git commit -m "feat: add persistent pooled game audio director"
git push origin master
```

### Task 3: UI, Pickup, Combat, and Boss Event Wiring

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/GameAudioButtonFeedback.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RuntimeUiFactory.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyUiFactory.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GameAudioIntegrationPlayModeTests.cs`

**Interfaces:**
- Produces: `GameAudioButtonFeedback.Attach(Button button, GameAudioCueId cue = GameAudioCueId.UiClick)` using `IPointerClickHandler`, independent of presenters calling `RemoveAllListeners`.
- Produces controller events: `ExperienceCollected`, `YeopjeonCollected`, `MagnetCollected`, `PlayerLevelIncreased`, `BossWarningStarted`, `BossAppeared`, and `BossDefeated`.
- Consumes `ConfirmedDamageEvent` in `CombatFeedbackDirector` and requests one weapon cue plus rate-limited impact feedback.

- [ ] **Step 1: Write failing integration tests**

```csharp
[UnityTest]
public IEnumerator Runtime_button_receives_audio_feedback_that_survives_remove_all_listeners()
{
    var root = new GameObject("Audio UI Test").transform;
    var button = RuntimeUiFactory.Button("Test", root, Color.black);
    button.onClick.RemoveAllListeners();
    Assert.That(button.GetComponent<GameAudioButtonFeedback>(), Is.Not.Null);
    Object.Destroy(root.gameObject);
    yield return null;
}

[UnityTest]
public IEnumerator Pickup_and_damage_events_are_forwarded_to_the_audio_director()
{
    GameAudioDirector.EnsureExists();
    yield return null;
    var audio = GameAudioDirector.Instance;
    audio.ResetRequestCountsForTests();

    var uiObject = new GameObject("First Playable UI Test");
    uiObject.AddComponent<FirstPlayableUiBootstrap>();
    var controllerObject = new GameObject("Controller");
    var controller = controllerObject.AddComponent<FirstPlayableController>();
    yield return null;
    controller.SpawnExperiencePickupForTests(Vector2.zero, 1);
    controller.TickGameplayIfRunningForTests(.02f);
    yield return null;
    Assert.That(audio.RequestCountForTests(GameAudioCueId.ExperiencePickup), Is.EqualTo(1));

    var registry = new CombatTargetRegistry();
    var service = new CombatDamageService(registry);
    var target = new AudioTestTarget(901, 100);
    Assert.That(registry.Register(target), Is.True);
    var feedbackObject = new GameObject("Feedback");
    var feedback = feedbackObject.AddComponent<CombatFeedbackDirector>();
    feedback.Bind(service);
    Assert.That(service.TryApply(WeaponDamageRequest.Create(
        71, WeaponId.GakgungShot, target, 5, false, new Float2(0f, 0f),
        ContactPhase.Enter, 1), out _), Is.True);
    Assert.That(audio.RequestCountForTests(GameAudioCueId.Gakgung), Is.EqualTo(1));

    Object.Destroy(controllerObject);
    Object.Destroy(feedbackObject);
    Object.Destroy(uiObject);
    yield return null;
}
```

`AudioTestTarget` is a test-local real `ICombatTarget` implementation with mutable health, matching the complete interface used by existing combat-service tests. The controller test also creates `FirstPlayableUiBootstrap` before ticking so the real subscription forwards its pickup event.

- [ ] **Step 2: Run the integration tests and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.GameAudioIntegrationPlayModeTests
```

Expected: failures for missing button feedback and missing event forwarding.

- [ ] **Step 3: Implement event wiring**

Invoke pickup events only after collection is confirmed. Emit `PlayerLevelIncreased` once per gained level. Emit boss warning/appearance only when their stage milestone transitions, and emit defeat after the actual boss death. Subscribe and unsubscribe these events in `FirstPlayableUiBootstrap`. In `CombatFeedbackDirector`, only request combat audio while `flow.IsGameplayRunning`.

- [ ] **Step 4: Run focused EditMode and PlayMode tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameAudioPlaybackBudgetTests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter "JoseonHunter.Tests.PlayMode.GameAudioDirectorPlayModeTests|JoseonHunter.Tests.PlayMode.GameAudioIntegrationPlayModeTests"
```

Expected: all audio tests pass with balanced subscriptions after object destruction and scene reload.

- [ ] **Step 5: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests/PlayMode/GameAudioIntegrationPlayModeTests.cs*
git commit -m "feat: connect core gameplay and UI audio cues"
git push origin master
```

### Task 4: Select and Import the CC0 Clips

**Files:**
- Create: `Assets/JoseonHunter/Resources/Audio/CC0/UI/*`
- Create: `Assets/JoseonHunter/Resources/Audio/CC0/Pickups/*`
- Create: `Assets/JoseonHunter/Resources/Audio/CC0/Weapons/*`
- Create: `Assets/JoseonHunter/Resources/Audio/CC0/Combat/*`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/GameAudioAssetContractTests.cs`
- Modify: `Docs/ThirdPartyAudio/free-audio-source-manifest.md` only if the final selected filenames differ from the design record.

**Interfaces:**
- Consumes the exact Resources paths from `GameAudioClipCatalog`.
- Produces a mono, Decompress-On-Load, non-streaming import profile for the selected short clips.

- [ ] **Step 1: Write the failing asset contract test**

```csharp
[TestCase("Assets/JoseonHunter/Resources/Audio/CC0/UI/ui_click.ogg")]
[TestCase("Assets/JoseonHunter/Resources/Audio/CC0/Pickups/experience.ogg")]
[TestCase("Assets/JoseonHunter/Resources/Audio/CC0/Weapons/gakgung.wav")]
public void Required_audio_clip_exists_and_uses_mobile_short_sfx_profile(string path)
{
    Assert.That(AssetDatabase.LoadAssetAtPath<AudioClip>(path), Is.Not.Null);
    var importer = AssetImporter.GetAtPath(path) as AudioImporter;
    Assert.That(importer.forceToMono, Is.True);
    Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
}
```

- [ ] **Step 2: Run the asset test and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameAudioAssetContractTests
```

Expected: required assets are missing.

- [ ] **Step 3: Copy only selected files with `Copy-Item` and import them**

Select exactly 18 clips from the downloaded CC0 packs. Rename destinations by purpose, keep only one encoding of each source, and extend `JoseonAssetPostprocessor` so only `Assets/JoseonHunter/Resources/Audio/CC0/` receives the short-SFX profile.

- [ ] **Step 4: Run the asset contract and audio integration tests**

Use the focused commands from Tasks 3 and 4. Expected: all catalog paths load and audio requests play without missing-clip warnings.

- [ ] **Step 5: Commit and push**

```powershell
git add -- Assets/JoseonHunter/Resources/Audio Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs Assets/JoseonHunter/Tests/EditMode/GameAudioAssetContractTests.cs* Docs/ThirdPartyAudio/free-audio-source-manifest.md
git commit -m "assets: add selected CC0 game sound effects"
git push origin master
```

### Task 5: Full Regression, Android Build, and Final Review

**Files:**
- Modify: `Docs/Verification/2026-08-06-conservative-game-audio.md`

- [ ] **Step 1: Run all EditMode tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

- [ ] **Step 2: Run all PlayMode tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode
```

- [ ] **Step 3: Build Android development player**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1
```

- [ ] **Step 4: Review final diff and write exact evidence**

Record test counts, build output, clip count, total imported audio size, pool size, and any manual listening limitation. Run `git diff --check` and confirm only intended files plus Unity-generated `.meta` files are staged.

- [ ] **Step 5: Commit and push verification**

```powershell
git add -- Docs/Verification/2026-08-06-conservative-game-audio.md
git commit -m "docs: verify conservative game audio integration"
git push origin master
```
