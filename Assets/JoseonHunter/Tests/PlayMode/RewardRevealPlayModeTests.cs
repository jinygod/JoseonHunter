using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class RewardRevealPlayModeTests
    {
        [TestCase(ProgressionRewardKind.Support, 70)]
        [TestCase(ProgressionRewardKind.WeaponLevel, 80)]
        [TestCase(ProgressionRewardKind.NewWeapon, 90)]
        [TestCase(ProgressionRewardKind.Evolution, 100)]
        public void Reward_kind_maps_to_expected_intensity(ProgressionRewardKind kind, int expected)
        {
            Assert.That(RewardRevealPresenter.IntensityFor(kind), Is.EqualTo(expected));
        }
    }
}
