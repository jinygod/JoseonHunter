# Premium lobby fidelity rework verification

Date: 2026-08-08  
Unity: 6000.5.5f1  
Scope: regenerated production lobby assets, final suites, portrait acceptance captures, and verified obsolete-sprite cleanup.

## Automated evidence

All Unity invocations were sequential; a new invocation was launched only after all Unity editor processes had exited.

- Production rebuild: `LobbySceneBuilder.BuildInBatchMode` completed successfully.
- Focused asset contract: 13/13 passed, 0 failed (`Artifacts/fidelity-assets-final.xml`, 0.101 s).
- Focused regenerated-scene contract: 1/1 passed, 0 failed (`Artifacts/fidelity-lobby-scene-contract-final.xml`, 0.710 s).
- Final full EditMode: 918/918 passed, 0 failed (`Artifacts/fidelity-full-editmode-final2.xml`, 17.685 s).
- Final full PlayMode: 321/321 passed, 0 failed (`Artifacts/fidelity-full-playmode-final2.xml`, 135.984 s).

The initial PlayMode failures separated into two production semantic-skin issues (the generic secondary-button pass overwrote dynamic research/training card frames) and one stale reward assertion. The generic pass now excludes only the dynamic `Style ` and `Training ` cards; the broad `Weapon ` exclusion was removed because it incorrectly left runtime `Weapon Option` images without a sprite. The reward expectation now matches the approved `primary_red_button` semantic mapping.

The shared `content_backplate.png` was regenerated in PixelLab (job `52d4f5dc-44d5-4a01-9fcb-2fb1953605a0`) as an edge-to-edge near-solid ink rounded backplate with a thin antique-gold rule, then reimported and rebuilt. This replaces the prior transparent-offset art; style-card TMP blocks use simple centered insets rather than negative visual-bounds compensation.

## Capture acceptance

Graphics-enabled capture groups were regenerated sequentially after the final code/build/test cycle. The following originals were inspected at native resolution:

- `Artifacts/LobbyPremium/720x1280-patrol.png`
- `Artifacts/LobbyPremium/720x1280-research-ready.png`
- `Artifacts/LobbyPremium/720x1280-training.png`
- `Artifacts/LobbyPremium/1080x2340-patrol.png`
- `Artifacts/LobbyPremium/1080x2340-research-ready.png`
- `Artifacts/PortraitValidation/720x1280/04-pause.png`

All acceptance criteria passed: patrol/research have no oversized architectural rail; research/training text remains inside the visible ink/gold content interiors with three-line readability; the patrol lock slash stays inside its card; selected navigation is visually indicated without bottom labels; and pause retains its thin-frame presentation and two action buttons. Final capture timestamps are 23:40:42--23:41:25 local time.

## Obsolete sprite audit and cleanup

Before deletion, the following exact content search was run for each name, excluding only that asset and its `.meta` file:

```powershell
rg -n --glob "!Resources/UI/PremiumJoseon/<name>.png" --glob "!Resources/UI/PremiumJoseon/<name>.png.meta" <name> Assets/JoseonHunter
```

The audit was repeated after final regeneration and returned zero production/test references for all six names. Deleted exactly these unreferenced PNG/meta pairs:

- `panel_frame`
- `card_idle_frame`
- `card_selected_frame`
- `stage_plaque_frame`
- `nav_idle_frame`
- `nav_selected_frame`

`hero_oval_frame` and shared icon sprites were retained because active production/test references remain.

## Overall status

Passed: production rebuild, focused contracts, full EditMode, full PlayMode, native-resolution capture acceptance, and zero-reference cleanup audit.
