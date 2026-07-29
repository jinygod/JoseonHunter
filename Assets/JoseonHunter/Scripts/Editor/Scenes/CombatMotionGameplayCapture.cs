using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JoseonHunter.Editor.Scenes
{
    public static class CombatMotionGameplayCapture
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private const double CaptureDelaySeconds = 9d;
        private const int CaptureWidth = 720;
        private const int CaptureHeight = 1280;
        private const string PendingSessionKey = "JoseonHunter.CombatMotionCapture.Pending";
        private const string QuitSessionKey = "JoseonHunter.CombatMotionCapture.Quit";

        private static double playStartedAt;
        private static bool capturePending;
        private static bool quitWhenFinished;

        [MenuItem("JoseonHunter/Validation/Capture Combat Motion Gameplay")]
        public static void CaptureFromMenu() => Begin(false);

        public static void CaptureFromCommandLine() => Begin(true);

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (!SessionState.GetBool(PendingSessionKey, false)) return;
            capturePending = true;
            quitWhenFinished = SessionState.GetBool(QuitSessionKey, false);
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
            EditorApplication.playModeStateChanged += HandlePlayModeState;
            if (!EditorApplication.isPlaying) return;
            playStartedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Begin(bool shouldQuit)
        {
            if (capturePending) return;
            quitWhenFinished = shouldQuit;
            SessionState.SetBool(PendingSessionKey, true);
            SessionState.SetBool(QuitSessionKey, shouldQuit);
            EditorSceneManager.OpenScene(GameplayScenePath);
            EditorApplication.playModeStateChanged += HandlePlayModeState;
            capturePending = true;
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playStartedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update += Tick;
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode || !capturePending) return;
            capturePending = false;
            SessionState.EraseBool(PendingSessionKey);
            SessionState.EraseBool(QuitSessionKey);
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
            if (quitWhenFinished) EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying ||
                EditorApplication.timeSinceStartup - playStartedAt < CaptureDelaySeconds)
            {
                return;
            }

            EditorApplication.update -= Tick;
            try
            {
                CaptureCamera();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (quitWhenFinished) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void CaptureCamera()
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

                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                    ?? throw new InvalidOperationException("Unable to resolve the Unity project root.");
                var outputPath = Path.Combine(projectRoot, "Logs", "combat-motion-gameplay.png");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                Debug.Log($"Combat motion gameplay capture written to {outputPath}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
