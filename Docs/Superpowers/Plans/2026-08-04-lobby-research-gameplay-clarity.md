# Lobby, Research, and Gameplay Clarity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 픽셀 로비의 시각 통일성을 회복하고 무기 연구·로딩·전투 조작·픽업·무기 HUD를 모바일에서 명확하게 보이도록 개선한다.

**Architecture:** 기존 Presenter와 저장 스키마를 유지하면서 보이는 계층과 입력 흐름만 단순화한다. 출전은 현재 활성 Loadout 하나를 자동 저장하고 Bootstrap 씬을 경유하며, 연구 단계 조건은 도메인에서 순차적으로 강제한다. 전투 HUD와 픽업은 기존 Controller 및 Presenter의 확장점 안에서 수정한다.

**Tech Stack:** Unity 6000.5.5f1, uGUI, TextMeshPro, NUnit EditMode/PlayMode, OpenAI ImageGen, PixelLab UI 생성, Git `master`

## Global Constraints

- 하단 메뉴 `무기 연구`, `출전`, `공통 수련`은 유지한다.
- 출전 화면의 `편성`, `편성 저장`, 편성 번호는 제거한다.
- 저장 스키마 버전과 기존 `PatrolLoadouts` 데이터는 변경하지 않는다.
- 잠금 안내 문구는 정확히 `2단계 연구 완료 시 해금`이다.
- 경험치 획득 반경 `StartingPickupRadius`와 수련 배율은 변경하지 않는다.
- 한연화는 로비에서 제거하고 시작·출전 로딩에서만 사용한다.
- 새 한연화 원화에는 손과 손가락이 보이지 않아야 한다.
- 기존 사용자 dirty 파일은 스테이징하지 않는다.
- Unity 배치 작업은 BelowNormal 우선순위와 4코어 affinity로 순차 실행한다.
- 완료한 각 기능 묶음은 `master`에 커밋하고 `origin/master`로 푸시한다.

---

### Task 1: 공용 버튼·무기 슬롯·한연화 에셋

**Files:**
- Create: `Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_secondary_button.png`
- Create: `Assets/JoseonHunter/Art/UI/Combat/compact_weapon_slot.png`
- Replace: `Assets/JoseonHunter/Art/Characters/Lobby/han_yeonhwa_hero.png`
- Replace: `Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/JoseonAssetPostprocessor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/PremiumLobbyAssetContractTests.cs`

**Interfaces:**
- Consumes: PixelLab PNG 결과, ImageGen PNG 결과, Unity `TextureImporter`
- Produces: 9-slice 보조 버튼, 9-slice 둥근 무기 슬롯, `Resources.Load<Sprite>("Lobby/han_yeonhwa_hero")`

- [ ] **Step 1: 실패하는 에셋 계약 테스트 작성**

```csharp
[Test]
public void SecondaryButtonAndCompactSlotAreSlicedMobileSprites()
{
    foreach (var path in new[] { SecondaryButtonPath, CompactWeaponSlotPath })
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Assert.That(sprite, Is.Not.Null, path);
        Assert.That(sprite.border.sqrMagnitude, Is.GreaterThan(0f), path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Assert.That(importer.mipmapEnabled, Is.False, path);
        Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(1024), path);
    }
}
```

- [ ] **Step 2: EditMode RED 실행**

Run filter: `JoseonHunter.Tests.EditMode.PremiumLobbyAssetContractTests`

Expected: 새 보조 버튼과 무기 슬롯 PNG가 없어 실패한다.

- [ ] **Step 3: PixelLab 에셋 2개 생성**

보조 버튼 프롬프트:

```text
Joseon dark-fantasy mobile game secondary button, rounded rectangle, matte ink-black and deep burgundy face, restrained antique-gold bevel and corner trim, broad simple pixel shapes, no text, no icon, no white outline, no tiny ornament, transparent background, suitable for nine-slicing
```

무기 슬롯 프롬프트:

```text
compact square weapon slot frame for a Joseon dark-fantasy pixel mobile game, gently rounded corners, matte ink-black center, clean antique-gold rim that can be tinted by level, broad readable pixel shapes, no text, no icon, no white outline, transparent background, suitable for nine-slicing
```

- [ ] **Step 4: 새 한연화 원화 생성**

