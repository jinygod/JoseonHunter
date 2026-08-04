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
        private const string HeroPath = "Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png";
        private const string PremiumFramePath = "Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_frame.png";
        private const string PremiumButtonPath =
            "Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_primary_button.png";

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
            foreach (var resolution in new[] { new Vector2Int(720, 1280), new Vector2Int(1080, 2340) })
            {
                ShowPanel(instance, "Patrol Panel");
                PopulatePatrolPreview(instance);
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-patrol.png"));

                ShowPanel(instance, "Weapon Research Panel");
                PopulateResearchPreview(instance, "연구 중", 0, string.Empty);
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-research-locked.png"));
                PopulateResearchPreview(instance, "해금 가능", 2000, string.Empty);
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-research-ready.png"));
                PopulateResearchPreview(instance, "엽전 부족", 2000, "엽전이 부족합니다.");
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-research-coins.png"));

                ShowPanel(instance, "Common Training Panel");
                PopulateTrainingPreview(instance);
                Capture(camera, resolution, Path.Combine(output, $"{resolution.x}x{resolution.y}-training.png"));
            }
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

            var hero = transforms.Single(item => item.name == "Hero Art").GetComponent<Image>();
            hero.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeroPath);
            if (hero.sprite == null) throw new InvalidOperationException($"Missing Lobby hero: {HeroPath}");
            hero.color = Color.white;

            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(PremiumFramePath);
            if (frame == null) throw new InvalidOperationException($"Missing Lobby frame: {PremiumFramePath}");
            foreach (var panelName in new[] { "Weapon Research Panel", "Patrol Panel", "Common Training Panel" })
            {
                var panel = transforms.Single(item => item.name == panelName).GetComponent<Image>();
                panel.sprite = frame;
                panel.type = Image.Type.Sliced;
                panel.color = Color.white;
            }

            var primarySprite = AssetDatabase.LoadAssetAtPath<Sprite>(PremiumButtonPath);
            if (primarySprite == null)
                throw new InvalidOperationException($"Missing Lobby primary button: {PremiumButtonPath}");
            var primary = transforms.Single(item => item.name == "Start Patrol").GetComponent<Image>();
            primary.sprite = primarySprite;
            primary.type = Image.Type.Sliced;
            primary.color = Color.white;

        }

        private static void PopulatePatrolPreview(GameObject instance)
        {
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            SetPreviewText(transforms, "Preset", "편성 1 · 첫 순찰대");
            SetPreviewText(transforms, "Starting Weapon", "시작 무기 · 환도 비검");
            SetPreviewText(transforms, "Style", "운용법 · 기본식");
            SetPreviewText(transforms, "Record", "최고 기록 · 아직 기록 없음");
        }

        private static void PopulateResearchPreview(GameObject instance, string state, int mastery, string feedback)
        {
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            SetPreviewText(transforms, "Research Title", "환도 비검 연구");
            SetPreviewText(transforms, "Mastery", $"숙련도 {mastery:N0} · 이 무기로 막타를 기록하면 숙련도가 오릅니다");
            SetPreviewText(transforms, "Research Feedback", feedback);
            var buttons = instance.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Style ", StringComparison.Ordinal))
                .OrderBy(button => button.name).ToArray();
            buttons[0].GetComponentInChildren<TMPro.TMP_Text>(true).text =
                "기본식 · 장착 중\n무기의 본래 운용법 / 추가 효과 없음\n처음부터 사용 가능";
            buttons[1].GetComponentInChildren<TMPro.TMP_Text>(true).text =
                $"맹독 비검 · {state}\n독 피해 강화 / 직접 피해 감소\n숙련도 2,000 · 엽전 800";
            buttons[2].GetComponentInChildren<TMPro.TMP_Text>(true).text =
                "월식 비검 · 연구 중\n강한 일격 / 재사용 대기 증가\n숙련도 8,000 · 엽전 2,400";
        }

        private static void PopulateTrainingPreview(GameObject instance)
        {
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            SetPreviewText(transforms, "Current", "현재 최대 체력 +0%");
            SetPreviewText(transforms, "Next", "강화 후 최대 체력 +2%");
            SetPreviewText(transforms, "Cost", "필요 엽전 100");
            SetPreviewText(transforms, "Training Feedback", "모든 출전에 적용되는 소규모 공통 강화입니다.");
        }

        private static void ShowPanel(GameObject instance, string selectedName)
        {
            foreach (var name in new[] { "Weapon Research Panel", "Patrol Panel", "Common Training Panel" })
            {
                var panel = instance.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == name).gameObject;
                panel.SetActive(name == selectedName);
            }
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
