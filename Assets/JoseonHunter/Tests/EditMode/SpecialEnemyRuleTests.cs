using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class SpecialEnemyRuleTests
    {
        [Test]
        public void ShieldOnlyResistsFrontDirectHits()
        {
            var shield = EnemyArchetypeProfile.ForContentId("shield_dokkaebi");
            Assert.That(shield.IncomingDamageMultiplier(Vector2.right, Vector2.right, WeaponHitTrait.Slash), Is.EqualTo(.65f));
            Assert.That(shield.IncomingDamageMultiplier(Vector2.right, Vector2.left, WeaponHitTrait.Slash), Is.EqualTo(1f));
            Assert.That(shield.IncomingDamageMultiplier(Vector2.right, Vector2.right, WeaponHitTrait.Explosion), Is.EqualTo(1f));
            Assert.That(shield.IncomingDamageMultiplier(Vector2.right, Vector2.right, WeaponHitTrait.Pull), Is.EqualTo(1f));
            Assert.That(shield.IncomingDamageMultiplier(Vector2.right, Vector2.right, WeaponHitTrait.Reaction), Is.EqualTo(1f));
        }

        [Test]
        public void HornGhostTelegraphsForSixTenthsAndControlInterruptsTheBoundedDash()
        {
            var state = default(SpecialEnemyMotionState);
            var result = SpecialEnemyMotion.Tick(EnemyArchetype.ChargingHornGhost, ref state, .3f,
                Vector2.right, false, false, false, 0, 100);
            Assert.That(result.IsTelegraphing, Is.True);
            result = SpecialEnemyMotion.Tick(EnemyArchetype.ChargingHornGhost, ref state, .29f,
                Vector2.right, false, false, false, 0, 100);
            Assert.That(result.IsTelegraphing, Is.True);
            result = SpecialEnemyMotion.Tick(EnemyArchetype.ChargingHornGhost, ref state, .02f,
                Vector2.right, false, false, false, 0, 100);
            Assert.That(result.Velocity.magnitude, Is.GreaterThan(1f).And.LessThanOrEqualTo(6f));
            result = SpecialEnemyMotion.Tick(EnemyArchetype.ChargingHornGhost, ref state, .01f,
                Vector2.right, true, false, false, 0, 100);
            Assert.That(result.WasInterrupted, Is.True);
            Assert.That(result.Velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ShamanPulsesQuarterSecondAuraAndSplitRatHonorsActiveCap()
        {
            var shaman = default(SpecialEnemyMotionState);
            Assert.That(SpecialEnemyMotion.Tick(EnemyArchetype.SpiritShaman, ref shaman, .24f,
                Vector2.right, false, false, false, 0, 100).AuraPulse, Is.False);
            Assert.That(SpecialEnemyMotion.Tick(EnemyArchetype.SpiritShaman, ref shaman, .01f,
                Vector2.right, false, false, false, 0, 100).AuraPulse, Is.True);

            var rat = default(SpecialEnemyMotionState);
            var split = SpecialEnemyMotion.Tick(EnemyArchetype.SplittingRat, ref rat, 0f,
                Vector2.zero, false, false, true, 98, 100);
            Assert.That(split.SplitChildren, Is.EqualTo(2));
            var capped = default(SpecialEnemyMotionState);
            var fallback = SpecialEnemyMotion.Tick(EnemyArchetype.SplittingRat, ref capped, 0f,
                Vector2.zero, false, false, true, 99, 100);
            Assert.That(fallback.SplitChildren, Is.Zero);
            Assert.That(fallback.FallbackBlast, Is.True);
        }
    }
}
