using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Runs;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [Serializable]
    public struct StagePresentationSpriteEntry
    {
        [SerializeField] private string contentId;
        [SerializeField] private Sprite sprite;

        public StagePresentationSpriteEntry(string contentId, Sprite sprite)
        {
            this.contentId = contentId ?? string.Empty;
            this.sprite = sprite;
        }

        public string ContentId => contentId;
        public Sprite Sprite => sprite;
    }

    [Serializable]
    public struct StagePresentationEntry
    {
        [SerializeField] private string stageId;
        [SerializeField] private Sprite ground;
        [SerializeField] private Sprite alternateGround;
        [SerializeField] private Sprite[] decorations;

        public StagePresentationEntry(StageId stageId, Sprite ground, Sprite alternateGround, Sprite[] decorations)
        {
            this.stageId = stageId.Value;
            this.ground = ground;
            this.alternateGround = alternateGround;
            this.decorations = decorations ?? Array.Empty<Sprite>();
        }

        public string StageId => stageId;
        public Sprite Ground => ground;
        public Sprite AlternateGround => alternateGround;
        public IReadOnlyList<Sprite> Decorations => decorations ?? Array.Empty<Sprite>();
    }

    public sealed class StagePresentationCatalog : ScriptableObject
    {
        [SerializeField] private StagePresentationSpriteEntry[] sprites =
            Array.Empty<StagePresentationSpriteEntry>();
        [SerializeField] private StagePresentationEntry[] stages =
            Array.Empty<StagePresentationEntry>();

        public bool TryGetSprite(string contentId, out Sprite sprite)
        {
            for (var index = 0; index < sprites.Length; index++)
            {
                if (!string.Equals(sprites[index].ContentId, contentId, StringComparison.Ordinal)) continue;
                sprite = sprites[index].Sprite;
                return sprite != null;
            }
            sprite = null;
            return false;
        }

        public bool TryGetStage(StageId stageId, out StagePresentationEntry entry)
        {
            for (var index = 0; index < stages.Length; index++)
            {
                if (!string.Equals(stages[index].StageId, stageId.Value, StringComparison.Ordinal)) continue;
                entry = stages[index];
                return entry.Ground != null;
            }
            entry = default;
            return false;
        }

        public void Configure(StagePresentationSpriteEntry[] spriteEntries, StagePresentationEntry[] stageEntries)
        {
            sprites = spriteEntries ?? Array.Empty<StagePresentationSpriteEntry>();
            stages = stageEntries ?? Array.Empty<StagePresentationEntry>();
        }
    }
}