```text
Use case: stylized-concept
Asset type: vertical mobile game loading key art
Primary request: a close upper-body portrait of Han Yeonhwa, an unmistakably adult glamorous Korean Joseon-fantasy exorcist swordswoman, alluring confident expression and elegant sensual silhouette
Scene/backdrop: moonlit tiled palace roofs and dark mist
Style/medium: premium semi-realistic Korean fantasy game illustration
Composition/framing: face and upper torso close to camera, waist and lower body cropped, both arms held behind her back and both hands completely outside the frame, no visible fingers
Color palette: deep crimson, charcoal black, restrained antique gold
Constraints: no text, no logo, no watermark, no visible hands, no extra limbs, no white outline, no modern clothing, adult character only
```

원본과 Resources 사본은 동일한 최종 PNG를 사용한다.

- [ ] **Step 5: 임포트 설정 후 GREEN 실행**

UI 프레임은 Point, mipmap off, max 1024, border 24~32px. 한연화는 Bilinear, mipmap off, max 2048, Android ASTC 6x6을 적용한다.

- [ ] **Step 6: 에셋 묶음 커밋·푸시**

```powershell
git add -- Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_secondary_button.png Assets/JoseonHunter/Art/UI/Combat/compact_weapon_slot.png Assets/JoseonHunter/Art/Characters/Lobby/han_yeonhwa_hero.png Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png Assets/JoseonHunter/Scripts/Editor/AssetProduction/JoseonAssetPostprocessor.cs Assets/JoseonHunter/Tests/EditMode/PremiumLobbyAssetContractTests.cs
git commit -m "art: unify lobby and combat controls"
git push origin master
```

### Task 2: 픽셀 전용 로비와 단순 출전 카드

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyUiFactory.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponCatalogAsset`, primary/secondary button sprites, current active loadout
- Produces: auto-saved selected weapon, no visible preset controls, three existing bottom navigation tabs

- [ ] **Step 1: 로비 계약 RED 테스트 작성**

```csharp
Assert.That(transforms.Any(item => item.name == "Hero Art"), Is.False);
Assert.That(transforms.Any(item => item.name == "Hero Name"), Is.False);
Assert.That(transforms.Any(item => item.name == "Hero Subtitle"), Is.False);
Assert.That(transforms.Any(item => item.name == "Previous Preset"), Is.False);
Assert.That(transforms.Any(item => item.name == "Save Preset"), Is.False);
Assert.That(navigationButtons.Select(TextOf), Is.EquivalentTo(
    new[] { "무기 연구", "출전", "공통 수련" }));
```

PlayMode 테스트는 무기 이동 버튼을 한 번 눌렀을 때 `ActiveLoadout.StartingWeapon`이 즉시 변경되는지 확인한다.

- [ ] **Step 2: 로비 RED 실행**

Run filters: `LobbySceneContractTests`, `LobbyPatrolPlayModeTests`

Expected: Hero와 preset 버튼이 존재해 실패한다.

- [ ] **Step 3: Hero 계층과 장식 문구 제거**

`LobbyBootstrap.BuildShell()`에서 `Hero Viewport`, `Hero Art`, `Hero Shade`, `Hero Name`, `Hero Subtitle` 생성 코드를 제거한다. 픽셀 `Lobby Background`은 유지하고 `Stage Content` 위 영역에서 그대로 보이게 한다.

- [ ] **Step 4: 출전 카드 단순화**

`PatrolPresenter`는 `Current Weapon Icon`, `Current Weapon Name`, `Previous Weapon`, `Next Weapon`, `Record`, `Start Patrol`만 만든다. `CycleWeapon`은 즉시 현재 활성 loadout에 저장한다.

```csharp
private void CycleWeapon(int direction)
{
    selectedWeapon = WeaponAtOffset(direction);
    SaveCurrentWeapon();
    Refresh();
}
```

- [ ] **Step 5: 모든 버튼에 9-slice 프레임 적용**

`LobbySceneBuilder.AssignSprites`에서 `Start Patrol`과 `Purchase Training`에는 주요 버튼, 그 외 모든 Lobby `Button`에는 보조 버튼을 지정하고 `Image.Type.Sliced`를 설정한다.

- [ ] **Step 6: Lobby 재생성 및 GREEN 실행**

Run: `LobbySceneBuilder.BuildInBatchMode`, 이후 두 focused suite.

- [ ] **Step 7: 로비 묶음 커밋·푸시**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/Lobby Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs Assets/JoseonHunter/Tests/PlayMode/LobbyPatrolPlayModeTests.cs Assets/JoseonHunter/Scenes/Lobby.unity Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab
git commit -m "feat: simplify pixel lobby patrol"
git push origin master
```

