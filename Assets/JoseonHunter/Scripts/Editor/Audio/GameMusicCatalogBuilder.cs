using System;
using System.IO;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.Audio
{
    public static class GameMusicCatalogBuilder
    {
        private const string CatalogPath = "Assets/JoseonHunter/Resources/Audio/GameMusicCatalog.asset";
        private const string MusicRoot = "Assets/JoseonHunter/Audio/Music/CC0/";

        [MenuItem("JoseonHunter/Setup/Rebuild Game Music Catalog")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath) ?? string.Empty);
            var catalog = AssetDatabase.LoadAssetAtPath<GameMusicCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameMusicCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetForImport(new[]
            {
                Entry(GameMusicRole.Lobby, "lobby_yoiyami.ogg", .34f),
                Entry(GameMusicRole.CombatEarly, "gwigok_early_asianoriental.ogg", .38f),
                Entry(GameMusicRole.CombatMid, "gwigok_mid_frozen_desert.ogg", .40f),
                Entry(GameMusicRole.CombatLate, "gwigok_late_hope.ogg", .42f),
                Entry(GameMusicRole.MidBoss, "midboss_determined_pursuit.ogg", .44f),
                Entry(GameMusicRole.FinalBoss, "finalboss_epic_battle.ogg", .46f)
            });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("JoseonHunter game music catalog rebuilt.");
        }

        private static GameMusicCatalogAsset.Entry Entry(GameMusicRole role, string filename, float volume)
        {
            var path = MusicRoot + filename;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new InvalidOperationException("Missing game music clip: " + path);
            return new GameMusicCatalogAsset.Entry(role, clip, volume);
        }
    }
}
