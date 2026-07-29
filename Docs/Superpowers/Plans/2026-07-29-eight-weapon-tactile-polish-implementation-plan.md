# 8종 무기 전투 손맛 폴리싱 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 픽셀 접촉 판정과 8종 무기 메커니즘을 보존하면서 각 무기의 발사·이동·명중·레벨 성장·최종 진화를 고유한 픽셀 연출로 완성한다.

**Architecture:** 공격 실행기는 피해 판정과 상태 전이를 계속 소유하고, 새 `WeaponTransientVisualPool`은 판정에 관여하지 않는 단기 스프라이트 연출만 풀링한다. 무기 카탈로그의 `PresentationSprites`는 고정된 파트 인덱스 계약으로 개별 PNG 프레임을 공급하며, `ConfirmedDamageEvent` 이후에만 접촉점 명중 이펙트와 피해 숫자가 나타난다.

**Tech Stack:** Unity 6000.5.5f1, C# 9, Unity 2D SpriteRenderer, NUnit EditMode/PlayMode, official Unity MCP, PixelLab API

## Global Constraints

- 대상 프로젝트는 `D:\UnityProjects\JoseonHunter`이다.
- Android 세로 화면 기준 해상도는 360×800이다.
- 모든 피해는 기존 `CombatDamageService`와 픽셀 접촉 판정을 통해서만 발생한다.
- 일반 명중은 카메라 흔들림과 히트 스톱을 사용하지 않는다.
- 새 픽셀 PNG는 Point 필터, PPU 32, mipmap 비활성화, 무압축, 투명 배경을 사용한다.
- 새 제작물은 한 PNG에 한 프레임 또는 한 독립 에셋만 둔다.
- PixelLab 프레임 수는 크레딧이 아니라 실제 재생 품질을 기준으로 정한다.
- 기존 사용자의 dirty worktree 변경은 수정·스테이징·삭제하지 않는다.
- 생산용 사운드, 로비 개편, 슬롯머신 전체 재구현은 이 계획의 범위가 아니다.

---

## 파일 구조

### 새 파일

- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponVisualCue.cs`  
  무기 연출 단계, 레벨, 진화 상태를 값 형식으로 표현한다.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponTransientVisualPool.cs`  
  단기 SpriteRenderer 생성·재생·반환을 한곳에서 관리한다.
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponVisualPartIndex.cs`  
  각 무기의 `PresentationSprites` 파트 인덱스 계약을 정의한다.
- `Assets/JoseonHunter/Tests/EditMode/WeaponVisualCueTests.cs`  
  레벨별 크기·수명·강도 규칙을 검증한다.
- `Assets/JoseonHunter/Tests/PlayMode/WeaponTransientVisualPoolPlayModeTests.cs`  
  풀 재사용과 수명 종료를 검증한다.
- `Assets/JoseonHunter/Tests/EditMode/WeaponPolishPixelAssetContractTests.cs`  
  새 PNG의 개별 파일, PPU, 필터, 압축, mipmap 계약을 검증한다.
- `Assets/JoseonHunter/Scripts/Editor/Scenes/EightWeaponPolishCapture.cs`  
  8종의 1·3·5레벨·진화를 고정 조건에서 캡처한다.
- `ArtSource/Pixel/Weapons/Polish/pixellab-eight-weapon-polish-ledger.csv`  
  생성 프롬프트, 작업 ID, 채택 경로와 폐기 사유를 기록한다.

### 수정 파일

- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/LinearProjectileExecutor.cs`
- `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetContract.cs`
- `Assets/JoseonHunter/Content/Weapons/HwandoFlyingBlade.asset`
- `Assets/JoseonHunter/Content/Weapons/GakgungShot.asset`
- `Assets/JoseonHunter/Content/Weapons/TalismanThrow.asset`
- `Assets/JoseonHunter/Content/Weapons/ThunderCrashBomb.asset`
- `Assets/JoseonHunter/Content/Weapons/JangseungWard.asset`
- `Assets/JoseonHunter/Content/Weapons/SingijeonVolley.asset`
- `Assets/JoseonHunter/Content/Weapons/FrostFlask.asset`
- `Assets/JoseonHunter/Content/Weapons/WindThunderFan.asset`
- `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- `Assets/JoseonHunter/Tests/EditMode/WeaponPixelAssetContractTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`

---

### Task 1: 공통 무기 연출 값과 단기 스프라이트 풀

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponVisualCue.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponTransientVisualPool.cs`
- Create: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation/WeaponVisualPartIndex.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponVisualCueTests.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponTransientVisualPoolPlayModeTests.cs`

**Interfaces:**
- Produces: `WeaponVisualCue(WeaponId weaponId, WeaponVisualStage stage, int level, bool evolved, float baseScale, float lifetime)`
- Produces: `float WeaponVisualCue.ResolvedScale`
- Produces: `float WeaponVisualCue.ResolvedLifetime`
- Produces: `void WeaponTransientVisualPool.Play(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float lifetime, int sortingOrder)`
- Produces: `void WeaponTransientVisualPool.Tick(float deltaTime)`
- Produces: `void WeaponTransientVisualPool.Dispose()`
- Produces: weapon-specific nested constants under `WeaponVisualPartIndex`
- Produces: `int WeaponVisualPartIndex.RequiredCount(WeaponId weaponId)`

- [ ] **Step 1: Write the failing cue tests**

```csharp
[Test]
public void LevelThreeCue_IsVisiblyStrongerThanLevelOneWithoutDoublingSize()
{
    var one = new WeaponVisualCue(WeaponId.GakgungShot, WeaponVisualStage.Impact, 1, false, 1f, .12f);
    var three = new WeaponVisualCue(WeaponId.GakgungShot, WeaponVisualStage.Impact, 3, false, 1f, .12f);
    Assert.That(three.ResolvedScale, Is.GreaterThan(one.ResolvedScale));
    Assert.That(three.ResolvedScale, Is.LessThan(2f));
}

