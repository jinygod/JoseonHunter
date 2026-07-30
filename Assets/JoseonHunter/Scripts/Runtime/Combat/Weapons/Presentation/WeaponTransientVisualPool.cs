using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons.Presentation
{
    public sealed class WeaponTransientVisualPool : IDisposable
    {
        private const int MaximumPooledVisuals = 48;
        private readonly Transform root;
        private readonly List<Entry> active = new List<Entry>();
        private readonly Stack<SpriteRenderer> pooled = new Stack<SpriteRenderer>();
        private bool disposed;

        public WeaponTransientVisualPool(Transform root)
        {
            this.root = root ? root : throw new ArgumentNullException(nameof(root));
        }

        public int CreatedCount { get; private set; }
        public int ActiveCount => active.Count;

        public void Play(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float lifetime, int sortingOrder, int ownerId = 0)
        {
            if (disposed || sprite == null) return;

            var renderer = pooled.Count > 0 ? pooled.Pop() : CreateRenderer();
            var transform = renderer.transform;
            transform.SetParent(root, false);
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.gameObject.SetActive(true);
            active.Add(new Entry(renderer, Mathf.Max(0f, lifetime), ownerId));
        }

        public void Tick(float deltaTime)
        {
            if (disposed) return;

            var elapsed = Mathf.Max(0f, deltaTime);
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var entry = active[index];
                entry.RemainingLifetime -= elapsed;
                if (entry.RemainingLifetime > 0f)
                {
                    active[index] = entry;
                    continue;
                }

                active.RemoveAt(index);
                Return(entry.Renderer);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            foreach (var entry in active) Destroy(entry.Renderer);
            active.Clear();
            while (pooled.Count > 0) Destroy(pooled.Pop());
        }

        public void CancelOwner(int ownerId)
        {
            if (disposed) return;
            for (var index = active.Count - 1; index >= 0; index--)
            {
                if (active[index].OwnerId != ownerId) continue;
                var entry = active[index]; active.RemoveAt(index); Return(entry.Renderer);
            }
        }

        public void Clear()
        {
            if (disposed) return;
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var entry = active[index]; active.RemoveAt(index); Return(entry.Renderer);
            }
        }

        private SpriteRenderer CreateRenderer()
        {
            var visual = new GameObject("Weapon Transient Visual");
            visual.transform.SetParent(root, false);
            visual.SetActive(false);
            CreatedCount++;
            return visual.AddComponent<SpriteRenderer>();
        }

        private void Return(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            renderer.sprite = null;
            renderer.gameObject.SetActive(false);
            if (pooled.Count < MaximumPooledVisuals) pooled.Push(renderer);
            else Destroy(renderer);
        }

        private static void Destroy(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(renderer.gameObject);
            else UnityEngine.Object.DestroyImmediate(renderer.gameObject);
        }

        private struct Entry
        {
            public Entry(SpriteRenderer renderer, float remainingLifetime, int ownerId)
            {
                Renderer = renderer;
                RemainingLifetime = remainingLifetime;
                OwnerId = ownerId;
            }

            public SpriteRenderer Renderer;
            public float RemainingLifetime;
            public int OwnerId;
        }
    }
}
