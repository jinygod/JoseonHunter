using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameFlowStateTests
    {
        [TestCase(GameFlowState.Playing, GameFlowState.LevelUpSelection, true)]
        [TestCase(GameFlowState.LevelUpSelection, GameFlowState.AugmentResult, true)]
        [TestCase(GameFlowState.AugmentResult, GameFlowState.LevelUpSelection, true)]
        [TestCase(GameFlowState.AugmentResult, GameFlowState.Playing, true)]
        [TestCase(GameFlowState.Playing, GameFlowState.Paused, true)]
        [TestCase(GameFlowState.Paused, GameFlowState.Playing, true)]
        [TestCase(GameFlowState.Paused, GameFlowState.AugmentResult, false)]
        [TestCase(GameFlowState.Playing, GameFlowState.GameOver, true)]
        [TestCase(GameFlowState.GameOver, GameFlowState.Playing, true)]
        [TestCase(GameFlowState.GameOver, GameFlowState.LevelUpSelection, false)]
        public void Transition_policy_is_explicit(GameFlowState from, GameFlowState to, bool expected)
        {
            Assert.That(GameFlowTransitions.CanTransition(from, to), Is.EqualTo(expected));
        }

        [TestCase(GameFlowState.Playing)]
        [TestCase(GameFlowState.LevelUpSelection)]
        [TestCase(GameFlowState.AugmentResult)]
        [TestCase(GameFlowState.Paused)]
        [TestCase(GameFlowState.GameOver)]
        public void A_state_can_transition_to_itself(GameFlowState state)
        {
            Assert.That(GameFlowTransitions.CanTransition(state, state), Is.True);
        }
    }
}
