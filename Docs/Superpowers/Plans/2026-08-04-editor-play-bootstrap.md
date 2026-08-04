# Unity Editor Play Bootstrap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unity 에디터의 일반 Play가 현재 편집 씬과 관계없이 Bootstrap 로딩 화면과 Lobby를 거치게 한다.

**Architecture:** 기존 에디터 전용 `PlayModeSceneGuard`를 Unity 표준 `EditorSceneManager.playModeStartScene` 설정기로 단순화한다. 시작 씬 선택은 순수한 조건 메서드로 분리해 배치 테스트 제외 정책을 EditMode에서 검증한다.

**Tech Stack:** Unity 6.0, UnityEditor, NUnit EditMode/PlayMode tests

## Global Constraints

- 일반 Unity 에디터 Play는 `Assets/JoseonHunter/Scenes/Bootstrap.unity`에서 시작한다.
- 배치 모드 및 자동화 테스트는 시작 씬 강제 설정의 영향을 받지 않는다.
- 런타임 씬, 프리팹, 저장 데이터는 변경하지 않는다.
- 기존의 사용자 작업 파일은 스테이징하거나 수정하지 않는다.

---

### Task 1: 에디터 Play 시작 씬 고정

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Editor/Scenes/PlayModeSceneGuard.cs`
- Create: `Assets/JoseonHunter/Tests/EditMode/PlayModeSceneGuardTests.cs`

**Interfaces:**
- Consumes: `EditorSceneManager.playModeStartScene`, `AssetDatabase.LoadAssetAtPath<SceneAsset>(string)`
- Produces: `PlayModeSceneGuard.ResolveStartScenePath(bool isBatchMode) : string`, `PlayModeSceneGuard.ConfigureStartScene()`

- [ ] **Step 1: Write the failing test**

```csharp
[Test]
public void InteractiveEditorResolvesBootstrapAsPlayStartScene()
{
    Assert.That(
        PlayModeSceneGuard.ResolveStartScenePath(false),
        Is.EqualTo("Assets/JoseonHunter/Scenes/Bootstrap.unity"));
}

[Test]
public void BatchModeDoesNotOverridePlayStartScene()
{
    Assert.That(PlayModeSceneGuard.ResolveStartScenePath(true), Is.Null);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run Unity EditMode with filter `JoseonHunter.Tests.EditMode.PlayModeSceneGuardTests`.

Expected: FAIL to compile because `ResolveStartScenePath` does not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
private const string BootstrapScenePath = "Assets/JoseonHunter/Scenes/Bootstrap.unity";

public static string ResolveStartScenePath(bool isBatchMode)
{
    return isBatchMode ? null : BootstrapScenePath;
}

public static void ConfigureStartScene()
{
    var path = ResolveStartScenePath(Application.isBatchMode);
    if (path == null) return;
    EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
}
```

Call `ConfigureStartScene()` from the `[InitializeOnLoad]` static constructor and remove the obsolete scene-switch/restart handler.

- [ ] **Step 4: Run focused and regression tests**

Run:

- EditMode filter `JoseonHunter.Tests.EditMode.PlayModeSceneGuardTests`
- Full EditMode suite
- PlayMode filter `JoseonHunter.Tests.PlayMode.BootstrapLoadingPlayModeTests`

Expected: all tests PASS; Bootstrap still loads Lobby and removes its loading overlay.

- [ ] **Step 5: Commit and push**

```powershell
git add -- 'Assets/JoseonHunter/Scripts/Editor/Scenes/PlayModeSceneGuard.cs' 'Assets/JoseonHunter/Tests/EditMode/PlayModeSceneGuardTests.cs' 'Assets/JoseonHunter/Tests/EditMode/PlayModeSceneGuardTests.cs.meta'
git commit -m "fix: route editor play through bootstrap"
git push origin master
```
