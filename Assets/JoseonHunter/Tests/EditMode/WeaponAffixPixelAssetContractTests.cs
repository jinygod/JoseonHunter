using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponAffixPixelAssetContractTests
    {
        [Test]
        public void ApprovedAtlasesAreBinaryAndExactDimensions()
        {
            WeaponAffixPixelAssetImporter.EnsureImported();
            foreach (var path in new[] { WeaponAffixPixelAssetImporter.SlotKitPath, WeaponAffixPixelAssetImporter.StatusSymbolsPath, WeaponAffixPixelAssetImporter.PotentialPartsAPath, WeaponAffixPixelAssetImporter.PotentialPartsBPath })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(256), path);
                Assert.That(texture.height, Is.EqualTo(128), path);
                Assert.That(texture.GetPixels32().All(pixel => pixel.a == 0 || pixel.a == byte.MaxValue), Is.True, path);
            }
        }

        [Test]
        public void PotentialMasksAreBinarySubsetsAndEveryPotentialResolves()
        {
            WeaponAffixPixelAssetImporter.EnsureImported();
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var ids = WeaponAffixCatalog.CompatiblePotentials(WeaponRoster.All[0])
                .Concat(WeaponRoster.All.Skip(1).SelectMany(WeaponAffixCatalog.CompatiblePotentials)).Distinct().ToArray();
            Assert.That(ids, Has.Length.EqualTo(24));
            Assert.That(catalog.Validate(ids), Is.Empty);
            foreach (var id in ids)
            {
                Assert.That(catalog.SpriteForPotential(id), Is.Not.Null, id.Value);
                var mask = catalog.MaskForPotential(id);
                Assert.That(mask, Is.Not.Null, id.Value);
                Assert.That(mask.GetPixels32().Any(pixel => pixel.a == byte.MaxValue), Is.True, id.Value);
            }
            ValidateMaskSubset(WeaponAffixPixelAssetImporter.PotentialPartsAPath, WeaponAffixPixelAssetImporter.PotentialPartsAMaskPath);
            ValidateMaskSubset(WeaponAffixPixelAssetImporter.PotentialPartsBPath, WeaponAffixPixelAssetImporter.PotentialPartsBMaskPath);
        }

        [Test]
        public void UiAtlasesHaveNoGameplayMaskAssets()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(WeaponAffixPixelAssetImporter.SlotKitPath.Replace(".png", "-hit-mask.png")), Is.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(WeaponAffixPixelAssetImporter.StatusSymbolsPath.Replace(".png", "-hit-mask.png")), Is.Null);
        }

        private static void ValidateMaskSubset(string sourcePath, string maskPath)
        {
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            Assert.That(mask.width, Is.EqualTo(source.width));
            Assert.That(mask.height, Is.EqualTo(source.height));
            var sourcePixels = source.GetPixels32();
            var maskPixels = mask.GetPixels32();
            for (var index = 0; index < maskPixels.Length; index++)
            {
                Assert.That(maskPixels[index].a == 0 || maskPixels[index].a == byte.MaxValue, Is.True, maskPath);
                Assert.That(maskPixels[index].a != byte.MaxValue || sourcePixels[index].a == byte.MaxValue, Is.True, maskPath);
            }
        }
    }
}
