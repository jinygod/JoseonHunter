using UnityEngine;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyHeroMotion : MonoBehaviour
    {
        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (baseScale == Vector3.zero) baseScale = transform.localScale;
        }

        private void Update()
        {
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.2f) * .008f;
            transform.localScale = baseScale * pulse;
        }

        private void OnDisable()
        {
            transform.localScale = baseScale;
        }
    }
}