[Test]
public void EvolvedCue_OutlivesNormalCueButRemainsShort()
{
    var normal = new WeaponVisualCue(WeaponId.ThunderCrashBomb, WeaponVisualStage.Detonation, 5, false, 1f, .2f);
    var evolved = new WeaponVisualCue(WeaponId.ThunderCrashBomb, WeaponVisualStage.Detonation, 5, true, 1f, .2f);
    Assert.That(evolved.ResolvedLifetime, Is.GreaterThan(normal.ResolvedLifetime));
    Assert.That(evolved.ResolvedLifetime, Is.LessThanOrEqualTo(.32f));
}
```

- [ ] **Step 2: Run the focused EditMode tests and verify failure**

Run with Unity Test Framework through the official Unity MCP:

```text
mode: EditMode
test_names:
  - JoseonHunter.Tests.EditMode.WeaponVisualCueTests
```

Expected: compilation fails because `WeaponVisualCue` and `WeaponVisualStage` do not exist.

- [ ] **Step 3: Implement cue resolution and part index contracts**

```csharp
public enum WeaponVisualStage
{
    Windup,
    Projectile,
    Trail,
    Impact,
    Field,
    Detonation
}

public readonly struct WeaponVisualCue
{
    public WeaponVisualCue(
        WeaponId weaponId,
        WeaponVisualStage stage,
        int level,
        bool evolved,
        float baseScale,
        float lifetime)
    {
        WeaponId = weaponId;
        Stage = stage;
        Level = Mathf.Clamp(level, 1, 5);
        Evolved = evolved;
        ResolvedScale = Mathf.Max(.01f, baseScale) *
            (1f + (Level >= 3 ? .12f : 0f) + (Level >= 5 ? .12f : 0f) + (Evolved ? .16f : 0f));
        ResolvedLifetime = Mathf.Min(.32f, Mathf.Max(.04f, lifetime) * (Evolved ? 1.25f : 1f));
    }

    public WeaponId WeaponId { get; }
    public WeaponVisualStage Stage { get; }
    public int Level { get; }
    public bool Evolved { get; }
    public float ResolvedScale { get; }
    public float ResolvedLifetime { get; }
}
```

`WeaponVisualPartIndex`에 다음 고정 인덱스를 선언한다.

```csharp
public static class Gakgung
{
    public const int Projectile = 0;
    public const int Windup = 1;
    public const int Impact = 2;
    public const int Trail = 3;
}
```

나머지 7종도 `Projectile`, `Windup`, `Trail`, `Impact`, `Field`, `Detonation` 중 실제 사용하는 이름만 같은 방식으로 선언한다.

`RequiredCount`는 `WeaponRoster.All`의 여덟 ID를 명시적으로 분기해 각 무기에서 가장 큰 인덱스에 1을 더한 값을 반환하며, 미등록 ID에는 `ArgumentOutOfRangeException`을 던진다.

- [ ] **Step 4: Write the failing pool reuse test**

```csharp
[UnityTest]
public IEnumerator ExpiredVisual_IsReusedWithoutGrowingCreatedCount()
{
    var root = new GameObject("Weapon Visual Test Root").transform;
    var pool = new WeaponTransientVisualPool(root);
    var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
    texture.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 255), 64).ToArray());
    texture.Apply();
    var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 32f);
    pool.Play(sprite, Vector3.zero, Quaternion.identity, Vector3.one, Color.white, .05f, 10);
    pool.Tick(.06f);
    var created = pool.CreatedCount;
    pool.Play(sprite, Vector3.zero, Quaternion.identity, Vector3.one, Color.white, .05f, 10);
    Assert.That(pool.CreatedCount, Is.EqualTo(created));
    pool.Dispose();
    Object.Destroy(root.gameObject);
    yield return null;
}
```

- [ ] **Step 5: Implement the bounded pool**

`WeaponTransientVisualPool`은 활성 목록과 최대 48개의 비활성 스택을 소유한다. `Play`는 비활성 렌더러를 재사용하고, `Tick`은 알파를 남은 수명 비율로 감쇠한 뒤 반환한다. `Dispose`는 생성한 GameObject만 파괴한다.

```csharp
private const int MaximumPooledVisuals = 48;
private readonly List<Entry> active = new List<Entry>();
private readonly Stack<SpriteRenderer> pooled = new Stack<SpriteRenderer>();
public int CreatedCount { get; private set; }
public int ActiveCount => active.Count;
```

- [ ] **Step 6: Run both focused suites**

Expected: `WeaponVisualCueTests` and `WeaponTransientVisualPoolPlayModeTests` pass; Unity Console has no compilation errors.

- [ ] **Step 7: Commit**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/Presentation Assets/JoseonHunter/Tests/EditMode/WeaponVisualCueTests.cs Assets/JoseonHunter/Tests/PlayMode/WeaponTransientVisualPoolPlayModeTests.cs
git commit -m "feat: add pooled weapon presentation runtime"
```

