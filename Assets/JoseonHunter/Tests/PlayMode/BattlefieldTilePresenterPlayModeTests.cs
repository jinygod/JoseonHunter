using System.Collections;
using System.Linq;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class BattlefieldTilePresenterPlayModeTests
    {
        [UnityTest]
        public IEnumerator InfiniteBattlefieldKeepsNineChunksAndDoesNotRebuildInsideOneCoordinate()
        {
            var fixture = CreateFixture();

            fixture.Presenter.BuildInfinite(
                fixture.Sprite,
                fixture.Sprite,
                System.Array.Empty<Sprite>(),
                fixture.Sprite,
                0x4A4F5345);
            var initialRebuilds = fixture.Presenter.RebuildCount;
            fixture.Presenter.Track(new Vector2(15f, 15f));
            yield return null;

            Assert.That(fixture.Presenter.ActiveChunkCount, Is.EqualTo(9));
            Assert.That(fixture.Root.transform.childCount, Is.EqualTo(9));
            Assert.That(fixture.Presenter.RebuildCount, Is.EqualTo(initialRebuilds));
            Assert.That(fixture.Presenter.ChunkCoordinates.Distinct().Count(), Is.EqualTo(9));

            fixture.Dispose();
        }

        [UnityTest]
        public IEnumerator CrossingCoordinatesRecyclesExistingObjectsAndReturningRestoresDecorationSignature()
        {
            var fixture = CreateFixture();
            fixture.Presenter.BuildInfinite(
                fixture.Sprite,
                fixture.Sprite,
                new[] { fixture.Sprite },
                fixture.Sprite,
                0x4A4F5345);
            var originalObjects = Enumerable.Range(0, fixture.Root.transform.childCount)
                .Select(index => fixture.Root.transform.GetChild(index).gameObject)
                .ToArray();
            var before = fixture.Presenter.DecorationSignature(Vector2Int.zero);

            fixture.Presenter.Track(new Vector2(96f, 0f));
            fixture.Presenter.Track(Vector2.zero);
            yield return null;

            Assert.That(fixture.Presenter.ActiveChunkCount, Is.EqualTo(9));
            Assert.That(fixture.Presenter.DecorationSignature(Vector2Int.zero), Is.EqualTo(before));
            Assert.That(Enumerable.Range(0, fixture.Root.transform.childCount)
                .Select(index => fixture.Root.transform.GetChild(index).gameObject),
                Is.EquivalentTo(originalObjects));

            fixture.Dispose();
        }

        private static Fixture CreateFixture()
        {
            var root = new GameObject("Battlefield");
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
            return new Fixture(root, texture, sprite, root.AddComponent<BattlefieldTilePresenter>());
        }

        private readonly struct Fixture
        {
            public Fixture(
                GameObject root,
                Texture2D texture,
                Sprite sprite,
                BattlefieldTilePresenter presenter)
            {
                Root = root;
                Texture = texture;
                Sprite = sprite;
                Presenter = presenter;
            }

            public GameObject Root { get; }
            public Texture2D Texture { get; }
            public Sprite Sprite { get; }
            public BattlefieldTilePresenter Presenter { get; }

            public void Dispose()
            {
                Object.Destroy(Root);
                Object.Destroy(Sprite);
                Object.Destroy(Texture);
            }
        }
    }
}
