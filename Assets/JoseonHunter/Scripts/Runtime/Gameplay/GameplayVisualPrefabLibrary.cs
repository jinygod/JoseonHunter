using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [CreateAssetMenu(
        fileName = "GameplayVisualPrefabLibrary",
        menuName = "Joseon Hunter/Gameplay Visual Prefab Library")]
    public sealed class GameplayVisualPrefabLibrary : ScriptableObject
    {
        [Header("Combatants")]
        [SerializeField] private GameObject playerVisual;
        [SerializeField] private GameObject enemyVisual;

        [Header("World bars")]
        [SerializeField] private GameObject worldHealthBar;
        [SerializeField] private GameObject worldShieldBar;

        [Header("Pickups")]
        [SerializeField] private GameObject experiencePickup;
        [SerializeField] private GameObject yeopjeonPickup;
        [SerializeField] private GameObject magnetPickup;

        public GameObject PlayerVisual => playerVisual;
        public GameObject EnemyVisual => enemyVisual;
        public GameObject WorldHealthBar => worldHealthBar;
        public GameObject WorldShieldBar => worldShieldBar;
        public GameObject ExperiencePickup => experiencePickup;
        public GameObject YeopjeonPickup => yeopjeonPickup;
        public GameObject MagnetPickup => magnetPickup;

        public bool IsComplete => playerVisual != null && enemyVisual != null &&
                                  worldHealthBar != null && worldShieldBar != null &&
                                  experiencePickup != null && yeopjeonPickup != null &&
                                  magnetPickup != null;

        public void Configure(
            GameObject player,
            GameObject enemy,
            GameObject healthBar,
            GameObject shieldBar,
            GameObject experience,
            GameObject yeopjeon,
            GameObject magnet)
        {
            playerVisual = player;
            enemyVisual = enemy;
            worldHealthBar = healthBar;
            worldShieldBar = shieldBar;
            experiencePickup = experience;
            yeopjeonPickup = yeopjeon;
            magnetPickup = magnet;
        }
    }
}
