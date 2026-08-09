# Task 2 — Home Hub and Four-State Navigation

## Scope

- Added `LobbyHomeView` and `LobbyHomePresenter` for the selected stage, difficulty, and starting-weapon summary.
- Replaced three-tab navigation behavior with explicit `LobbyPageId` Home, Training, Patrol, and Research state plus Home menu and Back bindings.
- Added real PlayMode behavior coverage for four-page transitions, repeated initialization listener cleanup, no session mutation during navigation, and the Home summary/menu contract.
- Did not edit `Lobby.unity`, `LobbyShell.prefab`, or progression/save code.

## RED evidence

The requested fixture commands were run with no Unity process present, sequentially, from a BelowNormal shell with affinity mask `255`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode `
  -Filter JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests
```

The Unity compile log recorded the intended missing-contract errors: no 10-argument `Initialize`, no `CurrentPage`, no `LobbyHomePresenter`, and no `LobbyHomeView`. The PlayMode assembly compiles all fixtures together, so this same compile RED covered the Home fixture contract as well. The wrapper command exceeded the outer 10-minute capture timeout after Unity had written the compilation failure to `Logs/playmode.log`; no Unity process remained afterward.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode `
  -Filter JoseonHunter.Tests.PlayMode.LobbyHomePlayModeTests
```

The redundant second RED process was stopped after the same assembly-level missing-type compile evidence had already been recorded, per coordinator instruction.

## GREEN evidence

Each command was run only after confirming no Unity process, sequentially, with BelowNormal priority and affinity mask `255`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode `
  -Filter JoseonHunter.Tests.PlayMode.LobbyNavigationPlayModeTests
```

Result: 9 total, 9 passed, 0 failed.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/Test-Unity.ps1 `
  -Platform playmode `
  -Filter JoseonHunter.Tests.PlayMode.LobbyHomePlayModeTests
```

Result: 1 total, 1 passed, 0 failed.

## Files

- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/Views/LobbyHomeView.cs`
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyHomePresenter.cs`
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyNavigationPresenter.cs`
- `Assets/JoseonHunter/Scripts/Presentation/UI/Lobby/LobbyViewModels.cs`
- `Assets/JoseonHunter/Tests/PlayMode/LobbyNavigationPlayModeTests.cs`
- `Assets/JoseonHunter/Tests/PlayMode/LobbyHomePlayModeTests.cs`

## Self-review

- `Show(LobbyPageId)` is the sole state transition path and always activates exactly one of the four new pages.
- `Bind()` clears and re-adds only navigation/back button callbacks, preventing duplicate transitions on reinitialization.
- The Home presenter reads only `ActiveStageSelection`, `ActiveLoadout`, `StageCatalog`, and optional weapon catalog presentation data; it does not mutate the session or inspect research/style state.
- `git diff --check` was clean for the Task 2 file set.
- Existing unrelated dirty files were preserved.

## Commit and push

- Commit: `feat: add lobby home navigation` (final hash is recorded by Git and supplied with the task handoff).
- Push: `origin master` after the report amendment.

## Concerns

- Scene and prefab composition is intentionally deferred to Task 7, so the new Home presenter/view require Task 7 authoring/wiring before appearing in the Lobby scene.
