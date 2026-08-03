using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponLegacyRuntimeModifierTests
    {
        [Test]
        public void Legacy_snapshot_is_preserved_beside_general_affix_totals()
        {
            var profile = new WeaponRunAffixProfile(new[]
            {
                new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.Standard, 20d)
            });
            var legacy = new WeaponLegacySnapshot(WeaponLegacyPathId.FrostMist, WeaponLegacyStage.Reinforced);

            var modifiers = WeaponRuntimeModifiers.From(profile, legacy);

            Assert.That(modifiers.ScaleDamage(100f), Is.EqualTo(120f).Within(.001f));
            Assert.That(modifiers.Legacy, Is.EqualTo(legacy));
        }

        [Test]
        public void Missing_profile_and_legacy_remain_identity_safe()
        {
            var modifiers = WeaponRuntimeModifiers.From(null, default);

            Assert.That(modifiers.ScaleDamage(17f), Is.EqualTo(17f));
            Assert.That(modifiers.Legacy.Stage, Is.EqualTo(WeaponLegacyStage.None));
        }
    }
}
