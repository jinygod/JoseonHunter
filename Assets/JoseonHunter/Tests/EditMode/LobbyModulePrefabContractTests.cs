using System;
using System.IO;
using System.Linq;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using NUnit.Framework;
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
            AssertBindings("CommonHeader", "Account Level", "Account Progress", "Coins");
            AssertBindings("PageHeader", "Back Button", "Title", "Icon");
            AssertBindings("HomeMenuCard", "Button", "Title", "Description", "Icon");
            AssertBindings("InfoStrip", "Label", "Value");
            AssertBindings("ProgressBar", "Track", "Fill", "Value");
            AssertBindings("DifficultyCard", "Button", "Label");
            AssertBindings("PrimaryActionButton", "Button");
            AssertBindings("SecondaryActionButton", "Button");

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

        private static GameObject[] ModulePrefabs() => new[]
        {
            "CommonHeader", "PageHeader", "HomeMenuCard", "InfoStrip", "ProgressBar", "DifficultyCard",
            "PrimaryActionButton", "SecondaryActionButton"
        }.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + name + ".prefab")).ToArray();

        private static void AssertBindings(string prefabName, params string[] names)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModuleRoot + prefabName + ".prefab");
            Assert.That(prefab, Is.Not.Null, prefabName);
            foreach (var name in names)
                Assert.That(prefab.transform.Find(name), Is.Not.Null, prefabName + " direct child " + name);
        }
    }
}
