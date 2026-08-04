# 프리미엄 모바일 로비 검증

검증일: 2026-08-04  
Unity: 6000.5.5f1  
대상 브랜치: `master`

## 결과

- EditMode: 631개 통과, 실패 0개
- PlayMode: 233개 통과, 실패 0개
- 로비 흐름: Bootstrap → Lobby → Gameplay 유지
- 실제 메뉴: `무기 연구`, `출전`, `공통 수련` 3개만 노출
- 출전 카드: 프리셋·시작 무기·운용법·기록과 주 행동 `출전` 확인
- 연구/수련: 한국어 글리프, 64px 이상 주요 터치 영역, 불투명 어두운 카드 확인
- 한연화: 로비와 로딩 화면이 같은 Resources 스프라이트를 재사용
- 텍스처: 로비 원화 최대 2048, UI 프레임/버튼 최대 1024, 밉맵 비활성 계약 통과

## 모바일 렌더 캡처

두 화면 비율에서 실제 Unity 카메라 렌더를 확인했다.

- 720×1280: 출전, 무기 연구, 공통 수련, Bootstrap 로딩
- 1080×2340: 출전, 무기 연구, 공통 수련, Bootstrap 로딩

확인 항목:

- 상단 정보, 하단 3개 메뉴, 주요 행동 버튼이 안전 영역 안에 있음
- 글자 겹침·잘림·영문 임시 문구 없음
- 한연화 얼굴과 실루엣이 두 비율에서 유지됨
- 흰 외곽선이나 과도한 장식 없이 먹색·적갈색·금색 팔레트 유지
- 수련 설명은 한 줄로 정리되고 상세 수치와 버튼이 분리됨

## 증거 파일

- `Artifacts/LobbyPremium/720x1280-patrol.png`
- `Artifacts/LobbyPremium/720x1280-research-ready.png`
- `Artifacts/LobbyPremium/720x1280-training.png`
- `Artifacts/LobbyPremium/1080x2340-patrol.png`
- `Artifacts/BootstrapLoadingPremium/720x1280.png`
- `Artifacts/BootstrapLoadingPremium/1080x2340.png`

캡처는 고정 미리보기 데이터를 사용하므로 실제 저장 데이터의 엽전·숙련도 숫자는 실행 시 달라질 수 있다. 배치와 기능 코드는 동일한 프리팹과 Presenter를 사용한다.
