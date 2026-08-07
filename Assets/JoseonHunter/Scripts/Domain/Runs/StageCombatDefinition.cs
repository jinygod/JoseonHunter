using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Runs
{
    public sealed class StageCombatDefinition
    {
        public StageCombatDefinition(
            StageId stageId,
            StageWaveProfile waves,
            StageBattlefieldDefinition battlefield,
            IReadOnlyList<StageBossDefinition> bosses,
            bool presentationReady)
        {
            StageId = stageId;
            Waves = waves ?? throw new ArgumentNullException(nameof(waves));
            Battlefield = battlefield;
            Bosses = bosses ?? throw new ArgumentNullException(nameof(bosses));
            PresentationReady = presentationReady;
        }

        public StageId StageId { get; }
        public StageWaveProfile Waves { get; }
        public StageBattlefieldDefinition Battlefield { get; }
        public IReadOnlyList<StageBossDefinition> Bosses { get; }
        public bool PresentationReady { get; }
    }
}
