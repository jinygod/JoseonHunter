using NUnit.Framework;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons;
using UnityEngine;

namespace JoseonHunter.Tests.PlayMode
{
    /// <summary>Task 7 guardrail: every runtime potential must use its checked-in Task4 mask, never a display-only overlap.</summary>
    public sealed class WeaponPotentialCombatBPlayModeTests
    {
        private static readonly WeaponPotentialId[] Potentials =
        {
            WeaponPotentialId.JangseungGhostFace, WeaponPotentialId.JangseungFourDirectionBarrier, WeaponPotentialId.JangseungGuardianDescent,
            WeaponPotentialId.SingijeonPowderTrail, WeaponPotentialId.SingijeonSubmunitionSplit, WeaponPotentialId.SingijeonChainIgnition,
            WeaponPotentialId.FrostCrackMark, WeaponPotentialId.FrostSpread, WeaponPotentialId.FrostMist,
            WeaponPotentialId.FanVacuumEdge, WeaponPotentialId.FanDistantThunder, WeaponPotentialId.FanReturningChain
        };

        [Test]
        public void Every_task7_potential_has_a_real_catalog_sprite_and_pixel_mask()
        {
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            foreach (var potential in Potentials)
            {
                var sprite = catalog.SpriteForPotential(potential); var texture = catalog.MaskForPotential(potential);
                Assert.That(sprite, Is.Not.Null, potential.Value); Assert.That(texture, Is.Not.Null, potential.Value);
                var mask = PixelHitMask.FromTexture(texture, sprite.pivot, sprite.pixelsPerUnit);
                Assert.That(mask, Is.Not.Null, potential.Value);
            }
        }

        [Test]
        public void Potential_masks_do_not_authorize_a_base_only_target_fixture()
        {
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            var baseMask = PixelHitMask.FromRows("1");
            foreach (var potential in Potentials)
            {
                var sprite = catalog.SpriteForPotential(potential); var mask = PixelHitMask.FromTexture(catalog.MaskForPotential(potential), sprite.pivot, sprite.pixelsPerUnit);
                Assert.That(PixelMaskContactService.TryFindContact(mask, PixelMaskTransform.Translation(1f, 0f), baseMask, PixelMaskTransform.Translation(1f, 0f), out _), Is.False, potential.Value);
            }
        }
    }
}
