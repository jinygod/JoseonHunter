using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Runs;

namespace JoseonHunter.Domain.Save
{
    [Serializable]
    public sealed class StageClearRecordData
    {
        public string StageId;
        public string Difficulty;
        public bool Victory;
        public float BestElapsed;
        public int BestKills;
        public int BestLevel;

        public StageClearRecordData Copy() => new StageClearRecordData
        {
            StageId = StageId,
            Difficulty = Difficulty,
            Victory = Victory,
            BestElapsed = BestElapsed,
            BestKills = BestKills,
            BestLevel = BestLevel
        };

        public bool TryToDomain(out StageClearRecord record)
        {
            record = default;
            if (string.IsNullOrWhiteSpace(StageId) ||
                !StageDifficultyNames.TryParse(Difficulty, out var difficulty)) return false;
            var candidate = new Runs.StageId(StageId);
            if (!StageCatalog.TryGet(candidate, out var definition)) return false;
            record = new StageClearRecord(
                new StageSelection(definition.Id, difficulty),
                Victory,
                Math.Max(0f, BestElapsed),
                Math.Max(0, BestKills),
                Math.Max(0, BestLevel));
            return true;
        }

        public static StageClearRecordData From(StageClearRecord record) => new StageClearRecordData
        {
            StageId = record.Selection.StageId.Value,
            Difficulty = StageDifficultyNames.StorageId(record.Selection.Difficulty),
            Victory = record.VictoryAchieved,
            BestElapsed = record.BestElapsed,
            BestKills = record.BestKills,
            BestLevel = record.BestLevel
        };

        public static List<StageClearRecord> DomainRecords(IEnumerable<StageClearRecordData> source)
        {
            var result = new List<StageClearRecord>();
            if (source == null) return result;
            foreach (var data in source)
            {
                if (data == null || !data.TryToDomain(out var record)) continue;
                var merged = false;
                for (var index = 0; index < result.Count; index++)
                {
                    if (!result[index].Selection.Equals(record.Selection)) continue;
                    result[index] = result[index].Merge(record);
                    merged = true;
                    break;
                }
                if (!merged) result.Add(record);
            }
            return result;
        }
    }

    [Serializable]
    public sealed class PatrolLoadoutData
    {
        public string Name;
        public string StartingWeaponId;
        public Dictionary<string, string> WeaponStyleIds = new Dictionary<string, string>();
        public string DifficultyId = "normal";

        public PatrolLoadoutData Copy() => new PatrolLoadoutData
        {
            Name = Name,
            StartingWeaponId = StartingWeaponId,
            WeaponStyleIds = new Dictionary<string, string>(WeaponStyleIds ?? new Dictionary<string, string>()),
            DifficultyId = DifficultyId
        };
    }

    [Serializable]
    public sealed class SaveDataV1
    {
        public int SchemaVersion = ProjectIdentity.SaveSchemaVersion;
        public int AccountExperience;
        public int Coins;
        public string SelectedStageId = StageId.GwigokField.Value;
        public string SelectedStageDifficulty = "normal";
        public List<StageClearRecordData> StageClearRecords = new List<StageClearRecordData>();
        public string OwnedHero = "hunter";
        public string EquippedHero = "hunter";
        public Dictionary<string, int> EquipmentLevels = new Dictionary<string, int>();
        public Dictionary<string, int> EquipmentQualities = new Dictionary<string, int>();
        public Dictionary<string, int> EquipmentFragments = new Dictionary<string, int>();
        public Dictionary<string, int> EvolutionNodeRanks = new Dictionary<string, int>();
        public Dictionary<string, int> EvolutionSpentCoins = new Dictionary<string, int>();
        public List<string> InvestigationClues = new List<string>();
        public List<int> ClaimedInvestigationMilestones = new List<int>();
        public List<string> MonsterCompendiumEntries = new List<string>();
        public List<string> UnlockedHeroes = new List<string>();
        public List<string> UnlockedDifficulties = new List<string>();
        public List<string> UnlockedRecipes = new List<string>();
        public List<string> UnlockedAppearances = new List<string>();
        public Dictionary<string, int> BestPatrolResults = new Dictionary<string, int>();
        public bool TutorialCompleted;
        public bool AccessibilityEnabled;
        // Retained as the migration source for saves written before split audio controls.
        public float AudioVolume = 1f;
        public float MusicVolume = 1f;
        public float SoundEffectVolume = 1f;
        public bool HasSplitAudioSettings = true;
        public List<string> FirstSolutionFlags = new List<string>();
        public List<string> SelectableInvestigationPolicies = new List<string>();
        public string SelectedInvestigationPolicy;
        public Dictionary<string, int> WeaponMasteryPoints = new Dictionary<string, int>();
        public List<string> UnlockedWeaponStyles = new List<string>();
        public Dictionary<string, int> CommonTrainingRanks = new Dictionary<string, int>();
        public Dictionary<string, int> CommonTrainingSpentCoins = new Dictionary<string, int>();
        public List<PatrolLoadoutData> PatrolLoadouts = new List<PatrolLoadoutData>();
        public int ActivePatrolLoadoutIndex;

