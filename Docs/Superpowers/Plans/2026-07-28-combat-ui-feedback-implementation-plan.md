# Combat UI and Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the first playable's IMGUI combat and upgrade interface with a portrait-mobile uGUI presentation layer, then add restrained high-impact reward and contact feedback.

**Architecture:** Runtime gameplay publishes immutable UI snapshots and progression events without referencing presentation code. A presentation-owned bootstrap creates a screen-space canvas, binds focused HUD, weapon rack, upgrade choice, reveal, and feedback presenters, and forwards a single guarded choice command back to the controller. The existing confirmed-damage event remains the source of truth for damage numbers and hit feedback.

**Tech Stack:** Unity 6000.5.5f1, C# 9, Unity uGUI 2.5.0, TextMeshPro, Unity Input System, NUnit EditMode/PlayMode tests

## Global Constraints

- Target layout is portrait mobile at a 1080 × 1920 reference resolution.
- Approved art direction is **귀살 아케이드**: dark ink battlefield, hanji-like choice cards, gold reward light, and weapon-family accent colors.
- Level-up flow is 0.3 seconds of unscaled deceleration followed by a complete combat pause.
- Damage numbers and hit feedback originate only from `ConfirmedDamageEvent` contact points.
- Normal combat stays at feedback intensity 70; critical/kill 80; acquisition/key upgrade 90; evolution/boss kill 100.
- Existing weapon icons under `Assets/JoseonHunter/Art/Weapons/Runtime/*/icon.png` are reused before generating new art.
- Presentation code must not mutate damage, enemy health, or weapon execution state.
- Reduced-effects mode preserves essential state, selection, damage values, and confirmed-contact feedback.
- Experience that crosses more than one threshold queues additional upgrade choices and opens them one at a time.
- Preserve unrelated dirty `.meta`, scene, character, VFX, and `ProjectSettings` files.

---

## File Structure

### Runtime contracts

- Create `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`: immutable HUD and upgrade snapshots plus progression event payloads.
- Modify `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`: publish snapshots/events, expose guarded selection, remove gameplay IMGUI HUD/upgrade rendering.

### Presentation components

- Create `Assets/JoseonHunter/Scripts/Presentation/UI/JoseonUiPalette.cs`: shared colors, spacing, and weapon accent mapping.
- Create `Assets/JoseonHunter/Scripts/Presentation/UI/RuntimeUiFactory.cs`: focused helpers for uGUI/TMP object creation.
- Create `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`: canvas lifetime and controller binding.
- Create `Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs`: top HUD and boss warning/health.
- Create `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`: owned weapon icons, levels, recently changed highlight.
- Create `Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs`: slowdown, pause, three cards, guarded choice, resume.
- Create `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`: acquisition, key upgrade, evolution overlay.
- Create `Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs`: intensity budget, hit stop, camera impulse, reduced-effects behavior.
- Modify `Assets/JoseonHunter/Scripts/Presentation/Combat/FirstPlayableDamageNumberBootstrap.cs`: bind feedback director with the existing damage-number pool.
- Modify `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs`: normal, critical, and boss motion profiles.

### Tests

- Create `Assets/JoseonHunter/Tests/EditMode/FirstPlayableUiStateTests.cs`
- Create `Assets/JoseonHunter/Tests/EditMode/FeedbackBudgetTests.cs`
- Create `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`
- Create `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`
- Create `Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs`

---

