using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplaySpawnGuide : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private float minimumMargin;
        [SerializeField] private float maximumMargin;

        public void Configure(Camera camera, float minimum, float maximum)
        {
            gameplayCamera = camera;
            minimumMargin = minimum;
            maximumMargin = maximum;
        }

        private void OnDrawGizmosSelected()
        {
            if (gameplayCamera == null)
                return;

            var distance = Mathf.Max(gameplayCamera.nearClipPlane, 0.01f);
            var bottomLeft = gameplayCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
            var bottomRight = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, 0f, distance));
            var topRight = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distance));
            var topLeft = gameplayCamera.ViewportToWorldPoint(new Vector3(0f, 1f, distance));

            DrawRectangle(bottomLeft, bottomRight, topRight, topLeft, Color.white);
            DrawRectangle(
                ExpandFromCamera(bottomLeft, minimumMargin),
                ExpandFromCamera(bottomRight, minimumMargin),
                ExpandFromCamera(topRight, minimumMargin),
                ExpandFromCamera(topLeft, minimumMargin),
                Color.yellow);
            DrawRectangle(
                ExpandFromCamera(bottomLeft, maximumMargin),
                ExpandFromCamera(bottomRight, maximumMargin),
                ExpandFromCamera(topRight, maximumMargin),
                ExpandFromCamera(topLeft, maximumMargin),
                Color.red);
        }

        private Vector3 ExpandFromCamera(Vector3 point, float margin)
        {
            return point + (point - gameplayCamera.transform.position).normalized * margin;
        }

        private static void DrawRectangle(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topRight, Vector3 topLeft, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}
