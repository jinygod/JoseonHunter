using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameplayBattlefieldHostTests
    {
        private GameObject root;
        private Texture2D texture;
        private Sprite sprite;
        private Texture2D primaryTexture;
        private Texture2D alternateTexture;
        private Texture2D decorationTexture;
        private Sprite primarySprite;
        private Sprite alternateSprite;
        private Sprite decorationSprite;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Battlefield Host Fixture");
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * .5f, 1f);
            primarySprite = CreateSprite("Primary", Color.red, out primaryTexture);
            alternateSprite = CreateSprite("Alternate", Color.green, out alternateTexture);
            decorationSprite = CreateSprite("Decoration", Color.blue, out decorationTexture);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(primarySprite);
            Object.DestroyImmediate(alternateSprite);
            Object.DestroyImmediate(decorationSprite);
            Object.DestroyImmediate(primaryTexture);
            Object.DestroyImmediate(alternateTexture);
            Object.DestroyImmediate(decorationTexture);
        }

        [Test]
        public void ReconfiguringStageKeepsHostAndRuntimeRootIdentityWhileReplacingGeneratedPresentation()
        {
            var host = root.AddComponent<GameplayBattlefieldHost>();
            var runtimeRoot = new GameObject("Runtime Battlefield").transform;
            runtimeRoot.SetParent(root.transform, false);
            var preview = new GameObject("Authoring Preview");
            preview.transform.SetParent(root.transform, false);
            host.ConfigureAuthoringRoots(runtimeRoot, preview);

            host.ConfigureForStage(
                StageId.GwigokField,
                StageBattlefieldDefinition.Infinite("gwigok_field"),
                null,
                null,
                sprite,
                17);

            var hostId = host.GetEntityId();
            var runtimeRootId = host.RuntimeRoot.GetEntityId();

            host.ConfigureForStage(
                StageId.DokkaebiPass,
                StageBattlefieldDefinition.Bounded(72f, 112f, "dokkaebi_pass"),
                null,
                null,
                sprite,
                23);

            Assert.That(host.GetEntityId(), Is.EqualTo(hostId));
            Assert.That(host.RuntimeRoot.GetEntityId(), Is.EqualTo(runtimeRootId));
            Assert.That(host.IsBuilt, Is.True);
            Assert.That(host.PresentedStageId, Is.EqualTo(StageId.DokkaebiPass));
            Assert.That(host.HasBoundedBounds, Is.True);
            Assert.That(host.BoundedBounds, Is.EqualTo(Rect.MinMaxRect(-36f, -56f, 36f, 56f)));
            Assert.That(runtimeRoot.GetComponentsInChildren<BattlefieldTilePresenter>(true), Is.Empty);
            Assert.That(runtimeRoot.GetComponentsInChildren<BoundedBattlefieldPresenter>(true), Has.Length.EqualTo(1));
        }

        [Test]
        public void InfiniteHostTracksAcrossChunksWithoutCreatingASecondRuntimeRoot()
        {
            var host = root.AddComponent<GameplayBattlefieldHost>();
            var runtimeRoot = new GameObject("Runtime Battlefield").transform;
            runtimeRoot.SetParent(root.transform, false);
            host.ConfigureAuthoringRoots(runtimeRoot, null);

            host.ConfigureForStage(
                StageId.GwigokField,
                StageBattlefieldDefinition.Infinite("gwigok_field"),
                null,
                null,
                sprite,
                17);
            var runtimeRootId = host.RuntimeRoot.GetEntityId();

            host.Track(new Vector2(BattlefieldChunkLayout.ChunkSize * 2f, 0f));

            var presenter = runtimeRoot.GetComponentInChildren<BattlefieldTilePresenter>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.ActiveChunkCount, Is.EqualTo(9));
            Assert.That(host.RuntimeRoot.GetEntityId(), Is.EqualTo(runtimeRootId));
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(root.transform.GetChild(0).GetEntityId(), Is.EqualTo(runtimeRootId));
        }

        [Test]
        public void InfiniteHostUsesConfiguredLegacyFallbackTilesWhenPresentationLibraryIsUnavailable()
        {
            var host = root.AddComponent<GameplayBattlefieldHost>();
            var runtimeRoot = new GameObject("Runtime Battlefield").transform;
            runtimeRoot.SetParent(root.transform, false);
            host.ConfigureAuthoringRoots(runtimeRoot, null);
            host.ConfigureFallbackTiles(primarySprite, alternateSprite, new[] { decorationSprite });

            host.ConfigureForStage(
                StageId.GwigokField,
                StageBattlefieldDefinition.Infinite("gwigok_field"),
                null,
                null,
                sprite,
                17);

            var groundSprites = runtimeRoot.GetComponentsInChildren<SpriteRenderer>()
                .Where(renderer => renderer.gameObject.name == "Ground")
                .Select(renderer => renderer.sprite)
                .ToArray();
            var decorationSprites = runtimeRoot.GetComponentsInChildren<SpriteRenderer>()
                .Where(renderer => renderer.gameObject.name.StartsWith("Decoration"))
                .Select(renderer => renderer.sprite)
                .ToArray();

            Assert.That(groundSprites, Does.Contain(primarySprite));
            Assert.That(groundSprites, Does.Contain(alternateSprite));
            Assert.That(decorationSprites, Is.Not.Empty);
            Assert.That(decorationSprites, Is.All.EqualTo(decorationSprite));
        }

        private static Sprite CreateSprite(string name, Color color, out Texture2D createdTexture)
        {
            createdTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            createdTexture.name = name + " Texture";
            createdTexture.SetPixel(0, 0, color);
            createdTexture.Apply();
            var createdSprite = Sprite.Create(
                createdTexture,
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one * .5f,
                1f);
            createdSprite.name = name;
            return createdSprite;
        }
    }
}
