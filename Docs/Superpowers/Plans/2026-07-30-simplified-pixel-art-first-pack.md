# 단순화 픽셀 아트 첫 최종 묶음 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 한연화, 산적, 역병쥐, 환도비검을 굵은 외곽선과 큰 색면 중심의 최종 인게임 에셋으로 교체하고 기존 Unity 전투에 연결한다.

**Architecture:** PixelLab이 투명 단일 프레임 PNG와 애니메이션 프레임을 생성한다. 기존 파일 경로와 GUID를 보존하며, 에디터 임포터와 `CombatMotionLibraryBuilder`가 Point/PPU 64 규격과 프레임 배열을 구성한다.

**Tech Stack:** PixelLab Pixflux/Animate Image, Unity 6000.5.5f1, URP 2D, C#, NUnit.

## Global Constraints

- 한 PNG에는 한 프레임만 저장한다.
- 캐릭터 프레임은 투명 96×96, Point, 무압축, 밉맵 해제, PPU 64다.
- 검정 2~3px 외곽선과 4~6색 큰 색면을 사용한다.
- 기존 조선 복식과 무기 정체성을 보존한다.
- 기존 사용자 소유 `.meta`, 씬, ProjectSettings 변경을 일괄 스테이징하지 않는다.

---

### Task 1: 에셋 품질 계약

**Files:**
- Create: `Assets/JoseonHunter/Tests/EditMode/SimplifiedPixelArtContractTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/JoseonAssetPostprocessor.cs`

**Interfaces:**
- Produces: 대상 폴더의 크기, 단일 스프라이트, Point, 무압축, PPU 64 검증.

- [ ] 테스트가 96×96, 프레임 수, 임포트 규격을 요구하도록 작성한다.
- [ ] 현재 에셋으로 테스트를 실행해 크기·프레임 수 실패를 확인한다.
- [ ] 대상 경로에 안전한 임포트 프로필을 추가한다.
- [ ] 임포트 규격 테스트를 다시 실행한다.

### Task 2: PixelLab 기본 프레임 생성

**Files:**
- Replace: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png`
- Replace: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png`
- Replace: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png`
- Replace: `Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando/hwando_blade.png`
- Create: `ArtSource/Pixel/SimplifiedQuality/<asset>/prompt.md`
- Create: `ArtSource/Pixel/SimplifiedQuality/<asset>/provenance.json`

**Interfaces:**
- Produces: 최종 팔레트·외곽선 기준이 되는 96×96 투명 기본 프레임 4개.

- [ ] 기존 에셋 팔레트와 복식 특징을 기록한다.
- [ ] PixelLab Pixflux로 낮은 세부 묘사, 검정 외곽선, 기본 음영의 후보를 생성한다.
- [ ] 투명도, 실루엣 점유율, 외곽선 연속성을 확인한다.
- [ ] 승인 기준을 통과한 결과만 기존 런타임 경로에 저장한다.

### Task 3: 이동·대기·무기 애니메이션

**Files:**
- Replace/Create: `Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa/Idle/idle_00.png` through `idle_03.png`
- Replace/Create: `Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa/Walk/walk_00.png` through `walk_07.png`
- Replace/Create: `Assets/JoseonHunter/Art/Animation/Enemies/Bandit/Walk/walk_00.png` through `walk_05.png`
- Replace/Create: `Assets/JoseonHunter/Art/Animation/Enemies/PlagueRat/Walk/walk_00.png` through `walk_05.png`
- Replace/Create: `Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando/hwando_blade.png` through `hwando_blade_04.png`
- Replace/Create: matching four afterimage and four contact-spark frames.

**Interfaces:**
- Consumes: Task 2 base frames.
- Produces: 루프 가능한 한연화 4/8프레임, 산적 6프레임, 역병쥐 6프레임, 환도 4/4/4프레임.

- [ ] Animate Image 작업을 기본 프레임별로 큐에 넣는다.
- [ ] 첫 프레임 포함 결과에서 루프에 맞는 프레임을 선별한다.
- [ ] 프레임별 피벗과 불투명 바운드의 변동을 검사한다.
- [ ] 파일명을 0부터 연속된 두 자리 번호로 정리한다.

### Task 4: Unity 연결과 화면 크기

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/CombatMotionLibraryBuilder.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatVisualScaleProfile.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatMotionLibraryTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CombatVisualScaleProfileTests.cs`

**Interfaces:**
- Produces: 한연화 4/8, 산적 1/6, 역병쥐 1/6 모션 세트와 유사한 화면 실루엣 크기.

- [ ] 새 프레임 수를 요구하는 실패 테스트를 작성한다.
- [ ] 모션 FPS를 한연화 9fps, 산적 8fps, 역병쥐 10fps로 조정한다.
- [ ] 플레이어와 일반몹의 화면 높이 차이가 15% 이내가 되도록 스케일 프로필을 조정한다.
- [ ] 라이브러리를 재빌드하되 Gameplay 씬은 저장하지 않는다.

### Task 5: 검증

**Files:**
- Modify only if focused verification exposes a regression.

- [ ] 품질 계약, 모션 라이브러리, 스케일 EditMode 테스트를 실행한다.
- [ ] `EightWeaponCombatPlayModeTests`와 `CombatantVisualRigPlayModeTests`를 실행한다.
- [ ] 1920×1080 Gameplay 캡처에서 실루엣, 외곽선, 프레임 전환을 확인한다.
- [ ] 변경 파일만 `diff --check`하고 사용자 소유 변경을 제외해 커밋한다.