### Task 3: 무기 연구 진행 바와 순차 해금

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/WeaponMasteryProgression.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMasteryProgressionTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponMasteryCatalog.StylesFor(weaponId)`, `WeaponCatalogAsset`
- Produces: `Selected Weapon Icon`, `Mastery Progress Fill`, exact locked feedback copy

- [ ] **Step 1: 도메인 순차 해금 RED 테스트 작성**

```csharp
[Test]
public void ThirdStyleRequiresSecondStyleUnlockEvenWithEnoughResources()
{
    var data = SaveDataV1.CreateDefaults();
    data.Coins = 9999;
    data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 9999;
    var third = WeaponMasteryCatalog.StylesFor(WeaponId.GakgungShot)[2];
    var result = new WeaponMasteryProgression(data).CanPurchase(
        WeaponId.GakgungShot, third.LegacyPathId);
    Assert.That(result.Error, Is.EqualTo(ProgressionError.InvalidSelection));
}
```

- [ ] **Step 2: 연구 UI RED 테스트 작성**

선택 아이콘 sprite가 null이 아니고, 진행 fill의 `anchorMax.x`가 `564f / 2000f`이며, 3단계 클릭 후 피드백이 정확히 `2단계 연구 완료 시 해금`인지 확인한다.

- [ ] **Step 3: 두 focused suite RED 실행**

Expected: 순차 조건과 진행 바가 없어 실패한다.

- [ ] **Step 4: 도메인 순차 조건 구현**

```csharp
var styles = WeaponMasteryCatalog.StylesFor(weaponId);
if (styles.Count == 3 && styles[2].LegacyPathId.Equals(pathId) &&
    !data.UnlockedWeaponStyles.Contains(styles[1].LegacyPathId.Value))
    return new ProgressionResult(false, ProgressionError.InvalidSelection);
```

기존에 이미 해금된 3단계는 `AlreadyUnlocked`로 처리되어 유지된다.

- [ ] **Step 5: 연구 상단과 카드 상태 구현**

상단에 86px 아이콘, 무기명, 초록색 진행 fill을 만든다. 장문 `Mastery` 문구는 제거한다. 카드 상태는 `연구 중 ({mastery:N0}/{required:N0})`, `해금 가능`, `해금 완료`, `장착 중`으로 만든다.

- [ ] **Step 6: focused GREEN 및 Lobby 재생성**

Run filters: `WeaponMasteryProgressionTests`, `WeaponResearchLobbyPlayModeTests`.

- [ ] **Step 7: 연구 묶음 커밋·푸시**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Progression/WeaponMasteryProgression.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/WeaponResearchPresenter.cs Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs Assets/JoseonHunter/Tests/EditMode/WeaponMasteryProgressionTests.cs Assets/JoseonHunter/Tests/PlayMode/WeaponResearchLobbyPlayModeTests.cs Assets/JoseonHunter/Scenes/Lobby.unity Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab
git commit -m "feat: clarify sequential weapon research"
git push origin master
```

