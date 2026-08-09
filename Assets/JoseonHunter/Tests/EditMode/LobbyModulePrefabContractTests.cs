using System;
using System.IO;
using System.Linq;
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
            AssertBindings("CommonHeader", ("Account Level", typeof(TMP_Text)),
                ("Account Progress", typeof(Image)), ("Coins", typeof(TMP_Text)));
            AssertBindings("PageHeader", ("Back Button", typeof(Button)), ("Title", typeof(TMP_Text)),
                ("Icon", typeof(Image)));
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
            AssertBindings("PrimaryActionButton", ("Button", typeof(Button)));
            AssertBindings("SecondaryActionButton", ("Button", typeof(Button)));

            foreach (var prefab in ModulePrefabs())
            {
                foreach (var image in prefab.GetComponentsInChildren<Image>(true)
                             .Where(image => image.sprite != null && image.type != Image.Type.Simple))
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
        public void CreateOrValidateDoesNotOverwriteValidModules()
        {
            LobbyModulePrefabBuilder.CreateOrValidateProductionModules();
            var before = File.ReadAllBytes(CommonHeaderPath);
            var trainingBefore = File.ReadAllBytes(ModuleRoot + "TrainingRow.prefab");
            LobbyModulePrefabBuilder.CreateOrValidateProductionModules();
            Assert.That(File.ReadAllBytes(CommonHeaderPath), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(ModuleRoot + "TrainingRow.prefab"), Is.EqualTo(trainingBefore));
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

        private static GameObject[] ModulePrefabs() => new[]
        {
            "CommonHeader", "PageHeader", "HomeMenuCard", "InfoStrip", "ProgressBar", "DifficultyCard",
            "WeaponSelectorCard", "PrimaryActionButton", "SecondaryActionButton"
            , "TrainingRow"
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
    }
}
