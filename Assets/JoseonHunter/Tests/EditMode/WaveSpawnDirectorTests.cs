using System.Linq;
using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WaveSpawnDirectorTests
    {
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
    }
}