### Task 1: Publish a presentation-safe UI state contract

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/FirstPlayableUiStateTests.cs`

**Interfaces:**
- Produces: `FirstPlayableUiState`, `UpgradeChoiceState`, `UpgradeChoiceView`, `ProgressionRewardEvent`
- Produces: `FirstPlayableController.UiState`, `UpgradeOpened`, `UpgradeChosen`, `RunReset`
- Produces: `bool TryChooseUpgrade(int index)`
- Consumes: existing `UpgradeOffer`, `WeaponId`, `WeaponCatalogAsset`

- [ ] **Step 1: Write the state contract test**

```csharp
[Test]
public void Upgrade_choice_state_copies_source_items()
{
    var source = new[]
    {
        new UpgradeChoiceView("gakgung_shot", UpgradeKind.Weapon, 1, "신규 무기", "각궁", "직선 관통 사격", "신규", null),
        new UpgradeChoiceView("boots", UpgradeKind.Support, 2, "능력 강화", "경쾌한 버선", "이동 속도 증가", "+12%", null)
    };

    var state = new UpgradeChoiceState(3, source);
    source[0] = default;

    Assert.That(state.Level, Is.EqualTo(3));
    Assert.That(state.Choices[0].Id, Is.EqualTo("gakgung_shot"));
}
```

- [ ] **Step 2: Run the focused EditMode test and confirm RED**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.FirstPlayableUiStateTests' -testResults 'Temp\ui-state-red.xml' -logFile 'Temp\ui-state-red.log' -quit
```

Expected: compilation fails because `UpgradeChoiceState` and `UpgradeChoiceView` do not exist.

- [ ] **Step 3: Add immutable payloads**

```csharp
public readonly struct FirstPlayableUiState
{
    public FirstPlayableUiState(
        int level, int experience, int experienceToNext, int coins, int kills,
        float elapsed, float duration, float health, float maximumHealth,
        bool bossWarning, bool bossAlive, float bossHealth, float bossMaximumHealth,
        IReadOnlyList<WeaponSlotView> weapons)
    {
        Level = level;
        Experience = experience;
        ExperienceToNext = experienceToNext;
        Coins = coins;
        Kills = kills;
        Elapsed = elapsed;
        Duration = duration;
        Health = health;
        MaximumHealth = maximumHealth;
        BossWarning = bossWarning;
        BossAlive = bossAlive;
        BossHealth = bossHealth;
        BossMaximumHealth = bossMaximumHealth;
        Weapons = Array.AsReadOnly(weapons.ToArray());
    }

    public int Level { get; }
    public int Experience { get; }
    public int ExperienceToNext { get; }
    public int Coins { get; }
    public int Kills { get; }
    public float Elapsed { get; }
    public float Duration { get; }
    public float Health { get; }
    public float MaximumHealth { get; }
    public bool BossWarning { get; }
    public bool BossAlive { get; }
    public float BossHealth { get; }
    public float BossMaximumHealth { get; }
    public IReadOnlyList<WeaponSlotView> Weapons { get; }
}

public readonly struct UpgradeChoiceView
{
    public UpgradeChoiceView(
        string id, UpgradeKind kind, int nextLevel, string category, string name,
        string behavior, string delta, Sprite icon)
    {
        Id = id;
        Kind = kind;
        NextLevel = nextLevel;
        Category = category;
        Name = name;
        Behavior = behavior;
        Delta = delta;
        Icon = icon;
    }

    public string Id { get; }
    public UpgradeKind Kind { get; }
    public int NextLevel { get; }
    public string Category { get; }
    public string Name { get; }
    public string Behavior { get; }
    public string Delta { get; }
    public Sprite Icon { get; }
}

public readonly struct WeaponSlotView
{
    public WeaponSlotView(string id, string displayName, int level, Sprite icon)
    {
        Id = id;
        DisplayName = displayName;
        Level = level;
        Icon = icon;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public int Level { get; }
    public Sprite Icon { get; }
}

public sealed class UpgradeChoiceState
{
    public UpgradeChoiceState(int level, IEnumerable<UpgradeChoiceView> choices)
    {
        Level = level;
        Choices = Array.AsReadOnly(choices.ToArray());
    }

    public int Level { get; }
    public IReadOnlyList<UpgradeChoiceView> Choices { get; }
}

public enum ProgressionRewardKind { Support, WeaponLevel, NewWeapon, Evolution }

public readonly struct ProgressionRewardEvent
{
    public ProgressionRewardEvent(
        string id, string weaponId, int newLevel, ProgressionRewardKind kind,
        string displayName, string changeSummary, Sprite icon)
    {
        Id = id;
        WeaponId = weaponId;
        NewLevel = newLevel;
        Kind = kind;
        DisplayName = displayName;
        ChangeSummary = changeSummary;
        Icon = icon;
    }

    public string Id { get; }
    public string WeaponId { get; }
    public int NewLevel { get; }
    public ProgressionRewardKind Kind { get; }
    public string DisplayName { get; }
    public string ChangeSummary { get; }
    public Sprite Icon { get; }
}
```

