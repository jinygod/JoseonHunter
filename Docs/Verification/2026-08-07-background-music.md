# 배경 음악 통합 검증 기록

검증일: 2026-08-07  
Unity: `6000.5.5f1 (d16e074b49fd)`  
대상: 로비와 현재 플레이 가능한 `귀곡 들판` 15분 전투  
상태: **Ready with limitations**

## 구현 범위

- CC0 음악 6곡을 `Assets/JoseonHunter/Audio/Music/CC0/`에 OGG Vorbis로 추가했다.
- 모든 음악은 stereo, Streaming, background loading, preload 비활성, 품질 `0.55` 임포트 계약을 사용한다.
- `GameMusicDirector`는 장면 전환에도 유지되는 두 개의 2D 반복 `AudioSource`를 번갈아 사용한다.
- 같은 역할을 다시 요청하면 곡을 처음부터 재시작하지 않는다.
- 교차 전환은 `Time.unscaledDeltaTime`을 사용하므로 레벨업·일시정지 중에도 전환 상태가 망가지지 않는다.
- 음악 우선순위는 `종료 > 최종 보스 > 생존 중간 보스 > 현재 전투 구간`이다.
- 음악이 없거나 카탈로그 로드에 실패해도 로비 진입과 전투 진행을 막지 않는다.

## 역할과 실제 음원

| 역할 | 구간 | 게임 파일 | 기본 볼륨 |
|---|---|---|---:|
| 로비 | 로비 진입부터 출전 전환까지 | `lobby_yoiyami.ogg` | 0.34 |
| 전투 초반 | 0:00–4:59 | `gwigok_early_asianoriental.ogg` | 0.38 |
| 전투 중반 | 5:00–9:59 | `gwigok_mid_frozen_desert.ogg` | 0.40 |
| 전투 후반 | 10:00–최종 보스 직전 | `gwigok_late_hope.ogg` | 0.42 |
| 중간 보스 | 중간 보스 생존 중 | `midboss_determined_pursuit.ogg` | 0.44 |
| 최종 보스 | 최종 보스 생존 중 | `finalboss_epic_battle.ogg` | 0.46 |

곡별 제작자, 원본 URL, 원본 파일명, CC0 확인과 원본 SHA-256은
`Docs/ThirdPartyAudio/free-audio-source-manifest.md` 및
`Docs/Assets/audio-rights-ledger.csv`에 기록했다.

게임 사본 SHA-256:

| 파일 | SHA-256 |
|---|---|
| `lobby_yoiyami.ogg` | `3132BCB840D02442C35AC11FCB0E3C328D6516AA2F84C937D78E53FFDF420B8D` |
| `gwigok_early_asianoriental.ogg` | `172D95262348D020D7D1428046B100AEFBAD0A6CBAA93EF87EFAFEB8ADD107D0` |
| `gwigok_mid_frozen_desert.ogg` | `62DDC8D2A52A94FA42DDAAB48A01157F9B6B2A84A726C3090BE222C9D944B949` |
| `gwigok_late_hope.ogg` | `1615903236286AF59D14B4D71DA3FC2518A3091ECDD8F162A50215B9B3D0F320` |
| `midboss_determined_pursuit.ogg` | `863B64F5FFC9F45F5140C1919FAC9970E22C5FAF7EF6CC420A97C0B799D3CD60` |
| `finalboss_epic_battle.ogg` | `C3D8FF842DAF18EE1ACB637B0AB4E09E693490BD20297CF445C56F7DBA73FA2E` |

## 자동 검증

### 테스트 주도 회귀 검증

- 음악 임포트 계약: 7/7 통과
- 5분·10분 구간 정책: 10/10 통과
- 기본 카탈로그: 1/1 통과
- 영속 재생기·교차 페이드: 5/5 통과
- 전투 음악 우선순위 상태: 5/5 통과
- 로비·15분 전투·보스 통합 흐름: 2/2 통과
- 기존 전투 페이싱·효과음까지 포함한 관련 PlayMode 묶음: 22/22 통과

새 타입이 없거나 임포트 프로필이 적용되지 않은 상태에서 먼저 실패하는 RED를 확인한 뒤 구현 후 GREEN을 확인했다.

### 전체 회귀 테스트

| 범위 | 결과 | 결과 파일 |
|---|---:|---|
| 전체 EditMode | 796/796 통과, 실패 0, 건너뜀 0 | `Artifacts/bgm-final-editmode.xml` |
| 전체 PlayMode | 278/278 통과, 실패 0, 건너뜀 0 | `Artifacts/bgm-final-clean-playmode.xml` |

