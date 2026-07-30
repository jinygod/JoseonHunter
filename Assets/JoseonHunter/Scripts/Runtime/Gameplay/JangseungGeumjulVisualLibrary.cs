using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [CreateAssetMenu(menuName = "Joseon Hunter/Presentation/Jangseung Geumjul Visual Library")]
    public sealed class JangseungGeumjulVisualLibrary : ScriptableObject
    {
        [SerializeField] private Texture2D geumjulRopeTexture;
        [SerializeField] private Sprite geumjulAnchor;
        [SerializeField] private Sprite[] geumjulKnotVariants = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] geumjulClosureFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] jangseungDustFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] jangseungCrossingFrames = Array.Empty<Sprite>();

        public Texture2D GeumjulRopeTexture => geumjulRopeTexture;
        public Sprite GeumjulAnchor => geumjulAnchor;
        public Sprite[] GeumjulKnotVariants => geumjulKnotVariants;
        public Sprite[] GeumjulClosureFrames => geumjulClosureFrames;
        public Sprite[] JangseungDustFrames => jangseungDustFrames;
        public Sprite[] JangseungCrossingFrames => jangseungCrossingFrames;

#if UNITY_EDITOR
        public void ConfigureForImport(
            Texture2D ropeTexture,
            Sprite anchor,
            Sprite[] knotVariants,
            Sprite[] closureFrames,
            Sprite[] dustFrames,
            Sprite[] crossingFrames)
        {
            geumjulRopeTexture = ropeTexture;
            geumjulAnchor = anchor;
            geumjulKnotVariants = knotVariants ?? Array.Empty<Sprite>();
            geumjulClosureFrames = closureFrames ?? Array.Empty<Sprite>();
            jangseungDustFrames = dustFrames ?? Array.Empty<Sprite>();
            jangseungCrossingFrames = crossingFrames ?? Array.Empty<Sprite>();
        }
#endif
    }
}
