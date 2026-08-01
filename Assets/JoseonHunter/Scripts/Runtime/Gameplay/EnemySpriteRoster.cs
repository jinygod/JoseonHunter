using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class EnemySpriteRoster
    {
        private readonly Dictionary<string, Sprite> sprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly HashSet<string> warnedMissingIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Sprite fallback;

        public EnemySpriteRoster(Sprite plagueRat, Sprite legacyAlternate, IReadOnlyList<Sprite> orderedSprites)
        {
            fallback = plagueRat != null ? plagueRat : legacyAlternate;
            Add("plague_rat", At(orderedSprites, 0) ?? plagueRat);
            Add("bandit", At(orderedSprites, 1) ?? legacyAlternate);
            Add("dokkaebi", At(orderedSprites, 2));
            Add("sakkat_specter", At(orderedSprites, 3));
            Add("vengeful_spirit", At(orderedSprites, 4));
        }

        public Sprite Resolve(string contentId)
        {
            if (!string.IsNullOrEmpty(contentId) &&
                sprites.TryGetValue(contentId, out var sprite) && sprite != null)
            {
                return sprite;
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var warningId = string.IsNullOrEmpty(contentId) ? "<empty>" : contentId;
            if (warnedMissingIds.Add(warningId))
                Debug.LogWarning($"Enemy sprite roster is missing '{warningId}'; using plague rat fallback.");
#endif
            return fallback;
        }

        private void Add(string contentId, Sprite sprite)
        {
            if (sprite != null) sprites[contentId] = sprite;
        }

        private static Sprite At(IReadOnlyList<Sprite> source, int index) =>
            source != null && index >= 0 && index < source.Count ? source[index] : null;
    }
}
