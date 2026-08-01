using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class BattlefieldChunkView : MonoBehaviour
    {
        private const int DecorationCapacity = 4;
        [SerializeField] private SpriteRenderer ground;
        [SerializeField] private SpriteRenderer[] decorations = new SpriteRenderer[DecorationCapacity];

        public Vector2Int Coordinate { get; private set; }
        public int DecorationSignature { get; private set; }

        public void EnsureStructure()
        {
            if (decorations == null || decorations.Length != DecorationCapacity)
                System.Array.Resize(ref decorations, DecorationCapacity);
            if (ground == null)
            {
                var groundObject = new GameObject("Ground");
                groundObject.transform.SetParent(transform, false);
                ground = groundObject.AddComponent<SpriteRenderer>();
                ground.sortingOrder = -20;
            }

            for (var index = 0; index < decorations.Length; index++)
            {
                if (decorations[index] != null) continue;
                var decorationObject = new GameObject($"Decoration {index + 1}");
                decorationObject.transform.SetParent(transform, false);
                decorations[index] = decorationObject.AddComponent<SpriteRenderer>();
                decorations[index].sortingOrder = -18;
            }
        }

        public void Assign(
            Vector2Int coordinate,
            Sprite primaryTile,
            Sprite alternateTile,
            IReadOnlyList<Sprite> decorationSprites,
            Sprite fallbackSprite,
            int battlefieldSeed)
        {
            EnsureStructure();
            Coordinate = coordinate;
            transform.position = BattlefieldChunkLayout.WorldCenter(coordinate);

            var useAlternate = alternateTile != null && ((coordinate.x + coordinate.y) & 1) != 0;
            var tile = useAlternate ? alternateTile : primaryTile;
            ConfigureGround(tile != null ? tile : fallbackSprite, tile == null);

            var random = new System.Random(BattlefieldChunkLayout.DecorationSeed(coordinate, battlefieldSeed));
            var signature = 17;
            for (var index = 0; index < decorations.Length; index++)
            {
                var renderer = decorations[index];
                var hasSprite = decorationSprites != null && decorationSprites.Count > 0;
                renderer.gameObject.SetActive(hasSprite);
                if (!hasSprite)
                {
                    renderer.sprite = null;
                    continue;
                }

                var spriteIndex = random.Next(0, decorationSprites.Count);
                var positionX = Mathf.Lerp(-13.5f, 13.5f, (float)random.NextDouble());
                var positionY = Mathf.Lerp(-13.5f, 13.5f, (float)random.NextDouble());
                var quarterTurn = random.Next(0, 4);
                var flipX = random.Next(0, 2) == 0;

                renderer.sprite = decorationSprites[spriteIndex];
                renderer.transform.localPosition = new Vector3(positionX, positionY, 0f);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, quarterTurn * 90f);
                renderer.transform.localScale = Vector3.one;
                renderer.flipX = flipX;
                renderer.color = new Color(0.56f, 0.59f, 0.52f, .16f);

                unchecked
                {
                    signature = signature * 31 + spriteIndex;
                    signature = signature * 31 + Mathf.RoundToInt(positionX * 100f);
                    signature = signature * 31 + Mathf.RoundToInt(positionY * 100f);
                    signature = signature * 31 + quarterTurn;
                    signature = signature * 31 + (flipX ? 1 : 0);
                }
            }

            DecorationSignature = signature;
            gameObject.name = $"Battlefield Chunk {coordinate.x}, {coordinate.y}";
        }

        private void ConfigureGround(Sprite sprite, bool fallback)
        {
            ground.sprite = sprite;
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localRotation = Quaternion.identity;
            ground.transform.localScale = Vector3.one;
            ground.color = fallback
                ? new Color(0.34f, 0.39f, 0.33f)
                : new Color(0.72f, 0.74f, 0.70f);

            if (sprite == null) return;
            if (sprite.rect.width >= 128f || sprite.rect.height >= 128f)
            {
                ground.drawMode = SpriteDrawMode.Simple;
                var spriteSize = sprite.bounds.size;
                ground.transform.localScale = new Vector3(
                    BattlefieldChunkLayout.ChunkSize / Mathf.Max(.01f, spriteSize.x),
                    BattlefieldChunkLayout.ChunkSize / Mathf.Max(.01f, spriteSize.y),
                    1f);
            }
            else
            {
                ground.drawMode = SpriteDrawMode.Tiled;
                ground.size = Vector2.one * BattlefieldChunkLayout.ChunkSize;
            }
        }
    }
}
