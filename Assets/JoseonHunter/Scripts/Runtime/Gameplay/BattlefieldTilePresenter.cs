using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class BattlefieldTilePresenter : MonoBehaviour
    {
        private static readonly Vector2 FieldSize = new Vector2(22f, 32f);

        public void Build(
            Sprite primaryTile,
            Sprite alternateTile,
            IReadOnlyList<Sprite> decals,
            Sprite fallbackSprite)
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }

            if (primaryTile == null)
            {
                var fallback = CreateRenderer("Occult Ground", fallbackSprite, -20);
                fallback.transform.localScale = new Vector3(FieldSize.x, FieldSize.y, 1f);
                fallback.color = new Color(0.12f, 0.105f, 0.11f);
                return;
            }

            var ground = CreateRenderer("Haunted Soil", primaryTile, -20);
            if (primaryTile.rect.width > 128f || primaryTile.rect.height > 128f)
            {
                ground.drawMode = SpriteDrawMode.Simple;
                var spriteSize = primaryTile.bounds.size;
                ground.transform.localScale = new Vector3(
                    FieldSize.x / spriteSize.x,
                    FieldSize.y / spriteSize.y,
                    1f);
            }
            else
            {
                ground.drawMode = SpriteDrawMode.Tiled;
                ground.size = FieldSize;
            }
            ground.color = new Color(0.72f, 0.72f, 0.76f);

            if (alternateTile != null && primaryTile.rect.width <= 128f)
            {
                var alternate = CreateRenderer("Hanji Ash Variation", alternateTile, -19);
                alternate.drawMode = SpriteDrawMode.Tiled;
                alternate.size = FieldSize;
                alternate.color = new Color(0.42f, 0.38f, 0.45f, 0.22f);
            }

            if (decals == null || decals.Count == 0)
            {
                return;
            }

            var random = new System.Random(0x4A4F5345);
            for (var index = 0; index < 24; index++)
            {
                var sprite = decals[index % decals.Count];
                if (sprite == null) continue;
                var renderer = CreateRenderer("Battlefield Decal", sprite, -18);
                renderer.transform.localPosition = new Vector3(
                    Mathf.Lerp(-10.2f, 10.2f, (float)random.NextDouble()),
                    Mathf.Lerp(-15.2f, 15.2f, (float)random.NextDouble()),
                    0f);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, random.Next(0, 4) * 90f);
                renderer.flipX = random.Next(0, 2) == 0;
                renderer.color = new Color(0.82f, 0.78f, 0.82f, 0.72f);
            }
        }

        private SpriteRenderer CreateRenderer(string objectName, Sprite sprite, int sortingOrder)
        {
            var result = new GameObject(objectName);
            result.transform.SetParent(transform, false);
            var renderer = result.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
