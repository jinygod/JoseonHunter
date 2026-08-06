# Account Level and Training Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 한 판 정산으로 성장하는 계정 레벨 1~100과 계정 레벨에 연동된 총 100강 수련 시스템을 기존 저장·로비·결과 UI에 안전하게 통합한다.

**Architecture:** 경험치 곡선과 수련 계산은 `JoseonHunter.Domain`의 순수 C# 규칙으로 둔다. `AutoSaveOrchestrator`가 계정 경험치를 기존 한 판 보상과 같은 복사본 트랜잭션에서 저장하고, 기존 uGUI 런타임 빌더와 Presenter가 저장 데이터에서 파생된 읽기 전용 상태를 표시한다. 저장 문서는 스키마 3으로 올리되 스키마 1·2를 현재 수련 단계에 맞는 최소 계정 레벨로 이관한다.

**Tech Stack:** Unity 6000.5.5f1, C# 9, uGUI, TextMeshPro, NUnit EditMode/PlayMode, JsonUtility 기반 체크섬 저장소

## Global Constraints

- 작업 루트는 `D:\UnityProjects\JoseonHunter`, 브랜치는 사용자가 명시적으로 승인한 `master`이다.
- 계정 최고 레벨은 100이며 신규 계정은 레벨 1·누적 경험치 0으로 시작한다.
- 한 판 계정 경험치는 `min(floor(생존 초/6),150) + min(floor(처치/4),200) + 승리 250`이고 포기는 전체의 25% 내림값이다.
- 다음 레벨 요구 경험치는 `100 + 40 × (L-1) + 2 × (L-1)²`이며 레벨 20 누적치는 12,958이다.
- 수련은 계정 레벨당 총 5강, 전체 최대 100강, 각 트랙 최대 20강이다.
- 수련 효과는 1~5강 단계당 2%, 6~10강 단계당 0.6%, 11~20강 단계당 0.2%로 20강 최대 15%이다.
- 기존 1~5강 비용 `[100,180,280,420,600]`을 유지하고 6~20강은 `600 + 35d + 8d²`, `d=n-5`를 사용한다.
- 스키마 1·2 저장의 엽전, 숙련도, 해금, 수련 단계·소비 엽전과 기록을 보존한다.
- 저장 실패 시 엽전·숙련도·계정 경험치·기록 어느 것도 라이브 데이터에 부분 반영하지 않는다.
- 로비에 새 실사 또는 AI 이미지를 추가하지 않고 기존 조선풍 픽셀 UI 자원을 사용한다.
- 사용자 소유의 기존 미커밋 `.meta` 변경은 스테이징하거나 수정하지 않는다.
- Unity 배치 작업은 순차 실행하고 프로세스 우선순위를 `BelowNormal`, CPU affinity를 `[IntPtr]15`로 제한한다.

---

### Task 1: Account Experience Domain Rules

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/AccountProgression.cs`
- Create: `Assets/JoseonHunter/Scripts/Domain/Progression/AccountProgression.cs.meta`
- Create: `Assets/JoseonHunter/Tests/EditMode/AccountProgressionTests.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/AccountProgressionTests.cs.meta`

**Interfaces:**
- Consumes: `RunSettlement.Elapsed`, `Kills`, `Victory`, `Abandoned`.
- Produces: `AccountProgression.MaximumLevel`, `RewardFor`, `RequiredForNextLevel`, `TotalExperienceForLevel`, `StateFor`, `TryAdd`.
- Produces: `AccountLevelState(Level, CurrentLevelExperience, NextLevelRequirement, TotalExperience, IsMaximumLevel)`.

- [ ] **Step 1: Write failing account progression tests**

```csharp
[TestCase(0f, 0, false, false, 0)]
[TestCase(900f, 800, true, false, 600)]
[TestCase(42f, 21, false, true, 3)]
public void RewardUsesApprovedFormula(float seconds, int kills, bool victory, bool abandoned, int expected)
{
    var run = new RunSettlement(new Dictionary<WeaponId, int>(), 0, kills, seconds, victory, abandoned);
    Assert.That(AccountProgression.RewardFor(run), Is.EqualTo(expected));
}

