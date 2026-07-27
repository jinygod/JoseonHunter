using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Save
{
    [Serializable]
    public sealed class SaveDataV1
    {
        public int SchemaVersion = 1;
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
            return data;
        }

        public SaveDataV1 Copy()
        {
            var copy = new SaveDataV1
            {
                SchemaVersion = SchemaVersion, Coins = Coins, OwnedHero = OwnedHero, EquippedHero = EquippedHero,
                TutorialCompleted = TutorialCompleted, AccessibilityEnabled = AccessibilityEnabled, AudioVolume = AudioVolume,
                EquipmentLevels = new Dictionary<string, int>(EquipmentLevels), EquipmentQualities = new Dictionary<string, int>(EquipmentQualities),
                EquipmentFragments = new Dictionary<string, int>(EquipmentFragments), EvolutionNodeRanks = new Dictionary<string, int>(EvolutionNodeRanks), EvolutionSpentCoins = new Dictionary<string, int>(EvolutionSpentCoins),
                BestPatrolResults = new Dictionary<string, int>(BestPatrolResults), InvestigationClues = new List<string>(InvestigationClues),
                ClaimedInvestigationMilestones = new List<int>(ClaimedInvestigationMilestones), MonsterCompendiumEntries = new List<string>(MonsterCompendiumEntries),
                UnlockedHeroes = new List<string>(UnlockedHeroes), UnlockedDifficulties = new List<string>(UnlockedDifficulties),
                UnlockedRecipes = new List<string>(UnlockedRecipes), UnlockedAppearances = new List<string>(UnlockedAppearances),
                FirstSolutionFlags = new List<string>(FirstSolutionFlags)
            };
            return copy;
        }

        public void CopyFrom(SaveDataV1 source)
        {
            var copy = source.Copy();
            SchemaVersion = copy.SchemaVersion; Coins = copy.Coins; OwnedHero = copy.OwnedHero; EquippedHero = copy.EquippedHero;
            EquipmentLevels = copy.EquipmentLevels; EquipmentQualities = copy.EquipmentQualities; EquipmentFragments = copy.EquipmentFragments;
            EvolutionNodeRanks = copy.EvolutionNodeRanks; EvolutionSpentCoins = copy.EvolutionSpentCoins; InvestigationClues = copy.InvestigationClues; ClaimedInvestigationMilestones = copy.ClaimedInvestigationMilestones;
            MonsterCompendiumEntries = copy.MonsterCompendiumEntries; UnlockedHeroes = copy.UnlockedHeroes; UnlockedDifficulties = copy.UnlockedDifficulties;
            UnlockedRecipes = copy.UnlockedRecipes; UnlockedAppearances = copy.UnlockedAppearances; BestPatrolResults = copy.BestPatrolResults;
            TutorialCompleted = copy.TutorialCompleted; AccessibilityEnabled = copy.AccessibilityEnabled; AudioVolume = copy.AudioVolume; FirstSolutionFlags = copy.FirstSolutionFlags;
        }
    }
}
