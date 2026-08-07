using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JoseonHunter.Domain;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Domain.Runs;
using UnityEngine;

namespace JoseonHunter.Infrastructure.Save
{
    public sealed class JsonSaveRepository : ISaveRepository
    {
        private const string CurrentName = "progression.json";
        private const string BackupName = "progression.bak";
        private const string TemporaryName = "progression.tmp";
        private readonly string directory;
        private readonly Action<string, string> writeAllText;
        private readonly Action<string, string, string> replaceTemporary;

        public JsonSaveRepository() : this(Application.persistentDataPath) { }
        public JsonSaveRepository(string directory) : this(directory, (path, contents) => File.WriteAllText(path, contents, new UTF8Encoding(false)), Replace) { }
        public JsonSaveRepository(string directory, Action<string, string> writeAllText)
            : this(directory, writeAllText, Replace) { }
        public JsonSaveRepository(string directory, Action<string, string> writeAllText, Action<string, string, string> replaceTemporary)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.writeAllText = writeAllText ?? throw new ArgumentNullException(nameof(writeAllText));
            this.replaceTemporary = replaceTemporary ?? throw new ArgumentNullException(nameof(replaceTemporary));
        }

        public LoadResult Load()
        {
            SaveDataV1 data;
            if (TryLoad(Path.Combine(directory, CurrentName), out data)) return new LoadResult(data, LoadSource.Current, SaveError.None);
            if (TryLoad(Path.Combine(directory, BackupName), out data)) return new LoadResult(data, LoadSource.Backup, SaveError.Corrupt);
            var corrupt = File.Exists(Path.Combine(directory, CurrentName)) || File.Exists(Path.Combine(directory, BackupName));
            return new LoadResult(SaveDataV1.CreateDefaults(), LoadSource.Defaults, corrupt ? SaveError.Corrupt : SaveError.None);
        }

        public SaveResult Save(SaveDataV1 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            try
            {
                Directory.CreateDirectory(directory);
                var current = Path.Combine(directory, CurrentName);
                var backup = Path.Combine(directory, BackupName);
                var temporary = Path.Combine(directory, TemporaryName);
                var payload = JsonUtility.ToJson(SaveDocument.From(data));
                var envelope = new SaveEnvelope { payload = payload, checksum = SaveChecksum.ForCanonicalPayload(payload) };
                writeAllText(temporary, JsonUtility.ToJson(envelope));
                if (!TryLoad(temporary, out _)) return new SaveResult(false, SaveError.Corrupt);
                var currentIsValid = TryLoad(current, out _);
                var backupIsValid = TryLoad(backup, out _);
                replaceTemporary(temporary, current, currentIsValid || !backupIsValid ? backup : null);
                return new SaveResult(true, SaveError.None);
            }
            catch (IOException exception)
            {
                return new SaveResult(false, IsFull(exception) ? SaveError.InsufficientStorage : SaveError.IoFailure);
            }
            catch (UnauthorizedAccessException) { return new SaveResult(false, SaveError.IoFailure); }
        }

