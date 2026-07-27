using System;

namespace JoseonHunter.Domain.Save
{
    public interface ISaveRepository { LoadResult Load(); SaveResult Save(SaveDataV1 data); }
    public readonly struct LoadResult { public LoadResult(SaveDataV1 data, LoadSource source, SaveError error) { Data = data; Source = source; Error = error; } public SaveDataV1 Data { get; } public LoadSource Source { get; } public SaveError Error { get; } }
    public readonly struct SaveResult { public SaveResult(bool success, SaveError error) { Success = success; Error = error; } public bool Success { get; } public SaveError Error { get; } }
    public enum LoadSource { Current, Backup, Defaults }
    public enum SaveError { None, Corrupt, InsufficientStorage, IoFailure }
    public enum AutoSaveTrigger { RunResult, EquipmentPurchase, EvolutionPurchase, SettingsChanged, AppPaused }
    public sealed class AutoSaveOrchestrator
    {
        private readonly ISaveRepository repository;
        public AutoSaveOrchestrator(ISaveRepository repository) { this.repository = repository ?? throw new ArgumentNullException(nameof(repository)); }
        public AutoSaveOrchestrator(ISaveRepository repository, SaveDataV1 live) : this(repository) { this.live = live ?? throw new ArgumentNullException(nameof(live)); }
        private SaveDataV1 live;
        public SaveResult SaveFor(AutoSaveTrigger trigger, SaveDataV1 data) { return repository.Save(data); }
        public TransactionResult PurchaseEquipment(string id, int cost) { return Apply(copy => new Progression.EquipmentProgression(copy).PurchaseLevel(id, cost)); }
        public TransactionResult PurchaseEvolution(string id, int cost) { return Apply(copy => new Progression.EvolutionBoard(copy).Purchase(id, cost)); }
        private TransactionResult Apply(Func<SaveDataV1, Progression.ProgressionResult> mutate) { if (live == null) throw new InvalidOperationException("A live save is required."); var copy = live.Copy(); var result = mutate(copy); if (!result.Success) return new TransactionResult(false, result.Error, SaveError.None); var save = repository.Save(copy); if (!save.Success) return new TransactionResult(false, Progression.ProgressionError.None, save.Error); live.CopyFrom(copy); return new TransactionResult(true, Progression.ProgressionError.None, SaveError.None); }
    }
    public readonly struct TransactionResult { public TransactionResult(bool success, Progression.ProgressionError error, SaveError saveError) { Success = success; Error = error; SaveError = saveError; } public bool Success { get; } public Progression.ProgressionError Error { get; } public SaveError SaveError { get; } }
}