- [ ] **Step 4: Publish state and guarded commands from the controller**

```csharp
public FirstPlayableUiState UiState => BuildUiState();
public bool IsUpgradeOpen => upgradeOpen;
public event Action<UpgradeChoiceState> UpgradeOpened;
public event Action<ProgressionRewardEvent> UpgradeChosen;
public event Action RunReset;
public bool IsCombatTargetAlive(int runtimeId) =>
    combatTargets != null && combatTargets.TryGet(runtimeId, out var target) && target.IsAlive;

public bool TryChooseUpgrade(int index)
{
    if (!upgradeOpen || index < 0 || index >= upgradeOfferData.Count) return false;
    var reward = ApplyUpgrade(upgradeOfferData[index]);
    upgradeOpen = false;
    upgradeOffers.Clear();
    upgradeOfferData.Clear();
    UpgradeChosen?.Invoke(reward);
    return true;
}
```

`OpenUpgrade()` must map the three domain offers to `UpgradeChoiceView` values and raise one `UpgradeOpened` event after the list is complete. `ResetRun()` must raise `RunReset` after state initialization.
Change `AddExperience` to a `while (experience >= experienceToNext)` loop that
increments `pendingUpgradeCount`; open one choice immediately and open the next
queued choice only after the current reward close callback completes.

Add explicit test hooks behind Unity's test define so PlayMode tests do not use
reflection:

```csharp
#if UNITY_INCLUDE_TESTS
public IReadOnlyList<UpgradeOffer> CurrentOffers => upgradeOfferData;
public int AppliedUpgradeCount { get; private set; }
public void OpenUpgradeForTests() => OpenUpgrade();
#endif
```

Increment `AppliedUpgradeCount` only after `TryChooseUpgrade` accepts and applies
an offer, and reset it in `ResetRun()`.

- [ ] **Step 5: Run the focused test and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.FirstPlayableUiStateTests' -testResults 'Temp\ui-state-green.xml' -logFile 'Temp\ui-state-green.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs' 'Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs' 'Assets/JoseonHunter/Tests/EditMode/FirstPlayableUiStateTests.cs'
git commit -m 'feat: publish first playable UI state'
```

Expected: focused test passes and no unrelated files are staged.

---

### Task 2: Build the portrait combat HUD and weapon rack

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/JoseonUiPalette.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/RuntimeUiFactory.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`

**Interfaces:**
- Consumes: `FirstPlayableController.UiState`
- Produces: `FirstPlayableUiBootstrap.BoundController`
- Produces: `CombatHudPresenter.Render(FirstPlayableUiState state)`
- Produces: `WeaponRackPresenter.Render(IReadOnlyList<WeaponSlotView> weapons)`

- [ ] **Step 1: Write a PlayMode test for the generated hierarchy**

```csharp
[UnityTest]
public IEnumerator Bootstrap_creates_portrait_hud_and_weapon_rack()
{
    var root = new GameObject("UI Test");
    root.AddComponent<FirstPlayableUiBootstrap>();
    yield return null;

    var canvas = root.GetComponentInChildren<Canvas>(true);
    var scaler = root.GetComponentInChildren<CanvasScaler>(true);

    Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
    Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
    Assert.That(root.GetComponentInChildren<CombatHudPresenter>(true), Is.Not.Null);
    Assert.That(root.GetComponentInChildren<WeaponRackPresenter>(true), Is.Not.Null);
    Object.Destroy(root);
}
```

