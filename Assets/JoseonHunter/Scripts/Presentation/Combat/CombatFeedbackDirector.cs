using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Combat;
using UnityEngine;

namespace JoseonHunter.Presentation.Combat
{
    public readonly struct FeedbackRequest
    {
        public FeedbackRequest(bool critical, bool killed, bool boss, bool reducedEffects)
        {
            Critical = critical;
            Killed = killed;
            Boss = boss;
            ReducedEffects = reducedEffects;
        }

        public bool Critical { get; }
        public bool Killed { get; }
        public bool Boss { get; }
        public bool ReducedEffects { get; }
    }

    public readonly struct FeedbackProfile
    {
        public FeedbackProfile(int intensity, float hitStopSeconds, float cameraImpulse, bool showContactFlash)
        {
            Intensity = intensity;
            HitStopSeconds = hitStopSeconds;
            CameraImpulse = cameraImpulse;
            ShowContactFlash = showContactFlash;
        }

        public int Intensity { get; }
        public float HitStopSeconds { get; }
        public float CameraImpulse { get; }
        public bool ShowContactFlash { get; }
    }

    public static class CombatFeedbackBudget
    {
        public static FeedbackProfile Resolve(FeedbackRequest request)
        {
            var intensity = request.Boss && request.Killed ? 100 : request.Killed || request.Critical ? 80 : 70;
            if (request.ReducedEffects) return new FeedbackProfile(intensity, 0f, 0f, true);
            return intensity == 80
                ? new FeedbackProfile(80, 0.035f, 0.08f, true)
                : new FeedbackProfile(70, 0f, 0f, true);
        }
    }

    /// <summary>Presentation-only contact feedback driven by already-confirmed combat damage.</summary>
    public sealed class CombatFeedbackDirector : MonoBehaviour
    {
        private const int FlashPoolSize = 12;
        private const float FlashLifetime = 0.12f;

        private sealed class ContactFlash
        {
            public SpriteRenderer Renderer;
            public float Remaining;
            public float Lifetime;
        }

        private readonly List<ContactFlash> flashes = new List<ContactFlash>();
        private CombatDamageService damageService;
        private Func<int, bool> isTargetAlive;
        private Texture2D flashTexture;
        private Sprite flashSprite;
        private float hitStopRemaining;
        private float timeScaleBeforeHitStop = 1f;
        private bool ownsHitStop;
        private float impulseRemaining;
        private float impulseMagnitude;
        private Camera renderCamera;
        private Vector3 renderBaseline;
        private bool hasRenderScopedImpulse;

        /// <summary>Can be set by an accessibility/preferences owner when reduced visual motion is enabled.</summary>
        public bool ReducedEffects { get; set; }

        private void Awake()
        {
            CreateFlashPool();
        }

        private void OnEnable()
        {
            Subscribe();
            Camera.onPreCull += OnCameraPreCull;
            Camera.onPostRender += OnCameraPostRender;
        }
        private void OnDisable()
        {
            Unsubscribe();
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPostRender -= OnCameraPostRender;
            RestoreHitStop();
            RestoreRenderBaseline();
        }

        private void OnDestroy()
        {
            Unbind();
            if (flashSprite != null) Destroy(flashSprite);
            if (flashTexture != null) Destroy(flashTexture);
        }

        private void Update()
        {
            var delta = Time.unscaledDeltaTime;
            UpdateFlashes(delta);
            UpdateHitStop(delta);
            impulseRemaining = Mathf.Max(0f, impulseRemaining - delta);
        }

        private void OnCameraPreCull(Camera camera)
        {
            // A previous camera can be skipped when cameras switch or rendering is interrupted.
            RestoreRenderBaseline();
            if (camera == null || camera != Camera.main || impulseRemaining <= 0f || impulseMagnitude <= 0f) return;

            var amount = impulseMagnitude * (impulseRemaining / 0.035f);
            renderCamera = camera;
            renderBaseline = camera.transform.position;
            camera.transform.position = renderBaseline + (Vector3)(UnityEngine.Random.insideUnitCircle * amount);
            hasRenderScopedImpulse = true;
        }

        private void OnCameraPostRender(Camera camera)
        {
            if (camera == renderCamera) RestoreRenderBaseline();
        }

        public void Bind(CombatDamageService service)
        {
            if (ReferenceEquals(damageService, service)) return;
            Unsubscribe();
            damageService = service ?? throw new ArgumentNullException(nameof(service));
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            damageService = null;
            RestoreHitStop();
        }

