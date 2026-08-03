using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponLegacyCatalogTests
    {
        [Test]
        public void Every_launch_weapon_has_two_distinct_readable_paths()
        {
            foreach (var weaponId in WeaponRoster.All)
            {
                var paths = WeaponLegacyCatalog.PathsFor(weaponId);

                Assert.That(paths, Has.Count.EqualTo(2), weaponId.Value);
                Assert.That(paths.Select(path => path.Id), Is.Unique, weaponId.Value);
                Assert.That(paths.All(path => path.WeaponId.Equals(weaponId)), Is.True, weaponId.Value);
                Assert.That(paths.All(path => !string.IsNullOrWhiteSpace(path.DisplayName)), Is.True, weaponId.Value);
                Assert.That(paths.All(path => !string.IsNullOrWhiteSpace(path.CombatStyle)), Is.True, weaponId.Value);
                Assert.That(paths.All(path => !string.IsNullOrWhiteSpace(path.Benefit)), Is.True, weaponId.Value);
                Assert.That(paths.All(path => !string.IsNullOrWhiteSpace(path.Cost)), Is.True, weaponId.Value);
                Assert.That(paths.All(path => !string.IsNullOrWhiteSpace(path.CompletionName)), Is.True, weaponId.Value);
            }
        }

        [Test]
        public void Approved_path_ids_keep_their_weapon_and_primary_multiplier()
        {
            AssertPath(WeaponLegacyPathId.HwandoVenom, WeaponId.HwandoFlyingBlade, .80f);
            AssertPath(WeaponLegacyPathId.GakgungSplitFletching, WeaponId.GakgungShot, .75f);
            AssertPath(WeaponLegacyPathId.TalismanHeavenSeal, WeaponId.TalismanThrow, .75f);
            AssertPath(WeaponLegacyPathId.ThunderEarthCurrent, WeaponId.ThunderCrashBomb, .70f);
            AssertPath(WeaponLegacyPathId.JangseungFourGuardians, WeaponId.JangseungWard, .70f);
            AssertPath(WeaponLegacyPathId.SingijeonFireNet, WeaponId.SingijeonVolley, .70f);
            AssertPath(WeaponLegacyPathId.FrostMist, WeaponId.FrostFlask, .65f);
            AssertPath(WeaponLegacyPathId.FanVacuum, WeaponId.WindThunderFan, .70f);
        }

        private static void AssertPath(WeaponLegacyPathId pathId, WeaponId weaponId, float multiplier)
        {
            Assert.That(WeaponLegacyCatalog.TryGet(pathId, out var definition), Is.True, pathId.Value);
            Assert.That(definition.WeaponId, Is.EqualTo(weaponId));
            Assert.That(definition.DirectDamageMultiplier, Is.EqualTo(multiplier).Within(.0001f));
        }
    }
}
