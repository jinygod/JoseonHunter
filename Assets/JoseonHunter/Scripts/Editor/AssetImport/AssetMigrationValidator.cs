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
            var assetRights = ReadRightsLedger(
                Path.Combine(projectRoot, "Docs/Assets/asset-rights-ledger.csv"), "asset", errors);
            var audioRights = ReadRightsLedger(
                Path.Combine(projectRoot, "Docs/Assets/audio-rights-ledger.csv"), "audio", errors);
            var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in manifest.entries)
            {
                ValidateEntry(entry, projectRoot, assetRights, audioRights, destinations, errors);
            }

            return errors.ToList();
        }

        private static void ValidateEntry(
            AssetMigrationEntry entry,
            string projectRoot,
            IDictionary<string, RightsRecord> assetRights,
            IDictionary<string, RightsRecord> audioRights,
            ISet<string> destinations,
            ISet<string> errors)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.destination))
            {
                errors.Add("manifest entry has no destination");
                return;
            }

            var requestedDestination = NormalizePath(entry.destination);
            string destination;
            string destinationPath;
            if (!TryResolveApprovedDestination(projectRoot, requestedDestination, out destination, out destinationPath))
            {
                errors.Add("destination outside approved roots: " + requestedDestination);
                return;
            }

            if (!destinations.Add(destination))
            {
                errors.Add("duplicate destination: " + destination);
            }

            if (!string.Equals(entry.licenseStatus, "approved", StringComparison.Ordinal))
            {
                errors.Add("license status is not approved: " + destination);
            }
            if (!File.Exists(destinationPath))
            {
                errors.Add("missing destination file: " + destination);
            }

            ValidateRights(entry, destination, destinationPath, assetRights, audioRights, errors);
            ValidateImporter(entry, destination, errors);
            ValidateFontLicense(entry, destination, projectRoot, errors);
        }

        private static void ValidateRights(
            AssetMigrationEntry entry,
            string destination,
            string destinationPath,
            IDictionary<string, RightsRecord> assetRights,
            IDictionary<string, RightsRecord> audioRights,
            ISet<string> errors)
        {
            if (string.Equals(entry.profile, "raw", StringComparison.Ordinal))
            {
                return;
            }

            var isAudio = string.Equals(entry.profile, "music", StringComparison.Ordinal) ||
                string.Equals(entry.profile, "sfx", StringComparison.Ordinal) ||
                NormalizePath(entry.source).StartsWith("assets/audio/", StringComparison.OrdinalIgnoreCase);
            var rightsByPath = isAudio ? audioRights : assetRights;
            var ledgerName = isAudio ? "audio" : "asset";
            RightsRecord rights;
            if (!rightsByPath.TryGetValue(NormalizePath(entry.source), out rights))
            {
                errors.Add("missing " + ledgerName + " rights ledger entry: " + entry.source);
                return;
            }

            if (!string.Equals(rights.Status, "approved", StringComparison.Ordinal))
            {
                errors.Add(ledgerName + " rights ledger status is not approved: " + entry.source);
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

        private static Dictionary<string, RightsRecord> ReadRightsLedger(
            string ledgerPath,
            string ledgerName,
            ISet<string> errors)
        {
            var rights = new Dictionary<string, RightsRecord>(StringComparer.Ordinal);
            if (!File.Exists(ledgerPath))
            {
                errors.Add("missing " + ledgerName + " rights ledger: " +
                    NormalizePath(ledgerPath.Substring(Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Length)));
                return rights;
            }

            var lines = File.ReadAllLines(ledgerPath);
            if (lines.Length < 2)
            {
                return rights;
            }

            var headers = lines[0].Split(',');
            var runtimePathIndex = Array.IndexOf(headers, "runtime_path");
            if (runtimePathIndex < 0)
            {
                runtimePathIndex = Array.IndexOf(headers, "local_path");
            }
            var statusIndex = Array.IndexOf(headers, "status");
            var notesIndex = Array.IndexOf(headers, "notes");
            if (runtimePathIndex < 0 || statusIndex < 0)
            {
                errors.Add("rights ledger is missing required columns");
                return rights;
            }

            for (var index = 1; index < lines.Length; index++)
            {
                var columns = lines[index].Split(',');
                if (columns.Length <= Math.Max(runtimePathIndex, statusIndex))
                {
                    continue;
                }

                var match = notesIndex >= 0 && columns.Length > notesIndex
                    ? RuntimeHash.Match(columns[notesIndex])
                    : Match.Empty;
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

        private static bool TryResolveApprovedDestination(
            string projectRoot,
            string destination,
            out string canonicalDestination,
            out string destinationPath)
        {
            destinationPath = Path.GetFullPath(Path.Combine(projectRoot, destination));
            var assetRoot = Path.GetFullPath(Path.Combine(projectRoot, AssetRoot));
            var documentationRoot = Path.GetFullPath(Path.Combine(projectRoot, DocumentationRoot));
            if (!IsContainedBy(destinationPath, assetRoot) && !IsContainedBy(destinationPath, documentationRoot))
            {
                canonicalDestination = null;
                return false;
            }

            canonicalDestination = NormalizePath(destinationPath.Substring(projectRoot.Length).TrimStart('\\', '/'));
            return true;
        }

        private static bool IsContainedBy(string candidate, string root)
        {
            var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
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
