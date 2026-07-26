using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class ProductionAssetValidator
    {
        private static readonly HashSet<string> ValidBatches = new HashSet<string>
        {
            "characters", "enemies", "weapons_vfx", "stage", "ui", "audio", "store"
        };

        private static readonly Regex ApprovedSha256 = new Regex("^[0-9a-f]{64}$", RegexOptions.Compiled);

        public static IReadOnlyList<string> Validate(string manifestPath)
        {
            var errors = new SortedSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            {
                errors.Add("manifest file does not exist: " + manifestPath);
                return errors.ToList();
            }

            var manifest = JsonUtility.FromJson<ProductionAssetManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.assets == null)
            {
                errors.Add("manifest has no assets");
                return errors.ToList();
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in manifest.assets)
            {
                ValidateEntry(entry, ids, errors);
            }

            return errors.ToList();
        }

        private static void ValidateEntry(
            ProductionAssetEntry entry,
            ISet<string> ids,
            ISet<string> errors)
        {
            var id = entry == null ? string.Empty : entry.id;
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("missing asset id");
                return;
            }

            if (!ids.Add(id))
            {
                errors.Add("duplicate asset id: " + id);
            }

            if (!ValidBatches.Contains(entry.batch ?? string.Empty))
            {
                errors.Add("unknown batch: " + (entry.batch ?? string.Empty));
            }

            if (string.IsNullOrWhiteSpace(entry.sourcePath))
            {
                errors.Add("missing source path: " + id);
            }
            else if (!IsWithinRoot(entry.sourcePath, "ArtSource"))
            {
                errors.Add("source path outside ArtSource: " + entry.sourcePath);
            }

            if (string.IsNullOrWhiteSpace(entry.runtimePath))
            {
                errors.Add("missing runtime path: " + id);
            }
            else if (!IsWithinRoot(entry.runtimePath, "Assets/JoseonHunter"))
            {
                errors.Add("runtime path outside Assets/JoseonHunter: " + entry.runtimePath);
            }

            if (!string.Equals(entry.licenseStatus, "approved", StringComparison.Ordinal))
            {
                errors.Add("license other than approved: " + id);
            }

            var pending = string.Equals(entry.approvalStatus, "pending", StringComparison.Ordinal);
            var approved = string.Equals(entry.approvalStatus, "approved", StringComparison.Ordinal);
            if (!pending && !approved)
            {
                errors.Add("approval status other than pending or approved: " + id);
            }

            if (!string.Equals(entry.batch, "audio", StringComparison.Ordinal))
            {
                if (entry.width <= 0 || entry.height <= 0)
                {
                    errors.Add("missing dimensions: " + id);
                }

                if (entry.frameCount <= 0)
                {
                    errors.Add("missing frame count: " + id);
                }

                if (entry.pivotX == 0f && entry.pivotY == 0f)
                {
                    errors.Add("missing pivot: " + id);
                }

                if (entry.pixelsPerUnit <= 0)
                {
                    errors.Add("missing PPU: " + id);
                }
            }

            if (string.IsNullOrWhiteSpace(entry.promptRevision))
            {
                errors.Add("missing prompt revision: " + id);
            }

            if (approved && !ApprovedSha256.IsMatch(entry.sha256 ?? string.Empty))
            {
                errors.Add("missing SHA-256: " + id);
            }
        }

        private static bool IsWithinRoot(string path, string root)
        {
            var normalizedPath = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
            return normalizedPath.StartsWith(root + "/", StringComparison.Ordinal) &&
                !normalizedPath.Contains("/../") && !normalizedPath.EndsWith("/..", StringComparison.Ordinal);
        }
    }
}
