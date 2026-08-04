# Support Upgrade Icons Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generic support-upgrade `福` glyph with three readable, cohesive pixel-art icons.

**Architecture:** Add a small ID-to-Resources-path sprite resolver dedicated to support upgrades. `FirstPlayableController.BuildUpgradeChoiceView` consumes the resolver, while existing `UpgradeChoicePresenter` fallback behavior remains unchanged for missing assets.

**Tech Stack:** Unity, C#, NUnit Unity Test Framework, PixelLab-generated PNG assets, Resources API

## Global Constraints

- Work on `master`, preserve unrelated dirty files, and push each completed bundle to `origin/master`.
- Use 96×96 transparent pixel art with Point filtering, mipmaps disabled, and no white outline.
- Keep the current passive mechanics and selection probabilities unchanged.
- Keep CPU load controlled by running Unity test processes sequentially at BelowNormal priority and processor affinity `0xF`.

---

### Task 1: Define the support icon contract with failing tests

**Files:**
- Create: `Assets/JoseonHunter/Tests/EditMode/SupportUpgradeIconCatalogTests.cs`
- Modify: `Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs`

**Interfaces:**
- Consumes: current support IDs `talisman`, `boots`, `warding_bell`
- Produces: expected `SupportUpgradeIconCatalog.Resolve(string id) : Sprite` behavior

- [ ] **Step 1: Write an EditMode test for complete, distinct icons**

```csharp
[TestCase("talisman")]
[TestCase("boots")]
[TestCase("warding_bell")]
public void Every_support_upgrade_resolves_a_pixel_art_sprite(string id)
{
    var sprite = SupportUpgradeIconCatalog.Resolve(id);
    Assert.That(sprite, Is.Not.Null);
    Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
}
```

- [ ] **Step 2: Write a PlayMode test for card presentation**

Force three support offers through `FirstPlayableController.OpenUpgradeOffersForTests`, then assert every visible card has an enabled `Icon` image and an inactive `Glyph` object.

- [ ] **Step 3: Run the focused tests and verify RED**

Run the EditMode catalog fixture and PlayMode card fixture sequentially. Expected: compilation failure because `SupportUpgradeIconCatalog` does not exist.

- [ ] **Step 4: Commit the test contract**

```powershell
git add Assets/JoseonHunter/Tests/EditMode/SupportUpgradeIconCatalogTests.cs Assets/JoseonHunter/Tests/PlayMode/UpgradeChoicePlayModeTests.cs
git commit -m "test: define support upgrade icon contract"
git push origin master
```

### Task 2: Generate and import the three PixelLab icons

**Files:**
- Create: `Assets/JoseonHunter/Resources/UI/SupportIcons/talisman.png`
- Create: `Assets/JoseonHunter/Resources/UI/SupportIcons/boots.png`
- Create: `Assets/JoseonHunter/Resources/UI/SupportIcons/warding_bell.png`
- Create: matching Unity `.meta` files through Unity import

**Interfaces:**
- Consumes: visual rules in the design spec
- Produces: `Resources.Load<Sprite>("UI/SupportIcons/<id>")` assets

- [ ] **Step 1: Generate three separate 96×96 PixelLab objects**

Use a shared prompt suffix: `Joseon folk fantasy pixel-art item icon, transparent background, simplified broad silhouette, 3-4 colors, dark ink outline, no white outline, no text, readable at mobile UI size.` Add the item-specific descriptions from the design spec.

- [ ] **Step 2: Inspect every candidate at original resolution**

Reject candidates with white outlines, excessive texture noise, illegible silhouettes, text, or an opaque background. Select one coherent candidate for each ID.

- [ ] **Step 3: Download and import the selected PNG files**

Save the exact files under `Assets/JoseonHunter/Resources/UI/SupportIcons`, start Unity headlessly, and wait for import completion.

- [ ] **Step 4: Verify importer settings**

Confirm each texture is Sprite, Point-filtered, has mipmaps disabled, preserves alpha, and uses no lossy compression.

- [ ] **Step 5: Commit the asset bundle**

```powershell
git add Assets/JoseonHunter/Resources/UI/SupportIcons
git commit -m "art: add support upgrade pixel icons"
git push origin master
```

### Task 3: Resolve icons in support choice views

**Files:**
- Create: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/SupportUpgradeIconCatalog.cs`
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- Produces: `public static Sprite Resolve(string id)`
- Consumes: `Resources/UI/SupportIcons/{talisman,boots,warding_bell}`

- [ ] **Step 1: Implement the resolver**

```csharp
public static Sprite Resolve(string id)
{
    if (string.IsNullOrWhiteSpace(id)) return null;
    return Resources.Load<Sprite>($"UI/SupportIcons/{id}");
}
```

Cache resolved sprites by ID so repeated level-up screens do not repeat path lookups.

- [ ] **Step 2: Connect support choices**

Change the final argument of the support `UpgradeChoiceView` construction from `null` to `SupportUpgradeIconCatalog.Resolve(offer.Id)`.

- [ ] **Step 3: Run focused tests and verify GREEN**

Run `SupportUpgradeIconCatalogTests` in EditMode and all `UpgradeChoicePlayModeTests` in PlayMode. Expected: all pass.

- [ ] **Step 4: Commit and push the implementation**

```powershell
git add Assets/JoseonHunter/Scripts/Runtime/Gameplay/SupportUpgradeIconCatalog.cs Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs Assets/JoseonHunter/Tests
git commit -m "feat: show dedicated support upgrade icons"
git push origin master
```

### Task 4: Visual and regression verification

**Files:**
- Modify only if a verified layout defect is discovered.

**Interfaces:**
- Consumes: completed support icon flow
- Produces: verified mobile portrait presentation

- [ ] **Step 1: Capture a portrait support-choice screenshot**

Force all three support offers, render the level-up modal at the target portrait resolution, and inspect that each icon is readable, centered, and does not overlap text.

- [ ] **Step 2: Run full EditMode regression tests**

Expected: all tests pass with zero failures.

- [ ] **Step 3: Run full PlayMode regression tests**

Expected: all tests pass with zero failures.

- [ ] **Step 4: Commit verification artifacts if they belong in the repository**

```powershell
git add docs/verification
git commit -m "test: verify support upgrade icon presentation"
git push origin master
```
