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
        public SaveResult SaveFor(AutoSaveTrigger trigger, SaveDataV1 data) { return repository.Save(data); }
    }
}
