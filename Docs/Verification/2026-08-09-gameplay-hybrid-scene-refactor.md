# Gameplay 하이브리드 씬 이관 검증 보고서

## 판정

**Ready with one pre-existing lobby limitation**

- 전투의 안정적인 시작 구성은 이제 `Gameplay.unity`에 직렬화되어 Scene View에서 직접 확인하고 편집할 수 있다.
- 적·투사체·획득물처럼 수와 생명주기가 계속 바뀌는 요소는 기존처럼 런타임 생성·풀링을 유지한다.
- 하이브리드 이관과 관련된 EditMode·PlayMode 계약은 모두 통과했다.
- 전체 PlayMode의 유일한 실패는 작업 전부터 있던 로비 난이도 카드 스킨 기대값 불일치이며 이번 전투 씬 범위와 무관하다.
- Android ARM64 개발 APK 빌드는 성공했지만 실제 Android 기기 설치·터치·장시간 성능 측정은 이번 검증에 포함하지 않았다.

검증 코드 기준은 `e1bf285`, Unity는 `6000.5.5f1`이다.

## 1. 최종 Scene 경계

```text
Gameplay
├─ Main Camera                         # Scene/Inspector 소유
├─ FirstPlayable
│  ├─ FlatField                       # 안정적인 전장 루트
│  │  ├─ Authoring Preview            # 편집 시 보이는 3×3 미리보기
│  │  └─ Runtime Battlefield          # 실행 중 스테이지별 생성
│  ├─ RuntimeObjects
│  │  └─ Han Yeonhwa                  # 연결된 PlayerVisual Prefab
│  ├─ RuntimeSystems                  # 한 판 단위 Presenter/Pool
│  └─ Spawn Guides                    # Scene View 안내
├─ First Playable UI                  # 안정적인 UI 루트
└─ EventSystem
```

`Main Camera`, `FlatField`, `RuntimeObjects`, `RuntimeSystems`, `Han Yeonhwa`, `First Playable UI`는 재시작 뒤에도 같은 인스턴스를 유지한다. 적, 투사체, 장판, 보물, 경험치·엽전·자석과 실행 전용 Presenter는 런타임 자식으로 생성·정리된다.

## 2. Scene View에서 가능한 작업

- `FirstPlayable/RuntimeObjects/Han Yeonhwa`를 선택해 시작 위치와 크기를 편집하고 Scene에 저장한다.
- `Main Camera`의 Orthographic Size, 시작 위치, 배경색과 Clear Flags를 Inspector에서 편집한다.
- `FlatField/Authoring Preview`로 플레이 전 필드 구도를 확인한다.
- `PlayerVisual`, `EnemyVisual`, `WorldHealthBar`, `WorldShieldBar`는 Prefab Mode에서 외형과 Anchor를 수정한다.
- 캐릭터 이동, 카메라 추적, 웨이브·투사체·획득물 시뮬레이션은 Play Mode에서 확인한다.

자세한 초보자용 절차는 `Docs/GameplaySceneAuthoring.md`에 정리했다.

## 3. 주요 리팩터링

- `GameplaySceneComposition`: Scene 소유 카메라·필드·런타임 루트·플레이어·UI 참조와 시작 포즈 복원
- `GameplayBattlefieldHost`: 전장 Presenter 생성·교체·정리를 Controller에서 분리
- `GameplayVisualFactory`: 플레이어·적·픽업·월드 바 생성/바인딩과 안전한 legacy fallback을 분리
- `FirstPlayableController`: 전투 규칙·밸런스·저장·공개 테스트 API를 보존하면서 위 세 구성요소에 위임
- `FirstPlayableSceneGenerator`: 파괴적 전체 재생성 대신 누락 구성만 생성·검증하고, 저장하지 않은 Scene 변경은 거부

