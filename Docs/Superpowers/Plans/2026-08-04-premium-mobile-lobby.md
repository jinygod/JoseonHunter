# Premium Mobile Lobby Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** C 혼합형 구도로 한연화 영웅 일러스트, 명확한 출전 카드, 실제 세 메뉴만 갖춘 출시형 모바일 로비와 공유 로딩 화면을 만든다.

**Architecture:** 기존 `LobbyBootstrap`과 세 프레젠터의 데이터·버튼 동작은 유지하고, `LobbyUiFactory`의 시각 토큰과 생성 구조를 프리미엄 셸에 맞게 확장한다. 한연화 원화는 로비와 Bootstrap 로딩 프리팹에서 같은 Resources 스프라이트를 서로 다른 크롭으로 사용하며, 생성 자산은 Unity 임포트 규칙과 씬 계약 테스트로 보호한다.

**Tech Stack:** Unity 6.0, uGUI, TextMeshPro, NUnit EditMode/PlayMode, OpenAI ImageGen, PixelLab UI asset generation

## Global Constraints

- 실제 메뉴는 `무기 연구`, `출전`, `공통 수련` 세 개뿐이다.
- 상점, 우편, 임무, 에너지, 보석 등 새 메타 시스템을 만들지 않는다.
- 한연화는 성인 여성인 비픽셀 조선 판타지 퇴마 검사로 표현한다.
- 팔레트는 먹색, 검붉은색, 탁한 금색, 따뜻한 한지색으로 제한한다.
- 흰 외곽선과 작은 화면에서 뭉개지는 잔픽셀 장식을 사용하지 않는다.
- 저장 데이터 형식과 게임플레이 씬은 변경하지 않는다.
- 사용자 소유의 기존 dirty 파일은 스테이징하지 않는다.
- Unity 배치 작업은 `BelowNormal` 우선순위와 4코어 affinity로 순차 실행한다.

---

### Task 1: 한연화 및 로비 UI 자산 제작

**Files:**
- Create: `Assets/JoseonHunter/Art/Characters/Lobby/han_yeonhwa_hero.png`
- Create: `Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_frame.png`
- Create: `Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_primary_button.png`
- Create: `Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png`
- Create: Unity-generated `.meta` files for the three assets
- Test: `Assets/JoseonHunter/Tests/EditMode/PremiumLobbyAssetContractTests.cs`

**Interfaces:**
- Consumes: ImageGen generated PNG, PixelLab UI panel PNG, Unity `TextureImporter`
- Produces: `Resources.Load<Sprite>("Lobby/han_yeonhwa_hero")`, 9-slice-capable frame and primary-button sprites

- [ ] **Step 1: Write the failing asset contract test**

```csharp
[Test]
public void PremiumLobbyArtExistsAndIsMobileBounded()
{
    foreach (var path in new[] { HeroPath, ResourceHeroPath, FramePath, PrimaryButtonPath })
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048));
    }
}
```

- [ ] **Step 2: Run the focused EditMode test and verify RED**

Run Unity EditMode with filter `JoseonHunter.Tests.EditMode.PremiumLobbyAssetContractTests`.

Expected: FAIL because the hero and premium frame sprites do not exist.

- [ ] **Step 3: Generate the hero illustration**

Use ImageGen with this production prompt:

```text
Premium vertical mobile game key art of Han Yeonhwa, an unmistakably adult Korean woman and confident Joseon-fantasy exorcist swordswoman. Beautiful, glamorous and alluring but tasteful, elegant facial features, long flowing black hair, fitted deep crimson and charcoal hanbok-inspired battle dress with restrained antique-gold ornaments, one hand near a traditional Korean sword, moonlit tiled palace roofs and mist behind her. Painterly semi-realistic Korean fantasy game illustration, strong readable silhouette, limited burgundy/ink/gold palette, no text, no logo, no white outline, no chibi, no pixel art, no modern clothing. Full body to upper-thigh composition, character biased to the right, clean darker negative space on the left and lower center for mobile UI, portrait 9:16.
```

