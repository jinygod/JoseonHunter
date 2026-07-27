using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using UnityEngine;

namespace JoseonHunter.Content.Weapons
{
    [CreateAssetMenu(menuName = "JoseonHunter/Weapons/Weapon Catalog")]
    public sealed class WeaponCatalogAsset : ScriptableObject
    {
        [SerializeField] private WeaponDefinitionAsset[] definitions;

        public IReadOnlyList<WeaponDefinitionAsset> Definitions => definitions ?? Array.Empty<WeaponDefinitionAsset>();

        public bool TryGet(WeaponId id, out WeaponDefinitionAsset definition)
        {
            definition = Definitions.FirstOrDefault(candidate =>
                candidate != null && candidate.TryGetId(out var candidateId) && candidateId.Equals(id));
            return definition != null;
        }

        public IReadOnlyList<string> ValidateLaunchRoster()
        {
            var errors = new List<string>();
            if (Definitions.Count != WeaponRoster.All.Count)
            {
                errors.Add("launch catalog must contain exactly eight weapons");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var mechanics = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in Definitions)
            {
                if (definition == null)
                {
                    errors.Add("launch catalog contains a missing definition");
                    continue;
                }

                foreach (var error in definition.Validate()) errors.Add(error);
                if (!definition.TryGetId(out var weaponId)) continue;

                var id = weaponId.Value;
                if (!ids.Add(id)) errors.Add($"launch catalog contains duplicate weapon ID '{id}'");
                if (!WeaponRoster.All.Any(rosterId => rosterId.Equals(weaponId)))
                {
                    errors.Add($"launch catalog contains unknown weapon ID '{id}'");
                }

                var fingerprint = $"{definition.Targeting}|{definition.Geometry}|{definition.ContactPhase}|{definition.RepeatHitPolicy}";
                if (!mechanics.Add(fingerprint)) errors.Add("launch catalog contains mechanically identical definitions");
            }

            foreach (var rosterId in WeaponRoster.All)
            {
                if (!ids.Contains(rosterId.Value)) errors.Add($"launch catalog is missing weapon ID '{rosterId.Value}'");
            }

            return errors;
        }

        public void SetDefinitionsForTests(WeaponDefinitionAsset[] weaponDefinitions) => definitions = weaponDefinitions;
    }
}
