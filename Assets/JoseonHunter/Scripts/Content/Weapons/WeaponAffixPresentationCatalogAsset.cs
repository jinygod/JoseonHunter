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
            [SerializeField] private Rect sourceRect;
            [SerializeField] private Vector2 pivot;

            public PotentialPresentation(WeaponPotentialId id, Sprite presentationSprite, Texture2D mask, Rect documentedSourceRect, Vector2 spritePivot)
            {
                potentialId = id.Value;
                sprite = presentationSprite;
                hitMask = mask;
                sourceRect = documentedSourceRect;
                pivot = spritePivot;
            }

            public bool Matches(WeaponPotentialId id) => string.Equals(potentialId, id.Value, StringComparison.Ordinal);
            public Sprite Sprite => sprite;
            public Texture2D HitMask => hitMask;
            public Rect SourceRect => sourceRect;
            public Vector2 Pivot => pivot;
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
        [Header("PixelLab slot-kit slices")]
        [SerializeField] private Sprite reelFrame;
        [SerializeField] private Sprite emptyLineFrame;
        [SerializeField] private Sprite jackpotBurst1;
        [SerializeField] private Sprite jackpotBurst2;
        [SerializeField] private Sprite jackpotBurst3;

        public Sprite SpriteForAffix(WeaponAffixTier tier) =>
            (rarityFrames ?? Array.Empty<RarityFrame>()).FirstOrDefault(frame => frame.Matches(tier)).Sprite;

        public Sprite SpriteForPotential(WeaponPotentialId potentialId) =>
            (potentials ?? Array.Empty<PotentialPresentation>()).FirstOrDefault(entry => entry.Matches(potentialId)).Sprite;

        public Texture2D MaskForPotential(WeaponPotentialId potentialId) =>
            (potentials ?? Array.Empty<PotentialPresentation>()).FirstOrDefault(entry => entry.Matches(potentialId)).HitMask;

        public Sprite ReelFrame => reelFrame;
        public Sprite EmptyLineFrame => emptyLineFrame;
        public Sprite JackpotBurstFor(int lines) => lines == 1 ? jackpotBurst1 : lines == 2 ? jackpotBurst2 : jackpotBurst3;

        public bool HasRequiredUiSprites => reelFrame != null && emptyLineFrame != null &&
            jackpotBurst1 != null && jackpotBurst2 != null && jackpotBurst3 != null;

        public bool TryGetPotentialPresentation(WeaponPotentialId potentialId, out PotentialPresentation presentation)
        {
            foreach (var entry in potentials ?? Array.Empty<PotentialPresentation>())
                if (entry.Matches(potentialId)) { presentation = entry; return true; }
            presentation = default;
            return false;
        }

        public IReadOnlyList<string> Validate(IEnumerable<WeaponPotentialId> requiredPotentialIds)
        {
            var errors = new List<string>();
            foreach (var tier in new[] { WeaponAffixTier.Standard, WeaponAffixTier.High, WeaponAffixTier.Perfect })
                if (SpriteForAffix(tier) == null) errors.Add($"missing rarity frame for {tier}");
            if (reelFrame == null) errors.Add("missing PixelLab reel frame");
            if (emptyLineFrame == null) errors.Add("missing PixelLab empty-line frame");
            if (jackpotBurst1 == null || jackpotBurst2 == null || jackpotBurst3 == null)
                errors.Add("missing PixelLab jackpot burst frame");

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

        public void SetSlotKitForImport(Sprite importedReelFrame, Sprite importedEmptyLineFrame,
            Sprite importedBurst1, Sprite importedBurst2, Sprite importedBurst3)
        {
            reelFrame = importedReelFrame;
            emptyLineFrame = importedEmptyLineFrame;
            jackpotBurst1 = importedBurst1;
            jackpotBurst2 = importedBurst2;
            jackpotBurst3 = importedBurst3;
        }
    }
}
