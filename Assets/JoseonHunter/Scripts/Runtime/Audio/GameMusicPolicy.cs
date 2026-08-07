using UnityEngine;

namespace JoseonHunter.Runtime.Audio
{
    public static class GameMusicPolicy
    {
        private const float MidPhaseStartsAt = 300f;
        private const float LatePhaseStartsAt = 600f;

        public static CombatMusicPhase PhaseAt(float elapsedSeconds)
        {
            var elapsed = Mathf.Max(0f, elapsedSeconds);
            if (elapsed >= LatePhaseStartsAt) return CombatMusicPhase.Late;
            return elapsed >= MidPhaseStartsAt ? CombatMusicPhase.Mid : CombatMusicPhase.Early;
        }

        public static GameMusicRole RoleFor(CombatMusicPhase phase) => phase switch
        {
            CombatMusicPhase.Mid => GameMusicRole.CombatMid,
            CombatMusicPhase.Late => GameMusicRole.CombatLate,
            _ => GameMusicRole.CombatEarly
        };
    }
}
