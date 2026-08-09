using System;
using System.IO;
using System.Linq;
using JoseonHunter.Editor.Scenes;
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

        [TestCase("CommonHeader", typeof(LobbyHeaderView))]
        [TestCase("PageHeader", typeof(LobbyPageHeaderView))]
        [TestCase("HomeMenuCard", typeof(LobbyMenuCardView))]
        [TestCase("ProgressBar", typeof(LobbyProgressBarView))]
        [TestCase("DifficultyCard", typeof(LobbyDifficultyCardView))]
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
            LobbyModulePrefabBuilder.CreateOrValidateProductionModules();
            Assert.That(File.ReadAllBytes(CommonHeaderPath), Is.EqualTo(before));
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
            "PrimaryActionButton", "SecondaryActionButton"
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