두 실행 모두 Unity를 `BelowNormal` 우선순위와 4코어 affinity로 제한해 순차 실행했다.

### Console 분류

- 최종 EditMode: C# 경고 0, 컴파일 오류 0
- 최종 Android 빌드: C# 경고 0, 빌드 오류 0
- 음악 관련 22개 PlayMode 묶음: AudioListener 경고 0
- 전체 PlayMode: 테스트가 `GameAudioDirector`의 안전 AudioListener를 의도적으로 파괴하는 격리 구간에서만 `There are no audio listeners`가 30회 기록됐다. 모든 테스트는 통과했고 실제 Lobby/Gameplay 장면 및 Android 플레이어에는 장면 리스너 또는 효과음 디렉터의 안전 리스너가 존재한다. 제품 런타임 오류로 분류하지 않았으며 로그는 보존했다.

## Android 개발 빌드

- 결과: `Build Finished, Result: Success`
- APK: `D:\UnityProjects\JoseonHunter\Builds\Android\JoseonHunter-development.apk`
- 생성 시각: `2026-08-07T11:14:53.7614709+09:00`
- 크기: `173,388,848 bytes`
- SHA-256: `802EA2F5A56DBF6BF9987ECF62D35F4A7A40DD0BE71A79B1C99823CFFDB4E4E2`
- 패키지: `com.jinygod.joseonhunter`, version `0.1.0` (`1`)
- Android: min SDK 26, target/compile SDK 36, ARM64 `libil2cpp.so` 포함, IL2CPP
- 빌드 장면: `Bootstrap`, `Lobby`, `Gameplay`

APK는 프로덕션 코드 커밋 `a188e74` 이후 생성했다. 이후 커밋 `797b4a6`은 PlayMode 테스트 정리만 포함하므로 플레이어 어셈블리 내용에는 영향이 없다.

## 수용 기준

| 기준 | 상태 | 증거 |
|---|---|---|
| CC0 6곡과 권리 기록 | 통과 | 권리 원장, 원본·게임 사본 해시 |
| 정확히 6개의 Streaming 음악 클립 | 통과 | 임포트 계약 7/7, 정적 파일 수 검사 |
| 두 채널 영속 교차 페이드 | 통과 | 재생기 PlayMode 5/5 |
| 로비와 전투 초·중·후반 전환 | 통과 | 통합 PlayMode 2/2 |
| 중간 보스 우선 및 격파 후 최신 구간 복귀 | 통과 | 상태 5/5, 통합 흐름 |
| 최종 보스와 승패·포기 종료 | 통과 | 통합 흐름, 기존 효과음 회귀 묶음 |
| Android 플레이어 빌드 | 통과 | 새 ARM64 IL2CPP APK |
| 실제 휴대폰 음량·스피커·이어폰 청감 | 미실행 | 연결된 Android 기기 없음 |

## 제한과 다음 기기 확인

물리 Android 기기에서 다음 항목은 아직 검증하지 않았다.

1. 로비에서 음악이 UI 클릭음과 대사를 덮지 않는지
2. 출전 로딩 뒤 초반 곡 전환에 공백·중복 재생이 없는지
3. 5분·10분 경계와 중간 보스 복귀가 실제 청감상 자연스러운지
4. 최종 보스 진입과 승전 결과에서 페이드 길이가 적절한지
5. 스마트폰 스피커와 이어폰 양쪽에서 무기·경고 효과음이 음악 위로 선명한지
6. 15분 연속 플레이에서 스트리밍 끊김, 메모리 급증, 발열 문제가 없는지

첫 기기 플레이에서는 곡 교체보다 각 역할의 볼륨만 먼저 조정한다. 신규 스테이지 음악은 해당 스테이지 콘텐츠가 실제 구현될 때 같은 3구간 역할 구조에 변주곡을 추가한다.

## 저장소 상태

- 구현·테스트 커밋은 모두 `origin/master`에 푸시했다.
- 기존 작업에서 남아 있던 CombatChoices 및 일부 캐릭터/VFX `.meta` 변경은 이번 음악 작업에 섞지 않고 로컬에 그대로 보존했다.
- `Artifacts/`, `Logs/`, `Builds/`는 검증 산출물이며 기능 소스 커밋에는 포함하지 않았다.
