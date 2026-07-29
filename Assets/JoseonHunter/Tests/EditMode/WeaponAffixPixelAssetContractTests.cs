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
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var ids = WeaponAffixCatalog.CompatiblePotentials(WeaponRoster.All[0])
                .Concat(WeaponRoster.All.Skip(1).SelectMany(WeaponAffixCatalog.CompatiblePotentials)).Distinct().ToArray();
            Assert.That(ids, Has.Length.EqualTo(24));
            Assert.That(catalog.Validate(ids), Is.Empty);
            foreach (var id in ids)
            {
                var sprite = catalog.SpriteForPotential(id);
                Assert.That(sprite, Is.Not.Null, id.Value);
                var mask = catalog.MaskForPotential(id);
                Assert.That(mask, Is.Not.Null, id.Value);
                Assert.That(mask.width, Is.EqualTo(64), id.Value);
                Assert.That(mask.height, Is.EqualTo(32), id.Value);
                ValidateCellLocalMask(sprite, mask, id.Value);
                Assert.That(catalog.TryGetPotentialPresentation(id, out var entry), Is.True, id.Value);
                Assert.That(entry.SourceRect, Is.EqualTo(ExpectedRect(id)));
                Assert.That(entry.Pivot, Is.EqualTo(sprite.pivot));
                Assert.That(entry.Sprite.rect.size, Is.EqualTo(new Vector2(64f, 32f)));
            }
            ValidateMaskSubset(WeaponAffixPixelAssetImporter.PotentialPartsAPath, WeaponAffixPixelAssetImporter.PotentialPartsAMaskPath);
            ValidateMaskSubset(WeaponAffixPixelAssetImporter.PotentialPartsBPath, WeaponAffixPixelAssetImporter.PotentialPartsBMaskPath);
        }

        private static Rect ExpectedRect(WeaponPotentialId id)
        {
            var index = RequiredIds().ToList().FindIndex(candidate => candidate.Equals(id));
            return new Rect((index % 4) * 64, 128 - ((index % 12 / 4) + 1) * 32, 64, 32);
        }

        private static IEnumerable<WeaponPotentialId> RequiredIds() => WeaponAffixCatalog.CompatiblePotentials(WeaponRoster.All[0])
            .Concat(WeaponRoster.All.Skip(1).SelectMany(WeaponAffixCatalog.CompatiblePotentials)).Distinct();

        [Test]
        public void UiAtlasesHaveNoGameplayMaskAssets()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(WeaponAffixPixelAssetImporter.SlotKitPath.Replace(".png", "-hit-mask.png")), Is.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(WeaponAffixPixelAssetImporter.StatusSymbolsPath.Replace(".png", "-hit-mask.png")), Is.Null);
        }

        [Test]
        public void Every_potential_sprite_and_mask_uses_the_mobile_safe_pixel_import_profile()
        {
            foreach (var id in RequiredIds())
            {
                AssertPixelImport("Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/Sprites/" + id.Value + ".png");
                AssertPixelImport("Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/Masks/" + id.Value + "-hit-mask.png");
            }
        }

        [Test]
        public void Micro_slot_uses_one_point_filtered_sprite_per_png()
        {
            var names = new[]
            {
                "slot_machine_shell", "reel_window", "locked_potential_slot", "reel_symbol_stat",
                "reel_symbol_rarity", "reel_symbol_potential", "reel_stop_flash",
                "jackpot_burst_1", "jackpot_burst_2", "jackpot_burst_3"
            };
            foreach (var name in names)
            {
                var path = WeaponAffixPixelAssetImporter.MicroSlotRoot + "/" + name + ".png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.GreaterThanOrEqualTo(48), path);
                Assert.That(texture.height, Is.GreaterThanOrEqualTo(48), path);
                AssertPixelImport(path);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                Assert.That(sprites, Has.Length.EqualTo(1), path);
            }

            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.HasRequiredUiSprites, Is.True);
        }

        private static void AssertPixelImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.isReadable, Is.True, path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
            foreach (var platform in new[] { "Android", "Standalone", "WebGL" })
                Assert.That(importer.GetPlatformTextureSettings(platform).overridden, Is.False, path + " " + platform);
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

        private static void ValidateCellLocalMask(Sprite sprite, Texture2D mask, string label)
        {
            Assert.That(sprite.texture.width, Is.EqualTo(64), label);
            Assert.That(sprite.texture.height, Is.EqualTo(32), label);
            var sourcePixels = sprite.texture.GetPixels32();
            var maskPixels = mask.GetPixels32();
            var opaquePixelCount = 0;
            for (var index = 0; index < maskPixels.Length; index++)
            {
                Assert.That(maskPixels[index].a == 0 || maskPixels[index].a == byte.MaxValue, Is.True, label);
                if (maskPixels[index].a != byte.MaxValue)
                {
                    continue;
                }

                opaquePixelCount++;
                Assert.That(sourcePixels[index].a, Is.EqualTo(byte.MaxValue), label);
            }

            Assert.That(opaquePixelCount, Is.GreaterThan(0), label);
        }
    }
}