기존 authored 카메라의 위치·회전·투영·FOV·크기·배경·Clear Flags는 검증 시 덮어쓰지 않는다. 새 Camera를 실제로 만든 경우에만 portrait 기본값을 적용한다. 잘못된 외부 `sceneComposition` 참조는 Scene을 변경하기 전에 즉시 거부한다.

`sceneComposition`이 아예 없는 코드 기반 테스트/legacy 진입만 기존 생성 경로를 사용한다. 구성 컴포넌트가 존재하지만 참조가 불완전하거나 reset 루트 배치가 위험한 경우에는 카메라·플레이어·필드를 추측해서 중복 생성하지 않고, 한 번의 명확한 경고와 함께 실행을 안전하게 멈춘다. Editor 복구 메뉴로 구성을 다시 검증한 뒤 실행할 수 있다.

## 4. 자동 검증 결과

### 집중 계약

- `GameplaySceneCompositionTests`: 7/7 PASS
- `GameplayBattlefieldHostTests`: 3/3 PASS
- `GameplaySceneAuthoringContractTests`: 19/19 PASS
- `SceneScaffoldTests`: 8/8 PASS
- `GameplayVisualPrefabContractTests`: 15/15 PASS
- `GameplayHybridSceneOwnershipPlayModeTests`: 12/12 PASS
- `GameplayVisualPrefabPlayModeTests`: 11/11 PASS
- `FirstPlayablePresentationPlayModeTests`: 5/5 PASS
- `FirstPlayablePickupRangePlayModeTests`: 5/5 PASS
- `StagePacingPlayModeTests`: 6/6 PASS

### 전체 Suite

- 전체 EditMode: 968/968 PASS
- 전체 PlayMode: 343/344 PASS
  - 이관 관련 실패: 0
  - 기존 로비 실패: `LobbyPatrolPlayModeTests.PatrolUsesStageArrowsPremiumCardsAndHeroFrame`
  - expected `difficulty_selected`, actual `difficulty_idle`

XML과 로그는 로컬 `Artifacts/GameplayHybridScene/`에 보관했다.

## 5. 직렬화·Scene 감사

- `Gameplay.unity`의 `m_Script: {fileID: 0}`: 0건
- `Main Camera.orthographic size`: 18
- `FirstPlayableController.sceneComposition`: 같은 GameObject의 `GameplaySceneComposition` fileID `1589757388`
- Build Settings 활성 Scene 순서: `Bootstrap` → `Lobby` → `Gameplay`
- `GameplayVisualPreview`는 Build Settings에서 제외
- Scene 생성기 반복 검증 시 기존 Player Prefab 연결·Transform·카메라 설정 보존
- 더티 Gameplay Scene 및 잘못된 구성 참조는 변경 전에 거부

## 6. Android 개발 빌드

- 결과: SUCCESS
- 대상: Android ARM64 / IL2CPP
- 패키지: `com.jinygod.joseonhunter`
- APK: `Builds/Android/JoseonHunter-development.apk`
- 크기: 173,854,534 bytes
- SHA-256: `6495F266712DFB97E097BCBBD06418E67D0FAD69814A6C5888A894274855F69A`
- 빌드 로그: `Logs/android-development-build.log`
- Unity 로그 결과: `Build Finished, Result: Success.`

APK는 위 검증 코드 기준의 런타임·Scene을 포함해 다시 생성했다.

CPU 폭주를 피하기 위해 Unity는 순차 실행했고 BelowNormal 우선순위, 논리 코어 8개 범위로 제한했다.

## 7. 남은 제한사항

- 실제 Android 기기 설치·터치 조작·발열·메모리 장시간 측정은 별도 실기기 검증이 필요하다.
- 적·투사체·픽업이 Scene에 미리 보이지 않는 것은 의도된 런타임 경계다. 외형은 Prefab Mode/Visual Preview에서, 실제 다수 생성은 Play Mode에서 확인한다.
- 로비 난이도 선택 카드 스킨 테스트 1건은 이번 이관 전부터 있던 별도 로비 작업의 실패로 유지했다.
