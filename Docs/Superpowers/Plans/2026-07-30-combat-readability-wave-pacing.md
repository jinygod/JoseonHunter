# 전투 가독성·15분 웨이브 통합 폴리싱 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 8종 무기의 명중 가독성과 피격 반응을 높이고, 2/5/10/15분 사건을 가진 15분 웨이브를 60초 샘플에서도 압축 체험하게 만든다.

**Architecture:** 정식 900초 좌표계의 순수 C# `StagePacingTimeline`이 밀도와 사건을 계산하고, `FirstPlayableController`가 일회성 사건과 몬스터 생명주기를 소유한다. 기존 전투 피드백·무기 프레젠테이션·HUD 런타임 부트스트랩을 확장하며 새 전역 관리자나 씬 직렬화는 추가하지 않는다.

**Tech Stack:** Unity 6000.5.5f1, C#, NUnit EditMode/PlayMode, uGUI, URP 2D.

## Global Constraints

- 정식 스테이지 사건은 2분 급습, 5분 중간보스, 7분 급습, 10분 중간보스, 12분 급습, 14분 경고, 15분 최종보스다.
- 60초 샘플은 사건을 처음 50초에 18배 압축하고 최종보스 전투 여유 10초를 남긴다.
- 최종보스 등장 이후 시간 초과만으로 패배시키지 않는다.
- 일반 명중은 전역 히트스톱이나 카메라 흔들림을 만들지 않는다.
- 새 런타임 시각 오브젝트는 기존 풀 또는 고정 개수 오브젝트를 사용한다.
- 씬, 프리팹, ProjectSettings는 수정하지 않는다.

---

### Task 1: 15분 웨이브 타임라인

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Runs/StagePacingTimeline.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/StagePacingTimelineTests.cs`

**Interfaces:**
- Produces: `StagePacingTimeline.ForDuration(float)`, `Sample(float)`, `Crossed(float,float,StageMilestone)`.
- Produces: `StagePacingSnapshot` with `ActiveCap`, `SpawnIntervalSeconds`, `BatchSize`, `EliteChance`, `SurgeIntensity`.

- [ ] **Step 1: Write failing boundary and preview tests**

```csharp
[TestCase(120f, StageMilestone.FirstSurge)]
[TestCase(300f, StageMilestone.FirstMidBoss)]
[TestCase(600f, StageMilestone.SecondMidBoss)]
[TestCase(900f, StageMilestone.FinalBoss)]
public void ProductionTimelineCrossesAuthoredMilestones(float time, StageMilestone milestone)
{
    var timeline = StagePacingTimeline.ForDuration(900f);
    Assert.That(timeline.Crossed(time - .1f, time, milestone), Is.True);
}

[Test]
public void PreviewSpawnsFinalBossAtFiftySeconds()
{
    var timeline = StagePacingTimeline.ForDuration(60f);
    Assert.That(timeline.Crossed(49.9f, 50f, StageMilestone.FinalBoss), Is.True);
}
```

- [ ] **Step 2: Run the focused EditMode fixture and confirm missing-type failures**

Run: `Tools/Unity/Test-Unity.ps1 -TestPlatform EditMode -TestFilter JoseonHunter.Tests.EditMode.StagePacingTimelineTests`

- [ ] **Step 3: Implement immutable timeline values and bounded surge profiles**

Use a 900-second canonical table. `ForDuration(60)` uses a 50-second event window; all longer runs use their full duration. Keep every calculation allocation-free.

- [ ] **Step 4: Re-run the fixture and confirm all timeline tests pass**

- [ ] **Step 5: Commit the timeline and tests**

```powershell
git add -- Assets/JoseonHunter/Scripts/Domain/Runs/StagePacingTimeline.cs Assets/JoseonHunter/Tests/EditMode/StagePacingTimelineTests.cs
git commit -m "feat: add fifteen minute stage pacing timeline"
```

### Task 2: Runtime spawning, midbosses, and wave HUD

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyDensityProfile.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/StagePacingPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/EnemyDensityProfileTests.cs`

**Interfaces:**
- Consumes: `StagePacingTimeline`.
- Produces: controller-owned one-shot milestone state, explicit midboss/final-boss rank, and `WaveAnnouncement` UI state.

- [ ] **Step 1: Write failing tests for surge density, one-shot midbosses, and final-boss-only victory**

```csharp
[Test]
public void SurgeDensityIsHigherButNeverExceedsMobileCap()
{
    var timeline = StagePacingTimeline.ForDuration(900f);
    var calm = timeline.Sample(90f);
    var surge = timeline.Sample(125f);
    Assert.That(surge.BatchSize / surge.SpawnIntervalSeconds,
        Is.GreaterThan(calm.BatchSize / calm.SpawnIntervalSeconds));
    Assert.That(surge.ActiveCap, Is.LessThanOrEqualTo(EnemyDensityProfile.MaximumActiveEnemies));
}
```