[Test]
public void LevelTwentyStartsAtTwelveThousandNineHundredFiftyEight()
{
    Assert.That(AccountProgression.TotalExperienceForLevel(20), Is.EqualTo(12958));
}
```

음수 입력 정규화, 레벨 1 요구치 100, 250 누적 경험치가 레벨 3의 8/188인지, 최대 레벨 고정, 덧셈 오버플로 거부를 각각 독립 테스트한다.

- [ ] **Step 2: Run RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.AccountProgressionTests
```

Expected: 새 타입이 없어 컴파일 또는 테스트가 실패한다.

- [ ] **Step 3: Implement minimal domain types**

```csharp
public readonly struct AccountLevelState
{
    public int Level { get; }
    public int CurrentLevelExperience { get; }
    public int NextLevelRequirement { get; }
    public int TotalExperience { get; }
    public bool IsMaximumLevel { get; }
}

public static class AccountProgression
{
    public const int MaximumLevel = 100;
    public static int RewardFor(RunSettlement settlement);
    public static int RequiredForNextLevel(int level);
    public static int TotalExperienceForLevel(int level);
    public static AccountLevelState StateFor(int totalExperience);
    public static bool TryAdd(int currentExperience, int reward, out int nextExperience);
}
```

최대 레벨 누적치로 값을 고정하고 화면 갱신 때만 계산한다.

- [ ] **Step 4: Run focused tests GREEN**
- [ ] **Step 5: Stage only the four listed files, commit `feat: add account experience progression`, and push `origin master`**

---

### Task 2: Training Rank 20 and Account-Gated Capacity

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/CommonTrainingProgression.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Progression/EquipmentProgression.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/CommonTrainingProgressionTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/PatrolLoadoutGameplayTests.cs`

**Interfaces:**
- Consumes: `SaveDataV1.AccountExperience`, `AccountProgression.StateFor`.
- Produces: `MaximumRankPerTrack`, `MaximumTotalRanks`, `Rank`, `TotalRanks`, `Capacity`, `CostForRank`, `BonusForRank`, `DamageTakenMultiplier`, `NextCapacityLevel`.
- Produces: `ProgressionError.AccountLevelRequired`.

- [ ] **Step 1: Write failing cost, effect, and capacity tests**

```csharp
[TestCase(1, 100)]
[TestCase(5, 600)]
[TestCase(6, 643)]
[TestCase(10, 975)]
[TestCase(15, 1750)]
[TestCase(20, 2925)]
public void CostForRankUsesApprovedCurve(int rank, int expected) =>
    Assert.That(CommonTrainingProgression.CostForRank(rank), Is.EqualTo(expected));

[TestCase(0, 0f)]
[TestCase(5, .10f)]
[TestCase(10, .13f)]
[TestCase(20, .15f)]
public void BonusForRankUsesDiminishingReturns(int rank, float expected) =>
    Assert.That(CommonTrainingProgression.BonusForRank(rank), Is.EqualTo(expected).Within(.0001f));
```

계정 레벨 1에서 여섯 번째 총 구매를 무변경 거부, 계정 레벨 20에서 총 100강 허용, 트랙 20강 거부, 한 트랙 총비용 24,700 환급, 수호 20강 피해 배율 0.85를 별도 테스트한다.

- [ ] **Step 2: Run `CommonTrainingProgressionTests` RED**
- [ ] **Step 3: Implement the approved rules**

```csharp
public const int MaximumRankPerTrack = 20;
public const int MaximumTotalRanks = 100;
public int TotalRanks { get; }
public int Capacity => Math.Min(AccountProgression.StateFor(data.AccountExperience).Level * 5, 100);
public int NextCapacityLevel => Math.Min(20, TotalRanks / 5 + 1);
public static int CostForRank(int oneBasedRank);
public static float BonusForRank(int rank);
public float DamageTakenMultiplier();
```

`Purchase`는 트랙 최대치, 총 한도, 엽전, 오버플로 순으로 검사한다. `FirstPlayableController`는 기존 10% 하한 대신 `DamageTakenMultiplier()`를 사용한다.

- [ ] **Step 4: Run focused EditMode GREEN, then PlayMode `PatrolLoadoutGameplayTests` GREEN**
- [ ] **Step 5: Stage only listed files, commit `feat: expand account-gated common training`, and push**

---

### Task 3: Save Schema 3 and Backward-Compatible Migration

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/ProjectIdentity.cs`
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/SaveDataV1.cs`
- Modify: `Assets/JoseonHunter/Scripts/Infrastructure/Save/JsonSaveRepository.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/SaveRecoveryTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/BootstrapLoadingPlayModeTests.cs`

**Interfaces:**
- Consumes: account curve and total common-training ranks.
- Produces: `SaveDataV1.AccountExperience`, schema 3 round trip, schema 1·2 migration.

- [ ] **Step 1: Write failing migration and copy tests**

```csharp
[Test]
public void SchemaTwoTrainingMigratesToEnoughAccountExperienceWithoutLosingProgress()
{
    // 유효 체크섬 schema 2 fixture의 총 수련을 17강으로 구성한다.
    var loaded = new JsonSaveRepository(directory).Load().Data;
    Assert.That(loaded.SchemaVersion, Is.EqualTo(3));
    Assert.That(AccountProgression.StateFor(loaded.AccountExperience).Level, Is.EqualTo(4));
    Assert.That(loaded.CommonTrainingRanks[CommonTrainingId.Learning.ToString()], Is.EqualTo(2));
}

