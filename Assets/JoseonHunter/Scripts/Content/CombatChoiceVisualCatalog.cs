using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Content
{
    [CreateAssetMenu(menuName = "Joseon Hunter/Combat Choice Visual Catalog")]
    public sealed class CombatChoiceVisualCatalog : ScriptableObject
    {
        [Serializable] public struct LegacyEntry { public string PathId; public Sprite Icon; }
        [Serializable] public struct ReactionEntry { public StatusReactionKind Kind; public Sprite Icon; }
        [Serializable] public struct EnemyEntry { public string ContentId; public Sprite[] Frames; }

        [SerializeField] private LegacyEntry[] legacyEntries = Array.Empty<LegacyEntry>();
        [SerializeField] private ReactionEntry[] reactionEntries = Array.Empty<ReactionEntry>();
        [SerializeField] private EnemyEntry[] enemyEntries = Array.Empty<EnemyEntry>();

        public Sprite LegacyIcon(WeaponLegacyPathId pathId)
        {
            for (var index = 0; index < legacyEntries.Length; index++)
                if (string.Equals(legacyEntries[index].PathId, pathId.Value, StringComparison.Ordinal))
                    return legacyEntries[index].Icon;
            return null;
        }

        public Sprite ReactionIcon(StatusReactionKind kind)
        {
            for (var index = 0; index < reactionEntries.Length; index++)
                if (reactionEntries[index].Kind == kind) return reactionEntries[index].Icon;
            return null;
        }

        public IReadOnlyList<Sprite> EnemyFrames(string contentId)
        {
            for (var index = 0; index < enemyEntries.Length; index++)
                if (string.Equals(enemyEntries[index].ContentId, contentId, StringComparison.Ordinal))
                    return enemyEntries[index].Frames ?? Array.Empty<Sprite>();
            return Array.Empty<Sprite>();
        }

        public static CombatChoiceVisualCatalog LoadDefault() =>
            Resources.Load<CombatChoiceVisualCatalog>("CombatChoiceVisualCatalog");

#if UNITY_EDITOR
        public void Configure(LegacyEntry[] legacy, ReactionEntry[] reactions, EnemyEntry[] enemies)
        {
            legacyEntries = legacy ?? Array.Empty<LegacyEntry>();
            reactionEntries = reactions ?? Array.Empty<ReactionEntry>();
            enemyEntries = enemies ?? Array.Empty<EnemyEntry>();
        }
#endif
    }
}
