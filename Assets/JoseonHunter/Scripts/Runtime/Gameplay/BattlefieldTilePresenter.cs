using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class BattlefieldTilePresenter : MonoBehaviour
    {
        private readonly BattlefieldChunkView[] chunks =
            new BattlefieldChunkView[BattlefieldChunkLayout.ActiveChunkCount];
        private readonly Vector2Int[] coordinates =
            new Vector2Int[BattlefieldChunkLayout.ActiveChunkCount];
        private readonly Vector2Int[] required =
            new Vector2Int[BattlefieldChunkLayout.ActiveChunkCount];
        private readonly bool[] retainedChunks =
            new bool[BattlefieldChunkLayout.ActiveChunkCount];
        private readonly bool[] assignedRequirements =
            new bool[BattlefieldChunkLayout.ActiveChunkCount];

        private Sprite primaryTile;
        private Sprite alternateTile;
        private IReadOnlyList<Sprite> decals;
        private Sprite fallbackSprite;
        private int battlefieldSeed;
        private Vector2Int centerCoordinate;
        private bool built;

        public int ActiveChunkCount => built ? chunks.Length : 0;
        public IReadOnlyList<Vector2Int> ChunkCoordinates => coordinates;
        public int RebuildCount { get; private set; }

        public void Build(
            Sprite primaryTile,
            Sprite alternateTile,
            IReadOnlyList<Sprite> decals,
            Sprite fallbackSprite)
        {
            BuildInfinite(primaryTile, alternateTile, decals, fallbackSprite, 0x4A4F5345);
        }

        public void BuildInfinite(
            Sprite primaryTile,
            Sprite alternateTile,
            IReadOnlyList<Sprite> decals,
            Sprite fallbackSprite,
            int battlefieldSeed)
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }

            this.primaryTile = primaryTile;
            this.alternateTile = alternateTile;
            this.decals = decals;
            this.fallbackSprite = fallbackSprite;
            this.battlefieldSeed = battlefieldSeed;
            RebuildCount = 0;
            centerCoordinate = Vector2Int.zero;
            BattlefieldChunkLayout.FillRequired(centerCoordinate, required);

            for (var index = 0; index < chunks.Length; index++)
            {
                var chunkObject = new GameObject("Battlefield Chunk");
                chunkObject.transform.SetParent(transform, false);
                chunks[index] = chunkObject.AddComponent<BattlefieldChunkView>();
                Assign(index, required[index]);
            }

            built = true;
        }

        public void Track(Vector2 playerPosition)
        {
            if (!built) return;
            var nextCenter = BattlefieldChunkLayout.CoordinateAt(playerPosition);
            if (nextCenter == centerCoordinate) return;

            centerCoordinate = nextCenter;
            BattlefieldChunkLayout.FillRequired(centerCoordinate, required);
            System.Array.Clear(retainedChunks, 0, retainedChunks.Length);
            System.Array.Clear(assignedRequirements, 0, assignedRequirements.Length);

            for (var requirementIndex = 0; requirementIndex < required.Length; requirementIndex++)
            {
                for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    if (retainedChunks[chunkIndex] || coordinates[chunkIndex] != required[requirementIndex])
                        continue;
                    retainedChunks[chunkIndex] = true;
                    assignedRequirements[requirementIndex] = true;
                    break;
                }
            }

            var reusableIndex = 0;
            for (var requirementIndex = 0; requirementIndex < required.Length; requirementIndex++)
            {
                if (assignedRequirements[requirementIndex]) continue;
                while (reusableIndex < chunks.Length && retainedChunks[reusableIndex]) reusableIndex++;
                if (reusableIndex >= chunks.Length) break;
                Assign(reusableIndex, required[requirementIndex]);
                retainedChunks[reusableIndex] = true;
                reusableIndex++;
            }
        }

        public int DecorationSignature(Vector2Int coordinate)
        {
            for (var index = 0; index < chunks.Length; index++)
            {
                if (coordinates[index] == coordinate && chunks[index] != null)
                    return chunks[index].DecorationSignature;
            }

            return int.MinValue;
        }

        private void Assign(int index, Vector2Int coordinate)
        {
            coordinates[index] = coordinate;
            chunks[index].Assign(
                coordinate,
                primaryTile,
                alternateTile,
                decals,
                fallbackSprite,
                battlefieldSeed);
            RebuildCount++;
        }
    }
}
