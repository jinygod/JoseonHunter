# Mobile Pixel Art Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the undersized temporary combat art with a coherent Han Yeonhwa roster, elite hierarchy, correctly sized weapon visuals, curved attack motion, pixel-perfect mobile rendering, and a polished Joseon occult battlefield.

**Architecture:** PixelLab produces one transparent PNG per independently rendered asset. Existing ScriptableObject catalogs remain the asset source of truth; runtime combat executors retain damage authority while presentation data supplies independently addressable projectile, trail, cue, and impact sprites. Enemy rank becomes explicit runtime state, and URP Pixel Perfect Camera owns display alignment without changing combat positions.

**Tech Stack:** Unity 6000.5.5f1, URP 2D, C#, Unity Test Framework, TextMeshPro, official Unity MCP, PixelLab MCP.

## Global Constraints

- Reference virtual resolution is `360 x 800`; pixel-art assets use `64 PPU`.
- Runtime pixel textures use Point filtering, no mipmaps, single Sprite mode, and no Android compression.
- One PNG file contains exactly one independently rendered asset; runtime sprite sheets and contact sheets are forbidden.
- Han Yeonhwa and normal monsters have similar combat size.
- Elite monsters are `1.20–1.28x` normal display size, `4x` health, `1.5x` contact damage, `0.92x` speed, and `5x` experience.
- The boss is the largest combat silhouette, approximately `1.7–1.9x` Han Yeonhwa's height.
- Hwando blades, arrows, and rockets remain at most `24` reference pixels long and never exceed the heroine body.
- Decorative trails, glows, aim cues, and sparks never enter contact masks.
- Existing attack instance IDs, hit memory, affixes, evolutions, and confirmed-damage authority remain intact.
- Preserve unrelated dirty Unity metadata and user-created scene changes.

---

### Task 1: Enforce the mobile pixel-art import and single-PNG contract

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/MobilePixelArtImportTests.cs`
- Create: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/SinglePngAssetValidator.cs`

**Interfaces:**
- Consumes: asset paths under `Art/StaticSprites/Runtime`, `Art/Weapons/Runtime/Polish`, and `Art/World/Runtime`.
- Produces: `SinglePngAssetValidator.Validate(string assetPath) : IReadOnlyList<string>`.

- [ ] **Step 1: Write failing importer and one-asset tests**

```csharp
[Test]
public void RuntimePolishTextureUsesCrispMobileProfile()
{
    var importer = AssetImporter.GetAtPath(FixturePath) as TextureImporter;
    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
    Assert.That(importer.mipmapEnabled, Is.False);
    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
    Assert.That(importer.GetPlatformTextureSettings("Android").overridden, Is.False);
}

[Test]
public void ValidatorRejectsMultipleOpaqueIslandsMarkedAsIndependentAssets()
{
    Assert.That(SinglePngAssetValidator.Validate(MultiAssetFixture),
        Does.Contain("multiple independent asset islands"));
}
```

- [ ] **Step 2: Run the focused EditMode tests and confirm both fail**

Run: `Tools/Unity/Test-Unity.ps1 -TestFilter "MobilePixelArtImportTests"`  
Expected: FAIL because polish paths and `SinglePngAssetValidator` do not exist.

- [ ] **Step 3: Extend the postprocessor and implement connected-component validation**

```csharp
private static bool IsMobilePixelRuntime(string path) =>
    path.StartsWith(StaticSpriteRuntimeRoot, StringComparison.Ordinal) ||
    path.StartsWith("Assets/JoseonHunter/Art/Weapons/Runtime/Polish/", StringComparison.Ordinal) ||
    path.StartsWith("Assets/JoseonHunter/Art/World/Runtime/", StringComparison.Ordinal);

settings.spritePixelsPerUnit = 64f;
settings.filterMode = FilterMode.Point;
settings.mipmapEnabled = false;
settings.textureCompression = TextureImporterCompression.Uncompressed;
importer.spriteImportMode = SpriteImportMode.Single;
importer.ClearPlatformTextureSettings("Android");
```

`SinglePngAssetValidator` flood-fills nontransparent pixels and permits one principal island plus tiny detached effect pixels belonging to that same named asset; it rejects two separated object-sized islands.

- [ ] **Step 4: Run the focused tests**

Expected: PASS with no new Console errors.

- [ ] **Step 5: Commit**

```text
git add Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs \
        Assets/JoseonHunter/Scripts/Editor/AssetProduction/SinglePngAssetValidator.cs \
        Assets/JoseonHunter/Tests/EditMode/MobilePixelArtImportTests.cs
git commit -m "feat: enforce mobile pixel art asset contract"
```

