# Task 6 Report: Static Sprite Catalog, Prefabs, and Gameplay Proof

## RED/GREEN evidence

- Initial focused run recorded the expected RED compiler error: `CS0246`, because
  `StaticSpriteCatalog` did not exist.
- The dirty Gameplay guard has a regression test. With the guard temporarily
  changed to return, it was RED (3/4 passed, dirty-scene test failed); restored
  guard was GREEN (4/4 passed).
- The literal ID-to-runtime-sprite and prefab-path regression test was proven by
  temporarily generating `shaman` from `Heroes/rookie_constable.png`. The fresh
  RED XML is `Logs/task6-review-red-results.xml`: 4/5 passed, with the exact
  path assertion failing. The generator mapping was restored and generated
  assets were restored to their committed serialized references.
- The final focused GREEN used Unity without `-quit`:

  ```powershell
  & 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' `
    -batchmode -nographics -projectPath . `
    -runTests -testPlatform editmode `
    -testFilter JoseonHunter.Tests.EditMode.StaticSpriteContentTests `
    -testResults Logs/task6-review-final-results.xml `
    -logFile Logs/task6-review-final.log
  ```

  `Logs/task6-review-final-results.xml` reports 5/5 passed and
  `Logs/task6-review-final.log` records `Test run completed. Exiting with code 0`.

## Generated assets

- `Assets/JoseonHunter/Content/StaticSpriteCatalog.asset` with the exact twelve
  approved IDs, sprites, and prefabs.
- Twelve `Assets/JoseonHunter/Prefabs/StaticSprites/*.prefab` assets. Each has
  one `SpriteRenderer` and one `StaticSpriteMotionPresenter`.
- `Gameplay/SceneRoot/World/StaticSpriteLaunchProof`, inactive, with twelve
  prefab children named by their catalog IDs.

## Validation

- `StaticSpriteContentTests`: 5/5 passed in the final uniquely named XML above.
- Static runtime batch preflight with `-RequireRuntime`: passed.
- Full EditMode suite: 101/101 passed.
- `git diff --check`: passed.
- Serialized Gameplay diff was inspected: the only scene addition is the
  inactive proof root and its twelve prefab instances. Focused tests resolve
  all catalog assets and prefab components, with no missing references.

## Commit

- Main content commit: e37c3d3
- Review-fix commit: cadbfc8

## Self-review and risks

- Generator uses only editor APIs under `Scripts/Editor`; runtime catalog has
  no `UnityEditor` dependency.
- Generation is deterministic from the fixed ordered ID/runtime-path manifest,
  replaces the proof lineup, and rejects a dirty open Gameplay scene.
- Tests now assert literal runtime sprite and prefab paths, renderer sprite
  identity, and proof-child prefab source identity in addition to the original
  catalog/count checks.
- Unity generated an untracked `ProjectSettings/SceneTemplateSettings.json`.
  It is intentionally excluded because Task 6 must not modify ProjectSettings.
