using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class BattlefieldTilePresenterPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator GameplayResolvesFolkFieldPresentationLibraryAndTracksNineChunks()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            var library = Resources.Load<ScriptableObject>(
                "Presentation/BattlefieldPresentationLibrary");
            var presenter = Object.FindAnyObjectByType<BattlefieldTilePresenter>();
            var groundSprites = presenter == null
                ? System.Array.Empty<string>()
                : presenter.GetComponentsInChildren<SpriteRenderer>()
                    .Where(renderer => renderer.gameObject.name == "Ground" && renderer.sprite != null)
                    .Select(renderer => renderer.sprite.name)
                    .ToArray();

            Assert.That(library, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.ActiveChunkCount, Is.EqualTo(9));
            Assert.That(groundSprites, Is.Not.Empty);
            Assert.That(groundSprites, Is.All.EqualTo("joseon_folk_field_tile"));
        }

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

        [UnityTest]
        public IEnumerator DokkaebiPassBuildsFiniteFieldAndKeepsSpawnInsideItsOuterBoundary()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 400, 35)));
            data.SelectedStageId = StageId.DokkaebiPass.Value;
            data.SelectedStageDifficulty = "normal";
            MetaGameSession.EnsureExists(new MemoryRepository(data));

            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            var presenter = Object.FindAnyObjectByType<BoundedBattlefieldPresenter>();
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.Bounds.width, Is.EqualTo(72f));
            Assert.That(presenter.Bounds.height, Is.EqualTo(112f));
            Assert.That(presenter.ActiveTileCount, Is.EqualTo(12));

            controller.ConfigureViewportSpawnForTests(0, .5f, 1f, false);
            controller.SpawnEnemyForViewportClearanceTests(false, 0);
            Assert.That(presenter.Bounds.Contains(controller.LastSpawnPositionForTests), Is.True);
            Assert.That(ViewportBounds(Camera.main).Contains(controller.LastSpawnPositionForTests), Is.False);
            Assert.That(presenter.GetComponentsInChildren<SpriteRenderer>()
                    .Where(renderer => renderer.gameObject.name.Contains("Boundary"))
                    .Select(renderer => renderer.drawMode),
                Is.All.EqualTo(SpriteDrawMode.Simple));
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

        private static Rect ViewportBounds(Camera camera)
        {
            var bottomLeft = camera.ViewportToWorldPoint(Vector3.zero);
            var topRight = camera.ViewportToWorldPoint(Vector3.one);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                stored = data.Copy();
                return new SaveResult(true, SaveError.None);
            }
        }
    }
}
