using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Save
{
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
        public float AudioVolume = 1f;
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
                data.PatrolLoadouts.Add(loadout);
            }
            return data;
        }

        public SaveDataV1 Copy()
        {
            var copy = new SaveDataV1
            {
                SchemaVersion = SchemaVersion, AccountExperience = AccountExperience, Coins = Coins, OwnedHero = OwnedHero, EquippedHero = EquippedHero,
                TutorialCompleted = TutorialCompleted, AccessibilityEnabled = AccessibilityEnabled, AudioVolume = AudioVolume,
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
            EquipmentLevels = copy.EquipmentLevels; EquipmentQualities = copy.EquipmentQualities; EquipmentFragments = copy.EquipmentFragments;
            EvolutionNodeRanks = copy.EvolutionNodeRanks; EvolutionSpentCoins = copy.EvolutionSpentCoins; InvestigationClues = copy.InvestigationClues; ClaimedInvestigationMilestones = copy.ClaimedInvestigationMilestones;
            MonsterCompendiumEntries = copy.MonsterCompendiumEntries; UnlockedHeroes = copy.UnlockedHeroes; UnlockedDifficulties = copy.UnlockedDifficulties;
            UnlockedRecipes = copy.UnlockedRecipes; UnlockedAppearances = copy.UnlockedAppearances; BestPatrolResults = copy.BestPatrolResults;
            TutorialCompleted = copy.TutorialCompleted; AccessibilityEnabled = copy.AccessibilityEnabled; AudioVolume = copy.AudioVolume; FirstSolutionFlags = copy.FirstSolutionFlags; SelectableInvestigationPolicies = copy.SelectableInvestigationPolicies; SelectedInvestigationPolicy = copy.SelectedInvestigationPolicy;
            WeaponMasteryPoints = copy.WeaponMasteryPoints; UnlockedWeaponStyles = copy.UnlockedWeaponStyles;
            CommonTrainingRanks = copy.CommonTrainingRanks; CommonTrainingSpentCoins = copy.CommonTrainingSpentCoins;
            PatrolLoadouts = copy.PatrolLoadouts; ActivePatrolLoadoutIndex = copy.ActivePatrolLoadoutIndex;
        }
    }
}
