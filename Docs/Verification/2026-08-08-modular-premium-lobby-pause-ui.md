# 모듈형 프리미엄 로비·일시정지 UI 검증

## 환경

- 프로젝트: `D:\UnityProjects\JoseonHunter`
- Unity: `6000.5.5f1 (d16e074b49fd)`
- 브랜치: `master`
- 검증일: 2026-08-08

## 빌드와 자동 테스트

로비 프리팹과 씬은 다음 편집기 메서드로 재생성했다.

```powershell
Unity.exe -batchmode -nographics -projectPath D:\UnityProjects\JoseonHunter -executeMethod JoseonHunter.Editor.Scenes.LobbySceneBuilder.BuildInBatchMode -logFile Artifacts\premium-lobby-build-final.log
```

로그에서 `JoseonHunter Lobby presentation built.`를 확인했다.

최종 트리 전체 테스트 결과:

- EditMode: `912/912` 통과, 실패 0
  - 결과: `Artifacts/premium-lobby-final-editmode.xml`
- PlayMode: `310/310` 통과, 실패 0
  - 결과: `Artifacts/premium-lobby-final-playmode.xml`

실행 명령:

```powershell
Unity.exe -batchmode -nographics -projectPath D:\UnityProjects\JoseonHunter -runTests -testPlatform EditMode -testResults Artifacts\premium-lobby-final-editmode.xml -logFile Artifacts\premium-lobby-final-editmode.log
Unity.exe -batchmode -nographics -projectPath D:\UnityProjects\JoseonHunter -runTests -testPlatform PlayMode -testResults Artifacts\premium-lobby-final-playmode.xml -logFile Artifacts\premium-lobby-final-playmode.log
```

## 원본 해상도 시각 검수

다음 PNG를 원본 해상도로 열어 확인했다.

- `Artifacts/LobbyPremium/720x1280-patrol.png`
- `Artifacts/LobbyPremium/1080x2340-patrol.png`
- `Artifacts/PortraitValidation/720x1280/04-pause.png`

확인 결과:

- 스테이지 좌우 화살표가 명패 양옆에 배치되고 캐릭터 선택을 건드리지 않는다.
- 보통·흉조·대흉 카드의 선택/비선택 밝기와 대흉의 대각선 잠금 표시가 구분된다.
- 시작 무기 카드와 출전 버튼이 서로 겹치지 않는다.
- 하단 메뉴는 글자 없이 무기 연구·출전·수련 픽셀 아이콘만 표시한다.
- 새 프레임과 아이콘에 흰색 후광이 없고 Point 필터 픽셀이 유지된다.
- 일시정지 패널에는 오디오 슬라이더 두 개, `계속하기`, `로비로 돌아가기`만 있으며 별도 설정 버튼이 없다.
- 일시정지 제목·설명·슬라이더·두 행동 버튼이 패널 테두리 안에 들어온다.

## 비차단 참고사항

- Unity가 생성한 일부 `.meta` YAML에는 빈 값 뒤 공백이 남지만 임포트와 테스트에는 영향이 없다.
- 첫 배치 실행에서 라이선스 클라이언트 토큰 갱신이 한 차례 실패했으나, 설치 버전 `6000.5.5f1`로 재실행한 최종 빌드·전체 테스트·캡처는 정상 완료됐다.