Save the selected result as both art-source and Resources PNG without resampling it twice.

- [ ] **Step 4: Generate the PixelLab UI frame**

Call PixelLab `create_ui_asset` at 600×448 with:

```text
Joseon dark-fantasy mobile game stage card, broad simple shapes, matte ink-black interior, deep burgundy lower edge, restrained antique-gold trim, warm hanji inset, clean corners suitable for nine-slicing, no text, no icons, no white outline, no tiny ornaments, limited four-color palette
```

Use `elements: ["panel"]`, transparent background, then download the completed PNG to `premium_lobby_frame.png`. Make a second 384×192 `elements: ["button"]` asset with the same palette and a broad gold-faced primary action, saving it as `premium_lobby_primary_button.png`.

- [ ] **Step 5: Import with mobile-safe settings and run GREEN**

Set hero sprites to Sprite/Single, max size 2048, mipmaps off, alpha as transparency when present, bilinear filtering; set the pixel UI sprites to Sprite/Single, max size 1024, mipmaps off, point filtering. Set the frame border to 48px on every side and the primary button border to 32px so Unity can use `Image.Type.Sliced`. Run the focused test and expect PASS.

- [ ] **Step 6: Commit and push the asset bundle**

```powershell
git add -- 'Assets/JoseonHunter/Art/Characters/Lobby/han_yeonhwa_hero.png' 'Assets/JoseonHunter/Art/Characters/Lobby/han_yeonhwa_hero.png.meta' 'Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_frame.png' 'Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_frame.png.meta' 'Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_primary_button.png' 'Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_primary_button.png.meta' 'Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png' 'Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png.meta' 'Assets/JoseonHunter/Tests/EditMode/PremiumLobbyAssetContractTests.cs' 'Assets/JoseonHunter/Tests/EditMode/PremiumLobbyAssetContractTests.cs.meta'
git commit -m "art: add premium lobby presentation"
git push origin master
```

### Task 2: 프리미엄 로비 셸과 스타일 토큰

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyUiFactory.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyHeroMotion.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`

**Interfaces:**
- Consumes: `Resources.Load<Sprite>("Lobby/han_yeonhwa_hero")`, existing session coin count
- Produces: named roots `Hero Art`, `Stage Content`, `Bottom Navigation`; exactly three functional navigation buttons

- [ ] **Step 1: Extend scene contract tests for the C layout**

```csharp
foreach (var required in new[] { "Hero Art", "Hero Shade", "Stage Content", "Bottom Navigation" })
    Assert.That(transforms.Any(item => item.name == required), Is.True, required);
Assert.That(transforms.Single(item => item.name == "Hero Art").GetComponent<Image>().sprite, Is.Not.Null);
Assert.That(transforms.Single(item => item.name == "Hero Art").GetComponent<LobbyHeroMotion>(), Is.Not.Null);
```

Keep the PlayMode assertion that navigation labels equal exactly `무기 연구`, `출전`, `공통 수련`.

- [ ] **Step 2: Run the two focused tests and verify RED**

Run filters `LobbySceneContractTests` and `LobbyNavigationPlayModeTests`.

Expected: the scene contract fails because the new named layout roots do not exist.

- [ ] **Step 3: Add reusable premium style helpers**

Add these factory APIs:

```csharp
internal static readonly Color NightInk = new(.035f, .043f, .065f, 1f);
internal static readonly Color Crimson = new(.34f, .10f, .075f, 1f);
internal static readonly Color AntiqueGold = new(.78f, .54f, .20f, 1f);
internal static Button Button(string name, Transform parent, string label, float size,
    Color background, Color foreground);
