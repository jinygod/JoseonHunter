using JoseonHunter.Domain.Runs;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class BoundedBattlefieldPresenter : MonoBehaviour
    {
        private const float TileSize = BattlefieldChunkLayout.ChunkSize;
        private StageBattlefieldDefinition definition;

        public Rect Bounds => definition.IsBounded
            ? Rect.MinMaxRect(-definition.Width * .5f, -definition.Height * .5f,
                definition.Width * .5f, definition.Height * .5f)
            : default;
        public int ActiveTileCount { get; private set; }

        public void Configure(
            StageBattlefieldDefinition battlefield,
            BattlefieldPresentationLibrary library,
            Sprite fallbackSprite,
            int seed)
        {
            Configure(battlefield,
                library != null ? library.ChunkPrefab : null,
                library != null ? library.GroundTile : null,
                library != null ? library.AlternateGroundTile : null,
                library != null ? library.Decorations : System.Array.Empty<Sprite>(),
                fallbackSprite,
                seed);
        }

        public void Configure(
            StageBattlefieldDefinition battlefield,
            BattlefieldChunkView prefab,
            Sprite primary,
            Sprite alternate,
            IReadOnlyList<Sprite> decorations,
            Sprite fallbackSprite,
            int seed)
        {
            if (!battlefield.IsBounded)
                throw new System.ArgumentException("A bounded battlefield definition is required.", nameof(battlefield));

            for (var index = transform.childCount - 1; index >= 0; index--)
                Destroy(transform.GetChild(index).gameObject);

            definition = battlefield;
            ActiveTileCount = 0;
            var columns = Mathf.CeilToInt(battlefield.Width / TileSize);
            var rows = Mathf.CeilToInt(battlefield.Height / TileSize);
            var startX = -(columns - 1) * TileSize * .5f;
            var startY = -(rows - 1) * TileSize * .5f;

            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                BattlefieldChunkView chunk;
                if (prefab != null)
                {
                    chunk = Instantiate(prefab, transform);
                    chunk.gameObject.SetActive(true);
                }
                else
                {
                    var chunkObject = new GameObject("Bounded Battlefield Tile");
                    chunkObject.transform.SetParent(transform, false);
                    chunk = chunkObject.AddComponent<BattlefieldChunkView>();
                }

                var coordinate = new Vector2Int(column, row);
                chunk.Assign(coordinate, primary, alternate, decorations, fallbackSprite, seed);
                chunk.transform.SetParent(transform, true);
                chunk.transform.localPosition = new Vector3(
                    startX + column * TileSize,
                    startY + row * TileSize,
                    0f);
                ActiveTileCount++;
            }

            BuildBoundary("North Boundary", new Vector2(0f, battlefield.Height * .5f),
                new Vector2(battlefield.Width, .4f), fallbackSprite);
            BuildBoundary("South Boundary", new Vector2(0f, -battlefield.Height * .5f),
                new Vector2(battlefield.Width, .4f), fallbackSprite);
            BuildBoundary("East Boundary", new Vector2(battlefield.Width * .5f, 0f),
                new Vector2(.4f, battlefield.Height), fallbackSprite);
            BuildBoundary("West Boundary", new Vector2(-battlefield.Width * .5f, 0f),
                new Vector2(.4f, battlefield.Height), fallbackSprite);
        }

        private void BuildBoundary(string objectName, Vector2 position, Vector2 size, Sprite sprite)
        {
            var boundary = new GameObject(objectName);
            boundary.transform.SetParent(transform, false);
            boundary.transform.localPosition = position;
            boundary.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = boundary.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = new Color(.08f, .12f, .11f, 1f);
            renderer.sortingOrder = -16;
            var collider = boundary.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }
    }
}
