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
    }
}