---

### Task 2: PixelLab 개별 프레임 에셋 계약과 생성

**Files:**
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponPolishPixelAssetContractTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetContract.cs`
- Create: `ArtSource/Pixel/Weapons/Polish/pixellab-eight-weapon-polish-ledger.csv`
- Create: `Assets/JoseonHunter/Art/Weapons/Runtime/Polish/<weapon>/<individual-frame>.png`

**Interfaces:**
- Consumes: `WeaponVisualPartIndex`
- Produces: `IReadOnlyList<string> WeaponPixelAssetContract.ValidatePolishFrame(Texture2D texture, TextureImporter importer, string assetPath)`
- Produces: individual PNG assets with stable pivots and PPU 32

- [ ] **Step 1: Write the failing asset contract tests**

```csharp
[TestCase("Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando/hwando_blade.png")]
[TestCase("Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Gakgung/gakgung_arrow.png")]
public void ExistingPolishFrame_UsesMobilePixelImportContract(string path)
{
    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
    Assert.That(WeaponPixelAssetContract.ValidatePolishFrame(texture, importer, path), Is.Empty);
}

[Test]
public void PolishFrame_RejectsSpriteSheetMode()
{
    var importer = AssetImporter.GetAtPath(KnownFixturePath) as TextureImporter;
    importer.spriteImportMode = SpriteImportMode.Multiple;
    importer.SaveAndReimport();
    Assert.That(
        WeaponPixelAssetContract.ValidatePolishFrame(
            AssetDatabase.LoadAssetAtPath<Texture2D>(KnownFixturePath),
            importer,
            KnownFixturePath),
        Does.Contain("polish frame must be a single sprite"));
}
```

- [ ] **Step 2: Run and verify the new API is missing**

Run `WeaponPolishPixelAssetContractTests` in EditMode.

Expected: compilation fails because `ValidatePolishFrame` does not exist.

- [ ] **Step 3: Implement the single-frame contract**

```csharp
public static IReadOnlyList<string> ValidatePolishFrame(
    Texture2D texture,
    TextureImporter importer,
    string assetPath)
{
    var errors = new List<string>();
    if (texture == null) errors.Add("missing polish frame");
    if (importer == null) errors.Add("missing polish frame importer");
    if (errors.Count != 0) return errors;
    ValidateImporter(importer, "polish frame", RequiredPixelsPerUnit, errors);
    if (importer.spriteImportMode != SpriteImportMode.Single)
        errors.Add("polish frame must be a single sprite");
    if (Path.GetExtension(assetPath) != ".png")
        errors.Add("polish frame must be png");
    return errors;
}
```

- [ ] **Step 4: Generate the approved PixelLab batches**

각 프롬프트는 다음 공통 문장을 포함한다.

```text
Joseon folk-fantasy pixel art VFX, transparent background, crisp hard pixel edges,
no anti-aliasing, no text, no UI frame, centered stable pivot, one isolated asset only,
orthographic top-down mobile action game, readable at 360x800, restrained silhouette
```

무기별 필수 생성물:

```text
Hwando: 4 blade flight frames, 4 return afterimages, 4 contact sparks
Gakgung: 3 draw glints, 3 arrow flight frames, 5 impact splinters
Talisman: 4 rotating talismans, 5 seal pulses, 5 binding closures
Thunder: 6 lob rotations, 4 landing warnings, 6 blast rings, 5 ground-current frames
Jangseung: 5 rise frames, 4 ward pulses, 5 guardian strikes
Singijeon: 4 rocket flights, 5 ember trails, 6 focus explosions
Frost: 6 flask rotations, 5 frost growth frames, 6 shatter frames
Fan: 5 gust layers, 4 target marks, 6 lightning strikes
```

모든 결과를 개별 PNG로 저장하고 CSV에 아래 열을 기록한다.

```csv
weapon,stage,frame_index,prompt,job_id,source_path,adopted_path,status,rejection_reason
```

- [ ] **Step 5: Import and bind stable pivots**

Unity 임포터에서 모든 프레임을 `SpriteImportMode.Single`, PPU 32, Point, Uncompressed, mipmap off, readable로 설정한다. 같은 애니메이션 묶음의 캔버스 크기와 피벗이 모두 같아야 한다.

- [ ] **Step 6: Run the complete asset contract suite**

Run:

```text
JoseonHunter.Tests.EditMode.WeaponPixelAssetContractTests
JoseonHunter.Tests.EditMode.WeaponPolishPixelAssetContractTests
JoseonHunter.Tests.EditMode.MobilePixelArtImportTests
```

Expected: all pass.

- [ ] **Step 7: Commit**

```powershell
git add -- ArtSource/Pixel/Weapons/Polish Assets/JoseonHunter/Art/Weapons/Runtime/Polish Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetContract.cs Assets/JoseonHunter/Tests/EditMode/WeaponPolishPixelAssetContractTests.cs
git commit -m "art: add eight weapon polish frames"
```

---

### Task 3: 환도·각궁·신기전 투사체 계열 폴리싱

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/LinearProjectileExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponTransientVisualPool`
- Consumes: `WeaponVisualCue`
- Consumes: `WeaponVisualPartIndex.Hwando`, `.Gakgung`, `.Singijeon`
- Produces: `LinearProjectileSpec.VisualFrameCount`
- Produces: `LinearProjectileSpec.VisualFrameSeconds`
- Produces: `LinearProjectileSpec.VisualPartStart`
- Produces: test-visible projectile position and last impact contact

