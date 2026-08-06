using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
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
            if (intensity >= 100) return new FeedbackProfile(100, 0.055f, 0.14f, true);
            if (intensity == 80) return new FeedbackProfile(80, 0.03f, 0.075f, true);
            return new FeedbackProfile(70, 0f, 0f, true);
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
        private GameFlowCoordinator flow;
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
            SubscribeFlow();
            Camera.onPreCull += OnCameraPreCull;
            Camera.onPostRender += OnCameraPostRender;
        }
        private void OnDisable()
        {
            Unsubscribe();
            UnsubscribeFlow();
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPostRender -= OnCameraPostRender;
            RestoreRenderBaseline();
        }

        private void OnDestroy()
        {
            Unbind();
            BindGameFlow(null);
            if (flashSprite != null) Destroy(flashSprite);
            if (flashTexture != null) Destroy(flashTexture);
        }

        private void Update()
        {
            var delta = Time.unscaledDeltaTime;
            UpdateFlashes(delta);
            impulseRemaining = Mathf.Max(0f, impulseRemaining - delta);
        }

        private void OnCameraPreCull(Camera camera)
        {
            // A previous camera can be skipped when cameras switch or rendering is interrupted.
            RestoreRenderBaseline();
            if (flow != null && !flow.IsGameplayRunning) return;
            if (camera == null || camera != Camera.main || impulseRemaining <= 0f || impulseMagnitude <= 0f) return;

            var amount = impulseMagnitude * Mathf.Clamp01(impulseRemaining / 0.055f);
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
        }

        public void BindGameFlow(GameFlowCoordinator value)
        {
            if (ReferenceEquals(flow, value)) return;
            UnsubscribeFlow();
            flow = value;
            SubscribeFlow();
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

        private void SubscribeFlow()
        {
            if (isActiveAndEnabled && flow != null) flow.StateChanged += OnFlowStateChanged;
        }

        private void UnsubscribeFlow()
        {
            if (flow != null) flow.StateChanged -= OnFlowStateChanged;
        }

        private void OnFlowStateChanged(GameFlowState previous, GameFlowState current)
        {
            if (current == GameFlowState.Playing) return;
            impulseRemaining = 0f;
            RestoreRenderBaseline();
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent confirmed)
        {
            var killed = isTargetAlive != null && !isTargetAlive(confirmed.TargetRuntimeId);
            if (flow == null || flow.IsGameplayRunning)
            {
                GameAudioDirector.EnsureExists();
                var audio = GameAudioDirector.Instance;
                audio?.TryPlayWeapon(confirmed.WeaponId, confirmed.AttackInstanceId);
                audio?.TryPlay(confirmed.IsCritical ? GameAudioCueId.CriticalHit : GameAudioCueId.NormalHit);
            }

            var request = new FeedbackRequest(confirmed.IsCritical, killed,
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

        private void BeginHitStop(float duration) => flow?.RequestHitStop(duration);

        private void CreateFlashPool()
        {
            const int size = 9;
            flashTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ContactSparkTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            var center = size / 2;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Abs(x - center);
                var dy = Mathf.Abs(y - center);
                var core = dx <= 1 && dy <= 1;
                var ray = (dx == 0 && dy <= 4) || (dy == 0 && dx <= 4);
                var diagonal = dx == dy && dx <= 2;
                pixels[y * size + x] = core || ray || diagonal ? Color.white : Color.clear;
            }
            flashTexture.SetPixels(pixels);
            flashTexture.Apply();
            flashSprite = Sprite.Create(
                flashTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(.5f, .5f),
                size);
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
            flash.Renderer.transform.rotation = Quaternion.Euler(
                0f, 0f, UnityEngine.Random.Range(0, 4) * 45f);
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
