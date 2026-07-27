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
            var copy = data.Copy();
            if (copy.InvestigationClues.Count < 9)
            {
                var clue = "clue_" + (copy.InvestigationClues.Count + 1).ToString("00");
                copy.InvestigationClues.Add(clue);
            }
            Claim(copy, 3); Claim(copy, 6); Claim(copy, 9); data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None);
        }
        private static void Claim(SaveDataV1 data, int milestone)
        {
            if (data.InvestigationClues.Count < milestone || data.ClaimedInvestigationMilestones.Contains(milestone)) return;
            data.ClaimedInvestigationMilestones.Add(milestone);
            if (milestone == 6) { Add(data.UnlockedRecipes, "hwando_evolution"); Add(data.FirstSolutionFlags, "investigation_policy"); }
            if (milestone == 9) { Add(data.UnlockedHeroes, "shaman"); Add(data.UnlockedDifficulties, "hard"); }
        }
        private static void Add(System.Collections.Generic.List<string> values, string value) { if (!values.Contains(value)) values.Add(value); }
    }
}
