using System.Linq;
using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WaveSpawnDirectorTests
    {
        [TestCase(20f, "plague_rat")]
        [TestCase(46f, "vengeful_spirit")]
        [TestCase(94f, "dokkaebi")]
        public void IntroductionWindowsSelectOnlyTheNewNormalFamily(float elapsed, string expected)
        {
            var director = new WaveSpawnDirector(1701);

            Assert.That(Enumerable.Range(0, 32).Select(_ => director.SelectNormal(elapsed)),
                Is.All.EqualTo(expected));
        }

        [Test]
        public void LearnedNormalFamiliesMixOnlyAtTheApprovedWeights()
        {
            Assert.That(WaveSchedule.NormalEntriesAt(60f)
                    .Select(entry => (entry.ContentId, entry.Weight)),
                Is.EqualTo(new[] { ("plague_rat", 60), ("vengeful_spirit", 40) }));
            Assert.That(WaveSchedule.NormalEntriesAt(110f)
                    .Select(entry => (entry.ContentId, entry.Weight)),
                Is.EqualTo(new[] { ("plague_rat", 25), ("vengeful_spirit", 40), ("dokkaebi", 35) }));
            Assert.That(WaveSchedule.NormalEntriesAt(145f)
                    .Select(entry => (entry.ContentId, entry.Weight)),
                Is.EqualTo(new[] { ("plague_rat", 20), ("vengeful_spirit", 40), ("dokkaebi", 40) }));
        }

        [Test]
        public void SameSeedProducesSameNormalAndPackSequence()
        {
            var left = new WaveSpawnDirector(1701);
            var right = new WaveSpawnDirector(1701);

            var leftNormals = Enumerable.Range(0, 32)
                .Select(_ => left.SelectNormal(RunPhase.WaveTwo))
                .ToArray();
            var rightNormals = Enumerable.Range(0, 32)
                .Select(_ => right.SelectNormal(RunPhase.WaveTwo))
                .ToArray();

            Assert.That(leftNormals, Is.EqualTo(rightNormals));
            Assert.That(left.TryCreatePack(60f, 20, out var leftPack), Is.True);
            Assert.That(right.TryCreatePack(60f, 20, out var rightPack), Is.True);
            Assert.That(leftPack.ContentId, Is.EqualTo(rightPack.ContentId));
            Assert.That(leftPack.Count, Is.EqualTo(rightPack.Count));
            Assert.That(leftPack.Side, Is.EqualTo(rightPack.Side));
        }

        [Test]
        public void PackCountNeverExceedsDefinitionOrAvailableSlots()
        {
            var director = new WaveSpawnDirector(9);

            Assert.That(director.TryCreatePack(60f, 11, out var plan), Is.True);
            Assert.That(plan.Count, Is.InRange(10, 11));
        }

        [Test]
        public void BossWarningDoesNotCreateNewNormalPack()
        {
            var director = new WaveSpawnDirector(9);

            Assert.That(director.TryCreatePack(170f, 140, out _), Is.False);
        }

        [Test]
        public void ResetRestoresTheSeededSequence()
        {
            var director = new WaveSpawnDirector(1701);
            var first = Enumerable.Range(0, 16)
                .Select(_ => director.SelectNormal(RunPhase.WaveThree))
                .ToArray();

            director.Reset();

            Assert.That(Enumerable.Range(0, 16)
                .Select(_ => director.SelectNormal(RunPhase.WaveThree)), Is.EqualTo(first));
        }

        [Test]
        public void SpecialFamiliesAreIntroducedByPhaseAndNeverExceedTheQuarterCap()
        {
            var director = new WaveSpawnDirector(1701);
            Assert.That(director.TrySelectSpecial(RunPhase.WaveOne, 100, 0, out _), Is.False);
            Assert.That(director.TrySelectSpecial(RunPhase.WaveTwo, 20, 5, out _), Is.False);
            Assert.That(director.TrySelectSpecial(RunPhase.WaveTwo, 20, 4, out var waveTwo), Is.True);
            Assert.That(new[] { "shield_dokkaebi", "spirit_shaman" }, Does.Contain(waveTwo));

            director.Reset();
            var waveThree = Enumerable.Range(0, 12)
                .Select(_ => director.TrySelectSpecial(RunPhase.WaveThree, 100, 0, out var id) ? id : string.Empty)
                .Where(id => id.Length > 0).Distinct().ToArray();
            Assert.That(waveThree.Length, Is.EqualTo(1));
            Assert.That(new[] { "charging_horn_ghost", "splitting_rat" }, Does.Contain(waveThree[0]));

            director.Reset();
            var peak = Enumerable.Range(0, 24)
                .Select(_ => director.TrySelectSpecial(RunPhase.Peak, 100, 0, out var id) ? id : string.Empty)
                .Where(id => id.Length > 0).Distinct().ToArray();
            Assert.That(peak.Length, Is.InRange(1, 2));
        }
    }
}
