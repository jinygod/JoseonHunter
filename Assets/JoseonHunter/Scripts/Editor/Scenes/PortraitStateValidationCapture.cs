using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JoseonHunter.Editor.Scenes
{
    public static class PortraitStateValidationCapturePolicy
    {
        public static bool ShouldCaptureThisTick(int updatesSinceTransition) => updatesSinceTransition <= 0;

        public static bool CanResumeInCurrentProcess(int captureProcessId, int currentProcessId) =>
            captureProcessId == currentProcessId;
    }

    internal static class PortraitStateCaptureSession
    {
        private const string Prefix = "JoseonHunter.PortraitStateValidationCapture.";
        private const string PendingKey = Prefix + "Pending";
        private const string ResolutionKey = Prefix + "Resolution";
        private const string PhaseKey = Prefix + "Phase";
        private const string ReadyKey = Prefix + "Ready";
        private const string FailedKey = Prefix + "Failed";
        private const string ProcessIdKey = Prefix + "ProcessId";

        public static bool IsPending => EditorPrefs.GetBool(PendingKey, false);
        public static int ResolutionIndex => EditorPrefs.GetInt(ResolutionKey, 0);
        public static int Phase => EditorPrefs.GetInt(PhaseKey, 0);
        public static bool IsReadyToCapture => EditorPrefs.GetBool(ReadyKey, false);
        public static bool Failed => EditorPrefs.GetBool(FailedKey, false);
        public static bool IsOwnedByCurrentProcess => PortraitStateValidationCapturePolicy.CanResumeInCurrentProcess(
            EditorPrefs.GetInt(ProcessIdKey, -1), System.Diagnostics.Process.GetCurrentProcess().Id);

        public static void Begin()
        {
            EditorPrefs.SetBool(PendingKey, true);
            EditorPrefs.SetInt(ResolutionKey, 0);
            EditorPrefs.SetInt(PhaseKey, 0);
            EditorPrefs.SetBool(ReadyKey, false);
            EditorPrefs.SetBool(FailedKey, false);
            EditorPrefs.SetInt(ProcessIdKey, System.Diagnostics.Process.GetCurrentProcess().Id);
        }

        public static void SaveProgress(int resolutionIndex, int phase)
        {
            EditorPrefs.SetInt(ResolutionKey, resolutionIndex);
            EditorPrefs.SetInt(PhaseKey, phase);
            EditorPrefs.SetBool(ReadyKey, false);
        }

        public static void MarkReadyToCapture() => EditorPrefs.SetBool(ReadyKey, true);

        public static void MarkFailed() => EditorPrefs.SetBool(FailedKey, true);

        public static void Clear()
        {
            EditorPrefs.DeleteKey(PendingKey);
            EditorPrefs.DeleteKey(ResolutionKey);
            EditorPrefs.DeleteKey(PhaseKey);
            EditorPrefs.DeleteKey(ReadyKey);
            EditorPrefs.DeleteKey(FailedKey);
            EditorPrefs.DeleteKey(ProcessIdKey);
        }
    }

    /// <summary>Release evidence capture that drives the existing Gameplay coordinator and presenters.</summary>
    [InitializeOnLoad]
    public static class PortraitStateValidationCapture
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private static readonly string[] Names = { "01-gameplay.png", "02-level-up.png", "03-appraisal.png", "04-resumed-combat.png" };
        private static readonly MethodInfo OpenDetails = typeof(FirstPlayableUiBootstrap).GetMethod("OpenWeaponDetails", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo CloseUpgrade = typeof(FirstPlayableUiBootstrap).GetMethod("CloseUpgradeChoice", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo DismissDetails = typeof(WeaponAffixRevealPresenter).GetMethod("DismissDetails", BindingFlags.Instance | BindingFlags.NonPublic);
        private static int resolutionIndex;
        private static int phase;
        private static int updatesSinceTransition;

        static PortraitStateValidationCapture()
        {
            Debug.Log($"Portrait state capture static initialization; pending={PortraitStateCaptureSession.IsPending}; playing={EditorApplication.isPlaying}.");
            AttachPlayModeHandler();
        }

        public static IReadOnlyList<Vector2Int> Resolutions => PortraitUiMetrics.ValidationResolutions;
        public static IReadOnlyList<string> CaptureNames => Names;

        [MenuItem("Tools/Joseon Hunter/Capture/Portrait State Validation")]
        public static void CaptureInBatchMode()
        {
            Debug.Log($"Portrait state capture execute entry; pending={PortraitStateCaptureSession.IsPending}; batch={Application.isBatchMode}; playing={EditorApplication.isPlaying}.");
            if (PortraitStateCaptureSession.IsPending)
            {
                Debug.LogWarning("Portrait state capture recovered a stale pending marker before starting a new session.");
                PortraitStateCaptureSession.Clear();
            }

            try
            {
                ValidateReflectionHooks();
                PortraitStateCaptureSession.Begin();
                Debug.Log($"Portrait state capture pending marker persisted before play request; pending={PortraitStateCaptureSession.IsPending}.");
                resolutionIndex = 0;
                phase = 0;
                updatesSinceTransition = 1;
                EditorSceneManager.OpenScene(GameplayScenePath);
                AttachPlayModeHandler();
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail(exception, "Unable to begin portrait state capture.");
            }
        }

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            Debug.Log($"Portrait state capture domain reload resume; pending={PortraitStateCaptureSession.IsPending}; playing={EditorApplication.isPlaying}.");
            if (PortraitStateCaptureSession.IsPending && !PortraitStateCaptureSession.IsOwnedByCurrentProcess)
            {
                PortraitStateCaptureSession.Clear();
                return;
            }
            AttachPlayModeHandler();
            if (PortraitStateCaptureSession.IsPending && EditorApplication.isPlaying)
                BeginPlayModeCapture();
        }

        private static void AttachPlayModeHandler()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeState;
            EditorApplication.playModeStateChanged += OnPlayModeState;
        }

        private static void OnPlayModeState(PlayModeStateChange state)
        {
            Debug.Log($"Portrait state capture play-mode event={state}; pending={PortraitStateCaptureSession.IsPending}; playing={EditorApplication.isPlaying}.");
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (PortraitStateCaptureSession.IsPending && PortraitStateCaptureSession.IsOwnedByCurrentProcess)
                    BeginPlayModeCapture();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeState;
            var failed = PortraitStateCaptureSession.Failed;
            PortraitStateCaptureSession.Clear();
            Debug.Log($"Portrait state capture exited play mode; failed={failed}.");
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }

        private static void BeginPlayModeCapture()
        {
            try
            {
                resolutionIndex = PortraitStateCaptureSession.ResolutionIndex;
                phase = PortraitStateCaptureSession.Phase;
                updatesSinceTransition = 1;
                Require(resolutionIndex >= 0 && resolutionIndex < Resolutions.Count, "Persisted capture resolution index is invalid.");
                Require(phase >= 0 && phase < Names.Length, "Persisted capture phase is invalid.");
                Debug.Log($"Portrait state capture Begin; resolutionIndex={resolutionIndex}; phase={phase}.");
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            catch (Exception exception)
            {
                Fail(exception, "Unable to initialize portrait state capture after entering play mode.");
            }
        }

        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying) return;
                if (!PortraitStateValidationCapturePolicy.ShouldCaptureThisTick(updatesSinceTransition))
                {
                    updatesSinceTransition--;
                    return;
                }

                var controller = Object.FindAnyObjectByType<FirstPlayableController>();
                var bootstrap = Object.FindAnyObjectByType<FirstPlayableUiBootstrap>();
                if (controller == null || bootstrap == null || bootstrap.BoundController == null) return;
                var resolution = Resolutions[resolutionIndex];
                Screen.SetResolution(resolution.x, resolution.y, false);
                bootstrap.ApplySafeArea(new Rect(0f, 0f, resolution.x, resolution.y), resolution);
                Canvas.ForceUpdateCanvases();

                Debug.Log($"Portrait state capture Tick; resolutionIndex={resolutionIndex}; phase={phase}; ready={PortraitStateCaptureSession.IsReadyToCapture}.");
                if (!PortraitStateCaptureSession.IsReadyToCapture)
                {
                    PreparePhase(controller, bootstrap);
                    PortraitStateCaptureSession.MarkReadyToCapture();
                    updatesSinceTransition = 1;
                    return;
                }
                switch (phase)
                {
                    case 0:
                        CaptureAndAdvance(resolution, Names[0]);
                        break;
                    case 1:
                        CaptureAndAdvance(resolution, Names[1]);
                        break;
                    case 2:
                        CaptureAndAdvance(resolution, Names[2]);
                        break;
                    default:
                        CaptureAndAdvance(resolution, Names[3]);
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(exception, "Portrait state capture failed.");
            }
        }

        private static void PreparePhase(FirstPlayableController controller, FirstPlayableUiBootstrap bootstrap)
        {
            switch (phase)
            {
                case 0:
                    Require(controller.Flow.State == GameFlowState.Playing, "Gameplay must start Playing.");
                    break;
                case 1:
                    controller.OpenUpgradeOffersForTests(new UpgradeOffer("hwando_flying_blade", UpgradeKind.Weapon, 2));
                    RequirePresenter<UpgradeChoicePresenter>(controller, GameFlowState.LevelUpSelection, presenter => presenter.IsOpen, "Level-up presenter/state was not opened.");
                    break;
                case 2:
                    controller.CancelUiModalPresentation();
                    CloseUpgrade.Invoke(bootstrap, null);
                    Require(controller.Flow.State == GameFlowState.Playing, "Level-up did not resume Playing.");
                    var weapons = controller.UiState.Weapons;
                    Require(weapons.Count > 0 && OpenDetails != null, "No gameplay weapon is available for appraisal.");
                    OpenDetails.Invoke(bootstrap, new object[] { weapons[0] });
                    RequirePresenter<WeaponAffixRevealPresenter>(controller, GameFlowState.Paused, presenter => presenter.IsDetailOpen, "Appraisal presenter/state was not opened.");
                    break;
                default:
                    var detail = Object.FindAnyObjectByType<WeaponAffixRevealPresenter>();
                    Require(DismissDetails != null && detail != null, "Appraisal close seam is unavailable.");
                    DismissDetails.Invoke(detail, null);
                    Require(controller.Flow.State == GameFlowState.Playing, "Appraisal did not resume Playing.");
                    break;
            }
        }

        private static void CaptureAndAdvance(Vector2Int resolution, string name)
        {
            CaptureCamera(resolution, name);
            if (phase == Names.Length - 1)
            {
                resolutionIndex++;
                phase = 0;
                if (resolutionIndex >= Resolutions.Count)
                {
                    Debug.Log("Portrait state capture completed all 20 PNGs; requesting exit from play mode.");
                    EditorApplication.update -= Tick;
                    EditorApplication.isPlaying = false;
                    return;
                }
            }
            else phase++;

            PortraitStateCaptureSession.SaveProgress(resolutionIndex, phase);
            updatesSinceTransition = 1;
        }

        private static void CaptureCamera(Vector2Int resolution, string name)
        {
            var camera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            Require(camera != null, "Gameplay capture requires an active camera.");
            var directory = Path.Combine(ProjectRoot(), "Artifacts", "PortraitValidation", resolution.x + "x" + resolution.y);
            Directory.CreateDirectory(directory);
            var output = Path.Combine(directory, name);
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            var originalModes = new RenderMode[canvases.Length];
            var originalCameras = new Camera[canvases.Length];
            try
            {
                for (var index = 0; index < canvases.Length; index++)
                {
                    originalModes[index] = canvases[index].renderMode;
                    originalCameras[index] = canvases[index].worldCamera;
                    if (canvases[index].renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    canvases[index].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[index].worldCamera = camera;
                }

                Canvas.ForceUpdateCanvases();
                renderTexture = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
                texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, resolution.x, resolution.y), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(output, texture.EncodeToPNG());
                Require(new FileInfo(output).Length > 0, "Capture PNG was empty.");
                Debug.Log($"Portrait state capture written: {output}; resolution={resolution.x}x{resolution.y}; phase={phase}.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                for (var index = 0; index < canvases.Length; index++)
                {
                    if (canvases[index] == null) continue;
                    canvases[index].renderMode = originalModes[index];
                    canvases[index].worldCamera = originalCameras[index];
                }
                Canvas.ForceUpdateCanvases();
                if (texture != null) Object.DestroyImmediate(texture);
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
            }
        }

        private static void RequirePresenter<T>(FirstPlayableController controller, GameFlowState expectedState, Func<T, bool> isOpen, string message) where T : Component
        {
            var presenter = Object.FindAnyObjectByType<T>();
            Require(controller.Flow.State == expectedState && presenter != null && isOpen(presenter), message);
        }

        private static void ValidateReflectionHooks()
        {
            Require(OpenDetails != null && CloseUpgrade != null && DismissDetails != null, "Portrait capture reflection hooks no longer match the UI bootstrap presenters.");
        }

        private static string ProjectRoot() => Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Unable to resolve the project root.");

        private static void Fail(Exception exception, string context)
        {
            PortraitStateCaptureSession.MarkFailed();
            Debug.LogError($"{context} {exception.Message}");
            Debug.LogException(exception);
            try
            {
                File.WriteAllText(Path.Combine(ProjectRoot(), "Artifacts", "PortraitValidation", "capture.failed.txt"), exception.ToString());
            }
            catch (Exception markerException)
            {
                Debug.LogWarning($"Unable to write portrait capture failure marker: {markerException.Message}");
            }

            EditorApplication.update -= Tick;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            else if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
