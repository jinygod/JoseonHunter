using JoseonHunter.Domain.Runs;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class GameplayBattlefieldHost : MonoBehaviour
    {
        [SerializeField] private Transform runtimeRoot;
        [SerializeField] private GameObject authoringPreviewRoot;
        private GameObject generatedPresentation;
        private BattlefieldTilePresenter battlefieldPresenter;
        private BoundedBattlefieldPresenter boundedBattlefieldPresenter;

        public StageId PresentedStageId { get; private set; }
        public bool IsBuilt { get; private set; }
        public bool HasBoundedBounds => boundedBattlefieldPresenter != null;
        public Rect BoundedBounds => HasBoundedBounds ? boundedBattlefieldPresenter.Bounds : default;
        public Transform RuntimeRoot => runtimeRoot;

        public void ConfigureAuthoringRoots(Transform nextRuntimeRoot, GameObject nextAuthoringPreviewRoot)
        {
            runtimeRoot = nextRuntimeRoot;
            authoringPreviewRoot = nextAuthoringPreviewRoot;
        }

        public void ConfigureForStage(
            StageId stageId,
            StageBattlefieldDefinition battlefield,
            StagePresentationCatalog stagePresentationCatalog,
            BattlefieldPresentationLibrary presentation,
            Sprite fallbackSprite,
            int seed)
        {
            EnsureRuntimeRoot();
            if (Application.isPlaying && authoringPreviewRoot != null)
                authoringPreviewRoot.SetActive(false);

            DestroyGeneratedPresentation();
            generatedPresentation = new GameObject("Generated Battlefield Presentation");
            generatedPresentation.transform.SetParent(runtimeRoot, false);
            battlefieldPresenter = null;
            boundedBattlefieldPresenter = null;

            if (battlefield.IsBounded)
            {
                boundedBattlefieldPresenter = generatedPresentation.AddComponent<BoundedBattlefieldPresenter>();
                if (stagePresentationCatalog != null && stagePresentationCatalog.TryGetStage(stageId, out var stagePresentation))
                {
                    boundedBattlefieldPresenter.Configure(
                        battlefield,
                        presentation != null ? presentation.ChunkPrefab : null,
                        stagePresentation.Ground,
                        stagePresentation.AlternateGround,
                        stagePresentation.Decorations,
                        fallbackSprite,
                        seed);
                }
                else
                {
                    boundedBattlefieldPresenter.Configure(battlefield, presentation, fallbackSprite, seed);
                }
            }
            else
            {
                battlefieldPresenter = generatedPresentation.AddComponent<BattlefieldTilePresenter>();
                if (presentation != null && presentation.GroundTile != null)
                {
                    battlefieldPresenter.BuildInfinite(
                        presentation.ChunkPrefab,
                        presentation.GroundTile,
                        presentation.AlternateGroundTile,
                        presentation.Decorations,
                        fallbackSprite,
                        seed);
                }
                else
                {
                    battlefieldPresenter.BuildInfinite(
                        fallbackSprite,
                        fallbackSprite,
                        System.Array.Empty<Sprite>(),
                        fallbackSprite,
                        seed);
                }
            }

            PresentedStageId = stageId;
            IsBuilt = true;
        }

        public void Track(Vector2 playerPosition)
        {
            battlefieldPresenter?.Track(playerPosition);
        }

        private void EnsureRuntimeRoot()
        {
            if (runtimeRoot != null) return;
            runtimeRoot = new GameObject("Runtime Battlefield").transform;
            runtimeRoot.SetParent(transform, false);
        }

        private void DestroyGeneratedPresentation()
        {
            if (generatedPresentation == null) return;
            generatedPresentation.SetActive(false);
            if (Application.isPlaying)
                Destroy(generatedPresentation);
            else
                DestroyImmediate(generatedPresentation);
            generatedPresentation = null;
        }
    }
}