- [ ] **Step 1: Add failing trajectory and timing tests**

```csharp
[Test]
public void FlyingBlade_LevelFive_UsesDistinctCurvedOutboundAndInboundPositions()
{
    var executor = Rig.CreateFlyingBlade(level: 5);
    executor.Tick(.08f, Rig.Context);
    var outbound = executor.FirstActivePositionForTests;
    executor.Tick(.08f, Rig.Context);
    var later = executor.FirstActivePositionForTests;
    Assert.That(Mathf.Abs(later.Y - outbound.Y), Is.GreaterThan(.01f));
}

[Test]
public void Singijeon_LevelFive_LaunchesAcrossMultipleTicks()
{
    var executor = Rig.CreateSingijeon(level: 5);
    executor.Tick(.01f, Rig.Context);
    var first = executor.ActiveProjectileCount;
    executor.Tick(.06f, Rig.Context);
    Assert.That(executor.ActiveProjectileCount, Is.GreaterThan(first));
}
```

- [ ] **Step 2: Run and verify failure**

Expected: test-visible position property is missing and the existing 신기전 launch count does not grow over the intended stagger window.

- [ ] **Step 3: Polish 환도 movement without changing contact masks**

Use the existing normalized flight progress and add a perpendicular visual offset:

```csharp
var arc = Mathf.Sin(progress * Mathf.PI) * (.10f + .025f * Mathf.Min(4, BladeCount));
var perpendicular = new Float2(-direction.Y, direction.X);
var straight = new Float2(
    Mathf.Lerp(blade.Start.X, blade.End.X, easedProgress),
    Mathf.Lerp(blade.Start.Y, blade.End.Y, easedProgress));
blade.Position = new Float2(
    straight.X + perpendicular.X * arc * blade.ArcSign,
    straight.Y + perpendicular.Y * arc * blade.ArcSign);
```