### Task 2: Generate and preflight the complete PixelLab polish batch

**Files:**
- Create: `Docs/Assets/pixellab-mobile-polish-generation-ledger.csv`
- Create: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png`
- Replace: five PNGs under `Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/`
- Create: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Elites/dokkaebi_captain.png`
- Replace: `Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png`
- Create: individual PNG files under `Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando/`
- Create: individual PNG files under `Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Gakgung/`
- Create: individual PNG files under `Assets/JoseonHunter/Art/World/Runtime/Battlefield/`

**Interfaces:**
- Consumes: approved prompts, PixelLab job IDs, and the art-size contract.
- Produces: validated standalone PNGs and a ledger row `asset_id,job_id,prompt_revision,cost,status,remaining`.

- [ ] **Step 1: Record the starting PixelLab balance**

Call PixelLab balance and write the exact remaining subscription generations to the ledger header.

- [ ] **Step 2: Generate four Han Yeonhwa candidates**

Use PixelLab `create_character` v3 or the best available high-quality character operation with `size=96`, low top-down view, selective ink outline, and four different deterministic seeds. Each candidate must be a complete adult heroine and not a sheet of parts.

- [ ] **Step 3: Select the strongest heroine and generate the roster**

Generate five normal monsters at 64px, the 80px Dokkaebi Captain elite, and the 128px Fallen General boss using the selected heroine's palette and outline as style guidance where supported.

- [ ] **Step 4: Generate standalone weapon parts**

Create exactly these independent transparent PNGs:

```text
Hwando/hwando_blade.png
Hwando/hwando_afterimage.png
Hwando/hwando_contact_spark.png
Gakgung/gakgung_arrow.png
Gakgung/gakgung_aim_glint.png
Gakgung/gakgung_impact_splinter.png
```

- [ ] **Step 5: Generate standalone world tiles and decals**

Create two seamless 64x64 dark soil/hanji tiles and four single-object decals, one object per PNG.

- [ ] **Step 6: Preflight every downloaded file**

Check dimensions, RGBA mode, binary alpha, transparent corners, opaque bounds, silhouette size, island count, palette size, and SHA-256. Reject and regenerate any asset that violates the contract.

- [ ] **Step 7: Import and visually inspect native and 4x nearest-neighbor previews**

No composite review image enters `Assets/`; temporary review boards remain under `Temp/`.

- [ ] **Step 8: Commit approved assets and ledger**

```text
git add Assets/JoseonHunter/Art/StaticSprites/Runtime \
        Assets/JoseonHunter/Art/Weapons/Runtime/Polish \
        Assets/JoseonHunter/Art/World/Runtime \
        Docs/Assets/pixellab-mobile-polish-generation-ledger.csv
git commit -m "art: add mobile pixel polish asset batch"
```

### Task 3: Add normal, elite, and boss enemy rank behavior

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyRankProfile.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- Modify via Unity Editor: `Assets/JoseonHunter/Scenes/Gameplay.unity`
- Create: `Assets/JoseonHunter/Tests/EditMode/EnemyRankProfileTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/FirstPlayableControllerCombatBridgePlayModeTests.cs`

**Interfaces:**
- Produces: `EnemyRank { Normal, Elite, Boss }` and `EnemyRankProfile.For(EnemyRank rank)`.
- `EnemyRankProfile` exposes `DisplayScale`, `HealthMultiplier`, `ContactDamageMultiplier`, `SpeedMultiplier`, and `ExperienceMultiplier`.

- [ ] **Step 1: Write failing exact-value rank tests**

```csharp
[TestCase(EnemyRank.Elite, 1.24f, 4f, 1.5f, .92f, 5)]
[TestCase(EnemyRank.Boss, 1.8f, 1f, 1f, 1f, 1)]
public void Rank_contract_is_stable(EnemyRank rank, float scale, float hp, float damage, float speed, int xp)
{
    var profile = EnemyRankProfile.For(rank);
    Assert.That(profile.DisplayScale, Is.EqualTo(scale));
    Assert.That(profile.HealthMultiplier, Is.EqualTo(hp));
    Assert.That(profile.ContactDamageMultiplier, Is.EqualTo(damage));
    Assert.That(profile.SpeedMultiplier, Is.EqualTo(speed));
    Assert.That(profile.ExperienceMultiplier, Is.EqualTo(xp));
}
```

