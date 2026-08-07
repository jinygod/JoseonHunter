using System.Linq;
using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StageCombatCatalogTests
    {
        [Test]
        public void EveryListedStageHasAStableCombatDefinition()
        {
            foreach (var stage in StageCatalog.All)
            {
                Assert.That(StageCombatCatalog.TryGet(stage.Id, out var definition), Is.True,
                    stage.DisplayName);
                Assert.That(definition.StageId, Is.EqualTo(stage.Id));
            }
        }

        [Test]
        public void GwigokDefinitionPreservesApprovedOpeningAndPeakCaps()
        {
            var combat = StageCombatCatalog.For(StageId.GwigokField);

            Assert.That(combat.Waves.WaveAt(0f).ActiveCap, Is.EqualTo(72));
            Assert.That(combat.Waves.WaveAt(610f).ActiveCap, Is.EqualTo(140));
            Assert.That(combat.Waves.NormalEntriesAt(0f).Single().ContentId,
                Is.EqualTo("plague_rat"));
        }

        [Test]
        public void StageProfilesDoNotShareTheSameOpeningRoster()
        {
            var first = StageCombatCatalog.For(StageId.GwigokField).Waves.NormalEntriesAt(0f);
            var second = StageCombatCatalog.For(StageId.DokkaebiPass).Waves.NormalEntriesAt(0f);
            var third = StageCombatCatalog.For(StageId.MoonlitTomb).Waves.NormalEntriesAt(0f);

            Assert.That(second.Select(entry => entry.ContentId), Is.Not.EqualTo(first.Select(entry => entry.ContentId)));
            Assert.That(third.Select(entry => entry.ContentId), Is.Not.EqualTo(first.Select(entry => entry.ContentId)));
        }

        [Test]
        public void UnknownStageCannotSilentlyUseFirstStageCombat()
        {
            Assert.That(StageCombatCatalog.TryGet(new StageId("unknown_stage"), out _), Is.False);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
                StageCombatCatalog.For(new StageId("unknown_stage")));
        }

        [TestCase("stage_01_gwigok_field", 1f, 1f, 1f, 1f)]
        [TestCase("stage_02_dokkaebi_pass", 1.35f, 1.12f, 1.15f, 1.25f)]
        [TestCase("stage_03_moonlit_tomb", 1.70f, 1.25f, 1.30f, 1.55f)]
        public void StageProfilesMatchApprovedRiskAndReward(
            string stageId,
            float health,
            float damage,
            float experience,
            float reward)
        {
            var combat = StageCombatCatalog.For(new StageId(stageId));

            Assert.That(combat.PresentationReady, Is.True);
            Assert.That(combat.Stats.EnemyHealthMultiplier, Is.EqualTo(health).Within(.001f));
            Assert.That(combat.Stats.EnemyDamageMultiplier, Is.EqualTo(damage).Within(.001f));
            Assert.That(combat.Stats.EnemyExperienceMultiplier, Is.EqualTo(experience).Within(.001f));
            Assert.That(combat.Rewards.CoinMultiplier, Is.EqualTo(reward).Within(.001f));
            Assert.That(combat.Rewards.AccountExperienceMultiplier, Is.EqualTo(reward).Within(.001f));
            Assert.That(combat.Rewards.MasteryMultiplier, Is.EqualTo(reward).Within(.001f));
        }
    }
}
