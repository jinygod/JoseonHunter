using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetImport
{
    public static class AssetMigrationValidator
    {
        private const string AssetRoot = "Assets/JoseonHunter/";
        private const string DocumentationRoot = "Docs/Assets/";
        private const string FontRoot = "Assets/JoseonHunter/Art/Fonts/";
        private static readonly Regex RuntimeHash = new Regex(
            "runtime-sha256=([0-9A-Fa-f]{64})", RegexOptions.Compiled);

        public static IReadOnlyList<string> Validate(string manifestPath)
        {
            var errors = new SortedSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            {
                errors.Add("manifest file does not exist: " + manifestPath);
                return errors.ToList();
            }

            var manifest = JsonUtility.FromJson<AssetMigrationManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.entries == null)
            {
                errors.Add("manifest has no entries");
                return errors.ToList();
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var rightsByPath = ReadRightsLedger(Path.Combine(projectRoot, "Docs/Assets/asset-rights-ledger.csv"), errors);
            var destinations = new HashSet<string>(StringComparer.Ordinal);
            var approvedDestinations = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in manifest.entries)
            {
                ValidateEntry(entry, projectRoot, rightsByPath, destinations, approvedDestinations, errors);
            }

            ValidateApprovedAssetsAreManifested(projectRoot, approvedDestinations, errors);
            return errors.ToList();
        }

        private static void ValidateEntry(
            AssetMigrationEntry entry,
            string projectRoot,
            IDictionary<string, RightsRecord> rightsByPath,
            ISet<string> destinations,
            ISet<string> approvedDestinations,
            ISet<string> errors)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.destination))
            {
                errors.Add("manifest entry has no destination");
                return;
            }

            var destination = NormalizePath(entry.destination);
            if (!destinations.Add(destination))
            {
                errors.Add("duplicate destination: " + destination);
            }

            if (!IsApprovedDestination(destination))
            {
                errors.Add("destination outside approved roots: " + destination);
                return;
            }

            if (!string.Equals(entry.licenseStatus, "approved", StringComparison.Ordinal))
            {
                errors.Add("license status is not approved: " + destination);
            }
            else
            {
                approvedDestinations.Add(destination);
            }

            var destinationPath = Path.Combine(projectRoot, destination);
            if (!File.Exists(destinationPath))
            {
                errors.Add("missing destination file: " + destination);
            }

            ValidateRights(entry, destination, destinationPath, rightsByPath, errors);
            ValidateImporter(entry, destination, errors);
            ValidateFontLicense(entry, destination, projectRoot, errors);
        }

        private static void ValidateRights(
            AssetMigrationEntry entry,
            string destination,
            string destinationPath,
            IDictionary<string, RightsRecord> rightsByPath,
            ISet<string> errors)
        {
            if (string.Equals(entry.profile, "raw", StringComparison.Ordinal))
            {
                return;
            }

            RightsRecord rights;
            if (!rightsByPath.TryGetValue(NormalizePath(entry.source), out rights))
            {
                errors.Add("missing rights ledger entry: " + entry.source);
                return;
            }

            if (!string.Equals(rights.Status, "approved", StringComparison.Ordinal))
            {
                errors.Add("rights ledger status is not approved: " + entry.source);
            }

            if (!File.Exists(destinationPath) || string.IsNullOrEmpty(rights.RuntimeHash))
            {
                return;
            }

            if (!string.Equals(ComputeSha256(destinationPath), rights.RuntimeHash, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("destination hash does not match rights ledger: " + destination);
            }
        }

        private static void ValidateImporter(AssetMigrationEntry entry, string destination, ISet<string> errors)
        {
            if (!string.Equals(entry.profile, "pixel", StringComparison.Ordinal) ||
                !destination.StartsWith(AssetRoot, StringComparison.Ordinal))
            {
                return;
            }

            var textureImporter = AssetImporter.GetAtPath(destination) as TextureImporter;
            if (textureImporter == null)
            {
                errors.Add("pixel profile has no texture importer: " + destination);
                return;
            }

            if (textureImporter.mipmapEnabled)
            {
                errors.Add("pixel profile has mipmaps enabled: " + destination);
            }
        }

        private static void ValidateFontLicense(
            AssetMigrationEntry entry,
            string destination,
            string projectRoot,
            ISet<string> errors)
        {
            if (!destination.StartsWith(FontRoot, StringComparison.Ordinal))
            {
                return;
            }

            var fontName = Path.GetFileName(destination);
            var licenseName = fontName.StartsWith("SongMyung", StringComparison.Ordinal)
                ? "SongMyung-OFL.txt"
                : fontName.StartsWith("GowunBatang", StringComparison.Ordinal) ? "GowunBatang-OFL.txt" : null;
            if (licenseName == null)
            {
                return;
            }

            var licenseDestination = FontRoot + "Licenses/" + licenseName;
            if (!File.Exists(Path.Combine(projectRoot, licenseDestination)))
            {
                errors.Add("missing font license: " + licenseName);
            }
        }

        private static void ValidateApprovedAssetsAreManifested(
            string projectRoot,
            ISet<string> approvedDestinations,
            ISet<string> errors)
        {
            var artDirectory = Path.Combine(projectRoot, "Assets/JoseonHunter/Art");
            if (!Directory.Exists(artDirectory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(artDirectory, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var destination = NormalizePath(file.Substring(projectRoot.Length).TrimStart('\\', '/'));
                if (!approvedDestinations.Contains(destination))
                {
                    errors.Add("asset is not approved by manifest: " + destination);
                }
            }
        }

        private static Dictionary<string, RightsRecord> ReadRightsLedger(string ledgerPath, ISet<string> errors)
        {
            var rights = new Dictionary<string, RightsRecord>(StringComparer.Ordinal);
            if (!File.Exists(ledgerPath))
            {
                errors.Add("missing rights ledger: Docs/Assets/asset-rights-ledger.csv");
                return rights;
            }

            var lines = File.ReadAllLines(ledgerPath);
            if (lines.Length < 2)
            {
                return rights;
            }

            var headers = lines[0].Split(',');
            var runtimePathIndex = Array.IndexOf(headers, "runtime_path");
            var statusIndex = Array.IndexOf(headers, "status");
            var notesIndex = Array.IndexOf(headers, "notes");
            if (runtimePathIndex < 0 || statusIndex < 0 || notesIndex < 0)
            {
                errors.Add("rights ledger is missing required columns");
                return rights;
            }

            for (var index = 1; index < lines.Length; index++)
            {
                var columns = lines[index].Split(',');
                if (columns.Length <= Math.Max(runtimePathIndex, Math.Max(statusIndex, notesIndex)))
                {
                    continue;
                }

                var match = RuntimeHash.Match(columns[notesIndex]);
                rights[NormalizePath(columns[runtimePathIndex])] = new RightsRecord(
                    columns[statusIndex], match.Success ? match.Groups[1].Value : null);
            }

            return rights;
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static bool IsApprovedDestination(string destination)
        {
            return destination.StartsWith(AssetRoot, StringComparison.Ordinal) ||
                destination.StartsWith(DocumentationRoot, StringComparison.Ordinal);
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
        }

        private readonly struct RightsRecord
        {
            public RightsRecord(string status, string runtimeHash)
            {
                Status = status;
                RuntimeHash = runtimeHash;
            }

            public string Status { get; }
            public string RuntimeHash { get; }
        }
    }
}
