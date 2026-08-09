using System;
using System.IO;
using System.Linq;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby.Views;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Editor.Scenes
{
    public static class LobbyModulePrefabBuilder
    {
        private const string ModuleDirectory = "Assets/JoseonHunter/Prefabs/UI/Lobby/Modules";

        private static readonly ModuleDefinition[] Definitions =
        {
            new ModuleDefinition("CommonHeader", BuildCommonHeader, ValidateCommonHeader),
            new ModuleDefinition("PageHeader", BuildPageHeader, ValidatePageHeader),
            new ModuleDefinition("HomeMenuCard", BuildHomeMenuCard, ValidateHomeMenuCard),
            new ModuleDefinition("InfoStrip", BuildInfoStrip, ValidateInfoStrip),
            new ModuleDefinition("ProgressBar", BuildProgressBar, ValidateProgressBar),
            new ModuleDefinition("DifficultyCard", BuildDifficultyCard, ValidateDifficultyCard),
            new ModuleDefinition("WeaponSelectorCard", BuildWeaponSelectorCard, ValidateWeaponSelectorCard),
            new ModuleDefinition("PrimaryActionButton", BuildPrimaryActionButton, ValidateActionButton),
            new ModuleDefinition("SecondaryActionButton", BuildSecondaryActionButton, ValidateActionButton)
        };

        [MenuItem("JoseonHunter/Setup/Create Or Validate Lobby Modules")]
        public static void CreateOrValidateProductionModules()
        {
            Directory.CreateDirectory(ModuleDirectory);
            var createdAny = false;
            foreach (var definition in Definitions)
            {
                var path = PathFor(definition.Name);
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null)
                {
                    definition.Validate(existing);
                    continue;
                }

                var root = definition.Build();
                try
                {
                    definition.Validate(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    createdAny = true;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            if (createdAny) AssetDatabase.SaveAssets();
        }

        public static void BuildInBatchMode()
        {
            try
            {
                CreateOrValidateProductionModules();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static GameObject BuildCommonHeader()
        {
            var root = Root("CommonHeader", new Vector2(640f, 116f));
            var frame = Image("Frame", root.transform, Color.white);
            Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            PremiumPixelUiSkin.ApplyFrame(frame, PremiumFrame.HeaderBar);
            var level = Text("Account Level", root.transform, "레벨 1", 28f);
            Place(level.rectTransform, new Vector2(.05f, .5f), new Vector2(180f, 46f), Vector2.zero);
            var progress = Image("Account Progress", root.transform, new Color(.78f, .54f, .20f, 1f));
            progress.type = UnityEngine.UI.Image.Type.Filled;
            progress.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            progress.fillOrigin = 0;
            Place(progress.rectTransform, new Vector2(.5f, .5f), new Vector2(250f, 12f), Vector2.zero);
            var coins = Text("Coins", root.transform, "0", 26f, TextAlignmentOptions.Right);
            Place(coins.rectTransform, new Vector2(.94f, .5f), new Vector2(130f, 46f), Vector2.zero);
            root.AddComponent<LobbyHeaderView>().Configure(level, progress, coins);
            return root;
        }

        private static GameObject BuildPageHeader()
        {
            var root = Root("PageHeader", new Vector2(640f, 100f));
            var frame = Image("Frame", root.transform, Color.white);
            Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            PremiumPixelUiSkin.ApplyFrame(frame, PremiumFrame.HeaderBar);
            var back = Button("Back Button", root.transform, PremiumActionStyle.Secondary);
            Place(back.GetComponent<RectTransform>(), new Vector2(.08f, .5f), new Vector2(72f, 56f), Vector2.zero);
            var title = Text("Title", root.transform, "순찰", 32f);
            Place(title.rectTransform, new Vector2(.5f, .5f), new Vector2(340f, 58f), Vector2.zero);
            var icon = Image("Icon", root.transform, Color.white);
            Place(icon.rectTransform, new Vector2(.91f, .5f), new Vector2(48f, 48f), Vector2.zero);
            PremiumPixelUiSkin.ApplyIcon(icon, PremiumIcon.Patrol);
            root.AddComponent<LobbyPageHeaderView>().Configure(back, title, icon);
            return root;
        }

        private static GameObject BuildHomeMenuCard()
        {
            var root = Root("HomeMenuCard", new Vector2(600f, 170f));
            var button = Button("Button", root.transform, null);
            Stretch(button.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            PremiumPixelUiSkin.ApplyFrame(button.GetComponent<Image>(), PremiumFrame.ContentBackplate);
            var title = Text("Title", root.transform, "순찰", 30f, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(.16f, .66f), new Vector2(320f, 42f), Vector2.zero);
            var description = Text("Description", root.transform, "귀신을 물리치고 마을을 지키세요", 20f, TextAlignmentOptions.Left);
            Place(description.rectTransform, new Vector2(.16f, .34f), new Vector2(350f, 54f), Vector2.zero);
            var icon = Image("Icon", root.transform, Color.white);
            Place(icon.rectTransform, new Vector2(.84f, .5f), new Vector2(78f, 78f), Vector2.zero);
            PremiumPixelUiSkin.ApplyIcon(icon, PremiumIcon.Patrol);
            root.AddComponent<LobbyMenuCardView>().Configure(button, title, description, icon);
            return root;
        }

        private static GameObject BuildInfoStrip()
        {
            var root = Root("InfoStrip", new Vector2(600f, 72f));
            var frame = Image("Frame", root.transform, Color.white);
            Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            PremiumPixelUiSkin.ApplyFrame(frame, PremiumFrame.SmallItem);
            var label = Text("Label", root.transform, "정보", 20f, TextAlignmentOptions.Left);
            Place(label.rectTransform, new Vector2(.06f, .5f), new Vector2(230f, 38f), Vector2.zero);
            var value = Text("Value", root.transform, "0", 20f, TextAlignmentOptions.Right);
            Place(value.rectTransform, new Vector2(.94f, .5f), new Vector2(230f, 38f), Vector2.zero);
            return root;
        }

        private static GameObject BuildProgressBar()
        {
            var root = Root("ProgressBar", new Vector2(600f, 76f));
            var track = Image("Track", root.transform, Color.white);
            Stretch(track.rectTransform, 0f, 0f, 0f, 0f);
            PremiumPixelUiSkin.ApplyFrame(track, PremiumFrame.SmallItem);
            var fill = Image("Fill", root.transform, new Color(.78f, .54f, .20f, 1f));
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            Place(fill.rectTransform, new Vector2(.06f, .5f), new Vector2(430f, 14f), Vector2.zero);
            var value = Text("Value", root.transform, "0 / 0", 19f, TextAlignmentOptions.Right);
            Place(value.rectTransform, new Vector2(.94f, .5f), new Vector2(130f, 38f), Vector2.zero);
            root.AddComponent<LobbyProgressBarView>().Configure(fill, value);
            return root;
        }

        private static GameObject BuildDifficultyCard()
        {
            var root = Root("DifficultyCard", new Vector2(280f, 100f));
            var button = Button("Button", root.transform, null);
            Stretch(button.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            var label = Text("Label", root.transform, "보통", 24f);
            Stretch(label.rectTransform, 18f, 12f, 18f, 12f);
            var lockSlash = Image("Lock Slash", button.transform, new Color(.92f, .63f, .18f, .95f));
            lockSlash.sprite = null;
            lockSlash.raycastTarget = false;
            var lockSlashConstraint = lockSlash.gameObject.AddComponent<LockSlashConstraint>();
            lockSlashConstraint.Configure();
            var lockIcon = Image("Lock Icon", button.transform, Color.white);
            lockIcon.raycastTarget = false;
            var view = root.AddComponent<LobbyDifficultyCardView>();
            view.Configure(button, label, lockSlash, lockIcon, lockSlashConstraint);
            view.Render("보통", false, false);
            return root;
        }

        private static GameObject BuildWeaponSelectorCard()
        {
            var root = Root("WeaponSelectorCard", new Vector2(600f, 116f));
            var frame = root.AddComponent<Image>();
            frame.raycastTarget = false;
            PremiumPixelUiSkin.ApplyFrame(frame, PremiumFrame.ContentBackplate);

            var button = Button("Button", root.transform, null);
            Stretch(button.GetComponent<RectTransform>(), 6f, 6f, 6f, 6f);
            PremiumPixelUiSkin.ApplyFrame(button.GetComponent<Image>(), PremiumFrame.WeaponSelector);

            var icon = Image("Icon", root.transform, Color.white);
            icon.preserveAspect = true;
            Place(icon.rectTransform, new Vector2(.11f, .5f), new Vector2(76f, 76f), Vector2.zero);
            var caption = Text("Caption", root.transform, "시작 무기", 17f, TextAlignmentOptions.Left);
            Place(caption.rectTransform, new Vector2(.28f, .68f), new Vector2(170f, 34f), Vector2.zero);
            var weaponName = Text("Weapon Name", root.transform, "환도 비검", 25f, TextAlignmentOptions.Left);
            Place(weaponName.rectTransform, new Vector2(.47f, .37f), new Vector2(360f, 46f), Vector2.zero);
            var chevron = Text("Chevron", root.transform, "〉", 30f);
            Place(chevron.rectTransform, new Vector2(.91f, .5f), new Vector2(54f, 66f), Vector2.zero);

            root.AddComponent<LobbyWeaponSelectorCardView>()
                .Configure(button, icon, caption, weaponName, chevron);
            return root;
        }

        private static GameObject BuildPrimaryActionButton() => BuildActionButton("PrimaryActionButton", PremiumActionStyle.Primary);

        private static GameObject BuildSecondaryActionButton() => BuildActionButton("SecondaryActionButton", PremiumActionStyle.Secondary);

        private static GameObject BuildActionButton(string name, PremiumActionStyle style)
        {
            var root = Root(name, new Vector2(300f, 78f));
            var button = Button("Button", root.transform, style);
            Stretch(button.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            var label = Text("Label", button.transform, "확인", 25f);
            Stretch(label.rectTransform, 14f, 8f, 14f, 8f);
            return root;
        }

        private static GameObject Root(string name, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = size;
            return root;
        }

        private static Image Image(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button Button(string name, Transform parent, PremiumActionStyle? style)
        {
            var image = Image(name, parent, Color.white);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (style.HasValue) PremiumPixelUiSkin.ApplyAction(button, style.Value);
            else button.colors = Colors();
            return button;
        }

        private static TMP_Text Text(string name, Transform parent, string value, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(.96f, .89f, .71f, 1f);
            text.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        private static ColorBlock Colors() => new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1f, .9f, .86f, 1f),
            pressedColor = new Color(.72f, .72f, .72f, 1f),
            selectedColor = new Color(1f, .94f, .9f, 1f),
            disabledColor = new Color(1f, 1f, 1f, .45f),
            colorMultiplier = 1f,
            fadeDuration = .08f
        };

        private static string PathFor(string name) => ModuleDirectory + "/" + name + ".prefab";

        private static void ValidateCommonHeader(GameObject root)
        {
            ValidateRoot(root, "Account Level", "Account Progress", "Coins");
            var view = Require<LobbyHeaderView>(root);
            if (!view.HasRequiredBindings) throw new InvalidOperationException(root.name + " has incomplete header bindings.");
            RequireDirect<TMP_Text>(root, "Account Level");
            RequireDirect<UnityEngine.UI.Image>(root, "Account Progress");
            RequireDirect<TMP_Text>(root, "Coins");
        }

        private static void ValidatePageHeader(GameObject root)
        {
            ValidateRoot(root, "Back Button", "Title", "Icon");
            var view = Require<LobbyPageHeaderView>(root);
            if (view.BackButton == null || view.Title == null || view.Icon == null)
                throw new InvalidOperationException(root.name + " has incomplete page header bindings.");
            RequireDirect<Button>(root, "Back Button");
            RequireDirect<TMP_Text>(root, "Title");
            RequireDirect<UnityEngine.UI.Image>(root, "Icon");
        }

        private static void ValidateHomeMenuCard(GameObject root)
        {
            ValidateRoot(root, "Button", "Title", "Description", "Icon");
            var view = Require<LobbyMenuCardView>(root);
            if (view.Button == null || view.Title == null || view.Description == null || view.Icon == null)
                throw new InvalidOperationException(root.name + " has incomplete menu card bindings.");
            RequireDirect<Button>(root, "Button");
            RequireDirect<TMP_Text>(root, "Title");
            RequireDirect<TMP_Text>(root, "Description");
            RequireDirect<UnityEngine.UI.Image>(root, "Icon");
        }

        private static void ValidateInfoStrip(GameObject root)
        {
            ValidateRoot(root, "Label", "Value");
            RequireDirect<TMP_Text>(root, "Label");
            RequireDirect<TMP_Text>(root, "Value");
        }

        private static void ValidateProgressBar(GameObject root)
        {
            ValidateRoot(root, "Track", "Fill", "Value");
            var view = Require<LobbyProgressBarView>(root);
            if (!view.HasRequiredBindings) throw new InvalidOperationException(root.name + " has incomplete progress bindings.");
            RequireDirect<UnityEngine.UI.Image>(root, "Track");
            RequireDirect<UnityEngine.UI.Image>(root, "Fill");
            RequireDirect<TMP_Text>(root, "Value");
        }

        private static void ValidateDifficultyCard(GameObject root)
        {
            ValidateRoot(root, "Button", "Label");
            var view = Require<LobbyDifficultyCardView>(root);
            if (!view.HasRequiredBindings) throw new InvalidOperationException(root.name + " has incomplete difficulty bindings.");
            var button = RequireDirect<Button>(root, "Button");
            RequireDirect<TMP_Text>(root, "Label");
            var lockSlash = button.transform.Find("Lock Slash")?.GetComponent<Image>();
            var lockIcon = button.transform.Find("Lock Icon")?.GetComponent<Image>();
            var lockSlashConstraint = lockSlash?.GetComponent<LockSlashConstraint>();
            if (lockSlash == null || lockIcon == null || lockSlashConstraint == null)
                throw new InvalidOperationException(root.name + " has incomplete authored lock decoration.");
            if (view.LockSlash != lockSlash || view.LockIcon != lockIcon ||
                view.LockSlashConstraint != lockSlashConstraint)
                throw new InvalidOperationException(root.name + " has mismatched authored lock bindings.");
        }

        private static void ValidateWeaponSelectorCard(GameObject root)
        {
            ValidateRoot(root, "Button", "Icon", "Caption", "Weapon Name", "Chevron");
            var view = Require<LobbyWeaponSelectorCardView>(root);
            if (!view.HasRequiredBindings)
                throw new InvalidOperationException(root.name + " has incomplete weapon selector bindings.");
            var rootImage = Require<Image>(root);
            var button = RequireDirect<Button>(root, "Button");
            RequireDirect<Image>(root, "Icon");
            RequireDirect<TMP_Text>(root, "Caption");
            RequireDirect<TMP_Text>(root, "Weapon Name");
            RequireDirect<TMP_Text>(root, "Chevron");
            if (rootImage.sprite == null || rootImage.type != UnityEngine.UI.Image.Type.Sliced)
                throw new InvalidOperationException(root.name + " root must use a sliced frame.");
            var buttonImage = button.targetGraphic as Image;
            if (buttonImage == null || buttonImage.sprite == null ||
                buttonImage.type != UnityEngine.UI.Image.Type.Sliced)
                throw new InvalidOperationException(root.name + "/Button must use a sliced frame.");
        }

        private static void ValidateActionButton(GameObject root)
        {
            ValidateRoot(root, "Button");
            RequireDirect<Button>(root, "Button");
        }

        private static void ValidateRoot(GameObject root, params string[] requiredChildren)
        {
            if (root == null || root.GetComponent<RectTransform>() == null)
                throw new InvalidOperationException("Lobby module root must be a RectTransform.");
            foreach (var child in requiredChildren)
                if (root.transform.Find(child) == null)
                    throw new InvalidOperationException(root.name + " is missing direct child " + child + ".");
            var missingScripts = root.GetComponentsInChildren<Transform>(true)
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
            if (missingScripts != 0) throw new InvalidOperationException(root.name + " has missing scripts.");
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                         .Where(image => image.sprite != null && image.type != UnityEngine.UI.Image.Type.Simple))
                if (image.type != UnityEngine.UI.Image.Type.Sliced)
                    throw new InvalidOperationException(root.name + "/" + image.name + " must use a sliced frame.");
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var colors = button.colors;
                if (colors.normalColor.a <= 0f || colors.highlightedColor.a <= 0f ||
                    colors.pressedColor.a <= 0f || colors.disabledColor.a <= 0f)
                    throw new InvalidOperationException(root.name + "/" + button.name + " has incomplete colors.");
            }
        }

        private static T Require<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            if (component == null) throw new InvalidOperationException(root.name + " is missing " + typeof(T).Name + ".");
            return component;
        }

        private static T RequireDirect<T>(GameObject root, string name) where T : Component
        {
            var child = root.transform.Find(name);
            var component = child == null ? null : child.GetComponent<T>();
            if (component == null) throw new InvalidOperationException(root.name + " is missing " + name + " " + typeof(T).Name + ".");
            return component;
        }

        private sealed class ModuleDefinition
        {
            public ModuleDefinition(string name, Func<GameObject> build, Action<GameObject> validate)
            {
                Name = name;
                Build = build;
                Validate = validate;
            }

            public string Name { get; }
            public Func<GameObject> Build { get; }
            public Action<GameObject> Validate { get; }
        }
    }
}