### Task 4: 시작·출전 로딩과 로비 복귀 버그 수정

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Meta/GameSceneRouter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Meta/MetaGameSession.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/BootstrapLoadingPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/BootstrapLoadingPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/GameSceneRouterPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`

**Interfaces:**
- Produces: `GameSceneRouter.LoadBootstrap()`, `MetaGameSession.SetPendingDestination(string)`, `ConsumePendingDestination(string)`
- Guarantees: 비동기 씬 완료 시 `IsRouting == false`

- [ ] **Step 1: 파괴되는 route host 재현 테스트 작성**

Lobby의 임시 MonoBehaviour에서 `router.LoadGameplay()`를 시작해 Gameplay 전환으로 host가 파괴된 뒤에도 `router.IsRouting`이 false인지 확인한다. 이어 `router.LoadLobby()`가 실제 Lobby를 여는지 확인한다.

- [ ] **Step 2: 출전 Bootstrap 경유 RED 테스트 작성**

Lobby에서 `Start Patrol`을 호출한 뒤 활성 씬이 `Bootstrap`, 최종 씬이 `Gameplay` 순서로 바뀌고 로딩 Presenter가 최소 1.5초 유지되는지 확인한다.

- [ ] **Step 3: RED 실행과 root-cause 로그 확인**

Run filters: `GameSceneRouterPlayModeTests`, `BootstrapLoadingPlayModeTests`, `CombatHudPlayModeTests.GameplayRunResultButtonReturnsToLobby`.

- [ ] **Step 4: 라우터 완료 상태 보장**

```csharp
var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
operation.completed += _ => IsRouting = false;
while (!operation.isDone) yield return null;
IsRouting = false;
```

- [ ] **Step 5: Bootstrap 목적지 전달 구현**

`MetaGameSession`에 메모리 전용 pending destination을 추가한다. 출전은 `Gameplay`를 저장하고 `LoadBootstrap()`을 호출한다. `BootstrapLoadingPresenter.Start()`는 pending 값을 한 번 소비하고 없으면 `Lobby`를 사용한다.

- [ ] **Step 6: 로딩 최소 노출 시간 변경**

`MinimumVisibleSeconds`를 `1.5f`로 변경한다. 시작과 출전 모두 같은 한연화 Resources 스프라이트를 사용한다.

- [ ] **Step 7: 실제 승전 복귀 GREEN 실행**

Lobby → Bootstrap → Gameplay → 승전 → Lobby 경로를 테스트한다.

- [ ] **Step 8: 전환 묶음 커밋·푸시**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Meta Assets/JoseonHunter/Scripts/Presentation/UI/BootstrapLoadingPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/PatrolPresenter.cs Assets/JoseonHunter/Tests/PlayMode
git commit -m "fix: make loading routes recoverable"
git push origin master
```

### Task 5: 픽업 흡수 연출과 일시정지 메뉴

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/ExperiencePickupMotion.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/ExperiencePickupMotionTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`

**Interfaces:**
- Preserves: `StartingPickupRadius == .58f`
- Produces: top-right `Pause Button`, `Pause Bar Left`, `Pause Bar Right`

- [ ] **Step 1: 반경 보존과 motion RED 테스트 작성**

```csharp
Assert.That(controller.StartingPickupRadiusForTests, Is.EqualTo(.58f).Within(.001f));
Assert.That(ExperiencePickupMotion.SpeedAt(0f, false), Is.LessThan(4f));
Assert.That(ExperiencePickupMotion.SpeedAt(.3f, false), Is.GreaterThan(10f));
```

- [ ] **Step 2: 일시정지 UI RED 테스트 작성**

`Return Button`과 `Return Label`이 없고, 58px 이상 `Pause Button`과 두 막대가 존재하며 클릭 시 `일시정지` 제목의 메뉴가 열리는지 확인한다.

- [ ] **Step 3: RED 실행**

Run filters: `ExperiencePickupMotionTests`, relevant `CombatHudPlayModeTests`, `PortraitUiLayoutPlayModeTests`.

- [ ] **Step 4: 픽업 크기와 motion 조정**

경험치 trigger 조건 `distance <= pickupRadius`는 그대로 둔다. 일반 흡수 속도는 약 `2.2f → 13f`, 가속 시간은 `.30f`, 실제 획득 거리만 `.42f → .18f`로 줄여 같은 반경 안에서 보이는 이동을 늘린다. 자석 force collect는 기존 고속 값을 유지한다. 보물/엽전은 `.34f → .48f`, 자석은 `.18f → .50f`로 키운다.

- [ ] **Step 5: 일시정지 버튼과 메뉴 문구 구현**

상단 Vitals 오른쪽 안쪽에 둥근 정사각형 버튼을 만들고 자식 Image 두 개로 `Ⅱ` 모양을 만든다. `AbandonRunPresenter` 제목은 `일시정지`, 버튼은 `계속하기`, `로비로 돌아가기`로 바꾼다.

- [ ] **Step 6: focused GREEN 실행**

- [ ] **Step 7: 전투 조작 묶음 커밋·푸시**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/ExperiencePickupMotion.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs Assets/JoseonHunter/Scripts/Presentation/UI/AbandonRunPresenter.cs Assets/JoseonHunter/Tests
git commit -m "feat: improve pickups and pause flow"
git push origin master
```