[Test]
public void SchemaThreeRoundTripPreservesAccountExperience()
{
    var data = SaveDataV1.CreateDefaults();
    data.AccountExperience = 12958;
    var repository = new JsonSaveRepository(directory);
    Assert.That(repository.Save(data).Success, Is.True);
    Assert.That(repository.Load().Data.AccountExperience, Is.EqualTo(12958));
}
```

`Copy`와 `CopyFrom`의 필드 보존도 검증한다. 기존 스키마 기대값은 3으로 갱신한다.

- [ ] **Step 2: Run `MetaSaveMigrationTests` and `SaveRecoveryTests` RED**
- [ ] **Step 3: Implement schema 3**

`ProjectIdentity.SaveSchemaVersion`과 DTO 기본값을 3으로 맞춘다. SaveDocument에 `accountExperience`를 추가한다. 스키마 1·2는 총 수련 `ceil(total/5)`의 최소 레벨(1~20) 시작 누적치로 이관하고, 스키마 3은 0부터 레벨 100 시작 누적치까지 정규화한다. 다른 필드는 기존 overlay/copy 경로를 유지한다.

- [ ] **Step 4: Run migration/recovery EditMode and `BootstrapLoadingPlayModeTests` GREEN**
- [ ] **Step 5: Stage listed files, commit `feat: migrate saves to account progression schema`, and push**

---

### Task 4: Atomic Run Settlement and Result State

한 판 보상은 기존 복사본 저장 경로 안에서 원자적으로 처리하며, 저장 실패 시 라이브 데이터에 어떠한 일부 보상도 남기지 않는다.

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Domain/Save/ISaveRepository.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableUiState.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/MetaSaveMigrationTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/FirstPlayableUiStateTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/RunSettlementLobbyPlayModeTests.cs`

**Interfaces:**
- Consumes: `AccountProgression.RewardFor`, `TryAdd`, `StateFor`.
- Produces: one-time atomic account XP update and `FirstPlayableUiState.AccountExperienceEarned`, `AccountLevelBefore`, `AccountLevelAfter`.

- [ ] **Step 1: Write failing tests**

42초·21처치·포기 정산은 계정 경험치 3을 한 번 저장해야 한다. 저장 실패는 계정 경험치를 포함한 모든 라이브 값을 유지해야 한다. 중복 귀환도 한 번만 지급한다. UI state 생성자 값 보존도 별도 검증한다.

- [ ] **Step 2: Run `MetaSaveMigrationTests`, `FirstPlayableUiStateTests`, and `RunSettlementLobbyPlayModeTests` RED**
- [ ] **Step 3: Extend the existing copy transaction**

`AutoSaveOrchestrator.CommitRun` 복사본 안에서 계정 경험치를 더한다. Controller는 정산 전 레벨과 보상을 캡처하고 저장 성공 후 최종 레벨을 읽는다. `FirstPlayableUiState` 마지막 선택 인자에 아래를 추가해 기존 호출을 보존한다.

```csharp
int accountExperienceEarned = 0,
int accountLevelBefore = 1,
int accountLevelAfter = 1
```

저장 실패 중에는 획득 계정 경험치를 확정 표시하지 않는다.

