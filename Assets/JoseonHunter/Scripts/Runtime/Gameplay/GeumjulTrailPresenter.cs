using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    /// <summary>Owns the visual-only representation of the player's geumjul trail.</summary>
    public sealed class GeumjulTrailPresenter : MonoBehaviour
    {
        private const int MaximumKnots = 10;
        private const float ClosureFrameDuration = .1f;
        private const float KnotMinimumWorldSpacing = 1.1f;
        private const float AnchorMaximumWorldSize = .42f;
        private const float KnotMaximumWorldSize = .28f;
        private const float ClosurePolygonCoverage = .72f;
        private readonly List<Vector2> displayedPoints = new List<Vector2>();
        private readonly List<SpriteRenderer> knotPool = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> closurePool = new List<SpriteRenderer>();

        private JangseungGeumjulVisualLibrary visuals;
        private Transform visualRoot;
        private Material ropeMaterial;
        private LineRenderer outlineLine;
        private LineRenderer ropeLine;
        private SpriteRenderer anchor;
        private Coroutine closureAnimation;
        private bool closureReady;
        private int sortingOrder;

        public int ActiveKnotCountForTests { get; private set; }
        public int ActiveClosureVisualCountForTests { get; private set; }
        public bool HasAnchorForTests => anchor != null && anchor.gameObject.activeSelf;
        public bool IsClosureReadyForTests => closureReady;
        public Material CachedMaterialForTests => ropeMaterial;
        public JangseungGeumjulVisualLibrary ConfiguredVisualLibraryForTests => visuals;
        public float AnchorWorldSizeForTests => WorldSize(anchor);
        public float LargestActiveKnotWorldSizeForTests
        {
            get
            {
                var largest = 0f;
                foreach (var knot in knotPool) if (knot.gameObject.activeSelf) largest = Mathf.Max(largest, WorldSize(knot));
                return largest;
            }
        }
        public float ClosureTargetWorldSizeForTests { get; private set; }
        public float ActiveClosureWorldSizeForTests
        {
            get
            {
                foreach (var closure in closurePool) if (closure.gameObject.activeSelf) return WorldSize(closure);
                return 0f;
            }
        }

        public void Configure(JangseungGeumjulVisualLibrary library, Transform root, int order)
        {
            visuals = library;
            visualRoot = root != null ? root : transform;
            sortingOrder = order;
            EnsureVisuals();
            Clear();
        }

        public void SetTrail(IReadOnlyList<Vector2> points, float closureDistance)
        {
            if (points == null) points = Array.Empty<Vector2>();
            var changed = HasChanged(points);
            if (changed)
            {
                UpdateLines(points);
                UpdateAnchor(points);
                UpdatePooledKnots(points);
                FadeOldSegments(points);
                displayedPoints.Clear();
                for (var index = 0; index < points.Count; index++) displayedPoints.Add(points[index]);
            }

            SetClosureReady(points.Count >= 16 &&
                Vector2.Distance(points[0], points[points.Count - 1]) <= closureDistance);
        }

        public void PlayClosure(IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count == 0 || visuals == null || visuals.GeumjulClosureFrames.Length == 0) return;
            ClearTrailVisuals();
            if (closureAnimation != null) StopCoroutine(closureAnimation);
            var targetWorldSize = ClosureTargetWorldSize(polygon);
            ClosureTargetWorldSizeForTests = targetWorldSize;
            closureAnimation = StartCoroutine(PlayClosureFrames(Centroid(polygon), targetWorldSize));
        }

        public void Clear()
        {
            if (closureAnimation != null)
            {
                StopCoroutine(closureAnimation);
                closureAnimation = null;
            }

            ClearTrailVisuals();
            ReleaseClosureVisuals();
        }

        private void OnDestroy()
        {
            Clear();
            if (ropeMaterial != null) Destroy(ropeMaterial);
        }

        private void Update()
        {
            if (anchor != null && anchor.gameObject.activeSelf && closureReady)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 9f) * .12f;
                anchor.transform.localScale = Vector3.one * AnchorScale() * pulse;
            }
        }

        private void EnsureVisuals()
        {
            if (ropeMaterial == null)
            {
                ropeMaterial = new Material(Shader.Find("Sprites/Default"));
                if (visuals != null) ropeMaterial.mainTexture = visuals.GeumjulRopeTexture;
            }

            if (outlineLine == null) outlineLine = CreateLine("Geumjul Outline", .072f, sortingOrder);
            if (ropeLine == null) ropeLine = CreateLine("Geumjul Rope", .042f, sortingOrder + 1);
            if (anchor == null)
            {
                anchor = CreateSprite("Geumjul Anchor", sortingOrder + 2);
                anchor.sprite = visuals != null ? visuals.GeumjulAnchor : null;
            }
        }

        private LineRenderer CreateLine(string name, float width, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(visualRoot, false);
            line.useWorldSpace = true;
            line.sharedMaterial = ropeMaterial;
            line.textureMode = LineTextureMode.Tile;
            line.alignment = LineAlignment.View;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.sortingOrder = order;
            return line;
        }

        private SpriteRenderer CreateSprite(string name, int order)
        {
            var spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(visualRoot, false);
            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            spriteObject.SetActive(false);
            return renderer;
        }

        private bool HasChanged(IReadOnlyList<Vector2> points)
        {
            if (displayedPoints.Count != points.Count) return true;
            for (var index = 0; index < points.Count; index++)
                if (displayedPoints[index] != points[index]) return true;
            return false;
        }

        private void UpdateLines(IReadOnlyList<Vector2> points)
        {
            EnsureVisuals();
            outlineLine.positionCount = points.Count;
            ropeLine.positionCount = points.Count;
            for (var index = 0; index < points.Count; index++)
            {
                var position = new Vector3(points[index].x, points[index].y, 0f);
                outlineLine.SetPosition(index, position);
                ropeLine.SetPosition(index, position);
            }
        }

        private void UpdateAnchor(IReadOnlyList<Vector2> points)
        {
            if (anchor == null) return;
            var active = points.Count > 0 && anchor.sprite != null;
            anchor.gameObject.SetActive(active);
            if (!active) return;
            anchor.transform.position = points[0];
            anchor.transform.localScale = Vector3.one * AnchorScale();
        }

        private void UpdatePooledKnots(IReadOnlyList<Vector2> points)
        {
            var activeCount = 0;
            var lastKnot = Vector2.positiveInfinity;
            for (var index = 1; index < points.Count && activeCount < MaximumKnots; index++)
            {
                if (activeCount > 0 && Vector2.Distance(lastKnot, points[index]) < KnotMinimumWorldSpacing) continue;
                var knot = KnotAt(activeCount++);
                knot.transform.position = points[index];
                var variants = visuals != null ? visuals.GeumjulKnotVariants : Array.Empty<Sprite>();
                knot.sprite = variants.Length == 0 ? null : variants[(activeCount - 1) % variants.Length];
                knot.transform.localScale = Vector3.one * ScaleForMaximumWorldSize(knot.sprite, KnotMaximumWorldSize);
                knot.gameObject.SetActive(knot.sprite != null);
                lastKnot = points[index];
            }

            for (var index = activeCount; index < knotPool.Count; index++) knotPool[index].gameObject.SetActive(false);
            ActiveKnotCountForTests = activeCount;
        }

        private SpriteRenderer KnotAt(int index)
        {
            while (knotPool.Count <= index) knotPool.Add(CreateSprite("Geumjul Knot", sortingOrder + 2));
            return knotPool[index];
        }

        private void SetClosureReady(bool ready)
        {
            closureReady = ready;
            if (anchor != null && !ready) anchor.transform.localScale = Vector3.one * AnchorScale();
        }

        private void FadeOldSegments(IReadOnlyList<Vector2> points)
        {
            var oldAlpha = points.Count > 1 ? .3f : .9f;
            outlineLine.startColor = new Color(.10f, .035f, .015f, oldAlpha);
            outlineLine.endColor = new Color(.10f, .035f, .015f, .95f);
            ropeLine.startColor = new Color(.68f, .33f, .06f, oldAlpha);
            ropeLine.endColor = new Color(1f, .78f, .23f, .95f);
        }

        private IEnumerator PlayClosureFrames(Vector2 centroid, float targetWorldSize)
        {
            var frames = visuals.GeumjulClosureFrames;
            for (var index = 0; index < frames.Length; index++)
            {
                var visual = ClosureAt(index);
                visual.sprite = frames[index];
                visual.transform.position = centroid;
                var frameScale = ScaleForMaximumWorldSize(visual.sprite, targetWorldSize);
                visual.transform.localScale = Vector3.one * (frameScale * (1f + index * .08f));
                visual.gameObject.SetActive(visual.sprite != null);
                ActiveClosureVisualCountForTests = visual.sprite != null ? 1 : 0;
                yield return new WaitForSeconds(ClosureFrameDuration);
                visual.gameObject.SetActive(false);
            }

            ActiveClosureVisualCountForTests = 0;
            ClosureTargetWorldSizeForTests = 0f;
            closureAnimation = null;
        }

        private SpriteRenderer ClosureAt(int index)
        {
            while (closurePool.Count <= index) closurePool.Add(CreateSprite("Geumjul Closure", sortingOrder + 3));
            return closurePool[index];
        }

        private void ClearTrailVisuals()
        {
            displayedPoints.Clear();
            closureReady = false;
            ActiveKnotCountForTests = 0;
            if (outlineLine != null) outlineLine.positionCount = 0;
            if (ropeLine != null) ropeLine.positionCount = 0;
            if (anchor != null)
            {
                anchor.transform.localScale = Vector3.one;
                anchor.gameObject.SetActive(false);
            }
            foreach (var knot in knotPool) knot.gameObject.SetActive(false);
        }

        private void ReleaseClosureVisuals()
        {
            ActiveClosureVisualCountForTests = 0;
            ClosureTargetWorldSizeForTests = 0f;
            foreach (var closure in closurePool) closure.gameObject.SetActive(false);
        }

        private float AnchorScale() => ScaleForMaximumWorldSize(anchor != null ? anchor.sprite : null, AnchorMaximumWorldSize);

        private static float ScaleForMaximumWorldSize(Sprite sprite, float maximumWorldSize)
        {
            if (sprite == null) return 1f;
            var sourceSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return sourceSize <= .0001f ? 1f : maximumWorldSize / sourceSize;
        }

        private static float WorldSize(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return 0f;
            var size = renderer.sprite.bounds.size;
            return Mathf.Max(size.x * Mathf.Abs(renderer.transform.lossyScale.x), size.y * Mathf.Abs(renderer.transform.lossyScale.y));
        }

        private static float ClosureTargetWorldSize(IReadOnlyList<Vector2> polygon)
        {
            var minimum = polygon[0];
            var maximum = polygon[0];
            for (var index = 1; index < polygon.Count; index++)
            {
                minimum = Vector2.Min(minimum, polygon[index]);
                maximum = Vector2.Max(maximum, polygon[index]);
            }

            return Mathf.Max(.01f, Mathf.Max(maximum.x - minimum.x, maximum.y - minimum.y) * ClosurePolygonCoverage);
        }

        private static Vector2 Centroid(IReadOnlyList<Vector2> polygon)
        {
            var sum = Vector2.zero;
            for (var index = 0; index < polygon.Count; index++) sum += polygon[index];
            return sum / polygon.Count;
        }
    }
}
