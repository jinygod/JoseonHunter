using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Gameplay;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons.Presentation
{
    /// <summary>Visual-only, pooled representation of finite Jangseung ward sets.</summary>
    public sealed class JangseungWardPresenter : IDisposable
    {
        private const int MaximumPooledSets = 4;
        private readonly Dictionary<int, SetVisual> activeSets = new Dictionary<int, SetVisual>();
        private readonly Stack<SetVisual> pooledSets = new Stack<SetVisual>();
        private readonly WeaponTransientVisualPool crossingPool;
        private readonly WeaponTransientVisualPool dustPool;
        private readonly Transform root;
        private readonly JangseungGeumjulVisualLibrary library;
        private readonly int sortingOrder;
        private bool disposed;

        public JangseungWardPresenter(JangseungGeumjulVisualLibrary library, Transform root, int sortingOrder)
        {
            this.library = library;
            this.root = root ? root : throw new ArgumentNullException(nameof(root));
            this.sortingOrder = sortingOrder;
            crossingPool = new WeaponTransientVisualPool(root);
            dustPool = new WeaponTransientVisualPool(root);
        }

        public int ActiveSetCountForTests => activeSets.Count;
        public int CrossingCountForTests { get; private set; }
        public Vector2 LastCrossingContactForTests { get; private set; }

        public void ShowSet(int setId, IReadOnlyList<Float2> posts, Sprite postSprite)
        {
            if (disposed || activeSets.ContainsKey(setId)) return;
            var set = pooledSets.Count > 0 ? pooledSets.Pop() : new SetVisual(root, sortingOrder);
            activeSets.Add(setId, set);
            Update(set, posts, postSprite);
            PlayDust(posts);
        }

        public void UpdateSet(int setId, IReadOnlyList<Float2> posts, Sprite postSprite)
        {
            if (!disposed && activeSets.TryGetValue(setId, out var set)) Update(set, posts, postSprite);
        }

        public void PlayCrossing(int setId, int segmentIndex, Vector2 start, Vector2 end, Vector2 contact)
        {
            if (disposed || !activeSets.TryGetValue(setId, out var set)) return;
            set.FlashOnly(segmentIndex, .12f);
            crossingPool.Play(FirstFrame(library != null ? library.JangseungCrossingFrames : null), contact, Quaternion.identity,
                Vector3.one * .85f, Color.white, .04f, sortingOrder + 3);
            CrossingCountForTests++;
            LastCrossingContactForTests = contact;
        }

        public void Tick(float deltaTime)
        {
            if (disposed) return;
            crossingPool.Tick(deltaTime);
            dustPool.Tick(deltaTime);
            foreach (var set in activeSets.Values) set.TickFlash(deltaTime);
        }

        public void RetireSet(int setId)
        {
            if (disposed || !activeSets.TryGetValue(setId, out var set)) return;
            activeSets.Remove(setId);
            set.Clear();
            if (pooledSets.Count < MaximumPooledSets) pooledSets.Push(set);
            else set.Dispose();
        }

        public void Clear()
        {
            foreach (var id in new List<int>(activeSets.Keys)) RetireSet(id);
        }

        public void Dispose()
        {
            if (disposed) return;
            Clear();
            disposed = true;
            crossingPool.Dispose();
            dustPool.Dispose();
            while (pooledSets.Count > 0) pooledSets.Pop().Dispose();
        }

        private void Update(SetVisual set, IReadOnlyList<Float2> posts, Sprite postSprite)
        {
            set.Update(posts, postSprite, library != null ? library.GeumjulRopeTexture : null,
                library != null ? library.GeumjulAnchor : null);
        }

        private void PlayDust(IReadOnlyList<Float2> posts)
        {
            var dust = FirstFrame(library != null ? library.JangseungDustFrames : null);
            if (dust == null || posts == null) return;
            foreach (var post in posts)
                dustPool.Play(dust, new Vector3(post.X, post.Y, 0f), Quaternion.identity, Vector3.one * .7f,
                    new Color(1f, .86f, .52f, .75f), .1f, sortingOrder + 2);
        }

        private static Sprite FirstFrame(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;

        private sealed class SetVisual : IDisposable
        {
            private readonly Transform root;
            private readonly int sortingOrder;
            private readonly List<SpriteRenderer> posts = new List<SpriteRenderer>();
            private readonly List<LineRenderer> ropes = new List<LineRenderer>();
            private SpriteRenderer seal;
            private Material ropeMaterial;
            private float flashRemaining;

            public SetVisual(Transform root, int sortingOrder) { this.root = root; this.sortingOrder = sortingOrder; }

            public void Update(IReadOnlyList<Float2> positions, Sprite postSprite, Texture2D ropeTexture, Sprite sealSprite)
            {
                var count = positions != null ? positions.Count : 0;
                Ensure(count, ropeTexture);
                var center = Vector2.zero;
                for (var index = 0; index < count; index++)
                {
                    var position = new Vector3(positions[index].X, positions[index].Y, 0f);
                    posts[index].sprite = postSprite;
                    posts[index].transform.position = position;
                    posts[index].gameObject.SetActive(postSprite != null);
                    center += (Vector2)position;
                }
                for (var index = count; index < posts.Count; index++) posts[index].gameObject.SetActive(false);
                var segmentCount = count == 2 ? 1 : count;
                for (var index = 0; index < ropes.Count; index++)
                {
                    var active = index < segmentCount;
                    ropes[index].gameObject.SetActive(active);
                    if (!active) continue;
                    var endIndex = (index + 1) % count;
                    ropes[index].SetPosition(0, new Vector3(positions[index].X, positions[index].Y, 0f));
                    ropes[index].SetPosition(1, new Vector3(positions[endIndex].X, positions[endIndex].Y, 0f));
                }
                seal.sprite = sealSprite;
                seal.transform.position = count == 0 ? Vector3.zero : (Vector3)(center / count);
                seal.gameObject.SetActive(sealSprite != null && count > 0);
            }

            public void FlashOnly(int segmentIndex, float duration)
            {
                if (segmentIndex >= 0 && segmentIndex < ropes.Count) flashRemaining = Mathf.Max(flashRemaining, duration);
            }

            public void TickFlash(float deltaTime)
            {
                flashRemaining = Mathf.Max(0f, flashRemaining - Mathf.Max(0f, deltaTime));
                var color = flashRemaining > 0f ? new Color(1f, .93f, .55f, .95f) : new Color(.92f, .55f, .16f, .7f);
                foreach (var rope in ropes) rope.startColor = rope.endColor = color;
            }

            public void Clear()
            {
                foreach (var post in posts) post.gameObject.SetActive(false);
                foreach (var rope in ropes) rope.gameObject.SetActive(false);
                if (seal != null) seal.gameObject.SetActive(false);
                flashRemaining = 0f;
            }

            public void Dispose()
            {
                foreach (var post in posts) if (post != null) DestroyObject(post.gameObject);
                foreach (var rope in ropes) if (rope != null) DestroyObject(rope.gameObject);
                if (seal != null) DestroyObject(seal.gameObject);
                if (ropeMaterial != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(ropeMaterial);
                    else UnityEngine.Object.DestroyImmediate(ropeMaterial);
                }
            }

            private static void DestroyObject(GameObject visual)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(visual);
                else UnityEngine.Object.DestroyImmediate(visual);
            }

            private void Ensure(int count, Texture2D ropeTexture)
            {
                if (ropeMaterial == null)
                {
                    ropeMaterial = new Material(Shader.Find("Sprites/Default"));
                    ropeMaterial.mainTexture = ropeTexture;
                }
                while (posts.Count < count) posts.Add(CreateSprite("Jangseung Ward Post", sortingOrder + 2));
                var segmentCount = count == 2 ? 1 : count;
                while (ropes.Count < segmentCount) ropes.Add(CreateRope());
                if (seal == null) { seal = CreateSprite("Jangseung Ward Seal", sortingOrder - 1); seal.color = new Color(1f, .76f, .3f, .16f); }
            }

            private SpriteRenderer CreateSprite(string name, int order)
            {
                var visual = new GameObject(name); visual.transform.SetParent(root, false); visual.SetActive(false);
                var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sortingOrder = order; return renderer;
            }

            private LineRenderer CreateRope()
            {
                var rope = new GameObject("Jangseung Ward Boundary").AddComponent<LineRenderer>();
                rope.transform.SetParent(root, false); rope.useWorldSpace = true; rope.sharedMaterial = ropeMaterial;
                rope.textureMode = LineTextureMode.Tile; rope.widthMultiplier = .08f; rope.numCapVertices = 2; rope.sortingOrder = sortingOrder;
                rope.gameObject.SetActive(false); return rope;
            }
        }
    }
}
