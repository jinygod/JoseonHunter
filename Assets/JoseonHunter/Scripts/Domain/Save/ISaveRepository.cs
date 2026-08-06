using System;

namespace JoseonHunter.Domain.Save
{
    public interface ISaveRepository { LoadResult Load(); SaveResult Save(SaveDataV1 data); }
    public readonly struct LoadResult { public LoadResult(SaveDataV1 data, LoadSource source, SaveError error) { Data = data; Source = source; Error = error; } public SaveDataV1 Data { get; } public LoadSource Source { get; } public SaveError Error { get; } }
    public readonly struct SaveResult { public SaveResult(bool success, SaveError error) { Success = success; Error = error; } public bool Success { get; } public SaveError Error { get; } }
    public enum LoadSource { Current, Backup, Defaults }
    public enum SaveError { None, Corrupt, InsufficientStorage, IoFailure }
    public enum AutoSaveTrigger { RunResult, EquipmentPurchase, EvolutionPurchase, WeaponStylePurchase, CommonTrainingPurchase, LoadoutChanged, SettingsChanged, AppPaused }
    public sealed class AutoSaveOrchestrator
    {
        private readonly ISaveRepository repository;
        public AutoSaveOrchestrator(ISaveRepository repository) { this.repository = repository ?? throw new ArgumentNullException(nameof(repository)); }
        public AutoSaveOrchestrator(ISaveRepository repository, SaveDataV1 live) : this(repository) { this.live = live ?? throw new ArgumentNullException(nameof(live)); }
        private SaveDataV1 live;
        public SaveResult SaveFor(AutoSaveTrigger trigger, SaveDataV1 data) { return repository.Save(data); }
        public TransactionResult PurchaseEquipment(string id, int cost) { return Apply(copy => new Progression.EquipmentProgression(copy).PurchaseLevel(id, cost)); }
        public TransactionResult PurchaseEvolution(string id, int cost) { return Apply(copy => new Progression.EvolutionBoard(copy).Purchase(id, cost)); }
        public TransactionResult PurchaseWeaponStyle(Combat.WeaponId weaponId, Progression.WeaponLegacyPathId styleId) { return Apply(copy => new Progression.WeaponMasteryProgression(copy).Purchase(weaponId, styleId)); }
        public TransactionResult PurchaseCommonTraining(Progression.CommonTrainingId id) { return Apply(copy => new Progression.CommonTrainingProgression(copy).Purchase(id)); }
        public TransactionResult ResetCommonTraining() { return Apply(copy => new Progression.CommonTrainingProgression(copy).Reset()); }
        public TransactionResult SaveLoadout(int index, Progression.PatrolLoadout loadout)
        {
            if (loadout == null) throw new ArgumentNullException(nameof(loadout));
            return Apply(copy =>
            {
                if (index < 0 || index >= copy.PatrolLoadouts.Count)
                    return new Progression.ProgressionResult(false, Progression.ProgressionError.InvalidSelection);
                foreach (var pair in loadout.Styles)
                    if (!string.IsNullOrEmpty(pair.Value.Value) && !copy.UnlockedWeaponStyles.Contains(pair.Value.Value))
                        return new Progression.ProgressionResult(false, Progression.ProgressionError.InvalidSelection);
                var dto = new PatrolLoadoutData
                {
                    Name = loadout.Name,
                    StartingWeaponId = loadout.StartingWeapon.Value,
                    DifficultyId = loadout.DifficultyId
                };
                foreach (var pair in loadout.Styles)
                    dto.WeaponStyleIds[pair.Key.Value] = pair.Value.Value ?? string.Empty;
                foreach (var weaponId in Combat.WeaponRoster.All)
                    if (!dto.WeaponStyleIds.ContainsKey(weaponId.Value)) dto.WeaponStyleIds[weaponId.Value] = string.Empty;
                copy.PatrolLoadouts[index] = dto;
                copy.ActivePatrolLoadoutIndex = index;
                return new Progression.ProgressionResult(true, Progression.ProgressionError.None);
            });
        }
        public TransactionResult CommitRun(Progression.RunSettlement settlement)
        {
            return Apply(copy =>
            {
                var accountReward = Progression.AccountProgression.RewardFor(settlement);
                if (!Progression.AccountProgression.TryAdd(
                        copy.AccountExperience, accountReward, out var nextAccountExperience))
                    return new Progression.ProgressionResult(false, Progression.ProgressionError.InvalidAmount);
                var nextCoins = (long)copy.Coins + settlement.Coins;
                if (nextCoins > int.MaxValue) return new Progression.ProgressionResult(false, Progression.ProgressionError.InvalidAmount);
                copy.Coins = (int)nextCoins;
                copy.AccountExperience = nextAccountExperience;
                foreach (var pair in settlement.Mastery)
                {
                    var current = copy.WeaponMasteryPoints.TryGetValue(pair.Key.Value, out var value) ? value : 0;
                    var next = (long)current + pair.Value;
                    if (next > int.MaxValue) return new Progression.ProgressionResult(false, Progression.ProgressionError.InvalidAmount);
                    copy.WeaponMasteryPoints[pair.Key.Value] = (int)next;
                }
                var recordKey = settlement.Victory ? "victory_kills" : "patrol_kills";
                if (!copy.BestPatrolResults.TryGetValue(recordKey, out var best) || settlement.Kills > best)
                    copy.BestPatrolResults[recordKey] = settlement.Kills;
                return new Progression.ProgressionResult(true, Progression.ProgressionError.None);
            });
        }
        private TransactionResult Apply(Func<SaveDataV1, Progression.ProgressionResult> mutate) { if (live == null) throw new InvalidOperationException("A live save is required."); var copy = live.Copy(); var result = mutate(copy); if (!result.Success) return new TransactionResult(false, result.Error, SaveError.None); var save = repository.Save(copy); if (!save.Success) return new TransactionResult(false, Progression.ProgressionError.None, save.Error); live.CopyFrom(copy); return new TransactionResult(true, Progression.ProgressionError.None, SaveError.None); }
    }
    public readonly struct TransactionResult { public TransactionResult(bool success, Progression.ProgressionError error, SaveError saveError) { Success = success; Error = error; SaveError = saveError; } public bool Success { get; } public Progression.ProgressionError Error { get; } public SaveError SaveError { get; } }
}
