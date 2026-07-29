# Weapon Affix Micro-Slot Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a fast, readable three-line weapon-affix slot reveal with purpose-built PixelLab assets and deterministic unscaled timing.

**Architecture:** A pure timeline helper defines presentation phases independently of rendering. The presenter consumes the already-rolled result, cycles anticipation symbols, locks final sprites in sequence, and completes without touching progression state. Individual point-filtered PNG assets are imported through the existing affix catalog pipeline.

**Tech Stack:** Unity 6000.5, C#, uGUI, TextMeshPro, Unity Test Framework, PixelLab pixel-art generation.

## Global Constraints

- Slot reveal runs only for weapon acquisition and weapon upgrade.
- Standard result target duration is `0.86s`; maximum three-line jackpot duration is `1.96s`.
- Presentation must never reroll or mutate `WeaponAffixRollResult`.
- All timing uses `Time.unscaledDeltaTime`.
- Each generated PNG contains exactly one transparent-background asset.
- Preserve unrelated dirty Unity `.meta`, font, ArtSource, and ProjectSettings files.

---

### Task 1: Deterministic Reveal Timeline

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixRevealTimelineTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Consumes: `WeaponAffixRollResult`.
- Produces: `WeaponAffixRevealTimeline.For(WeaponAffixRollResult)`, `Duration`, `SpinEndsAt`, `AffixStopsAt`, `PotentialStopsAt(int)`, `ReadStartsAt`, `CloseStartsAt`, and `SkipFinishAt(float elapsed)`.

- [ ] **Step 1: Write failing exact-boundary tests**

Add cases asserting durations `.86f`, `1.08f`, `1.28f`, `1.38f`, `1.66f`, and `1.96f`; assert the affix stop occurs after spin and each awarded potential stop is strictly later than the previous stop.

- [ ] **Step 2: Run the EditMode test filter**

Run `WeaponAffixRevealTimelineTests` through Unity Test Runner. Expected: compilation failure because the timeline type does not exist.

- [ ] **Step 3: Implement the immutable timeline**

Create a readonly value type whose `For` factory chooses exact phase boundaries from tier and potential count. Clamp `PotentialStopsAt` to indices `0..2`; return `float.PositiveInfinity` for a line not awarded. `SkipFinishAt` must never return earlier than the affix stop plus `0.18f`, and three-line results must preserve at least `0.62f` total presentation.

- [ ] **Step 4: Run the focused tests**

Expected: all timeline tests pass and the existing roll-result identity tests still compile.