PlayMode coverage must assert that a spawned midboss has a health bar, its death does not end the run,
and final-boss death does.

- [ ] **Step 2: Run the new fixtures and confirm behavioral failures**

- [ ] **Step 3: Replace normalized linear spawning with timeline snapshots**

Track the previous elapsed time, consume crossed milestones once, spawn midbosses with separate health multipliers, and publish short Korean announcements.

- [ ] **Step 4: Render wave announcements without pausing gameplay**

Add a centered, outlined HUD label. Use the state-provided remaining display time and intensity to control visibility, scale, and color.

- [ ] **Step 5: Run focused EditMode and PlayMode fixtures**

- [ ] **Step 6: Commit runtime pacing integration**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay Assets/JoseonHunter/Scripts/Presentation/UI/CombatHudPresenter.cs Assets/JoseonHunter/Tests/EditMode/EnemyDensityProfileTests.cs Assets/JoseonHunter/Tests/PlayMode/StagePacingPlayModeTests.cs
git commit -m "feat: integrate surge and boss pacing"
```

### Task 3: 8종 무기 화면 가독성

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponPresentationScale.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponVisualCue.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponVisualCueTests.cs`

**Interfaces:**
- Produces: weapon-and-stage-specific scale and lifetime with bounded level/evolution growth.

- [ ] **Step 1: Write failing per-weapon silhouette tests**

Assert all projectiles exceed the mobile readability floor, trails stay smaller than their projectiles,
and all area/detonation cues remain below the screen-filling cap.

- [ ] **Step 2: Run the fixture and confirm current common multipliers violate the new contract**

- [ ] **Step 3: Implement explicit weapon/stage profiles**

Keep projectiles recognizable, keep trails subordinate, and make evolved impact timing stronger without
turning evolution into a uniform scale increase.

- [ ] **Step 4: Run `WeaponVisualCueTests` and `EightWeaponCombatPlayModeTests`**

- [ ] **Step 5: Commit weapon readability changes**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation Assets/JoseonHunter/Tests/EditMode/WeaponVisualCueTests.cs
git commit -m "feat: tune eight weapon combat readability"
```

### Task 4: 피격 대상 반응과 피해 숫자 마감

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/CombatFeedbackDirector.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/Combat/DamageNumberPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/FeedbackBudgetTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatantVisualRigPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/DamageNumberPoolPlayModeTests.cs`

**Interfaces:**
- Produces: local hit-flash state in `CombatantVisualRig`, bounded critical/kill feedback profiles, compact anchored number motion.

- [ ] **Step 1: Write failing tests for hit tint restoration and feedback budgets**

Assert normal hits retain zero hitstop/impulse, critical hits stay below 0.035 seconds, and the rig restores
its sprite color after the local flash.

- [ ] **Step 2: Run focused fixtures and confirm failure**

- [ ] **Step 3: Implement unscaled local hit flash and preserve death alpha**

Drive tint inside `CombatantVisualRig.Tick`, update follower layers consistently, and avoid coroutines per enemy hit.

- [ ] **Step 4: Tighten number rise and critical differentiation**

Keep normal numbers close to contact, use gold and a short scale punch for criticals, and keep boss values only slightly larger.

- [ ] **Step 5: Run focused EditMode and PlayMode fixtures**

- [ ] **Step 6: Commit combat feedback changes**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/CombatantVisualRig.cs Assets/JoseonHunter/Scripts/Presentation/Combat Assets/JoseonHunter/Tests/EditMode/FeedbackBudgetTests.cs Assets/JoseonHunter/Tests/PlayMode
git commit -m "feat: polish contact feedback"
```

### Task 5: 통합 검증

**Files:**
- Modify only if verification exposes a regression in the files above.

- [ ] **Step 1: Run focused EditMode tests**

Run timeline, density, weapon cue, feedback budget, and run-rule fixtures.

- [ ] **Step 2: Run focused PlayMode tests**

Run stage pacing, eight-weapon combat, combatant visual rig, and damage-number fixtures.

- [ ] **Step 3: Run Unity compilation and inspect the newest Console errors**

- [ ] **Step 4: Review `git diff --check`, changed-file scope, and serialized assets**

Confirm no scene, prefab, ProjectSettings, or unrelated user-owned dirty files were staged.

- [ ] **Step 5: Commit only verification-driven corrections**

