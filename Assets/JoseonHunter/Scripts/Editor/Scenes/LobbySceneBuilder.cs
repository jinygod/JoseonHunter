using System;
using System.IO;
using System.Linq;
using JoseonHunter.Presentation.UI.Lobby;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoseonHunter.Editor.Scenes
{
    public static class LobbySceneBuilder
    {
        private const string ScenePath = "Assets/JoseonHunter/Scenes/Lobby.unity";
        private const string PrefabPath = "Assets/JoseonHunter/Prefabs/UI/LobbyShell.prefab";
        private const string BackgroundPath = "Assets/JoseonHunter/Art/UI/Lobby/lobby_courtyard.png";
        private const string ConstablePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/rookie_constable.png";

        [MenuItem("JoseonHunter/Setup/Build Lobby")]
        public static void Build()
        {
            RefuseDirtyLobby();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? string.Empty);
            var scene = FindLoadedLobby();
            var openedForBuild = !scene.IsValid();
            if (openedForBuild) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var previousActive = SceneManager.GetActiveScene();

            try
            {
                SceneManager.SetActiveScene(scene);
                foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);

                var cameraObject = new GameObject("Lobby Camera", typeof(Camera),
                    typeof(UniversalAdditionalCameraData), typeof(AudioListener));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f, .025f, .018f, 1f);
                camera.orthographic = true;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                var canvasObject = new GameObject("Lobby Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LobbyBootstrap));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var bootstrap = canvasObject.GetComponent<LobbyBootstrap>();
                bootstrap.BuildShell();

                AssignSprites(canvasObject);
                var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem == null) throw new InvalidOperationException("Lobby EventSystem was not created.");
                eventSystem.name = "EventSystem";
                if (eventSystem.gameObject.scene != scene) SceneManager.MoveGameObjectToScene(eventSystem.gameObject, scene);

                PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObject, PrefabPath, InteractionMode.AutomatedAction);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Could not save Lobby scene.");
                AssetDatabase.SaveAssets();
                Debug.Log("JoseonHunter Lobby presentation built.");
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
                if (openedForBuild && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void BuildInBatchMode()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("JoseonHunter/Validation/Capture Lobby")]
        public static void CapturePreview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException($"Missing Lobby prefab: {PrefabPath}");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Lobby Preview Camera", typeof(Camera),
                typeof(UniversalAdditionalCameraData));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .025f, .018f, 1f);
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate Lobby preview.");
            PopulatePatrolPreview(instance);
            var canvas = instance.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            var output = Path.GetFullPath("Artifacts/Lobby");
            Directory.CreateDirectory(output);
            Capture(camera, new Vector2Int(720, 1280), Path.Combine(output, "720x1280.png"));
            Capture(camera, new Vector2Int(1080, 2340), Path.Combine(output, "1080x2340.png"));
            UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"Captured Lobby previews to {output}.");
        }

        public static void CapturePreviewInBatchMode()
        {
            try
            {
                CapturePreview();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndCaptureInBatchMode()
        {
            try
            {
                Build();
                CapturePreview();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void AssignSprites(GameObject canvasObject)
        {
            var transforms = canvasObject.GetComponentsInChildren<Transform>(true);
            var background = transforms.Single(item => item.name == "Lobby Background").GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (background.sprite == null) throw new InvalidOperationException($"Missing Lobby background: {BackgroundPath}");
            background.color = Color.white;

            var patrol = canvasObject.GetComponentInChildren<PatrolPresenter>(true);
            if (patrol == null || patrol.ConstableImage == null)
                throw new InvalidOperationException("Lobby patrol portrait slot is missing.");
            patrol.ConstableImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ConstablePath);
            if (patrol.ConstableImage.sprite == null)
                throw new InvalidOperationException($"Missing constable portrait: {ConstablePath}");
            patrol.ConstableImage.color = Color.white;
        }

        private static void PopulatePatrolPreview(GameObject instance)
        {
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            SetPreviewText(transforms, "Preset", "편성 1 · 첫 순찰대");
            SetPreviewText(transforms, "Starting Weapon", "시작 무기\n환도 비검");
            SetPreviewText(transforms, "Style", "운용법 · 기본식");
            SetPreviewText(transforms, "Record", "아직 승리 기록이 없습니다");
        }

        private static void SetPreviewText(Transform[] transforms, string name, string value)
        {
            var text = transforms.Single(item => item.name == name).GetComponent<TMPro.TMP_Text>();
            text.text = value;
        }

        private static void Capture(Camera camera, Vector2Int resolution, string outputPath)
        {
            var renderTexture = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, resolution.x, resolution.y), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Scene FindLoadedLobby()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var candidate = SceneManager.GetSceneAt(index);
                if (candidate.path == ScenePath) return candidate;
            }
            return default;
        }

        private static void RefuseDirtyLobby()
        {
            var scene = FindLoadedLobby();
            if (scene.IsValid() && scene.isDirty)
                throw new InvalidOperationException("Lobby scene has unsaved changes. Save or close it before rebuilding.");
        }
    }
}