- [ ] **Step 2: Run the focused PlayMode test and confirm RED**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests' -testResults 'Temp\hud-red.xml' -logFile 'Temp\hud-red.log' -quit
```

- [ ] **Step 3: Implement the shared palette and UI factory**

```csharp
public static class JoseonUiPalette
{
    public static readonly Color Ink = new(0.055f, 0.064f, 0.082f, 0.96f);
    public static readonly Color Hanji = new(0.91f, 0.86f, 0.72f, 1f);
    public static readonly Color Crimson = new(0.72f, 0.12f, 0.13f, 1f);
    public static readonly Color Jade = new(0.20f, 0.72f, 0.68f, 1f);
    public static readonly Color Gold = new(0.94f, 0.67f, 0.20f, 1f);

    public static Color WeaponAccent(WeaponId id)
    {
        if (id.Equals(WeaponId.FrostFlask)) return Jade;
        if (id.Equals(WeaponId.ThunderCrashBomb) || id.Equals(WeaponId.WindThunderFan))
            return new Color(0.62f, 0.42f, 0.94f, 1f);
        if (id.Equals(WeaponId.SingijeonVolley)) return new Color(0.94f, 0.34f, 0.18f, 1f);
        return Gold;
    }
}
```

`RuntimeUiFactory` must expose concrete helpers:

```csharp
public static RectTransform Rect(string name, Transform parent);
public static Image Image(string name, Transform parent, Color color);
public static TextMeshProUGUI Text(string name, Transform parent, string value, float size, TextAlignmentOptions alignment);
public static Button Button(string name, Transform parent, Color color);
public static void Stretch(RectTransform rect, float left, float bottom, float right, float top);
```

- [ ] **Step 4: Create the bootstrap, HUD, and weapon rack**

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void EnsureBootstrap()
{
    if (FindObjectOfType<FirstPlayableUiBootstrap>() != null) return;
    var root = new GameObject("First Playable UI");
    root.AddComponent<FirstPlayableUiBootstrap>();
}

private void BuildCanvas()
{
    var canvas = gameObject.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 100;
    var scaler = gameObject.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1080f, 1920f);
    scaler.matchWidthOrHeight = 1f;
    gameObject.AddComponent<GraphicRaycaster>();
}
```

Anchor health/experience at the safe-area top-left, timer/kills top-right, and weapon rack bottom-left. Keep the center 65% of the screen free of persistent panels. Render only owned weapons and load each icon from `WeaponSlotView.Icon`.

- [ ] **Step 5: Disable legacy gameplay IMGUI and verify**

Keep only the run-ended restart panel in `FirstPlayableController.OnGUI()` until a later results-screen task. Remove HUD, boss warning, boss health, magnet message, and upgrade-choice drawing from `OnGUI()`.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests' -testResults 'Temp\hud-green.xml' -logFile 'Temp\hud-green.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI' 'Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs' 'Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs'
git commit -m 'feat: add portrait combat HUD'
```

Expected: the generated canvas and presenters exist, and only the new HUD is visible during combat.

---

### Task 3: Add the 0.3-second slowdown and guarded upgrade cards

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`

**Interfaces:**
- Consumes: `FirstPlayableController.UpgradeOpened`
- Consumes: `FirstPlayableController.TryChooseUpgrade(int index)`
- Produces: `UpgradeChoicePresenter.Open(UpgradeChoiceState, Func<int, bool>)`
- Produces: `UpgradeChoicePresenter.IsOpen`, `IsChoiceLocked`

- [ ] **Step 1: Write the slowdown/pause test**

```csharp
[UnityTest]
public IEnumerator Upgrade_open_slows_for_point_three_seconds_then_pauses()
{
    var go = new GameObject("Upgrade Presenter");
    var presenter = go.AddComponent<UpgradeChoicePresenter>();
    presenter.BuildForTests();
    presenter.Open(new UpgradeChoiceState(2, new[]
    {
        new UpgradeChoiceView("gakgung_shot", UpgradeKind.Weapon, 1, "신규 무기", "각궁", "직선 관통 사격", "신규", null),
        new UpgradeChoiceView("boots", UpgradeKind.Support, 1, "능력 강화", "경쾌한 버선", "이동 속도 증가", "+12%", null),
        new UpgradeChoiceView("talisman", UpgradeKind.Support, 1, "능력 강화", "호신부적", "최대 체력 증가", "+20", null)
    }), _ => true);

    yield return new WaitForSecondsRealtime(0.15f);
    Assert.That(Time.timeScale, Is.InRange(0.01f, 0.99f));

    yield return new WaitForSecondsRealtime(0.20f);
    Assert.That(Time.timeScale, Is.EqualTo(0f));
    Assert.That(presenter.IsOpen, Is.True);

    presenter.CloseImmediately();
    Assert.That(Time.timeScale, Is.EqualTo(1f));
    Object.Destroy(go);
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.UpgradeChoicePlayModeTests' -testResults 'Temp\upgrade-red.xml' -logFile 'Temp\upgrade-red.log' -quit
```

