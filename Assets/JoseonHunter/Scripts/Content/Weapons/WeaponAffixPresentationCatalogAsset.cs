using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Content.Weapons
{
    [CreateAssetMenu(menuName = "JoseonHunter/Weapons/Affix Presentation Catalog")]
    public sealed class WeaponAffixPresentationCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct PotentialPresentation
        {
            [SerializeField] private string potentialId;
            [SerializeField] private Sprite sprite;
            [SerializeField] private Texture2D hitMask;

            public PotentialPresentation(WeaponPotentialId id, Sprite presentationSprite, Texture2D mask)
            {
                potentialId = id.Value;
                sprite = presentationSprite;
                hitMask = mask;
            }

            public bool Matches(WeaponPotentialId id) => string.Equals(potentialId, id.Value, StringComparison.Ordinal);
            public Sprite Sprite => sprite;
            public Texture2D HitMask => hitMask;
        }

        [Serializable]
        public struct RarityFrame
        {
            [SerializeField] private WeaponAffixTier tier;
            [SerializeField] private Sprite sprite;

            public RarityFrame(WeaponAffixTier affixTier, Sprite presentationSprite)
            {
                tier = affixTier;
                sprite = presentationSprite;
            }

            public bool Matches(WeaponAffixTier value) => tier == value;
            public Sprite Sprite => sprite;
        }

        [SerializeField] private RarityFrame[] rarityFrames;
        [SerializeField] private PotentialPresentation[] potentials;

        public Sprite SpriteForAffix(WeaponAffixTier tier) =>
            (rarityFrames ?? Array.Empty<RarityFrame>()).FirstOrDefault(frame => frame.Matches(tier)).Sprite;

        public Sprite SpriteForPotential(WeaponPotentialId potentialId) =>
            (potentials ?? Array.Empty<PotentialPresentation>()).FirstOrDefault(entry => entry.Matches(potentialId)).Sprite;

        public Texture2D MaskForPotential(WeaponPotentialId potentialId) =>
            (potentials ?? Array.Empty<PotentialPresentation>()).FirstOrDefault(entry => entry.Matches(potentialId)).HitMask;

        public IReadOnlyList<string> Validate(IEnumerable<WeaponPotentialId> requiredPotentialIds)
        {
            var errors = new List<string>();
            foreach (var tier in new[] { WeaponAffixTier.Standard, WeaponAffixTier.High, WeaponAffixTier.Perfect })
                if (SpriteForAffix(tier) == null) errors.Add($"missing rarity frame for {tier}");

            foreach (var potential in requiredPotentialIds ?? Array.Empty<WeaponPotentialId>())
            {
                if (SpriteForPotential(potential) == null) errors.Add($"missing potential sprite '{potential.Value}'");
                if (MaskForPotential(potential) == null) errors.Add($"missing potential hit mask '{potential.Value}'");
            }

            return errors;
        }

        public void SetForImport(RarityFrame[] frames, PotentialPresentation[] potentialEntries)
        {
            rarityFrames = frames;
            potentials = potentialEntries;
        }
    }
}
