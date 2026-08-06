using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Runs;

namespace JoseonHunter.Domain.Progression
{
    public readonly struct RunSettlement
    {
        public RunSettlement(
            IReadOnlyDictionary<WeaponId, int> mastery,
            int coins,
            int kills,
            float elapsed,
            bool victory,
            bool abandoned)
            : this(
                mastery,
                coins,
                kills,
                elapsed,
                victory,
                abandoned,
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal),
                0)
        {
        }

        public RunSettlement(
            IReadOnlyDictionary<WeaponId, int> mastery,
            int coins,
            int kills,
            float elapsed,
            bool victory,
            bool abandoned,
            StageSelection stageSelection,
            int level)
        {
            if (mastery == null) throw new ArgumentNullException(nameof(mastery));
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            if (kills < 0) throw new ArgumentOutOfRangeException(nameof(kills));
            if (elapsed < 0f || float.IsNaN(elapsed) || float.IsInfinity(elapsed))
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
            if (!StageCatalog.TryGet(stageSelection.StageId, out _))
                throw new ArgumentException("Unknown stage selection.", nameof(stageSelection));

            var copy = new Dictionary<WeaponId, int>();
            foreach (var pair in mastery)
            {
                if (pair.Value < 0) throw new ArgumentOutOfRangeException(nameof(mastery));
                if (pair.Value > 0) copy[pair.Key] = pair.Value;
            }

            Mastery = new ReadOnlyDictionary<WeaponId, int>(copy);
            Coins = coins;
            Kills = kills;
            Elapsed = elapsed;
            Victory = victory;
            Abandoned = abandoned;
            StageSelection = stageSelection;
            Level = level;
        }

        public IReadOnlyDictionary<WeaponId, int> Mastery { get; }
        public int Coins { get; }
        public int Kills { get; }
        public float Elapsed { get; }
        public bool Victory { get; }
        public bool Abandoned { get; }
        public StageSelection StageSelection { get; }
        public int Level { get; }
    }
}
