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
            float bossHealth, float bossMaximumHealth, IReadOnlyList<WeaponSlotView> weapons,
            string waveAnnouncement = null, float waveAnnouncementRemaining = 0f,
            int waveAnnouncementIntensity = 0, bool runEnded = false, bool victory = false,
            int runMasteryEarned = 0, bool settlementFailed = false)
        {
            Level = level; Experience = experience; ExperienceToNext = experienceToNext; Coins = coins; Kills = kills;
            Elapsed = elapsed; Duration = duration; Health = health; MaximumHealth = maximumHealth;
            BossWarning = bossWarning; BossAlive = bossAlive; BossHealth = bossHealth; BossMaximumHealth = bossMaximumHealth;
            Weapons = Array.AsReadOnly(weapons.ToArray());
            WaveAnnouncement = waveAnnouncement ?? string.Empty;
            WaveAnnouncementRemaining = waveAnnouncementRemaining;
            WaveAnnouncementIntensity = waveAnnouncementIntensity;
            RunEnded = runEnded;
            Victory = victory;
            RunMasteryEarned = runMasteryEarned;
            SettlementFailed = settlementFailed;
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
        public string WaveAnnouncement { get; }
        public float WaveAnnouncementRemaining { get; }
        public int WaveAnnouncementIntensity { get; }
        public bool RunEnded { get; }
        public bool Victory { get; }
        public int RunMasteryEarned { get; }
        public bool SettlementFailed { get; }
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
            string generalAffixSummary = null, IEnumerable<WeaponPotentialId> potentialIds = null,
            IEnumerable<WeaponAffixTier> generalAffixTiers = null, string behavior = null,
            string legacyName = null, string legacyStageName = null, string nextLegacyMilestone = null,
            IEnumerable<WeaponAffixRoll> generalAffixRolls = null)
        {
            Id = id; DisplayName = displayName; Level = level; Icon = icon;
            GeneralAffixSummary = generalAffixSummary ?? string.Empty;
            Behavior = behavior ?? string.Empty;
            LegacyName = legacyName ?? string.Empty;
            LegacyStageName = legacyStageName ?? "무기 3레벨에서 두 방식 중 하나 선택";
            NextLegacyMilestone = nextLegacyMilestone ?? "무기 3레벨에서 두 방식 중 하나 선택";
            PotentialIds = Array.AsReadOnly((potentialIds ?? Array.Empty<WeaponPotentialId>()).ToArray());
            GeneralAffixTiers = Array.AsReadOnly((generalAffixTiers ?? Array.Empty<WeaponAffixTier>()).ToArray());
            GeneralAffixRolls = Array.AsReadOnly((generalAffixRolls ?? Array.Empty<WeaponAffixRoll>()).ToArray());
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Level { get; }
        public Sprite Icon { get; }
        public string GeneralAffixSummary { get; }
        public string Behavior { get; }
        public string LegacyName { get; }
        public string LegacyStageName { get; }
        public string NextLegacyMilestone { get; }
        public IReadOnlyList<WeaponPotentialId> PotentialIds { get; }
        public IReadOnlyList<WeaponAffixTier> GeneralAffixTiers { get; }
        public IReadOnlyList<WeaponAffixRoll> GeneralAffixRolls { get; }
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

    public readonly struct WeaponReplacementChoiceView
    {
        public WeaponReplacementChoiceView(string weaponId, string displayName, int level,
            string legacyName, Sprite icon)
        {
            WeaponId = weaponId;
            DisplayName = displayName ?? string.Empty;
            Level = level;
            LegacyName = legacyName ?? string.Empty;
            Icon = icon;
        }

        public string WeaponId { get; }
        public string DisplayName { get; }
        public int Level { get; }
        public string LegacyName { get; }
        public Sprite Icon { get; }
    }

    public sealed class WeaponReplacementState
    {
        public WeaponReplacementState(string newWeaponId, string newWeaponName,
            IEnumerable<WeaponReplacementChoiceView> choices)
        {
            NewWeaponId = newWeaponId ?? string.Empty;
            NewWeaponName = newWeaponName ?? string.Empty;
            Choices = Array.AsReadOnly((choices ?? Array.Empty<WeaponReplacementChoiceView>()).ToArray());
        }

        public string NewWeaponId { get; }
        public string NewWeaponName { get; }
        public IReadOnlyList<WeaponReplacementChoiceView> Choices { get; }
    }

    public readonly struct WeaponLegacyChoiceView
    {
        public WeaponLegacyChoiceView(WeaponLegacyPathId pathId, string displayName, string combatStyle,
            string benefit, string cost, Sprite icon)
        {
            PathId = pathId;
            DisplayName = displayName ?? string.Empty;
            CombatStyle = combatStyle ?? string.Empty;
            Benefit = benefit ?? string.Empty;
            Cost = cost ?? string.Empty;
            Icon = icon;
        }

        public WeaponLegacyPathId PathId { get; }
        public string DisplayName { get; }
        public string CombatStyle { get; }
        public string Benefit { get; }
        public string Cost { get; }
        public Sprite Icon { get; }
    }

    public sealed class WeaponLegacyChoiceState
    {
        public WeaponLegacyChoiceState(string weaponId, string weaponName,
            IEnumerable<WeaponLegacyChoiceView> choices)
        {
            WeaponId = weaponId ?? string.Empty;
            WeaponName = weaponName ?? string.Empty;
            Choices = Array.AsReadOnly((choices ?? Array.Empty<WeaponLegacyChoiceView>()).ToArray());
        }

        public string WeaponId { get; }
        public string WeaponName { get; }
        public IReadOnlyList<WeaponLegacyChoiceView> Choices { get; }
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