- [ ] **Step 2: Run focused tests and confirm failure**

- [ ] **Step 3: Implement rank data and propagate elite state**

`EnemyState` stores `EnemyRank Rank`, `int ExperienceReward`, and `bool IsElite => Rank == EnemyRank.Elite`. `PrototypeCombatTarget.IsElite` delegates to state. Elite spawning begins after 12 seconds at 8% and rises to a capped 18%; boss spawning remains explicit.

- [ ] **Step 4: Bind `eliteSprite` to `dokkaebi_captain.png` using Unity serialization**

Do not hand-edit unrelated scene YAML. Re-read the serialized field after the Editor operation.

- [ ] **Step 5: Verify health, size, target priority, reward, reset, and scene reload**

- [ ] **Step 6: Commit**

```text
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/EnemyRankProfile.cs \
        Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs \
        Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs \
        Assets/JoseonHunter/Scenes/Gameplay.unity \
        Assets/JoseonHunter/Tests/EditMode/EnemyRankProfileTests.cs \
        Assets/JoseonHunter/Tests/PlayMode/FirstPlayableControllerCombatBridgePlayModeTests.cs
git commit -m "feat: add elite enemy rank"
```

### Task 4: Install the Han Yeonhwa scale hierarchy and pixel-perfect camera

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs`
- Modify via Unity Editor: `Assets/JoseonHunter/Scenes/Gameplay.unity`
- Create: `Assets/JoseonHunter/Tests/PlayMode/CombatScaleHierarchyPlayModeTests.cs`

**Interfaces:**
- Consumes: Han Yeonhwa, normal, elite, and boss sprite assignments.
- Produces: stable reference heights and a URP `PixelPerfectCamera` configured for 64 PPU and 360x800.

- [ ] **Step 1: Write a failing hierarchy PlayMode test**

```csharp
Assert.That(normalBounds.size.y, Is.InRange(heroBounds.size.y * .85f, heroBounds.size.y * 1.10f));
Assert.That(eliteBounds.size.y, Is.InRange(normalBounds.size.y * 1.20f, normalBounds.size.y * 1.28f));
Assert.That(bossBounds.size.y, Is.InRange(heroBounds.size.y * 1.70f, heroBounds.size.y * 1.90f));
```

- [ ] **Step 2: Bind Han Yeonhwa as the only player sprite**

Update both the scene generator and current Gameplay scene; remove the shaman runtime reference without deleting the source asset.

- [ ] **Step 3: Configure Pixel Perfect Camera**

Set `assetsPPU=64`, `refResolutionX=360`, `refResolutionY=800`, crop/stretch settings compatible with portrait safe areas, and pixel snapping enabled.

- [ ] **Step 4: Replace arbitrary `.3125f` entity scales with named scale constants**

Scale derives from desired opaque bounds and rank profile; no weapon or entity uses canvas size alone as its displayed size.

- [ ] **Step 5: Run the hierarchy test at 360x800 and a representative 1080-wide Game view**

- [ ] **Step 6: Commit**

```text
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs \
        Assets/JoseonHunter/Scripts/Editor/Scenes/FirstPlayableSceneGenerator.cs \
        Assets/JoseonHunter/Scenes/Gameplay.unity \
        Assets/JoseonHunter/Tests/PlayMode/CombatScaleHierarchyPlayModeTests.cs
git commit -m "feat: install pixel perfect combat scale"
```

### Task 5: Replace oversized weapon art and mechanical straight-line motion

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponDefinitionAsset.cs`
- Create: `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponPresentationPart.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/WeaponRuntimeController.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/FlyingBladeExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/LinearProjectileExecutor.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/GakgungExecutor.cs`
- Modify via Unity Editor: `Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset`
- Create: `Assets/JoseonHunter/Tests/PlayMode/WeaponPresentationPolishPlayModeTests.cs`

**Interfaces:**
- Produces: `WeaponDefinitionAsset.SpriteFor(WeaponPresentationPart part)`.
- `WeaponPresentationPart` values: `Primary`, `Trail`, `Cue`, `Impact`.
- `LinearProjectileSpec` gains `float AccelerationFraction` and `float LateralArc`.

- [ ] **Step 1: Write failing visual-size and motion tests**

```csharp
Assert.That(hwandoRenderer.bounds.size.y, Is.LessThan(heroRenderer.bounds.size.y * .5f));
Assert.That(arrowRenderer.bounds.size.x, Is.LessThanOrEqualTo(24f / 64f));
Assert.That(travelSamples.Select(p => p.y).Distinct().Count(), Is.GreaterThan(2));
Assert.That(gakgung.AimCueShownBeforeLastLaunchForTests, Is.True);
```

