using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class GameplayReadySignal
    {
        public static bool IsReady { get; private set; }

        public static void MarkReady() => IsReady = true;

        public static void Reset() => IsReady = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewPlayerSession() => Reset();
    }
}
