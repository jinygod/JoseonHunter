using System.Collections.Generic;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public interface IWeaponEvolutionProfile
    {
        bool IsEvolved { get; }
    }

    public sealed class WeaponEvolutionState
    {
        private readonly HashSet<string> evolvedWeaponIds = new HashSet<string>();

        public void SetEvolved(WeaponId weaponId) => evolvedWeaponIds.Add(weaponId.Value);
        public bool IsEvolved(WeaponId weaponId) => evolvedWeaponIds.Contains(weaponId.Value);
        public void Clear() => evolvedWeaponIds.Clear();
    }
}