The hit transform must continue to use `blade.Position`. Level 3 adds staggered arc signs; level 5 uses crossing inbound signs. Spawn contact spark only after `TryApply` succeeds.

- [ ] **Step 4: Polish 각궁 windup and impact**

Keep the arrow smaller than the player, clamp projectile visual scale to `0.72f..1.08f`, show the draw glint for `0.07s`, and rotate impact splinters along the confirmed travel direction. Level 5 side arrows remain ±8 degrees and launch in the same attack beat.

- [ ] **Step 5: Stagger 신기전 launches**

Store pending lane launches with `0.045s` spacing. The first rocket fires immediately; later rockets launch from `Tick` when accumulated time reaches their due time. The focused evolved volley uses `0.035s` spacing and a single shared focus marker.

- [ ] **Step 6: Animate projectile frames through `LinearProjectileExecutor`**

Add frame count, starting part index, and frame duration to `LinearProjectileSpec`:

```csharp
public LinearProjectileSpec(
    AttackInstance attack,
    WeaponId weaponId,
    Float2 position,
    Float2 direction,
    float speed,
    float lifetime,
    int damage,
    int maxImpacts,
    string visualName,
    float scale = 1f,
    bool allowExtendedImpacts = false,
    bool fullDraw = false,
    PixelHitMask potentialMask = null,
    float arcAmplitude = 0f,
    float acceleration = 0f,
    int visualPartStart = 0,
    int visualFrameCount = 1,
    float visualFrameSeconds = .05f)
```

The constructor clamps frame count to at least one and frame seconds to at least `0.01f`. Copy `VisualPartStart`, `VisualFrameCount`, and `VisualFrameSeconds` into the internal `Projectile`, increment its `VisualAge` in `Tick`, and select the pooled renderer frame with:

```csharp
var frame = Mathf.FloorToInt(projectile.VisualAge / projectile.VisualFrameSeconds)
    % projectile.VisualFrameCount;
renderer.sprite = context.PresentationSpriteFor(
    projectile.WeaponId,
    projectile.VisualPartStart + frame);
```

When frame count is one, retain current behavior.

- [ ] **Step 7: Run focused EditMode and PlayMode tests**

Run `WeaponMechanicTests` and the 환도·각궁·신기전 cases in `EightWeaponCombatPlayModeTests`.

Expected: damage counts remain unchanged, contacts still resolve, new trajectory and stagger assertions pass.

- [ ] **Step 8: Commit**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/SingijeonExecutor.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/LinearProjectileExecutor.cs Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs
git commit -m "feat: polish projectile weapon rhythm"
```

---

### Task 4: 부적·뇌진폭탄·빙결병 상태형 공격 폴리싱

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponTransientVisualPool`
- Consumes: `WeaponVisualPartIndex.Talisman`, `.Thunder`, `.Frost`
- Produces: state-matched frame indices and test-visible active visual stage

