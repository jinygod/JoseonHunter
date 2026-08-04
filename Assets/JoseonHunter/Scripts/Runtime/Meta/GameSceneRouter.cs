using System.Collections;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Runtime.Meta
{
    public sealed class GameSceneRouter
    {
        public bool IsRouting { get; private set; }

        public IEnumerator LoadLobby() => Load("Lobby");
        public IEnumerator LoadGameplay() => Load("Gameplay");

        private IEnumerator Load(string sceneName)
        {
            if (IsRouting) yield break;
            IsRouting = true;
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                IsRouting = false;
                yield break;
            }

            while (!operation.isDone) yield return null;
            IsRouting = false;
        }
    }
}
