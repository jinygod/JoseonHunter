using System;
using System.Collections.Generic;
using JoseonHunter.Content;
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
        private readonly CombatChoiceVisualCatalog combatChoiceVisuals;

        public EnemySpriteRoster(Sprite plagueRat, Sprite legacyAlternate, IReadOnlyList<Sprite> orderedSprites,
            CombatChoiceVisualCatalog combatChoiceVisuals = null)
        {
            this.combatChoiceVisuals = combatChoiceVisuals;
            fallback = plagueRat != null ? plagueRat : legacyAlternate;
            Add("plague_rat", At(orderedSprites, 0) ?? plagueRat);
            Add("bandit", At(orderedSprites, 1) ?? legacyAlternate);
            Add("dokkaebi", At(orderedSprites, 2));
            Add("sakkat_specter", At(orderedSprites, 3));
            Add("vengeful_spirit", At(orderedSprites, 4));
            Add("shield_dokkaebi", At(orderedSprites, 2) ?? legacyAlternate);
            Add("spirit_shaman", At(orderedSprites, 4) ?? legacyAlternate);
            Add("charging_horn_ghost", At(orderedSprites, 2) ?? legacyAlternate);
            Add("splitting_rat", At(orderedSprites, 0) ?? plagueRat);
        }

        public Sprite Resolve(string contentId)
        {
            var specialFrames = combatChoiceVisuals == null ? null : combatChoiceVisuals.EnemyFrames(contentId);
            if (specialFrames != null && specialFrames.Count > 0 && specialFrames[0] != null) return specialFrames[0];
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

        public IReadOnlyList<Sprite> Frames(string contentId) => combatChoiceVisuals == null
            ? Array.Empty<Sprite>()
            : combatChoiceVisuals.EnemyFrames(contentId);

        private void Add(string contentId, Sprite sprite)
        {
            if (sprite != null) sprites[contentId] = sprite;
        }

        private static Sprite At(IReadOnlyList<Sprite> source, int index) =>
            source != null && index >= 0 && index < source.Count ? source[index] : null;
    }
}
