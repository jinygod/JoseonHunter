using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [CreateAssetMenu(menuName = "JoseonHunter/Lobby/Training Icon Set")]
    public sealed class LobbyTrainingIconSet : ScriptableObject
    {
        [SerializeField] private Sprite[] icons;

        public Sprite Icon(CommonTrainingId id) =>
            icons != null && (int)id >= 0 && (int)id < icons.Length ? icons[(int)id] : null;

        public Sprite[] Icons => icons;
        public bool HasExactBindings => icons != null && icons.Length == 6 &&
            Matches(CommonTrainingId.Vitality, "training_vitality") &&
            Matches(CommonTrainingId.Power, "training_power") &&
            Matches(CommonTrainingId.Footwork, "training_footwork") &&
            Matches(CommonTrainingId.Learning, "training_learning") &&
            Matches(CommonTrainingId.Guard, "training_guard") &&
            Matches(CommonTrainingId.Resonance, "training_resonance");

        public void Configure(Sprite[] values) => icons = values;

        private bool Matches(CommonTrainingId id, string expectedName) =>
            Icon(id) != null && Icon(id).name == expectedName;
    }
}
