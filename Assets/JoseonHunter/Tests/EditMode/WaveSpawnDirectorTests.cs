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
        public void AuthoredIntroductionsAreEmittedOnceInChronologicalOrder()
        {
            var director = new WaveSpawnDirector(1701);
            Assert.That(director.TryCreateIntroduction(102f, 10, out var shield), Is.True);
            Assert.That((shield.ContentId, shield.SpawnCount), Is.EqualTo(("shield_dokkaebi", 1)));
            Assert.That(director.TryCreateIntroduction(102f, 10, out _), Is.False);
            Assert.That(director.TryCreateIntroduction(120f, 10, out var horn), Is.True);
            Assert.That(horn.ContentId, Is.EqualTo("charging_horn_ghost"));
            Assert.That(director.TryCreateIntroduction(138f, 10, out var shaman), Is.True);
            Assert.That(shaman.ContentId, Is.EqualTo("spirit_shaman"));
            Assert.That(director.TryCreateIntroduction(150f, 10, out var rat), Is.True);
            Assert.That(rat.ContentId, Is.EqualTo("splitting_rat"));
        }

        [Test]
        public void IntroductionWaitsForASpawnSlotWithoutAdvancing()
        {
            var director = new WaveSpawnDirector(1701);
            Assert.That(director.TryCreateIntroduction(102f, 0, out _), Is.False);
            Assert.That(director.TryCreateIntroduction(103f, 1, out var delayed), Is.True);
            Assert.That(delayed.ContentId, Is.EqualTo("shield_dokkaebi"));
        }

        [Test]
        public void SpecialsRequireIntroductionOneEighthCapacityAndEightSecondCooldown()
        {
            var director = new WaveSpawnDirector(1701);
            Assert.That(director.TrySelectSpecial(101f, 80, 0, out _), Is.False);
            Assert.That(director.TryCreateIntroduction(102f, 10, out _), Is.True);
            Assert.That(director.TrySelectSpecial(109.9f, 80, 0, out _), Is.False);
            Assert.That(director.TrySelectSpecial(110f, 80, 0, out var id), Is.True);
            Assert.That(id, Is.EqualTo("shield_dokkaebi"));
            Assert.That(director.TrySelectSpecial(118f, 80, 10, out _), Is.False);
        }
    }
}
