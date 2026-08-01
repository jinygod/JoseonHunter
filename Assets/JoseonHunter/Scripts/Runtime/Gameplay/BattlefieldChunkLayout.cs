using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class BattlefieldChunkLayout
    {
        public const float ChunkSize = 32f;
        public const int ActiveChunkCount = 9;

        public static Vector2Int CoordinateAt(Vector2 worldPosition) => new(
            Mathf.FloorToInt(worldPosition.x / ChunkSize),
            Mathf.FloorToInt(worldPosition.y / ChunkSize));

        public static void FillRequired(Vector2Int center, Vector2Int[] output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (output.Length < ActiveChunkCount)
                throw new ArgumentException("A 3-by-3 battlefield requires a nine-entry buffer.", nameof(output));

            var index = 0;
            for (var y = -1; y <= 1; y++)
            for (var x = -1; x <= 1; x++)
                output[index++] = new Vector2Int(center.x + x, center.y + y);
        }

        public static int DecorationSeed(Vector2Int coordinate, int battlefieldSeed)
        {
            unchecked
            {
                var hash = battlefieldSeed;
                hash = (hash * 397) ^ coordinate.x;
                hash = (hash * 397) ^ coordinate.y;
                hash ^= (coordinate.x * 73856093) ^ (coordinate.y * 19349663);
                return hash;
            }
        }

        public static Vector3 WorldCenter(Vector2Int coordinate) => new(
            coordinate.x * ChunkSize + ChunkSize * .5f,
            coordinate.y * ChunkSize + ChunkSize * .5f,
            0f);
    }
}
