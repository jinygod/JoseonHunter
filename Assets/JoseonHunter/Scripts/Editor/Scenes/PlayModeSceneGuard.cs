using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JoseonHunter.Editor.Scenes
{
    [InitializeOnLoad]
    public static class PlayModeSceneGuard
    {
        private const string BootstrapScenePath = "Assets/JoseonHunter/Scenes/Bootstrap.unity";

        static PlayModeSceneGuard()
        {
            ConfigureStartScene(Application.isBatchMode);
        }

        public static string ResolveStartScenePath(bool isBatchMode)
        {
            return isBatchMode ? null : BootstrapScenePath;
        }

        public static void ConfigureStartScene(bool isBatchMode)
        {
            var path = ResolveStartScenePath(isBatchMode);
            if (string.IsNullOrEmpty(path)) return;

            var bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (bootstrapScene == null)
            {
                EditorSceneManager.playModeStartScene = null;
                Debug.LogError($"Editor Play 시작 씬을 찾을 수 없습니다: {path}");
                return;
            }

            EditorSceneManager.playModeStartScene = bootstrapScene;
        }
    }
}
