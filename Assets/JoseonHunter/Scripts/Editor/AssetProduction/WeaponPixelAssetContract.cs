using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class WeaponPixelAssetContract
    {
        public const float RequiredPixelsPerUnit = 32f;
        public const float PolishPixelsPerUnit = 64f;

        public static IReadOnlyList<string> Validate(
            Texture2D spriteSource,
            TextureImporter spriteImporter,
            Texture2D binaryMask,
            TextureImporter binaryMaskImporter,
            float expectedPixelsPerUnit = RequiredPixelsPerUnit)
        {
            var errors = new List<string>();
            if (spriteSource == null) errors.Add("missing sprite source");
            if (binaryMask == null) errors.Add("missing binary mask");
            if (spriteImporter == null) errors.Add("missing sprite source importer");
            if (binaryMaskImporter == null) errors.Add("missing binary mask importer");
            if (errors.Count != 0) return errors;

            ValidateImporter(spriteImporter, "sprite source", expectedPixelsPerUnit, errors);
            ValidateImporter(binaryMaskImporter, "binary mask", expectedPixelsPerUnit, errors);
            if (!spriteSource.isReadable && spriteImporter.isReadable) errors.Add("sprite source texture must be readable for runtime mask loading");
            if (!binaryMask.isReadable && binaryMaskImporter.isReadable) errors.Add("binary mask texture must be readable for runtime mask loading");
            if (spriteSource.width != binaryMask.width || spriteSource.height != binaryMask.height) errors.Add("mask and sprite dimensions must match");
            if (!spriteImporter.isReadable || !binaryMaskImporter.isReadable || !spriteSource.isReadable || !binaryMask.isReadable) return errors;
            if (spriteSource.width == binaryMask.width && spriteSource.height == binaryMask.height)
                ValidatePixels(spriteSource.GetPixels32(), binaryMask.GetPixels32(), errors);
            return errors;
        }

        public static IReadOnlyList<string> ValidatePolishFrame(
            Texture2D texture,
            TextureImporter importer,
            string assetPath)
        {
            var errors = new List<string>();
            if (texture == null) errors.Add("missing polish frame");
            if (importer == null) errors.Add("missing polish frame importer");
            if (errors.Count != 0) return errors;

            ValidateImporter(importer, "polish frame", PolishPixelsPerUnit, errors);
            if (importer.spriteImportMode != SpriteImportMode.Single)
                errors.Add("polish frame must be a single sprite");
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteAlignment != (int)SpriteAlignment.Custom
                || Math.Abs(settings.spritePivot.x - 0.5f) > 0.0001f
                || Math.Abs(settings.spritePivot.y - 0.5f) > 0.0001f)
                errors.Add("polish frame pivot must be centered");
            if (!string.Equals(Path.GetExtension(assetPath), ".png", StringComparison.OrdinalIgnoreCase))
                errors.Add("polish frame must be png");
            return errors;
        }

        private static void ValidateImporter(TextureImporter importer, string label, float expectedPixelsPerUnit, List<string> errors)
        {
            if (Math.Abs(importer.spritePixelsPerUnit - expectedPixelsPerUnit) > 0.0001f) errors.Add(label + " has invalid pixels per unit");
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) errors.Add(label + " texture compression must be uncompressed");
            if (importer.mipmapEnabled) errors.Add(label + " mipmaps must be disabled");
            if (importer.filterMode != FilterMode.Point) errors.Add(label + " filter mode must be point");
            if (!importer.isReadable) errors.Add(label + " texture must be readable for runtime mask loading");
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
