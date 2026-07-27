using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JoseonHunter.Domain.Save;
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
                replaceTemporary(temporary, current, backup);
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
                if (document == null || document.schemaVersion != 1) return false;
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
        [Serializable] private sealed class SaveDocument
        {
            public int schemaVersion; public int coins; public string ownedHero; public string equippedHero; public Entry[] equipmentLevels; public Entry[] equipmentQualities; public Entry[] equipmentFragments; public Entry[] evolutionNodeRanks; public Entry[] evolutionSpentCoins; public string[] investigationClues; public int[] claimedInvestigationMilestones; public string[] monsterCompendiumEntries; public string[] unlockedHeroes; public string[] unlockedDifficulties; public string[] unlockedRecipes; public string[] unlockedAppearances; public Entry[] bestPatrolResults; public bool tutorialCompleted; public bool accessibilityEnabled; public float audioVolume; public string[] firstSolutionFlags;
            public static SaveDocument From(SaveDataV1 data) => new SaveDocument { schemaVersion = 1, coins = Math.Max(0, data.Coins), ownedHero = data.OwnedHero, equippedHero = data.EquippedHero, equipmentLevels = Entries(data.EquipmentLevels), equipmentQualities = Entries(data.EquipmentQualities), equipmentFragments = Entries(data.EquipmentFragments), evolutionNodeRanks = Entries(data.EvolutionNodeRanks), evolutionSpentCoins = Entries(data.EvolutionSpentCoins), investigationClues = data.InvestigationClues.OrderBy(value => value, StringComparer.Ordinal).ToArray(), claimedInvestigationMilestones = data.ClaimedInvestigationMilestones.OrderBy(value => value).ToArray(), monsterCompendiumEntries = data.MonsterCompendiumEntries.OrderBy(value => value, StringComparer.Ordinal).ToArray(), unlockedHeroes = data.UnlockedHeroes.OrderBy(value => value, StringComparer.Ordinal).ToArray(), unlockedDifficulties = data.UnlockedDifficulties.OrderBy(value => value, StringComparer.Ordinal).ToArray(), unlockedRecipes = data.UnlockedRecipes.OrderBy(value => value, StringComparer.Ordinal).ToArray(), unlockedAppearances = data.UnlockedAppearances.OrderBy(value => value, StringComparer.Ordinal).ToArray(), bestPatrolResults = Entries(data.BestPatrolResults), tutorialCompleted = data.TutorialCompleted, accessibilityEnabled = data.AccessibilityEnabled, audioVolume = data.AudioVolume, firstSolutionFlags = data.FirstSolutionFlags.OrderBy(value => value, StringComparer.Ordinal).ToArray() };
            public SaveDataV1 ToData() { var data = SaveDataV1.CreateDefaults(); data.Coins = Math.Max(0, coins); data.OwnedHero = ownedHero ?? data.OwnedHero; data.EquippedHero = equippedHero ?? data.EquippedHero; Overlay(data.EquipmentLevels, equipmentLevels); Overlay(data.EquipmentQualities, equipmentQualities); Overlay(data.EquipmentFragments, equipmentFragments); Overlay(data.EvolutionNodeRanks, evolutionNodeRanks); Overlay(data.EvolutionSpentCoins, evolutionSpentCoins); data.InvestigationClues = List(investigationClues); data.ClaimedInvestigationMilestones = new List<int>(claimedInvestigationMilestones ?? new int[0]); data.MonsterCompendiumEntries = List(monsterCompendiumEntries); data.UnlockedHeroes = List(unlockedHeroes); data.UnlockedDifficulties = List(unlockedDifficulties); data.UnlockedRecipes = List(unlockedRecipes); data.UnlockedAppearances = List(unlockedAppearances); data.BestPatrolResults = Dictionary(bestPatrolResults); data.TutorialCompleted = tutorialCompleted; data.AccessibilityEnabled = accessibilityEnabled; data.AudioVolume = audioVolume; data.FirstSolutionFlags = List(firstSolutionFlags); return data; }
            private static Entry[] Entries(Dictionary<string, int> values) => values.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new Entry { key = pair.Key, value = pair.Value }).ToArray();
            private static System.Collections.Generic.Dictionary<string, int> Dictionary(Entry[] entries) => (entries ?? new Entry[0]).Where(entry => entry != null && !string.IsNullOrEmpty(entry.key)).ToDictionary(entry => entry.key, entry => entry.value);
            private static void Overlay(System.Collections.Generic.Dictionary<string, int> destination, Entry[] entries) { foreach (var entry in entries ?? new Entry[0]) if (entry != null && !string.IsNullOrEmpty(entry.key)) destination[entry.key] = Math.Max(0, entry.value); }
            private static List<string> List(string[] values) => new List<string>(values ?? new string[0]);
        }
    }
}
