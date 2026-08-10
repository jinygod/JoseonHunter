using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby.Views
{
    public sealed class LobbyHomeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text difficultyText;
        [SerializeField] private TMP_Text startingWeaponText;
        [SerializeField] private Image startingWeaponIcon;
        [SerializeField] private LobbyMenuCardView trainingCard;
        [SerializeField] private LobbyMenuCardView patrolCard;
        [SerializeField] private LobbyMenuCardView researchCard;

        public TMP_Text StageText => stageText;
        public TMP_Text DifficultyText => difficultyText;
        public TMP_Text StartingWeaponText => startingWeaponText;
        public Image StartingWeaponIcon => startingWeaponIcon;
        public LobbyMenuCardView TrainingCard => trainingCard;
        public LobbyMenuCardView PatrolCard => patrolCard;
        public LobbyMenuCardView ResearchCard => researchCard;
        public bool HasRequiredBindings =>
            stageText != null && difficultyText != null && startingWeaponText != null && startingWeaponIcon != null &&
            trainingCard != null && patrolCard != null && researchCard != null;

        public void Configure(TMP_Text stage, TMP_Text difficulty, TMP_Text weapon, Image weaponIcon,
            LobbyMenuCardView training, LobbyMenuCardView patrol, LobbyMenuCardView research)
        {
            stageText = stage;
            difficultyText = difficulty;
            startingWeaponText = weapon;
            startingWeaponIcon = weaponIcon;
            trainingCard = training;
            patrolCard = patrol;
            researchCard = research;
        }
    }
}
