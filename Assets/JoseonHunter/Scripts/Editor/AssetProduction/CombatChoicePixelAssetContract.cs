using System;
using System.Collections.Generic;
using System.IO;
using JoseonHunter.Content;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class CombatChoicePixelAssetContract
    {
        public const string Root = "Assets/JoseonHunter/Art/CombatChoices";
        public const string CatalogPath = "Assets/JoseonHunter/Resources/CombatChoiceVisualCatalog.asset";

        public static readonly string[] LegacyIds =
        {
            "hwando_venom", "hwando_moon_eclipse", "gakgung_sun_piercer", "gakgung_split_fletching",
            "talisman_heaven_seal", "talisman_ghost_burst", "thunder_prison", "thunder_earth_current",
            "jangseung_four_guardians", "jangseung_guardian_descent", "singijeon_fire_dragon", "singijeon_fire_net",
            "frost_mist", "frost_shatter", "fan_vacuum", "fan_heaven_thunder"
        };
        public static readonly string[] ReactionIds = { "ice_shatter", "fire_wind", "formation_break", "overload" };
        public static readonly string[] EnemyIds = { "shield_dokkaebi", "spirit_shaman", "charging_horn_ghost", "splitting_rat" };

        public static IReadOnlyList<string> Validate(string assetPath)
        {
            var errors = new List<string>();
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) { errors.Add("Texture importer is missing."); return errors; }
            if (importer.textureType != TextureImporterType.Sprite) errors.Add("Texture type must be Sprite.");
            if (importer.filterMode != FilterMode.Point) errors.Add("Filter mode must be Point.");
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) errors.Add("Compression must be disabled.");
            if (importer.mipmapEnabled) errors.Add("Mipmaps must be disabled.");
            var texture = LoadSourceTexture(assetPath);
            if (texture == null) errors.Add("Texture asset is missing.");
            else
            {
                if (NonTransparentColorCount(texture) > 3) errors.Add("Opaque palette exceeds three colors.");
                if (HasOpaqueWhiteOutline(texture)) errors.Add("Opaque white outline is forbidden.");
                UnityEngine.Object.DestroyImmediate(texture);
            }
            return errors;
        }

        public static int NonTransparentColorCount(Texture2D texture)
        {
            if (texture == null) return 0;
            var colors = new HashSet<uint>();
            foreach (var pixel in texture.GetPixels32())
            {
                if (pixel.a == 0) continue;
                colors.Add((uint)(pixel.r << 16 | pixel.g << 8 | pixel.b));
            }
            return colors.Count;
        }

        public static bool HasOpaqueWhiteOutline(Texture2D texture)
        {
            if (texture == null) return false;
            var pixels = texture.GetPixels32(); var width = texture.width; var height = texture.height;
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                if (pixel.a < 240 || pixel.r < 235 || pixel.g < 235 || pixel.b < 235) continue;
                if (AdjacentTransparent(pixels, width, height, x, y)) return true;
            }
            return false;
        }

        private static bool AdjacentTransparent(Color32[] pixels, int width, int height, int x, int y)
        {
            for (var oy = -1; oy <= 1; oy++) for (var ox = -1; ox <= 1; ox++)
            {
                if (ox == 0 && oy == 0) continue;
                var px = x + ox; var py = y + oy;
                if (px < 0 || py < 0 || px >= width || py >= height || pixels[py * width + px].a == 0) return true;
            }
            return false;
        }

        public static void ApplyImporter(string assetPath)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer)) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        private static Texture2D LoadSourceTexture(string assetPath)
        {
            if (!File.Exists(assetPath)) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (ImageConversion.LoadImage(texture, File.ReadAllBytes(assetPath), false)) return texture;
            UnityEngine.Object.DestroyImmediate(texture); return null;
        }

        [MenuItem("Tools/Joseon Hunter/Rebuild Combat Choice Catalog")]
        public static void RebuildCatalog()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
                ApplyImporter(AssetDatabase.GUIDToAssetPath(path));

            var legacy = new CombatChoiceVisualCatalog.LegacyEntry[LegacyIds.Length];
            for (var index = 0; index < LegacyIds.Length; index++) legacy[index] = new CombatChoiceVisualCatalog.LegacyEntry
            { PathId = LegacyIds[index], Icon = LoadSprite($"{Root}/Branches/{LegacyIds[index]}.png") };
            var reactions = new CombatChoiceVisualCatalog.ReactionEntry[ReactionIds.Length];
            for (var index = 0; index < ReactionIds.Length; index++) reactions[index] = new CombatChoiceVisualCatalog.ReactionEntry
            { Kind = (StatusReactionKind)(index + 1), Icon = LoadSprite($"{Root}/Reactions/{ReactionIds[index]}.png") };
            var enemies = new CombatChoiceVisualCatalog.EnemyEntry[EnemyIds.Length];
            for (var index = 0; index < EnemyIds.Length; index++) enemies[index] = new CombatChoiceVisualCatalog.EnemyEntry
            {
                ContentId = EnemyIds[index],
                Frames = new[]
                {
                    LoadSprite($"{Root}/SpecialEnemies/{EnemyIds[index]}/base.png"),
                    LoadSprite($"{Root}/SpecialEnemies/{EnemyIds[index]}/telegraph_0.png"),
                    LoadSprite($"{Root}/SpecialEnemies/{EnemyIds[index]}/telegraph_1.png")
                }
            };
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath) ?? string.Empty);
            var catalog = AssetDatabase.LoadAssetAtPath<CombatChoiceVisualCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<CombatChoiceVisualCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.Configure(legacy, reactions, enemies);
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Joseon Hunter/Quantize PixelLab Combat Choices")]
        public static void QuantizeAndRebuild()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                { UnityEngine.Object.DestroyImmediate(texture); continue; }
                var pixels = texture.GetPixels32();
                var palette = BuildThreeColorPalette(pixels);
                for (var index = 0; index < pixels.Length; index++)
                {
                    if (pixels[index].a < 128) { pixels[index] = new Color32(0, 0, 0, 0); continue; }
                    pixels[index] = Nearest(pixels[index], palette);
                }
                texture.SetPixels32(pixels); texture.Apply(false, false);
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
                UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            RebuildCatalog();
        }

        private static Color32[] BuildThreeColorPalette(Color32[] pixels)
        {
            var opaque = new List<Color32>(pixels.Length);
            foreach (var pixel in pixels) if (pixel.a >= 128) opaque.Add(pixel);
            if (opaque.Count == 0) return new[] { new Color32(40, 32, 30, 255) };
            var darkest = opaque[0]; var brightest = opaque[0]; var saturated = opaque[0];
            foreach (var color in opaque)
            {
                if (Luma(color) < Luma(darkest)) darkest = color;
                if (Luma(color) > Luma(brightest)) brightest = color;
                if (Saturation(color) > Saturation(saturated)) saturated = color;
            }
            var centers = new[] { darkest, saturated, brightest };
            for (var iteration = 0; iteration < 8; iteration++)
            {
                var red = new long[3]; var green = new long[3]; var blue = new long[3]; var count = new int[3];
                foreach (var color in opaque)
                {
                    var nearest = NearestIndex(color, centers);
                    red[nearest] += color.r; green[nearest] += color.g; blue[nearest] += color.b; count[nearest]++;
                }
                for (var index = 0; index < 3; index++) if (count[index] > 0)
                    centers[index] = SafeColor((byte)(red[index] / count[index]), (byte)(green[index] / count[index]),
                        (byte)(blue[index] / count[index]));
            }
            return centers;
        }

        private static Color32 Nearest(Color32 color, Color32[] palette)
        {
            var result = palette[NearestIndex(color, palette)]; result.a = 255; return result;
        }

        private static int NearestIndex(Color32 color, Color32[] palette)
        {
            var best = 0; var bestDistance = long.MaxValue;
            for (var index = 0; index < palette.Length; index++)
            {
                var dr = color.r - palette[index].r; var dg = color.g - palette[index].g; var db = color.b - palette[index].b;
                var distance = dr * dr * 3L + dg * dg * 4L + db * db * 2L;
                if (distance < bestDistance) { bestDistance = distance; best = index; }
            }
            return best;
        }

        private static Color32 SafeColor(byte red, byte green, byte blue)
        {
            if (red >= 235 && green >= 235 && blue >= 235)
                return new Color32(218, 196, 142, 255);
            return new Color32(red, green, blue, 255);
        }

        private static int Luma(Color32 color) => color.r * 3 + color.g * 6 + color.b;
        private static int Saturation(Color32 color) => Math.Max(color.r, Math.Max(color.g, color.b)) -
            Math.Min(color.r, Math.Min(color.g, color.b));

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
