# Combat Information and Audio Settings Design

## Goal

Make the active run easier to read and give the player persistent control over music and sound volume from both gameplay pause and the lobby.

## Player-facing behavior

- The weapon detail sheet identifies the selected weapon, its run damage, its attributed kills, and every accumulated affix on a separate row.
- Each affix row shows a Korean grade label (`일반`, `고급`, or `최대`) beside its Korean stat description.
- The pause panel contains separate `배경 음악` and `효과음` sliders. Changes apply immediately and persist.
- The lobby header gains a small gear button because no settings entry currently exists. It opens the same two audio controls and reads/writes the same saved values.
- Health and experience bars use a visible empty track and a width-driven fill so the rendered fill always matches the numeric ratio.
- The run clock shows elapsed time as `경과 mm:ss`.

## Ownership and data flow

- `RunWeaponKillLedger` remains the run-scoped authority for weapon attribution. It additionally records confirmed damage and attributed kill counts.
- `FirstPlayableController` records every confirmed damage event and places the resulting totals in `WeaponSlotView`.
- `WeaponAffixRevealPresenter` only formats the supplied view state; it does not calculate combat statistics.
- `SaveDataV1` stores separate music and sound-effect values. `JsonSaveRepository` migrates older saves by falling back to the existing `AudioVolume` value.
- `MetaGameSession` owns the settings mutation and requests the existing settings autosave trigger.
- `GameMusicDirector` and `GameAudioDirector` apply their respective saved master volumes without changing catalog-authored relative clip volumes.
- A shared runtime `AudioSettingsPresenter` builds the two-slider panel. The pause presenter hosts it inline; the lobby gear button opens it as a modal.

## Compatibility and edge cases

- New saves default both channels to full volume.
- Old schema saves preserve their previous single audio volume for both new channels.
- Values are clamped to 0–1 on load and mutation.
- Damage accumulation saturates safely and ignores invalid weapon IDs or non-positive damage.
- Kill credit remains last-hit attribution, matching mastery attribution.
- Weapons with legacy summary strings but no structured roll list still display a readable fallback row.

## Validation

- EditMode tests cover damage/kill totals and audio save migration/persistence.
- PlayMode tests cover weapon detail rows, pause/lobby controls, HUD bar ratios, and elapsed-time copy.
- Full EditMode and PlayMode suites run with bounded CPU affinity, followed by a scoped diff review.