- [ ] **Step 1: Add failing state-to-visual tests**

```csharp
[Test]
public void ThunderBomb_LobHeightPeaksBeforeFuse()
{
    var executor = Rig.CreateThunderBomb(level: 3);
    executor.Tick(.05f, Rig.Context);
    var early = executor.FirstBombVisualHeightForTests;
    executor.Tick(executor.LobDuration * .45f, Rig.Context);
    Assert.That(executor.FirstBombVisualHeightForTests, Is.GreaterThan(early));
}

[Test]
public void FrostField_LevelFive_GrowsBeforeItShatters()
{
    var executor = Rig.CreateFrostFlask(level: 5);
    Rig.AdvanceUntilField(executor);
    var start = executor.LastFieldVisualScale;
    executor.Tick(executor.Duration * .5f, Rig.Context);
    Assert.That(executor.LastFieldVisualScale, Is.GreaterThan(start));
}
```

- [ ] **Step 2: Run and verify failure**

Expected: the thunder visual height property is missing and the non-potential frost field does not expose the intended growth.

- [ ] **Step 3: Animate 부적 flight, attachment, and closure**

Use a quadratic Bézier path between targets with a perpendicular control point. Rotate through four flight frames, freeze on the attached frame, then play five pulse frames while sealing. Only create an impact seal after confirmed contact.

- [ ] **Step 4: Animate 뇌진폭탄 lob and staged blast**

Render height uses `4 * t * (1 - t) * .55f` while logical X/Y follows the existing lob. A shadow stays on logical ground position. Fuse warning plays four frames; blast ring plays six frames; secondary current begins after the primary ring instead of on the same frame.

- [ ] **Step 5: Animate 빙결병 and field growth**

The flask cycles six rotation frames in flight. On landing, play glass fragments once, then grow the field from `0.65` to `1.0` over `0.18s`. Level 5 shatter warning begins `0.12s` before spikes. The field mask uses the current resolved field radius, not the decorative fragment size.

- [ ] **Step 6: Run state and evolved tests**

Run:

```text
WeaponMechanicTests
EightWeaponCombatPlayModeTests
EvolvedWeaponCombatPlayModeTests
```

Expected: original damage/state order assertions pass and new visual-stage assertions pass.

- [ ] **Step 7: Commit**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/TalismanExecutor.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/ThunderBombExecutor.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FrostFlaskExecutor.cs Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs
git commit -m "feat: polish stateful weapon presentations"
```

---

### Task 5: 장승진·풍뢰선 영역형 공격 폴리싱

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponTransientVisualPool`
- Consumes: `WeaponVisualPartIndex.Jangseung`, `.Fan`
- Produces: `float FirstWardVisualRiseForTests`
- Produces: `IReadOnlyList<float> LightningPresentationTimesForTests`

- [ ] **Step 1: Add failing rise and strike cadence tests**

```csharp
[Test]
public void JangseungWard_RisesBeforeBoundaryBecomesFullyVisible()
{
    var executor = Rig.CreateJangseung(level: 3);
    executor.Tick(.04f, Rig.Context);
    Assert.That(executor.FirstWardVisualRiseForTests, Is.InRange(0f, 1f));
}

[Test]
public void WindThunderFan_LightningPresentationMatchesDamageCadence()
{
    var executor = Rig.CreateFan(level: 5);
    Rig.ResolveFanCast(executor);
    CollectionAssert.AreEqual(
        executor.LastOutboundStrikeTimes,
        executor.LightningPresentationTimesForTests);
}
```

- [ ] **Step 2: Run and verify failure**

Expected: both test-visible presentation properties are missing.

- [ ] **Step 3: Implement 장승 three-stage placement**

Each post plays five rise frames over `0.16s`. Boundary alpha grows only after the second frame. Level 5 activates four directions at `0.10s` intervals. Evolved closure plays the guardian strike only after all active boundaries have completed their contact checks.

- [ ] **Step 4: Implement 풍뢰선 gust layers and lightning marks**

Wind uses five translucent layer frames that travel outward without spawning colliders. A successful wind contact creates a four-frame mark. Lightning visuals are scheduled from the same strike times already used for damage and appear at each confirmed contact. The evolved inbound pass uses a reversed frame order.

