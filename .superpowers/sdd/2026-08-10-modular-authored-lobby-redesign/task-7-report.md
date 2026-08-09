# Task 7 Report — Authored Modular Lobby

- Replaced the runtime-built lobby shell with a direct authored `Lobby.unity` hierarchy and serialized `LobbyRootView` bindings.
- Removed Bottom Navigation from the production scene. Home is the sole default page and owns the three `수련 / 출전 / 연구` menu-card instances.
- Authored Patrol, Training, Research, common header, page header, settings, selector, progress, and row bindings from production module prefabs. Legacy `LobbyShell` name/GUID audit is zero outside the migration-removal guard, so the obsolete prefab and meta remain deleted.
- Reworked `LobbySceneBuilder` into a dirty-safe, idempotent compose/repair path. Rebuilding twice preserves the authored scene contract, exact Safe Area sibling order, connected module instances, and one EventSystem/input module.
- Removed production legacy presenter initializers and runtime UI builders. Patrol, Training, Research, Home, and settings now use complete authored views with owned listener teardown/rebind behavior.
- Corrected header/session rendering, Home refresh on return, Korean copy, difficulty/lock bindings, training and research row geometry, selected-row/weapon chrome, and removed duplicate legacy Training/Research controls.
- Final focused evidence after the integrated fix rounds: combined PlayMode 57/57 and combined EditMode 32/32. Individual high-risk fixtures also passed: navigation 11/11, Home 2/2, Patrol 14/14, Training 8/8, Research 10/10, audio settings 2/2, and premium skin 9/9.
- Final review found and closed two P1s: legacy Patrol difficulty/selector duplication and partial mutation on an invalid authored repair. The fix-round re-review found no new P0/P1.
- Build Settings remains `Bootstrap → Lobby → Gameplay`. Production scene/prefab missing-script scan returned no matches.
- Unrelated historical captures, combat-art metadata, dynamic font assets, and the gameplay capture remain unstaged.
- Task 8 owns graphics-enabled authored-scene capture, twelve native-resolution images, full suites, Android build, and final visual acceptance.
