using System;
using JoseonHunter.Runtime.Audio;
using UnityEngine;

namespace JoseonHunter.Presentation.Audio
{
    [CreateAssetMenu(menuName = "JoseonHunter/Audio/Game Music Catalog", fileName = "GameMusicCatalog")]
    public sealed class GameMusicCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public Entry(GameMusicRole role, AudioClip clip, float volume)
            {
                Role = role;
                Clip = clip;
                Volume = Mathf.Clamp01(volume);
            }

            public GameMusicRole Role;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public static GameMusicCatalogAsset LoadDefault() =>
            Resources.Load<GameMusicCatalogAsset>("Audio/GameMusicCatalog");

        public bool TryGet(GameMusicRole role, out AudioClip clip, out float volume)
        {
            for (var index = 0; index < entries.Length; index++)
            {
                if (entries[index].Role != role || entries[index].Clip == null) continue;
                clip = entries[index].Clip;
                volume = Mathf.Clamp01(entries[index].Volume);
                return true;
            }

            clip = null;
            volume = 0f;
            return false;
        }

        public void SetForImport(Entry[] importedEntries) =>
            entries = importedEntries == null ? Array.Empty<Entry>() : importedEntries;
    }
}
