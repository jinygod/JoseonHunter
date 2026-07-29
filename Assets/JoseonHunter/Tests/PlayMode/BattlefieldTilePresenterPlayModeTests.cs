using System.Collections;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class BattlefieldTilePresenterPlayModeTests
    {
        [UnityTest]
        public IEnumerator BattlefieldUsesOneWideQuietWorldAnchoredGroundLayer()
        {
            var root = new GameObject("Battlefield");
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
            var presenter = root.AddComponent<BattlefieldTilePresenter>();

            presenter.Build(sprite, sprite, System.Array.Empty<Sprite>(), sprite);
            yield return null;

            Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            var ground = root.transform.GetChild(0).GetComponent<SpriteRenderer>();
            Assert.That(ground.bounds.size.x, Is.GreaterThanOrEqualTo(60f));
            var minimumColor = Mathf.Min(ground.color.r, Mathf.Min(ground.color.g, ground.color.b));
            Assert.That(ground.color.maxColorComponent - minimumColor, Is.LessThan(0.10f));

            Object.Destroy(root);
            Object.Destroy(sprite);
            Object.Destroy(texture);
        }
    }
}
