using System;
using JoseonHunter.Domain.Save;

namespace JoseonHunter.Domain.Progression
{
    public sealed class InvestigationCase
    {
        private readonly SaveDataV1 data;
        public InvestigationCase(SaveDataV1 data) { this.data = data ?? throw new ArgumentNullException(nameof(data)); }
        public ProgressionResult CompletePatrol(int selection)
        {
            var copy = data.Copy(); var available = new System.Collections.Generic.List<string>();
            for (var index = 1; index <= 9; index++) { var clue = "clue_" + index.ToString("00"); if (!copy.InvestigationClues.Contains(clue)) available.Add(clue); }
            if (available.Count > 0) { if (selection < 0 || selection >= available.Count) return new ProgressionResult(false, ProgressionError.InvalidSelection); copy.InvestigationClues.Add(available[selection]); }
            Claim(copy, 3); Claim(copy, 6); Claim(copy, 9); data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None);
        }
        private static void Claim(SaveDataV1 data, int milestone)
        {
            if (data.InvestigationClues.Count < milestone || data.ClaimedInvestigationMilestones.Contains(milestone)) return;
            data.ClaimedInvestigationMilestones.Add(milestone);
            if (milestone == 3) { Add(data.FirstSolutionFlags, "fallen_general_first_weakness"); Add(data.MonsterCompendiumEntries, "fallen_general_expanded"); }
            if (milestone == 6) { Add(data.UnlockedRecipes, "hwando_evolution"); Add(data.SelectableInvestigationPolicies, "next_patrol_focus"); }
            if (milestone == 9) { Add(data.UnlockedHeroes, "shaman"); Add(data.UnlockedDifficulties, "hard"); }
        }
        private static void Add(System.Collections.Generic.List<string> values, string value) { if (!values.Contains(value)) values.Add(value); }
        public ProgressionResult SelectPolicy(string policy) { if (string.IsNullOrEmpty(policy) || !data.SelectableInvestigationPolicies.Contains(policy)) return new ProgressionResult(false, ProgressionError.InvalidSelection); var copy = data.Copy(); copy.SelectedInvestigationPolicy = policy; data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None); }
    }
}
