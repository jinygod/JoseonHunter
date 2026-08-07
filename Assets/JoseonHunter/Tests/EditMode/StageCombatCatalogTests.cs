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
    }
}
