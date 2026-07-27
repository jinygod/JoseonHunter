using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class TargetSelectionTests
    {
        [Test]
        public void HighestThreatBreaksTiesByStableRuntimeId()
        {
            var targets = new[]
            {
                new CombatTargetSnapshot(9, 25f, 5f, false, false, new Float2(2f, 0f)),
                new CombatTargetSnapshot(4, 25f, 5f, false, false, new Float2(-2f, 0f))
            };

            Assert.That(SelectedId(WeaponTargeting.HighestThreat, targets), Is.EqualTo(4));
        }

        [Test]
        public void SelectReturnsNullForNoCandidates()
        {
            Assert.That(CombatTargetSelector.Select(WeaponTargeting.Nearest, new Float2(0f, 0f), new CombatTargetSnapshot[0]), Is.Null);
        }

        [Test]
        public void NearestAndNearestUnmarkedChooseClosestTarget()
        {
            var targets = new[]
            {
                Target(8, 1f, 0f),
                Target(3, 3f, 0f)
            };

            Assert.That(SelectedId(WeaponTargeting.Nearest, targets), Is.EqualTo(8));
            Assert.That(SelectedId(WeaponTargeting.NearestUnmarked, targets), Is.EqualTo(8));
        }

        [Test]
        public void HighestThreatPrioritizesBossThenEliteThenThreat()
        {
            var targets = new[]
            {
                new CombatTargetSnapshot(1, 100f, 99f, false, false, new Float2(1f, 0f)),
                new CombatTargetSnapshot(2, 100f, 1f, true, false, new Float2(1f, 0f)),
                new CombatTargetSnapshot(3, 1f, 0f, false, true, new Float2(1f, 0f))
            };

            Assert.That(SelectedId(WeaponTargeting.HighestThreat, targets), Is.EqualTo(3));
        }

        [Test]
        public void HighestThreatUsesRuntimeIdBeforeHealthWhenThreatIsTied()
        {
            var targets = new[]
            {
                new CombatTargetSnapshot(9, 100f, 5f, false, false, new Float2(1f, 0f)),
                new CombatTargetSnapshot(4, 1f, 5f, false, false, new Float2(1f, 0f))
            };

            Assert.That(SelectedId(WeaponTargeting.HighestThreat, targets), Is.EqualTo(4));
        }

        [Test]
        public void CrowdTargetingChoosesDensestCluster()
        {
            var targets = new[]
            {
                Target(1, -10f, 0f),
                Target(8, 4f, 0f),
                Target(2, 4.5f, 0f),
                Target(4, 5f, 0f)
            };

            Assert.That(SelectedId(WeaponTargeting.DensestCenter, targets), Is.EqualTo(2));
            Assert.That(SelectedId(WeaponTargeting.PredictedCrowd, targets), Is.EqualTo(2));
        }

        [Test]
        public void PlayerBoundaryChoosesClosestTargetToPlayer()
        {
            var targets = new[] { Target(8, 4f, 0f), Target(2, 2f, 0f) };

            Assert.That(SelectedId(WeaponTargeting.PlayerBoundary, targets), Is.EqualTo(2));
        }

        [Test]
        public void DensestDirectionChoosesMostPopulatedHalfPlane()
        {
            var targets = new[]
            {
                Target(5, 4f, 1f), Target(2, 4f, -1f), Target(9, -2f, 0f)
            };

            Assert.That(SelectedId(WeaponTargeting.DensestDirection, targets), Is.EqualTo(2));
        }

        [Test]
        public void DangerousSectorChoosesHighestThreatScore()
        {
            var targets = new[]
            {
                new CombatTargetSnapshot(8, 10f, 10f, false, false, new Float2(10f, 0f)),
                new CombatTargetSnapshot(3, 10f, 5f, true, false, new Float2(1f, 0f))
            };

            Assert.That(SelectedId(WeaponTargeting.DangerousSector, targets), Is.EqualTo(3));
        }

        [Test]
        public void UnknownTargetingModeIsRejected()
        {
            Assert.That(
                () => CombatTargetSelector.Select((WeaponTargeting)999, new Float2(0f, 0f), new[] { Target(1, 1f, 0f) }),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        private static int SelectedId(WeaponTargeting targeting, CombatTargetSnapshot[] targets)
        {
            var selected = CombatTargetSelector.Select(targeting, new Float2(0f, 0f), targets);
            Assert.That(selected.HasValue, Is.True);
            return selected.Value.RuntimeId;
        }

        private static CombatTargetSnapshot Target(int id, float x, float y) =>
            new CombatTargetSnapshot(id, 10f, 1f, false, false, new Float2(x, y));
    }
}