- [ ] **Step 4: Run the three focused filters GREEN**
- [ ] **Step 5: Stage listed files, commit `feat: award account experience on run settlement`, and push**

---

### Task 5: Lobby Account Header and Training Presentation

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/CommonTrainingPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CommonTrainingLobbyPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/LobbySceneContractTests.cs`

**Interfaces:**
- Consumes: authoritative save data and domain read-only calculations.
- Produces: `Account Badge`, `Account Level`, `Account Name`, `Account Experience Fill`, `Account Experience Text`, `Training Capacity`.

- [ ] **Step 1: Write failing lobby UI tests**

누적 경험치 250인 세션에서 레벨 3, 이름 `요괴 사냥꾼`, 경험치 `8 / 188`, fillAmount `8/188`을 확인한다. 계정 레벨 7·총 수련 24는 `총 수련 24/35 · 계정 7레벨 한도`, `활력 8/20`, 현재 11.8%, 다음 12.4%를 표시해야 한다. 한도 도달 시 버튼 비활성화와 `계정 레벨 8에서 추가 수련이 열립니다`를 확인한다.

- [ ] **Step 2: Run `LobbyNavigationPlayModeTests` and `CommonTrainingLobbyPlayModeTests` RED**
- [ ] **Step 3: Build the account header**

기존 좌측 제목을 계정 배지·이름·초록 진행 바로 바꾸고 우측 엽전 아이콘과 숫자는 유지한다. `RefreshHeader`에서만 텍스트와 fill을 갱신하며 Update를 추가하지 않는다.

- [ ] **Step 4: Expand training presenter**

총 단계/한도, 각 트랙 `n/20`, 현재·다음 효과, 비용, 강화 후 잔액을 표시한다. 트랙 최대와 계정 한도를 구분해 버튼/안내를 갱신하고 `호신`을 설계 명칭 `수호`로 통일한다.

- [ ] **Step 5: Run both PlayMode filters and EditMode `LobbySceneContractTests` GREEN**
- [ ] **Step 6: Stage listed files, commit `feat: show account growth in lobby`, and push**

---

### Task 6: Run Result Account Progression Presentation

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/RunResultPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/CombatHudPlayModeTests.cs`

**Interfaces:**
- Consumes: new account fields in `FirstPlayableUiState`.
- Produces: `계정 경험치 +N`, `계정 레벨 A → B`, and `계정 레벨 100 · 최대`.

- [ ] **Step 1: Write failing result copy tests**

계정 경험치 420과 레벨 7→8을 표시한다. 레벨 불변은 변화 줄을 생략하고, 최대 레벨은 최대 문구를 표시하며, 저장 실패는 확정 획득 문구를 숨긴다.

- [ ] **Step 2: Run `CombatHudPlayModeTests` RED**
- [ ] **Step 3: Append concise Korean result lines without changing panel/button structure**
- [ ] **Step 4: Run the focused PlayMode test GREEN**
- [ ] **Step 5: Stage two files, commit `feat: show account experience in run results`, and push**

---

### Task 7: Full Validation and Handoff

**Files:**
- Modify: `Docs/AI/UnityProjectContext.md`
- Create: `Docs/Verification/2026-08-06-account-level-training-expansion.md`

**Interfaces:**
- Consumes: all completed tasks and Unity XML/log evidence.
- Produces: reproducible verification record and current project handoff.

- [ ] **Step 1: Run full EditMode**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

- [ ] **Step 2: Run full PlayMode**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode
```

기존 문서의 deferred weapon-potential 실패가 남으면 정확한 동일 실패 수를 기록하고 이번 기능 필터가 모두 green임을 별도로 증명한다.

- [ ] **Step 3: Run batch compile/load with BelowNormal priority and affinity 15**
- [ ] **Step 4: Review `git diff --check`, `git status --short`, and all changed code/tests**
- [ ] **Step 5: Confirm no scene, prefab, ProjectSettings, unrelated asset, or pre-existing dirty `.meta` entered the commits**
- [ ] **Step 6: Record exact pass/fail counts, migration evidence, UI behavior, and remaining device-only validation**
- [ ] **Step 7: Commit `docs: verify account progression expansion` and push**
- [ ] **Step 8: Confirm `HEAD == origin/master` and only pre-existing user `.meta` changes remain**
