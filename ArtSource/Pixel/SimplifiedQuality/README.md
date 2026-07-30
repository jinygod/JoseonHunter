# Simplified Combat Pixel Pack

This is production source art for the distant mobile combat camera. The pack
uses 96×96 transparent canvases, a 2–3 px dark outline, large color blocks, and
minimal internal detail so each silhouette survives gameplay scaling.

## Runtime mapping

- `HanYeonhwa`: hero reference, four idle frames, eight walk frames
- `Bandit`: enemy reference, six walk frames
- `PlagueRat`: enemy reference, six walk frames
- `Hwando`: rigid projectile, circular afterimage, four-frame contact flash

The generated source remains here; selected frames are copied one PNG per asset
into the established runtime paths so existing Unity GUIDs remain stable.

