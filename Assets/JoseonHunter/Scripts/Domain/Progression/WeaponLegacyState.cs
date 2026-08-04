using System.Collections.Generic;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public sealed class WeaponLegacyState
    {
        private readonly Dictionary<WeaponId, WeaponLegacyPathId> selectedPaths = new();
        private readonly HashSet<WeaponId> equippedFromRunStart = new();

        public bool TryChoose(WeaponId weaponId, WeaponLegacyPathId pathId)
        {
            if (selectedPaths.ContainsKey(weaponId) ||
                !WeaponLegacyCatalog.TryGet(pathId, out var definition) ||
                !definition.WeaponId.Equals(weaponId))
            {
                return false;
            }

            selectedPaths.Add(weaponId, pathId);
            return true;
        }

        public WeaponLegacySnapshot SnapshotFor(WeaponId weaponId, int weaponLevel)
        {
            if (!selectedPaths.TryGetValue(weaponId, out var pathId) ||
                (weaponLevel < 3 && !equippedFromRunStart.Contains(weaponId)))
                return default;

            var stage = weaponLevel >= 5
                ? WeaponLegacyStage.Completed
                : weaponLevel >= 4
                    ? WeaponLegacyStage.Reinforced
                    : WeaponLegacyStage.Chosen;
            return new WeaponLegacySnapshot(pathId, stage);
        }

        public bool EquipForRun(WeaponId weaponId, WeaponLegacyPathId pathId)
        {
            if (!WeaponLegacyCatalog.TryGet(pathId, out var definition) ||
                !definition.WeaponId.Equals(weaponId)) return false;
            selectedPaths[weaponId] = pathId;
            equippedFromRunStart.Add(weaponId);
            return true;
        }

        public bool Remove(WeaponId weaponId)
        {
            equippedFromRunStart.Remove(weaponId);
            return selectedPaths.Remove(weaponId);
        }

        public void Clear()
        {
            selectedPaths.Clear();
            equippedFromRunStart.Clear();
        }
    }
}