- [ ] **Step 5: Run focused tests**

Expected: boundary crossing and mark/damage counts remain unchanged; presentation cadence assertions pass.

- [ ] **Step 6: Commit**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/JangseungWardExecutor.cs Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs
git commit -m "feat: polish ward and wind lightning weapons"
```

---

### Task 6: 8종 카탈로그 프레임 연결과 레벨 성장

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Content/Weapons/HwandoFlyingBlade.asset`
- Modify: `Assets/JoseonHunter/Content/Weapons/GakgungShot.asset`
- Modify: `Assets/JoseonHunter/Content/Weapons/TalismanThrow.asset`
- Modify: `Assets/JoseonHunter/Content/Weapons/ThunderCrashBomb.asset`
- Modify: `Assets/JoseonHunter/Content/Weapons/JangseungWard.asset`
- Modify: `Assets/JoseonHunter/Content/Weapons/SingijeonVolley.asset`
- Modify: `Assets/JoseonHunter/Content/Weapons/FrostFlask.asset`
- Modify: `Assets/JoseonHunter/Content/Weapons/WindThunderFan.asset`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponEvolutionCatalogTests.cs`

**Interfaces:**
- Consumes: `WeaponVisualPartIndex`
- Produces: exact ordered `PresentationSprites` arrays for all eight definitions
- Produces: `Sprite ResolveWeaponPresentationSprite(WeaponId id, int partIndex)` without last-frame clamping errors

- [ ] **Step 1: Add failing exact-count and non-null tests**

```csharp
[TestCaseSource(nameof(AllWeaponDefinitions))]
public void PolishPresentationFrames_MatchDeclaredPartCount(WeaponDefinitionAsset definition)
{
    var expected = WeaponVisualPartIndex.RequiredCount(definition.Id);
    Assert.That(definition.PresentationSprites.Count, Is.EqualTo(expected));
    Assert.That(definition.PresentationSprites.All(sprite => sprite != null), Is.True);
}
```

- [ ] **Step 2: Run and verify failure**

Expected: catalog arrays contain only the old representative/secondary parts and do not match the new count.

- [ ] **Step 3: Bind every individual PNG in deterministic order**

For every weapon asset, assign sprites in the exact order declared by `WeaponVisualPartIndex`. Do not use clamp-to-last as a substitute for missing frames.

- [ ] **Step 4: Make missing part indices fail visibly in development**

`ResolveWeaponPresentationSprite` returns the representative sprite only for an invalid request and logs one development-build warning containing weapon ID and requested index. Valid requests always return the exact indexed sprite.

- [ ] **Step 5: Verify level growth is presentation-only where intended**

Ensure levels 1, 3, and 5 pass the executor’s current `Level` into `WeaponVisualCue`. Do not alter catalog damage numbers except where an existing level field is incorrectly ignored.

- [ ] **Step 6: Run content and evolution catalog tests**

Expected: all eight definitions have five gameplay levels, complete frame arrays, and valid evolution presentation assets.

- [ ] **Step 7: Commit**

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Content/Weapons Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs Assets/JoseonHunter/Tests/EditMode/WeaponEvolutionCatalogTests.cs
git commit -m "content: bind eight weapon polish frames"
```

---