- [ ] **Step 5: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealTimeline.cs* Assets/JoseonHunter/Tests/EditMode/WeaponAffixRevealTimelineTests.cs* Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs
git commit -m "test: define affix slot reveal timeline"
```

---

### Task 2: Purpose-Built PixelLab Slot Asset Set

**Files:**
- Create: `ArtSource/Pixel/UI/AffixJackpot/MicroSlot/*.png`
- Create: `ArtSource/Pixel/UI/AffixJackpot/MicroSlot/prompt.md`
- Create: `ArtSource/Pixel/UI/AffixJackpot/MicroSlot/provenance.json`
- Create/import: `Assets/JoseonHunter/Art/UI/AffixJackpot/MicroSlot/*.png`
- Modify: `Assets/JoseonHunter/Scripts/Content/Weapons/WeaponAffixPresentationCatalogAsset.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponAffixPixelAssetImporter.cs`
- Modify: `Assets/JoseonHunter/Tests/EditMode/WeaponAffixPixelAssetContractTests.cs`

**Interfaces:**
- Produces catalog properties `SlotMachineShell`, `ReelWindow`, `LockedPotentialSlot`, `ReelSymbolStat`, `ReelSymbolRarity`, `ReelSymbolPotential`, `ReelStopFlash`, and `JackpotBurstFor(int)`.

- [ ] **Step 1: Write failing catalog contract tests**

Assert every new property is non-null after `EnsureImported`, each source texture is a single sprite with point filtering, no mipmaps, uncompressed default platform settings, and dimensions at least `48 x 48`.

- [ ] **Step 2: Generate the controlled PixelLab batch**

Generate the eight functional assets plus three jackpot bursts using one consistent prompt: compact Joseon occult slot mechanism, dark iron and brass, jade glow, red talisman accent, hard pixel edges, transparent background, no letters, no human figure, no scenery. Use one PNG per object and inspect every candidate at original resolution.

- [ ] **Step 3: Copy approved sources and record provenance**

Record PixelLab job ID, prompt, seed when available, generation timestamp, source dimensions, and approved runtime destination. Do not retain rejected candidates in runtime folders.

- [ ] **Step 4: Extend importer and catalog**

Replace the old five-part slot contract with the new eleven-sprite contract. Keep rarity frames and 24 weapon-potential result sprites unchanged.

- [ ] **Step 5: Run asset contract tests**

Expected: all generated sources and runtime imports resolve, have transparency, point filtering, no compression, and one sprite per PNG.

- [ ] **Step 6: Commit**

```powershell
git add ArtSource/Pixel/UI/AffixJackpot/MicroSlot Assets/JoseonHunter/Art/UI/AffixJackpot/MicroSlot Assets/JoseonHunter/Scripts/Content/Weapons/WeaponAffixPresentationCatalogAsset.cs Assets/JoseonHunter/Scripts/Editor/AssetProduction/WeaponAffixPixelAssetImporter.cs Assets/JoseonHunter/Tests/EditMode/WeaponAffixPixelAssetContractTests.cs
git commit -m "feat: replace affix slot pixel assets"
```

---

### Task 3: Real Micro-Slot Presenter

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs`

**Interfaces:**
- Consumes: Task 1 timeline and Task 2 catalog.
- Preserves: `Play`, `Skip`, `HideImmediately`, `RevealCompleted`, `IsRevealing`, `LastCompletedResult`, and `DurationFor`.
- Produces test-visible `Phase`, `VisiblePotentialCount`, and `IsFinalAffixVisible`.

- [ ] **Step 1: Write failing phase and visibility tests**

Add PlayMode tests proving the final affix is hidden during spin, becomes visible at `AffixStopsAt`, awarded potential rows open sequentially, unawarded rows remain locked, and the panel alpha remains near one throughout the read phase.

- [ ] **Step 2: Replace static panel construction**

Build a safe-area centered shell, a masked primary reel window, three potential windows, separate anticipation/final symbol images, one stop flash, title/detail TMP labels, and a background dimmer. Keep all generated sprites at preserved aspect ratio.

- [ ] **Step 3: Implement phase-driven motion**

During spin, move three anticipation symbols vertically and wrap them within the mask. At each stop boundary, hide anticipation, reveal the actual catalog sprite, apply a `0.92 -> 1.08 -> 1.0` overshoot, and flash the stop highlight. Enable the correct jackpot burst only after the final awarded potential stops.

- [ ] **Step 4: Implement readable opening, hold, and close**

Use eased alpha only during opening and closing. Keep alpha one during spin, stop, and read. Standard has no scale pulse; High/Perfect receives one `1.0 -> 1.045 -> 1.0` pulse; potential jackpots add a maximum `6px` local shake and never move the gameplay camera.

- [ ] **Step 5: Preserve skip and completion contracts**

Skip compresses remaining time through the timeline helper, cannot bypass the affix stop/read minimum, never invokes progression rolling, and fires `RevealCompleted` exactly once.

- [ ] **Step 6: Run focused PlayMode tests**

Expected: timing, visibility, original-result identity, weapon-only routing, queued-upgrade sequencing, pause-safe timing, and idempotent skip all pass.

- [ ] **Step 7: Commit**

```powershell
git add Assets/JoseonHunter/Scripts/Presentation/UI/WeaponAffixRevealPresenter.cs Assets/JoseonHunter/Tests/PlayMode/WeaponAffixRevealPlayModeTests.cs
git commit -m "feat: animate weapon affix micro slot"
```

---

### Task 4: Unity Integration and Mobile Visual Validation

**Files:**
- Modify only if required: `Assets/JoseonHunter/Resources/WeaponAffixPresentationCatalog.asset`
- Create: `Docs/Verification/2026-07-29-weapon-affix-micro-slot-polish.md`

**Interfaces:**
- Consumes all preceding tasks.
- Produces import, compile, console, test, and 1080 x 1920 visual evidence.

- [ ] **Step 1: Run the importer in Unity**

Invoke `WeaponAffixPixelAssetImporter.EnsureImported`, save assets, and wait for compilation. Expected: the Resources catalog contains all new sprites.

- [ ] **Step 2: Clear and inspect compilation errors**

Read Unity Console with stack traces. Fix only new errors introduced by this feature; preserve unrelated working-tree files.

- [ ] **Step 3: Run focused EditMode and PlayMode filters**

Run timeline, asset-contract, and presenter tests once. Record exact pass/fail counts; do not claim success when the runner fails to produce results.

- [ ] **Step 4: Capture a 1080 x 1920 representative reveal**

Force one Standard and one three-line potential result. Verify safe-area containment, crisp point-filtered art, final text readability, correct locked/open cells, and no stretched ornament.

- [ ] **Step 5: Write verification evidence and check the diff**

Run `git diff --check`, list console status, tests executed, capture paths, and any remaining manual check. Ensure only feature files are staged.

- [ ] **Step 6: Commit final integration**

```powershell
git add Assets/JoseonHunter/Resources/WeaponAffixPresentationCatalog.asset Docs/Verification/2026-07-29-weapon-affix-micro-slot-polish.md
git commit -m "chore: validate affix micro slot polish"
```

