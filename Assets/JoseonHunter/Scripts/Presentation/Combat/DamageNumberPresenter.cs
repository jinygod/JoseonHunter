using System;
using JoseonHunter.Domain.Combat;
using TMPro;
using UnityEngine;
using JoseonHunter.Presentation.UI;

namespace JoseonHunter.Presentation.Combat
{
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class DamageNumberPresenter : MonoBehaviour
    {
        private const float NormalLifetime = 0.48f;
        private const float BossLifetimeBonus = 0.12f;
        private const float VerticalAnchorOffset = 0.20f;
        private const float RiseDistance = 0.22f;
        private const float NormalRiseDuration = 0.30f;
        private const float CriticalPunchDuration = 0.12f;
        private const float CriticalPunchScale = 1.16f;
        private const float NormalFontSize = 2.7f;
        private const float BossFontSize = 3.15f;

        private TextMeshPro textMesh;
        private Action<DamageNumberPresenter> completed;
        private Vector3 startPosition;
        private float elapsed;
        private float lifetime;
        private float horizontalDrift;

        public bool IsActive { get; private set; }
        public bool IsCritical { get; private set; }
        public bool IsBoss { get; private set; }
        public string DisplayText => textMesh == null ? string.Empty : textMesh.text;
        public Color DisplayColor => textMesh == null ? Color.clear : textMesh.color;
        public float DisplayFontSize => textMesh == null ? 0f : textMesh.fontSize;
        public string DisplayFontName => textMesh == null || textMesh.font == null ? string.Empty : textMesh.font.name;

        private void Awake()
        {
            textMesh = GetComponent<TextMeshPro>();
            textMesh.font = RuntimeFontCatalog.For(RuntimeFontRole.Damage);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.enableWordWrapping = false;
            textMesh.fontSize = NormalFontSize;
            textMesh.sortingOrder = 30;
            ResetState();
        }

        private void Update()
        {
            if (!IsActive) return;

            elapsed += Time.deltaTime;
            // Numbers complete their rise before their visibility window ends; boss values then linger in place.
            var progress = Mathf.Clamp01(elapsed / NormalRiseDuration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            transform.position = startPosition + new Vector3(horizontalDrift * eased, RiseDistance * eased, 0f);
            transform.localScale = Vector3.one * (IsCritical && elapsed < CriticalPunchDuration
                ? Mathf.Lerp(CriticalPunchScale, 1f, elapsed / CriticalPunchDuration)
                : 1f);

            if (elapsed < lifetime) return;
            var callback = completed;
            ResetState();
            callback?.Invoke(this);
        }

        public void Play(in DamageNumberDisplay display, bool isBoss, Color accent, Action<DamageNumberPresenter> onCompleted)
        {
            if (textMesh == null) textMesh = GetComponent<TextMeshPro>();

            startPosition = new Vector3(display.ContactPoint.X,
                display.ContactPoint.Y + VerticalAnchorOffset, transform.position.z);
            horizontalDrift = Mathf.Sin(display.ContactPoint.X * 12.9898f +
                display.ContactPoint.Y * 78.233f) * 0.045f;
            transform.position = startPosition;
            elapsed = 0f;
            lifetime = NormalLifetime + (isBoss ? BossLifetimeBonus : 0f);
            IsActive = true;
            IsCritical = display.IsCritical;
            IsBoss = isBoss;
            completed = onCompleted;
            textMesh.text = display.DisplayedDamage.ToString();
            textMesh.fontSize = isBoss ? BossFontSize : NormalFontSize;
            textMesh.color = display.IsCritical ? new Color(1f, 0.79f, 0.24f, 1f) : accent;
            textMesh.fontStyle = isBoss ? FontStyles.Bold : FontStyles.Normal;
            transform.localScale = Vector3.one * (display.IsCritical ? CriticalPunchScale : 1f);
            gameObject.SetActive(true);
        }

        public void ResetState()
        {
            IsActive = false;
            IsCritical = false;
            IsBoss = false;
            elapsed = 0f;
            lifetime = 0f;
            completed = null;
            horizontalDrift = 0f;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            if (textMesh != null)
            {
                textMesh.text = string.Empty;
                textMesh.color = Color.clear;
                textMesh.fontStyle = FontStyles.Normal;
            }
        }
    }
}
