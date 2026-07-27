using System;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    /// <summary>Derives binary hit-mask pixels; callers persist the returned texture as the checked-in mask PNG.</summary>
    public static class WeaponPixelAssetImporter
    {
        public static Texture2D DeriveMask(Texture2D approvedSpriteSource, Texture2D exclusionPng = null)
        {
            if (approvedSpriteSource == null) throw new ArgumentNullException(nameof(approvedSpriteSource));
            if (exclusionPng != null && (exclusionPng.width != approvedSpriteSource.width || exclusionPng.height != approvedSpriteSource.height))
                throw new ArgumentException("Exclusion PNG dimensions must match the sprite source.", nameof(exclusionPng));

            var source = approvedSpriteSource.GetPixels32();
            var exclusion = exclusionPng == null ? null : exclusionPng.GetPixels32();
            var output = new Color32[source.Length];
            for (var index = 0; index < output.Length; index++)
            {
                var active = source[index].a == byte.MaxValue && (exclusion == null || exclusion[index].a == 0);
                output[index] = active ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
            var mask = new Texture2D(approvedSpriteSource.width, approvedSpriteSource.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = approvedSpriteSource.name + "_mask"
            };
            mask.SetPixels32(output);
            mask.Apply(false, false);
            return mask;
        }
    }
}
