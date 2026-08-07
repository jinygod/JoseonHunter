using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponLegacyStateTests
    {
        [TestCase(1, WeaponLegacyStage.None)]
        [TestCase(2, WeaponLegacyStage.None)]
        [TestCase(3, WeaponLegacyStage.None)]
        [TestCase(4, WeaponLegacyStage.Reinforced)]
        [TestCase(5, WeaponLegacyStage.Completed)]
        public void Equipped_path_activates_at_four_and_completes_at_five(
            int weaponLevel,
            WeaponLegacyStage expectedStage)
        {
            var state = new WeaponLegacyState();
            Assert.That(state.EquipForRun(
                WeaponId.HwandoFlyingBlade,
                WeaponLegacyPathId.HwandoVenom), Is.True);

            Assert.That(state.TryGetEquippedPath(
                WeaponId.HwandoFlyingBlade, out var path), Is.True);
            Assert.That(path, Is.EqualTo(WeaponLegacyPathId.HwandoVenom));
            Assert.That(state.SnapshotFor(
                WeaponId.HwandoFlyingBlade, weaponLevel).Stage, Is.EqualTo(expectedStage));
        }

        [Test]
        public void A_weapon_accepts_one_matching_path_and_rejects_the_opposite_path()
        {
            var state = new WeaponLegacyState();

            Assert.That(state.TryChoose(WeaponId.HwandoFlyingBlade, WeaponLegacyPathId.HwandoVenom), Is.True);
            Assert.That(state.TryChoose(WeaponId.HwandoFlyingBlade, WeaponLegacyPathId.HwandoMoonEclipse), Is.False);
            Assert.That(state.TryChoose(WeaponId.HwandoFlyingBlade, WeaponLegacyPathId.FrostMist), Is.False);
            Assert.That(state.SnapshotFor(WeaponId.HwandoFlyingBlade, 3).PathId,
                Is.EqualTo(WeaponLegacyPathId.HwandoVenom));
        }

        [TestCase(3, WeaponLegacyStage.Chosen)]
        [TestCase(4, WeaponLegacyStage.Reinforced)]
        [TestCase(5, WeaponLegacyStage.Completed)]
        [TestCase(8, WeaponLegacyStage.Completed)]
        public void Selected_path_stage_is_derived_from_current_weapon_level(
            int weaponLevel,
            WeaponLegacyStage expectedStage)
        {
            var state = new WeaponLegacyState();
            Assert.That(state.TryChoose(WeaponId.FrostFlask, WeaponLegacyPathId.FrostMist), Is.True);

            Assert.That(state.SnapshotFor(WeaponId.FrostFlask, weaponLevel).Stage, Is.EqualTo(expectedStage));
        }

        [Test]
        public void Removing_a_weapon_clears_its_selected_path()
        {
            var state = new WeaponLegacyState();
            state.TryChoose(WeaponId.GakgungShot, WeaponLegacyPathId.GakgungSunPiercer);

            Assert.That(state.Remove(WeaponId.GakgungShot), Is.True);
            Assert.That(state.SnapshotFor(WeaponId.GakgungShot, 5).HasPath, Is.False);
            Assert.That(state.Remove(WeaponId.GakgungShot), Is.False);
        }
    }
}
