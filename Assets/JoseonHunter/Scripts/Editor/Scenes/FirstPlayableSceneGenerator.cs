using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Editor.Scenes
{
    public static class FirstPlayableSceneGenerator
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";

        [MenuItem("JoseonHunter/Setup/Generate First Playable")]
        public static void Generate()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.88f, 0.72f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var controllerObject = new GameObject("FirstPlayable");
            var controller = controllerObject.AddComponent<FirstPlayableController>();
            var serialized = new SerializedObject(controller);
            AssignSprite(serialized, "playerSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/rookie_constable.png");
            AssignSprite(serialized, "enemySprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png");
            AssignSprite(serialized, "enemySpriteAlt",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png");
            AssignSprite(serialized, "bossSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png");
            AssignSprite(serialized, "experienceSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/experience_spirit_flame.png");
            AssignSprite(serialized, "coinSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/coin.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, GameplayScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/JoseonHunter/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene("Assets/JoseonHunter/Scenes/Lobby.unity", true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = controllerObject;
            Debug.Log("JoseonHunter first playable generated. Press Play in Gameplay scene.");
        }

        private static void AssignSprite(SerializedObject serialized, string propertyName, string assetPath)
        {
            serialized.FindProperty(propertyName).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
