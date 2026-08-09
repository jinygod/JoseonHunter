using System.Linq;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class LobbySceneContractTests
    {
        [Test]
        public void LobbySceneContainsDirectAuthoredHomeShell()
        {
            var scene = EditorSceneManager.OpenScene("Assets/JoseonHunter/Scenes/Lobby.unity", OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().Single(root => root.name == "Lobby Canvas");
            var safeArea = canvas.transform.Find("Safe Area");
            Assert.That(safeArea.Find("Bottom Navigation"), Is.Null);
            Assert.That(safeArea.Find("Home Page").GetComponentsInChildren<LobbyMenuCardView>(true), Has.Length.EqualTo(3));
            Assert.That(canvas.GetComponent<LobbyBootstrap>(), Is.Not.Null);
            Assert.That(canvas.GetComponent<LobbyRootView>().HasRequiredBindings, Is.True);
        }
    }
}
