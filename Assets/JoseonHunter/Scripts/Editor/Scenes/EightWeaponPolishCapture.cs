using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JoseonHunter.Editor.Scenes
{
    internal enum CapturePhaseAction
    {
        Wait,
        Capture,
        Fail
    }

    internal enum CapturePredicateKind
    {
        WeaponPresentation,
        NearPlayerPresentation,
        SpecialEvolved,
        SunPiercer
    }

    internal static class CapturePhasePolicy
    {
        public static CapturePhaseAction Evaluate(
            bool predicateSatisfied,
            double elapsedSeconds,
            double earliestCaptureSeconds,
            double timeoutSeconds)
        {
            if (predicateSatisfied && elapsedSeconds >= earliestCaptureSeconds)
                return CapturePhaseAction.Capture;
            return elapsedSeconds >= timeoutSeconds
                ? CapturePhaseAction.Fail
                : CapturePhaseAction.Wait;
        }

        public static CapturePredicateKind PredicateFor(EightWeaponPolishCapture.CaptureCase captureCase)
        {
            if (captureCase.Evolved &&
                (captureCase.WeaponId.Equals(WeaponId.FrostFlask) ||
                 captureCase.WeaponId.Equals(WeaponId.JangseungWard) ||
                 captureCase.WeaponId.Equals(WeaponId.SingijeonVolley)))
            {
                return CapturePredicateKind.SpecialEvolved;
            }

            if (captureCase.Evolved && captureCase.WeaponId.Equals(WeaponId.GakgungShot))
                return CapturePredicateKind.SunPiercer;
            if (captureCase.WeaponId.Equals(WeaponId.GakgungShot) ||
                captureCase.WeaponId.Equals(WeaponId.WindThunderFan))
            {
                return CapturePredicateKind.NearPlayerPresentation;
            }

            return CapturePredicateKind.WeaponPresentation;
        }
    }

    internal static class CaptureSessionState
    {
        private const string PendingKey = "JoseonHunter.EightWeaponPolishCapture.Pending";
        private const string WeaponKey = PendingKey + ".Weapon";

        public static bool IsPending => SessionState.GetBool(PendingKey, false);
        public static string WeaponFilter => SessionState.GetString(WeaponKey, string.Empty);

        public static void Begin(string selectedWeapon)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(WeaponKey, selectedWeapon ?? string.Empty);
        }

        public static void Clear()
        {
            SessionState.EraseBool(PendingKey);
            SessionState.EraseString(WeaponKey);
        }
    }

    public static class EightWeaponPolishCapture
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private const string MenuPath = "Tools/Joseon Hunter/Capture/Eight Weapon Polish";
        private const int CaptureWidth = 360;
        private const int CaptureHeight = 800;
        private const double SettleSeconds = 0.35d;
        private const double FirstCycleSeconds = 1.6d;
        private const double MeaningfulPhaseTimeoutSeconds = 2.5d;

        private static readonly Vector2[] TargetPositions =
        {
            new(1.15f, 0.15f),
            new(-1.65f, 1.65f),
            new(2.05f, -2.35f)
        };

        private static readonly FieldInfo WeaponLevelsField =
            typeof(FirstPlayableController).GetField("weaponLevels", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo RebuildWeaponsMethod =
            typeof(FirstPlayableController).GetMethod("RebuildWeaponExecutorsForLevel", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo SpawnTimerField =
            typeof(FirstPlayableController).GetField("spawnTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ChestSpawnTimerField =
            typeof(FirstPlayableController).GetField("chestSpawnTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo EnemiesField =
            typeof(FirstPlayableController).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ExecutorsByWeaponField =
            typeof(JoseonHunter.Runtime.Combat.WeaponRuntimeController)
                .GetField("executorsByWeapon", BindingFlags.Instance | BindingFlags.NonPublic);

        private static int caseIndex;
        private static CaptureStage stage;
        private static double stageStartedAt;
        private static FirstPlayableController controller;
        private static Transform playerTransform;
        private static CaptureCase currentCase;
        private static string weaponFilter;

        public readonly struct CaptureCase
        {
            public CaptureCase(WeaponId weaponId, int level, bool evolved, string label)
            {
                WeaponId = weaponId;
                Level = level;
                Evolved = evolved;
                Label = label;
            }

            public WeaponId WeaponId { get; }
            public int Level { get; }
            public bool Evolved { get; }
            public string Label { get; }
        }

        private enum CaptureStage
        {
            None,
            Settle,
            FirstCycle,
            MeaningfulPhase,
            MeaningfulHold
        }

        public static IReadOnlyList<CaptureCase> BuildCases()
        {
            var cases = new List<CaptureCase>(WeaponRoster.All.Count * 4);
            foreach (var weapon in WeaponRoster.All)
            {
                cases.Add(new CaptureCase(weapon, 1, false, "level-1"));
                cases.Add(new CaptureCase(weapon, 3, false, "level-3"));
                cases.Add(new CaptureCase(weapon, 5, false, "level-5"));
                cases.Add(new CaptureCase(weapon, 5, true, "evolved"));
            }

            return cases;
        }

        [MenuItem(MenuPath)]
        public static void CaptureAll()
        {
            BeginCapture(null);
        }

        public static void CaptureWeapon(WeaponId weaponId)
        {
            BeginCapture(weaponId.Value);
        }

        private static void BeginCapture(string selectedWeapon)
        {
            if (CaptureSessionState.IsPending)
            {
                Debug.LogWarning("Eight-weapon polish capture is already running.");
                return;
            }

            try
            {
                ValidateReflectionHooks();
                CaptureSessionState.Begin(selectedWeapon);
                caseIndex = 0;
                currentCase = default;
                EditorSceneManager.OpenScene(GameplayScenePath);
                EditorApplication.playModeStateChanged -= HandlePlayModeState;
                EditorApplication.playModeStateChanged += HandlePlayModeState;
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail(exception, "Unable to begin capture session.");
            }
        }

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (!CaptureSessionState.IsPending) return;
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
            EditorApplication.playModeStateChanged += HandlePlayModeState;
            if (EditorApplication.isPlaying) BeginPlayModeCapture();
        }

        private static void HandlePlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BeginPlayModeCapture();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode) return;
            CleanupCaptureSession(exitPlayMode: false);
            Debug.Log($"Eight-weapon polish capture finished. Output: {OutputDirectory()}");
        }

        private static void BeginPlayModeCapture()
        {
            try
            {
                Screen.SetResolution(CaptureWidth, CaptureHeight, false);
                caseIndex = 0;
                weaponFilter = CaptureSessionState.WeaponFilter;
                controller = Object.FindFirstObjectByType<FirstPlayableController>();
                if (controller == null)
                    throw new InvalidOperationException("Gameplay scene did not create FirstPlayableController.");

                Directory.CreateDirectory(OutputDirectory());
                ConfigureCase(ActiveCases()[caseIndex]);
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            catch (Exception exception)
            {
                Fail(exception, "Unable to initialize play-mode capture.");
            }
        }

        private static void ConfigureCase(CaptureCase captureCase)
        {
            currentCase = captureCase;
            controller.ResetRunForTests();
            SpawnTimerField.SetValue(controller, 9999f);
            ChestSpawnTimerField.SetValue(controller, 9999f);

            var weaponLevels = (Dictionary<string, int>)WeaponLevelsField.GetValue(controller);
            weaponLevels.Clear();
            weaponLevels.Add(captureCase.WeaponId.Value, captureCase.Level);
            RebuildWeaponsMethod.Invoke(controller, null);
            if (captureCase.Evolved)
            {
                var evolution = WeaponEvolutionCatalog.All.Single(item => item.RequiredWeaponId.Equals(captureCase.WeaponId));
                controller.AcquireEvolutionForTests(evolution.Id);
            }

            foreach (var position in TargetPositions) controller.SpawnEnemyForTests(position);
            MakeEnemiesStationaryAndDurable();
            playerTransform = GameObject.Find("Han Yeonhwa")?.transform;
            DeletePreviousCaseResult(captureCase);
            stage = CaptureStage.Settle;
            stageStartedAt = EditorApplication.timeSinceStartup;
            Debug.Log($"Eight-weapon capture prepared: {captureCase.WeaponId}/{captureCase.Label}");
        }

        private static void Tick()
        {
            try
            {
                TickCapture();
            }
            catch (Exception exception)
            {
                Fail(exception, $"Capture failed for {currentCase.WeaponId}/{currentCase.Label}.");
            }
        }

        private static void TickCapture()
        {
            if (!EditorApplication.isPlaying || controller == null) return;
            PinEnemiesToCapturePositions();
            var elapsed = EditorApplication.timeSinceStartup - stageStartedAt;
            switch (stage)
            {
                case CaptureStage.Settle when elapsed >= SettleSeconds:
                    stage = CaptureStage.FirstCycle;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                    break;
                case CaptureStage.FirstCycle when elapsed >= FirstCycleSeconds:
                    // Fast projectile beats need a deterministic restart; stateful fields keep their completed
                    // first cadence so level growth and evolved payoffs remain visible in the capture.
                    if (currentCase.WeaponId.Equals(WeaponId.GakgungShot) ||
                        currentCase.WeaponId.Equals(WeaponId.WindThunderFan))
                    {
                        controller.SetWeaponLevelForTests(currentCase.WeaponId, currentCase.Level);
                    }
                    stage = CaptureStage.MeaningfulPhase;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                    break;
                case CaptureStage.MeaningfulPhase:
                    var predicateKind = CapturePhasePolicy.PredicateFor(currentCase);
                    var predicateSatisfied = IsRequiredPhaseActive(predicateKind);
                    var earliestCapture = predicateKind == CapturePredicateKind.NearPlayerPresentation
                        ? .04d
                        : .025d;
                    var timeout = predicateKind == CapturePredicateKind.SpecialEvolved ||
                                  predicateKind == CapturePredicateKind.SunPiercer
                        ? 6d
                        : MeaningfulPhaseTimeoutSeconds;
                    var action = CapturePhasePolicy.Evaluate(
                        predicateSatisfied,
                        elapsed,
                        earliestCapture,
                        timeout);
                    if (action == CapturePhaseAction.Fail)
                    {
                        throw new TimeoutException(
                            $"Meaningful phase predicate '{predicateKind}' timed out after {timeout:F2}s " +
                            $"for {currentCase.WeaponId}/{currentCase.Label}; no PNG was written.");
                    }

                    if (action != CapturePhaseAction.Capture) break;
                    if (predicateKind == CapturePredicateKind.WeaponPresentation)
                    {
                        stage = CaptureStage.MeaningfulHold;
                        stageStartedAt = EditorApplication.timeSinceStartup;
                        break;
                    }
                    CaptureAndAdvance();
                    break;
                case CaptureStage.MeaningfulHold when elapsed >= .015d:
                    CaptureAndAdvance();
                    break;
            }
        }

        private static bool IsRequiredPhaseActive(CapturePredicateKind predicateKind)
        {
            return predicateKind switch
            {
                CapturePredicateKind.SpecialEvolved => IsSpecialEvolvedPhaseActive(),
                CapturePredicateKind.SunPiercer => IsSunPiercerActive(),
                CapturePredicateKind.NearPlayerPresentation => HasActiveWeaponPresentation(),
                CapturePredicateKind.WeaponPresentation => HasActiveWeaponPresentation(),
                _ => false
            };
        }

        private static bool IsSunPiercerActive()
        {
            return ExecutorForCapture<JoseonHunter.Runtime.Combat.Weapons.GakgungExecutor>(
                       WeaponId.GakgungShot) is { } gakgung &&
                   gakgung.LastProjectileScale > 1f;
        }

        private static bool IsSpecialEvolvedPhaseActive()
        {
            if (currentCase.WeaponId.Equals(WeaponId.FrostFlask))
                return ExecutorForCapture<JoseonHunter.Runtime.Combat.Weapons.FrostFlaskExecutor>(
                           WeaponId.FrostFlask) is { } frost &&
                       frost.ExpiredFieldCount > 0;
            if (currentCase.WeaponId.Equals(WeaponId.SingijeonVolley))
                return ExecutorForCapture<JoseonHunter.Runtime.Combat.Weapons.SingijeonExecutor>(
                           WeaponId.SingijeonVolley) is { } singijeon &&
                       singijeon.FocusProjectileCount > 0 &&
                       singijeon.ActiveProjectileCount > 0;
            if (!currentCase.WeaponId.Equals(WeaponId.JangseungWard)) return false;
            return Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                       .Any(renderer => renderer != null &&
                                        renderer.gameObject.name == "Jangseung Evolved Guardian Burst" &&
                                        renderer.gameObject.activeInHierarchy);
        }

        private static T ExecutorForCapture<T>(WeaponId weaponId) where T : class
        {
            if (controller?.WeaponRuntime == null || ExecutorsByWeaponField == null) return null;
            var executors = ExecutorsByWeaponField.GetValue(controller.WeaponRuntime) as IDictionary;
            return executors?[weaponId] as T;
        }

        private static bool HasActiveWeaponPresentation()
        {
            return Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Any(renderer => renderer != null &&
                                 renderer.enabled &&
                                 renderer.gameObject.activeInHierarchy &&
                                 IsMeaningfulWeaponPresentation(renderer));
        }

        private static bool IsMeaningfulWeaponPresentation(SpriteRenderer renderer)
        {
            var objectName = renderer.gameObject.name;
            if (!IsWeaponPresentationName(objectName)) return false;
            if (!currentCase.WeaponId.Equals(WeaponId.GakgungShot) &&
                !currentCase.WeaponId.Equals(WeaponId.WindThunderFan))
            {
                return true;
            }

            var isGakgungProjectile = currentCase.WeaponId.Equals(WeaponId.GakgungShot) &&
                                      objectName == "Gakgung Arrow";
            if (objectName != "Weapon Transient Visual" && !isGakgungProjectile || playerTransform == null)
                return false;
            return Vector2.Distance(renderer.transform.position, playerTransform.position) <= 2.2f;
        }

        private static bool IsWeaponPresentationName(string objectName) =>
            objectName == "Weapon Transient Visual" ||
            objectName == "Hwando Flying Blade" ||
            objectName == "Blade Afterimage" ||
            objectName == "Gakgung Arrow" ||
            objectName == "Talisman Flight" ||
            objectName == "Thunder Crash Bomb" ||
            objectName == "Bomb Shadow" ||
            objectName == "Frost Flask" ||
            objectName.StartsWith("Singijeon", StringComparison.Ordinal) ||
            objectName.StartsWith("Jangseung", StringComparison.Ordinal);

        private static void AdvanceCase()
        {
            caseIndex++;
            var cases = ActiveCases();
            if (caseIndex >= cases.Count)
            {
                EditorApplication.update -= Tick;
                EditorApplication.isPlaying = false;
                return;
            }

            ConfigureCase(cases[caseIndex]);
        }

        private static IReadOnlyList<CaptureCase> ActiveCases()
        {
            var cases = BuildCases();
            return string.IsNullOrEmpty(weaponFilter)
                ? cases
                : cases.Where(item => item.WeaponId.Value == weaponFilter).ToArray();
        }

        private static void MakeEnemiesStationaryAndDurable()
        {
            var enemies = (IEnumerable)EnemiesField.GetValue(controller);
            foreach (var enemy in enemies)
            {
                var type = enemy.GetType();
                type.GetField("Speed", BindingFlags.Instance | BindingFlags.Public)?.SetValue(enemy, 0f);
                type.GetField("ContactDamage", BindingFlags.Instance | BindingFlags.Public)?.SetValue(enemy, 0f);
                type.GetField("MaximumHealth", BindingFlags.Instance | BindingFlags.Public)?.SetValue(enemy, 99999f);
                type.GetField("Health", BindingFlags.Instance | BindingFlags.Public)?.SetValue(enemy, 99999f);
            }
        }

        private static void PinEnemiesToCapturePositions()
        {
            var enemies = (IEnumerable)EnemiesField.GetValue(controller);
            var index = 0;
            foreach (var enemy in enemies)
            {
                if (index >= TargetPositions.Length) break;
                var gameObject = enemy.GetType()
                    .GetField("Object", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(enemy) as GameObject;
                if (gameObject != null) gameObject.transform.position = TargetPositions[index];
                index++;
            }
        }

        private static void CaptureCamera(CaptureCase captureCase)
        {
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera == null) throw new InvalidOperationException("Gameplay capture requires an active camera.");

            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
                texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
                texture.Apply();

                var fileName = $"{captureCase.WeaponId.Value}-{captureCase.Label}.png";
                var outputPath = Path.Combine(OutputDirectory(), fileName);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                Debug.Log(
                    $"Eight-weapon capture written: {outputPath}; phase={stage}; " +
                    $"editorTime={EditorApplication.timeSinceStartup:F3}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (texture != null) Object.DestroyImmediate(texture);
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
            }
        }

        private static void CaptureAndAdvance()
        {
            var previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                CaptureCamera(currentCase);
                AdvanceCase();
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        private static string CaseOutputPath(CaptureCase captureCase, string extension) =>
            Path.Combine(
                OutputDirectory(),
                $"{captureCase.WeaponId.Value}-{captureCase.Label}{extension}");

        private static void DeletePreviousCaseResult(CaptureCase captureCase)
        {
            var pngPath = CaseOutputPath(captureCase, ".png");
            var failurePath = CaseOutputPath(captureCase, ".failed.txt");
            if (File.Exists(pngPath)) File.Delete(pngPath);
            if (File.Exists(failurePath)) File.Delete(failurePath);
        }

        private static string OutputDirectory()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unable to resolve Unity project root.");
            return Path.Combine(projectRoot, "Artifacts", "WeaponPolish");
        }

        private static void ValidateReflectionHooks()
        {
            if (WeaponLevelsField == null || RebuildWeaponsMethod == null || SpawnTimerField == null ||
                ChestSpawnTimerField == null || EnemiesField == null || ExecutorsByWeaponField == null)
            {
                throw new MissingMemberException(
                    "Eight-weapon capture no longer matches FirstPlayableController's deterministic capture hooks.");
            }
        }

        private static void Fail(Exception exception, string reason)
        {
            var label = $"{currentCase.WeaponId}/{currentCase.Label}";
            var message = $"{reason} Case={label}. {exception.Message}";
            TryDeleteFailedPng();
            TryWriteFailureMarker(message);
            Debug.LogError(message);
            Debug.LogException(exception);
            CleanupCaptureSession(exitPlayMode: true);
        }

        private static void TryWriteFailureMarker(string message)
        {
            if (string.IsNullOrEmpty(currentCase.Label)) return;
            try
            {
                Directory.CreateDirectory(OutputDirectory());
                File.WriteAllText(CaseOutputPath(currentCase, ".failed.txt"), message);
            }
            catch (Exception markerException)
            {
                Debug.LogWarning($"Unable to write capture failure marker: {markerException.Message}");
            }
        }

        private static void TryDeleteFailedPng()
        {
            if (string.IsNullOrEmpty(currentCase.Label)) return;
            try
            {
                var path = CaseOutputPath(currentCase, ".png");
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception deleteException)
            {
                Debug.LogWarning($"Unable to delete failed capture PNG: {deleteException.Message}");
            }
        }

        private static void CleanupCaptureSession(bool exitPlayMode)
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
            CaptureSessionState.Clear();
            Time.timeScale = 1f;
            stage = CaptureStage.None;
            caseIndex = 0;
            currentCase = default;
            controller = null;
            playerTransform = null;
            weaponFilter = null;
            if (exitPlayMode && EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
        }

    }
}
