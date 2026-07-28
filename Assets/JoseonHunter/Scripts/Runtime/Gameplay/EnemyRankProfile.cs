namespace JoseonHunter.Runtime.Gameplay
{
    public enum EnemyRank
    {
        Normal,
        Elite,
        Boss
    }

    public readonly struct EnemyRankProfile
    {
        public EnemyRankProfile(
            EnemyRank rank,
            float displayScale,
            float healthMultiplier,
            float contactDamageMultiplier,
            float speedMultiplier,
            int experienceValue)
        {
            Rank = rank;
            DisplayScale = displayScale;
            HealthMultiplier = healthMultiplier;
            ContactDamageMultiplier = contactDamageMultiplier;
            SpeedMultiplier = speedMultiplier;
            ExperienceValue = experienceValue;
        }

        public EnemyRank Rank { get; }
        public float DisplayScale { get; }
        public float HealthMultiplier { get; }
        public float ContactDamageMultiplier { get; }
        public float SpeedMultiplier { get; }
        public int ExperienceValue { get; }
        public bool IsElite => Rank == EnemyRank.Elite;
        public bool IsBoss => Rank == EnemyRank.Boss;

        public static EnemyRankProfile Normal =>
            new EnemyRankProfile(EnemyRank.Normal, 1f, 1f, 1f, 1f, 1);

        public static EnemyRankProfile Elite =>
            new EnemyRankProfile(EnemyRank.Elite, 1.24f, 4f, 1.5f, 0.92f, 5);

        public static EnemyRankProfile Boss =>
            new EnemyRankProfile(EnemyRank.Boss, 1f, 1f, 1f, 1f, 0);
    }
}