        private static bool TryLoad(string path, out SaveDataV1 data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path, Encoding.UTF8));
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || envelope.checksum != SaveChecksum.ForCanonicalPayload(envelope.payload)) return false;
                var document = JsonUtility.FromJson<SaveDocument>(envelope.payload);
                if (document == null ||
                    (document.schemaVersion != 1 && document.schemaVersion != 2 &&
                     document.schemaVersion != 3 && document.schemaVersion != 4))
                    return false;
                data = document.ToData();
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException) { return false; }
        }

        private static bool IsFull(IOException exception)
        {
            var message = exception.Message.ToLowerInvariant();
            return message.Contains("disk") || message.Contains("space") || message.Contains("storage");
        }
        private static void Replace(string temporary, string current, string backup) { if (File.Exists(current)) File.Replace(temporary, current, backup, true); else File.Move(temporary, current); }

        [Serializable] private sealed class SaveEnvelope { public string payload; public string checksum; }
        [Serializable] private sealed class Entry { public string key; public int value; }
        [Serializable] private sealed class StringEntry { public string key; public string value; }
        [Serializable] private sealed class PatrolLoadoutDocument
        {
            public string name;
            public string startingWeaponId;
            public StringEntry[] weaponStyleIds;
            public string difficultyId;
        }
        [Serializable] private sealed class StageClearRecordDocument
        {
            public string stageId;
            public string difficulty;
            public bool victory;
            public float bestElapsed;
            public int bestKills;
            public int bestLevel;
        }
        [Serializable] private sealed class SaveDocument
        {
            public int schemaVersion;
            public int accountExperience;
            public int coins;
            public string selectedStageId;
            public string selectedStageDifficulty;
            public StageClearRecordDocument[] stageClearRecords;
            public string ownedHero;
            public string equippedHero;
            public Entry[] equipmentLevels;
            public Entry[] equipmentQualities;
            public Entry[] equipmentFragments;
            public Entry[] evolutionNodeRanks;
            public Entry[] evolutionSpentCoins;
            public string[] investigationClues;
            public int[] claimedInvestigationMilestones;
            public string[] monsterCompendiumEntries;
            public string[] unlockedHeroes;
            public string[] unlockedDifficulties;
            public string[] unlockedRecipes;
            public string[] unlockedAppearances;
            public Entry[] bestPatrolResults;
            public bool tutorialCompleted;
            public bool accessibilityEnabled;
            public float audioVolume;
            public string[] firstSolutionFlags;
            public string[] selectableInvestigationPolicies;
            public string selectedInvestigationPolicy;
            public Entry[] weaponMasteryPoints;
            public string[] unlockedWeaponStyles;
            public Entry[] commonTrainingRanks;
            public Entry[] commonTrainingSpentCoins;
            public PatrolLoadoutDocument[] patrolLoadouts;
            public int activePatrolLoadoutIndex;

            public static SaveDocument From(SaveDataV1 data) => new SaveDocument
            {
                schemaVersion = ProjectIdentity.SaveSchemaVersion,
                accountExperience = Math.Max(0, Math.Min(
                    AccountProgression.TotalExperienceForLevel(AccountProgression.MaximumLevel),
                    data.AccountExperience)),
                coins = Math.Max(0, data.Coins),
                selectedStageId = data.SelectedStageId,
                selectedStageDifficulty = data.SelectedStageDifficulty,
                stageClearRecords = StageClearRecords(data.StageClearRecords),
                ownedHero = data.OwnedHero,
                equippedHero = data.EquippedHero,
                equipmentLevels = Entries(data.EquipmentLevels),
                equipmentQualities = Entries(data.EquipmentQualities),
                equipmentFragments = Entries(data.EquipmentFragments),
                evolutionNodeRanks = Entries(data.EvolutionNodeRanks),
                evolutionSpentCoins = Entries(data.EvolutionSpentCoins),
                investigationClues = Sorted(data.InvestigationClues),
                claimedInvestigationMilestones = data.ClaimedInvestigationMilestones.OrderBy(value => value).ToArray(),
                monsterCompendiumEntries = Sorted(data.MonsterCompendiumEntries),
                unlockedHeroes = Sorted(data.UnlockedHeroes),
                unlockedDifficulties = Sorted(data.UnlockedDifficulties),
                unlockedRecipes = Sorted(data.UnlockedRecipes),
                unlockedAppearances = Sorted(data.UnlockedAppearances),
                bestPatrolResults = Entries(data.BestPatrolResults),
                tutorialCompleted = data.TutorialCompleted,
                accessibilityEnabled = data.AccessibilityEnabled,
                audioVolume = data.AudioVolume,
                firstSolutionFlags = Sorted(data.FirstSolutionFlags),
                selectableInvestigationPolicies = Sorted(data.SelectableInvestigationPolicies),
                selectedInvestigationPolicy = data.SelectedInvestigationPolicy,
                weaponMasteryPoints = Entries(data.WeaponMasteryPoints),
                unlockedWeaponStyles = Sorted(data.UnlockedWeaponStyles),
                commonTrainingRanks = Entries(data.CommonTrainingRanks),
                commonTrainingSpentCoins = Entries(data.CommonTrainingSpentCoins),
                patrolLoadouts = data.PatrolLoadouts.Select(FromLoadout).ToArray(),
                activePatrolLoadoutIndex = data.ActivePatrolLoadoutIndex
            };

            public SaveDataV1 ToData()
            {
                var data = SaveDataV1.CreateDefaults();
                data.SchemaVersion = ProjectIdentity.SaveSchemaVersion;
                data.Coins = Math.Max(0, coins);
                data.OwnedHero = ownedHero ?? data.OwnedHero;
                data.EquippedHero = equippedHero ?? data.EquippedHero;
                Overlay(data.EquipmentLevels, equipmentLevels);
                Overlay(data.EquipmentQualities, equipmentQualities);
                Overlay(data.EquipmentFragments, equipmentFragments);
                Overlay(data.EvolutionNodeRanks, evolutionNodeRanks);
                Overlay(data.EvolutionSpentCoins, evolutionSpentCoins);
                data.InvestigationClues = List(investigationClues);
                data.ClaimedInvestigationMilestones = new List<int>(claimedInvestigationMilestones ?? Array.Empty<int>());
                data.MonsterCompendiumEntries = List(monsterCompendiumEntries);
                data.UnlockedHeroes = List(unlockedHeroes);
                data.UnlockedDifficulties = List(unlockedDifficulties);
                data.UnlockedRecipes = List(unlockedRecipes);
                data.UnlockedAppearances = List(unlockedAppearances);
                data.BestPatrolResults = Dictionary(bestPatrolResults);
                data.StageClearRecords = NormalizeStageClearRecords(stageClearRecords, data.BestPatrolResults);
                NormalizeSelectedStage(data);
                data.TutorialCompleted = tutorialCompleted;
                data.AccessibilityEnabled = accessibilityEnabled;
                data.AudioVolume = audioVolume;
                data.FirstSolutionFlags = List(firstSolutionFlags);
                data.SelectableInvestigationPolicies = List(selectableInvestigationPolicies);
                data.SelectedInvestigationPolicy = selectedInvestigationPolicy;
                Overlay(data.WeaponMasteryPoints, weaponMasteryPoints);
                data.UnlockedWeaponStyles = List(unlockedWeaponStyles);
                Overlay(data.CommonTrainingRanks, commonTrainingRanks);
                Overlay(data.CommonTrainingSpentCoins, commonTrainingSpentCoins);
                data.AccountExperience = MigratedAccountExperience(data);
                if (patrolLoadouts != null && patrolLoadouts.Length > 0)
                    data.PatrolLoadouts = NormalizeLoadouts(patrolLoadouts);
                data.ActivePatrolLoadoutIndex = Math.Max(0, Math.Min(data.PatrolLoadouts.Count - 1, activePatrolLoadoutIndex));
                NormalizeStarterWeaponStyles(data);
                return data;
            }

            private static void NormalizeStarterWeaponStyles(SaveDataV1 data)
            {
                AddUnique(data.UnlockedWeaponStyles, WeaponLegacyPathId.HwandoVenom.Value);
                AddUnique(data.UnlockedWeaponStyles, WeaponLegacyPathId.HwandoMoonEclipse.Value);
                foreach (var loadout in data.PatrolLoadouts)
                {
                    var current = loadout.WeaponStyleIds.TryGetValue(
                        WeaponId.HwandoFlyingBlade.Value, out var value)
                        ? value
                        : string.Empty;
                    if (current != WeaponLegacyPathId.HwandoVenom.Value &&
                        current != WeaponLegacyPathId.HwandoMoonEclipse.Value)
                    {
                        loadout.WeaponStyleIds[WeaponId.HwandoFlyingBlade.Value] =
                            WeaponLegacyPathId.HwandoVenom.Value;
                    }
                }
            }

            private static void AddUnique(List<string> values, string value)
            {
                if (!values.Contains(value)) values.Add(value);
            }

            private void NormalizeSelectedStage(SaveDataV1 data)
            {
                var fallback = new StageSelection(StageId.GwigokField, StageDifficulty.Normal);
                if (string.IsNullOrWhiteSpace(selectedStageId) ||
                    !StageDifficultyNames.TryParse(selectedStageDifficulty, out var difficulty))
                {
                    ApplySelection(data, fallback);
                    return;
                }

                var stageId = new StageId(selectedStageId);
                if (!StageCatalog.TryGet(stageId, out var definition))
                {
                    ApplySelection(data, fallback);
                    return;
                }

                var selection = new StageSelection(definition.Id, difficulty);
                if (!StageUnlockRules.IsUnlocked(
                        selection,
                        StageClearRecordData.DomainRecords(data.StageClearRecords)))
                    selection = fallback;
                ApplySelection(data, selection);
            }

            private static void ApplySelection(SaveDataV1 data, StageSelection selection)
            {
                data.SelectedStageId = selection.StageId.Value;
                data.SelectedStageDifficulty = StageDifficultyNames.StorageId(selection.Difficulty);
            }

            private int MigratedAccountExperience(SaveDataV1 data)
            {
                var maximum = AccountProgression.TotalExperienceForLevel(AccountProgression.MaximumLevel);
                if (schemaVersion >= 3)
                    return Math.Max(0, Math.Min(maximum, accountExperience));

                var totalRanks = 0;
                foreach (CommonTrainingId id in Enum.GetValues(typeof(CommonTrainingId)))
                {
                    if (data.CommonTrainingRanks.TryGetValue(id.ToString(), out var rank))
                        totalRanks += Math.Max(0, Math.Min(CommonTrainingProgression.MaximumRankPerTrack, rank));
                }

                var minimumLevel = Math.Max(1, Math.Min(20, (totalRanks + 4) / 5));
                return AccountProgression.TotalExperienceForLevel(minimumLevel);
            }

            private static PatrolLoadoutDocument FromLoadout(PatrolLoadoutData data) => new PatrolLoadoutDocument
            {
                name = data.Name,
                startingWeaponId = data.StartingWeaponId,
                weaponStyleIds = StringEntries(data.WeaponStyleIds),
                difficultyId = data.DifficultyId
            };

            private static StageClearRecordDocument[] StageClearRecords(IEnumerable<StageClearRecordData> records) =>
                (records ?? Array.Empty<StageClearRecordData>())
                .Where(record => record != null)
                .Select(record => new StageClearRecordDocument
                {
                    stageId = record.StageId,
                    difficulty = record.Difficulty,
                    victory = record.Victory,
                    bestElapsed = Math.Max(0f, record.BestElapsed),
                    bestKills = Math.Max(0, record.BestKills),
                    bestLevel = Math.Max(0, record.BestLevel)
                }).ToArray();

            private List<StageClearRecordData> NormalizeStageClearRecords(
                StageClearRecordDocument[] documents,
                Dictionary<string, int> patrolResults)
            {
                var result = new List<StageClearRecordData>();
                foreach (var document in documents ?? Array.Empty<StageClearRecordDocument>())
                {
                    if (document == null || string.IsNullOrWhiteSpace(document.stageId) ||
                        string.IsNullOrWhiteSpace(document.difficulty)) continue;
                    MergeStageRecord(result, new StageClearRecordData
                    {
                        StageId = document.stageId,
                        Difficulty = document.difficulty,
                        Victory = document.victory,
                        BestElapsed = Math.Max(0f, document.bestElapsed),
                        BestKills = Math.Max(0, document.bestKills),
                        BestLevel = Math.Max(0, document.bestLevel)
                    });
                }

                if (schemaVersion < 4 && patrolResults.TryGetValue("victory_kills", out var victoryKills))
                {
                    MergeStageRecord(result, StageClearRecordData.From(StageClearRecord.Victory(
                        new StageSelection(StageId.GwigokField, StageDifficulty.Normal),
                        StagePacingTimeline.CanonicalDurationSeconds,
                        Math.Max(0, victoryKills),
                        0)));
                }
                return result;
            }

            private static void MergeStageRecord(List<StageClearRecordData> records, StageClearRecordData candidate)
            {
                if (!candidate.TryToDomain(out var domainCandidate))
                {
                    records.Add(candidate);
                    return;
                }

                for (var index = 0; index < records.Count; index++)
                {
                    if (!records[index].TryToDomain(out var existing) ||
                        !existing.Selection.Equals(domainCandidate.Selection)) continue;
                    records[index] = StageClearRecordData.From(existing.Merge(domainCandidate));
                    return;
                }
                records.Add(candidate);
            }

            private static List<PatrolLoadoutData> NormalizeLoadouts(PatrolLoadoutDocument[] documents)
            {
                var defaults = SaveDataV1.CreateDefaults().PatrolLoadouts;
                var result = new List<PatrolLoadoutData>(3);
                for (var index = 0; index < 3; index++)
                {
                    if (index >= documents.Length || documents[index] == null)
                    {
                        result.Add(defaults[index].Copy());
                        continue;
                    }

                    var document = documents[index];
                    var loadout = defaults[index].Copy();
                    loadout.Name = string.IsNullOrWhiteSpace(document.name) ? loadout.Name : document.name;
                    loadout.StartingWeaponId = string.IsNullOrWhiteSpace(document.startingWeaponId) ? loadout.StartingWeaponId : document.startingWeaponId;
                    loadout.DifficultyId = string.IsNullOrWhiteSpace(document.difficultyId) ? loadout.DifficultyId : document.difficultyId;
                    foreach (var pair in StringDictionary(document.weaponStyleIds)) loadout.WeaponStyleIds[pair.Key] = pair.Value;
                    result.Add(loadout);
                }
                return result;
            }

            private static Entry[] Entries(Dictionary<string, int> values) => values.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new Entry { key = pair.Key, value = pair.Value }).ToArray();
            private static StringEntry[] StringEntries(Dictionary<string, string> values) => values.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new StringEntry { key = pair.Key, value = pair.Value }).ToArray();
            private static Dictionary<string, int> Dictionary(Entry[] entries) => (entries ?? Array.Empty<Entry>()).Where(entry => entry != null && !string.IsNullOrEmpty(entry.key)).ToDictionary(entry => entry.key, entry => Math.Max(0, entry.value));
            private static Dictionary<string, string> StringDictionary(StringEntry[] entries) => (entries ?? Array.Empty<StringEntry>()).Where(entry => entry != null && !string.IsNullOrEmpty(entry.key)).ToDictionary(entry => entry.key, entry => entry.value ?? string.Empty);
            private static void Overlay(Dictionary<string, int> destination, Entry[] entries) { foreach (var entry in entries ?? Array.Empty<Entry>()) if (entry != null && !string.IsNullOrEmpty(entry.key)) destination[entry.key] = Math.Max(0, entry.value); }
            private static List<string> List(string[] values) => new List<string>(values ?? Array.Empty<string>());
            private static string[] Sorted(IEnumerable<string> values) => (values ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
    }
}
