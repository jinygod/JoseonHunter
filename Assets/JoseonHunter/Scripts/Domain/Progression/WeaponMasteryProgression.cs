using System;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Save;

namespace JoseonHunter.Domain.Progression
{
    public sealed class WeaponMasteryProgression
    {
        private readonly SaveDataV1 data;

        public WeaponMasteryProgression(SaveDataV1 data) =>
            this.data = data ?? throw new ArgumentNullException(nameof(data));

        public ProgressionResult CanPurchase(WeaponId weaponId, WeaponLegacyPathId pathId)
        {
            if (!WeaponMasteryCatalog.TryGet(weaponId, pathId, out var style))
                return new ProgressionResult(false, ProgressionError.InvalidSelection);
            if (data.UnlockedWeaponStyles.Contains(pathId.Value))
                return new ProgressionResult(false, ProgressionError.AlreadyUnlocked);

            var mastery = data.WeaponMasteryPoints.TryGetValue(weaponId.Value, out var points) ? points : 0;
            if (mastery < style.RequiredMastery)
                return new ProgressionResult(false, ProgressionError.InsufficientMastery);
            if (data.Coins < style.CoinCost)
                return new ProgressionResult(false, ProgressionError.InsufficientCoins);
            return new ProgressionResult(true, ProgressionError.None);
        }

        public ProgressionResult Purchase(WeaponId weaponId, WeaponLegacyPathId pathId)
        {
            var validation = CanPurchase(weaponId, pathId);
            if (!validation.Success) return validation;

            WeaponMasteryCatalog.TryGet(weaponId, pathId, out var style);
            var copy = data.Copy();
            copy.Coins -= style.CoinCost;
            copy.UnlockedWeaponStyles.Add(pathId.Value);
            data.CopyFrom(copy);
            return new ProgressionResult(true, ProgressionError.None);
        }
    }
}
