# Task 6 — Authored Weapon Research Page Binding

## Implemented

- Added `ResearchPageView`, `LobbyResearchRowView`, and the authored `ResearchRow` module prefab.
- Bound the exact eight `WeaponRoster` selector slots and three authored research rows without runtime child creation.
- Preserved the transitional legacy shell adapter only until Task 7; the strict authored path validates before it removes its own listeners.
- Research action rendering derives effect and requirements from `WeaponMasteryStyleDefinition` and preserves purchase-save followed by equip-save behavior.

## Validation

- Focused Weapon Research PlayMode fixture.
- Weapon mastery progression and patrol-loadout EditMode fixtures.
- Lobby module prefab contract fixture.

## Review fix round 1

- Explicit equipped-state evaluation prevents base or equipped research styles from being actionable or causing a redundant `SaveLoadout`.
- `ResearchPageView` rejects selector-to-row action-button aliases before owned-listener teardown.
- Counting-repository coverage proves path-two-first makes no writes, every successful purchase performs purchase-save then loadout-save, and repeated initialization does not duplicate listeners.
- The retained two-save purchase/equip sequence is intentionally unchanged. Atomic rollback across the two existing session writes is pre-existing technical debt outside Task 6 scope.

## Review fix round 2

- The authored selector order test now compares the production `WeaponRoster` directly with the literal expected order and separately verifies literal Korean labels and catalog-backed icons.
- A failing-save repository makes duplicate row-action callbacks observable: after two authored initializations, one actual row-button click produces exactly one loadout save attempt and one header refresh.
- RED was demonstrated by temporarily bypassing owned-listener removal; the new test failed, then passed after restoring the production teardown.

## Review fix round 3

- Every authored selector now proves its icon is the exact catalog reference selected by the production fallback rule: `UiIcon ?? PresentationSprites.FirstOrDefault()`.
- RED was demonstrated by temporarily clearing the rendered first selector sprite; the exact-reference assertion failed before the test-side mutation was removed.
