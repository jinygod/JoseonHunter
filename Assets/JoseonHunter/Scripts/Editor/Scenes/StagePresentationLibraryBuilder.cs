using System.Collections.Generic;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.Scenes
{
    public static class StagePresentationLibraryBuilder
    {
        private const string AssetPath = "Assets/JoseonHunter/Resources/StagePresentationCatalog.asset";

        [MenuItem("JoseonHunter/Build Stage Presentation Catalog")]
        public static void Build()
        {
            var sprites = new List<StagePresentationSpriteEntry>();
            AddSprites(sprites, "Assets/JoseonHunter/Art/Enemies/DokkaebiPass/",
                "club_dokkaebi", "shield_guard_dokkaebi", "iron_horn_dokkaebi",
                "stone_thrower_dokkaebi", "red_horn_elite");
            AddSprites(sprites, "Assets/JoseonHunter/Art/Bosses/DokkaebiPass/",
                "one_horn_captain", "iron_shield_general", "dokkaebi_king");
            AddSprites(sprites, "Assets/JoseonHunter/Art/Enemies/MoonlitTomb/",
                "tomb_attendant", "tomb_archer_ghost", "red_lantern_wraith",
                "curse_shaman", "grave_ambusher_elite");
            AddSprites(sprites, "Assets/JoseonHunter/Art/Bosses/MoonlitTomb/",
                "royal_guard_wraith", "eclipse_priest", "eclipse_queen");

            var stages = new List<StagePresentationEntry>();
            AddStage(stages, StageId.DokkaebiPass, "DokkaebiPass", "dokkaebi_pass");
            AddStage(stages, StageId.MoonlitTomb, "MoonlitTomb", "moonlit_tomb");

            var catalog = AssetDatabase.LoadAssetAtPath<StagePresentationCatalog>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StagePresentationCatalog>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }
            catalog.Configure(sprites.ToArray(), stages.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildFromCommandLine()
        {
            Build();
            EditorApplication.Exit(0);
        }

        private static void AddSprites(List<StagePresentationSpriteEntry> output, string root, params string[] ids)
        {
            foreach (var id in ids)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + id + ".png");
                if (sprite != null) output.Add(new StagePresentationSpriteEntry(id, sprite));
            }
        }

        private static void AddStage(List<StagePresentationEntry> output, StageId stageId, string folder, string prefix)
        {
            var root = "Assets/JoseonHunter/Art/Stages/" + folder + "/";
            var ground = AssetDatabase.LoadAssetAtPath<Sprite>(root + prefix + "_ground.png");
            if (ground == null) return;
            var alternate = AssetDatabase.LoadAssetAtPath<Sprite>(root + prefix + "_ground_alt.png") ?? ground;
            var decorations = new List<Sprite>();
            for (var index = 1; index <= 4; index++)
            {
                var decoration = AssetDatabase.LoadAssetAtPath<Sprite>(root + prefix + "_decoration_" + index + ".png");
                if (decoration != null) decorations.Add(decoration);
            }
            output.Add(new StagePresentationEntry(stageId, ground, alternate, decorations.ToArray()));
        }
    }
}
