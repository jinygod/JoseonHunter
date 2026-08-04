using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JoseonHunter.Domain.Combat;

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
        {
            if (mastery == null) throw new ArgumentNullException(nameof(mastery));
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            if (kills < 0) throw new ArgumentOutOfRangeException(nameof(kills));
            if (elapsed < 0f || float.IsNaN(elapsed) || float.IsInfinity(elapsed))
                throw new ArgumentOutOfRangeException(nameof(elapsed));

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
        }

        public IReadOnlyDictionary<WeaponId, int> Mastery { get; }
        public int Coins { get; }
        public int Kills { get; }
        public float Elapsed { get; }
        public bool Victory { get; }
        public bool Abandoned { get; }
    }
}
