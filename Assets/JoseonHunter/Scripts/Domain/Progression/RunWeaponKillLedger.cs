using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public enum EnemyMasteryClass
    {
        Normal,
        Special,
        Elite,
        MidBoss,
        FinalBoss
    }

    public sealed class RunWeaponKillLedger
    {
        private readonly Dictionary<int, WeaponId> lastHitByTarget = new Dictionary<int, WeaponId>();
        private readonly Dictionary<WeaponId, int> pointsByWeapon = new Dictionary<WeaponId, int>();
        private readonly Dictionary<WeaponId, long> damageByWeapon = new Dictionary<WeaponId, long>();
        private readonly Dictionary<WeaponId, int> killsByWeapon = new Dictionary<WeaponId, int>();

        public void RecordDamage(WeaponId weaponId, int damage)
        {
            if (string.IsNullOrEmpty(weaponId.Value) || damage <= 0) return;
            var current = damageByWeapon.TryGetValue(weaponId, out var total) ? total : 0L;
            damageByWeapon[weaponId] = current > long.MaxValue - damage ? long.MaxValue : current + damage;
        }

        public void RecordHit(int targetRuntimeId, WeaponId weaponId)
        {
            if (targetRuntimeId <= 0 || string.IsNullOrEmpty(weaponId.Value)) return;
            lastHitByTarget[targetRuntimeId] = weaponId;
        }

        public int ConfirmDeath(int targetRuntimeId, EnemyMasteryClass enemyClass)
        {
            if (!lastHitByTarget.TryGetValue(targetRuntimeId, out var weaponId)) return 0;
            lastHitByTarget.Remove(targetRuntimeId);
            var awarded = PointsFor(enemyClass);
            var current = pointsByWeapon.TryGetValue(weaponId, out var points) ? points : 0;
            pointsByWeapon[weaponId] = current > int.MaxValue - awarded ? int.MaxValue : current + awarded;
            var currentKills = killsByWeapon.TryGetValue(weaponId, out var kills) ? kills : 0;
            killsByWeapon[weaponId] = currentKills == int.MaxValue ? int.MaxValue : currentKills + 1;
            return awarded;
        }

        public void ForgetTarget(int targetRuntimeId) => lastHitByTarget.Remove(targetRuntimeId);

        public int PointsFor(WeaponId weaponId) =>
            pointsByWeapon.TryGetValue(weaponId, out var points) ? points : 0;

        public long DamageFor(WeaponId weaponId) =>
            damageByWeapon.TryGetValue(weaponId, out var damage) ? damage : 0L;

        public int KillsFor(WeaponId weaponId) =>
            killsByWeapon.TryGetValue(weaponId, out var kills) ? kills : 0;

        public IReadOnlyDictionary<WeaponId, int> Snapshot() =>
            new ReadOnlyDictionary<WeaponId, int>(new Dictionary<WeaponId, int>(pointsByWeapon));

        public void Reset()
        {
            lastHitByTarget.Clear();
            pointsByWeapon.Clear();
            damageByWeapon.Clear();
            killsByWeapon.Clear();
        }

        private static int PointsFor(EnemyMasteryClass enemyClass) => enemyClass switch
        {
            EnemyMasteryClass.Normal => 1,
            EnemyMasteryClass.Special => 3,
            EnemyMasteryClass.Elite => 10,
            EnemyMasteryClass.MidBoss => 30,
            EnemyMasteryClass.FinalBoss => 100,
            _ => throw new ArgumentOutOfRangeException(nameof(enemyClass), enemyClass, null)
        };
    }
}
