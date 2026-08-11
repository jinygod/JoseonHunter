# 로비 UI Inspector 편집 가이드

이 문서는 코드가 아닌 Unity Inspector에서 현재 authored 로비 UI를 안전하게 수정하기 위한 기준이다. 대상은 `Assets/JoseonHunter/Scenes/Lobby.unity`와 `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/`의 모듈 프리팹이다.

## 편집 위치와 페이지 순서

- 씬: `Assets/JoseonHunter/Scenes/Lobby.unity`
- 공통 모듈: `Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/`
- 주요 경로: `Lobby Canvas/Safe Area/Common Header`, `Home Page`, `Training Page`, `Patrol Page`, `Research Page`, `Settings Overlay`
- Inspector 편집은 `JoseonHunter/Lobby Editing/Open Lobby Scene` 또는 `Open Lobby Modules`로 연다.

씬을 연 뒤 현재 활성 상태를 먼저 기록하고, 편집할 페이지의 GameObject만 임시로 활성화한다. 권장 확인 순서는 Home → Training → Patrol → Research이며, 한 번에 하나의 페이지와 필요한 공통 헤더만 켠다. 저장 전에는 작업 시작 때 기록한 활성 상태로 되돌린다. 런타임은 진입 시 Common Header와 Home을 정상 상태로 복원한다. Play Mode에서는 편집하지 않는다. 런타임 presenter가 텍스트, 아이콘, 활성 상태를 갱신하고 Play Mode 종료 시 변경이 사라지거나 dirty 상태가 섞일 수 있다.

## HomeMenuCard 역할과 클릭 표면

`HomeMenuCard.prefab`의 기본 구조는 `HomeMenuCard/Button/Body`와 카드 직속 `Icon`, `Title`, 선택적 `Description`이다.

- `Button`: 전환 이벤트를 소유하는 `UnityEngine.UI.Button`.
- `Body`: 카드의 입력 표면이자 현재 `Button.targetGraphic`이다. 현재 구현은 `LobbyMenuCardView.inputSurface`가 이 Image를 명시 참조한다.
- `Icon`: 장식 Sprite. `raycastTarget = false`.
- `Title`: 장식 TMP 텍스트. `raycastTarget = false`.
- `Description`: 선택적 장식 텍스트. 없애도 필수 binding은 깨지지 않는다.

입력은 Body 하나만 담당한다. Body는 활성화, `raycastTarget = true`, Button의 `targetGraphic` 지정 상태를 유지한다. Icon/Title/Description 및 프레임·장식 Image는 `raycastTarget = false`를 권장한다. Body의 색상·Sprite를 시각적으로 바꾸더라도 `Button.targetGraphic`과 `inputSurface` 참조를 끊지 않는다.

## 유지해야 할 serialized 참조와 컴포넌트

카드의 `LobbyMenuCardView`에서 `button`, `title`, `icon`, `inputSurface` 참조를 유지한다. `description`은 선택 사항이다. `Button` GameObject의 Button·RectTransform과 `Button/Body`의 Image·CanvasRenderer를 유지하며, Body가 원하는 클릭 영역을 덮게 배치한다. `LobbyHomeView`는 Training/Patrol/Research 카드 세 개를, `LobbyRootView`는 Home과 Navigation을 참조한다. 누락된 참조를 임의로 `GetComponent`나 이름 검색으로 대체하지 않는다.

## Sprite와 RectTransform 편집

Sprite는 해당 Image의 `Source Image`만 바꾸고 `Type`, `Preserve Aspect`, 색상/알파를 함께 확인한다. 픽셀 아트는 import의 point filtering과 원본 비율을 보존한다. RectTransform은 기존 anchor, pivot, `sizeDelta`, sibling order를 먼저 기록한 뒤 의도한 값만 변경한다. 카드 Body의 크기와 여백은 Inspector에서 자유롭게 바꿀 수 있지만, 실제로 눌려야 하는 영역을 덮어야 한다. `Current Deployment`는 이 작업에서 편집하지 않는다.

## 새 카드 또는 페이지 추가

1. `HomeMenuCard.prefab`를 복제하고 `Button/Body`, `Icon`, `Title`의 역할을 유지한다.
2. `LobbyHomeView`에 새 카드를 직렬화하고 `LobbyNavigationPresenter`에 페이지 GameObject와 메뉴/뒤로가기 Button을 명시 연결한다.
3. 새 페이지는 `PageHeader`와 기존 모듈 프리팹을 사용하고, 한 번에 하나만 활성화되도록 Navigation에 연결한다.
4. 코드에는 데이터 표시·상태·이벤트만 둔다. 고정 색상, 폰트, RectTransform, Sprite 장착을 presenter에 추가하지 않는다.
5. EditMode 계약 테스트와 해당 Lobby PlayMode 테스트를 먼저 실행한다.

## 자동 생성 도구의 주의점

- `JoseonHunter/Setup/Build Lobby` → `LobbySceneBuilder.Build()`: 씬 hierarchy를 수리/재구성하고 저장한다. dirty 씬 보호가 있지만 수동 override를 덮을 수 있으므로 checkpoint 후에만 사용한다.
- `JoseonHunter/Setup/Create Or Validate Lobby Modules` → `LobbyModulePrefabBuilder.CreateOrValidateProductionModules()`: 없는 프리팹을 만들고 기존 프리팹을 검증한다.
- `JoseonHunter/Setup/Rebuild Lobby Modules` → `RebuildProductionModules()`: 모든 모듈을 코드로 다시 만들어 저장한다. 수동 prefab override가 사라질 수 있으므로 현재 편집 후 실행 금지.
- `JoseonHunter/Lobby Editing/Rebuild Authored Lobby`도 `LobbySceneBuilder.Build()`를 호출하므로 동일한 위험이 있다.
- `JoseonHunter/Lobby Editing/Validate Authored Lobby`는 읽기 전용 검증용으로 우선 사용한다.

## Overrides와 저장

Prefab 인스턴스의 Inspector 변경은 Overrides 창에서 필요한 항목만 `Apply`한다. Apply 전에 대상 프리팹과 property 목록을 확인하고, 씬 전용 변경은 Apply하지 말고 `Lobby.unity`에 저장한다. 씬을 저장하기 전 Hierarchy 활성 상태, Button targetGraphic, Body raycastTarget, missing reference를 다시 확인한다. 변경 파일은 의도한 씬/프리팹/문서만 남기고 `git diff --check`로 공백 오류를 확인한다.