- [ ] **Step 2: Add named standalone presentation parts to weapon definitions**

Keep the existing array serialized field for compatibility and map indices through the enum. Missing optional parts return null and do not block combat.

- [ ] **Step 3: Polish the hwando**

Use the standalone blade sprite, rotate it from travel direction plus spin, keep the outbound Bézier-like arc, and add the opposite signed return arc. Pool a small afterimage sprite that never participates in contact.

- [ ] **Step 4: Polish Gakgung**

Queue a 0.10-second aim cue, then launch a small arrow. `LinearProjectileExecutor` accelerates during the first 15% of life and adds a deterministic shallow lateral arc while sweeping pixel contacts along the actual curved segment.

- [ ] **Step 5: Spawn standalone impact sprites from confirmed contact**

Impact presentation is pooled, short-lived, and never creates damage.

- [ ] **Step 6: Run existing combat and new presentation tests**

Verify arrows can still miss, contact masks still gate damage, full draw and split fletching remain functional, and no projectile exceeds the visual-size budget.

- [ ] **Step 7: Commit**

```text
git add Assets/JoseonHunter/Scripts/Content/Weapons \
        Assets/JoseonHunter/Scripts/Runtime/Combat \
        Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset \
        Assets/JoseonHunter/Tests/PlayMode/WeaponPresentationPolishPlayModeTests.cs
git commit -m "feat: polish hwando and gakgung presentation"
```

### Task 6: Replace the development grid with a layered occult battlefield

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/World/BattlefieldTilePresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`
- Create: `Assets/JoseonHunter/Tests/PlayMode/BattlefieldTilePresenterPlayModeTests.cs`

**Interfaces:**
- Consumes: two 64x64 seamless tiles and four independent decal PNGs.
- Produces: bounded pooled tiles and decals following the camera without affecting combat.

- [ ] **Step 1: Write failing coverage and pooling tests**

Assert that the visible camera rectangle is covered, tile count remains bounded after camera movement, and decal renderers never have colliders.

- [ ] **Step 2: Implement deterministic tile variation and edge-weighted decals**

Use a stable coordinate hash; keep the combat center sparse and place more decals near screen edges.

- [ ] **Step 3: Remove the generated green rectangle and grid lines**

Do not alter player movement bounds or combat coordinates.

- [ ] **Step 4: Validate readability on light/dark enemy and projectile samples**

- [ ] **Step 5: Commit**

```text
git add Assets/JoseonHunter/Scripts/Presentation/World/BattlefieldTilePresenter.cs \
        Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs \
        Assets/JoseonHunter/Tests/PlayMode/BattlefieldTilePresenterPlayModeTests.cs
git commit -m "feat: add occult battlefield presentation"
```

### Task 7: Final mobile visual and regression validation

**Files:**
- Create: `Docs/Verification/2026-07-28-mobile-pixel-art-polish.md`
- Create temporary captures only under: `Temp/MobilePixelPolishReview/`

**Interfaces:**
- Consumes: completed tasks 1–6.
- Produces: evidence for import settings, visual hierarchy, runtime motion, pixel contact, Console state, and PixelLab usage.

- [ ] **Step 1: Run focused EditMode and PlayMode tests**

Run the new import, rank, hierarchy, weapon presentation, and battlefield tests.

- [ ] **Step 2: Run the existing weapon contact and affix regression suites**

Confirm the eight weapon mechanics, pixel masks, damage numbers, potentials, and evolution behavior remain intact.

- [ ] **Step 3: Capture 360x800 and 1080-wide gameplay**

Capture heroine/normal/elite/boss comparisons, hwando outbound/return curves, Gakgung cue/flight/impact, and a crowded combat frame.

- [ ] **Step 4: Inspect Unity Console**

Record exact error and warning counts; distinguish Unity AI account warnings from first-party errors.

- [ ] **Step 5: Verify the final PixelLab balance and ledger**

Every used generation has a job ID and status; no API secret is persisted.

- [ ] **Step 6: Review final diff**

Confirm no runtime sprite sheet was introduced, every new PNG contains one asset, and unrelated dirty metadata remains untouched.

- [ ] **Step 7: Commit verification evidence**

```text
git add Docs/Verification/2026-07-28-mobile-pixel-art-polish.md
git commit -m "docs: verify mobile pixel art polish"
```
