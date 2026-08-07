using System.Collections.Generic;
using JoseonHunter.Runtime.Audio;
using UnityEngine;

namespace JoseonHunter.Presentation.Audio
{
    internal sealed class GameAudioClipCatalog
    {
        private readonly Dictionary<GameAudioCueId, AudioClip[]> clips =
            new Dictionary<GameAudioCueId, AudioClip[]>();

        public static GameAudioClipCatalog LoadDefault()
        {
            var catalog = new GameAudioClipCatalog();
            catalog.Add(GameAudioCueId.UiClick, "Audio/CC0/UI/ui_click");
            catalog.Add(GameAudioCueId.UiConfirm, "Audio/CC0/UI/ui_confirm");
            catalog.Add(GameAudioCueId.UiCancel, "Audio/CC0/UI/ui_click");
            catalog.Add(GameAudioCueId.ExperiencePickup, "Audio/CC0/Pickups/experience");
            catalog.Add(GameAudioCueId.YeopjeonPickup,
                "Audio/CC0/Pickups/yeopjeon_1",
                "Audio/CC0/Pickups/yeopjeon_2");
            catalog.Add(GameAudioCueId.MagnetPickup, "Audio/CC0/Pickups/magnet");
            catalog.Add(GameAudioCueId.LevelUp, "Audio/CC0/Pickups/level_up");
            catalog.Add(GameAudioCueId.UpgradeSelected, "Audio/CC0/UI/ui_confirm");
            catalog.Add(GameAudioCueId.Gakgung, "Audio/CC0/Weapons/gakgung");
            catalog.Add(GameAudioCueId.Hwando, "Audio/CC0/Weapons/hwando");
            catalog.Add(GameAudioCueId.ThunderBomb, "Audio/CC0/Weapons/thunder_bomb");
            catalog.Add(GameAudioCueId.FrostFlask, "Audio/CC0/Weapons/frost_flask");
            catalog.Add(GameAudioCueId.WindThunderFan, "Audio/CC0/Weapons/wind_fan");
            catalog.Add(GameAudioCueId.Talisman, "Audio/CC0/Pickups/magnet");
            catalog.Add(GameAudioCueId.Jangseung, "Audio/CC0/Weapons/jangseung");
            catalog.Add(GameAudioCueId.Geumjul, "Audio/CC0/Weapons/geumjul");
            catalog.Add(GameAudioCueId.Singijeon, "Audio/CC0/Weapons/singijeon");
            catalog.Add(GameAudioCueId.NormalHit, "Audio/CC0/Combat/hit_soft_1");
            catalog.Add(GameAudioCueId.CriticalHit, "Audio/CC0/Combat/hit_critical");
            catalog.Add(GameAudioCueId.PlayerHurt,
                "Audio/CC0/Combat/player_hurt_1",
                "Audio/CC0/Combat/player_hurt_2");
            catalog.Add(GameAudioCueId.PlayerDefeat, "Audio/CC0/Combat/player_defeat");
            catalog.Add(GameAudioCueId.EliteDefeat, "Audio/CC0/Combat/elite_defeat");
            catalog.Add(GameAudioCueId.BossWarning, "Audio/CC0/Weapons/geumjul");
            catalog.Add(GameAudioCueId.BossAppear, "Audio/CC0/Weapons/geumjul");
            catalog.Add(GameAudioCueId.BossDefeat, "Audio/CC0/Combat/boss_defeat");
            catalog.Add(GameAudioCueId.BossSlam, "Audio/CC0/Combat/boss_slam");
            catalog.Add(GameAudioCueId.BossCharge, "Audio/CC0/Combat/boss_charge");
            catalog.Add(GameAudioCueId.BossVolley, "Audio/CC0/Combat/boss_volley");
            catalog.Add(GameAudioCueId.TreasureAppear, "Audio/CC0/Events/treasure_appear");
            catalog.Add(GameAudioCueId.TreasureOpen, "Audio/CC0/Events/treasure_open");
            catalog.Add(GameAudioCueId.WaveWarning, "Audio/CC0/Events/wave_warning");
            catalog.Add(GameAudioCueId.EliteAppear, "Audio/CC0/Events/elite_appear");
            catalog.Add(GameAudioCueId.PauseOpen, "Audio/CC0/UI/pause_open");
            catalog.Add(GameAudioCueId.AppraisalTick, "Audio/CC0/UI/appraisal_tick");
            catalog.Add(GameAudioCueId.AppraisalReveal, "Audio/CC0/UI/appraisal_reveal");
            catalog.Add(GameAudioCueId.Victory, "Audio/CC0/Pickups/level_up");
            catalog.Add(GameAudioCueId.Defeat, "Audio/CC0/Weapons/geumjul");
            return catalog;
        }

        public bool TryGet(GameAudioCueId cue, out AudioClip[] variants) => clips.TryGetValue(cue, out variants);

        private void Add(GameAudioCueId cue, params string[] resourcePaths)
        {
            var loaded = new List<AudioClip>(resourcePaths.Length);
            for (var index = 0; index < resourcePaths.Length; index++)
            {
                var clip = Resources.Load<AudioClip>(resourcePaths[index]);
                if (clip != null) loaded.Add(clip);
            }

            if (loaded.Count > 0) clips[cue] = loaded.ToArray();
        }
    }
}
