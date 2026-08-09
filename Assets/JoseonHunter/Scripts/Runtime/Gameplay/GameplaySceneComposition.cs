using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplaySceneComposition : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform battlefieldRoot;
        [SerializeField] private Transform runtimeObjectsRoot;
        [SerializeField] private Transform runtimeSystemsRoot;
        [SerializeField] private Transform spawnGuidesRoot;
        [SerializeField] private CombatantVisualView authoredPlayer;
        [SerializeField] private GameObject uiRoot;

        private bool hasCapturedAuthoredState;
        private Vector3 capturedCameraPosition;
        private Quaternion capturedCameraRotation;
        private Vector3 capturedPlayerLocalPosition;
        private Quaternion capturedPlayerLocalRotation;
        private Vector3 capturedPlayerLocalScale;
        private bool capturedPlayerActive;

        public Camera GameplayCamera => gameplayCamera;
        public Transform BattlefieldRoot => battlefieldRoot;
        public Transform RuntimeObjectsRoot => runtimeObjectsRoot;
        public Transform RuntimeSystemsRoot => runtimeSystemsRoot;
        public Transform SpawnGuidesRoot => spawnGuidesRoot;
        public CombatantVisualView AuthoredPlayer => authoredPlayer;
        public GameObject UiRoot => uiRoot;
        public bool IsComplete => HasCompleteConfiguration();

        public void Configure(
            Camera camera,
            Transform battlefield,
            Transform runtimeObjects,
            Transform runtimeSystems,
            Transform spawnGuides,
            CombatantVisualView player,
            GameObject ui)
        {
            gameplayCamera = camera;
            battlefieldRoot = battlefield;
            runtimeObjectsRoot = runtimeObjects;
            runtimeSystemsRoot = runtimeSystems;
            spawnGuidesRoot = spawnGuides;
            authoredPlayer = player;
            uiRoot = ui;
        }

        public void CaptureAuthoredState()
        {
            if (hasCapturedAuthoredState || !HasCompleteConfiguration())
                return;

            capturedCameraPosition = gameplayCamera.transform.position;
            capturedCameraRotation = gameplayCamera.transform.rotation;
            capturedPlayerLocalPosition = authoredPlayer.transform.localPosition;
            capturedPlayerLocalRotation = authoredPlayer.transform.localRotation;
            capturedPlayerLocalScale = authoredPlayer.transform.localScale;
            capturedPlayerActive = authoredPlayer.gameObject.activeSelf;
            hasCapturedAuthoredState = true;
        }

        public void RestoreAuthoredState()
        {
            if (!hasCapturedAuthoredState || !HasCompleteConfiguration())
                return;

            gameplayCamera.transform.SetPositionAndRotation(capturedCameraPosition, capturedCameraRotation);
            authoredPlayer.transform.SetLocalPositionAndRotation(capturedPlayerLocalPosition, capturedPlayerLocalRotation);
            authoredPlayer.transform.localScale = capturedPlayerLocalScale;
            authoredPlayer.gameObject.SetActive(capturedPlayerActive);
        }

        public void ClearRunScopedChildren()
        {
            if (!HasCompleteConfiguration())
                return;

            ClearChildren(runtimeObjectsRoot, authoredPlayer.transform);
            ClearChildren(runtimeSystemsRoot, null);
        }

        private bool HasCompleteConfiguration()
        {
            if (gameplayCamera == null || battlefieldRoot == null || runtimeObjectsRoot == null ||
                runtimeSystemsRoot == null || spawnGuidesRoot == null || authoredPlayer == null || uiRoot == null)
                return false;

            var scene = gameObject.scene;
            return scene.IsValid() &&
                   gameplayCamera.gameObject.scene == scene &&
                   battlefieldRoot.gameObject.scene == scene &&
                   runtimeObjectsRoot.gameObject.scene == scene &&
                   runtimeSystemsRoot.gameObject.scene == scene &&
                   spawnGuidesRoot.gameObject.scene == scene &&
                   authoredPlayer.gameObject.scene == scene &&
                   uiRoot.scene == scene &&
                   authoredPlayer.transform.parent == runtimeObjectsRoot &&
                   !RootsOverlap(runtimeObjectsRoot, runtimeSystemsRoot);
        }

        private static bool RootsOverlap(Transform first, Transform second)
        {
            return first == second || first.IsChildOf(second) || second.IsChildOf(first);
        }

        private static void ClearChildren(Transform root, Transform preservedChild)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index);
                if (child == preservedChild)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
