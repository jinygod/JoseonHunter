# Lobby Header and Wind-Thunder Fan Clarity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the lobby account/currency/settings header readable, give Wind-Thunder Fan a dedicated crisp icon and larger clear combat presentation, and eliminate unknown affix grades when tier data exists.

**Architecture:** Extend the existing runtime-generated uGUI presenters and weapon ScriptableObject rather than adding a parallel UI or VFX system. PixelLab-created PNGs remain data assets; `LobbyBootstrap`, `WindThunderFanExecutor`, and `WeaponAffixRevealPresenter` continue to own layout, combat presentation, and detail formatting respectively.

**Tech Stack:** Unity 6000.5.5f1, C#, uGUI/TMP, ScriptableObjects, PNG sprite assets, PixelLab MCP, NUnit Unity Test Framework.

## Global Constraints

- Preserve the portrait Android layout and existing `MetaGameSession` save format.
- Preserve Wind-Thunder Fan damage, cooldown, targeting, hit masks, and combat rules.
- Use a limited navy/cyan/light-blue/gold palette without white outer outlines or dense internal texture.
- Preserve existing asset GUIDs where files are replaced; let Unity create metadata only for genuinely new assets.
- Do not stage or modify the pre-existing dirty image `.meta` files or dynamic TMP atlas changes.
- Commit and push each completed task to `origin/master`, as explicitly authorized by the user.

---

### Task 1: Affix Tier Fallback

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponSlotView.GeneralAffixSummary`, `WeaponSlotView.GeneralAffixRolls`, and `WeaponSlotView.GeneralAffixTiers`.
- Produces: `BindEffectRows(string, IReadOnlyList<WeaponAffixRoll>, IReadOnlyList<WeaponAffixTier>, ...)` with a neutral fallback only when no tier exists.

- [ ] **Step 1: Write the failing fallback test**

Add a test that opens details with two summary entries, no rolls, and tiers `Perfect` then `High`, and asserts literal rows `[최대] 재사용 대기시간 -12%` and `[고급] 공격 범위 +15%` while rejecting `등급 미상`.

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.WeaponAffixRevealPlayModeTests
```

Expected: the new assertion fails because summary-only rows currently use `등급 미상`.

- [ ] **Step 3: Implement tier-aware fallback**

Pass `weapon.GeneralAffixTiers` into `BindEffectRows`. Pair split summaries with tier entries by index; use `추가옵션` only when the tier is unavailable.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: all `WeaponAffixRevealPlayModeTests` pass.

- [ ] **Step 5: Commit and push**

Stage the presenter and focused test only, inspect the cached diff, commit with `fix: preserve weapon affix grades in details`, and push `master`.

### Task 2: Lobby Header and Settings Control

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyBootstrap.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/AudioSettingsPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/LobbySceneBuilder.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`
- Create: `Assets/JoseonHunter/Art/UI/Lobby/settings_gear.png`
- Create: `Assets/JoseonHunter/Art/UI/Lobby/settings_gear.png.meta`

**Interfaces:**
- Consumes: existing header named children and `MetaGameSession` account/currency values.
- Produces: an idempotent `ApplyHeaderLayout()` path that works for both freshly built and serialized lobby hierarchies.

- [ ] **Step 1: Write failing layout and icon tests**

Extend lobby PlayMode coverage to assert that the profile block ends before the currency block, currency text is visible and nonzero-width, `Settings Icon` has a non-null sprite, old `Gear Tooth` objects do not exist, and slider handles stay inside their tracks.

- [ ] **Step 2: Run focused lobby tests and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests
```

Expected: the current code-drawn gear and header anchors fail the new contract.

- [ ] **Step 3: Create the gear asset with PixelLab**

Generate a 32×32 transparent pixel-art settings gear using dark navy, muted gold, and a square center hole. Import with point filtering, no mipmaps, no compression, and 32 pixels per unit.

- [ ] **Step 4: Implement idempotent lobby layout**

Create or restyle one compact profile medallion, name, and XP bar on the left; one dark currency capsule with bright coin icon and number on the right; and a separate settings button using the new sprite. Remove or hide legacy gear-tooth children without touching unrelated lobby panels.

- [ ] **Step 5: Correct audio slider geometry**

Keep existing audio behavior but constrain the handle slide area to the 500-pixel track and use a compact gold handle that does not protrude beyond the modal panel.

- [ ] **Step 6: Rebuild serialized lobby assets**

Run `JoseonHunter.Editor.Scenes.LobbySceneBuilder.Build` through Unity batch mode so `LobbyShell.prefab` and `Lobby.unity` use the same layout and sprite references.

- [ ] **Step 7: Run focused tests and verify GREEN**

Run the command from Step 2. Expected: all lobby navigation tests pass.

- [ ] **Step 8: Commit and push**

Stage only the explicit lobby code, test, new gear asset and meta, prefab, and scene; inspect the cached diff before committing and pushing.

### Task 3: Dedicated Wind-Thunder Fan Icon

