using System;
using System.Collections.Generic;
using JoseonHunter.Runtime.Gameplay;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons.Presentation
{
    /// <summary>Bounded, single-silhouette presentation for the guardian descent potential.</summary>
    public sealed class JangseungGuardianDescentPresenter : IDisposable
    {
        private const int Capacity = 4;
        private const int RingPointCount = 16;
        private const float TelegraphEndsAt = .10f;
        private const float DescentEndsAt = .28f;
        private const float SquashEndsAt = .36f;
        private const float Lifetime = .58f;
        private readonly List<Entry> entries = new List<Entry>(Capacity);
        private readonly Transform root;
        private readonly Material material;
        private bool disposed;

        public JangseungGuardianDescentPresenter(Transform root)
        {
            this.root = root ? root : throw new ArgumentNullException(nameof(root));
            material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Flat Guardian Descent Material",
                mainTexture = Texture2D.whiteTexture
            };
        }

        public void Play(int ownerId, Sprite sprite, Vector2 contact, int sortingOrder)
        {
            if (disposed || sprite == null) return;
            Cancel(ownerId);
            var entry = FirstInactive() ?? Oldest();
            if (entry == null) return;
            entry.Play(ownerId, sprite, contact, sortingOrder);
        }

        public int ActiveSilhouetteCountForTests
        {
            get
            {
                var count = 0;
                foreach (var entry in entries) if (entry.Active) count++;
                return count;
            }
        }
        public bool UsesCroppedGuardianPartsForTests => false;
        public int ActivePaletteColorCountForTests => ActiveSilhouetteCountForTests == 0 ? 0 : 3;
        public bool UsesWhiteOutlineForTests => false;

        public void Tick(float deltaTime)
        {
            if (disposed) return;
            var step = Mathf.Max(0f, deltaTime);
            foreach (var entry in entries) entry.Tick(step);
        }

        public void Cancel(int ownerId)
        {
            foreach (var entry in entries)
                if (entry.Active && entry.OwnerId == ownerId)
                    entry.Clear();
        }

        public void Clear()
        {
            foreach (var entry in entries) entry.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var entry in entries) entry.Dispose();
            entries.Clear();
            DestroyObject(material);
        }

        private Entry FirstInactive()
        {
            foreach (var entry in entries) if (!entry.Active) return entry;
            if (entries.Count >= Capacity) return null;
            var created = new Entry(root, material);
            entries.Add(created);
            return created;
        }

        private Entry Oldest()
        {
            Entry oldest = null;
            foreach (var entry in entries)
                if (oldest == null || entry.Elapsed > oldest.Elapsed)
                    oldest = entry;
            return oldest;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }

        private sealed class Entry : IDisposable
        {
            private readonly GameObject gameObject;
            private readonly SpriteRenderer guardian;
            private readonly LineRenderer shadow;
            private readonly LineRenderer dust;
            private Vector2 contact;
            private Vector3 baseScale;

            public Entry(Transform root, Material material)
            {
                gameObject = new GameObject("Guardian Descent");
                gameObject.transform.SetParent(root, false);
                guardian = gameObject.AddComponent<SpriteRenderer>();
                shadow = CreateRing(gameObject.transform, "Guardian Shadow", material);
                dust = CreateRing(gameObject.transform, "Guardian Dust", material);
                gameObject.SetActive(false);
            }

            public bool Active => gameObject.activeSelf;
            public int OwnerId { get; private set; }
            public float Elapsed { get; private set; }

            public void Play(int ownerId, Sprite sprite, Vector2 position, int sortingOrder)
            {
                OwnerId = ownerId;
                Elapsed = 0f;
                contact = position;
                guardian.sprite = sprite;
                guardian.sortingOrder = sortingOrder;
                guardian.color = Color.white;
                shadow.sortingOrder = sortingOrder - 2;
                dust.sortingOrder = sortingOrder - 1;
                baseScale = ScaleToWorldSize(sprite, 1.05f, 1.35f);
                gameObject.transform.position = new Vector3(contact.x, contact.y, 0f);
                gameObject.SetActive(true);
                ApplyFrame();
            }

            public void Tick(float step)
            {
                if (!Active) return;
                Elapsed += step;
                if (Elapsed >= Lifetime)
                {
                    Clear();
                    return;
                }
                ApplyFrame();
            }

            public void Clear()
            {
                OwnerId = 0;
                Elapsed = 0f;
                guardian.sprite = null;
                gameObject.SetActive(false);
            }

            public void Dispose() => DestroyObject(gameObject);

            private void ApplyFrame()
            {
                var descent = Mathf.InverseLerp(TelegraphEndsAt, DescentEndsAt, Elapsed);
                var y = Mathf.Lerp(1.4f, 0f, EaseOutCubic(descent));
                guardian.transform.localPosition = new Vector3(0f, y, 0f);

                var squash = Elapsed < DescentEndsAt
                    ? 1f
                    : Elapsed < SquashEndsAt
                        ? Mathf.Lerp(1f, .78f, Mathf.InverseLerp(DescentEndsAt, SquashEndsAt, Elapsed))
                        : Mathf.Lerp(.78f, 1f, Mathf.InverseLerp(SquashEndsAt, Lifetime, Elapsed));
                guardian.transform.localScale = new Vector3(baseScale.x / squash, baseScale.y * squash, 1f);

                var fade = Elapsed < SquashEndsAt ? 1f : 1f - Mathf.InverseLerp(SquashEndsAt, Lifetime, Elapsed);
                guardian.color = new Color(1f, 1f, 1f, fade);

                var shadowProgress = Mathf.Clamp01(Elapsed / DescentEndsAt);
                ConfigureRing(shadow, .22f + .38f * shadowProgress,
                    WithAlpha(FlatWardVisualPalette.Outline, .62f * fade), .075f);
                var dustProgress = Mathf.InverseLerp(DescentEndsAt, Lifetime, Elapsed);
                dust.enabled = Elapsed >= DescentEndsAt;
                ConfigureRing(dust, Mathf.Lerp(.25f, .90f, dustProgress),
                    WithAlpha(FlatWardVisualPalette.MainBright, (1f - dustProgress) * .82f), .055f);
            }

            private static LineRenderer CreateRing(Transform parent, string name, Material material)
            {
                var ringObject = new GameObject(name);
                ringObject.transform.SetParent(parent, false);
                var ring = ringObject.AddComponent<LineRenderer>();
                ring.sharedMaterial = material;
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = RingPointCount;
                ring.numCornerVertices = 0;
                ring.numCapVertices = 0;
                return ring;
            }

            private static void ConfigureRing(LineRenderer ring, float radius, Color color, float width)
            {
                ring.enabled = color.a > .001f;
                ring.startColor = ring.endColor = color;
                ring.startWidth = ring.endWidth = width;
                for (var index = 0; index < RingPointCount; index++)
                {
                    var angle = index * Mathf.PI * 2f / RingPointCount;
                    ring.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius * .38f, 0f));
                }
            }

            private static Vector3 ScaleToWorldSize(Sprite sprite, float width, float height) =>
                new Vector3(width / Mathf.Max(.01f, sprite.bounds.size.x),
                    height / Mathf.Max(.01f, sprite.bounds.size.y), 1f);

            private static Color WithAlpha(Color color, float alpha)
            {
                color.a = Mathf.Clamp01(alpha);
                return color;
            }

            private static float EaseOutCubic(float value)
            {
                value = Mathf.Clamp01(value);
                return 1f - Mathf.Pow(1f - value, 3f);
            }
        }
    }
}
