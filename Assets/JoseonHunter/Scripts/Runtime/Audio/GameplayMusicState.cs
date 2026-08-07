using JoseonHunter.Domain.Runs;

namespace JoseonHunter.Runtime.Audio
{
    public sealed class GameplayMusicState
    {
        private CombatMusicPhase phase;
        private int activeMidBosses;
        private bool finalBossActive;
        private bool ended = true;
        private StageId stageId = StageId.GwigokField;

        public GameMusicRole CurrentRole
        {
            get
            {
                if (ended) return GameMusicRole.None;
                if (finalBossActive) return GameMusicRole.FinalBoss;
                if (activeMidBosses > 0) return GameMusicRole.MidBoss;
                if (stageId.Equals(StageId.DokkaebiPass)) return GameMusicRole.DokkaebiPass;
                if (stageId.Equals(StageId.MoonlitTomb)) return GameMusicRole.MoonlitTomb;
                return GameMusicPolicy.RoleFor(phase);
            }
        }

        public void Reset() => Reset(StageId.GwigokField);

        public void Reset(StageId nextStageId)
        {
            stageId = nextStageId;
            phase = CombatMusicPhase.Early;
            activeMidBosses = 0;
            finalBossActive = false;
            ended = false;
        }

        public void SetPhase(CombatMusicPhase nextPhase)
        {
            if (ended) return;
            phase = nextPhase;
        }

        public void EnterMidBoss()
        {
            if (ended) return;
            activeMidBosses++;
        }

        public void ExitMidBoss()
        {
            if (ended) return;
            if (activeMidBosses > 0) activeMidBosses--;
        }

        public void EnterFinalBoss()
        {
            if (ended) return;
            finalBossActive = true;
        }

        public void EndRun()
        {
            ended = true;
            activeMidBosses = 0;
            finalBossActive = false;
        }
    }
}