**Files:**
- Create: `Assets/JoseonHunter/Art/Weapons/Runtime/wind_thunder_fan/ui-icon.png`
- Create: `Assets/JoseonHunter/Art/Weapons/Runtime/wind_thunder_fan/ui-icon.png.meta`
- Modify: `Assets/JoseonHunter/Content/Weapons/WindThunderFan.asset`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponContentTests.cs`

**Interfaces:**
- Consumes: `WeaponDefinitionAsset.UiIcon` and existing `ResolveWeaponSprite` behavior.
- Produces: a non-null, dedicated Wind-Thunder Fan UI sprite distinct from `PresentationSprites[0]`.

- [ ] **Step 1: Write the failing asset contract test**

Load `WindThunderFan.asset` and assert `UiIcon` is non-null, its AssetDatabase path ends in `wind_thunder_fan/ui-icon.png`, and it is not the first presentation sprite.

- [ ] **Step 2: Run the focused EditMode test and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode.WeaponContentTests
```

Expected: `UiIcon` is currently null.

- [ ] **Step 3: Generate the dedicated icon with PixelLab**

Generate a readable opened folding-fan silhouette on transparency, limited to navy, cyan, light-blue, and a tiny gold pivot, with a dark one-to-two-pixel outline and no white halo.

- [ ] **Step 4: Import and assign the icon**

Use point filtering, no mipmaps, no compression, and assign the sprite to `WindThunderFan.asset.uiIcon` while preserving all presentation sprite and mask references.

- [ ] **Step 5: Run focused EditMode tests and verify GREEN**

Run the command from Step 2. Expected: all weapon content tests pass.

- [ ] **Step 6: Commit and push**

Stage only the icon, its meta, the weapon asset, and its test; inspect, commit, and push.

### Task 4: Wind-Thunder Fan Combat Readability

**Files:**
- Replace in place: `Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Fan/fan_gust_01.png` through `fan_gust_05.png`
- Replace in place: `Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Fan/fan_target_01.png` through `fan_target_04.png`
- Replace in place: `Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Fan/fan_lightning_01.png` through `fan_lightning_06.png`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Combat/Weapons/WindThunderFanExecutor.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/EightWeaponCombatPlayModeTests.cs`

**Interfaces:**
- Consumes: existing `WeaponVisualPartIndex.WindThunderFan` ranges and `WeaponTransientVisualPool`.
- Produces: the same 15-frame contract with larger minimum visible scales and clearer limited-palette silhouettes.

- [ ] **Step 1: Write a failing minimum-scale presentation test**

Execute a level-one fan attack against a legal target, inspect spawned fan transient renderers, and assert the gust and impact visible bounds exceed the old tiny footprint while the existing presentation-part sequence remains unchanged.

- [ ] **Step 2: Run the focused PlayMode test and verify RED**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode.EightWeaponCombatPlayModeTests
```

Expected: the current doubly reduced visual scale fails the minimum-size assertion.

- [ ] **Step 3: Generate and replace the 15 PixelLab frames**

Create five broad wind fan frames, four simple target wind marks, and six thick lightning frames using the approved icon palette. Replace PNG contents in place so existing GUIDs remain valid.

- [ ] **Step 4: Remove duplicate scale reduction**

Raise the authored gust and contact-sequence scale inputs so level-one effects remain readable after `WeaponPresentationScale.For`, without modifying hit masks or damage.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run the command from Step 2 and the EditMode `WeaponMechanicTests`. Expected: visual size, frame order, contact, and mechanics all pass.

- [ ] **Step 6: Commit and push**

Stage the 15 PNGs, executor, and test only. Do not stage pre-existing dirty `.meta` files.

### Task 5: Integration, Capture, and Final Validation

**Files:**
- Create: `Docs/Verification/2026-08-08-lobby-header-and-wind-thunder-fan-clarity.md`

**Interfaces:**
- Consumes: all prior tasks.
- Produces: final test and visual evidence.

- [ ] **Step 1: Run full EditMode tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform editmode -Filter JoseonHunter.Tests.EditMode
```

- [ ] **Step 2: Run full PlayMode tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 -Platform playmode -Filter JoseonHunter.Tests.PlayMode
```

- [ ] **Step 3: Capture lobby and Wind-Thunder Fan gameplay**

Use existing editor capture paths where available. Inspect at portrait mobile resolution for header overlap, currency readability, gear silhouette, dedicated fan icon, gust visibility, target mark, lightning impact, and absence of `등급 미상`.

- [ ] **Step 4: Inspect Console and serialized references**

Verify no new compile errors, missing sprites, missing references, or asset import warnings attributable to this change.

- [ ] **Step 5: Write verification record**

Record exact test counts, capture paths, visual findings, PixelLab assets created, and any physical-device limitation.

- [ ] **Step 6: Review and commit final diff**

Check `git diff --check`, `git diff --stat`, and `git status --short`; stage only task files, commit verification evidence, and push `master`.
