using System;

namespace JoseonHunter.Domain.Runs
{
    public sealed class StageCombatDefinition
    {
        public StageCombatDefinition(StageId stageId, StageWaveProfile waves, bool presentationReady)
        {
            StageId = stageId;
            Waves = waves ?? throw new ArgumentNullException(nameof(waves));
            PresentationReady = presentationReady;
        }

        public StageId StageId { get; }
        public StageWaveProfile Waves { get; }
        public bool PresentationReady { get; }
    }
}
