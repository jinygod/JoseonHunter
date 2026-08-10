using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class LobbyModulePrefabContractTests
    {
        private const string ModuleRoot = "Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/";
        private const string CommonHeaderPath = ModuleRoot + "CommonHeader.prefab";
        private const string TrainingIconSetPath = "Assets/JoseonHunter/Prefabs/UI/Lobby/TrainingIconSet.asset";

        [TestCase("CommonHeader", typeof(LobbyHeaderView))]
        [TestCase("PageHeader", typeof(LobbyPageHeaderView))]
        [TestCase("HomeMenuCard", typeof(LobbyMenuCardView))]
        [TestCase("ProgressBar", typeof(LobbyProgressBarView))]
        [TestCase("DifficultyCard", typeof(LobbyDifficultyCardView))]
        [TestCase("WeaponSelectorCard", typeof(LobbyWeaponSelectorCardView))]
        [TestCase("TrainingRow", typeof(LobbyTrainingRowView))]
        [TestCase("ResearchRow", typeof(LobbyResearchRowView))]
        public void ProductionModuleHasRequiredViewAndNoMissingScripts(string name, Type viewType)
        {
            var path = ModuleRoot + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(prefab.GetComponent(viewType), Is.Not.Null);
            Assert.That(prefab.GetComponent<RectTransform>(), Is.Not.Null, path);
            Assert.That(prefab.GetComponentsInChildren<Transform>(true)
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)), Is.Zero);
        }

        [Test]
        public void ModulesExposeExactDirectChildBindingsAndPremiumFrames()
        {
            AssertBindings("CommonHeader", ("Account Profile", typeof(RectTransform)),
                ("Currency Capsule", typeof(RectTransform)), ("Settings Button", typeof(Button)));
            AssertBindings("PageHeader", ("Back Button", typeof(Button)), ("Title", typeof(TMP_Text)),
                ("Icon", typeof(Image)));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + "PageHeader.prefab")
                .transform.Find("Back Button/Back Icon")?.GetComponent<Image>(), Is.Not.Null);
            AssertBindings("HomeMenuCard", ("Button", typeof(Button)), ("Title", typeof(TMP_Text)),
                ("Description", typeof(TMP_Text)), ("Icon", typeof(Image)));
            AssertBindings("InfoStrip", ("Label", typeof(TMP_Text)), ("Value", typeof(TMP_Text)));
            AssertBindings("ProgressBar", ("Track", typeof(Image)), ("Fill", typeof(Image)),
                ("Value", typeof(TMP_Text)));
            AssertBindings("DifficultyCard", ("Button", typeof(Button)), ("Label", typeof(TMP_Text)));
            AssertBindings("WeaponSelectorCard", ("Button", typeof(Button)), ("Icon", typeof(Image)),
                ("Caption", typeof(TMP_Text)), ("Weapon Name", typeof(TMP_Text)),
                ("Chevron", typeof(TMP_Text)));
            AssertBindings("TrainingRow", ("Button", typeof(Button)), ("Icon", typeof(Image)),
                ("Name", typeof(TMP_Text)), ("Rank", typeof(TMP_Text)), ("Progress", typeof(LobbyProgressBarView)));
            AssertBindings("ResearchRow", ("Stage", typeof(TMP_Text)), ("Status", typeof(TMP_Text)),
                ("Effect", typeof(TMP_Text)), ("Requirement", typeof(TMP_Text)), ("Action", typeof(Button)),
                ("Lock Overlay", typeof(Image)));
            AssertBindings("PrimaryActionButton", ("Button", typeof(Button)));
            AssertBindings("SecondaryActionButton", ("Button", typeof(Button)));

            foreach (var prefab in ModulePrefabs())
            {
                foreach (var image in prefab.GetComponentsInChildren<Image>(true)
                             .Where(image => image.sprite != null &&
                                             image.type != Image.Type.Simple &&
                                             image.type != Image.Type.Filled))
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), prefab.name + "/" + image.name);

                foreach (var button in prefab.GetComponentsInChildren<Button>(true))
                {
                    var colors = button.colors;
                    Assert.That(colors.normalColor.a, Is.GreaterThan(0f), prefab.name + "/" + button.name);
                    Assert.That(colors.highlightedColor.a, Is.GreaterThan(0f), prefab.name + "/" + button.name);
                    Assert.That(colors.pressedColor.a, Is.GreaterThan(0f), prefab.name + "/" + button.name);
                    Assert.That(colors.disabledColor.a, Is.GreaterThan(0f), prefab.name + "/" + button.name);
                }
            }
        }

        [Test]
        public void CommonHeaderIsOneCompleteResponsiveModuleWithoutLegacyDuplicates()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonHeaderPath);
            var view = prefab.GetComponent<LobbyHeaderView>();
            var profile = prefab.transform.Find("Account Profile");
            var currency = prefab.transform.Find("Currency Capsule");
            var settings = prefab.transform.Find("Settings Button");

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.Find("Account Level")?.GetComponent<TMP_Text>(), Is.Not.Null);
            Assert.That(profile.Find("Account Name")?.GetComponent<TMP_Text>(), Is.Not.Null);
            Assert.That(profile.Find("Account Experience/Account Experience Fill")?.GetComponent<Image>(), Is.Not.Null);
            Assert.That(profile.Find("Account Experience/Account Experience Text")?.GetComponent<TMP_Text>(), Is.Not.Null);
            Assert.That(currency, Is.Not.Null);
            Assert.That(currency.Find("Coin Icon")?.GetComponent<Image>(), Is.Not.Null);
            Assert.That(currency.Find("Coin Text")?.GetComponent<TMP_Text>(), Is.Not.Null);
            Assert.That(settings?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(settings.Find("Settings Icon")?.GetComponent<Image>(), Is.Not.Null);
            Assert.That(view.HasRequiredBindings, Is.True);
            Assert.That(prefab.transform.Find("Header"), Is.Null);
        }

        [Test]
        public void CreateOrValidateDoesNotOverwriteValidModules()
        {
            LobbyModulePrefabBuilder.CreateOrValidateProductionModules();
            var before = File.ReadAllBytes(CommonHeaderPath);
            var trainingBefore = File.ReadAllBytes(ModuleRoot + "TrainingRow.prefab");
            var researchBefore = File.ReadAllBytes(ModuleRoot + "ResearchRow.prefab");
            LobbyModulePrefabBuilder.CreateOrValidateProductionModules();
            Assert.That(File.ReadAllBytes(CommonHeaderPath), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(ModuleRoot + "TrainingRow.prefab"), Is.EqualTo(trainingBefore));
            Assert.That(File.ReadAllBytes(ModuleRoot + "ResearchRow.prefab"), Is.EqualTo(researchBefore));
        }

        [Test]
        public void ModuleValidationAcceptsAuthoredFilledProgressImages()
        {
            var build = typeof(LobbyModulePrefabBuilder).GetMethod("BuildProgressBar",
                BindingFlags.Static | BindingFlags.NonPublic);
            var validate = typeof(LobbyModulePrefabBuilder).GetMethod("ValidateRoot",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(build, Is.Not.Null);
            Assert.That(validate, Is.Not.Null);
            var root = (GameObject)build.Invoke(null, null);
            try
            {
                var fill = root.transform.Find("Fill").GetComponent<Image>();
                Assert.That(fill.sprite, Is.Not.Null);
                Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
                Assert.DoesNotThrow(() => validate.Invoke(null, new object[] { root, Array.Empty<string>() }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProgressModulesUsePlainFilledSpritesInsteadOfDecorativeDividers()
        {
            var fills = new[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>(CommonHeaderPath)
                    .transform.Find("Account Profile/Account Experience/Account Experience Fill").GetComponent<Image>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + "ProgressBar.prefab")
                    .transform.Find("Fill").GetComponent<Image>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + "TrainingRow.prefab")
                    .transform.Find("Progress/Fill").GetComponent<Image>()
            };
            Assert.That(fills, Is.All.Matches<Image>(fill => fill.sprite != null &&
                fill.type == Image.Type.Filled && fill.sprite.name != "divider_gold"));
        }

        [Test]
        public void WeaponSelectorCardModuleExistsWithAuthoredDirectChildren()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + "WeaponSelectorCard.prefab");

            Assert.That(prefab, Is.Not.Null, "The authored patrol page requires a reusable weapon selector module.");
            var view = prefab.GetComponent<LobbyWeaponSelectorCardView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredBindings, Is.True);
            Assert.That(prefab.GetComponent<Image>().type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(view.Background.type, Is.EqualTo(Image.Type.Sliced));
        }

        [Test]
        public void DifficultyCardModuleOwnsCompleteAuthoredLockBindings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + "DifficultyCard.prefab");
            var view = prefab.GetComponent<LobbyDifficultyCardView>();
            var lockSlash = prefab.transform.Find("Button/Lock Slash")?.GetComponent<Image>();
            var lockIcon = prefab.transform.Find("Button/Lock Icon")?.GetComponent<Image>();
            var constraint = lockSlash?.GetComponent<LockSlashConstraint>();

            Assert.That(lockSlash, Is.Not.Null, "Lock Slash must be authored under the card button.");
            Assert.That(lockIcon, Is.Not.Null, "Lock Icon must be authored under the card button.");
            Assert.That(constraint, Is.Not.Null, "Lock Slash must own its serialized layout constraint.");

            var viewType = typeof(LobbyDifficultyCardView);
            var slashProperty = viewType.GetProperty("LockSlash");
            var iconProperty = viewType.GetProperty("LockIcon");
            var constraintProperty = viewType.GetProperty("LockSlashConstraint");
            Assert.That(slashProperty, Is.Not.Null, "The view must expose its authored slash binding.");
            Assert.That(iconProperty, Is.Not.Null, "The view must expose its authored lock-icon binding.");
            Assert.That(constraintProperty, Is.Not.Null, "The view must expose its authored constraint binding.");
            Assert.That(slashProperty.GetValue(view), Is.SameAs(lockSlash));
            Assert.That(iconProperty.GetValue(view), Is.SameAs(lockIcon));
            Assert.That(constraintProperty.GetValue(view), Is.SameAs(constraint));
            Assert.That(view.HasRequiredBindings, Is.True);
        }

        [Test]
        public void TrainingIconSetBindsTheSixTaskThreeSpritesInTrainingOrder()
        {
            var iconSet = AssetDatabase.LoadAssetAtPath<LobbyTrainingIconSet>(TrainingIconSetPath);

            Assert.That(iconSet, Is.Not.Null, "Training rows require one authored icon-set asset.");
            Assert.That(iconSet.HasExactBindings, Is.True);
            var paths = iconSet.Icons.Select(AssetDatabase.GetAssetPath).ToArray();
            Assert.That(paths, Is.EqualTo(new[]
            {
                "Assets/JoseonHunter/Art/UI/Lobby/Training/training_vitality.png",
                "Assets/JoseonHunter/Art/UI/Lobby/Training/training_power.png",
                "Assets/JoseonHunter/Art/UI/Lobby/Training/training_footwork.png",
                "Assets/JoseonHunter/Art/UI/Lobby/Training/training_learning.png",
                "Assets/JoseonHunter/Art/UI/Lobby/Training/training_guard.png",
                "Assets/JoseonHunter/Art/UI/Lobby/Training/training_resonance.png"
            }));
        }

        [Test]
        public void ResearchRowModuleOwnsCompleteAuthoredLockAndActionBindings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + "ResearchRow.prefab");
            var view = prefab.GetComponent<LobbyResearchRowView>();

            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredBindings, Is.True);
            Assert.That(view.ActionButton, Is.SameAs(prefab.transform.Find("Action").GetComponent<Button>()));
            Assert.That(view.ActionText, Is.Not.Null);
            Assert.That(view.LockOverlay, Is.SameAs(prefab.transform.Find("Lock Overlay").gameObject));
        }

        [Test]
        public void CreateOrValidateRejectsExistingModuleWithNamedChildOfWrongType()
        {
            const string path = ModuleRoot + "InfoStrip.prefab";
            var original = File.ReadAllBytes(path);
            try
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var label = contents.transform.Find("Label");
                    UnityEngine.Object.DestroyImmediate(label.GetComponent<TMP_Text>());
                    label.gameObject.AddComponent<Image>();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                Assert.That(() => LobbyModulePrefabBuilder.CreateOrValidateProductionModules(),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                File.WriteAllBytes(path, original);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        [TestCase("BuildHomeMenuCard", 216f, 352f)]
        [TestCase("BuildPageHeader", 662f, 83f)]
        [TestCase("BuildProgressBar", 634f, 54f)]
        [TestCase("BuildDifficultyCard", 211f, 115f)]
        [TestCase("BuildWeaponSelectorCard", 634f, 115f)]
        [TestCase("BuildTrainingRow", 634f, 110f)]
        [TestCase("BuildResearchRow", 634f, 160f)]
        public void ResponsiveModuleDefinitionsKeepDirectContentInsidePortraitAnchors(
            string buildMethodName, float width, float height)
        {
            var build = typeof(LobbyModulePrefabBuilder).GetMethod(buildMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(build, Is.Not.Null, buildMethodName);
            var root = (GameObject)build.Invoke(null, null);
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(width, height);
                Canvas.ForceUpdateCanvases();
                foreach (RectTransform child in root.transform)
                    AssertRectInside(child, rootRect, root.name + "/" + child.name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject[] ModulePrefabs() => new[]
        {
            "CommonHeader", "PageHeader", "HomeMenuCard", "InfoStrip", "ProgressBar", "DifficultyCard",
            "WeaponSelectorCard", "PrimaryActionButton", "SecondaryActionButton"
            , "TrainingRow", "ResearchRow"
        }.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + name + ".prefab")).ToArray();

        private static void AssertBindings(string prefabName, params (string Name, Type Type)[] bindings)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + prefabName + ".prefab");
            Assert.That(prefab, Is.Not.Null, prefabName);
            foreach (var binding in bindings)
            {
                var child = prefab.transform.Find(binding.Name);
                Assert.That(child, Is.Not.Null, prefabName + " direct child " + binding.Name);
                Assert.That(child.GetComponent(binding.Type), Is.Not.Null,
                    prefabName + " direct child " + binding.Name + " " + binding.Type.Name);
            }
        }

        private static void AssertRectInside(RectTransform child, RectTransform parent, string label)
        {
            var childCorners = new Vector3[4];
            var parentCorners = new Vector3[4];
            child.GetWorldCorners(childCorners);
            parent.GetWorldCorners(parentCorners);
            foreach (var corner in childCorners)
            {
                Assert.That(corner.x, Is.InRange(parentCorners[0].x, parentCorners[2].x), label);
                Assert.That(corner.y, Is.InRange(parentCorners[0].y, parentCorners[2].y), label);
            }
        }
    }
}