        public static SaveDataV1 CreateDefaults()
        {
            var data = new SaveDataV1();
            for (var index = 1; index <= 12; index++)
            {
                var id = "weapon_" + index.ToString("00");
                data.EquipmentLevels[id] = 0;
                data.EquipmentQualities[id] = 0;
                data.EquipmentFragments[id] = 0;
                data.EvolutionNodeRanks["node_" + index.ToString("00")] = 0;
                data.EvolutionSpentCoins["node_" + index.ToString("00")] = 0;
            }

            data.UnlockedHeroes.Add("hunter");
            data.UnlockedDifficulties.Add("normal");
            foreach (var weaponId in Combat.WeaponRoster.All)
                data.WeaponMasteryPoints[weaponId.Value] = 0;
            data.UnlockedWeaponStyles.Add(Progression.WeaponLegacyPathId.HwandoVenom.Value);
            data.UnlockedWeaponStyles.Add(Progression.WeaponLegacyPathId.HwandoMoonEclipse.Value);
            foreach (Progression.CommonTrainingId id in Enum.GetValues(typeof(Progression.CommonTrainingId)))
            {
                data.CommonTrainingRanks[id.ToString()] = 0;
                data.CommonTrainingSpentCoins[id.ToString()] = 0;
            }
            for (var index = 0; index < 3; index++)
            {
                var loadout = new PatrolLoadoutData
                {
                    Name = "순찰대 " + (index + 1),
                    StartingWeaponId = Combat.WeaponId.HwandoFlyingBlade.Value,
                    DifficultyId = "normal"
                };
                foreach (var weaponId in Combat.WeaponRoster.All)
                    loadout.WeaponStyleIds[weaponId.Value] = string.Empty;
                loadout.WeaponStyleIds[Combat.WeaponId.HwandoFlyingBlade.Value] =
                    Progression.WeaponLegacyPathId.HwandoVenom.Value;
                data.PatrolLoadouts.Add(loadout);
            }
            return data;
        }