        public void SetTargetAlivePredicate(Func<int, bool> predicate) => isTargetAlive = predicate;

        private void Subscribe()
        {
            if (isActiveAndEnabled && damageService != null) damageService.DamageConfirmed += OnDamageConfirmed;
        }

        private void Unsubscribe()
        {
            if (damageService != null) damageService.DamageConfirmed -= OnDamageConfirmed;
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent confirmed)
        {
            var request = new FeedbackRequest(confirmed.IsCritical, isTargetAlive != null && !isTargetAlive(confirmed.TargetRuntimeId),
                confirmed.IsBossTarget, ReducedEffects);
            var profile = CombatFeedbackBudget.Resolve(request);
            if (profile.ShowContactFlash) ShowFlash(confirmed.ContactPoint, profile.Intensity);
            if (profile.HitStopSeconds > 0f) BeginHitStop(profile.HitStopSeconds);
            if (profile.CameraImpulse > 0f)
            {
                impulseRemaining = Mathf.Max(impulseRemaining, profile.HitStopSeconds);
                impulseMagnitude = Mathf.Max(impulseMagnitude, profile.CameraImpulse);
            }
        }

        private void BeginHitStop(float duration)
        {
            if (UpgradeChoiceOwnsPause() || Time.timeScale <= 0f) return;
            if (!ownsHitStop)
            {
                timeScaleBeforeHitStop = Time.timeScale;
                ownsHitStop = true;
                Time.timeScale = 0f;
            }
            hitStopRemaining = Mathf.Max(hitStopRemaining, duration);
        }

        private void UpdateHitStop(float delta)
        {
            if (!ownsHitStop) return;
            hitStopRemaining -= delta;
            if (hitStopRemaining <= 0f) RestoreHitStop();
        }

        private void RestoreHitStop()
        {
            if (!ownsHitStop) return;
            ownsHitStop = false;
            hitStopRemaining = 0f;
            // Upgrade choice owns its own slowdown/pause, so this director must never unpause it.
            if (!UpgradeChoiceOwnsPause()) Time.timeScale = timeScaleBeforeHitStop;
            timeScaleBeforeHitStop = 1f;
        }

        private static bool UpgradeChoiceOwnsPause()
        {
            var presenter = UnityEngine.Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            return presenter != null && presenter.IsOpen;
        }

        private void CreateFlashPool()
        {
            flashTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            flashTexture.SetPixel(0, 0, Color.white);
            flashTexture.Apply();
            flashSprite = Sprite.Create(flashTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f), 1f);
            for (var index = 0; index < FlashPoolSize; index++)
            {
                var flashObject = new GameObject("Contact Flash", typeof(SpriteRenderer));
                flashObject.transform.SetParent(transform, false);
                flashObject.SetActive(false);
                var renderer = flashObject.GetComponent<SpriteRenderer>();
                renderer.sprite = flashSprite;
                renderer.sortingOrder = 20;
                flashes.Add(new ContactFlash { Renderer = renderer });
            }
        }

        private void ShowFlash(Float2 contactPoint, int intensity)
        {
            var flash = flashes.Find(candidate => !candidate.Renderer.gameObject.activeSelf) ?? flashes[0];
            flash.Renderer.transform.position = new Vector3(contactPoint.X, contactPoint.Y, 0f);
            flash.Renderer.transform.localScale = Vector3.one * (intensity >= 100 ? .56f : intensity == 80 ? .42f : .30f);
            flash.Renderer.color = intensity >= 100 ? new Color(1f, .42f, .12f, .95f) : intensity == 80 ? new Color(1f, .79f, .24f, .9f) : new Color(1f, 1f, 1f, .7f);
            flash.Remaining = flash.Lifetime = FlashLifetime;
            flash.Renderer.gameObject.SetActive(true);
        }

        private void UpdateFlashes(float delta)
        {
            foreach (var flash in flashes)
            {
                if (!flash.Renderer.gameObject.activeSelf) continue;
                flash.Remaining -= delta;
                var color = flash.Renderer.color;
                color.a = Mathf.Clamp01(flash.Remaining / flash.Lifetime);
                flash.Renderer.color = color;
                if (flash.Remaining <= 0f) flash.Renderer.gameObject.SetActive(false);
            }
        }

        private void RestoreRenderBaseline()
        {
            if (hasRenderScopedImpulse && renderCamera != null) renderCamera.transform.position = renderBaseline;
            renderCamera = null;
            renderBaseline = Vector3.zero;
            hasRenderScopedImpulse = false;
        }
    }
}