- [ ] **Step 3: Implement unscaled transition and safe cleanup**

```csharp
private IEnumerator OpenRoutine()
{
    var elapsed = 0f;
    while (elapsed < 0.3f)
    {
        elapsed += Time.unscaledDeltaTime;
        Time.timeScale = Mathf.Lerp(1f, 0.08f, Mathf.Clamp01(elapsed / 0.3f));
        overlay.alpha = Mathf.Clamp01(elapsed / 0.3f);
        yield return null;
    }

    Time.timeScale = 0f;
    cardsRoot.SetActive(true);
}

public void CloseImmediately()
{
    if (openRoutine != null) StopCoroutine(openRoutine);
    openRoutine = null;
    Time.timeScale = 1f;
    IsOpen = false;
    IsChoiceLocked = false;
    root.SetActive(false);
}

private void OnDisable() => CloseImmediately();
```

- [ ] **Step 4: Build three readable cards and lock the first accepted click**

```csharp
private void Choose(int index)
{
    if (!IsOpen || IsChoiceLocked) return;
    IsChoiceLocked = true;
    if (!choose(index))
    {
        IsChoiceLocked = false;
        return;
    }

    StartCoroutine(CloseRoutine());
}
```

Each card must render `Category`, `Icon`, `Name`, `Behavior`, and `Delta` in the same locations. `최종 진화` uses gold; `신규 무기` uses jade; support uses neutral hanji. Buttons must remain reachable inside the device safe area.
If `Icon` is null, render the category's shared TMP glyph (`刀`, `符`, or `氣`)
without disabling the button.

- [ ] **Step 5: Verify and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.UpgradeChoicePlayModeTests' -testResults 'Temp\upgrade-green.xml' -logFile 'Temp\upgrade-green.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/UpgradeChoicePresenter.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs' 'Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs' 'Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs'
git commit -m 'feat: add cinematic upgrade choice flow'
```

Expected: one selection is accepted, combat is fully paused after 0.3 seconds, and timescale always returns to 1 on close/reset/disable.

---

### Task 4: Add reward reveals and weapon-rack change feedback

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs`

**Interfaces:**
- Consumes: `ProgressionRewardEvent`
- Produces: `RewardRevealPresenter.Play(ProgressionRewardEvent reward)`
- Produces: `WeaponRackPresenter.Pulse(string weaponId, int newLevel)`

- [ ] **Step 1: Write reveal-priority tests**

```csharp
[TestCase(ProgressionRewardKind.Support, 70)]
[TestCase(ProgressionRewardKind.WeaponLevel, 80)]
[TestCase(ProgressionRewardKind.NewWeapon, 90)]
[TestCase(ProgressionRewardKind.Evolution, 100)]
public void Reward_kind_maps_to_expected_intensity(ProgressionRewardKind kind, int expected)
{
    Assert.That(RewardRevealPresenter.IntensityFor(kind), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Implement reveal profiles**

```csharp
public static int IntensityFor(ProgressionRewardKind kind) => kind switch
{
    ProgressionRewardKind.Evolution => 100,
    ProgressionRewardKind.NewWeapon => 90,
    ProgressionRewardKind.WeaponLevel => 80,
    _ => 70
};

