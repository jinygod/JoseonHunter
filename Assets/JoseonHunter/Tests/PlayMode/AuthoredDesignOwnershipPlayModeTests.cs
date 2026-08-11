using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class AuthoredDesignOwnershipPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator CommonTrainingInitializationPreservesAuthoredActionCopyAndDesign()
        {
            var root = new GameObject("Training design ownership", typeof(RectTransform));
            var presenter = root.AddComponent<CommonTrainingPresenter>();
            var view = root.AddComponent<TrainingPageView>();
            var rows = new LobbyTrainingRowView[6];
            for (var index = 0; index < rows.Length; index++) rows[index] = CreateTrainingRow(root.transform, (CommonTrainingId)index);

            var purchase = CreateButton("Purchase", root.transform, "AUTHORED PURCHASE", out var purchaseLabel);
            var reset = CreateButton("Reset", root.transform, "AUTHORED RESET", out var resetLabel);
            var purchaseSprite = CreateSprite("Authored Purchase Sprite");
            var resetSprite = CreateSprite("Authored Reset Sprite");
            AuthorDesign(purchase, purchaseLabel, purchaseSprite, new Color(.17f, .38f, .71f, 1f), new Vector2(231f, 71f));
            AuthorDesign(reset, resetLabel, resetSprite, new Color(.46f, .19f, .62f, 1f), new Vector2(197f, 63f));

            var iconSet = AssetDatabase.LoadAssetAtPath<LobbyTrainingIconSet>(
                "Assets/JoseonHunter/Prefabs/UI/Lobby/TrainingIconSet.asset");
            view.Configure(rows, iconSet, CreateText("Current", root.transform, "Authored Current"),
                CreateText("Next", root.transform, "Authored Next"), CreateText("Cost", root.transform, "Authored Cost"),
                CreateText("Capacity", root.transform, "Authored Capacity"), purchase, reset,
                CreateText("Feedback", root.transform, "Authored Feedback"));
            presenter.ConfigureView(view);

            presenter.InitializeAuthored(CreateSession(), null);

            AssertAuthoredDesign(purchase, purchaseLabel, "AUTHORED PURCHASE", purchaseSprite,
                new Color(.17f, .38f, .71f, 1f), new Vector2(231f, 71f));
            AssertAuthoredDesign(reset, resetLabel, "AUTHORED RESET", resetSprite,
                new Color(.46f, .19f, .62f, 1f), new Vector2(197f, 63f));
            DestroySprite(purchaseSprite);
            DestroySprite(resetSprite);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PatrolInitializationPreservesAuthoredStaticCopyAndActionDesign()
        {
            var root = new GameObject("Patrol design ownership", typeof(RectTransform));
            var presenter = root.AddComponent<PatrolPresenter>();
            var view = root.AddComponent<PatrolPageView>();
            var header = CreateHeader(root.transform, out var title);
            var selector = CreateWeaponSelector(root.transform, out var caption);
            var overlay = new GameObject("Weapon Selection Overlay", typeof(RectTransform));
            overlay.transform.SetParent(root.transform, false);
            var panel = new GameObject("Weapon Selection Panel", typeof(RectTransform));
            panel.transform.SetParent(overlay.transform, false);
            var close = CreateButton("Close", panel.transform, "AUTHORED CLOSE", out var closeLabel);
            var grid = new GameObject("Weapon Grid", typeof(RectTransform));
            grid.transform.SetParent(panel.transform, false);
            var start = CreateButton("Start", root.transform, "AUTHORED START", out var startLabel);
            var closeSprite = CreateSprite("Authored Close Sprite");
            var startSprite = CreateSprite("Authored Start Sprite");
            AuthorDesign(close, closeLabel, closeSprite, new Color(.25f, .57f, .36f, 1f), new Vector2(177f, 51f));
            AuthorDesign(start, startLabel, startSprite, new Color(.69f, .31f, .16f, 1f), new Vector2(293f, 79f));

            view.Configure(header, CreateText("Stage", root.transform, "Authored Stage"),
                CreateText("Status", root.transform, "Authored Status"), CreateButton("Previous", root.transform, "<", out _),
                CreateButton("Next", root.transform, ">", out _), CreateImage("Hero", root.transform),
                CreateDifficulty(root.transform), CreateDifficulty(root.transform), CreateDifficulty(root.transform), selector,
                CreateText("Feedback", root.transform, "Authored Feedback"), overlay, close, start);
            presenter.ConfigureView(view);

            presenter.InitializeAuthored(CreateSession(), null);

            Assert.That(title.text, Is.EqualTo("AUTHORED PATROL TITLE"));
            Assert.That(caption.text, Is.EqualTo("AUTHORED WEAPON CAPTION"));
            AssertAuthoredDesign(close, closeLabel, "AUTHORED CLOSE", closeSprite,
                new Color(.25f, .57f, .36f, 1f), new Vector2(177f, 51f));
            AssertAuthoredDesign(start, startLabel, "AUTHORED START", startSprite,
                new Color(.69f, .31f, .16f, 1f), new Vector2(293f, 79f));
            DestroySprite(closeSprite);
            DestroySprite(startSprite);
            Object.Destroy(root);
            yield return null;
        }

        private static MetaGameSession CreateSession() => MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));

        private static void AssertAuthoredDesign(Button button, TMP_Text label, string expectedText, Sprite expectedSprite,
            Color expectedColor, Vector2 expectedSize)
        {
            Assert.That(label.text, Is.EqualTo(expectedText));
            Assert.That(button.targetGraphic, Is.SameAs(button.GetComponent<Image>()));
            Assert.That(button.GetComponent<Image>().sprite, Is.SameAs(expectedSprite));
            Assert.That(button.GetComponent<Image>().color, Is.EqualTo(expectedColor));
            Assert.That(button.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(expectedSize));
            Assert.That(label.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(13f, -9f)));
        }

        private static void AuthorDesign(Button button, TMP_Text label, Sprite sprite, Color color, Vector2 size)
        {
            button.GetComponent<Image>().sprite = sprite;
            button.GetComponent<Image>().color = color;
            button.GetComponent<RectTransform>().sizeDelta = size;
            label.rectTransform.anchoredPosition = new Vector2(13f, -9f);
        }

        private static LobbyTrainingRowView CreateTrainingRow(Transform parent, CommonTrainingId id)
        {
            var root = new GameObject("Training Row", typeof(RectTransform)); root.transform.SetParent(parent, false);
            var button = CreateButton("Row Button", root.transform, "Row", out _);
            var progressRoot = new GameObject("Progress", typeof(RectTransform)); progressRoot.transform.SetParent(root.transform, false);
            var progress = progressRoot.AddComponent<LobbyProgressBarView>();
            progress.Configure(CreateImage("Fill", progressRoot.transform), CreateText("Value", progressRoot.transform, string.Empty));
            var row = root.AddComponent<LobbyTrainingRowView>();
            row.Configure(id, button, CreateText("Name", root.transform, string.Empty), CreateImage("Icon", root.transform),
                CreateText("Rank", root.transform, string.Empty), progress);
            return row;
        }

        private static LobbyPageHeaderView CreateHeader(Transform parent, out TMP_Text title)
        {
            var root = new GameObject("Header", typeof(RectTransform)); root.transform.SetParent(parent, false);
            var header = root.AddComponent<LobbyPageHeaderView>();
            title = CreateText("Title", root.transform, "AUTHORED PATROL TITLE");
            header.Configure(CreateButton("Back", root.transform, "Back", out _), title, CreateImage("Icon", root.transform));
            return header;
        }

        private static LobbyWeaponSelectorCardView CreateWeaponSelector(Transform parent, out TMP_Text caption)
        {
            var root = new GameObject("Selector", typeof(RectTransform)); root.transform.SetParent(parent, false);
            var selector = root.AddComponent<LobbyWeaponSelectorCardView>();
            caption = CreateText("Caption", root.transform, "AUTHORED WEAPON CAPTION");
            selector.Configure(CreateButton("Button", root.transform, "Choose", out _), CreateImage("Icon", root.transform), caption,
                CreateText("Weapon", root.transform, "Authored Weapon"), CreateText("Chevron", root.transform, ">"));
            return selector;
        }

        private static LobbyDifficultyCardView CreateDifficulty(Transform parent)
        {
            var root = new GameObject("Difficulty", typeof(RectTransform)); root.transform.SetParent(parent, false);
            var button = CreateButton("Button", root.transform, "Difficulty", out _);
            var slash = CreateImage("Lock Slash", button.transform);
            var constraint = slash.gameObject.AddComponent<LockSlashConstraint>(); constraint.Configure();
            var icon = CreateImage("Lock Icon", button.transform);
            var view = root.AddComponent<LobbyDifficultyCardView>();
            view.Configure(button, CreateText("Label", root.transform, "Authored Difficulty"), slash, icon, constraint);
            return view;
        }

        private static Button CreateButton(string name, Transform parent, string labelValue, out TMP_Text label)
        {
            var image = CreateImage(name, parent);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            label = CreateText("Label", button.transform, labelValue);
            return button;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false); text.text = value;
            return text;
        }

        private static Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(2, 2) { name = name + " Texture" };
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f)); sprite.name = name;
            return sprite;
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null) return;
            var texture = sprite.texture;
            Object.Destroy(sprite);
            Object.Destroy(texture);
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }
    }
}
