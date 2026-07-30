using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public sealed class WeaponAppraisalViewModel
    {
        private WeaponAppraisalViewModel(
            string weaponId,
            string displayName,
            int level,
            string behavior,
            Sprite icon,
            WeaponAffixRollResult result,
            IReadOnlyList<WeaponPotentialId> currentPotentials,
            int existingPotentialCount)
        {
            WeaponId = weaponId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Level = Mathf.Max(1, level);
            Behavior = behavior ?? string.Empty;
            Icon = icon;
            Result = result;
            CurrentPotentials = currentPotentials ?? Array.Empty<WeaponPotentialId>();
            ExistingPotentialCount = Mathf.Clamp(existingPotentialCount, 0, CurrentPotentials.Count);
        }

        public string WeaponId { get; }
        public string DisplayName { get; }
        public int Level { get; }
        public string Behavior { get; }
        public Sprite Icon { get; }
        public WeaponAffixRollResult Result { get; }
        public IReadOnlyList<WeaponPotentialId> CurrentPotentials { get; }
        public int ExistingPotentialCount { get; }

        public static WeaponAppraisalViewModel From(ProgressionRewardEvent reward, WeaponSlotView slot)
        {
            var result = reward.AffixResult;
            var current = Array.AsReadOnly(slot.PotentialIds.ToArray());
            var awardedCount = result?.NewPotentials.Count ?? 0;
            return new WeaponAppraisalViewModel(
                reward.WeaponId,
                string.IsNullOrEmpty(slot.DisplayName) ? reward.DisplayName : slot.DisplayName,
                slot.Level > 0 ? slot.Level : reward.NewLevel,
                slot.Behavior,
                slot.Icon != null ? slot.Icon : reward.Icon,
                result,
                current,
                Mathf.Max(0, current.Count - awardedCount));
        }

        public static WeaponAppraisalViewModel ForResult(WeaponAffixRollResult result) =>
            new(string.Empty, "무기 운명 감정", 1, "추가옵션과 잠재 능력을 확인합니다",
                null, result, result?.NewPotentials ?? Array.Empty<WeaponPotentialId>(), 0);
    }
}
