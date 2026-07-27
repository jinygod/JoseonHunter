using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class WeaponPixelAssetContract
    {
        public const float RequiredPixelsPerUnit = 32f;

        public static IReadOnlyList<string> Validate(Texture2D spriteSource, Texture2D binaryMask, TextureImporter importer, float expectedPixelsPerUnit = RequiredPixelsPerUnit)
        {
            var errors = new List<string>();
            if (spriteSource == null) errors.Add("missing sprite source");
            if (binaryMask == null) errors.Add("missing binary mask");
            if (importer == null) errors.Add("missing texture importer");
            if (errors.Count != 0) return errors;

            if (Math.Abs(importer.spritePixelsPerUnit - expectedPixelsPerUnit) > 0.0001f) errors.Add("invalid pixels per unit");
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) errors.Add("texture compression must be uncompressed");
            if (importer.mipmapEnabled) errors.Add("mipmaps must be disabled");
            if (importer.filterMode != FilterMode.Point) errors.Add("filter mode must be point");
            if (!importer.isReadable) errors.Add("texture must be readable for runtime mask loading");
            if (spriteSource.width != binaryMask.width || spriteSource.height != binaryMask.height) errors.Add("mask and sprite dimensions must match");
            else ValidatePixels(spriteSource.GetPixels32(), binaryMask.GetPixels32(), errors);
            return errors;
        }

        private static void ValidatePixels(Color32[] source, Color32[] mask, List<string> errors)
        {
            for (var index = 0; index < source.Length; index++)
            {
                if (source[index].a != 0 && source[index].a != byte.MaxValue) { errors.Add("sprite alpha must be 0 or 255"); break; }
            }
            for (var index = 0; index < mask.Length; index++)
            {
                if (mask[index].a != 0 && mask[index].a != byte.MaxValue) { errors.Add("mask alpha must be 0 or 255"); break; }
                if (mask[index].a == byte.MaxValue && source[index].a != byte.MaxValue) { errors.Add("mask contains active pixel outside opaque sprite source"); break; }
            }
        }
    }
}
