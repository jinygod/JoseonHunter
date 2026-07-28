using System;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;

namespace JoseonHunter.Tests.PlayMode
{
    internal sealed class EvolvedWeaponTestRig : IDisposable
    {
        private EvolvedWeaponTestRig(WeaponId weaponId)
        {
            Registry = new CombatTargetRegistry();
            Damage = new CombatDamageService(Registry);
            Runtime = new WeaponRuntimeController(Registry, Damage, PixelHitMask.FromRows("1"));
            Executor = EvolvedExecutorFactory.CreateForTests(weaponId, Runtime);
            Runtime.Register(weaponId, Executor);
        }

        public CombatTargetRegistry Registry { get; }
        public CombatDamageService Damage { get; }
        public WeaponRuntimeController Runtime { get; }
        public IWeaponExecutor Executor { get; }

        public static EvolvedWeaponTestRig For(WeaponId weaponId) => new EvolvedWeaponTestRig(weaponId);

        public void Dispose() => Runtime.Dispose();
    }
}
