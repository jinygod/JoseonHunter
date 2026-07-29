using System;
using System.Collections.Generic;
using System.IO;
using JoseonHunter.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Editor.Scenes
{
    public static class CombatMotionLibraryBuilder
    {
        public const string AssetPath = "Assets/JoseonHunter/Content/Motion/CombatMotionLibrary.asset";

        private readonly struct Entry
        {
            public Entry(string id, string reference, string idleDirectory, string moveDirectory, float idleFps, float moveFps, MotionWeight weight)
            {
                Id = id;
                Reference = reference;
                IdleDirectory = idleDirectory;
                MoveDirectory = moveDirectory;
                IdleFps = idleFps;
                MoveFps = moveFps;
                Weight = weight;
            }

            public string Id { get; }
            public string Reference { get; }
            public string IdleDirectory { get; }
            public string MoveDirectory { get; }
            public float IdleFps { get; }
            public float MoveFps { get; }
            public MotionWeight Weight { get; }
        }

        [MenuItem("JoseonHunter/Setup/Rebuild Combat Motion Library")]
        public static void RebuildFromMenu()
        {
            BuildAndWireGameplay();
        }

        public static void BuildAndWireGameplay()
        {
            var library = Build();
            const string gameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
            var scene = EditorSceneManager.OpenScene(gameplayScenePath, OpenSceneMode.Single);
            var controller = UnityEngine.Object.FindFirstObjectByType<FirstPlayableController>();
            if (controller == null) throw new InvalidOperationException("Gameplay scene is missing FirstPlayableController.");
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("motionLibrary").objectReferenceValue = library;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, gameplayScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Combat motion library rebuilt and wired to Gameplay.");
        }

        public static CombatMotionLibrary Build()
        {
            EnsureFolder("Assets/JoseonHunter/Content/Motion");
            var asset = AssetDatabase.LoadAssetAtPath<CombatMotionLibrary>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CombatMotionLibrary>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            var entries = new[]
            {
                new Entry("han_yeonhwa",
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png",
                    "Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa/Idle",
                    "Assets/JoseonHunter/Art/Animation/Characters/HanYeonhwa/Walk", 3f, 10f, MotionWeight.Light),
                new Entry("bandit",
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/bandit.png", null,
                    "Assets/JoseonHunter/Art/Animation/Enemies/Bandit/Walk", 2.4f, 7.5f, MotionWeight.Medium),
                new Entry("dokkaebi",
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/dokkaebi.png", null,
                    "Assets/JoseonHunter/Art/Animation/Enemies/Dokkaebi/Walk", 2.2f, 6.5f, MotionWeight.Medium),
                new Entry("plague_rat",
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/plague_rat.png", null,
                    "Assets/JoseonHunter/Art/Animation/Enemies/PlagueRat/Walk", 3f, 10f, MotionWeight.Light),
                new Entry("sakkat_specter",
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/sakkat_specter.png", null,
                    "Assets/JoseonHunter/Art/Animation/Enemies/SakkatSpecter/Walk", 2f, 5.5f, MotionWeight.Medium),
                new Entry("vengeful_spirit",
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/vengeful_spirit.png", null,
                    "Assets/JoseonHunter/Art/Animation/Enemies/VengefulSpirit/Walk", 2f, 5.8f, MotionWeight.Medium),
                new Entry("dokkaebi_captain",
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Elites/dokkaebi_captain.png",
                    "Assets/JoseonHunter/Art/Animation/Elites/DokkaebiCaptain/Idle",
                    "Assets/JoseonHunter/Art/Animation/Elites/DokkaebiCaptain/Walk", 2.2f, 5.5f, MotionWeight.Heavy),
                new Entry("fallen_general",
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/fallen_general.png",
                    "Assets/JoseonHunter/Art/Animation/Bosses/FallenGeneral/Idle",
                    "Assets/JoseonHunter/Art/Animation/Bosses/FallenGeneral/Walk", 1.8f, 4.5f, MotionWeight.Heavy)
            };

            var sets = new CombatMotionSet[entries.Length];
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var reference = AssetDatabase.LoadAssetAtPath<Sprite>(entry.Reference);
                if (reference == null) throw new InvalidOperationException($"Missing combat motion reference: {entry.Reference}");
                var idle = LoadFrames(entry.IdleDirectory, reference);
                var move = LoadFrames(entry.MoveDirectory, reference);
                sets[index] = new CombatMotionSet();
                sets[index].Configure(entry.Id, reference, idle, move, entry.IdleFps, entry.MoveFps, entry.Weight);
            }

            asset.Configure(sets);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Sprite[] LoadFrames(string assetDirectory, Sprite fallback)
        {
            if (string.IsNullOrWhiteSpace(assetDirectory) || !AssetDatabase.IsValidFolder(assetDirectory))
            {
                return new[] { fallback };
            }

            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { assetDirectory });
            var frames = new List<Sprite>(guids.Length);
            Array.Sort(guids, (left, right) => string.CompareOrdinal(
                AssetDatabase.GUIDToAssetPath(left),
                AssetDatabase.GUIDToAssetPath(right)));
            foreach (var guid in guids)
            {
                var frame = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (frame != null) frames.Add(frame);
            }

            return frames.Count == 0 ? new[] { fallback } : frames.ToArray();
        }

        private static void EnsureFolder(string assetPath)
        {
            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }
}
