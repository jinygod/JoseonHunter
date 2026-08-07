using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Runs
{
    public static class StageCombatCatalog
    {
        private static readonly IReadOnlyList<StageCombatDefinition> Definitions =
            Array.AsReadOnly(new[]
            {
                new StageCombatDefinition(StageId.GwigokField, WaveSchedule.Profile,
                    StageBattlefieldDefinition.Infinite("gwigok_field"), StageBossCatalog.For(StageId.GwigokField),
                    new StageStatProfile(1f, 1f, 1f),
                    new StageRewardProfile(1f, 1f, 1f), true),
                new StageCombatDefinition(StageId.DokkaebiPass, CreateDokkaebiPassWaves(),
                    StageBattlefieldDefinition.Bounded(72f, 112f, "dokkaebi_pass"), StageBossCatalog.For(StageId.DokkaebiPass),
                    new StageStatProfile(1.35f, 1.12f, 1.15f),
                    new StageRewardProfile(1.25f, 1.25f, 1.25f), true),
                new StageCombatDefinition(StageId.MoonlitTomb, CreateMoonlitTombWaves(),
                    StageBattlefieldDefinition.Bounded(84f, 84f, "moonlit_tomb"), StageBossCatalog.For(StageId.MoonlitTomb),
                    new StageStatProfile(1.70f, 1.25f, 1.30f),
                    new StageRewardProfile(1.55f, 1.55f, 1.55f), true)
            });

        public static StageCombatDefinition For(StageId stageId)
        {
            if (TryGet(stageId, out var definition)) return definition;
            throw new KeyNotFoundException($"No combat definition exists for stage '{stageId}'.");
        }

        public static bool TryGet(StageId stageId, out StageCombatDefinition definition)
        {
            for (var index = 0; index < Definitions.Count; index++)
            {
                if (!Definitions[index].StageId.Equals(stageId)) continue;
                definition = Definitions[index];
                return true;
            }

            definition = null;
            return false;
        }

        private static StageWaveProfile CreateDokkaebiPassWaves()
        {
            var club = StageWaveProfile.Entries(("club_dokkaebi", 100));
            var guarded = StageWaveProfile.Entries(("club_dokkaebi", 68), ("shield_guard_dokkaebi", 32));
            var charging = StageWaveProfile.Entries(("club_dokkaebi", 48), ("shield_guard_dokkaebi", 30),
                ("iron_horn_dokkaebi", 22));
            var ranged = StageWaveProfile.Entries(("club_dokkaebi", 36), ("shield_guard_dokkaebi", 26),
                ("iron_horn_dokkaebi", 20), ("stone_thrower_dokkaebi", 18));
            var elite = StageWaveProfile.Entries(("club_dokkaebi", 28), ("shield_guard_dokkaebi", 25),
                ("iron_horn_dokkaebi", 20), ("stone_thrower_dokkaebi", 19), ("red_horn_elite", 8));
            var peak = StageWaveProfile.Entries(("club_dokkaebi", 20), ("shield_guard_dokkaebi", 24),
                ("iron_horn_dokkaebi", 22), ("stone_thrower_dokkaebi", 22), ("red_horn_elite", 12));
            var boss = StageWaveProfile.Entries(("dokkaebi_king", 100));

            return new StageWaveProfile(
                new[]
                {
                    StageWaveProfile.Window(0f, 72, club, StageWaveProfile.Pack(new[] { "club_dokkaebi" }, 6, 9, 8f, 12f)),
                    StageWaveProfile.Window(120f, 92, guarded, StageWaveProfile.Pack(new[] { "shield_guard_dokkaebi" }, 6, 8, 11f, 15f)),
                    StageWaveProfile.Window(300f, 108, charging, StageWaveProfile.Pack(new[] { "iron_horn_dokkaebi" }, 4, 6, 12f, 16f)),
                    StageWaveProfile.Window(420f, 116, ranged, StageWaveProfile.Pack(new[] { "stone_thrower_dokkaebi" }, 4, 6, 12f, 16f)),
                    StageWaveProfile.Window(600f, 124, elite, StageWaveProfile.Pack(new[] { "red_horn_elite", "shield_guard_dokkaebi" }, 5, 8, 11f, 15f)),
                    StageWaveProfile.Window(720f, 130, peak, StageWaveProfile.Pack(new[] { "iron_horn_dokkaebi", "stone_thrower_dokkaebi" }, 7, 10, 9f, 13f)),
                    StageWaveProfile.Window(840f, 36, boss),
                    StageWaveProfile.Window(900f, 36, boss),
                    StageWaveProfile.Window(960f, 0, StageWaveProfile.Entries())
                },
                new[]
                {
                    new EnemyIntroductionDefinition(120f, "shield_guard_dokkaebi", 6),
                    new EnemyIntroductionDefinition(300f, "iron_horn_dokkaebi", 3),
                    new EnemyIntroductionDefinition(420f, "stone_thrower_dokkaebi", 3),
                    new EnemyIntroductionDefinition(600f, "red_horn_elite", 1)
                });
        }

        private static StageWaveProfile CreateMoonlitTombWaves()
        {
            var attendant = StageWaveProfile.Entries(("tomb_attendant", 100));
            var archer = StageWaveProfile.Entries(("tomb_attendant", 78), ("tomb_archer_ghost", 22));
            var lantern = StageWaveProfile.Entries(("tomb_attendant", 55), ("tomb_archer_ghost", 25),
                ("red_lantern_wraith", 20));
            var curse = StageWaveProfile.Entries(("tomb_attendant", 42), ("tomb_archer_ghost", 24),
                ("red_lantern_wraith", 18), ("curse_shaman", 16));
            var elite = StageWaveProfile.Entries(("tomb_attendant", 34), ("tomb_archer_ghost", 22),
                ("red_lantern_wraith", 18), ("curse_shaman", 18), ("grave_ambusher_elite", 8));
            var peak = StageWaveProfile.Entries(("tomb_attendant", 25), ("tomb_archer_ghost", 23),
                ("red_lantern_wraith", 20), ("curse_shaman", 20), ("grave_ambusher_elite", 12));
            var boss = StageWaveProfile.Entries(("eclipse_queen", 100));

            return new StageWaveProfile(
                new[]
                {
                    StageWaveProfile.Window(0f, 64, attendant, StageWaveProfile.Pack(new[] { "tomb_attendant" }, 6, 9, 9f, 13f)),
                    StageWaveProfile.Window(120f, 82, archer, StageWaveProfile.Pack(new[] { "tomb_archer_ghost" }, 3, 5, 13f, 17f)),
                    StageWaveProfile.Window(300f, 96, lantern, StageWaveProfile.Pack(new[] { "red_lantern_wraith" }, 3, 5, 13f, 17f)),
                    StageWaveProfile.Window(420f, 104, curse, StageWaveProfile.Pack(new[] { "tomb_archer_ghost", "curse_shaman" }, 4, 7, 12f, 16f)),
                    StageWaveProfile.Window(600f, 112, elite, StageWaveProfile.Pack(new[] { "grave_ambusher_elite", "red_lantern_wraith" }, 4, 6, 12f, 16f)),
                    StageWaveProfile.Window(720f, 118, peak, StageWaveProfile.Pack(new[] { "tomb_archer_ghost", "curse_shaman" }, 5, 8, 10f, 14f)),
                    StageWaveProfile.Window(840f, 32, boss),
                    StageWaveProfile.Window(900f, 32, boss),
                    StageWaveProfile.Window(960f, 0, StageWaveProfile.Entries())
                },
                new[]
                {
                    new EnemyIntroductionDefinition(120f, "tomb_archer_ghost", 3),
                    new EnemyIntroductionDefinition(300f, "red_lantern_wraith", 3),
                    new EnemyIntroductionDefinition(420f, "curse_shaman", 2),
                    new EnemyIntroductionDefinition(600f, "grave_ambusher_elite", 1)
                });
        }
    }
}
