using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    /// <summary>
    /// Keeps the visible world, combat silhouettes and contact distances on one coordinated scale.
    /// Values are expressed in world units and tuned for the 1920x1080 Android landscape target.
    /// </summary>
    public readonly struct CombatVisualScaleProfile
    {
        private CombatVisualScaleProfile(
            float baselineCameraOrthographicSize,
            float cameraOrthographicSize,
            float playerScale,
            float normalEnemyScale,
            float eliteEnemyScale,
            float bossEnemyScale,
            float normalContactRadius,
            float eliteContactRadius,
            float bossContactRadius,
            float spawnRadiusMinimum,
            float spawnRadiusMaximum)
        {
            BaselineCameraOrthographicSize = baselineCameraOrthographicSize;
            CameraOrthographicSize = cameraOrthographicSize;
            PlayerScale = playerScale;
            NormalEnemyScale = normalEnemyScale;
            EliteEnemyScale = eliteEnemyScale;
            BossEnemyScale = bossEnemyScale;
            NormalContactRadius = normalContactRadius;
            EliteContactRadius = eliteContactRadius;
            BossContactRadius = bossContactRadius;
            SpawnRadiusMinimum = spawnRadiusMinimum;
            SpawnRadiusMaximum = spawnRadiusMaximum;
        }

        public static CombatVisualScaleProfile MobileLandscape { get; } =
            new CombatVisualScaleProfile(
                6.25f,
                10.5f,
                0.62f,
                0.62f,
                0.775f,
                1.116f,
                0.32f,
                0.43f,
                0.64f,
                12.25f,
                15.25f);

        public float BaselineCameraOrthographicSize { get; }
        public float CameraOrthographicSize { get; }
        public float PlayerScale { get; }
        public float NormalEnemyScale { get; }
        public float EliteEnemyScale { get; }
        public float BossEnemyScale { get; }
        public float NormalContactRadius { get; }
        public float EliteContactRadius { get; }
        public float BossContactRadius { get; }
        public float SpawnRadiusMinimum { get; }
        public float SpawnRadiusMaximum { get; }

        public float CameraAreaRatio =>
            Mathf.Pow(CameraOrthographicSize / BaselineCameraOrthographicSize, 2f);

        public float PlayerScreenHeightRatio =>
            PlayerScale * BaselineCameraOrthographicSize / CameraOrthographicSize;

        public float ScaleFor(EnemyRankProfile rank) =>
            rank.IsBoss ? BossEnemyScale : rank.IsElite ? EliteEnemyScale : NormalEnemyScale;

        public float ContactRadiusFor(EnemyRankProfile rank) =>
            rank.IsBoss ? BossContactRadius : rank.IsElite ? EliteContactRadius : NormalContactRadius;
    }
}
