using System;
using JoseonHunter.Domain.Runs;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class GameFlowCoordinator : MonoBehaviour
    {
        private float hitStopRemaining;

        public GameFlowState State { get; private set; } = GameFlowState.Playing;
        public bool IsGameplayRunning => State == GameFlowState.Playing;
        public event Action<GameFlowState, GameFlowState> StateChanged;

        public bool TryTransition(GameFlowState next)
        {
            if (!GameFlowTransitions.CanTransition(State, next))
            {
                Debug.LogWarning($"Rejected game-flow transition {State} -> {next}.", this);
                return false;
            }

            var previous = State;
            State = next;
            if (next != GameFlowState.Playing) hitStopRemaining = 0f;
            ApplyTimeScale();
            if (previous != next) StateChanged?.Invoke(previous, next);
            return true;
        }

        public bool RequestHitStop(float seconds)
        {
            if (State != GameFlowState.Playing || seconds <= 0f) return false;
            hitStopRemaining = Mathf.Max(hitStopRemaining, seconds);
            ApplyTimeScale();
            return true;
        }

        public void ResetToPlaying()
        {
            var previous = State;
            State = GameFlowState.Playing;
            hitStopRemaining = 0f;
            ApplyTimeScale();
            if (previous != State) StateChanged?.Invoke(previous, State);
        }

        private void Update()
        {
            if (State != GameFlowState.Playing || hitStopRemaining <= 0f) return;
            hitStopRemaining = Mathf.Max(0f, hitStopRemaining - Time.unscaledDeltaTime);
            ApplyTimeScale();
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = State == GameFlowState.Playing && hitStopRemaining <= 0f ? 1f : 0f;
        }

        private void OnDisable()
        {
            State = GameFlowState.Playing;
            hitStopRemaining = 0f;
            Time.timeScale = 1f;
        }
    }
}
