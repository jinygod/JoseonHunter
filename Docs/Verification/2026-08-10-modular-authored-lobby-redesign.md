# 모듈형 로비 UI 최종 검증 보고서

## 판정

**READY — 전체 자동화 테스트와 Android 개발 빌드 결과를 기준으로 배포 후보 상태**

이번 변경은 시안의 그림을 그대로 복제하는 작업이 아니라, 승인된 정보 배치와 화면 이동 구조를 Unity authored UI로 이관한 작업이다. 기존 조선풍 9-slice UI 부품을 재사용했으며, 별도의 유료 에셋이나 새 외부 패키지를 추가하지 않았다.

## 최종 화면 구조

```text
Lobby Canvas
└─ Safe Area
   ├─ Background
   ├─ Common Header
   ├─ Home Page
   │  ├─ 수련
   │  ├─ 출전
   │  └─ 연구
   ├─ Training Page
   ├─ Patrol Page
   ├─ Research Page
   └─ Settings Overlay
```

- 시작 화면은 Home 하나만 활성화된다.
- Home에는 수련·출전·연구의 세 메뉴만 있고 `환도 비검 연구` 상세 문구는 없다.
- 하단 탭 내비게이션은 제거됐다.
- 상세 화면은 공통 헤더와 뒤로가기 규칙을 공유한다.
- 각 화면은 `Lobby.unity`와 연결된 모듈 prefab을 Scene/Prefab Mode에서 직접 편집할 수 있다.

## 기능 보존

- 계정 레벨·경험치·엽전·설정 헤더
- 수련 6종의 현재/다음 효과, 구매, 전체 초기화
- 출전 지역·난이도·시작 무기 선택과 Bootstrap 경유 전투 진입
- 무기 8종, 연구 3단계, 숙련도·엽전·선행 연구 규칙과 장착 저장
- 설정 오버레이 음악·효과음 슬라이더 및 닫기

## 자동 검증

| 구분 | 결과 |
|---|---:|
| 전체 EditMode | 1,014 / 1,014 PASS |
| 전체 PlayMode | 364 / 364 PASS |
| 최종 focused lobby fixtures | 131 / 131 PASS |
| Lobby scene/module missing scripts | 0 |

XML과 로그는 `Artifacts/LobbyModularFinal/`에 보관했다. Unity 실행은 순차적으로 수행했고 Editor 및 자식 프로세스는 BelowNormal 우선순위와 affinity mask 255를 적용했다.

## 시각 검증

Home, Training, Patrol, Research 네 화면을 다음 세 해상도로 다시 캡처했다.

- 720x1280
- 1080x1920
- 1080x2340

총 12장의 최종 PNG는 `Artifacts/LobbyPremium/`에 있다. 홈의 세 메뉴 구분, 공통 헤더, 뒤로가기, 수련 진행률, 출전 잠금 표시, 연구 행과 결과 피드백 영역을 원본 해상도로 확인했다.

## Scene/asset 감사

- 활성 Build Settings: `Bootstrap` → `Lobby` → `Gameplay`
- semantic sliced frame: FullRect mesh, border 유지
- 아이콘: point filtering, 투명 배경 유지
- 한글: TMP 텍스트 사용, 비트맵에 굽지 않음
- runtime 전체 화면 재생성 경로: 제거
- 캡처 중 production `Lobby.unity` 저장: 없음

## Android 개발 빌드

- 결과: SUCCESS
- 대상: ARM64 / IL2CPP 개발 APK
- 파일: `Builds/Android/JoseonHunter-development.apk`
- 크기: 173,866,430 bytes
- SHA-256: `DEDD44181657B2EC0358F18E189D7C483F67EB8EA95A895BBA3507D682512A71`
- 로그: `Logs/android-development-build.log`

## 남은 수동 확인

자동 캡처와 테스트는 통과했지만, 실제 Android 기기에서의 터치 감각·폰트 체감 크기·발열·메모리는 별도 기기 확인 항목이다.