### Task 6: compact 무기 HUD와 잠재 아이콘

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixVerticalSlicePlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponSlotView.Icon`, `WeaponSlotView.Level`, `WeaponSlotView.PotentialIds`, `WeaponPotentialVisuals.TryGet`
- Produces: square clickable slot, level-colored border, up to three potential icons

- [ ] **Step 1: compact card RED 테스트 작성**

```csharp
var rect = slot.GetComponent<RectTransform>();
Assert.That(Mathf.Abs(rect.rect.width - rect.rect.height), Is.LessThanOrEqualTo(2f));
Assert.That(slot.transform.Find("Name"), Is.Null);
Assert.That(slot.transform.Find("Legacy Path"), Is.Null);
Assert.That(slot.GetComponent<Button>(), Is.Not.Null);
```

레벨 1~5 입력에 대해 `Level Border` 색이 서로 다른지, potential id가 있는 경우 아이콘이 활성화되는지 확인한다.

- [ ] **Step 2: RED 실행**

Run filters: `WeaponAffixVerticalSlicePlayModeTests`, `PortraitUiLayoutPlayModeTests`.

- [ ] **Step 3: compact slot 구현**

`Name`, `Level`, `Totals` 텍스트를 제거하고 84×84 기본 크기의 `Compact Weapon Slot`을 만든다. 4열 그리드로 하단 중앙에 배치하며 아이콘은 58×58로 표시한다.

```csharp
private static Color LevelBorder(int level) => level switch
{
    <= 1 => new Color(.72f, .68f, .58f),
    2 => new Color(.22f, .72f, .60f),
    3 => new Color(.25f, .48f, .90f),
    4 => new Color(.63f, .36f, .82f),
    _ => new Color(.90f, .65f, .20f)
};
```

- [ ] **Step 4: 잠재 옵션 아이콘 연결**

각 `PotentialIds`를 `WeaponPotentialVisuals.TryGet`으로 해석하고 카드 하단의 18×18 셀에 표시한다. 없는 셀은 비활성화한다. 카드 click은 기존 `WeaponSelected` 이벤트를 유지한다.

- [ ] **Step 5: focused GREEN 실행**

- [ ] **Step 6: HUD 묶음 커밋·푸시**

```powershell
git add -- Assets/JoseonHunter/Scripts/Presentation/UI/WeaponRackPresenter.cs Assets/JoseonHunter/Tests/PlayMode/WeaponAffixVerticalSlicePlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/PortraitUiLayoutPlayModeTests.cs
git commit -m "feat: compact the combat weapon rack"
git push origin master
```

### Task 7: 전체 빌드·검증·모바일 캡처

**Files:**
- Modify: `Docs/Verification/2026-08-04-lobby-research-gameplay-clarity.md`
- Generate: `Artifacts/LobbyClarity/*.png`
- Generate: `Artifacts/BootstrapLoadingClarity/*.png`
- Generate: `Artifacts/GameplayClarity/*.png`

**Interfaces:**
- Consumes: scene builders, Unity Test Runner, capture helpers
- Produces: mobile-visible GitHub raw PNG links and verification evidence

- [ ] **Step 1: Lobby와 Bootstrap authored asset 재생성**

Run `LobbySceneBuilder.BuildInBatchMode` and `BootstrapLoadingBuilder.BuildInBatchMode` sequentially.

- [ ] **Step 2: 전체 EditMode 실행**

Expected: 실패 0, 컴파일 오류 0.

- [ ] **Step 3: 전체 PlayMode 실행**

Expected: 실패 0, Lobby → Bootstrap → Gameplay → Lobby 경로 통과.

- [ ] **Step 4: 모바일 캡처 생성**

720×1280과 1080×2340에서 다음을 캡처한다.

- 픽셀 로비 출전 화면
- 무기 연구 564/2,000 및 3단계 잠금 화면
- 공통 수련 버튼 화면
- 새 한연화 시작/출전 로딩
- 인게임 일시정지 버튼
- compact 무기 슬롯 2개 이상

- [ ] **Step 5: 캡처 육안 검수와 문서 작성**

버튼 9-slice, 한글 글리프, safe area, 무기 아이콘, 연구 progress, 잠금 문구, pickup 범위 불변, 손 미노출을 기록한다.

- [ ] **Step 6: 증거 커밋·푸시**

```powershell
git add -- Docs/Verification/2026-08-04-lobby-research-gameplay-clarity.md
git add -f -- Artifacts/LobbyClarity Artifacts/BootstrapLoadingClarity Artifacts/GameplayClarity
git commit -m "test: verify lobby and gameplay clarity pass"
git push origin master
```
