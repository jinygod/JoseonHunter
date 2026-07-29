using System.Collections;
using System.Linq;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponTransientVisualPoolPlayModeTests
    {
        [UnityTest]
        public IEnumerator ExpiredVisual_IsReusedWithoutGrowingCreatedCount()
        {
            var root = new GameObject("Weapon Visual Test Root").transform;
            var pool = new WeaponTransientVisualPool(root);
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            texture.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 255), 64).ToArray());
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 32f);

            pool.Play(sprite, Vector3.zero, Quaternion.identity, Vector3.one, Color.white, .05f, 10);
            pool.Tick(.06f);
            var created = pool.CreatedCount;
            pool.Play(sprite, Vector3.zero, Quaternion.identity, Vector3.one, Color.white, .05f, 10);

            Assert.That(pool.CreatedCount, Is.EqualTo(created));

            pool.Dispose();
            Object.Destroy(sprite);
            Object.Destroy(texture);
            Object.Destroy(root.gameObject);
            yield return null;
        }
    }
}
