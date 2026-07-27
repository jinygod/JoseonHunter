using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using UnityEngine;

namespace JoseonHunter.Content.Weapons
{
    [Serializable]
    public struct ActiveFrameWindow
    {
        public int firstFrame;
        public int lastFrame;
    }

    [CreateAssetMenu(menuName = "JoseonHunter/Weapons/Weapon Definition")]
    public sealed class WeaponDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private WeaponTargeting targeting;
        [SerializeField] private WeaponGeometry geometry;
        [SerializeField] private ContactPhase contactPhase;
        [SerializeField] private DamageElement element;
        [SerializeField] private RepeatHitPolicy repeatHitPolicy;
        [SerializeField] private WeaponLevelData[] levels;
        [SerializeField] private Sprite[] presentationSprites;
        [SerializeField] private Texture2D[] binaryMaskSourceReferences;
        [SerializeField] private ActiveFrameWindow[] activeFrameWindows;
        [SerializeField] private int poolCapacity = 1;

        public WeaponId Id => new(id);
        public WeaponTargeting Targeting => targeting;
        public WeaponGeometry Geometry => geometry;
        public ContactPhase ContactPhase => contactPhase;
        public DamageElement Element => element;
        public RepeatHitPolicy RepeatHitPolicy => repeatHitPolicy;
        public IReadOnlyList<WeaponLevelData> Levels => levels ?? Array.Empty<WeaponLevelData>();
        public IReadOnlyList<Sprite> PresentationSprites => presentationSprites ?? Array.Empty<Sprite>();
        public IReadOnlyList<Texture2D> BinaryMaskSourceReferences => binaryMaskSourceReferences ?? Array.Empty<Texture2D>();
        public IReadOnlyList<ActiveFrameWindow> ActiveFrameWindows => activeFrameWindows ?? Array.Empty<ActiveFrameWindow>();
        public int PoolCapacity => poolCapacity;

        public bool TryGetId(out WeaponId weaponId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                weaponId = default;
                return false;
            }

            weaponId = new WeaponId(id);
            return true;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("weapon definition ID is required");
                return errors;
            }

            if (levels == null || levels.Length != 5)
            {
                errors.Add($"weapon definition '{id}' must contain exactly five levels");
                return errors;
            }

            for (var index = 0; index < levels.Length; index++)
            {
                if (levels[index] == null)
                {
                    errors.Add($"weapon definition '{id}' has a missing level {index + 1}");
                    continue;
                }

                var error = levels[index].Validate(id, index + 1);
                if (error != null) errors.Add(error);
            }

            if (poolCapacity < 0) errors.Add($"weapon definition '{id}' pool capacity cannot be negative");
            return errors;
        }

        public void SetForTests(
            WeaponId weaponId,
            WeaponTargeting weaponTargeting,
            WeaponGeometry weaponGeometry,
            ContactPhase weaponContactPhase,
            DamageElement damageElement,
            RepeatHitPolicy hitPolicy,
            WeaponLevelData[] weaponLevels)
        {
            id = weaponId.Value;
            targeting = weaponTargeting;
            geometry = weaponGeometry;
            contactPhase = weaponContactPhase;
            element = damageElement;
            repeatHitPolicy = hitPolicy;
            levels = weaponLevels;
        }

        public void SetLevelsForTests(WeaponLevelData[] weaponLevels) => levels = weaponLevels;
    }
}