internal static void AddGoldRule(Transform parent, Vector2 min, Vector2 max);
```

The overload keeps existing call sites compiling while allowing active navigation and primary action colors.

- [ ] **Step 4: Rebuild the lobby shell hierarchy**

Build the safe-area children in this order:

```text
Header (90.5%–98.5%)
Hero Art (39%–91%)
Hero Shade (39%–91%)
Stage Content / active panels (12%–58%)
Bottom Navigation (1.5%–11%)
```

The patrol panel may visually overlap the hero region up to 60%; research and training use a solid NightInk content card. Preserve `MetaGameSession.EnsureExists()`, safe-area handling, and the existing three presenters.

Add a small presentation-only component:

```csharp
private void Update()
{
    var pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.2f) * .008f;
    transform.localScale = Vector3.one * pulse;
}
```

This provides a sub-1% breathing motion without allocations, blur, or duplicated textures.

- [ ] **Step 5: Rebuild Lobby scene and run GREEN**

Run `JoseonHunter.Editor.Scenes.LobbySceneBuilder.BuildInBatchMode`, then the two focused tests. Expect PASS.

- [ ] **Step 6: Commit and push the shell**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyUiFactory.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyHeroMotion.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyHeroMotion.cs.meta' 'Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs' 'Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs' 'Assets/JoseonHunter/Scenes/Lobby.unity' 'Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab'
git commit -m "feat: build premium mobile lobby shell"
git push origin master
```

### Task 3: 출전 카드와 기존 편성 기능 재배치

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`

**Interfaces:**
- Consumes: existing three loadouts, selected weapon/style, best patrol result, router
- Produces: `Stage Name`, `Difficulty`, `Starting Weapon`, `Style`, `Record`, large `Start Patrol` button

- [ ] **Step 1: Add a failing hierarchy and copy test**

```csharp
Assert.That(GameObject.Find("Stage Name").GetComponent<TMP_Text>().text, Is.EqualTo("귀곡 야행"));
Assert.That(GameObject.Find("Start Patrol").GetComponentInChildren<TMP_Text>().text, Is.EqualTo("출전"));
Assert.That(GameObject.Find("Start Patrol").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(76f));
```

Retain the existing independent three-preset persistence assertions.

- [ ] **Step 2: Run `LobbyPatrolPlayModeTests` and verify RED**

Expected: FAIL because `Stage Name` and the premium card sizing do not exist.

- [ ] **Step 3: Build the compact stage card**

Replace the rookie pixel character and full-height hanji detail panel with:

```text
Stage Name: 귀곡 야행
Difficulty: 난이도 · 보통
Preset: 편성 N · 이름
Starting Weapon: 시작 무기 · 무기명
Style: 운용법 · 운용법명
Record: 최고 승리 처치 N / 기록 없음
Primary: 출전
Secondary: 이전·다음 편성, 이전·다음 무기, 편성 저장
```

Use one-line labels, a 76px-or-taller primary button, and small secondary buttons that do not compete with `출전`.

- [ ] **Step 4: Run focused GREEN and navigation regression**

Run `LobbyPatrolPlayModeTests` and `LobbyNavigationPlayModeTests`. Expect PASS.

- [ ] **Step 5: Commit and push the patrol card**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs' 'Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs'
git commit -m "feat: focus lobby on patrol launch"
git push origin master
```

### Task 4: 무기 연구와 공통 수련의 모바일 카드화

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`
- Test: existing `WeaponResearchLobbyPlayModeTests.cs`
- Test: existing `CommonTrainingLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes: mastery/style purchase and common training purchase/reset APIs
- Produces: readable dark-card layouts with existing button names and test hooks unchanged

- [ ] **Step 1: Add failing visual-contract assertions**

Add assertions that style and training buttons use opaque backgrounds, body text is at least 18px, and the purchase action is at least 64px tall while keeping current behavioral assertions.

- [ ] **Step 2: Run the two focused suites and verify RED**

Expected: at least one height or premium background assertion fails on the current layout.

- [ ] **Step 3: Reflow both presenters**

Keep all public test hooks and button object names. Use a dark opaque content card, antique-gold headings, warm hanji body copy, 12–16px vertical gaps, and 64px-or-taller purchase actions. Do not add navigation or new progression features.

- [ ] **Step 4: Run focused GREEN**

