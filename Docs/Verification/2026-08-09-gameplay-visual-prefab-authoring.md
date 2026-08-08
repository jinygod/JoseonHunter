# 게임플레이 비주얼 Prefab 전환 검증 보고서

## 판정

**Ready with limitations**

- 이번 변경으로 생긴 컴파일·EditMode·PlayMode 회귀는 확인되지 않았다.
- 전체 PlayMode 330건 중 1건은 작업 전부터 있던 로비 난이도 카드 스킨 기대값 불일치이며, 이번 게임플레이 Prefab 범위 밖이라 보존했다.
- Android 개발 APK 빌드는 성공했지만 실제 Android 기기 설치·터치 조작은 실행하지 않았다.

기준 커밋은 `50a0ea6`, Unity는 `6000.5.5f1`이다. 작업 전 기준은 EditMode 924/924, PlayMode 320/321이었고 PlayMode의 유일한 실패는 현재와 같은 `LobbyPatrolPlayModeTests.PatrolUsesStageArrowsPremiumCardsAndHeroFrame`이었다.

## 1. 변경한 파일

### 런타임

- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/GameplayVisualPrefabLibrary.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualView.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/WorldBarView.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/PickupVisualView.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

### Editor 도구와 Scene 연결

- `Assets/JoseonHunter/Scripts/Editor/Scenes/GameplayVisualPrefabBuilder.cs`
- `Assets/JoseonHunter/Scripts/Editor/Scenes/GameplayVisualPreviewBuilder.cs`
- `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- `Assets/JoseonHunter/Scenes/Gameplay.unity`
- `Assets/JoseonHunter/Scenes/GameplayVisualPreview.unity`

### 테스트와 문서

- `Assets/JoseonHunter/Tests/EditMode/GameplayVisualPrefabContractTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/GameplayVisualPrefabPlayModeTests.cs`
- `Docs/GameplayPrefabAuthoring.md`
- `Docs/Superpowers/2026-08-09-gameplay-visual-prefab-authoring-design.md`
- `Docs/Superpowers/Plans/2026-08-09-gameplay-visual-prefab-authoring.md`
- 이 보고서

Unity가 생성한 각 스크립트·Scene·Prefab·폴더의 `.meta`도 함께 포함한다.

## 2. 새로 만든 Prefab과 Library

- `Assets/JoseonHunter/Prefabs/Gameplay/PlayerVisual.prefab`
- `Assets/JoseonHunter/Prefabs/Gameplay/EnemyVisual.prefab`
- `Assets/JoseonHunter/Prefabs/Gameplay/WorldHealthBar.prefab`
- `Assets/JoseonHunter/Prefabs/Gameplay/WorldShieldBar.prefab`
- `Assets/JoseonHunter/Prefabs/Gameplay/ExperiencePickup.prefab`
- `Assets/JoseonHunter/Prefabs/Gameplay/YeopjeonPickup.prefab`
- `Assets/JoseonHunter/Prefabs/Gameplay/MagnetPickup.prefab`
- `Assets/JoseonHunter/Prefabs/Gameplay/GameplayAuthoringPreview.prefab`
- `Assets/JoseonHunter/Resources/Gameplay/GameplayVisualPrefabLibrary.asset`
- `Assets/JoseonHunter/Resources/Gameplay/GameplayAuthoringWhite.asset`

## 3. Prefab Hierarchy

```text
PlayerVisual
├─ Soft Shadow
├─ Silhouette Outline
├─ Player Aura
├─ Visual Pivot (SpriteRenderer)
└─ HealthBarAnchor

EnemyVisual
├─ Soft Shadow
├─ Silhouette Outline
├─ Visual Pivot (SpriteRenderer)
├─ HealthBarAnchor
└─ ShieldBarAnchor

WorldHealthBar / WorldShieldBar
├─ Background (SpriteRenderer)
└─ Fill (SpriteRenderer)

ExperiencePickup
├─ TrailRenderer (Root)
└─ Visual (SpriteRenderer)

