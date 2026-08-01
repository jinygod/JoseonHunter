using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [CreateAssetMenu(
        fileName = "BattlefieldPresentationLibrary",
        menuName = "JoseonHunter/Presentation/Battlefield Library")]
    public sealed class BattlefieldPresentationLibrary : ScriptableObject
    {
        [SerializeField] private BattlefieldChunkView chunkPrefab;
        [SerializeField] private Sprite groundTile;
        [SerializeField] private Sprite alternateGroundTile;
        [SerializeField] private Sprite[] decorations;

        public BattlefieldChunkView ChunkPrefab => chunkPrefab;
        public Sprite GroundTile => groundTile;
        public Sprite AlternateGroundTile => alternateGroundTile;
        public IReadOnlyList<Sprite> Decorations => decorations;

        public void Configure(
            BattlefieldChunkView prefab,
            Sprite ground,
            Sprite alternate,
            Sprite[] decorationSprites)
        {
            chunkPrefab = prefab;
            groundTile = ground;
            alternateGroundTile = alternate;
            decorations = decorationSprites ?? System.Array.Empty<Sprite>();
        }
    }
}
