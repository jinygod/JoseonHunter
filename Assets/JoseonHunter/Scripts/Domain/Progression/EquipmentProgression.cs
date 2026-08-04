using System;
using JoseonHunter.Domain.Save;

namespace JoseonHunter.Domain.Progression
{
    public enum ProgressionError { None, InsufficientCoins, InsufficientFragments, InsufficientMastery, UnknownId, InvalidAmount, InvalidSelection, MaximumReached, AlreadyUnlocked }
    public readonly struct ProgressionResult { public ProgressionResult(bool success, ProgressionError error) { Success = success; Error = error; } public bool Success { get; } public ProgressionError Error { get; } }
    public sealed class EquipmentProgression
    {
        private readonly SaveDataV1 data;
        public EquipmentProgression(SaveDataV1 data) { this.data = data ?? throw new ArgumentNullException(nameof(data)); }
        public int SlotCount => 4; public int ItemCount => 12;
        public ProgressionResult PurchaseLevel(string itemId, int cost)
        {
            if (cost < 0) return new ProgressionResult(false, ProgressionError.InvalidAmount);
            if (!data.EquipmentLevels.ContainsKey(itemId)) return new ProgressionResult(false, ProgressionError.UnknownId);
            if (data.Coins < cost) return new ProgressionResult(false, ProgressionError.InsufficientCoins);
            var copy = data.Copy(); copy.Coins -= cost; copy.EquipmentLevels[itemId]++; data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None);
        }
        public ProgressionResult UpgradeQuality(string itemId, int selectedFragments)
        {
            if (selectedFragments <= 0) return new ProgressionResult(false, ProgressionError.InvalidAmount);
            if (!data.EquipmentFragments.ContainsKey(itemId) || !data.EquipmentQualities.ContainsKey(itemId)) return new ProgressionResult(false, ProgressionError.UnknownId);
            if (data.EquipmentQualities[itemId] >= 3) return new ProgressionResult(false, ProgressionError.MaximumReached);
            if (data.EquipmentFragments[itemId] < selectedFragments) return new ProgressionResult(false, ProgressionError.InsufficientFragments);
            var copy = data.Copy(); copy.EquipmentFragments[itemId] -= selectedFragments; copy.EquipmentQualities[itemId]++; data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None);
        }
    }
}
