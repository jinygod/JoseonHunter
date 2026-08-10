using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class PremiumLobbyUiAssetImporter
    {
        private const string Folder = "Assets/JoseonHunter/Resources/UI/PremiumJoseon";

        private static readonly string[] Sliced =
        {
            "thin_outer_frame", "header_bar", "stage_title_plate", "content_backplate",
            "difficulty_idle", "difficulty_selected", "difficulty_locked", "weapon_selector_frame",
            "primary_red_button", "secondary_dark_button", "tab_idle", "tab_selected",
            "small_item_frame"
        };

        [MenuItem("JoseonHunter/Assets/Import Premium Lobby UI")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException($"Missing texture importer: {path}");

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                SetUncompressedPlatform(importer, "Standalone");
                SetUncompressedPlatform(importer, "Android");
                SetUncompressedPlatform(importer, "WebGL");
                importer.spritePixelsPerUnit = 100f;
                var name = Path.GetFileNameWithoutExtension(path);
                var sliced = Array.IndexOf(Sliced, name) >= 0;
                importer.spriteBorder = sliced
                    ? new Vector4(name == "thin_outer_frame" ? 16f : 12f,
                        name == "thin_outer_frame" ? 16f : 12f,
                        name == "thin_outer_frame" ? 16f : 12f,
                        name == "thin_outer_frame" ? 16f : 12f)
                    : Vector4.zero;
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = sliced ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Imported premium Joseon lobby UI sprites.");
        }

        private static void SetUncompressedPlatform(TextureImporter importer, string platform)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
