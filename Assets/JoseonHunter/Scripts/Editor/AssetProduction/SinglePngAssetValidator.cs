using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public static class SinglePngAssetValidator
    {
        private const byte OpaqueThreshold = 16;

        public static IReadOnlyList<string> Validate(string assetPath)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(assetPath) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("asset must be one PNG file");
                return issues;
            }

            var absolutePath = Path.IsPathRooted(assetPath)
                ? assetPath
                : Path.GetFullPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                issues.Add("asset file does not exist");
                return issues;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath), false))
                {
                    issues.Add("asset is not a readable PNG");
                    return issues;
                }

                var pixels = texture.GetPixels32();
                var componentSizes = FindOpaqueComponents(pixels, texture.width, texture.height);
                if (componentSizes.Count == 0)
                {
                    issues.Add("asset contains no opaque pixels");
                    return issues;
                }

                componentSizes.Sort((left, right) => right.CompareTo(left));
                var principalSize = componentSizes[0];
                var independentIslandThreshold = Math.Max(12, Mathf.CeilToInt(principalSize * 0.15f));
                for (var index = 1; index < componentSizes.Count; index++)
                {
                    if (componentSizes[index] >= independentIslandThreshold)
                    {
                        issues.Add("multiple independent asset islands");
                        break;
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            return issues;
        }

        private static List<int> FindOpaqueComponents(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var components = new List<int>();
            var queue = new Queue<int>();
            for (var start = 0; start < pixels.Length; start++)
            {
                if (visited[start] || pixels[start].a < OpaqueThreshold)
                {
                    continue;
                }

                visited[start] = true;
                queue.Enqueue(start);
                var size = 0;
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    size++;
                    var x = current % width;
                    var y = current / width;
                    TryEnqueue(x - 1, y, width, height, pixels, visited, queue);
                    TryEnqueue(x + 1, y, width, height, pixels, visited, queue);
                    TryEnqueue(x, y - 1, width, height, pixels, visited, queue);
                    TryEnqueue(x, y + 1, width, height, pixels, visited, queue);
                }

                components.Add(size);
            }

            return components;
        }

        private static void TryEnqueue(
            int x,
            int y,
            int width,
            int height,
            Color32[] pixels,
            bool[] visited,
            Queue<int> queue)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            var index = y * width + x;
            if (visited[index] || pixels[index].a < OpaqueThreshold)
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue(index);
        }
    }
}
