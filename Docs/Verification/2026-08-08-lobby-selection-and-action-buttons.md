# 로비 선택 표시 및 공통 액션 버튼 검증

## 구현 범위

- 로비 난이도 선택: 금색 5px 외곽선, 청록 2px 내곽선, 비선택 갈색 얇은 테두리
- `대흉` 잠금: 문자열 `· 잠김` 제거, 대각선 봉인선과 PixelLab 자물쇠 아이콘 적용
- `15분 생존 · 난이도` 상태 문구 비활성화
- 출전 및 하단 선택 메뉴의 선택 테두리 강화
- 일시정지, 전투 결과, 설정, 보상, 무기 감정, 무기 교체 취소, 수련 액션에 공통 9-slice 프레임 적용
- `계속하기`에는 재생 아이콘, `로비로 돌아가기`에는 조선 기와문 아이콘 적용
- 증강/전승/무기 교체/무기 선택 정보 카드는 공통 액션 스킨 대상에서 제외

## 자동 검증

| 검증 | 결과 | 증거 |
| --- | --- | --- |
| PixelLab 에셋 계약 | 5/5 통과 | `Artifacts/lobby-buttons-assets-green.xml` |
| 공통 버튼 및 관련 화면 집중 PlayMode | 62/62 통과 | `Artifacts/button-skin-related-final.xml` |
| 로비 난이도/내비게이션 집중 PlayMode | 16/16 통과 | `Artifacts/lobby-selection-green.xml` |
| 캡처 상태 정책 | 3/3 통과 | `Artifacts/pause-name-green.xml` |
| 최종 EditMode 전체 | 905/905 통과, 실패 0 | `Artifacts/lobby-buttons-final-editmode.xml`, 2026-08-08 02:49:31Z–02:49:49Z |
| 최종 PlayMode 전체 | 305/305 통과, 실패 0 | `Artifacts/lobby-buttons-final-playmode.xml`, 2026-08-08 02:50:20Z–02:52:32Z |

## TDD 및 결함 재현

- 에셋 추가 전 계약 테스트는 5/5 실패했고, 생성·임포트 후 5/5 통과했다.
- `JoseonButtonSkin` 추가 전 새 PlayMode 테스트는 누락 타입으로 컴파일 실패했고, 구현 후 2/2 통과했다.
- 선택 테두리/잠금 계층 추가 전 로비 집중 테스트는 16개 중 새 계약 4개가 실패했고, 구현 후 16/16 통과했다.
- 공통 프레임 적용 뒤 기존 보상 테스트가 단색 배경 비교 때문에 1건 실패했다. 테스트를 승인된 sliced sprite 계약으로 갱신하고 관련 62/62를 재검증했다.
- 첫 일시정지 캡처에서 5번째 상태가 `04-pause.png`를 덮어쓰는 문제를 로그로 추적했다. 모든 phase를 고유 파일명으로 매핑하는 실패 테스트를 추가한 뒤 `04-pause.png`와 `05-resumed-combat.png`가 분리 생성되도록 수정했다.
- 첫 로비 캡처에서 출전 글자의 기존 어두운 색이 남는 문제를 확인했다. 공통 스킨의 밝은 한지 글자색 계약을 RED→GREEN으로 추가했다.

## 시각 검증

- 로비: `Artifacts/LobbyPremium/720x1280-patrol.png`
  - 보통 선택 테두리가 금색/청록으로 즉시 구분된다.
  - 하단 출전 탭도 같은 강조 규칙을 사용한다.
  - 대흉은 `대흉` 글자, 대각선 봉인선, 중앙 자물쇠로 보이며 `잠김` 문자열은 없다.
  - 상단 상태 문구는 없고, 출전 버튼의 한글이 어두운 프레임 위에서 읽힌다.
- 일시정지: `Artifacts/PortraitValidation/720x1280/04-pause.png`
  - 실제 Gameplay 씬과 실제 `FirstPlayableUiBootstrap` 상태를 사용했다.
  - 계속하기는 주 프레임과 재생 아이콘, 로비로 돌아가기는 보조 프레임과 기와문 아이콘을 사용한다.
  - 두 버튼 모두 한글 라벨과 아이콘이 겹치지 않으며 720×1280에서 판독 가능하다.
- 캡처 도구는 720×1280, 1080×1920, 1080×2340, 1170×2532, 1440×3200의 다섯 해상도에서 게임/증강/상세/일시정지/재개 상태 25장을 생성했다.

## 리소스 규칙

- `button_primary_frame.png`, `button_secondary_frame.png`: 64×32, Point, mipmap 없음, 무압축, 8px sprite border
- `icon_continue.png`, `icon_lobby.png`, `icon_lock.png`: 24×24, Point, mipmap 없음, 무압축, 투명 배경
- PixelLab 첫 보조 프레임 후보는 중앙이 흰색이라 제외했고 프로젝트에 포함하지 않았다.

