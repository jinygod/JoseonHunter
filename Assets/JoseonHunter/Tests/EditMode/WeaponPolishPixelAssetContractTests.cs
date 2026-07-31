using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponPolishPixelAssetContractTests
    {
        private const string KnownFixturePath =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando/hwando_blade.png";
        private const string PolishRoot =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish";
        private const string LedgerPath =
            "ArtSource/Pixel/Weapons/Polish/pixellab-eight-weapon-polish-ledger.csv";

        [TestCase("Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Hwando/hwando_blade.png")]
        [TestCase("Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Gakgung/gakgung_arrow.png")]
        public void ExistingPolishFrame_UsesMobilePixelImportContract(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(
                WeaponPixelAssetContract.ValidatePolishFrame(texture, importer, path),
                Is.Empty);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
        }

        [Test]
        public void PolishFrame_RejectsSpriteSheetMode()
        {
            var importer = AssetImporter.GetAtPath(KnownFixturePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            var originalMode = importer.spriteImportMode;

            try
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;

                Assert.That(
                    WeaponPixelAssetContract.ValidatePolishFrame(
                        AssetDatabase.LoadAssetAtPath<Texture2D>(KnownFixturePath),
                        importer,
                        KnownFixturePath),
                    Does.Contain("polish frame must be a single sprite"));
            }
            finally
            {
                importer.spriteImportMode = originalMode;
            }
        }

        [Test]
        public void PolishFrame_RejectsOffCenterPivot()
        {
            var importer = AssetImporter.GetAtPath(KnownFixturePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            var originalSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(originalSettings);
            var alteredSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(alteredSettings);

            try
            {
                alteredSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                alteredSettings.spritePivot = new Vector2(0.25f, 0.75f);
                importer.SetTextureSettings(alteredSettings);

                Assert.That(
                    WeaponPixelAssetContract.ValidatePolishFrame(
                        AssetDatabase.LoadAssetAtPath<Texture2D>(KnownFixturePath),
                        importer,
                        KnownFixturePath),
                    Does.Contain("polish frame pivot must be centered"));
            }
            finally
            {
                importer.SetTextureSettings(originalSettings);
            }
        }

        [Test]
        public void GeneratedPolishBatch_UsesContractOnEveryIndividualFrame()
        {
            var paths = Directory.GetFiles(PolishRoot, "*.png", SearchOption.AllDirectories);
            Assert.That(paths, Has.Length.EqualTo(119));

            foreach (var path in paths)
            {
                var assetPath = path.Replace('\\', '/');
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.That(
                    WeaponPixelAssetContract.ValidatePolishFrame(texture, importer, assetPath),
                    Is.Empty,
                    assetPath);
            }
        }

        [Test]
        public void GeneratedPolishBatch_HasEveryPlannedWeaponStageCount()
        {
            var expectedCounts = new Dictionary<string, int>
            {
                ["Hwando/hwando_blade"] = 4,
                ["Hwando/hwando_afterimage"] = 4,
                ["Hwando/hwando_contact_spark"] = 4,
                ["Gakgung/gakgung_aim_glint"] = 3,
                ["Gakgung/gakgung_arrow"] = 3,
                ["Gakgung/gakgung_impact_splinter"] = 5,
                ["Talisman/talisman_rotate"] = 4,
                ["Talisman/talisman_seal_pulse"] = 5,
                ["Talisman/talisman_binding"] = 5,
                ["Thunder/thunder_lob"] = 6,
                ["Thunder/thunder_warning"] = 4,
                ["Thunder/thunder_blast"] = 6,
                ["Thunder/thunder_ground_current"] = 5,
                ["Jangseung/jangseung_rise"] = 5,
                ["Jangseung/jangseung_ward"] = 4,
                ["Jangseung/jangseung_strike"] = 5,
                ["Singijeon/singijeon_rocket"] = 4,
                ["Singijeon/singijeon_ember"] = 5,
                ["Singijeon/singijeon_explosion"] = 6,
                ["Frost/frost_flask"] = 6,
                ["Frost/frost_growth"] = 5,
                ["Frost/frost_shatter"] = 6,
                ["Fan/fan_gust"] = 5,
                ["Fan/fan_target"] = 4,
                ["Fan/fan_lightning"] = 6,
            };

            foreach (var pair in expectedCounts)
            {
                var separator = pair.Key.IndexOf('/');
                var weapon = pair.Key.Substring(0, separator);
                var prefix = pair.Key.Substring(separator + 1);
                var paths = Directory.GetFiles(
                    Path.Combine(PolishRoot, weapon),
                    prefix + "*.png",
                    SearchOption.TopDirectoryOnly);
                Assert.That(paths, Has.Length.EqualTo(pair.Value), pair.Key);
            }
        }

        [Test]
        public void PixelLabLedger_CoversEveryAdoptedFrameAndMeaningfulHistory()
        {
            var records = ReadCsv(LedgerPath);
            var adopted = records.Where(row => row["status"] == "adopted").ToArray();
            var adoptedPaths = adopted.Select(row => row["adopted_path"]).ToArray();
            var generatedPaths = Directory.GetFiles(PolishRoot, "*.png", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .ToArray();

            Assert.That(adopted, Has.Length.EqualTo(119));
            Assert.That(adoptedPaths, Has.All.Not.Empty);
            Assert.That(adoptedPaths.Distinct().Count(), Is.EqualTo(adoptedPaths.Length));
            Assert.That(adoptedPaths, Is.EquivalentTo(generatedPaths));
            Assert.That(records.Count(row => row["status"] == "source_seed"), Is.EqualTo(25));
            Assert.That(records.All(row =>
                row["status"] == "source_seed" ||
                row["status"] == "adopted" ||
                row["status"] == "rejected"), Is.True);
            Assert.That(
                records.Where(row => row["status"] == "rejected"),
                Has.All.Matches<Dictionary<string, string>>(row =>
                    string.IsNullOrEmpty(row["adopted_path"]) &&
                    !string.IsNullOrWhiteSpace(row["rejection_reason"])));

            foreach (var repairedStage in new[]
            {
                "Gakgung/impact_splinter",
                "Thunder/landing_warning",
                "Jangseung/ward_pulse",
                "Frost/shatter",
            })
            {
                var parts = repairedStage.Split('/');
                var history = records.Where(row =>
                    row["weapon"] == parts[0] && row["stage"] == parts[1]).ToArray();
                Assert.That(history.Any(row => row["status"] == "rejected"), Is.True, repairedStage);
                Assert.That(
                    history.Any(row =>
                        row["status"] == "adopted" &&
                        row["frame_index"].StartsWith("repair", StringComparison.Ordinal)),
                    Is.True,
                    repairedStage);
            }

            var requiredPromptClauses = new[]
            {
                "Joseon folk-fantasy pixel art VFX",
                "transparent background",
                "crisp hard pixel edges",
                "no anti-aliasing",
                "no text",
                "no UI frame",
                "centered stable pivot",
                "one isolated asset only",
                "orthographic top-down mobile action game",
                "readable at 360x800",
                "restrained silhouette",
            };
            foreach (var row in records)
            foreach (var clause in requiredPromptClauses)
                Assert.That(row["prompt"], Does.Contain(clause), row["job_id"]);
        }

        private static IReadOnlyList<Dictionary<string, string>> ReadCsv(string path)
        {
            var rows = ParseCsv(File.ReadAllText(path));
            Assert.That(rows, Is.Not.Empty);
            var headers = rows[0];
            return rows.Skip(1).Select(values =>
            {
                Assert.That(values, Has.Count.EqualTo(headers.Count));
                return headers.Select((header, index) => new { header, value = values[index] })
                    .ToDictionary(pair => pair.header, pair => pair.value);
            }).ToArray();
        }

        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (character == ',' && !quoted)
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if ((character == '\r' || character == '\n') && !quoted)
                {
                    if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                        index++;
                    row.Add(field.ToString());
                    field.Clear();
                    if (row.Any(value => value.Length != 0)) rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(character);
                }
            }
            if (field.Length != 0 || row.Count != 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }
            return rows;
        }
    }
}
