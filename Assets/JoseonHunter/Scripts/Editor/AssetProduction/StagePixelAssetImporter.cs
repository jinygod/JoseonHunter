using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public sealed class StagePixelAssetImporter : AssetPostprocessor
    {
        private const string StageRoot = "Assets/JoseonHunter/Art/Stages/";
        private const string EnemyRoot = "Assets/JoseonHunter/Art/Enemies/";
        private const string BossRoot = "Assets/JoseonHunter/Art/Bosses/";

        private void OnPreprocessTexture()
        {
            var normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith(StageRoot, StringComparison.Ordinal) &&
                !normalized.StartsWith(EnemyRoot, StringComparison.Ordinal) &&
                !normalized.StartsWith(BossRoot, StringComparison.Ordinal)) return;
            if (!normalized.Contains("DokkaebiPass") && !normalized.Contains("MoonlitTomb")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = normalized.StartsWith(StageRoot, StringComparison.Ordinal)
                ? TextureWrapMode.Repeat
                : TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = normalized.StartsWith(StageRoot, StringComparison.Ordinal)
                ? 32f
                : ExpectedCanvas(normalized) / 1.5f;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }

        private static float ExpectedCanvas(string path)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            return id switch
            {
                "red_horn_elite" => 64f,
                "grave_ambusher_elite" => 64f,
                "one_horn_captain" => 80f,
                "iron_shield_general" => 80f,
                "royal_guard_wraith" => 80f,
                "eclipse_priest" => 80f,
                "dokkaebi_king" => 112f,
                "eclipse_queen" => 112f,
                _ => 48f
            };
        }
    }
}
