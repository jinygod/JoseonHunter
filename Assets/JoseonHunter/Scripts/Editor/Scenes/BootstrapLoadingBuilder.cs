using System;
using System.IO;
using JoseonHunter.Presentation.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoseonHunter.Editor.Scenes
{
    public static class BootstrapLoadingBuilder
    {
        private const string BootstrapScenePath = "Assets/JoseonHunter/Scenes/Bootstrap.unity";
        private const string PrefabPath = "Assets/JoseonHunter/Prefabs/UI/BootstrapLoading.prefab";
        private const string SpiritFlamePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/experience_spirit_flame.png";

        [MenuItem("JoseonHunter/Setup/Build Bootstrap Loading")]
        public static void Build()
        {
            RefuseDirtyBootstrap();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? string.Empty);
            var prefab = BuildPrefab();

            var scene = FindLoadedBootstrap();
            var openedForBuild = !scene.IsValid();
            if (openedForBuild)
                scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    UnityEngine.Object.DestroyImmediate(root);
                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate Bootstrap loading prefab.");
                instance.name = "Bootstrap Loading";
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
                    throw new InvalidOperationException("Could not save Bootstrap scene.");
                AssetDatabase.SaveAssets();
                Debug.Log("JoseonHunter Bootstrap loading presentation built.");
            }
            finally
            {
                if (openedForBuild && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
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

        private static GameObject BuildPrefab()
        {
            var root = new GameObject(
                "Bootstrap Loading",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(BootstrapLoadingPresenter));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(720f, 1280f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = .5f;

                var background = Image("Opaque Ink Background", root.transform,
                    new Color(.018f, .026f, .035f, 1f));
                Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

                var halo = Image("Spirit Halo", root.transform, new Color(.08f, .33f, .31f, .16f));
                SetAnchored(halo.rectTransform, new Vector2(.5f, .5f), new Vector2(360f, 360f), new Vector2(0f, 70f));

                var flame = Image("Spirit Flame", root.transform, Color.white);
                flame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpiritFlamePath);
                flame.preserveAspect = true;
                SetAnchored(flame.rectTransform, new Vector2(.5f, .5f), new Vector2(154f, 154f), new Vector2(0f, 75f));

                var title = Text("Title", root.transform, "조선 요괴 사냥꾼", 48f,
                    TextAlignmentOptions.Center, RuntimeFontRole.Title);
                title.color = new Color(.91f, .82f, .57f, 1f);
                title.fontStyle = FontStyles.Bold;
                SetAnchored(title.rectTransform, new Vector2(.5f, .5f), new Vector2(620f, 96f), new Vector2(0f, 260f));

                var subtitle = Text("Subtitle", root.transform, "어둠 속 길을 밝히는 중…", 24f,
                    TextAlignmentOptions.Center, RuntimeFontRole.Body);
                subtitle.color = new Color(.77f, .78f, .72f, 1f);
                SetAnchored(subtitle.rectTransform, new Vector2(.5f, .5f), new Vector2(620f, 60f), new Vector2(0f, -88f));

                var track = Image("Brush Progress Track", root.transform, new Color(.08f, .095f, .105f, 1f));
                SetAnchored(track.rectTransform, new Vector2(.5f, .5f), new Vector2(480f, 14f), new Vector2(0f, -160f));
                var fill = Image("Brush Progress Fill", track.transform, new Color(.82f, .62f, .27f, 1f));
                Stretch(fill.rectTransform, 0f, 2f, 0f, 2f);
                fill.rectTransform.pivot = new Vector2(0f, .5f);
                fill.rectTransform.anchorMax = new Vector2(0f, 1f);

                var accent = Image("Red Seal Accent", root.transform, new Color(.55f, .12f, .105f, 1f));
                SetAnchored(accent.rectTransform, new Vector2(.5f, .5f), new Vector2(54f, 8f), new Vector2(0f, -204f));

                root.GetComponent<BootstrapLoadingPresenter>().Configure(
                    root.GetComponent<CanvasGroup>(),
                    fill.rectTransform,
                    flame.rectTransform,
                    subtitle);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Image Image(string name, Transform parent, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = rect.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI Text(
            string name,
            Transform parent,
            string value,
            float size,
            TextAlignmentOptions alignment,
            RuntimeFontRole role)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var text = rect.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.raycastTarget = false;
            var font = RuntimeFontCatalog.For(role);
            if (font != null) text.font = font;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetAnchored(
            RectTransform rect,
            Vector2 anchor,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void RefuseDirtyBootstrap()
        {
            var scene = FindLoadedBootstrap();
            if (scene.IsValid() && scene.isDirty)
                throw new InvalidOperationException("Cannot replace a dirty open Bootstrap scene.");
        }

        private static Scene FindLoadedBootstrap()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && scene.path == BootstrapScenePath) return scene;
            }
            return default;
        }
    }
}
