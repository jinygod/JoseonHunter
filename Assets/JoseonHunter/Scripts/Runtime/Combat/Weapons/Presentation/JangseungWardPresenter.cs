using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Gameplay;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons.Presentation
{
    /// <summary>Visual-only, pooled, flat-color representation of finite Jangseung ward sets.</summary>
    public sealed class JangseungWardPresenter : IDisposable
    {
        private const int MaximumPooledSets = 4;
        private const float NewestSetAlpha = .78f;
        private const float OlderSetAlpha = .34f;
        private readonly Dictionary<int, SetVisual> activeSets = new Dictionary<int, SetVisual>();
        private readonly List<int> activeOrder = new List<int>(MaximumPooledSets);
        private readonly Stack<SetVisual> pooledSets = new Stack<SetVisual>();
        private readonly Transform root;
        private readonly Material material;
        private readonly FlatWardSparkPool crossingSparks;
        private readonly int sortingOrder;
        private bool disposed;

        public JangseungWardPresenter(JangseungGeumjulVisualLibrary library, Transform root, int sortingOrder)
        {
            this.root = root ? root : throw new ArgumentNullException(nameof(root));
            this.sortingOrder = sortingOrder;
            material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Flat Jangseung Ward Material",
                mainTexture = Texture2D.whiteTexture
            };
            crossingSparks = new FlatWardSparkPool(root, material, sortingOrder + 3, 24);
        }

        public int ActiveSetCountForTests => activeSets.Count;
        public int CrossingCountForTests { get; private set; }
        public Vector2 LastCrossingContactForTests { get; private set; }
        public int ActiveCrossingSparkCountForTests => crossingSparks.ActiveCountForTests;
        public bool UsesTexturedBoundariesForTests => false;
        public int ActiveDecorativeSpriteCountForTests => 0;
        public int ActivePaletteColorCountForTests => activeSets.Count == 0 ? 0 : 3;
        public bool UsesWhiteOutlineForTests => false;
        public bool NewestSetHasFullEmphasisForTests => activeOrder.Count == 0 ||
            Mathf.Approximately(SetMainAlphaForTests(activeOrder[activeOrder.Count - 1]), NewestSetAlpha);
        public float SetMainAlphaForTests(int setId) => activeSets.TryGetValue(setId, out var set) ? set.EmphasisAlpha : 0f;
        public bool PooledPersistentVisualsAreInactiveForTests
        {
            get
            {
                foreach (var set in pooledSets) if (!set.AllVisualsInactive) return false;
                return true;
            }
        }
        public bool IsSegmentFlashingForTests(int setId, int segmentIndex) =>
            activeSets.TryGetValue(setId, out var set) && set.IsSegmentFlashing(segmentIndex);
        public bool HasExactlyOneFlashingSegmentForCapture
        {
            get
            {
                var flashingSegments = 0;
                foreach (var set in activeSets.Values)
                {
                    flashingSegments += set.FlashingSegmentCount;
                    if (flashingSegments > 1) return false;
                }
                return flashingSegments == 1;
            }
        }

        public void ShowSet(int setId, IReadOnlyList<Float2> posts, Sprite postSprite)
        {
            if (disposed || activeSets.ContainsKey(setId)) return;
            var set = pooledSets.Count > 0 ? pooledSets.Pop() : new SetVisual(root, material, sortingOrder);
            activeSets.Add(setId, set);
            activeOrder.Add(setId);
            set.Update(posts, false);
            ApplySetEmphasis();
        }

        public void UpdateSet(int setId, IReadOnlyList<Float2> posts, Sprite postSprite)
        {
            if (!disposed && activeSets.TryGetValue(setId, out var set)) set.Update(posts, true);
        }

        public void SetPostRise(int setId, int postIndex, float progress)
        {
            if (!disposed && activeSets.TryGetValue(setId, out var set)) set.SetPostRise(postIndex, progress);
        }

        public void SetBoundaryAlpha(int setId, int segmentIndex, float alpha)
        {
            if (!disposed && activeSets.TryGetValue(setId, out var set)) set.SetBoundaryAlpha(segmentIndex, alpha);
        }

        public void PlayCrossing(int setId, int segmentIndex, Vector2 start, Vector2 end, Vector2 contact)
        {
            if (disposed || !activeSets.TryGetValue(setId, out var set)) return;
            set.FlashOnly(segmentIndex, .12f);
            crossingSparks.PlayBurst(contact, 3, .18f);
            CrossingCountForTests++;
            LastCrossingContactForTests = contact;
        }

        public void Tick(float deltaTime)
        {
            if (disposed) return;
            crossingSparks.Tick(deltaTime);
            foreach (var set in activeSets.Values) set.Tick(deltaTime);
        }

        public void RetireSet(int setId)
        {
            if (disposed || !activeSets.TryGetValue(setId, out var set)) return;
            activeSets.Remove(setId);
            activeOrder.Remove(setId);
            set.Clear();
            if (pooledSets.Count < MaximumPooledSets) pooledSets.Push(set);
            else set.Dispose();
            if (activeSets.Count == 0) crossingSparks.Clear();
            ApplySetEmphasis();
        }

        public void Clear()
        {
            while (activeOrder.Count > 0) RetireSet(activeOrder[activeOrder.Count - 1]);
            crossingSparks.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            Clear();
            disposed = true;
            crossingSparks.Dispose();
            while (pooledSets.Count > 0) pooledSets.Pop().Dispose();
            DestroyObject(material);
        }

        private void ApplySetEmphasis()
        {
            for (var index = 0; index < activeOrder.Count; index++)
                if (activeSets.TryGetValue(activeOrder[index], out var set))
                    set.SetEmphasis(index == activeOrder.Count - 1 ? NewestSetAlpha : OlderSetAlpha);
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }

        private sealed class SetVisual : IDisposable
        {
            private const float RepositionFadeDuration = .12f;
            private readonly Transform root;
            private readonly Material material;
            private readonly int sortingOrder;
            private readonly List<PostVisual> posts = new List<PostVisual>(4);
            private readonly List<SegmentVisual> segments = new List<SegmentVisual>(4);
            private float repositionRemaining;
            private float repositionFactor = 1f;

            public SetVisual(Transform root, Material material, int sortingOrder)
            {
                this.root = root;
                this.material = material;
                this.sortingOrder = sortingOrder;
            }

            public float EmphasisAlpha { get; private set; } = NewestSetAlpha;
            public bool AllVisualsInactive
            {
                get
                {
                    foreach (var post in posts) if (post.Active) return false;
                    foreach (var segment in segments) if (segment.Active) return false;
                    return true;
                }
            }

            public void Update(IReadOnlyList<Float2> positions, bool allowRepositionFade)
            {
                var count = positions != null ? positions.Count : 0;
                var moved = allowRepositionFade && HasMoved(positions);
                Ensure(count);
                for (var index = 0; index < count; index++)
                    posts[index].SetPosition(new Vector2(positions[index].X, positions[index].Y));
                for (var index = count; index < posts.Count; index++) posts[index].SetActive(false);

                var segmentCount = count == 2 ? 1 : count;
                for (var index = 0; index < segments.Count; index++)
                {
                    var active = index < segmentCount;
                    segments[index].SetActive(active);
                    if (!active) continue;
                    var endIndex = (index + 1) % count;
                    segments[index].SetPositions(
                        new Vector2(positions[index].X, positions[index].Y),
                        new Vector2(positions[endIndex].X, positions[endIndex].Y));
                }

                if (moved)
                {
                    repositionRemaining = RepositionFadeDuration;
                    repositionFactor = .28f;
                }
                RefreshColors();
            }

            public void SetPostRise(int index, float progress)
            {
                if (index < 0 || index >= posts.Count) return;
                posts[index].SetRise(Mathf.Clamp01(progress));
                posts[index].ApplyColor(EmphasisAlpha * repositionFactor);
            }

            public void SetBoundaryAlpha(int index, float alpha)
            {
                if (index < 0 || index >= segments.Count) return;
                segments[index].Visibility = Mathf.Clamp01(alpha);
                segments[index].ApplyColor(EmphasisAlpha * repositionFactor);
            }

            public void SetEmphasis(float alpha)
            {
                EmphasisAlpha = alpha;
                RefreshColors();
            }

            public void FlashOnly(int segmentIndex, float duration)
            {
                if (segmentIndex >= 0 && segmentIndex < segments.Count)
                    segments[segmentIndex].FlashRemaining = Mathf.Max(segments[segmentIndex].FlashRemaining, duration);
                RefreshColors();
            }

            public void Tick(float deltaTime)
            {
                var step = Mathf.Max(0f, deltaTime);
                if (repositionRemaining > 0f)
                {
                    repositionRemaining = Mathf.Max(0f, repositionRemaining - step);
                    repositionFactor = Mathf.Lerp(.28f, 1f, 1f - repositionRemaining / RepositionFadeDuration);
                }
                foreach (var segment in segments)
                    segment.FlashRemaining = Mathf.Max(0f, segment.FlashRemaining - step);
                RefreshColors();
            }

            public bool IsSegmentFlashing(int index) => index >= 0 && index < segments.Count && segments[index].FlashRemaining > 0f;
            public int FlashingSegmentCount
            {
                get
                {
                    var count = 0;
                    foreach (var segment in segments) if (segment.FlashRemaining > 0f) count++;
                    return count;
                }
            }

            public void Clear()
            {
                foreach (var post in posts) post.SetActive(false);
                foreach (var segment in segments)
                {
                    segment.SetActive(false);
                    segment.FlashRemaining = 0f;
                    segment.Visibility = 1f;
                }
                repositionRemaining = 0f;
                repositionFactor = 1f;
            }

            public void Dispose()
            {
                foreach (var post in posts) post.Dispose();
                foreach (var segment in segments) segment.Dispose();
                posts.Clear();
                segments.Clear();
            }

            private bool HasMoved(IReadOnlyList<Float2> positions)
            {
                if (positions == null || positions.Count != ActivePostCount()) return false;
                for (var index = 0; index < positions.Count; index++)
                {
                    var current = posts[index].Position;
                    if ((current - new Vector2(positions[index].X, positions[index].Y)).sqrMagnitude > .0001f) return true;
                }
                return false;
            }

            private int ActivePostCount()
            {
                var count = 0;
                foreach (var post in posts) if (post.Active) count++;
                return count;
            }

            private void Ensure(int count)
            {
                while (posts.Count < count) posts.Add(new PostVisual(root, material, sortingOrder + 2));
                var segmentCount = count == 2 ? 1 : count;
                while (segments.Count < segmentCount) segments.Add(new SegmentVisual(root, material, sortingOrder));
            }

            private void RefreshColors()
            {
                var alpha = EmphasisAlpha * repositionFactor;
                foreach (var post in posts) post.ApplyColor(alpha);
                foreach (var segment in segments) segment.ApplyColor(alpha);
            }
        }

        private sealed class PostVisual : IDisposable
        {
            private readonly GameObject root;
            private readonly LineRenderer body;
            private readonly LineRenderer crossbar;
            private float rise = 1f;

            public PostVisual(Transform parent, Material material, int order)
            {
                root = new GameObject("Flat Jangseung Post");
                root.transform.SetParent(parent, false);
                body = CreateLocalLine(root.transform, "Post Body", material, order, .12f,
                    new Vector3(0f, -.27f, 0f), new Vector3(0f, .25f, 0f));
                crossbar = CreateLocalLine(root.transform, "Post Crossbar", material, order + 1, .055f,
                    new Vector3(-.18f, .12f, 0f), new Vector3(.18f, .12f, 0f));
                root.SetActive(false);
            }

            public bool Active => root.activeSelf;
            public Vector2 Position => root.transform.position;

            public void SetPosition(Vector2 position)
            {
                root.transform.position = position;
                root.SetActive(true);
            }

            public void SetRise(float progress)
            {
                rise = progress;
                root.transform.localScale = new Vector3(.82f + .18f * rise, Mathf.Max(.04f, rise), 1f);
            }

            public void ApplyColor(float alpha)
            {
                var bodyColor = WithAlpha(FlatWardVisualPalette.Outline, FlatWardVisualPalette.Outline.a * alpha * rise);
                var crossbarColor = WithAlpha(FlatWardVisualPalette.Main, alpha * rise);
                body.startColor = body.endColor = bodyColor;
                crossbar.startColor = crossbar.endColor = crossbarColor;
            }

            public void SetActive(bool active) => root.SetActive(active);
            public void Dispose() => DestroyObject(root);
        }

        private sealed class SegmentVisual : IDisposable
        {
            private readonly GameObject root;
            private readonly LineRenderer outline;
            private readonly LineRenderer main;

            public SegmentVisual(Transform parent, Material material, int order)
            {
                root = new GameObject("Flat Jangseung Boundary");
                root.transform.SetParent(parent, false);
                outline = CreateWorldLine(root.transform, "Boundary Outline", material, order, FlatWardVisualPalette.OutlineWidth);
                main = CreateWorldLine(root.transform, "Boundary Main", material, order + 1, FlatWardVisualPalette.MainWidth);
                root.SetActive(false);
            }

            public bool Active => root.activeSelf;
            public float Visibility { get; set; } = 1f;
            public float FlashRemaining { get; set; }

            public void SetPositions(Vector2 start, Vector2 end)
            {
                outline.SetPosition(0, start);
                outline.SetPosition(1, end);
                main.SetPosition(0, start);
                main.SetPosition(1, end);
            }

            public void SetActive(bool active) => root.SetActive(active);

            public void ApplyColor(float alpha)
            {
                var outlineColor = WithAlpha(FlatWardVisualPalette.Outline,
                    FlatWardVisualPalette.Outline.a * alpha * Visibility);
                var source = FlashRemaining > 0f ? FlatWardVisualPalette.MainBright : FlatWardVisualPalette.Main;
                var mainColor = WithAlpha(source, alpha * Visibility);
                outline.startColor = outline.endColor = outlineColor;
                main.startColor = main.endColor = mainColor;
            }

            public void Dispose() => DestroyObject(root);
        }

        private static LineRenderer CreateLocalLine(Transform parent, string name, Material material, int order, float width,
            Vector3 start, Vector3 end)
        {
            var line = CreateWorldLine(parent, name, material, order, width);
            line.useWorldSpace = false;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            return line;
        }

        private static LineRenderer CreateWorldLine(Transform parent, string name, Material material, int order, float width)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.textureMode = LineTextureMode.Stretch;
            line.widthMultiplier = width;
            line.positionCount = 2;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.sortingOrder = order;
            return line;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
