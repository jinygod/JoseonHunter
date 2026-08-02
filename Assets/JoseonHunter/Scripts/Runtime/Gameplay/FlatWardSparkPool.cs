using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public sealed class FlatWardSparkPool : IDisposable
    {
        private const float Lifetime = .32f;
        private const float DiamondWorldSize = .11f;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private readonly List<SparkVisual> sparks = new List<SparkVisual>();
        private readonly Transform root;
        private readonly Material material;
        private readonly int sortingOrder;
        private readonly int capacity;
        private readonly Mesh diamondMesh;
        private bool disposed;

        public FlatWardSparkPool(Transform root, Material material, int sortingOrder, int capacity)
        {
            this.root = root ? root : throw new ArgumentNullException(nameof(root));
            this.material = material ? material : throw new ArgumentNullException(nameof(material));
            this.sortingOrder = sortingOrder;
            this.capacity = Mathf.Max(1, capacity);
            diamondMesh = CreateDiamondMesh();
        }

        public int ActiveCountForTests
        {
            get
            {
                var count = 0;
                foreach (var spark in sparks) if (spark.GameObject.activeSelf) count++;
                return count;
            }
        }

        public int CreatedCountForTests => sparks.Count;
        public bool UsesOnlyApprovedColorsForTests
        {
            get
            {
                foreach (var spark in sparks)
                {
                    if (!spark.GameObject.activeSelf) continue;
                    if (!SameRgb(spark.Color, FlatWardVisualPalette.Main) &&
                        !SameRgb(spark.Color, FlatWardVisualPalette.MainBright)) return false;
                }
                return true;
            }
        }

        public bool HasWhiteContourForTests
        {
            get
            {
                foreach (var spark in sparks)
                    if (spark.GameObject.activeSelf && spark.GameObject.GetComponent<LineRenderer>() != null) return true;
                return false;
            }
        }

        public void PlayBurst(Vector2 origin, int count, float radius)
        {
            if (disposed) return;
            var requested = Mathf.Clamp(count, 0, capacity);
            for (var index = 0; index < requested; index++)
            {
                var spark = FirstInactive();
                if (spark == null) break;
                var angle = (index + .5f) * Mathf.PI * 2f / Mathf.Max(1, requested);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                spark.GameObject.transform.position = origin + direction * (Mathf.Max(0f, radius) * .55f);
                spark.GameObject.transform.localScale = Vector3.one * DiamondWorldSize;
                spark.Velocity = direction * Mathf.Max(.01f, radius) / Lifetime;
                spark.Remaining = Lifetime;
                spark.Color = FlatWardVisualPalette.MainBright;
                ApplyColor(spark);
                spark.GameObject.SetActive(true);
            }
        }

        public void Tick(float deltaTime)
        {
            if (disposed) return;
            var step = Mathf.Max(0f, deltaTime);
            foreach (var spark in sparks)
            {
                if (!spark.GameObject.activeSelf) continue;
                spark.Remaining -= step;
                if (spark.Remaining <= 0f)
                {
                    spark.GameObject.SetActive(false);
                    continue;
                }

                spark.GameObject.transform.position += (Vector3)(spark.Velocity * step);
                var normalized = spark.Remaining / Lifetime;
                spark.GameObject.transform.localScale = Vector3.one * (DiamondWorldSize * (.55f + .45f * normalized));
                var color = FlatWardVisualPalette.MainBright;
                color.a *= normalized;
                spark.Color = color;
                ApplyColor(spark);
            }
        }

        public void Clear()
        {
            foreach (var spark in sparks)
            {
                spark.Remaining = 0f;
                spark.GameObject.SetActive(false);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var spark in sparks) DestroyObject(spark.GameObject);
            sparks.Clear();
            DestroyObject(diamondMesh);
        }

        private SparkVisual FirstInactive()
        {
            foreach (var spark in sparks) if (!spark.GameObject.activeSelf) return spark;
            if (sparks.Count >= capacity) return null;

            var gameObject = new GameObject("Flat Ward Spark");
            gameObject.transform.SetParent(root, false);
            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = diamondMesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            gameObject.SetActive(false);
            var created = new SparkVisual(gameObject, renderer);
            sparks.Add(created);
            return created;
        }

        private static Mesh CreateDiamondMesh()
        {
            var mesh = new Mesh { name = "Flat Ward Diamond" };
            mesh.vertices = new[]
            {
                new Vector3(0f, .5f, 0f), new Vector3(.5f, 0f, 0f),
                new Vector3(0f, -.5f, 0f), new Vector3(-.5f, 0f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(.5f, 1f), new Vector2(1f, .5f),
                new Vector2(.5f, 0f), new Vector2(0f, .5f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void ApplyColor(SparkVisual spark)
        {
            spark.Properties.SetColor(ColorProperty, spark.Color);
            spark.Renderer.SetPropertyBlock(spark.Properties);
        }

        private static bool SameRgb(Color left, Color right) =>
            Mathf.Approximately(left.r, right.r) && Mathf.Approximately(left.g, right.g) && Mathf.Approximately(left.b, right.b);

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }

        private sealed class SparkVisual
        {
            public SparkVisual(GameObject gameObject, MeshRenderer renderer)
            {
                GameObject = gameObject;
                Renderer = renderer;
                Properties = new MaterialPropertyBlock();
            }

            public GameObject GameObject { get; }
            public MeshRenderer Renderer { get; }
            public MaterialPropertyBlock Properties { get; }
            public Vector2 Velocity { get; set; }
            public float Remaining { get; set; }
            public Color Color { get; set; }
        }
    }
}
