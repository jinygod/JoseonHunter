using System;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class JangseungGeumjulAssetImporter
    {
        public const string ArtRoot = "Assets/JoseonHunter/Art/Vfx/JangseungGeumjul";
        public const string LibraryPath =
            "Assets/JoseonHunter/Content/Presentation/JangseungGeumjulVisualLibrary.asset";

        private const string RopePath = ArtRoot + "/geumjul_rope_tile.png";
        private const string AnchorPath = ArtRoot + "/geumjul_anchor.png";
        private static readonly string[] KnotPaths =
        {
            ArtRoot + "/geumjul_knot_a.png",
            ArtRoot + "/geumjul_knot_b.png"
        };
        private static readonly string[] ClosurePaths = FramePaths("geumjul_closure", 6);
        private static readonly string[] DustPaths = FramePaths("jangseung_dust", 4);
        private static readonly string[] CrossingPaths = FramePaths("jangseung_crossing", 4);

        [MenuItem("JoseonHunter/Assets/Rebuild Jangseung Geumjul Visual Library")]
        public static void Rebuild()
        {
            Configure(RopePath, TextureWrapMode.Repeat);
            Configure(AnchorPath, TextureWrapMode.Clamp);
            ConfigureAll(KnotPaths);
            ConfigureAll(ClosurePaths);
            ConfigureAll(DustPaths);
            ConfigureAll(CrossingPaths);

            EnsureFolder("Assets/JoseonHunter/Content");
            EnsureFolder("Assets/JoseonHunter/Content/Presentation");
            var library = AssetDatabase.LoadAssetAtPath<JangseungGeumjulVisualLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<JangseungGeumjulVisualLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.ConfigureForImport(
                AssetDatabase.LoadAssetAtPath<Texture2D>(RopePath),
                SpriteAt(AnchorPath),
                SpritesAt(KnotPaths),
                SpritesAt(ClosurePaths),
                SpritesAt(DustPaths),
                SpritesAt(CrossingPaths));
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureAll(string[] paths)
        {
            foreach (var path in paths) Configure(path, TextureWrapMode.Clamp);
        }

        private static void Configure(string path, TextureWrapMode wrapMode)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing checked-in sprite at '{path}'.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = wrapMode;
            importer.SaveAndReimport();
        }

        private static Sprite SpriteAt(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static Sprite[] SpritesAt(string[] paths)
        {
            var sprites = new Sprite[paths.Length];
            for (var index = 0; index < paths.Length; index++) sprites[index] = SpriteAt(paths[index]);
            return sprites;
        }

        private static string[] FramePaths(string prefix, int count)
        {
            var paths = new string[count];
            for (var index = 0; index < count; index++)
                paths[index] = ArtRoot + "/" + prefix + "_" + (index + 1).ToString("D2") + ".png";
            return paths;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
