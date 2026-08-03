# 무기 분기·조합 반응·특수 적 검증

검증 일시: 2026-08-03 KST

Unity: 6000.5.5f1

대상 브랜치: `master`

## 자동 테스트

### 전체 EditMode

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform editmode -Filter 'JoseonHunter.Tests.EditMode'
```

- Total: 597
- Passed: 597
- Failed: 0
- Skipped: 0
- Duration: 23.7317516초

### 변경 범위 PlayMode

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.UpgradeChoice|JoseonHunter.Tests.PlayMode.WeaponLegacyFlow|JoseonHunter.Tests.PlayMode.WeaponReplacementFlow|JoseonHunter.Tests.PlayMode.WeaponLegacyPresentation|JoseonHunter.Tests.PlayMode.HwandoGakgungLegacy|JoseonHunter.Tests.PlayMode.TalismanThunderLegacy|JoseonHunter.Tests.PlayMode.JangseungSingijeonLegacy|JoseonHunter.Tests.PlayMode.FrostFanLegacy|JoseonHunter.Tests.PlayMode.SpecialEnemyCombat|JoseonHunter.Tests.PlayMode.WaveRoster'
```

- Total: 45
- Passed: 45
- Failed: 0
- Skipped: 0
- Duration: 9.9856495초

### 140개 대상 30초 고부하 회귀

```powershell
& .\Tools\Unity\Test-Unity.ps1 -Platform playmode -Filter 'JoseonHunter.Tests.PlayMode.CombatPerformanceInvestigationPlayModeTests.CompletedAreaBranchesAgainstOneHundredFortyTargetsStayBoundedForThirtySeconds'
```

- 대상: 일반 112 + 특수 28 = 140
- 완성 분기: 각궁 분열 화살, 벽력탄 지맥 전류, 신기전 화망
- 시뮬레이션: 워밍업 40틱 + 측정 600틱(30초)
- 평균 틱: 3.7949ms
- 워밍업 후 관리 힙 할당: 0 bytes
- 추적 공격: 9개
- 활성 임시 시각효과: 47 / 192
- 특수 적 비율: 일반 적의 25% 이내
- 결과: Passed 1 / 1

## Android 개발 빌드

```powershell
& .\Tools\Unity\Build-AndroidDevelopment.ps1
```

- Result: Success
- APK: `Builds/Android/JoseonHunter-development.apk`
- Size: 172,977,202 bytes
- Modified: 2026-08-03 20:35:16 KST
- Build duration: 629.293초
- 빌드 중 Unity, Bee, IL2CPP, Clang, Java 프로세스는 BelowNormal 우선순위와 8/16 논리 코어 제한으로 실행했다.
- 제한 적용 후 관측 전체 CPU 사용률: 약 54.8~78.5%
- 다른 Unity 테스트와 빌드를 겹쳐 실행하지 않았다.

## PixelLab 산출물 추적

### 무기 분기 선택 이미지

리뷰 팩: `4f534d97-ee1b-46cf-a17d-1eb81614b63e`

- `81798cc5-9a5a-49c7-95b9-9d906e6b3b25`
- `2824d6a6-895f-46ed-a446-d60b5cf76efa`
- `12c3f75a-838c-4e7b-8312-eb3f4c7448b3`
- `cf5b0243-fbb6-470b-aea9-13e1d0700db5`
- `7357ce64-6d86-4166-8c13-b74e0f0c6be0`
- `61c5a17d-1ee9-4153-817e-3456c8240b8d`
- `a4e9f7b2-ab64-4753-b2ee-f70c62b43559`
- `60e844a5-53da-4c23-9bfd-7a500d4a96a2`
- `f4fb506d-a772-4272-b694-0a2699eae7f6`
- `d99238a3-20c6-450f-823a-a5fa9f6aa935`
- `9af14943-ba93-4c93-b58f-8db65e2aa135`
- `00d5bba0-fe7b-4129-a844-0837c92ce889`
- `695a9a53-0dc0-4b8e-99fa-4c1a624289da`
- `b5fcc998-1ab8-4cd4-a589-a4d89036f2b7`
- `d4d09909-5afa-4f11-8eb0-ebf7f298447b`
- `9dc2937e-446d-43a9-8a13-45da4c206c25`

### 조합 반응·특수 적 이미지

전투 팩: `f8f25e69-ae97-405a-8a88-8e47f433307c`

- 조합 반응: `2d1ce0a0-2a09-42eb-911a-2cf9dc0f1db3`, `8181304e-f170-4138-a94b-04aa63d4129e`, `c21cad99-9cf6-4020-85d6-4490bf1e8a7b`, `f3102de0-86ef-45cd-ac88-6e520071935d`
- 방패 도깨비: `0ac8b4af-b121-4938-81c1-d43cdf11c986`
- 원혼 무당: `47954392-a39f-4cca-b1bc-1e33942682e9`
- 돌진 쇠뿔귀: `98115008-fa4e-4bd7-8934-0822a5e320d4`
- 분열 쥐: `17b65698-a9d7-4fb1-af09-d13eb1cff950`

애니메이션 그룹:

- 방패 도깨비: `af791c95-a1e4-4305-b6e0-44c046b45575`
- 원혼 무당: `72e72547-74f0-4fa5-bdee-c1041360aa5f`
- 돌진 쇠뿔귀: `acef0790-1151-4de6-97e1-216350be3313`
- 분열 쥐: `6fa4b898-2577-4b5e-a67d-b2ea6ac0381b`

모든 산출물은 48×48 PNG, 3색 이하, 흰색 외곽선 없음, Point 필터, 무압축, mipmap 비활성 계약을 통과했다.

## 남은 수동 확인

- 실제 기기에서 먼 카메라 기준 무기 분기 아이콘과 특수 적 예고 동작의 가독성을 확인한다.
- 특수 적 최초 등장 한글 안내가 전투 흐름을 과하게 막지 않는지 확인한다.
- 과거 픽셀 마스크를 직접 비교하는 `WeaponPotentialCombatA/BPlayModeTests`는 현재 표현 계약과 맞지 않는 구형 묶음이므로 별도 마이그레이션 또는 제거가 필요하다. 이번 변경 범위 테스트에는 포함하지 않았다.
