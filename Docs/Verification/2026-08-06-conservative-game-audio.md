# 절제된 기본 사운드 통합 검증

검증일: 2026-08-06

프로젝트: `D:\UnityProjects\JoseonHunter`

Unity: `6000.5.5f1`

## 구현 범위

- 씬 전환에도 하나만 유지되는 `GameAudioDirector`
- 런타임 생성이 없는 12개 2D `AudioSource` 고정 풀
- UI, 경험치, 엽전, 자석, 레벨업, 증강 확정 이벤트
- 8개 무기의 대표 확정 명중음과 일반·치명타 피격음
- 최종 우두머리 경고, 등장, 격파, 승전·패전음
- 경험치·엽전·일반 피격 쿨다운과 동일 공격 인스턴스의 무기음 1회 제한
- CC0 음원 18개만 선별 편입하고 mono, Vorbis, `Decompress On Load` 적용

## 자동 검증

### 전체 EditMode

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

- 결과: `756 / 756` 통과
- 실패: `0`
- 건너뜀: `0`

### 전체 PlayMode

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode
```

- 결과: `264 / 264` 통과
- 실패: `0`
- 건너뜀: `0`

### Android 개발 빌드

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Build-AndroidDevelopment.ps1
```

- 결과: 성공 (`Build Finished, Result: Success`)
- APK: `Builds/Android/JoseonHunter-development.apk`
- APK 크기: `173,354,441 bytes` (`165.32 MB`)
- 빌드 시간: `504.137초`

## 오디오 계약 결과

- 승인된 런타임 클립: 정확히 `18개`
- 원본 파일 합계: `577,709 bytes`
- 모든 클립 길이: `4초 이하`
- 모든 클립: mono 강제, 사전 로드, Vorbis, `Decompress On Load`
- 클립 누락 경고: `0`
- C# 컴파일 경고·오류: `0`
- AudioSource 런타임 생성: 초기 12개 이후 `0`
- 동일 무기·공격 인스턴스 기록: 최대 `64개`

## 남은 수동 확인

자동 테스트와 Android 빌드는 통과했지만 실제 스마트폰 스피커·이어폰에서의 최종 음량과 체감은 자동 검증할 수 없다. 다음 기기 플레이에서 로비 클릭, 경험치 대량 흡수, 각궁, 환도, 벽력탄, 서리병, 우두머리 경고 순서로 들어 보고, 개별 볼륨 조정만 후속으로 수행한다. 배경 음악은 설계대로 이번 범위에 포함하지 않았다.
