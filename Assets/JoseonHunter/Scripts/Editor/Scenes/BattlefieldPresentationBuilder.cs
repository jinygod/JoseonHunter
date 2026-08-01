using System;
using System.IO;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.Scenes
{
    public static class BattlefieldPresentationBuilder
    {
        private const string TilePath =
            "Assets/JoseonHunter/Art/World/Runtime/Battlefield/joseon_folk_field_tile.png";
        private const string PrefabPath =
            "Assets/JoseonHunter/Prefabs/World/BattlefieldChunk.prefab";
        private const string LibraryPath =
            "Assets/JoseonHunter/Resources/Presentation/BattlefieldPresentationLibrary.asset";

        private static readonly string[] DecorationPaths =
        {
            "Assets/JoseonHunter/Art/World/Runtime/Battlefield/ward_paper_scraps.png",
            "Assets/JoseonHunter/Art/World/Runtime/Battlefield/shrine_roof_fragment.png",
            "Assets/JoseonHunter/Art/World/Runtime/Battlefield/dry_reed_clump.png",
            "Assets/JoseonHunter/Art/World/Runtime/Battlefield/ritual_stone.png"
        };

        [MenuItem("JoseonHunter/Setup/Build Battlefield Presentation")]
        public static void Build()
        {
            ConfigureTileImporter();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? string.Empty);
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath) ?? string.Empty);

            var prefab = BuildChunkPrefab();
            var ground = AssetDatabase.LoadAssetAtPath<Sprite>(TilePath);
            if (ground == null) throw new InvalidOperationException($"Missing battlefield tile: {TilePath}");

            var decorationSprites = new Sprite[DecorationPaths.Length];
            for (var index = 0; index < DecorationPaths.Length; index++)
                decorationSprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(DecorationPaths[index]);

            var library = AssetDatabase.LoadAssetAtPath<BattlefieldPresentationLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<BattlefieldPresentationLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.Configure(prefab, ground, null, decorationSprites);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("JoseonHunter battlefield presentation built.");
        }

        public static void BuildInBatchMode()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static BattlefieldChunkView BuildChunkPrefab()
        {
            var root = new GameObject("Battlefield Chunk");
            try
            {
                var view = root.AddComponent<BattlefieldChunkView>();
                view.EnsureStructure();
                root.SetActive(false);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return prefab.GetComponent<BattlefieldChunkView>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureTileImporter()
        {
            AssetDatabase.ImportAsset(TilePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(TilePath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing texture importer: {TilePath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.ClearPlatformTextureSettings("Android");
            importer.SaveAndReimport();
        }
    }
}
