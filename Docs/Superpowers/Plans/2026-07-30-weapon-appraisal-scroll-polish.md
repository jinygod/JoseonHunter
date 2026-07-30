# Weapon Appraisal Scroll Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a memory-safe PixelLab scroll reveal with distinct pacing for new weapons, repeat upgrades, and rare appraisal results.

**Architecture:** Extend the current appraisal view model and deterministic presentation helpers with a reveal profile. Keep the existing presenter and reward completion API, replace its opening transform with a masked scroll animation, and add optional PixelLab scroll sprites to the existing catalog/importer.

**Tech Stack:** Unity 6000.5.5f1, C#, uGUI, TextMeshPro, PixelLab PNG assets, NUnit EditMode and PlayMode tests.

## Global Constraints

- Preserve all general-affix values, accumulation, jackpot probabilities, potential uniqueness, and the three-line cap.
- Repeat standard results complete automatic motion in 0.90 seconds.
- Potential jackpots remain capped at 2.40 seconds.
- Use one PNG per asset and a `RectMask2D` reveal instead of frame-heavy animation.
- Use `Time.unscaledDeltaTime`; do not allocate presentation objects per frame.
- Preserve the legacy asset fallback and reward completion behavior.

---

### Task 1: Deterministic reveal profiles

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalPresentation.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAppraisalViewModel.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponAppraisalPresentationTests.cs`

**Interfaces:**
- Produces: `WeaponAppraisalRevealProfile`, `ProfileFor(WeaponAppraisalViewModel)`, and `ScrollOpenAt(profile, time)`.
- Consumes: weapon level, reward kind, affix tier, and awarded potential count.

- [ ] Write failing tests for first-acquisition, repeat-standard, and rare-result profiles.
- [ ] Run the focused EditMode tests and confirm the missing API failure.
- [ ] Implement profile selection and eased opening interpolation.
- [ ] Run the focused EditMode tests and confirm all cases pass.

### Task 2: PixelLab scroll asset contract

**Files:**
- Add: `Assets/JoseonHunter/Art/UI/AffixJackpot/Appraisal/*.png`
- Modify: `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponAffixPresentationCatalogAsset.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponAffixPixelAssetImporter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetImport/JoseonAssetPostprocessor.cs`
- Test: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixPixelAssetContractTests.cs`

**Interfaces:**
- Produces: optional `AppraisalScroll`, `AppraisalRoller`, `PotentialRitualSeal`, and `RareAppraisalStamp` sprites.

- [ ] Add failing asset-path and catalog-property tests.
- [ ] Download the four approved PixelLab outputs as separate PNG files.
- [ ] Extend the importer and catalog without renaming existing serialized fields.
- [ ] Let Unity generate metadata, import the assets, and run the contract tests.

### Task 3: Masked scroll reveal and accumulated stat copy

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/FirstPlayableUiBootstrap.cs`
- Test: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Consumes: Tasks 1–2 reveal profile and sprites.
- Produces: a vertically masked scroll, moving rods, rare stamp, ritual overlay, and total-affix summary.

- [ ] Add failing PlayMode tests for scroll opening, repeat timing, and total-summary visibility.
- [ ] Build one `RectMask2D` viewport and reuse the current detail hierarchy beneath it.
- [ ] Drive viewport height and rod positions from `ScrollOpenAt`.
- [ ] Add rare stamp and potential ritual overlay without obscuring weapon information.
- [ ] Run the focused PlayMode tests.

### Task 4: Regression validation and scoped commit

**Files:**
- Modify only if test evidence requires it.

**Interfaces:**
- Produces: Unity XML/log evidence and a scoped feature commit.

- [ ] Run appraisal EditMode tests.
- [ ] Run focused appraisal/rack PlayMode tests.
- [ ] Run eight-weapon combat PlayMode tests.
- [ ] Inspect the intended diff and exclude unrelated imported metadata.
- [ ] Commit the feature on the current non-main branch.

