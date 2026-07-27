using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Content
{
    [CreateAssetMenu(menuName = "JoseonHunter/Static Sprite Catalog")]
    public sealed class StaticSpriteCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public Sprite sprite;
            public GameObject prefab;
        }

        [SerializeField] private Entry[] entries;

        public IReadOnlyList<Entry> Entries => entries ?? Array.Empty<Entry>();

        public bool TryGet(string id, out Entry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            foreach (var candidate in Entries)
            {
                if (candidate != null && string.Equals(candidate.id, id, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
