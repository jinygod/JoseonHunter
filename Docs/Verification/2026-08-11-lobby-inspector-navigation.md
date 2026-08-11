# 2026-08-11 로비 Inspector 클릭·Navigation 검증 기록

## 범위

대상은 `Lobby.unity`, HomeMenuCard 모듈, Navigation presenter 및 관련 editor builder다. 시작 시 serialized YAML과 first-party 코드를 읽기 전용으로 감사했고, 이후 수동 씬 상태는 체크포인트로 보존한 채 HomeMenuCard의 기능 참조와 런타임 코드·테스트·문서만 변경했다.

## 클릭 원인과 출전 방식

기존 세 카드의 Inspector `On Click()`이 모두 비어 있던 것은 정상이다. `LobbyNavigationPresenter.Awake/Bind()`가 serialized Button에 런타임 listener를 등록하고, Patrol 카드도 이 경로로 작동했다. 실제 장애는 Training과 Research 프리팹 인스턴스에서 Button의 기존 `targetGraphic`이던 루트 Image만 비활성화된 override였다. Button은 enabled/interactable이어도 GraphicRaycaster가 맞힐 Graphic이 없어 포인터 클릭이 listener까지 도달하지 않았다. Patrol 인스턴스에는 이 비활성 override가 없어 계속 작동했다.

수정 후 카드 클릭은 `LobbyMenuCardView.inputSurface`가 기존 `Button/Body` Image를 명시 참조하는 방식이다. `HomeMenuCard.prefab`에서 Body는 Button 자식이며 `raycastTarget = true`, `Button.targetGraphic = Body`다. Body의 기존 색상·알파·Sprite·RectTransform은 바꾸지 않았다. Icon과 Title은 장식 Image/TMP라 `raycastTarget = false`다. Home의 Training/Patrol/Research 카드 모두 동일한 serialized Button/Navigation 경계를 사용한다.

`LobbyNavigationPresenter`는 `OpenTrainingPage`, `OpenPatrolPage`, `OpenResearchPage`, `ReturnToHomePage`의 명명된 메서드와 소유 `UnityAction`만 등록한다. 재바인딩 전에 자기 listener만 제거하고 `OnDestroy`에서도 해제하므로 Inspector 또는 외부 listener를 지우지 않는다. 페이지·메뉴·뒤로가기 참조가 빠졌거나 중복되면 hierarchy를 바꾸기 전에 명확한 예외를 낸다.

## Current Deployment 감사

정확한 경로는 `Safe Area/Home Page/Current Deployment/Starting Weapon Icon`이다. `Lobby.unity`의 `Current Deployment` Image는 `Source Image = content_backplate`(GUID `eca9541543dc88545b4389bdf6841493`, `Assets/JoseonHunter/Resources/UI/PremiumJoseon/content_backplate.png`), 흰색 RGBA `(1,1,1,1)`, alpha 1, sliced Image, `raycastTarget = true`다. 이 이미지는 어두운 금색 외곽 프레임이며 흰 세로 사각형의 원인이 아니다.

`Starting Weapon Icon`은 Image `Source Image = None`, 색상 흰색 RGBA `(1,1,1,1)`, alpha 1, `raycastTarget = true`, 활성 상태다. RectTransform은 Current Deployment 안에서 대략 x `.66-.75`, y `.24-.76`다. Unity Image의 null Sprite 기본 quad가 초기화 전 흰 세로 사각형처럼 보일 수 있다. `LobbyHomePresenter.Refresh()`(파일 `Scripts/Presentation/UI/Lobby/LobbyHomePresenter.cs`, 40–56행)가 catalog 아이콘을 지정하고 `enabled = icon != null`을 설정한다. 따라서 이 슬롯은 데이터가 있으면 기능성 무기 아이콘, 없으면 비활성화되어야 하는 placeholder다. `LobbyHomePlayModeTests`는 null Sprite 상태에서 `enabled == false`를 기대한다. 본 작업에서 오브젝트·색상·알파·활성·geometry는 변경하지 않는다.

## 자동 생성 도구와 위험

