namespace JoseonHunter.Domain.Runs
{
    public enum GameFlowState
    {
        Playing,
        LevelUpSelection,
        AugmentResult,
        Paused,
        GameOver
    }

    public static class GameFlowTransitions
    {
        public static bool CanTransition(GameFlowState from, GameFlowState to)
        {
            if (from == to) return true;
            if (to == GameFlowState.GameOver) return from != GameFlowState.GameOver;
            return (from, to) switch
            {
                (GameFlowState.Playing, GameFlowState.LevelUpSelection) => true,
                (GameFlowState.LevelUpSelection, GameFlowState.AugmentResult) => true,
                (GameFlowState.AugmentResult, GameFlowState.LevelUpSelection) => true,
                (GameFlowState.AugmentResult, GameFlowState.Playing) => true,
                (GameFlowState.Playing, GameFlowState.Paused) => true,
                (GameFlowState.Paused, GameFlowState.Playing) => true,
                (GameFlowState.GameOver, GameFlowState.Playing) => true,
                _ => false
            };
        }
    }
}