        public SaveDataV1 Copy()
        {
            var copy = new SaveDataV1
            {
                SchemaVersion = SchemaVersion, AccountExperience = AccountExperience, Coins = Coins, OwnedHero = OwnedHero, EquippedHero = EquippedHero,
                SelectedStageId = SelectedStageId, SelectedStageDifficulty = SelectedStageDifficulty,
                StageClearRecords = StageClearRecords.ConvertAll(record => record.Copy()),
                TutorialCompleted = TutorialCompleted, AccessibilityEnabled = AccessibilityEnabled, AudioVolume = AudioVolume,
                MusicVolume = MusicVolume, SoundEffectVolume = SoundEffectVolume,
                HasSplitAudioSettings = HasSplitAudioSettings,
                EquipmentLevels = new Dictionary<string, int>(EquipmentLevels), EquipmentQualities = new Dictionary<string, int>(EquipmentQualities),
                EquipmentFragments = new Dictionary<string, int>(EquipmentFragments), EvolutionNodeRanks = new Dictionary<string, int>(EvolutionNodeRanks), EvolutionSpentCoins = new Dictionary<string, int>(EvolutionSpentCoins),
                BestPatrolResults = new Dictionary<string, int>(BestPatrolResults), InvestigationClues = new List<string>(InvestigationClues),
                ClaimedInvestigationMilestones = new List<int>(ClaimedInvestigationMilestones), MonsterCompendiumEntries = new List<string>(MonsterCompendiumEntries),
                UnlockedHeroes = new List<string>(UnlockedHeroes), UnlockedDifficulties = new List<string>(UnlockedDifficulties),
                UnlockedRecipes = new List<string>(UnlockedRecipes), UnlockedAppearances = new List<string>(UnlockedAppearances),
                FirstSolutionFlags = new List<string>(FirstSolutionFlags), SelectableInvestigationPolicies = new List<string>(SelectableInvestigationPolicies), SelectedInvestigationPolicy = SelectedInvestigationPolicy,
                WeaponMasteryPoints = new Dictionary<string, int>(WeaponMasteryPoints), UnlockedWeaponStyles = new List<string>(UnlockedWeaponStyles),
                CommonTrainingRanks = new Dictionary<string, int>(CommonTrainingRanks), CommonTrainingSpentCoins = new Dictionary<string, int>(CommonTrainingSpentCoins),
                PatrolLoadouts = PatrolLoadouts.ConvertAll(loadout => loadout.Copy()), ActivePatrolLoadoutIndex = ActivePatrolLoadoutIndex
            };
            return copy;
        }

        public void CopyFrom(SaveDataV1 source)
        {
            var copy = source.Copy();
            SchemaVersion = copy.SchemaVersion; AccountExperience = copy.AccountExperience; Coins = copy.Coins; OwnedHero = copy.OwnedHero; EquippedHero = copy.EquippedHero;
            SelectedStageId = copy.SelectedStageId; SelectedStageDifficulty = copy.SelectedStageDifficulty; StageClearRecords = copy.StageClearRecords;
            EquipmentLevels = copy.EquipmentLevels; EquipmentQualities = copy.EquipmentQualities; EquipmentFragments = copy.EquipmentFragments;
            EvolutionNodeRanks = copy.EvolutionNodeRanks; EvolutionSpentCoins = copy.EvolutionSpentCoins; InvestigationClues = copy.InvestigationClues; ClaimedInvestigationMilestones = copy.ClaimedInvestigationMilestones;
            MonsterCompendiumEntries = copy.MonsterCompendiumEntries; UnlockedHeroes = copy.UnlockedHeroes; UnlockedDifficulties = copy.UnlockedDifficulties;
            UnlockedRecipes = copy.UnlockedRecipes; UnlockedAppearances = copy.UnlockedAppearances; BestPatrolResults = copy.BestPatrolResults;
            TutorialCompleted = copy.TutorialCompleted; AccessibilityEnabled = copy.AccessibilityEnabled; AudioVolume = copy.AudioVolume;
            MusicVolume = copy.MusicVolume; SoundEffectVolume = copy.SoundEffectVolume; HasSplitAudioSettings = copy.HasSplitAudioSettings;
            FirstSolutionFlags = copy.FirstSolutionFlags; SelectableInvestigationPolicies = copy.SelectableInvestigationPolicies; SelectedInvestigationPolicy = copy.SelectedInvestigationPolicy;
            WeaponMasteryPoints = copy.WeaponMasteryPoints; UnlockedWeaponStyles = copy.UnlockedWeaponStyles;
            CommonTrainingRanks = copy.CommonTrainingRanks; CommonTrainingSpentCoins = copy.CommonTrainingSpentCoins;
            PatrolLoadouts = copy.PatrolLoadouts; ActivePatrolLoadoutIndex = copy.ActivePatrolLoadoutIndex;
        }
    }
}
