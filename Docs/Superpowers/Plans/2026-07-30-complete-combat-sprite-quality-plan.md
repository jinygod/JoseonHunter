# Complete Combat Sprite Quality Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace every legacy sprite visible in the current combat loop with the approved simplified Joseon mobile-survivor art style.

**Architecture:** Preserve all current Unity asset paths, GUIDs, frame prefixes, and runtime presentation systems. PixelLab source jobs produce bounded final packs outside `Assets`; an editor rebuild command imports selected frames and rebuilds the existing motion library without saving the gameplay scene.

**Tech Stack:** Unity 6000.5.5f1, C#, NUnit, PixelLab Pixen/Pixflux and loose-sprite animation.

## Global Constraints

- Normal and weapon frames are 96×96 at PPU 64.
- Elite frames are 112×112 and boss frames are 128×128 at PPU 64.
- Pickups are 64×64 at PPU 64.
- Every runtime frame is an individual transparent PNG.
- Existing paths, GUIDs, prefix names, and exact weapon frame counts remain stable.
- Point filter, no mipmaps, uncompressed.
- Generate and download in batches of at most four jobs.

---

### Task 1: Full runtime contract

**Files:**
- Modify: `Assets/JoseonHunter/Tests/EditMode/SimplifiedPixelArtContractTests.cs`
- Modify: `Assets/JoseonHunter/Scripts/Editor/AssetProduction/SimplifiedPixelArtPackBuilder.cs`

- [ ] Add failing cases for all motion-library combatants, pickups, and weapon prefixes.
- [ ] Run the focused EditMode fixture and record the legacy failures.
- [ ] Extend the rebuild tool with every accepted runtime root.

### Task 2: Remaining combatants and pickups

**Files:**
- Replace existing PNGs under `Assets/JoseonHunter/Art/StaticSprites/Runtime`
- Replace/add animation PNGs under `Assets/JoseonHunter/Art/Animation`
- Add production sources under `ArtSource/Pixel/SimplifiedQuality/CompletePack`

- [ ] Generate one stable base per remaining combatant and pickup.
- [ ] Inspect every base at original resolution.
- [ ] Generate the contracted idle and walk loops.
- [ ] Reject identity-changing frames and repeat only the affected job.
- [ ] Copy accepted frames to established runtime paths.

### Task 3: Remaining seven weapons

**Files:**
- Replace PNGs under `Assets/JoseonHunter/Art/Weapons/Runtime/Polish`
- Add production sources and provenance under `ArtSource/Pixel/SimplifiedQuality/CompletePack`

- [ ] Generate one base for every existing weapon prefix.
- [ ] Use loose-sprite animation for flames, wind, lightning, growth, and explosions.
- [ ] Repeat rigid arrow, rocket, flask, bomb, and talisman bases where Unity already supplies motion.
- [ ] Preserve every prefix's exact current frame count and filename ordering.

### Task 4: Unity integration

**Files:**
- Modify: `Assets/JoseonHunter/Content/Motion/CombatMotionLibrary.asset`
- Modify only relevant `.meta` files through Unity reimport.

- [ ] Reimport the complete pack through the editor rebuild command.
- [ ] Rebuild the motion library without opening or saving the gameplay scene.
- [ ] Confirm no missing sprite references or compile errors.

### Task 5: Verification

- [ ] Run `SimplifiedPixelArtContractTests`, motion-library tests, scale tests, and weapon asset contract tests.
- [ ] Run combatant visual, eight-weapon combat, and evolved-weapon PlayMode tests.
- [ ] Capture a fresh 720×1280 gameplay frame.
- [ ] Inspect the capture for size hierarchy, legacy sprites, weapon clarity, and background separation.
- [ ] Stage only intended files and commit the complete pack.

