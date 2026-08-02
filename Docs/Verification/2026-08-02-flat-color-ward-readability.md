# 금줄·장승진 단색 시인성 개선 검증

검증일: 2026-08-02  
Unity: `6000.5.5f1 (d16e074b49fd)`  
대상 브랜치: `master`

## 구현 결과

- 금줄의 반복 밧줄 텍스처, 중간 매듭 PNG, 대형 폐쇄 도장 PNG를 제거했다.
- 금줄은 먹갈색 외곽선과 탁한 황토색 본선의 연속 실선으로 표시한다.
- 금줄 폐쇄는 낮은 알파의 폴리곤 면과 풀링된 황토색 마름모 반짝임 8개로 표시한다.
- 반짝임은 생성 순간부터 내부에 분산되므로 플레이어 중심 한 점에 겹치지 않는다.
- 장승진의 장승·매듭·중앙 봉인·늘인 필드 PNG를 제거했다.
- 장승은 먹갈색 기둥과 황토색 가로띠로 절차 생성하고, 경계는 텍스처 없는 두 겹 실선으로 표시한다.
- 적이 통과한 구간 하나만 본선 밝기가 상승하고 황토색 마름모 세 개가 접촉점에 발생한다.
- 오래된 장승진은 낮은 알파, 최신 장승진은 높은 알파로 표시한다.
- 5레벨 장승진의 재배치에는 짧은 알파 재형성 효과를 적용했다.
- 흰색 외곽선, 흰색 림라이트, 다색 그라데이션은 추가하지 않았다.

## 자동 검증

| 범위 | 결과 | 명령 |
| --- | --- | --- |
| 전체 EditMode | 563/563 통과 | `Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode` |
| 금줄 프레젠테이션 PlayMode | 4/4 통과 | `Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.FirstPlayablePresentationPlayModeTests` |
| 여덟 무기 통합 PlayMode | 9/9 통과 | `Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests` |
| 장승진 진화·표식·교차 PlayMode | 7/7 통과 | `EvolvedWeaponCombatPlayModeTests`의 `Twelve_guardians_*`, guardian ordering, normal ward 보존 테스트 선택 실행 |
| 시각 프레젠터 집중 EditMode | 15/15 통과 | `FlatWardVisualTests`, `EightWeaponPolishCapturePolicyTests`, `GeumjulTrailPresenterTests` 선택 실행 |

전체 EditMode에서는 C# 컴파일 오류, `NullReferenceException`, `MissingReferenceException`이 없었다. 성공한 관련 PlayMode 분리 실행에서도 같은 런타임 오류가 없었다.

## 기존 PlayMode 기준 분리

`FirstPlayablePresentationPlayModeTests`, `EightWeaponCombatPlayModeTests`, `EvolvedWeaponCombatPlayModeTests`를 한 번에 실행한 결과는 45개 중 44개 통과, 1개 실패였다. 실패는 이번 변경 범위가 아닌 월식 무기의 기존 테스트 `Moon_eclipse_keeps_outbound_and_return_contact_then_blasts_at_crossing`이며, 단독 재실행에서도 동일하게 `Blast` 누락으로 실패했다.

금줄과 장승진 테스트는 해당 묶음 안에서도 모두 통과했다. 이후 직접 수정 범위는 별도 프로세스로 분리해 4/4, 9/9, 7/7 통과를 확인했다. 두 PlayMode 클래스를 한 프로세스에 다시 묶은 한 차례 실행은 Gameplay 씬 진입 뒤 배치 러너가 결과 XML 없이 정지해 종료했으며, 각각 분리 실행하면 정상 종료됐다.

## 결정적 캡처와 시각 검수

그래픽 배치 캡처 명령:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\UnityProjects\JoseonHunter' `
  -executeMethod JoseonHunter.Editor.Scenes.EightWeaponPolishCapture.CaptureJangseungGeumjulReadabilityInBatchMode `
  -logFile 'D:\UnityProjects\JoseonHunter\Artifacts\flat-ward-capture-spark-final.log'
```

생성 결과:

- `Artifacts/WeaponPolish/jangseung_ward-jangseung-crossing.png` — 368,442 bytes
- `Artifacts/WeaponPolish/hwando_flying_blade-geumjul-closure-ready.png` — 365,945 bytes
- `Artifacts/WeaponPolish/hwando_flying_blade-geumjul-closure-impact.png` — 365,795 bytes
- `Logs/jangseung-geumjul-gameplay.png` — 최종 폐쇄 캡처를 추적 파일로 갱신

360×800 원본을 직접 확인한 결과:

- 장승진은 네 꼭짓점의 작은 단색 장승과 끊김 없는 마름모 경계로 읽힌다.
- 교차 지점 하나에만 짧은 황토색 밝기 상승이 보이고 다른 구간은 기본 색을 유지한다.
- 금줄 준비 상태에는 밧줄 무늬와 매듭이 없고 시작점의 작은 단색 표식만 남는다.
- 금줄 완성 상태에는 화면을 덮는 도장 PNG가 없으며 결계 내부의 낮은 알파 면과 분산된 황토색 밝은 픽셀이 보인다.
- 지속 선에는 흰 테두리나 흰 림라이트가 없다.
- 두 효과 모두 캐릭터와 몬스터보다 장식 이미지가 크게 보이지 않는다.

## 성능과 규칙 보존

- 선과 장승 프레젠터는 초기 생성 후 재사용한다.
- 금줄 폐쇄 메시, 머티리얼, 반짝임은 재사용하며 매 프레임 새로 만들지 않는다.
- 장승진 교차 반짝임은 최대 24개로 제한된 풀을 사용한다.
- 금줄 판정·피해·지속시간·성장 분기와 장승진 배치 수·경계 판정·피해·재진입·진화·5레벨 이동 규칙은 변경하지 않았다.

## 저장소 상태

구현 커밋:

- `afccf95` — 단색 결계 공용 프리미티브
- `8274a0b` — 금줄 프레젠테이션 교체
- `09f2536` — 장승진 프레젠테이션 교체
- `0215e6c` — 반짝임 분산과 새 경계 캡처 호환

작업 전부터 존재한 사용자 변경 파일인 Gameplay 씬, ProjectSettings, MaruBuri SDF 두 개와 `.utmp/`는 스테이징·복원하지 않았다.

