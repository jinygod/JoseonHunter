# Task 3 report

## Step 1 — characters review batch (2026-07-26)

Status: **pending explicit user visual approval**. No approval status was changed to `approved`.

### Image-generation provenance

Built-in `image_gen` was called separately once per hero; no CLI/API fallback was used.

1. Rookie constable: `Use case: stylized-concept; Asset type: original concept source for a Korean historical-fantasy mobile game hero, not a final sprite sheet; Primary request: rookie constable hero concept, navy patrol uniform, black gat, wooden hopae identification tag, and a compact hwando sword silhouette; Scene/backdrop: perfectly flat solid #00ff00 chroma-key background; Style/medium: crisp hand-painted pixel-art concept reference, three-quarter full body, clear readable silhouette, no text; Constraints: uniform #00ff00 only, no shadows/gradients/text/watermark, no #00ff00 on subject, Korean Joseon guard apparel, no samurai armor or Chinese imperial motifs.` Output copied to `ArtSource/Pixel/Characters/rookie-constable/concept-imagegen-raw.png`; cutout made with installed `remove_chroma_key.py --auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill`.
2. Shaman: `Use case: stylized-concept; Asset type: original concept source for a Korean historical-fantasy mobile game hero, not a final sprite sheet; Primary request: a shaman hero wearing a cream and vermilion ritual robe, jade accent, and talisman bundle, full body; Scene/backdrop: perfectly flat solid #00ff00 chroma-key background; Style/medium: crisp hand-painted pixel-art concept reference, three-quarter full body, clear readable silhouette, no text; Constraints: uniform #00ff00 only, no shadows/gradients/text/watermark, Korean Joseon folk ritual attire, no Japanese or Chinese imperial motifs.` Output copied to `ArtSource/Pixel/Characters/shaman/concept-imagegen-raw.png`; same installed chroma-key helper/options.
3. Mountain hunter: `Use case: stylized-concept; Asset type: original concept source for a Korean historical-fantasy mobile game hero, not a final sprite sheet; Primary request: mountain hunter hero in muted green hunting garb with a fur accent and a horn bow silhouette, full body; Scene/backdrop: perfectly flat solid #ff00ff chroma-key background; Style/medium: crisp hand-painted pixel-art concept reference, three-quarter full body, clear readable silhouette, no text; Constraints: uniform #ff00ff only, no shadows/gradients/text/watermark, Korean Joseon mountain hunting attire, no samurai armor or Chinese imperial motifs.` Output copied to `ArtSource/Pixel/Characters/mountain-hunter/concept-imagegen-raw.png`; same installed chroma-key helper/options.

Runtime sheets and source layers are deterministic RGBA pixel artwork built to the shared 384×448 / 64×64 / 38-frame contract; concept images are provenance-only.

### Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Assets/Test-ProductionAssets.ps1 -Batch characters`: passed (exit 0).
- Focused `AssetImportProfileTests` and full EditMode invocation were attempted after preflight, but Unity reported that another Unity instance already had this worktree open. The launcher returned exit 0 despite the fatal lock message, so these are recorded as **not independently verified** and must be rerun once the project lock is released.
