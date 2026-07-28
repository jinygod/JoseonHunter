using System.Collections.Generic;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    /// <summary>Runtime bridge for the checked-in PixelLab potential cells.  Damage masks are deliberately separate from display sprites.</summary>
    internal static class WeaponPotentialVisuals
    {
        private static readonly Dictionary<WeaponPotentialId, PixelHitMask> Masks = new Dictionary<WeaponPotentialId, PixelHitMask>();
        private static WeaponAffixPresentationCatalogAsset catalog;
        private static bool loaded;

        public static bool TryGet(WeaponPotentialId id, out Sprite sprite, out PixelHitMask mask)
        {
            EnsureLoaded();
            sprite = catalog == null ? null : catalog.SpriteForPotential(id);
            if (Masks.TryGetValue(id, out mask)) return sprite != null && mask != null;
            var texture = catalog == null ? null : catalog.MaskForPotential(id);
            if (texture == null || sprite == null) { mask = null; return false; }
            mask = PixelHitMask.FromTexture(texture, sprite.pivot, sprite.pixelsPerUnit);
            Masks.Add(id, mask);
            return true;
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
        }
    }
}
