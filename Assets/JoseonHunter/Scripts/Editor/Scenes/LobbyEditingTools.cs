using UnityEditor;
using UnityEditor.SceneManagement;

namespace JoseonHunter.Editor.Scenes
{
    public static class LobbyEditingTools
    {
        private const string LobbyScenePath = "Assets/JoseonHunter/Scenes/Lobby.unity";
        private const string ModulesPath = "Assets/JoseonHunter/Prefabs/UI/Lobby/Modules";

        [MenuItem("JoseonHunter/Lobby Editing/Open Lobby Scene")]
        public static void OpenLobbyScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        }

        [MenuItem("JoseonHunter/Lobby Editing/Open Lobby Modules")]
        public static void OpenLobbyModules() => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ModulesPath));

        [MenuItem("JoseonHunter/Lobby Editing/Validate Authored Lobby")]
        public static void Validate() => LobbySceneBuilder.Validate();

        [MenuItem("JoseonHunter/Lobby Editing/Rebuild Authored Lobby")]
        public static void Rebuild() => LobbySceneBuilder.Build();
    }
}