public void Play(ProgressionRewardEvent reward)
{
    StopAllCoroutines();
    title.text = reward.DisplayName;
    detail.text = reward.ChangeSummary;
    icon.sprite = reward.Icon;
    StartCoroutine(PlayRoutine(IntensityFor(reward.Kind)));
}
```

New weapon reveals last at most 0.6 unscaled seconds. Normal weapon-level rewards pulse only the rack slot. Evolution reveal owns the center overlay and is allowed to reach intensity 100.

- [ ] **Step 3: Bind events and verify**

```csharp
private void Bind(FirstPlayableController next)
{
    Unbind();
    controller = next;
    if (controller == null) return;
    controller.UpgradeChosen += OnUpgradeChosen;
    controller.RunReset += OnRunReset;
}

private void OnUpgradeChosen(ProgressionRewardEvent reward)
{
    weaponRack.Pulse(reward.Id, reward.NewLevel);
    rewardReveal.Play(reward);
}
```

- [ ] **Step 4: Run tests and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.RewardRevealPlayModeTests' -testResults 'Temp\reveal-green.xml' -logFile 'Temp\reveal-green.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/RewardRevealPresenter.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs' 'Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs'
git commit -m 'feat: add progression reward reveals'
```

---

### Task 5: Direct confirmed-contact feedback through an intensity budget

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/FirstPlayableDamageNumberBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/FeedbackBudgetTests.cs`

**Interfaces:**
- Consumes: `CombatDamageService.DamageConfirmed`
- Produces: `FeedbackRequest`
- Produces: `CombatFeedbackBudget.Resolve(FeedbackRequest request)`
- Produces: `CombatFeedbackDirector.Bind(CombatDamageService service)`

- [ ] **Step 1: Write deterministic feedback-budget tests**

```csharp
[Test]
public void Normal_contact_never_requests_camera_impulse()
{
    var profile = CombatFeedbackBudget.Resolve(new FeedbackRequest(
        critical: false, killed: false, boss: false, reducedEffects: false));

    Assert.That(profile.Intensity, Is.EqualTo(70));
    Assert.That(profile.HitStopSeconds, Is.EqualTo(0f));
    Assert.That(profile.CameraImpulse, Is.EqualTo(0f));
}

[Test]
public void Reduced_effects_preserves_contact_flash_but_removes_camera_impulse()
{
    var profile = CombatFeedbackBudget.Resolve(new FeedbackRequest(
        critical: true, killed: true, boss: false, reducedEffects: true));

    Assert.That(profile.ShowContactFlash, Is.True);
    Assert.That(profile.CameraImpulse, Is.EqualTo(0f));
}
```

- [ ] **Step 2: Implement budget values**

```csharp
public static FeedbackProfile Resolve(FeedbackRequest request)
{
    var intensity = request.Killed || request.Critical ? 80 : 70;
    if (request.ReducedEffects)
        return new FeedbackProfile(intensity, 0f, 0f, true);

    return intensity == 80
        ? new FeedbackProfile(80, 0.035f, 0.08f, true)
        : new FeedbackProfile(70, 0f, 0f, true);
}
```

Add the value types beside the budget so every caller uses the same profile:

```csharp
public readonly struct FeedbackRequest
{
    public FeedbackRequest(bool critical, bool killed, bool boss, bool reducedEffects)
    {
        Critical = critical;
        Killed = killed;
        Boss = boss;
        ReducedEffects = reducedEffects;
    }

    public bool Critical { get; }
    public bool Killed { get; }
    public bool Boss { get; }
    public bool ReducedEffects { get; }
}

