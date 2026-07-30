using System;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
                UnityEngine.Object.DestroyImmediate(root);
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.07f, 0.08f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null) eventSystemObject.AddComponent(inputModuleType);

            var controllerObject = new GameObject("FirstPlayable");
            var controller = controllerObject.AddComponent<FirstPlayableController>();
            var serialized = new SerializedObject(controller);
            AssignSprite(serialized, "playerSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png");
            AssignSprite(serialized, "enemySprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png");
            AssignSprite(serialized, "enemySpriteAlt",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png");
            AssignSprites(serialized, "enemySprites", new[]
            {
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/dokkaebi.png",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/sakkat_specter.png",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/vengeful_spirit.png"
            });
            AssignSprite(serialized, "eliteSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Elites/dokkaebi_captain.png");
            AssignSprite(serialized, "bossSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png");
            AssignSprite(serialized, "experienceSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/experience_spirit_flame.png");
            AssignSprite(serialized, "coinSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/coin.png");
            AssignSprite(serialized, "treasureChestSprite",
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/treasure_chest.png");
            AssignSprite(serialized, "battlefieldTilePrimary",
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/occult_battlefield.png");
            AssignSprites(serialized, "battlefieldDecals", new[]
            {
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/ward_paper_scraps.png",
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/shrine_roof_fragment.png",
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/dry_reed_clump.png",
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield/ritual_stone.png"
            });
            serialized.FindProperty("weaponCatalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<JoseonHunter.Content.Weapons.WeaponCatalogAsset>("Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset");
            serialized.FindProperty("motionLibrary").objectReferenceValue = CombatMotionLibraryBuilder.Build();
            serialized.FindProperty("jangseungGeumjulVisuals").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<JangseungGeumjulVisualLibrary>(
                    "Assets/JoseonHunter/Content/Presentation/JangseungGeumjulVisualLibrary.asset");
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

        private static void AssignSprites(SerializedObject serialized, string propertyName, string[] assetPaths)
        {
            var property = serialized.FindProperty(propertyName);
            property.arraySize = assetPaths.Length;
            for (var index = 0; index < assetPaths.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(assetPaths[index]);
            }
        }
    }
}
