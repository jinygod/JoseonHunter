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
            "panel_frame", "stage_plaque_frame", "card_idle_frame", "card_selected_frame",
            "nav_idle_frame", "nav_selected_frame"
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
                importer.spritePixelsPerUnit = 100f;
                var name = Path.GetFileNameWithoutExtension(path);
                importer.spriteBorder = Array.IndexOf(Sliced, name) >= 0
                    ? new Vector4(24f, 24f, 24f, 24f)
                    : Vector4.zero;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Imported premium Joseon lobby UI sprites.");
        }
    }
}
