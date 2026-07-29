using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [Serializable]
    public sealed class CombatMotionSet
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite referenceSprite;
        [SerializeField] private Sprite[] idleFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] moveFrames = Array.Empty<Sprite>();
        [SerializeField] private float idleFramesPerSecond = 3f;
        [SerializeField] private float moveFramesPerSecond = 8f;
        [SerializeField] private MotionWeight weight = MotionWeight.Medium;

        public string Id => id;
        public Sprite ReferenceSprite => referenceSprite;
        public IReadOnlyList<Sprite> IdleFrames => idleFrames;
        public IReadOnlyList<Sprite> MoveFrames => moveFrames;
        public float IdleFramesPerSecond => Mathf.Max(0.1f, idleFramesPerSecond);
        public float MoveFramesPerSecond => Mathf.Max(0.1f, moveFramesPerSecond);
        public MotionWeight Weight => weight;

#if UNITY_EDITOR
        public void Configure(
            string entryId,
            Sprite reference,
            Sprite[] idle,
            Sprite[] move,
            float idleFps,
            float moveFps,
            MotionWeight motionWeight)
        {
            id = entryId;
            referenceSprite = reference;
            idleFrames = idle ?? Array.Empty<Sprite>();
            moveFrames = move ?? Array.Empty<Sprite>();
            idleFramesPerSecond = Mathf.Max(0.1f, idleFps);
            moveFramesPerSecond = Mathf.Max(0.1f, moveFps);
            weight = motionWeight;
        }
#endif

        public Sprite Frame(bool moving, int index)
        {
            var preferred = moving ? moveFrames : idleFrames;
            var fallback = moving ? idleFrames : moveFrames;
            var frames = preferred != null && preferred.Length > 0 ? preferred : fallback;
            if (frames == null || frames.Length == 0) return referenceSprite;
            return frames[Mathf.Abs(index) % frames.Length] != null
                ? frames[Mathf.Abs(index) % frames.Length]
                : referenceSprite;
        }

        public int FrameCount(bool moving)
        {
            var preferred = moving ? moveFrames : idleFrames;
            var fallback = moving ? idleFrames : moveFrames;
            return preferred != null && preferred.Length > 0
                ? preferred.Length
                : (fallback == null ? 0 : fallback.Length);
        }
    }

    [CreateAssetMenu(fileName = "CombatMotionLibrary", menuName = "Joseon Hunter/Combat Motion Library")]
    public sealed class CombatMotionLibrary : ScriptableObject
    {
        [SerializeField] private CombatMotionSet[] sets = Array.Empty<CombatMotionSet>();

        private readonly Dictionary<Sprite, CombatMotionSet> byReference =
            new Dictionary<Sprite, CombatMotionSet>();

        public IReadOnlyList<CombatMotionSet> Sets => sets;

        private void OnEnable() => RebuildLookup();

        public CombatMotionSet Find(Sprite referenceSprite)
        {
            if (referenceSprite == null) return null;
            if (byReference.Count == 0 && sets != null && sets.Length > 0) RebuildLookup();
            return byReference.TryGetValue(referenceSprite, out var result) ? result : null;
        }

#if UNITY_EDITOR
        public void Configure(CombatMotionSet[] motionSets)
        {
            sets = motionSets ?? Array.Empty<CombatMotionSet>();
            RebuildLookup();
        }
#endif

        private void RebuildLookup()
        {
            byReference.Clear();
            if (sets == null) return;
            foreach (var set in sets)
            {
                if (set == null || set.ReferenceSprite == null || byReference.ContainsKey(set.ReferenceSprite)) continue;
                byReference.Add(set.ReferenceSprite, set);
            }
        }
    }
}
