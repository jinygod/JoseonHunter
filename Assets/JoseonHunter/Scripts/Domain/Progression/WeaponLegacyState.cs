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
            if (!selectedPaths.TryGetValue(weaponId, out var pathId))
                return default;

            if (equippedFromRunStart.Contains(weaponId))
            {
                if (weaponLevel < 4) return default;
                return new WeaponLegacySnapshot(pathId,
                    weaponLevel >= 5
                        ? WeaponLegacyStage.Completed
                        : WeaponLegacyStage.Reinforced);
            }

            if (weaponLevel < 3) return default;

            var stage = weaponLevel >= 5
                ? WeaponLegacyStage.Completed
                : weaponLevel >= 4
                    ? WeaponLegacyStage.Reinforced
                    : WeaponLegacyStage.Chosen;
            return new WeaponLegacySnapshot(pathId, stage);
        }

        public bool TryGetEquippedPath(WeaponId weaponId, out WeaponLegacyPathId pathId)
        {
            if (equippedFromRunStart.Contains(weaponId) &&
                selectedPaths.TryGetValue(weaponId, out pathId))
                return true;
            pathId = default;
            return false;
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
