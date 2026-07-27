using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PixelMaskContactTests
    {
        [Test]
        public void TransparentGlowCannotConfirmContact()
        {
            var attack = PixelHitMask.FromRows("0000", "0100", "0000");
            var enemy = PixelHitMask.FromRows("1");
            Assert.That(PixelMaskContactService.TryFindContact(attack, PixelMaskTransform.Identity, enemy, PixelMaskTransform.Translation(0f, 0f), out _), Is.False);
        }

        [Test]
        public void ActivePixelConfirmsContactAfterFlipAndRotation()
        {
            var attack = PixelHitMask.FromRows("001", "000", "000");
            var enemy = PixelHitMask.FromRows("1");
            var transform = new PixelMaskTransform(new Float2(2f, 0f), 90, true, 1);
            Assert.That(PixelMaskContactService.TryFindContact(attack, transform, enemy, PixelMaskTransform.Translation(2f, -2f), out var point), Is.True);
            Assert.That(point, Is.EqualTo(new Float2(2f, -2f)));
        }

        [Test]
        public void ConstructorCopiesPackedBitsToRemainImmutable()
        {
            var bits = new[] { 1u };
            var mask = new PixelHitMask(1, 1, UnityEngine.Vector2.zero, 1f, bits);
            bits[0] = 0;
            Assert.That(mask.IsActive(0, 0), Is.True);
        }
    }
}
