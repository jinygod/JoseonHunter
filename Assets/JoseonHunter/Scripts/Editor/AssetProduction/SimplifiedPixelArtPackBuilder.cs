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
            "Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa",
            "Assets/JoseonHunter/Art/Animation/Enemies/Bandit",
            "Assets/JoseonHunter/Art/Animation/Enemies/PlagueRat",
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando"
        };

        private static readonly string[] References =
        {
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png",
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png"
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