Run `WeaponResearchLobbyPlayModeTests` and `CommonTrainingLobbyPlayModeTests`. Expect PASS.

- [ ] **Step 5: Commit and push the secondary panels**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs' 'Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs' 'Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs' 'Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs'
git commit -m "feat: polish lobby progression panels"
git push origin master
```

### Task 5: 한연화 공유 로딩 화면

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/BootstrapLoadingBuilder.cs`
- Modify: `Assets/JoseonHunter/Prefabs/UI/BootstrapLoading.prefab`
- Modify: `Assets/JoseonHunter/Scenes/Bootstrap.unity`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/BootstrapLoadingPlayModeTests.cs`

**Interfaces:**
- Consumes: `Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png`
- Produces: Bootstrap overlay with shared hero image, title, Korean status, progress bar, unchanged Lobby destination

- [ ] **Step 1: Add a failing loading-art assertion**

```csharp
var hero = GameObject.Find("Han Yeonhwa Loading Art").GetComponent<Image>();
Assert.That(hero.sprite, Is.Not.Null);
Assert.That(hero.color.a, Is.GreaterThan(.9f));
```

- [ ] **Step 2: Run `BootstrapLoadingPlayModeTests` and verify RED**

Expected: FAIL because `Han Yeonhwa Loading Art` does not exist.

- [ ] **Step 3: Replace the spirit-flame center composition**

Use the shared hero sprite as a right-biased full-height image with a dark gradient overlay. Keep `조선 요괴 사냥꾼`, `어둠 속 길을 밝히는 중…`, progress fill, `MinimumVisibleSeconds`, timeout, and fade behavior. Reuse the hero image rather than creating a second texture.

- [ ] **Step 4: Build Bootstrap and run GREEN**

Run `BootstrapLoadingBuilder.BuildInBatchMode`, then `BootstrapLoadingPlayModeTests`. Expect PASS.

- [ ] **Step 5: Commit and push the loading presentation**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Editor/Scenes/BootstrapLoadingBuilder.cs' 'Assets/JoseonHunter/Prefabs/UI/BootstrapLoading.prefab' 'Assets/JoseonHunter/Scenes/Bootstrap.unity' 'Assets/JoseonHunter/Tests/PlayMode/BootstrapLoadingPlayModeTests.cs'
git commit -m "feat: feature han yeonhwa in loading"
git push origin master
```

### Task 6: 전체 검증과 모바일 공개 캡처

**Files:**
- Create: `Docs/Verification/2026-08-04-premium-mobile-lobby.md`
- Generate and force-add: `Artifacts/LobbyPremium/*.png`
- Generate and force-add: `Artifacts/BootstrapLoadingPremium/*.png`

**Interfaces:**
- Consumes: lobby and bootstrap builders/capture commands, Unity test runner
- Produces: mobile-visible GitHub raw URLs and evidence-backed verification report

- [ ] **Step 1: Rebuild authored assets**

Run lobby and Bootstrap builders sequentially in Unity batch mode with BelowNormal priority and four-core affinity.

- [ ] **Step 2: Run full regression tests**

Run the full EditMode suite and full PlayMode suite. Expected: zero failures and no C# compilation errors.

- [ ] **Step 3: Capture every required state**

Capture 720×1280 and 1080×2340 for:

```text
Lobby default patrol
Lobby weapon research
Lobby common training
Bootstrap loading
```

- [ ] **Step 4: Inspect captures and resource limits**

Verify Korean glyphs, safe area, no overlap, exactly three menu tabs, visible primary action, hero crop, no white outline, and max texture size ≤2048. Record actual test counts and any known limitation in the verification report.

- [ ] **Step 5: Commit and push evidence**

```powershell
git add -- 'Docs/Verification/2026-08-04-premium-mobile-lobby.md'
git add -f -- 'Artifacts/LobbyPremium' 'Artifacts/BootstrapLoadingPremium'
git commit -m "test: verify premium mobile lobby"
git push origin master
```
