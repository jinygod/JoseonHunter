using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Infrastructure.Save;
using UnityEngine;

namespace JoseonHunter.Runtime.Meta
{
    [DisallowMultipleComponent]
    public sealed class MetaGameSession : MonoBehaviour
    {
        private ISaveRepository repository;
        private AutoSaveOrchestrator autosave;
        private SaveDataV1 data;

        public static MetaGameSession Current { get; private set; }
        public SaveDataV1 Data { get { EnsureInitialized(); return data; } }
        public GameSceneRouter Router { get; private set; }
        public PatrolLoadout ActiveLoadout { get { EnsureInitialized(); return BuildActiveLoadout(data); } }

        public static MetaGameSession EnsureExists(ISaveRepository repository = null)
        {
            if (Current != null)
            {
                Current.EnsureInitialized(repository);
                return Current;
            }

            var root = new GameObject("Meta Game Session");
            var session = root.AddComponent<MetaGameSession>();
            session.EnsureInitialized(repository);
            return session;
        }

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(gameObject);
                return;
            }

            Current = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Router = new GameSceneRouter();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public TransactionResult PurchaseStyle(WeaponId weaponId, WeaponLegacyPathId styleId)
        {
            EnsureInitialized();
            return autosave.PurchaseWeaponStyle(weaponId, styleId);
        }

        public TransactionResult PurchaseTraining(CommonTrainingId id)
        {
            EnsureInitialized();
            return autosave.PurchaseCommonTraining(id);
        }

        public TransactionResult ResetTraining()
        {
            EnsureInitialized();
            return autosave.ResetCommonTraining();
        }

        public TransactionResult SaveLoadout(int index, PatrolLoadout loadout)
        {
            EnsureInitialized();
            return autosave.SaveLoadout(index, loadout);
        }

        public TransactionResult CommitRun(RunSettlement settlement)
        {
            EnsureInitialized();
            return autosave.CommitRun(settlement);
        }

        private void EnsureInitialized(ISaveRepository requestedRepository = null)
        {
            if (data != null)
            {
                if (requestedRepository != null && !ReferenceEquals(repository, requestedRepository))
                    throw new InvalidOperationException("The meta session is already initialized with another repository.");
                return;
            }

            repository = requestedRepository ?? new JsonSaveRepository();
            var loaded = repository.Load();
            data = loaded.Data ?? SaveDataV1.CreateDefaults();
            autosave = new AutoSaveOrchestrator(repository, data);
        }

        private static PatrolLoadout BuildActiveLoadout(SaveDataV1 source)
        {
            var index = Math.Max(0, Math.Min(source.PatrolLoadouts.Count - 1, source.ActivePatrolLoadoutIndex));
            var dto = source.PatrolLoadouts[index];
            var startingWeapon = WeaponRoster.All.FirstOrDefault(id => id.Value == dto.StartingWeaponId);
            if (string.IsNullOrEmpty(startingWeapon.Value)) startingWeapon = WeaponId.HwandoFlyingBlade;

            var styles = new Dictionary<WeaponId, WeaponLegacyPathId>();
            foreach (var weaponId in WeaponRoster.All)
            {
                if (!dto.WeaponStyleIds.TryGetValue(weaponId.Value, out var styleId) || string.IsNullOrEmpty(styleId))
                    continue;
                var pathId = new WeaponLegacyPathId(styleId);
                if (source.UnlockedWeaponStyles.Contains(styleId) &&
                    WeaponLegacyCatalog.TryGet(pathId, out var definition) &&
                    definition.WeaponId.Equals(weaponId))
                    styles[weaponId] = pathId;
            }

            var name = string.IsNullOrWhiteSpace(dto.Name) ? "순찰대 " + (index + 1) : dto.Name;
            var difficulty = string.IsNullOrWhiteSpace(dto.DifficultyId) ? "normal" : dto.DifficultyId;
            return new PatrolLoadout(name, startingWeapon, styles, difficulty);
        }
    }
}