YeopjeonPickup / MagnetPickup
└─ Visual (SpriteRenderer)
```

## 4. Prefab 참조 방식

`GameplayVisualPrefabLibrary.asset`이 일곱 production Prefab을 참조한다. `Gameplay.unity`의 `FirstPlayableController.gameplayVisualPrefabs`에 이 Library가 직렬화되어 있고, 테스트처럼 Controller를 코드로 직접 만드는 경우에는 `Resources/Gameplay/GameplayVisualPrefabLibrary`에서 같은 Library를 자동 해석한다. 누락·잘못된 개별 Prefab은 Editor/Development Build에서 한 번만 명확히 경고하고 기존 코드 생성 경로로 안전하게 돌아간다.

## 5. FirstPlayableController 변경 지점

- `ResolveGameplayVisualPrefabs`, `WarnMissingVisualPrefabOnce` 추가
- `CreateCombatantObject`: Player/Enemy Prefab `Instantiate` 후 `CombatantVisualRig.Bind`
- `CreateHealthBar`, `CreateShieldBar`, `CreateWorldBar`: authored Anchor와 WorldBar Prefab 사용
- `UpdateBarFill`: `WorldBarView`의 authored Fill 높이·오프셋을 보존하며 X 비율만 변경
- `CreatePickupObject`, `SpawnPickup`, `UpdateExperiencePickupVisual`: 종류별 Pickup Prefab과 기존 Pool을 연결

## 6. 더 이상 코드로 처음부터 조립하지 않는 시각 요소

- 플레이어의 Body/Shadow/Outline/Aura 계층
- 일반·특수·정예·중간보스·최종보스가 공유하는 Enemy 시각 계층
- 체력바와 보호막 바의 Background/Fill
- 경험치·엽전·자석의 기본 Visual 계층과 경험치 TrailRenderer

## 7. 계속 런타임에서 생성되는 요소

적·픽업·바의 **인스턴스 수와 생명주기**는 게임 진행에 따라 달라지므로 계속 런타임에 생성·풀링된다. 다만 외형은 이제 Prefab을 `Instantiate`한다. `RuntimeObjects`, 투사체, 보물상자, 경험치 흡수 플래시, 공격·보스 경고·필드/위험물 Presenter처럼 이번 최소 대상이 아닌 동적 효과는 기존 생성 방식을 유지했다. 전투 규칙과 Pool 의미를 바꾸지 않기 위한 선택이다.

## 8. Editor에서 안전하게 수정할 항목

- Shadow, Outline, Aura의 위치와 기본 크기
- `HealthBarAnchor`, `ShieldBarAnchor` 위치
- WorldBar의 Background 크기, Fill의 100% 기준 너비·높이·위치
- `PickupVisualView.Base Scale`
- 작은 범위의 Visual Pivot 오프셋

## 9. 건드리지 말아야 할 항목

- `CombatantVisualView`, `WorldBarView`, `PickupVisualView` 컴포넌트와 직렬화 참조
- 필수 자식 이름·계층, 중복 SpriteRenderer
- 경험치 Root의 TrailRenderer
- `GameplayVisualPrefabLibrary`의 production 참조
- `FirstPlayableController`와 전투/충돌/저장 스크립트
- 큰 폭의 Root 또는 Visual Pivot Scale 변경. 픽셀 충돌과 표시 크기를 함께 확인해야 한다.

## 10. Gameplay Preview 여는 방법

Unity 상단 메뉴에서 `JoseonHunter > Gameplay Editing > Open Visual Preview`를 선택한다. 직접 열 경로는 `Assets/JoseonHunter/Scenes/GameplayVisualPreview.unity`이다. 이 Scene은 Build Settings에서 제외되며 Controller, MetaGameSession, 저장·전투 흐름을 포함하지 않는다.

Production Prefab 수정 뒤 Preview Override가 꼬이면 `JoseonHunter > Gameplay Editing > Rebuild Visual Preview From Production Prefabs`를 실행한다. 이 메뉴는 Preview Prefab/Scene만 다시 만들고 일곱 production Prefab은 덮어쓰지 않는다.

## 11. 플레이어 크기와 체력바 위치 수정

`PlayerVisual.prefab`을 더블 클릭해 Prefab Mode에서 `Visual Pivot`의 Scale을 소폭 조정하고, `HealthBarAnchor`의 Local Position으로 바 위치를 조정한다. `Ctrl+S` 후 Preview와 실제 Gameplay Play Mode에서 공격·피격 위치를 확인한다. Play Mode 중 수정한 값은 종료하면 사라지므로 저장할 값은 Prefab Mode에서 다시 입력한다.

## 12. 적과 픽업 Sprite 교체

Prefab의 Sprite는 Preview 기본 이미지이고 실제 전투에서는 적 종류·픽업 종류·MotionLibrary/카탈로그 Sprite가 런타임에 주입된다. 따라서 공통 위치·크기는 Prefab에서 바꾸되 실제 적/플레이어 그림 교체는 연결된 Sprite 카탈로그와 MotionLibrary를 함께 바꿔야 한다. 픽업도 같은 방식이며, 이번 작업에서 안전하게 노출한 직접 조정값은 `PickupVisualView.Base Scale`이다.

## 13. 실행 명령

```powershell
Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.GameplayVisualPrefabContractTests
Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.GameplayVisualPrefabPlayModeTests
Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode
Unity.exe -batchmode -nographics -quit -projectPath D:\UnityProjects\JoseonHunter -executeMethod JoseonHunter.Editor.Build.AndroidDevelopmentBuild.Build -logFile D:\UnityProjects\JoseonHunter\Logs\android-development-build.log
```

Android 명령은 공식 `Tools/Unity/Build-AndroidDevelopment.ps1`과 같은 Build 메서드·`GRADLE_USER_HOME=C:\jh-gradle`을 사용했고, 작업 중 CPU 폭주를 피하려고 Unity를 BelowNormal/논리 코어 8개로 제한해 직접 실행했다.

Preview 재생성은 다음 Editor batch entry point로도 검증했다.

```text
JoseonHunter.Editor.Scenes.GameplayVisualPreviewBuilder.RebuildVisualPreviewFromProductionPrefabsBatch
```

## 14. 실제 결과

- Prefab/Scene EditMode 계약: 15/15 PASS
- Prefab 런타임 PlayMode: 9/9 PASS
- 전체 EditMode: 939/939 PASS
- 전체 PlayMode: 329/330 PASS
  - 기존 실패 1: `LobbyPatrolPlayModeTests.PatrolUsesStageArrowsPremiumCardsAndHeroFrame`
  - 작업 전과 동일: expected `difficulty_selected`, actual `difficulty_idle`
- Preview 재빌드 전후 production Prefab SHA-256 변경 수: 0/7
- Android Development APK: SUCCESS
  - 경로: `Builds/Android/JoseonHunter-development.apk`
  - 크기: 173,805,241 bytes
  - SHA-256: `F1E15EB85C2D48ED8547221F99BBBE625EDA1F58C904ACA1DAB024E0D6E64805`

검증 자료는 `Artifacts/GameplayPrefabAuthoring/`의 baseline, RED, GREEN, full XML/log에 있다.

## 15. 남은 제한사항

- 실제 `Gameplay.unity`는 의도대로 Main Camera, FirstPlayable, EventSystem 골격만 저장하고 동적 개체는 Play 중 생성한다. 전체 외형 편집은 별도 Preview에서 한다.
- Project 창의 SpriteRenderer 기본 Sprite는 런타임 카탈로그 주입으로 교체될 수 있다.
- Android 실기기 설치·성능·터치 조작은 이번 검증에 포함하지 않았다.
- 로비 스킨 테스트 1건은 기존 실패이며 별도 로비 작업에서 정리해야 한다.

## 16. 다음 Prefab화 후보

1. 보물상자와 경험치 흡수 플래시
2. 일반 투사체·보스 투사체의 공통 Visual Shell
3. 보스 경고 표식과 Stage Hazard 시각 Root
4. 금줄·무기별 Presenter의 반복 생성 레이어
5. 적 종류별 공통 EnemyVisual Variant 또는 Presentation Library
