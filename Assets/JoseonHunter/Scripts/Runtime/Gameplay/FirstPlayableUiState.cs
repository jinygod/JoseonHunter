using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public readonly struct FirstPlayableUiState
    {
        public FirstPlayableUiState(int level, int experience, int experienceToNext, int coins, int kills,
            float elapsed, float duration, float health, float maximumHealth, bool bossWarning, bool bossAlive,
            float bossHealth, float bossMaximumHealth, IReadOnlyList<WeaponSlotView> weapons)
        {
            Level = level; Experience = experience; ExperienceToNext = experienceToNext; Coins = coins; Kills = kills;
            Elapsed = elapsed; Duration = duration; Health = health; MaximumHealth = maximumHealth;
            BossWarning = bossWarning; BossAlive = bossAlive; BossHealth = bossHealth; BossMaximumHealth = bossMaximumHealth;
            Weapons = Array.AsReadOnly(weapons.ToArray());
        }

        public int Level { get; }
        public int Experience { get; }
        public int ExperienceToNext { get; }
        public int Coins { get; }
        public int Kills { get; }
        public float Elapsed { get; }
        public float Duration { get; }
        public float Health { get; }
        public float MaximumHealth { get; }
        public bool BossWarning { get; }
        public bool BossAlive { get; }
        public float BossHealth { get; }
        public float BossMaximumHealth { get; }
        public IReadOnlyList<WeaponSlotView> Weapons { get; }
    }

    public readonly struct UpgradeChoiceView
    {
        public UpgradeChoiceView(string id, UpgradeKind kind, int nextLevel, string category, string name,
            string behavior, string delta, Sprite icon)
        {
            Id = id; Kind = kind; NextLevel = nextLevel; Category = category; Name = name;
            Behavior = behavior; Delta = delta; Icon = icon;
        }

        public string Id { get; }
        public UpgradeKind Kind { get; }
        public int NextLevel { get; }
        public string Category { get; }
        public string Name { get; }
        public string Behavior { get; }
        public string Delta { get; }
        public Sprite Icon { get; }
    }

    public readonly struct WeaponSlotView
    {
        public WeaponSlotView(string id, string displayName, int level, Sprite icon,
            string generalAffixSummary = null, IEnumerable<WeaponPotentialId> potentialIds = null)
        {
            Id = id; DisplayName = displayName; Level = level; Icon = icon;
            GeneralAffixSummary = generalAffixSummary ?? string.Empty;
            PotentialIds = Array.AsReadOnly((potentialIds ?? Array.Empty<WeaponPotentialId>()).ToArray());
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Level { get; }
        public Sprite Icon { get; }
        public string GeneralAffixSummary { get; }
        public IReadOnlyList<WeaponPotentialId> PotentialIds { get; }
    }

    public sealed class UpgradeChoiceState
    {
        public UpgradeChoiceState(int level, IEnumerable<UpgradeChoiceView> choices)
        {
            Level = level;
            Choices = Array.AsReadOnly(choices.ToArray());
        }

        public int Level { get; }
        public IReadOnlyList<UpgradeChoiceView> Choices { get; }
    }

    public enum ProgressionRewardKind { Support, WeaponLevel, NewWeapon, Evolution }

    public readonly struct ProgressionRewardEvent
    {
        public ProgressionRewardEvent(string id, string weaponId, int newLevel, ProgressionRewardKind kind,
            string displayName, string changeSummary, Sprite icon, WeaponAffixRollResult affixResult = null)
        {
            Id = id; WeaponId = weaponId; NewLevel = newLevel; Kind = kind; DisplayName = displayName;
            ChangeSummary = changeSummary; Icon = icon; AffixResult = affixResult;
        }

        public string Id { get; }
        public string WeaponId { get; }
        public int NewLevel { get; }
        public ProgressionRewardKind Kind { get; }
        public string DisplayName { get; }
        public string ChangeSummary { get; }
        public Sprite Icon { get; }
        public WeaponAffixRollResult AffixResult { get; }
    }
}
