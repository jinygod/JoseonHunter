using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Presentation.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameAudioDirector : MonoBehaviour
    {
        private const int PoolSize = 12;

        private static GameAudioDirector instance;

        private readonly HashSet<GameAudioCueId> warnedMissingCues = new HashSet<GameAudioCueId>();
        private readonly Dictionary<GameAudioCueId, int> nextVariant = new Dictionary<GameAudioCueId, int>();
#if UNITY_INCLUDE_TESTS
        private readonly Dictionary<GameAudioCueId, int> requestCounts = new Dictionary<GameAudioCueId, int>();
#endif
        private AudioSource[] sources;
        private AudioListener fallbackListener;
        private GameAudioPriority[] sourcePriorities;
        private GameAudioClipCatalog catalog;
        private GameAudioPlaybackBudget budget;
        private bool combatEnabled = true;
        private int nextSourceIndex;

        public static GameAudioDirector Instance => instance;
        public int SourceCount => sources == null ? 0 : sources.Length;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureExists()
        {
            if (instance != null) return;
            var existing = FindAnyObjectByType<GameAudioDirector>(FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
                return;
            }

            new GameObject("Game Audio").AddComponent<GameAudioDirector>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            fallbackListener = gameObject.AddComponent<AudioListener>();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            RefreshFallbackListener();
            budget = new GameAudioPlaybackBudget(PoolSize);
            catalog = GameAudioClipCatalog.LoadDefault();
            sources = new AudioSource[PoolSize];
            sourcePriorities = new GameAudioPriority[PoolSize];
            for (var index = 0; index < sources.Length; index++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                sources[index] = source;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this) instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshFallbackListener();

        private void RefreshFallbackListener()
        {
            if (fallbackListener == null) return;

            var hasEnabledSceneListener = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
            {
                if (listener == fallbackListener || !listener.enabled || !listener.gameObject.activeInHierarchy)
                    continue;

                hasEnabledSceneListener = true;
                break;
            }

            fallbackListener.enabled = !hasEnabledSceneListener;
        }

        public void SetCombatEnabled(bool enabled) => combatEnabled = enabled;

        public bool CanRequest(GameAudioCueId cue) =>
            cue != GameAudioCueId.None && (combatEnabled || !IsCombatCue(cue));

        public bool TryPlay(GameAudioCueId cue)
        {
#if UNITY_INCLUDE_TESTS
            CountRequest(cue);
#endif
            if (!CanRequest(cue) || sources == null || catalog == null) return false;
            if (!TryResolveClip(cue, out var clip)) return false;
            var activeCount = ActiveSourceCount();
            if (!budget.TryReserve(cue, Time.unscaledTime, activeCount)) return false;
            return PlayReserved(cue, clip);
        }

        public bool TryPlayWeapon(WeaponId weaponId, int attackInstanceId)
        {
            var cue = CueForWeapon(weaponId);
#if UNITY_INCLUDE_TESTS
            CountRequest(cue);
#endif
            if (!CanRequest(cue) || sources == null || catalog == null) return false;
            if (!TryResolveClip(cue, out var clip)) return false;
            if (!budget.TryReserveWeapon(weaponId, attackInstanceId, Time.unscaledTime, ActiveSourceCount()))
                return false;
            return PlayReserved(cue, clip);
        }

        public static GameAudioCueId CueForWeapon(WeaponId weaponId)
        {
            if (weaponId.Equals(WeaponId.GakgungShot)) return GameAudioCueId.Gakgung;
            if (weaponId.Equals(WeaponId.HwandoFlyingBlade)) return GameAudioCueId.Hwando;
            if (weaponId.Equals(WeaponId.ThunderCrashBomb)) return GameAudioCueId.ThunderBomb;
            if (weaponId.Equals(WeaponId.FrostFlask)) return GameAudioCueId.FrostFlask;
            if (weaponId.Equals(WeaponId.WindThunderFan)) return GameAudioCueId.WindThunderFan;
            if (weaponId.Equals(WeaponId.TalismanThrow)) return GameAudioCueId.Talisman;
            if (weaponId.Equals(WeaponId.JangseungWard)) return GameAudioCueId.Jangseung;
            if (weaponId.Equals(WeaponId.SingijeonVolley)) return GameAudioCueId.Singijeon;
            return GameAudioCueId.Geumjul;
        }

#if UNITY_INCLUDE_TESTS
        public int RequestCountForTests(GameAudioCueId cue) =>
            requestCounts.TryGetValue(cue, out var count) ? count : 0;

        public void ResetRequestCountsForTests() => requestCounts.Clear();

        private void CountRequest(GameAudioCueId cue)
        {
            requestCounts.TryGetValue(cue, out var count);
            requestCounts[cue] = count + 1;
        }
#endif

        private bool TryResolveClip(GameAudioCueId cue, out AudioClip clip)
        {
            clip = null;
            if (!catalog.TryGet(cue, out var variants) || variants == null || variants.Length == 0)
            {
                if (warnedMissingCues.Add(cue))
                    Debug.LogWarning($"Game audio cue has no imported clip: {cue}", this);
                return false;
            }

            nextVariant.TryGetValue(cue, out var index);
            clip = variants[index % variants.Length];
            nextVariant[cue] = (index + 1) % variants.Length;
            return clip != null;
        }

        private bool PlayReserved(GameAudioCueId cue, AudioClip clip)
        {
            var priority = GameAudioPlaybackBudget.PriorityFor(cue);
            var sourceIndex = FindAvailableSource(priority);
            if (sourceIndex < 0) return false;

            var source = sources[sourceIndex];
            if (source.isPlaying) source.Stop();
            source.clip = clip;
            source.volume = VolumeFor(cue);
            source.pitch = Random.Range(.96f, 1.0401f);
            source.Play();
            sourcePriorities[sourceIndex] = priority;
            nextSourceIndex = (sourceIndex + 1) % sources.Length;
            return true;
        }

        private int FindAvailableSource(GameAudioPriority requestedPriority)
        {
            for (var offset = 0; offset < sources.Length; offset++)
            {
                var index = (nextSourceIndex + offset) % sources.Length;
                if (!sources[index].isPlaying) return index;
            }

            if (requestedPriority < GameAudioPriority.High) return -1;
            for (var offset = 0; offset < sources.Length; offset++)
            {
                var index = (nextSourceIndex + offset) % sources.Length;
                if (sourcePriorities[index] < requestedPriority) return index;
            }

            return -1;
        }

        private int ActiveSourceCount()
        {
            var count = 0;
            for (var index = 0; index < sources.Length; index++)
                if (sources[index].isPlaying) count++;
            return count;
        }

        private static bool IsCombatCue(GameAudioCueId cue)
        {
            if (cue >= GameAudioCueId.Gakgung && cue <= GameAudioCueId.EliteDefeat) return true;
            switch (cue)
            {
                case GameAudioCueId.BossWarning:
                case GameAudioCueId.BossAppear:
                case GameAudioCueId.BossDefeat:
                case GameAudioCueId.BossSlam:
                case GameAudioCueId.BossCharge:
                case GameAudioCueId.BossVolley:
                case GameAudioCueId.TreasureAppear:
                case GameAudioCueId.TreasureOpen:
                case GameAudioCueId.WaveWarning:
                case GameAudioCueId.EliteAppear:
                    return true;
                default:
                    return false;
            }
        }

        private static float VolumeFor(GameAudioCueId cue)
        {
            switch (cue)
            {
                case GameAudioCueId.ExperiencePickup:
                case GameAudioCueId.NormalHit:
                case GameAudioCueId.AppraisalTick:
                    return .42f;
                case GameAudioCueId.UiClick:
                case GameAudioCueId.YeopjeonPickup:
                    return .58f;
                case GameAudioCueId.BossWarning:
                case GameAudioCueId.BossAppear:
                case GameAudioCueId.BossDefeat:
                case GameAudioCueId.PlayerDefeat:
                case GameAudioCueId.BossSlam:
                case GameAudioCueId.BossCharge:
                case GameAudioCueId.BossVolley:
                    return .82f;
                case GameAudioCueId.PlayerHurt:
                case GameAudioCueId.EliteDefeat:
                    return .72f;
                default:
                    return .68f;
            }
        }
    }
}
