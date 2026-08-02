# 감정·경험치 흡수·서리병·벽력탄 개선 검증

검증 기준 커밋: `41cb435 fix: isolate experience pickup feedback`

## 구현 커밋

- `b2af45f fix: improve appraisal pacing and spacing`
- `b70845d feat: animate experience absorption`
- `5700c4e feat: clarify frost flask impact`
- `030a475 balance: reduce thunder bomb area damage`
- `41cb435 fix: isolate experience pickup feedback`

## 테스트

### 전체 EditMode

명령:

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode'
```

결과:

- Total: 571
- Passed: 571
- Failed: 0
- Skipped: 0
- Unity 기록 실행 시간: 24.801851초

### 변경 범위 PlayMode

명령:

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests|JoseonHunter.Tests.PlayMode.FirstPlayablePickupRangePlayModeTests'
```

결과:

- Total: 33
- Passed: 33
- Failed: 0
- Skipped: 0
- Unity 기록 실행 시간: 35.2189662초

감정 결과의 누적 문구·잠재능력·확인 버튼 경계, 수치 상승 곡선, 경험치 초기 범위 유지, 흡수 가속과 잔상 생성을 포함한다.

### 서리병·벽력탄 집중 검증

명령:

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode.WeaponContentTests|JoseonHunter.Tests.EditMode.WeaponMechanicTests'
```

결과:

- Total: 78
- Passed: 78
- Failed: 0
- 서리병 착지 100% 피해, 장판 50% 틱 피해, 에셋 둔화율 전달, 기존 빙결·진화·잠재능력 회귀를 포함한다.
- 벽력탄 피해 `12/15/18/21/24` 콘텐츠 계약을 포함한다.

전체 PlayMode 중 과거부터 실패하던 `WeaponPotentialCombatBPlayModeTests` 묶음은 이번 변경의 완료 게이트로 사용하지 않았다. 이번 변경 표면의 PlayMode 33개만 별도로 실행했다.

## Android 빌드

명령:

```powershell
& .\Tools\Unity\Build-AndroidDevelopment.ps1
```

결과:

- 프로세스 종료 코드: 0
- APK: `Builds/Android/JoseonHunter-development.apk`
- 크기: 172,257,702 bytes (164.28 MiB)
- 마지막 수정 시각: 2026-08-02 23:52:20 KST

## diff와 작업 파일 보존

명령:

```powershell
git diff --check 7e2ac18..41cb435
git diff --stat 7e2ac18..41cb435
git status --short
```

결과:

- 구현 범위의 whitespace 오류 없음.
- 구현 범위: 16개 파일, 448 additions, 68 deletions.
- 기존 사용자 변경인 Gameplay 씬, 프로젝트 설정, 폰트 에셋, 각궁 아이콘 메타 및 임시 디렉터리는 스테이징하지 않았다.
- Android 빌드 중 Unity가 자동 직렬화한 `DefaultVolumeProfile.asset`, `JoseonHunterUniversalRenderPipeline.asset`, `UniversalRenderPipelineGlobalSettings.asset`도 기능 커밋에 포함하지 않았다.
