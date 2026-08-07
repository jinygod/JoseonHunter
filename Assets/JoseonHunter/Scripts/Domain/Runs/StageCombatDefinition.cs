using System;

namespace JoseonHunter.Domain.Runs
{
    public sealed class StageCombatDefinition
    {
        public StageCombatDefinition(
            StageId stageId,
            StageWaveProfile waves,
            StageBattlefieldDefinition battlefield,
            bool presentationReady)
        {
            StageId = stageId;
            Waves = waves ?? throw new ArgumentNullException(nameof(waves));
            Battlefield = battlefield;
            PresentationReady = presentationReady;
        }

        public StageId StageId { get; }
        public StageWaveProfile Waves { get; }
        public StageBattlefieldDefinition Battlefield { get; }
        public bool PresentationReady { get; }
    }
}
