using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class RuntimeFontAssetGenerator
    {
        private const string ResourceDirectory = "Assets/JoseonHunter/Resources/Fonts";
        private const string FallbackPath = ResourceDirectory + "/NotoSansKR-Dynamic SDF.asset";

        private static readonly FontDefinition[] Definitions =
        {
            new("Assets/JoseonHunter/Art/Fonts/ChosunGs.TTF", "ChosunGs-Dynamic SDF"),
            new("Assets/JoseonHunter/Art/Fonts/MaruBuri-Regular.ttf", "MaruBuri-Regular-Dynamic SDF"),
            new("Assets/JoseonHunter/Art/Fonts/MaruBuri-SemiBold.ttf", "MaruBuri-SemiBold-Dynamic SDF"),
            new("Assets/JoseonHunter/Art/Fonts/BlackAndWhitePicture-Regular.ttf", "BlackAndWhitePicture-Dynamic SDF")
        };

        [MenuItem("JoseonHunter/Assets/Generate Runtime Font Assets")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(ResourceDirectory);
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
            if (fallback == null)
            {
                throw new InvalidOperationException($"Missing fallback TMP font asset: {FallbackPath}");
            }

            foreach (var definition in Definitions)
            {
                Generate(definition, fallback);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {Definitions.Length} licensed dynamic TMP font assets.");
        }

        private static void Generate(FontDefinition definition, TMP_FontAsset fallback)
        {
            var outputPath = $"{ResourceDirectory}/{definition.AssetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            {
                return;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(definition.SourcePath);
            if (source == null)
            {
                throw new InvalidOperationException($"Missing source font: {definition.SourcePath}");
            }

            var asset = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (asset == null)
            {
                throw new InvalidOperationException($"TMP failed to create font asset from {definition.SourcePath}");
            }

            asset.name = definition.AssetName;
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            asset.atlasTextures[0].name = definition.AssetName + " Atlas";
            asset.material.name = definition.AssetName + " Material";

            AssetDatabase.CreateAsset(asset, outputPath);
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
        }

        private readonly struct FontDefinition
        {
            public FontDefinition(string sourcePath, string assetName)
            {
                SourcePath = sourcePath;
                AssetName = assetName;
            }

            public string SourcePath { get; }
            public string AssetName { get; }
        }
    }
}
