using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Editor.Scenes
{
    [InitializeOnLoad]
    public static class PlayModeSceneGuard
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private static bool resumePlayQueued;

        static PlayModeSceneGuard()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        public static bool ShouldRedirectToGameplay(
            string activeScenePath,
            bool isBatchMode,
            bool isPlayModeTestRunner)
        {
            return string.IsNullOrEmpty(activeScenePath) && !isBatchMode && !isPlayModeTestRunner;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode ||
                !ShouldRedirectToGameplay(
                    SceneManager.GetActiveScene().path,
                    Application.isBatchMode,
                    IsPlayModeTestRunnerActive()))
            {
                return;
            }

            EditorApplication.isPlaying = false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(GameplayScenePath);
            resumePlayQueued = true;
            EditorApplication.delayCall += ResumePlayInGameplay;
        }

        private static void ResumePlayInGameplay()
        {
            if (!resumePlayQueued || EditorApplication.isPlayingOrWillChangePlaymode) return;

            resumePlayQueued = false;
            EditorApplication.isPlaying = true;
        }

        private static bool IsPlayModeTestRunnerActive()
        {
            try
            {
                var testRunnerType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
                var isRunActive = testRunnerType?.GetMethod(
                    "IsRunActive",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return isRunActive != null && (bool)isRunActive.Invoke(null, null);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