### Task 7: 8종 고정 캡처 도구와 모바일 가독성 검증

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Editor/Scenes/EightWeaponPolishCapture.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs`
- Create: `Artifacts/WeaponPolish/<weapon>-level-<level>.png`

**Interfaces:**
- Produces: Unity menu `Tools/Joseon Hunter/Capture/Eight Weapon Polish`
- Produces: one 360×800 capture for levels 1, 3, 5, and evolved state per weapon

- [ ] **Step 1: Add a failing capture matrix unit test**

```csharp
[Test]
public void CaptureMatrix_ContainsEveryWeaponAtRequiredGrowthStates()
{
    var cases = EightWeaponPolishCapture.BuildCases();
    foreach (var weapon in WeaponRoster.All)
        foreach (var state in new[] { "level-1", "level-3", "level-5", "evolved" })
            Assert.That(cases.Any(item => item.WeaponId.Equals(weapon) && item.Label == state), Is.True);
}
```

- [ ] **Step 2: Implement deterministic capture cases**

`BuildCases()` returns 32 cases. The capture routine opens `Gameplay`, sets portrait resolution 360×800, grants exactly one selected weapon, forces the requested level/evolution, spawns stationary standard targets at fixed near/mid/far points, then captures after the first complete attack cycle.

- [ ] **Step 3: Run the capture matrix test**

Expected: pass with 32 unique cases.

- [ ] **Step 4: Capture all states**

Run the Unity menu through official Unity MCP. Store captures under `Artifacts/WeaponPolish`; do not add them to the player build.

- [ ] **Step 5: Inspect visual criteria**

For every capture confirm:

```text
projectile smaller than player
player and nearest threats remain readable
impact appears at the contacted target
level 1, 3, and 5 silhouettes differ
evolved rhythm and screen shape differ from level 5
no opaque persistent effect covers the central movement lane
```

When a criterion fails, fix the responsible executor or frame and recapture only that weapon’s four states.

- [ ] **Step 6: Commit**

```powershell
git add -- Assets/JoseonHunter/Scripts/Editor/Scenes/EightWeaponPolishCapture.cs Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs
git commit -m "test: add eight weapon polish capture matrix"
```

---

### Task 8: 최종 회귀 검증과 정리

**Files:**
- Modify only files required by failures found in this task.

**Interfaces:**
- Consumes: all previous tasks
- Produces: compile-clean Unity project and passing focused weapon suites

- [ ] **Step 1: Clear Console and trigger Unity compilation**

Use official Unity MCP to refresh assets and wait until the editor is idle.

Expected: zero compiler errors.

- [ ] **Step 2: Run the complete focused EditMode set**

```text
WeaponVisualCueTests
WeaponMechanicTests
WeaponContentTests
WeaponEvolutionCatalogTests
WeaponPixelAssetContractTests
WeaponPolishPixelAssetContractTests
MobilePixelArtImportTests
```

Expected: all pass.

- [ ] **Step 3: Run the complete focused PlayMode set**

```text
WeaponTransientVisualPoolPlayModeTests
EightWeaponCombatPlayModeTests
EvolvedWeaponCombatPlayModeTests
CombatFeedbackDirectorPlayModeTests
```

Expected: all pass.

- [ ] **Step 4: Run a 60-second Gameplay smoke session**

Use a loadout containing 환도, 각궁, 뇌진폭탄, 풍뢰선 first, then a loadout containing 부적, 장승, 신기전, 빙결병. Confirm damage numbers only appear on confirmed contacts and upgrade-choice pause is never released by hit stop.

- [ ] **Step 5: Inspect pool and error state**

During the smoke run confirm:

```text
WeaponTransientVisualPool.ActiveCount returns near zero after attacks expire
no MissingReferenceException
no NullReferenceException
no presentation part fallback warnings
no persistent camera offset after hit stop
```

- [ ] **Step 6: Review the final scoped diff**

Run:

```powershell
git status --short
git diff --check HEAD~7..HEAD
git diff --stat HEAD~7..HEAD
```

Verify that unrelated `.meta`, font, TextMesh Pro, project setting, and user scene changes are not included.

- [ ] **Step 7: Commit any verification-only fix**

If Task 8 required a scoped fix:

```powershell
git add -- Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponPixelAssetContract.cs Assets/JoseonHunter/Scripts/Editor/Scenes/EightWeaponPolishCapture.cs Assets/JoseonHunter/Content/Weapons Assets/JoseonHunter/Tests/EditMode/WeaponVisualCueTests.cs Assets/JoseonHunter/Tests/EditMode/WeaponPolishPixelAssetContractTests.cs Assets/JoseonHunter/Tests/EditMode/WeaponMechanicTests.cs Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs Assets/JoseonHunter/Tests/EditMode/WeaponEvolutionCatalogTests.cs Assets/JoseonHunter/Tests/PlayMode/WeaponTransientVisualPoolPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs Assets/JoseonHunter/Tests/PlayMode/EvolvedWeaponCombatPlayModeTests.cs Assets/JoseonHunter/Art/Weapons/Runtime/Polish ArtSource/Pixel/Weapons/Polish
git commit -m "fix: resolve weapon polish verification issues"
```

If no fix was required, do not create an empty commit.
