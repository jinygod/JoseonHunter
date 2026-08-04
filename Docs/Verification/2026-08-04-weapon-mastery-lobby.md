# 무기 숙련도·로비 수직 기능 검증 보고서

## 판정

**Ready with limitations** — 구현 범위의 Unity 전체 테스트, 씬·프리팹 계약, 상태별 시각 캡처와 Android ARM64 개발 APK 빌드는 통과했다. 실제 Android 기기 설치, 터치 조작, 발열·프레임 페이싱은 이번 로컬 검증에 포함하지 않았다.

## 검증 기준

- Unity: `6000.5.5f1 (d16e074b49fd)`
- URP: `17.5.0`
- Input System: `1.20.0`
- 검증 소스: `a82f57d56e5ee1713e3569fa381fed84aaaa5835`
- 원격: `origin/master`가 위 커밋과 동일
- 빌드 씬 순서: `Bootstrap`, `Lobby`, `Gameplay`
- Android: 개발 APK, IL2CPP, ARM64, 세로 화면

## 승인 기준 결과

| 기준 | 상태 | 증거 |
|---|---|---|
| 무기별 막타 숙련도와 적 등급별 가중치 | Passed | `WeaponKillLedgerTests`, 전투 PlayMode 전체 통과 |
| 8개 무기마다 기본식과 해금 운용법 2개 제공 | Passed | `WeaponMasteryProgressionTests`, `WeaponResearchLobbyPlayModeTests` |
| 숙련도 조건 + 엽전 최종 해금, 중복 결제 방지 | Passed | 연구 PlayMode 및 저장 트랜잭션 테스트 |
| 엽전 기반 공통 수련 6종과 전액 초기화 | Passed | `CommonTrainingProgressionTests`, `CommonTrainingLobbyPlayModeTests` |
| 3개 독립 출전 편성과 시작 무기 저장 | Passed | `PatrolLoadoutTests`, `LobbyPatrolPlayModeTests` |
| 로비의 실제 씬 오브젝트·3개 메뉴·기본 출전 화면 | Passed | `LobbySceneContractTests`, `LobbyNavigationPlayModeTests` |
| 불투명 한지 패널, 한글 글꼴, 제한된 색상 | Passed | 720×1280 및 1080×2340 상태별 캡처 육안 확인 |
| 승리·패배·중도 귀환 정산을 정확히 한 번 저장 | Passed | `RunSettlementLobbyPlayModeTests` 3/3 |
| 저장 실패 시 진행도를 버리지 않고 재시도 | Passed | 첫 저장 실패 후 재시도·중복 없음 PlayMode 테스트 |
| Bootstrap → Lobby → Gameplay → Lobby 왕복 | Passed | 세션·로비·정산 PlayMode 전체 통과 |
| 스키마 1 저장 호환 및 스키마 2 왕복 | Passed | 마이그레이션·복구·메타 진행 EditMode 전체 통과 |
| Android ARM64 개발 빌드 | Passed | APK 생성, `aapt2` 및 ZIP 엔트리 검사 |

## 자동 테스트

### EditMode

- 명령 방식: Unity batch `-runTests -testPlatform editmode -testFilter JoseonHunter.Tests.EditMode`
- 결과: **631 passed / 0 failed / 0 skipped**
- Unity 테스트 시간: 26.786초
- 결과: `Logs/editmode-full-release.xml`
- 로그: `Logs/editmode-full-release.log`

### PlayMode

- 명령 방식: Unity batch `-runTests -testPlatform playmode -testFilter JoseonHunter.Tests.PlayMode`
- 결과: **232 passed / 0 failed / 0 skipped**
- Unity 테스트 시간: 119.469초
- 결과: `Logs/playmode-full-release.xml`
- 로그: `Logs/playmode-full-release.log`

전체 검증 중 발견한 회귀 두 건은 숨기지 않고 수정 후 전체 재실행했다.

1. 비어 있는 Lobby의 `SceneRoot`를 기대하던 기존 스캐폴드 테스트를 실제 `Lobby Camera`, `Lobby Canvas`, `EventSystem` 계약으로 갱신했다.
2. 영구 메타 세션이 테스트 사이에 남던 격리 문제를 새 로비·정산 픽스처의 시작/종료 정리로 수정했다.

## 시각 검증

`Artifacts/Lobby`에 아래 상태를 720×1280과 1080×2340으로 각각 캡처했다.

- 출전 및 3개 편성 편집
- 무기 연구 중
- 운용법 해금 가능
- 엽전 부족 피드백
- 공통 수련 현재값·다음값·비용

확인 결과: 한글 글리프 정상, 안전 영역 이탈 없음, 핵심 패널 불투명, 흰 외곽선 없음, 텍스트 겹침 없음. `Lobby.unity`와 `LobbyShell.prefab`에는 누락된 스크립트 참조가 없다.

## Android 개발 APK

- 경로: `Builds/Android/JoseonHunter-development.apk`
- 결과: `Build Finished, Result: Success.`
- 크기: `173,363,160` bytes
- SHA-256: `040CB9963C490FC0A7D7C1979FD11415018EA6F09333A81B413838BD99688BCC`
- 패키지: `com.jinygod.joseonhunter`
- 버전: `0.1.0`, version code `1`
- min SDK / target SDK: `26 / 36`
- ABI: `arm64-v8a`만 포함
- IL2CPP: `lib/arm64-v8a/libil2cpp.so` 포함
- 로그: `Logs/android-development-build-final.log`

APK는 런타임 소스 커밋 `bcf2d4b`에서 빌드했다. 이후 최종 커밋 `a82f57d`까지의 변경은 Editor 전용 로비 검증 캡처 코드뿐이므로 플레이어 런타임 내용에는 차이가 없다.

## 기존 작업 상태와 한계

- 검증 전부터 존재한 CombatChoices 메타, 동적 폰트 에셋, `Gameplay.unity`, URP/프로젝트 설정 등의 미커밋 변경은 이번 기능 커밋에 포함하지 않았다.
- 물리 Android 기기 설치·실행, 터치 입력, pause/resume, 저사양 기기 메모리, 발열과 장시간 프레임 페이싱은 실행하지 않았다.
- Android 개발 빌드는 profiler 연결 옵션 때문에 `INTERNET` 권한을 포함한다. 배포 빌드에서는 개발 옵션을 제거해 별도 검증해야 한다.
- 이번 범위는 로비·메타 진행·정산 기능이며, 전투 성능 최적화의 정량 전후 비교는 별도 작업이다.
