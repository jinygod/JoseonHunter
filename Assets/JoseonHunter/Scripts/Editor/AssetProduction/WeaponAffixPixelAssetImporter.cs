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

        [InitializeOnLoadMethod]
        private static void ScheduleMissingCatalogImport()
        {
            EditorApplication.delayCall += ImportIfCatalogIsIncomplete;
        }

        [MenuItem("JoseonHunter/Assets/Import Affix Jackpot Pixel Atlases")]
        public static void EnsureImported()
        {
            ConfigureAtlas(SlotKitPath, SlotSpriteNames, 64, 64);
            ConfigureAtlas(StatusSymbolsPath, StatusSpriteNames, 64, 64);
            ConfigureAtlas(PotentialPartsAPath, PotentialIds.Take(12).Select(id => id.Value).ToArray(), 64, 32);
            ConfigureAtlas(PotentialPartsBPath, PotentialIds.Skip(12).Select(id => id.Value).ToArray(), 64, 32);
            ConfigureMask(PotentialPartsAMaskPath);
            ConfigureMask(PotentialPartsBMaskPath);
            CreateOrUpdateCatalog();
        }

        private static void ImportIfCatalogIsIncomplete()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponAffixPresentationCatalogAsset>(CatalogPath);
            if (catalog != null && catalog.SpriteForAffix(WeaponAffixTier.Standard) != null &&
                catalog.SpriteForPotential(WeaponPotentialId.HwandoVenomFang) != null) return;
            EnsureImported();
        }

        private static void ConfigureAtlas(string path, string[] names, int cellWidth, int cellHeight)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing checked-in atlas at '{path}'.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.spritesheet = names.Select((name, index) => new SpriteMetaData
            {
                name = name,
                rect = new Rect((index % 4) * cellWidth, 128 - ((index / 4) + 1) * cellHeight, cellWidth, cellHeight),
                pivot = new Vector2(.5f, .5f),
                alignment = (int)SpriteAlignment.Center
            }).ToArray();
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

            var slotSprites = SpritesAt(SlotKitPath);
            var spritesA = SpritesAt(PotentialPartsAPath);
            var spritesB = SpritesAt(PotentialPartsBPath);
            var maskA = AssetDatabase.LoadAssetAtPath<Texture2D>(PotentialPartsAMaskPath);
            var maskB = AssetDatabase.LoadAssetAtPath<Texture2D>(PotentialPartsBMaskPath);
            var entries = new List<WeaponAffixPresentationCatalogAsset.PotentialPresentation>();
            for (var index = 0; index < PotentialIds.Length; index++)
            {
                var sprite = index < 12 ? spritesA[PotentialIds[index].Value] : spritesB[PotentialIds[index].Value];
                entries.Add(new WeaponAffixPresentationCatalogAsset.PotentialPresentation(PotentialIds[index], sprite, index < 12 ? maskA : maskB));
            }
            catalog.SetForImport(new[]
            {
                new WeaponAffixPresentationCatalogAsset.RarityFrame(WeaponAffixTier.Standard, slotSprites["standard_frame"]),
                new WeaponAffixPresentationCatalogAsset.RarityFrame(WeaponAffixTier.High, slotSprites["high_frame"]),
                new WeaponAffixPresentationCatalogAsset.RarityFrame(WeaponAffixTier.Perfect, slotSprites["perfect_frame"])
            }, entries.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static Dictionary<string, Sprite> SpritesAt(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
    }
}
