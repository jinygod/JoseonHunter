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
            float spawnMarginMinimum,
            float spawnMarginMaximum)
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
            SpawnMarginMinimum = spawnMarginMinimum;
            SpawnMarginMaximum = spawnMarginMaximum;
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
                9.5f,
                12.25f);

        public static CombatVisualScaleProfile MobilePortrait { get; } =
            new CombatVisualScaleProfile(
                baselineCameraOrthographicSize: 6.25f,
                cameraOrthographicSize: 7.25f,
                playerScale: .82f,
                normalEnemyScale: .78f,
                eliteEnemyScale: 1f,
                bossEnemyScale: 1.42f,
                normalContactRadius: .42f,
                eliteContactRadius: .55f,
                bossContactRadius: .78f,
                spawnMarginMinimum: .75f,
                spawnMarginMaximum: 1.5f);

        public float BaselineCameraOrthographicSize { get; }
        public float CameraOrthographicSize { get; }
        public float PlayerScale { get; }
        public float NormalEnemyScale { get; }
        public float EliteEnemyScale { get; }
        public float BossEnemyScale { get; }
        public float NormalContactRadius { get; }
        public float EliteContactRadius { get; }
        public float BossContactRadius { get; }
        public float SpawnMarginMinimum { get; }
        public float SpawnMarginMaximum { get; }

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
