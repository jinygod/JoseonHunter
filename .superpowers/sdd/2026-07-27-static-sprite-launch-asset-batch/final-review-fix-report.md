# Final review fix report

## RED / GREEN / mutation evidence

- RED: `Test-Unity.ps1 -Filter JoseonHunter.Tests.EditMode.StaticSpriteBatchContractTests` produced 24 total, 20 passed, 4 failed: the three wrong canonical role/source/runtime mapping cases and approved manifest with pending provenance. Artifact: `Logs/red-static-batch-contract.xml`.
- GREEN: the same focused contract suite passed 24/24 after the literal mapping and provenance approval check.
- GREEN: `JoseonHunter.Tests.EditMode.StaticSpriteContentTests` passed 5/5 after generator regeneration, including the exact twelve centered 4x3 positions `(-4.5,3)` through `(4.5,-3)` at 3-unit spacing.
- Mutation coverage is the three independent role/source/runtime mapping mutations plus the approval-status mismatch above; each was rejected before production code was restored.

## Scope and risks

- Source provenance for all twelve approved manifest assets now has `status: approved`.
- Regeneration touched only the twelve proof transforms and existing static prefabs. Unity may rewrite LF YAML to CRLF on a later asset import; final whitespace normalization is intentionally performed after Unity commands.
- `ProjectSettings/ProjectSettings.asset` was pre-existing/unrelated and remains unstaged. Generated `ProjectSettings/SceneTemplateSettings.json` was removed and is excluded.
