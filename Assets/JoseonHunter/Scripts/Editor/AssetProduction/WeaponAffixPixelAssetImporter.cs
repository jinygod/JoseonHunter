using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Progression;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class WeaponAffixPixelAssetImporter
    {
        public const string SlotKitPath = "Assets/JoseonHunter/Art/UI/AffixJackpot/slot-kit.png";
        public const string StatusSymbolsPath = "Assets/JoseonHunter/Art/UI/AffixJackpot/status-symbols.png";
        public const string PotentialPartsAPath = "Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/potential-parts-a.png";
        public const string PotentialPartsAMaskPath = "Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/potential-parts-a-hit-mask.png";
        public const string PotentialPartsBPath = "Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/potential-parts-b.png";
        public const string PotentialPartsBMaskPath = "Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/potential-parts-b-hit-mask.png";
        public const string CatalogPath = "Assets/JoseonHunter/Resources/WeaponAffixPresentationCatalog.asset";

        private const float PixelsPerUnit = 32f;
        private static readonly string[] SlotSpriteNames = { "reel_frame", "standard_frame", "high_frame", "perfect_frame", "jackpot_burst_1", "jackpot_burst_2", "jackpot_burst_3", "rarity_flash" };
        private static readonly string[] StatusSpriteNames = { "poison", "burn", "frost", "bleed", "armor_break", "seal_transfer", "lightning_mark", "experience" };
        private static readonly WeaponPotentialId[] PotentialIds =
        {
            WeaponPotentialId.HwandoVenomFang, WeaponPotentialId.HwandoReturningAfterimage, WeaponPotentialId.HwandoFlyingBladeDance, WeaponPotentialId.GakgungArmorBreakArrowhead,
            WeaponPotentialId.GakgungSplitFletching, WeaponPotentialId.GakgungFullDraw, WeaponPotentialId.TalismanFiveElementCycle, WeaponPotentialId.TalismanSealTransfer,
            WeaponPotentialId.TalismanVengefulGhostBurst, WeaponPotentialId.ThunderEarthCurrent, WeaponPotentialId.ThunderOverchargedCore, WeaponPotentialId.ThunderLightningRod,
            WeaponPotentialId.JangseungGhostFace, WeaponPotentialId.JangseungFourDirectionBarrier, WeaponPotentialId.JangseungGuardianDescent, WeaponPotentialId.SingijeonPowderTrail,
            WeaponPotentialId.SingijeonSubmunitionSplit, WeaponPotentialId.SingijeonChainIgnition, WeaponPotentialId.FrostCrackMark, WeaponPotentialId.FrostSpread,
            WeaponPotentialId.FrostMist, WeaponPotentialId.FanVacuumEdge, WeaponPotentialId.FanDistantThunder, WeaponPotentialId.FanReturningChain
        };

        [MenuItem("JoseonHunter/Assets/Import Affix Jackpot Pixel Atlases")]
        public static void EnsureImported()
        {
            ConfigureSlotKit();
            ConfigureAtlas(StatusSymbolsPath);
            ConfigureAtlas(PotentialPartsAPath);
            ConfigureAtlas(PotentialPartsBPath);
            ConfigureMask(PotentialPartsAMaskPath);
            ConfigureMask(PotentialPartsBMaskPath);
            foreach (var potentialId in PotentialIds) ConfigureMask(MaskPathFor(potentialId));
            foreach (var potentialId in PotentialIds) ConfigureSprite(SpritePathFor(potentialId));
            ConfigureSprite(RarityFramePath("standard"));
            ConfigureSprite(RarityFramePath("high"));
            ConfigureSprite(RarityFramePath("perfect"));
            CreateOrUpdateCatalog();
        }

        private static void ConfigureAtlas(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing checked-in atlas at '{path}'.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing checked-in cell sprite at '{path}'.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void ConfigureMask(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing checked-in potential mask at '{path}'.");
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void CreateOrUpdateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponAffixPresentationCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WeaponAffixPresentationCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var entries = new List<WeaponAffixPresentationCatalogAsset.PotentialPresentation>();
            for (var index = 0; index < PotentialIds.Length; index++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePathFor(PotentialIds[index]));
                var rect = new Rect((index % 4) * 64, 128 - ((index % 12 / 4) + 1) * 32, 64, 32);
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPathFor(PotentialIds[index]));
                entries.Add(new WeaponAffixPresentationCatalogAsset.PotentialPresentation(PotentialIds[index], sprite, mask, rect, sprite.pivot));
            }
            catalog.SetForImport(new[]
            {
                new WeaponAffixPresentationCatalogAsset.RarityFrame(WeaponAffixTier.Standard, AssetDatabase.LoadAssetAtPath<Sprite>(RarityFramePath("standard"))),
                new WeaponAffixPresentationCatalogAsset.RarityFrame(WeaponAffixTier.High, AssetDatabase.LoadAssetAtPath<Sprite>(RarityFramePath("high"))),
                new WeaponAffixPresentationCatalogAsset.RarityFrame(WeaponAffixTier.Perfect, AssetDatabase.LoadAssetAtPath<Sprite>(RarityFramePath("perfect")))
            }, entries.ToArray());
            var slotSprites = AssetDatabase.LoadAllAssetsAtPath(SlotKitPath).OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
            catalog.SetSlotKitForImport(
                slotSprites["reel_frame"], slotSprites["reel_frame"],
                slotSprites["jackpot_burst_1"], slotSprites["jackpot_burst_2"], slotSprites["jackpot_burst_3"]);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureSlotKit()
        {
            var importer = AssetImporter.GetAtPath(SlotKitPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing checked-in atlas at '{SlotKitPath}'.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.spritesheet = new[]
            {
                Slice("reel_frame", 0, 64), Slice("standard_frame", 64, 64), Slice("high_frame", 128, 64), Slice("perfect_frame", 192, 64),
                Slice("jackpot_burst_1", 0, 0), Slice("jackpot_burst_2", 64, 0), Slice("jackpot_burst_3", 128, 0), Slice("rarity_flash", 192, 0)
            };
            importer.SaveAndReimport();
        }

        private static SpriteMetaData Slice(string name, float x, float y) => new SpriteMetaData
        {
            name = name,
            rect = new Rect(x, y, 64f, 64f),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(.5f, .5f)
        };

        private static string MaskPathFor(WeaponPotentialId id) =>
            "Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/Masks/" + id.Value + "-hit-mask.png";

        private static string SpritePathFor(WeaponPotentialId id) =>
            "Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/Sprites/" + id.Value + ".png";

        private static string RarityFramePath(string tier) =>
            "Assets/JoseonHunter/Art/UI/AffixJackpot/RarityFrames/" + tier + ".png";
    }
}
