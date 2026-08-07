using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class BossSafeLaneTests
    {
        [TestCase("royal_guard_wraith", 300f, 1.7f)]
        [TestCase("eclipse_priest", 600f, 1.9f)]
        [TestCase("eclipse_queen", 900f, 2.8f)]
        public void MoonlitTombBossesUseApprovedTimeAndScale(string id, float seconds, float scale)
        {
            var boss = StageBossCatalog.Get(id);

            Assert.That(boss.AtSeconds, Is.EqualTo(seconds));
            Assert.That(boss.VisualScale, Is.EqualTo(scale));
            Assert.That(StageBossCatalog.For(StageId.MoonlitTomb).Select(entry => entry.ContentId),
                Does.Contain(id));
        }

        [Test]
        public void GreatOmenQueenPatternAlwaysLeavesAReachableGap()
        {
            var pattern = StageBossCatalog.Get("eclipse_queen").PatternFor(.35f, 2);

            Assert.That(pattern.All(step => step.WarningSeconds >= .75f), Is.True);
            Assert.That(BossSafeLaneValidator.HasReachableGap(pattern, minimumDegrees: 28f), Is.True);
        }

        [Test]
        public void PatternWithoutRadialGapIsRejected()
        {
            var unsafePattern = new[]
            {
                new StageBossAttackStep(BossAttackKind.RadialVolley, .9f, safeGapDegrees: 0f)
            };

            Assert.That(BossSafeLaneValidator.HasReachableGap(unsafePattern, 28f), Is.False);
        }
    }
}
