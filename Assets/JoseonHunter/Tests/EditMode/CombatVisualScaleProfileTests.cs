using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CombatVisualScaleProfileTests
    {
        [Test]
        public void MobileLandscapeShowsAtLeastTwoAndHalfTimesTheBaselineArea()
        {
            var profile = CombatVisualScaleProfile.MobileLandscape;

            Assert.That(profile.CameraAreaRatio, Is.GreaterThanOrEqualTo(2.5f));
            Assert.That(profile.CameraOrthographicSize, Is.GreaterThan(profile.BaselineCameraOrthographicSize));
        }

        [Test]
        public void PlayerOccupiesRoughlyOneThirdOfFormerScreenHeight()
        {
            var profile = CombatVisualScaleProfile.MobileLandscape;

            Assert.That(profile.PlayerScreenHeightRatio, Is.InRange(0.30f, 0.42f));
        }

        [Test]
        public void CombatantRankSilhouettesRemainClearlyOrdered()
        {
            var profile = CombatVisualScaleProfile.MobileLandscape;

            Assert.That(profile.NormalEnemyScale / profile.PlayerScale, Is.InRange(0.95f, 1.05f));
            Assert.That(profile.EliteEnemyScale / profile.NormalEnemyScale, Is.InRange(1.20f, 1.30f));
            Assert.That(profile.BossEnemyScale / profile.NormalEnemyScale, Is.InRange(1.70f, 1.90f));
        }

        [Test]
        public void ContactRadiiFollowTheSmallerSilhouettes()
        {
            var profile = CombatVisualScaleProfile.MobileLandscape;

            Assert.That(profile.NormalContactRadius, Is.LessThan(0.40f));
            Assert.That(profile.EliteContactRadius, Is.GreaterThan(profile.NormalContactRadius));
            Assert.That(profile.BossContactRadius, Is.GreaterThan(profile.EliteContactRadius));
        }

        [Test]
        public void MobilePortraitUsesReviewedCombatScaleAndSpawnMargins()
        {
            var profile = CombatVisualScaleProfile.MobilePortrait;

            Assert.That(profile.CameraOrthographicSize, Is.EqualTo(18f));
            Assert.That(profile.SpawnOrthographicSize, Is.EqualTo(8.5f));
            Assert.That(7.25f / profile.CameraOrthographicSize, Is.InRange(.33f, .5f));
            Assert.That(profile.PlayerScale, Is.EqualTo(.82f));
            Assert.That(profile.NormalEnemyScale, Is.LessThan(profile.EliteEnemyScale));
            Assert.That(profile.EliteEnemyScale, Is.LessThan(profile.BossEnemyScale));
            Assert.That(profile.SpawnMarginMinimum, Is.EqualTo(.75f));
            Assert.That(profile.SpawnMarginMaximum, Is.EqualTo(1.5f));
        }

        [Test]
        public void Portrait_spawn_bounds_stay_independent_from_the_zoomed_out_camera()
        {
            var profile = CombatVisualScaleProfile.MobilePortrait;
            var bounds = profile.SpawnBounds(new UnityEngine.Vector2(3f, -2f), 9f / 16f);

            Assert.That(bounds.height, Is.EqualTo(17f));
            Assert.That(bounds.width, Is.EqualTo(17f * 9f / 16f).Within(.001f));
            Assert.That(bounds.center, Is.EqualTo(new UnityEngine.Vector2(3f, -2f)));
            Assert.That(bounds.height, Is.LessThan(profile.CameraOrthographicSize * 2f));
        }
    }
}
