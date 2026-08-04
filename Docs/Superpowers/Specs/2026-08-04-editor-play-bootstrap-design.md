# Unity Editor Play 시작 흐름 설계

## 목적

Unity 에디터에서 어떤 씬을 열어 둔 상태로 Play를 눌러도 실제 게임과 동일하게 `Bootstrap → Lobby → Gameplay` 순서로 진입한다.

## 현재 문제

빌드에서는 Build Settings의 첫 번째 활성 씬인 `Bootstrap`부터 실행되지만, Unity 에디터의 일반 Play는 현재 열려 있는 씬부터 실행한다. 따라서 `Gameplay` 씬을 편집하다 Play하면 로딩 화면과 로비를 건너뛴다. 기존 `PlayModeSceneGuard`는 빈 씬일 때만 `Gameplay`로 보내기 때문에 이 문제를 해결하지 못한다.

## 선택한 설계

- 기존 `PlayModeSceneGuard`가 에디터 시작 흐름을 계속 소유한다.
- 에디터가 로드될 때 `EditorSceneManager.playModeStartScene`을 `Assets/JoseonHunter/Scenes/Bootstrap.unity`로 설정한다.
- 일반 사용자의 Play만 대상으로 하며 `Application.isBatchMode`인 자동화 실행에서는 설정하지 않는다.
- 시작 씬 에셋이 누락되면 값을 비워 두고 오류를 기록해 잘못된 씬으로 조용히 진입하지 않게 한다.
- 기존의 빈 씬을 Gameplay로 우회시키는 재생 재시작 로직은 제거한다. `playModeStartScene`이 더 단순하고 Unity가 제공하는 표준 에디터 기능이기 때문이다.

## 대안과 판단

1. `playModeStartScene` 사용 — 추천. 현재 편집 씬을 보존하면서 Play에만 Bootstrap을 적용한다.
2. Play 직전에 Bootstrap 씬을 직접 열기 — 저장 확인과 씬 복원 처리가 필요하고 편집 흐름을 방해한다.
3. 각 런타임 씬에서 Bootstrap으로 되돌리기 — 테스트와 직접 씬 로딩까지 방해하고 화면이 한 번 잘못 뜰 수 있다.

## 검증

- EditMode 테스트에서 일반 에디터 조건이면 Bootstrap 씬 에셋이 선택되는지 확인한다.
- 배치 모드 조건이면 강제 시작 씬을 선택하지 않는지 확인한다.
- 기존 씬 순서 및 Bootstrap 로딩 PlayMode 테스트를 재실행한다.

## 범위 밖

- 로딩 화면의 시각 디자인 변경
- Lobby 및 Gameplay 런타임 내비게이션 변경
- 저장 데이터 형식 변경
