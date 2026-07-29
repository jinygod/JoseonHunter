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
    public static class EightWeaponPolishCapture
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private const string MenuPath = "Tools/Joseon Hunter/Capture/Eight Weapon Polish";
        private const string PendingSessionKey = "JoseonHunter.EightWeaponPolishCapture.Pending";
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
            if (SessionState.GetBool(PendingSessionKey, false))
            {
                Debug.LogWarning("Eight-weapon polish capture is already running.");
                return;
            }

            ValidateReflectionHooks();
            SessionState.SetBool(PendingSessionKey, true);
            SessionState.SetString(PendingSessionKey + ".Weapon", selectedWeapon ?? string.Empty);
            caseIndex = 0;
            EditorSceneManager.OpenScene(GameplayScenePath);
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
            EditorApplication.playModeStateChanged += HandlePlayModeState;
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (!SessionState.GetBool(PendingSessionKey, false)) return;
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
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
            SessionState.EraseBool(PendingSessionKey);
            SessionState.EraseString(PendingSessionKey + ".Weapon");
            controller = null;
            Debug.Log($"Eight-weapon polish capture finished. Output: {OutputDirectory()}");
        }

        private static void BeginPlayModeCapture()
        {
            Screen.SetResolution(CaptureWidth, CaptureHeight, false);
            caseIndex = 0;
            weaponFilter = SessionState.GetString(PendingSessionKey + ".Weapon", string.Empty);
            controller = Object.FindFirstObjectByType<FirstPlayableController>();
            if (controller == null)
            {
                Fail(new InvalidOperationException("Gameplay scene did not create FirstPlayableController."));
                return;
            }

            Directory.CreateDirectory(OutputDirectory());
            ConfigureCase(ActiveCases()[caseIndex]);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
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
            stage = CaptureStage.Settle;
            stageStartedAt = EditorApplication.timeSinceStartup;
            Debug.Log($"Eight-weapon capture prepared: {captureCase.WeaponId}/{captureCase.Label}");
        }

        private static void Tick()
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
                    if (IsSpecialEvolvedCapture())
                    {
                        if (IsSpecialEvolvedPhaseActive() || elapsed >= 6d)
                        {
                            Time.timeScale = 0f;
                            CaptureCamera(currentCase);
                            AdvanceCase();
                        }
                        break;
                    }
                    if (currentCase.Evolved &&
                        currentCase.WeaponId.Equals(WeaponId.GakgungShot) &&
                        IsSunPiercerActive())
                    {
                        Time.timeScale = 0f;
                        CaptureCamera(currentCase);
                        AdvanceCase();
                    }
                    else if (elapsed >= .04d &&
                        !(currentCase.Evolved && currentCase.WeaponId.Equals(WeaponId.GakgungShot)) &&
                        (currentCase.WeaponId.Equals(WeaponId.GakgungShot) ||
                         currentCase.WeaponId.Equals(WeaponId.WindThunderFan)))
                    {
                        Time.timeScale = 0f;
                        CaptureCamera(currentCase);
                        AdvanceCase();
                    }
                    else if (elapsed >= .025d && HasActiveWeaponPresentation())
                    {
                        Time.timeScale = 0f;
                        stage = CaptureStage.MeaningfulHold;
                        stageStartedAt = EditorApplication.timeSinceStartup;
                    }
                    else if (elapsed >= MeaningfulPhaseTimeoutSeconds)
                    {
                        CaptureCamera(currentCase);
                        AdvanceCase();
                    }
                    break;
                case CaptureStage.MeaningfulHold when elapsed >= .015d:
                    CaptureCamera(currentCase);
                    AdvanceCase();
                    break;
            }
        }

        private static bool IsSunPiercerActive()
        {
            return ExecutorForCapture<JoseonHunter.Runtime.Combat.Weapons.GakgungExecutor>(
                       WeaponId.GakgungShot) is { } gakgung &&
                   gakgung.LastProjectileScale > 1f;
        }

        private static bool IsSpecialEvolvedCapture() =>
            currentCase.Evolved &&
            (currentCase.WeaponId.Equals(WeaponId.FrostFlask) ||
             currentCase.WeaponId.Equals(WeaponId.JangseungWard) ||
             currentCase.WeaponId.Equals(WeaponId.SingijeonVolley));

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

            var renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
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
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
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

        private static void Fail(Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.update -= Tick;
            SessionState.EraseBool(PendingSessionKey);
            EditorApplication.isPlaying = false;
        }

    }
}
