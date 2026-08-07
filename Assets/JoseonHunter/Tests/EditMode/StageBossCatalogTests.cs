using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StageBossCatalogTests
    {
        [TestCase("one_horn_captain", 300f, 1.7f)]
        [TestCase("iron_shield_general", 600f, 1.9f)]
        [TestCase("dokkaebi_king", 900f, 2.8f)]
        public void DokkaebiPassBossesUseApprovedTimeAndScale(string id, float seconds, float scale)
        {
            var boss = StageBossCatalog.Get(id);

            Assert.That(boss.AtSeconds, Is.EqualTo(seconds));
            Assert.That(boss.VisualScale, Is.EqualTo(scale));
            Assert.That(StageBossCatalog.For(StageId.DokkaebiPass).Select(entry => entry.ContentId),
                Does.Contain(id));
        }

        [Test]
        public void DokkaebiKingPhaseTwoStillTelegraphsEveryLinkedAttack()
        {
            var profile = StageBossCatalog.Get("dokkaebi_king");

            Assert.That(profile.PatternFor(.49f, 2).Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(profile.PatternFor(.49f, 2).All(step => step.WarningSeconds >= .75f), Is.True);
        }

        [Test]
        public void UnknownBossCannotFallBackToFallenGeneral()
        {
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
                StageBossCatalog.Get("missing_boss"));
        }
    }
}
