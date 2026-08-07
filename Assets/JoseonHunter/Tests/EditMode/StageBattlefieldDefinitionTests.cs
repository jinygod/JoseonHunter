using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StageBattlefieldDefinitionTests
    {
        [TestCase(36f, 56f, 15f, 35f)]
        [TestCase(-36f, -56f, -15f, -35f)]
        public void DokkaebiPassClampsCameraSafePlayerPosition(
            float x, float y, float expectedX, float expectedY)
        {
            var field = StageBattlefieldDefinition.Bounded(72f, 112f, "dokkaebi_pass");

            var clamped = field.ClampPlayer(new Float2(x, y), new Float2(21f, 21f));

            Assert.That(clamped, Is.EqualTo(new Float2(expectedX, expectedY)));
        }

        [Test]
        public void InfiniteFieldNeverChangesPlayerPosition()
        {
            var field = StageBattlefieldDefinition.Infinite("gwigok_field");
            var outside = new Float2(900f, -1200f);

            Assert.That(field.ClampPlayer(outside, new Float2(21f, 21f)), Is.EqualTo(outside));
        }

        [Test]
        public void CatalogAssignsApprovedBoundsWithoutChangingFirstStage()
        {
            Assert.That(StageCombatCatalog.For(StageId.GwigokField).Battlefield.IsBounded, Is.False);
            Assert.That(StageCombatCatalog.For(StageId.DokkaebiPass).Battlefield.Width, Is.EqualTo(72f));
            Assert.That(StageCombatCatalog.For(StageId.DokkaebiPass).Battlefield.Height, Is.EqualTo(112f));
            Assert.That(StageCombatCatalog.For(StageId.MoonlitTomb).Battlefield.Width, Is.EqualTo(84f));
            Assert.That(StageCombatCatalog.For(StageId.MoonlitTomb).Battlefield.Height, Is.EqualTo(84f));
        }

        [Test]
        public void BoundedFieldRejectsCameraThatCannotFit()
        {
            var field = StageBattlefieldDefinition.Bounded(30f, 40f, "small");

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                field.ClampPlayer(default, new Float2(16f, 4f)));
        }
    }
}
