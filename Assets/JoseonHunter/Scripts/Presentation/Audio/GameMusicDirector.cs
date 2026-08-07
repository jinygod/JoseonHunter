using System.Collections;
using System.Collections.Generic;
using JoseonHunter.Runtime.Audio;
using UnityEngine;

namespace JoseonHunter.Presentation.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameMusicDirector : MonoBehaviour
    {
        private const float DefaultCrossfadeSeconds = 2f;
        private const float DefaultFadeOutSeconds = .8f;

        private static GameMusicDirector instance;

        private readonly HashSet<GameMusicRole> warnedMissingRoles = new();
        private readonly AudioSource[] sources = new AudioSource[2];
        private GameMusicCatalogAsset catalog;
        private Coroutine transition;
        private int activeSourceIndex = -1;

        public static GameMusicDirector Instance => instance;
        public GameMusicRole CurrentRole { get; private set; } = GameMusicRole.None;
        public int SourceCount => sources.Length;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureExists()
        {
            if (instance != null) return;

            var existing = FindFirstObjectByType<GameMusicDirector>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Initialize();
                return;
            }

            var root = new GameObject("Game Music Director");
            root.AddComponent<GameMusicDirector>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            if (instance != null && instance != this) return;

            instance = this;
            DontDestroyOnLoad(gameObject);
            catalog ??= GameMusicCatalogAsset.LoadDefault();

            var existingSources = GetComponents<AudioSource>();
            for (var index = 0; index < sources.Length; index++)
            {
                sources[index] = index < existingSources.Length
                    ? existingSources[index]
                    : gameObject.AddComponent<AudioSource>();
                ConfigureSource(sources[index]);
            }

            for (var index = sources.Length; index < existingSources.Length; index++)
                Destroy(existingSources[index]);
        }

        public bool Request(GameMusicRole role, float fadeSeconds = DefaultCrossfadeSeconds)
        {
            if (role == GameMusicRole.None) return false;
            if (CurrentRole == role && HasActivePlayback()) return true;

            catalog ??= GameMusicCatalogAsset.LoadDefault();
            if (catalog == null || !catalog.TryGet(role, out var clip, out var targetVolume))
            {
                if (warnedMissingRoles.Add(role))
                    Debug.LogWarning($"Background music is not configured for role '{role}'.", this);
                return false;
            }

            StopTransition();
            var outgoingIndex = activeSourceIndex;
            var incomingIndex = outgoingIndex < 0 ? 0 : 1 - outgoingIndex;
            var incoming = sources[incomingIndex];
            incoming.Stop();
            incoming.clip = clip;
            incoming.volume = 0f;
            incoming.Play();

            activeSourceIndex = incomingIndex;
            CurrentRole = role;

            if (fadeSeconds <= 0f)
            {
                StopAndClear(outgoingIndex);
                incoming.volume = targetVolume;
                return true;
            }

            var outgoingVolume = outgoingIndex >= 0 ? sources[outgoingIndex].volume : 0f;
            transition = StartCoroutine(Crossfade(
                outgoingIndex,
                outgoingVolume,
                incomingIndex,
                targetVolume,
                fadeSeconds));
            return true;
        }

        public void FadeOut(float fadeSeconds = DefaultFadeOutSeconds)
        {
            StopTransition();
            CurrentRole = GameMusicRole.None;
            activeSourceIndex = -1;

            if (fadeSeconds <= 0f)
            {
                StopAndClear(0);
                StopAndClear(1);
                return;
            }

            transition = StartCoroutine(FadeOutAll(
                sources[0].volume,
                sources[1].volume,
                fadeSeconds));
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = 0f;
        }

        private bool HasActivePlayback() =>
            activeSourceIndex >= 0 && sources[activeSourceIndex] != null && sources[activeSourceIndex].isPlaying;

        private IEnumerator Crossfade(
            int outgoingIndex,
            float outgoingVolume,
            int incomingIndex,
            float incomingVolume,
            float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                if (outgoingIndex >= 0)
                    sources[outgoingIndex].volume = Mathf.Lerp(outgoingVolume, 0f, progress);
                sources[incomingIndex].volume = Mathf.Lerp(0f, incomingVolume, progress);
                yield return null;
            }

            StopAndClear(outgoingIndex);
            sources[incomingIndex].volume = incomingVolume;
            transition = null;
        }

        private IEnumerator FadeOutAll(float firstVolume, float secondVolume, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                sources[0].volume = Mathf.Lerp(firstVolume, 0f, progress);
                sources[1].volume = Mathf.Lerp(secondVolume, 0f, progress);
                yield return null;
            }

            StopAndClear(0);
            StopAndClear(1);
            transition = null;
        }

        private void StopAndClear(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= sources.Length || sources[sourceIndex] == null) return;
            sources[sourceIndex].Stop();
            sources[sourceIndex].clip = null;
            sources[sourceIndex].volume = 0f;
        }

        private void StopTransition()
        {
            if (transition == null) return;
            StopCoroutine(transition);
            transition = null;
        }

        private void OnDestroy()
        {
            StopTransition();
            if (instance == this) instance = null;
        }
    }
}