`LobbySceneBuilder`(`Scripts/Editor/Scenes/LobbySceneBuilder.cs`)는 `JoseonHunter/Setup/Build Lobby`, `Lobby Editing/Rebuild Authored Lobby`로 호출되며 Home summary를 이름으로 찾고, UI를 생성·재부모화·RectTransform 설정 후 씬을 저장한다. `LobbyModulePrefabBuilder`는 `JoseonHunter/Setup/Create Or Validate Lobby Modules`와 `Rebuild Lobby Modules`를 제공한다. 전자는 누락 프리팹을 만들고, 후자는 모든 모듈을 코드로 재생성해 수동 override를 덮을 수 있다. `LobbyEditingTools`의 Open/Validate 메뉴는 각각 씬·모듈을 열거나 계약을 검증하며, Validate를 먼저 실행하는 것이 안전하다.

## UI 패턴 3등급

### 1등급: 현재 로비의 안전한 소규모 확인

- HomeMenuCard의 Body만 입력 표면으로 유지하고 장식 raycast를 끈다.
- `inputSurface`, `Button.targetGraphic`, 페이지/카드 serialized 참조가 유지되는지 Inspector에서 확인한다.
- Current Deployment의 null 아이콘은 placeholder 동작을 확인하되 씬 값을 임의 수정하지 않는다.

### 2등급: 후속 리팩터링

- `PatrolPresenter.cs`의 `Transform.Find("Weapon Selection Panel/Weapon Grid/Weapon Option ...")`와 `Find("Weapon Option Icon")` 문자열 경로를 typed/serialized binding으로 교체한다.
- 300행 규모의 Patrol presenter와 200행 규모의 Research presenter에서 navigation, data rendering, listener lifetime을 분리한다.
- `LobbySceneBuilder`의 `Find("Current Deployment")` 이름 결합을 명시 참조 또는 계약 검증으로 보강한다.

### 3등급: 지금 건드리지 않을 위험 영역

- `LobbySceneBuilder`의 전체 hierarchy 재구성 및 `DestroyNamedDescendants` 정리 로직.
- `LobbyModulePrefabBuilder.RebuildProductionModules()`와 runtime UI factory의 일괄 이전.
- 씬/프리팹 전체의 RectTransform·Sprite·font·alpha를 재생성하는 변경.

## 권장 검증 순서

변경이 필요할 때는 `LobbyModulePrefabContractTests`, `LobbySceneContractTests`, `LobbyNavigationPlayModeTests`, `LobbyHomePlayModeTests`, `LobbyPatrolPlayModeTests`를 순서대로 선택한다. 전체 테스트나 웹/Unity 빌드는 중복 실행하지 않는다. 수동 확인 시 Home → Training → Home → Patrol(Start) → Home → Research 순으로 이동하고, 각 단계에서 정확히 한 페이지만 활성인지, Body 클릭이 동작하는지, 장식이 입력을 가로채지 않는지 확인한다.

## 2026-08-12 최종 검증 증거

- 수동 UI 체크포인트: `582b8a2 chore: checkpoint manual lobby ui edits` (`Lobby.unity`만 포함).
- 클릭·Navigation 기능 수정: `9ea6a45 fix: restore authored lobby menu clicks`.
- Inspector 디자인 소유권·문서: `7c9e322 refactor: separate authored lobby ui from behavior`.
- 체크포인트 이후 `git diff 582b8a2 -- Assets/JoseonHunter/Scenes/Lobby.unity`: 변경 없음.
- focused: Module 26/26, Scene 1/1, Modular Scene 8/8, Navigation 12/12, Navigation Structure 3/3, Home 3/3, Patrol 14/14, Training 8/8, Research 10/10, Authored Design Ownership 2/2.
- 전체 EditMode: 1,015/1,015 passed, failed/skipped/inconclusive 0.
- 전체 PlayMode: 370/370 passed, failed/skipped/inconclusive 0.
- 최신 `Logs/editmode.log`, `Logs/playmode.log`: 새 `NullReferenceException`, `MissingReferenceException`, `error CS`, missing script 없음.
- `Lobby.unity` 및 `HomeMenuCard.prefab`: `m_Script: {fileID: 0}` 없음.
- 전체 검증 동안 Lobby 전체 rebuild/menu generator는 실행하지 않았고 관련 없는 art/meta/font/capture 변경은 스테이징 대상에서 제외한다.
