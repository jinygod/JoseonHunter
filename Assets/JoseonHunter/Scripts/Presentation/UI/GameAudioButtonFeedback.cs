using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class GameAudioButtonFeedback : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameAudioCueId cue = GameAudioCueId.UiClick;

        private Button button;

#if UNITY_INCLUDE_TESTS
        public GameAudioCueId CueForTests => cue;
#endif

        private void Awake() => button = GetComponent<Button>();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return;
            GameAudioDirector.EnsureExists();
            GameAudioDirector.Instance?.TryPlay(cue);
        }

        public static GameAudioButtonFeedback Attach(
            Button target,
            GameAudioCueId requestedCue = GameAudioCueId.UiClick)
        {
            if (target == null) return null;
            var feedback = target.GetComponent<GameAudioButtonFeedback>() ??
                           target.gameObject.AddComponent<GameAudioButtonFeedback>();
            feedback.cue = requestedCue;
            feedback.button = target;
            return feedback;
        }

        public static void AttachAll(Transform root)
        {
            if (root == null) return;
            foreach (var target in root.GetComponentsInChildren<Button>(true)) Attach(target);
        }
    }
}