public readonly struct FeedbackProfile
{
    public FeedbackProfile(int intensity, float hitStopSeconds, float cameraImpulse, bool showContactFlash)
    {
        Intensity = intensity;
        HitStopSeconds = hitStopSeconds;
        CameraImpulse = cameraImpulse;
        ShowContactFlash = showContactFlash;
    }

    public int Intensity { get; }
    public float HitStopSeconds { get; }
    public float CameraImpulse { get; }
    public bool ShowContactFlash { get; }
}
```

- [ ] **Step 3: Subscribe beside the existing damage pool**

`FirstPlayableDamageNumberBootstrap` must create one `CombatFeedbackDirector`, set its boss predicate from `controller.IsBossCombatTarget`, and bind/unbind it whenever the combat service changes. The director spawns a short pooled sprite flash at `confirmed.ContactPoint`. It may apply hit stop only to critical or kill requests and must use unscaled time to restore `Time.timeScale`.
Set an alive predicate from `controller.IsCombatTargetAlive`; because resolved damage is
already applied before `DamageConfirmed` is raised, `!isAlive(confirmed.TargetRuntimeId)`
is the kill flag for the feedback request.

- [ ] **Step 4: Differentiate number motion without duplicating numbers**

Update `DamageNumberPresenter.Play(...)` so normal values rise quickly, critical values scale once, and boss values remain visible slightly longer. Do not add a second presenter for the same `DamageNumberDisplay`; aggregation remains owned by `DamageNumberAccumulator`.

- [ ] **Step 5: Run tests and commit**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform EditMode -testFilter 'JoseonHunter.Tests.EditMode.FeedbackBudgetTests' -testResults 'Temp\feedback-green.xml' -logFile 'Temp\feedback-green.log' -quit
git add -- 'Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs' 'Assets/JoseonHunter/Scripts/Presentation/Combat/FirstPlayableDamageNumberBootstrap.cs' 'Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs' 'Assets/JoseonHunter/Tests/EditMode/FeedbackBudgetTests.cs'
git commit -m 'feat: add confirmed-contact feedback budget'
```

---

### Task 6: Integrate and validate the UI vertical slice

**Files:**
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs`
- Modify only if required: `Assets/JoseonHunter/Scenes/Gameplay.unity`

**Interfaces:**
- Consumes all UI and feedback interfaces from Tasks 1–5
- Produces a playable HUD/upgrade/reward vertical slice without requiring scene-authored UI

- [ ] **Step 1: Add a single full-flow PlayMode test**

```csharp
[UnityTest]
public IEnumerator Level_up_opens_cards_accepts_one_choice_and_restores_combat()
{
    SceneManager.LoadScene("Gameplay");
    yield return null;
    var controller = Object.FindFirstObjectByType<FirstPlayableController>();
    var ui = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
    ui.BindForTests(controller);

    controller.OpenUpgradeForTests();
    yield return new WaitForSecondsRealtime(0.35f);
    Assert.That(Time.timeScale, Is.EqualTo(0f));

    ui.UpgradeChoice.ChooseForTests(0);
    ui.UpgradeChoice.ChooseForTests(1);
    yield return new WaitForSecondsRealtime(0.25f);

    Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(1));
    Assert.That(Time.timeScale, Is.EqualTo(1f));
    Assert.That(ui.RewardReveal.LastReward.Id, Is.Not.Empty);
}
```

- [ ] **Step 2: Run the focused UI suites**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\UnityProjects\JoseonHunter' -runTests -testPlatform PlayMode -testFilter 'JoseonHunter.Tests.PlayMode.CombatHudPlayModeTests|JoseonHunter.Tests.PlayMode.UpgradeChoicePlayModeTests|JoseonHunter.Tests.PlayMode.RewardRevealPlayModeTests' -testResults 'Temp\ui-slice.xml' -logFile 'Temp\ui-slice.log' -quit
```

Expected: all UI slice tests pass. If Unity's filter does not accept `|`, run the three class filters sequentially without changing the project.

- [ ] **Step 3: Perform one portrait manual check**

Open `Assets/JoseonHunter/Scenes/Gameplay.unity`, set Game view to 1080 × 1920 portrait, and verify:

- HUD respects the safe area and leaves the central combat field clear.
- Weapon icons show only acquired weapons.
- Upgrade flow visibly decelerates for 0.3 seconds, then completely pauses.
- Every card shows category, icon, name, behavior, and delta without clipping.
- One click applies exactly one reward.
- Damage numbers originate at contacts and do not duplicate.
- New weapon reveal is strong but shorter than evolution reveal.

- [ ] **Step 4: Commit only necessary integration changes**

```powershell
git add -- 'Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs' 'Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs' 'Assets/JoseonHunter/Tests/PlayMode/RewardRevealPlayModeTests.cs'
git commit -m 'test: validate combat UI vertical slice'
```

Do not stage `Gameplay.unity` if the runtime bootstrap made scene changes unnecessary.
