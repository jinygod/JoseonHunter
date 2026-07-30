using System.IO;
using JoseonHunter.Editor.Scenes;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class SimplifiedPixelArtPackBuilder
    {
        private static readonly string[] Roots =
        {
            "Assets/JoseonHunter/Art/Animation",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Elites",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups",
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish"
        };

        private static readonly string[] References =
        {
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png"
        };

        [MenuItem("JoseonHunter/Assets/Reimport Simplified Combat Pack")]
        public static void Rebuild()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            foreach (var root in Roots)
            {
                foreach (var path in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                    AssetDatabase.ImportAsset(path.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            }
            foreach (var path in References)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            CombatMotionLibraryBuilder.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("Simplified combat pixel pack reimported and motion library rebuilt.");
        }
    }
}
