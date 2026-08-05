using System;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public readonly struct SpriteSliceMetadata
    {
        public SpriteSliceMetadata(string name, Rect rect, SpriteAlignment alignment, Vector2 pivot)
        {
            Name = name;
            Rect = rect;
            Alignment = alignment;
            Pivot = pivot;
        }

        public string Name { get; }
        public Rect Rect { get; }
        public SpriteAlignment Alignment { get; }
        public Vector2 Pivot { get; }
    }

    public static class SpriteSheetMetadata
    {
        public static SpriteSliceMetadata[] Read(TextureImporter importer)
        {
            var provider = CreateProvider(importer);
            var spriteRects = provider.GetSpriteRects();
            var result = new SpriteSliceMetadata[spriteRects.Length];
            for (var index = 0; index < spriteRects.Length; index++)
            {
                var spriteRect = spriteRects[index];
                result[index] = new SpriteSliceMetadata(
                    spriteRect.name,
                    spriteRect.rect,
                    spriteRect.alignment,
                    spriteRect.pivot);
            }

            return result;
        }

        public static void Write(TextureImporter importer, SpriteSliceMetadata[] slices)
        {
            if (slices == null) throw new ArgumentNullException(nameof(slices));

            var provider = CreateProvider(importer);
            var existingRects = provider.GetSpriteRects();
            if (AreEquivalent(existingRects, slices)) return;

            var spriteRects = new SpriteRect[slices.Length];
            for (var index = 0; index < slices.Length; index++)
            {
                var slice = slices[index];
                var spriteRect = FindByName(existingRects, slice.Name) ?? new SpriteRect();
                spriteRect.name = slice.Name;
                spriteRect.rect = slice.Rect;
                spriteRect.alignment = slice.Alignment;
                spriteRect.pivot = slice.Pivot;
                _ = spriteRect.spriteID;
                spriteRects[index] = spriteRect;
            }

            provider.SetSpriteRects(spriteRects);
            provider.Apply();
        }

        private static bool AreEquivalent(SpriteRect[] existingRects, SpriteSliceMetadata[] slices)
        {
            if (existingRects.Length != slices.Length) return false;

            for (var index = 0; index < slices.Length; index++)
            {
                var existing = existingRects[index];
                var requested = slices[index];
                if (!string.Equals(existing.name, requested.Name, StringComparison.Ordinal)
                    || existing.rect != requested.Rect
                    || existing.alignment != requested.Alignment
                    || existing.pivot != requested.Pivot)
                {
                    return false;
                }
            }

            return true;
        }

        private static SpriteRect FindByName(SpriteRect[] spriteRects, string name)
        {
            for (var index = 0; index < spriteRects.Length; index++)
            {
                if (string.Equals(spriteRects[index].name, name, StringComparison.Ordinal))
                {
                    return spriteRects[index];
                }
            }

            return null;
        }

        private static ISpriteEditorDataProvider CreateProvider(TextureImporter importer)
        {
            if (importer == null) throw new ArgumentNullException(nameof(importer));

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer)
                ?? throw new InvalidOperationException($"No sprite data provider is available for '{importer.assetPath}'.");
            provider.InitSpriteEditorDataProvider();
            return provider;
        }
    }
}
