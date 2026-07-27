using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class DamageNumberAccumulatorTests
    {
        [Test]
        public void SameSourceTargetAndWeaponAggregateInsideQuarterSecondWindow()
        {
            var accumulator = new DamageNumberAccumulator(0.25f);
            accumulator.Add(Event(1, 2, 4, 1.00f), 1.00f);
            accumulator.Add(Event(1, 2, 6, 1.20f), 1.20f);

            Assert.That(accumulator.FlushReady(1.24f), Is.Empty);
            var display = accumulator.FlushReady(1.26f).Single();
            Assert.That(display.DisplayedDamage, Is.EqualTo(10));
            Assert.That(display.ContactPoint, Is.EqualTo(new Float2(6f, 2f)));
        }

        [Test]
        public void DifferentTargetsAndWeaponsRemainSeparateAndCriticalStateIsRetained()
        {
            var accumulator = new DamageNumberAccumulator(0.25f);
            accumulator.Add(Event(1, 2, 4, 0f, WeaponId.HwandoFlyingBlade), 0f);
            accumulator.Add(Event(1, 3, 6, 0f, WeaponId.HwandoFlyingBlade, true), 0f);
            accumulator.Add(Event(1, 2, 7, 0f, WeaponId.FrostFlask), 0f);

            var displays = accumulator.FlushReady(0.25f);
            Assert.That(displays, Has.Length.EqualTo(3));
            Assert.That(displays.Single(display => display.TargetRuntimeId == 3).IsCritical, Is.True);
        }

        private static ConfirmedDamageEvent Event(int source, int target, int damage, float time, WeaponId weapon = default, bool critical = false)
        {
            if (weapon.Equals(default(WeaponId))) weapon = WeaponId.HwandoFlyingBlade;
            return new ConfirmedDamageEvent(source, weapon, target, new DamageResult(damage, critical), new Float2(damage, target), ContactPhase.Tick, (int)(time * 100f));
        }
    }
}
