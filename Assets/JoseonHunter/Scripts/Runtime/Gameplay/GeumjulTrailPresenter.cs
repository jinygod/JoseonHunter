using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    /// <summary>Owns the visual-only, flat-color representation of the player's geumjul trail.</summary>
    public sealed class GeumjulTrailPresenter : MonoBehaviour
    {
        private const float AnchorMaximumWorldSize = .34f;
        private const float ClosureDuration = .55f;
        private const int ClosureSparkCapacity = 8;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private readonly List<Vector2> displayedPoints = new List<Vector2>();
        private readonly List<Vector3> closureVertices = new List<Vector3>(96);
        private readonly List<int> closureTriangles = new List<int>(282);
        private readonly List<int> triangulationIndices = new List<int>(96);
        private MaterialPropertyBlock closureProperties;

        private JangseungGeumjulVisualLibrary visuals;
        private Transform visualRoot;
        private Material ropeMaterial;
        private LineRenderer outlineLine;
        private LineRenderer ropeLine;
        private LineRenderer anchor;
        private Mesh closureMesh;
        private MeshRenderer closureFill;
        private FlatWardSparkPool closureSparks;
        private Coroutine closureAnimation;
        private bool closureReady;
        private int sortingOrder;

        public int ActiveClosureVisualCountForTests { get; private set; }
        public bool HasAnchorForTests => anchor != null && anchor.gameObject.activeSelf;
        public bool IsClosureReadyForTests => closureReady;
        public Material CachedMaterialForTests => ropeMaterial;
        public JangseungGeumjulVisualLibrary ConfiguredVisualLibraryForTests => visuals;
        public float AnchorWorldSizeForTests => anchor == null || !anchor.gameObject.activeSelf
            ? 0f
            : AnchorMaximumWorldSize * Mathf.Max(Mathf.Abs(anchor.transform.lossyScale.x), Mathf.Abs(anchor.transform.lossyScale.y));
        public bool UsesTexturedRopeForTests => ropeMaterial != null &&
            ropeMaterial.mainTexture != null && ropeMaterial.mainTexture != Texture2D.whiteTexture;
        public int ActiveDecorativeKnotCountForTests => 0;
        public int ClosureMeshVertexCountForTests => closureFill != null && closureFill.gameObject.activeSelf && closureMesh != null
            ? closureMesh.vertexCount
            : 0;
        public int ClosureSparkCountForTests => closureSparks != null ? closureSparks.ActiveCountForTests : 0;
        public bool UsesLegacyClosureSpritesForTests => false;
        public bool UsesOnlyApprovedLineColorsForTests =>
            outlineLine != null && ropeLine != null &&
            SameRgb(outlineLine.startColor, outlineLine.endColor) &&
            SameRgb(ropeLine.startColor, ropeLine.endColor) &&
            !SameRgb(outlineLine.startColor, Color.white) &&
            !SameRgb(ropeLine.startColor, Color.white) &&
            !SameRgb(outlineLine.startColor, ropeLine.startColor);

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
            if (HasChanged(points))
            {
                UpdateLines(points, false);
                UpdateAnchor(points);
                displayedPoints.Clear();
                for (var index = 0; index < points.Count; index++) displayedPoints.Add(points[index]);
            }

            SetClosureReady(points.Count >= 16 &&
                Vector2.Distance(points[0], points[points.Count - 1]) <= closureDistance);
        }

        public void PlayClosure(IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3) return;
            EnsureVisuals();
            if (closureAnimation != null) StopCoroutine(closureAnimation);
            ClearTrailVisuals();
            UpdateLines(polygon, true);
            BuildClosureMesh(polygon);
            closureFill.gameObject.SetActive(closureMesh.vertexCount >= 3);
            ApplyClosureFillColor(WithAlpha(FlatWardVisualPalette.Main, .12f));
            closureSparks.Clear();
            closureSparks.PlayBurst(Centroid(polygon), ClosureSparkCapacity, .32f);
            ActiveClosureVisualCountForTests = 1;
            closureAnimation = StartCoroutine(PlayClosurePulse());
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
            closureSparks?.Dispose();
            closureSparks = null;
            DestroyOwned(closureMesh);
            closureMesh = null;
            DestroyOwned(ropeMaterial);
            ropeMaterial = null;
        }

        private void Update()
        {
            if (anchor == null || !anchor.gameObject.activeSelf || !closureReady) return;
            var pulse = 1f + Mathf.Sin(Time.time * 9f) * .1f;
            anchor.transform.localScale = Vector3.one * pulse;
        }

        private void EnsureVisuals()
        {
            if (ropeMaterial == null)
            {
                ropeMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    name = "Flat Ward Material",
                    mainTexture = Texture2D.whiteTexture
                };
            }

            if (outlineLine == null) outlineLine = CreateLine("Geumjul Outline", FlatWardVisualPalette.OutlineWidth, sortingOrder);
            if (ropeLine == null) ropeLine = CreateLine("Geumjul Main", FlatWardVisualPalette.MainWidth, sortingOrder + 1);
            if (anchor == null)
            {
                anchor = CreateLine("Geumjul Start", FlatWardVisualPalette.MainWidth, sortingOrder + 2);
                anchor.useWorldSpace = false;
                anchor.positionCount = 5;
                var radius = AnchorMaximumWorldSize * .5f;
                anchor.SetPositions(new[]
                {
                    new Vector3(0f, radius, 0f), new Vector3(radius, 0f, 0f),
                    new Vector3(0f, -radius, 0f), new Vector3(-radius, 0f, 0f),
                    new Vector3(0f, radius, 0f)
                });
                anchor.startColor = anchor.endColor = FlatWardVisualPalette.Main;
                anchor.gameObject.SetActive(false);
            }

            if (closureMesh == null)
            {
                closureMesh = new Mesh { name = "Geumjul Closure Fill" };
                closureMesh.MarkDynamic();
            }

            if (closureProperties == null) closureProperties = new MaterialPropertyBlock();

            if (closureFill == null)
            {
                var fillObject = new GameObject("Geumjul Closure Fill");
                fillObject.transform.SetParent(visualRoot, false);
                var filter = fillObject.AddComponent<MeshFilter>();
                filter.sharedMesh = closureMesh;
                closureFill = fillObject.AddComponent<MeshRenderer>();
                closureFill.sharedMaterial = ropeMaterial;
                closureFill.sortingOrder = sortingOrder - 1;
                fillObject.SetActive(false);
            }

            if (closureSparks == null)
                closureSparks = new FlatWardSparkPool(visualRoot, ropeMaterial, sortingOrder + 3, ClosureSparkCapacity);
        }

        private LineRenderer CreateLine(string name, float width, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(visualRoot, false);
            line.useWorldSpace = true;
            line.sharedMaterial = ropeMaterial;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.widthMultiplier = width;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.sortingOrder = order;
            line.startColor = line.endColor = name.Contains("Outline")
                ? FlatWardVisualPalette.Outline
                : FlatWardVisualPalette.Main;
            return line;
        }

        private bool HasChanged(IReadOnlyList<Vector2> points)
        {
            if (displayedPoints.Count != points.Count) return true;
            for (var index = 0; index < points.Count; index++)
                if (displayedPoints[index] != points[index]) return true;
            return false;
        }

        private void UpdateLines(IReadOnlyList<Vector2> points, bool close)
        {
            EnsureVisuals();
            var positionCount = points.Count + (close ? 1 : 0);
            outlineLine.positionCount = positionCount;
            ropeLine.positionCount = positionCount;
            for (var index = 0; index < positionCount; index++)
            {
                var point = points[index % points.Count];
                var position = new Vector3(point.x, point.y, 0f);
                outlineLine.SetPosition(index, position);
                ropeLine.SetPosition(index, position);
            }
            outlineLine.startColor = outlineLine.endColor = FlatWardVisualPalette.Outline;
            ropeLine.startColor = ropeLine.endColor = FlatWardVisualPalette.Main;
        }

        private void UpdateAnchor(IReadOnlyList<Vector2> points)
        {
            var active = points.Count > 0;
            anchor.gameObject.SetActive(active);
            if (!active) return;
            anchor.transform.position = points[0];
            anchor.transform.localScale = Vector3.one;
        }

        private void SetClosureReady(bool ready)
        {
            closureReady = ready;
            if (anchor != null && !ready) anchor.transform.localScale = Vector3.one;
        }

        private IEnumerator PlayClosurePulse()
        {
            var elapsed = 0f;
            yield return null;
            while (elapsed < ClosureDuration)
            {
                var step = Mathf.Max(0f, Time.deltaTime);
                elapsed += step;
                closureSparks.Tick(step);
                var normalized = Mathf.Clamp01(elapsed / ClosureDuration);
                var pulse = Mathf.Sin(normalized * Mathf.PI);
                ropeLine.startColor = ropeLine.endColor = Color.Lerp(
                    FlatWardVisualPalette.Main, FlatWardVisualPalette.MainBright, pulse);
                ApplyClosureFillColor(WithAlpha(FlatWardVisualPalette.Main, .12f * pulse));
                yield return null;
            }

            ClearTrailVisuals();
            ReleaseClosureVisuals();
            closureAnimation = null;
        }

        private void BuildClosureMesh(IReadOnlyList<Vector2> polygon)
        {
            closureVertices.Clear();
            closureTriangles.Clear();
            triangulationIndices.Clear();
            for (var index = 0; index < polygon.Count; index++) closureVertices.Add(polygon[index]);

            var counterClockwise = SignedArea(polygon) > 0f;
            for (var index = 0; index < polygon.Count; index++)
                triangulationIndices.Add(counterClockwise ? index : polygon.Count - 1 - index);

            var safety = polygon.Count * polygon.Count;
            while (triangulationIndices.Count > 2 && safety-- > 0)
            {
                var earFound = false;
                for (var index = 0; index < triangulationIndices.Count; index++)
                {
                    var previous = triangulationIndices[(index - 1 + triangulationIndices.Count) % triangulationIndices.Count];
                    var current = triangulationIndices[index];
                    var next = triangulationIndices[(index + 1) % triangulationIndices.Count];
                    if (!IsEar(previous, current, next, polygon)) continue;
                    closureTriangles.Add(previous);
                    closureTriangles.Add(current);
                    closureTriangles.Add(next);
                    triangulationIndices.RemoveAt(index);
                    earFound = true;
                    break;
                }
                if (!earFound) break;
            }

            closureMesh.Clear();
            closureMesh.SetVertices(closureVertices);
            closureMesh.SetTriangles(closureTriangles, 0);
            closureMesh.RecalculateBounds();
        }

        private bool IsEar(int previous, int current, int next, IReadOnlyList<Vector2> polygon)
        {
            var a = polygon[previous];
            var b = polygon[current];
            var c = polygon[next];
            if (Cross(b - a, c - b) <= .00001f) return false;
            foreach (var candidate in triangulationIndices)
            {
                if (candidate == previous || candidate == current || candidate == next) continue;
                if (PointInTriangle(polygon[candidate], a, b, c)) return false;
            }
            return true;
        }

        private void ApplyClosureFillColor(Color color)
        {
            closureProperties.SetColor(ColorProperty, color);
            closureFill.SetPropertyBlock(closureProperties);
        }

        private void ClearTrailVisuals()
        {
            displayedPoints.Clear();
            closureReady = false;
            if (outlineLine != null) outlineLine.positionCount = 0;
            if (ropeLine != null) ropeLine.positionCount = 0;
            if (anchor != null)
            {
                anchor.transform.localScale = Vector3.one;
                anchor.gameObject.SetActive(false);
            }
        }

        private void ReleaseClosureVisuals()
        {
            ActiveClosureVisualCountForTests = 0;
            closureSparks?.Clear();
            if (closureMesh != null) closureMesh.Clear();
            if (closureFill != null) closureFill.gameObject.SetActive(false);
        }

        private static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            var area = 0f;
            for (var index = 0; index < polygon.Count; index++)
            {
                var current = polygon[index];
                var next = polygon[(index + 1) % polygon.Count];
                area += current.x * next.y - next.x * current.y;
            }
            return area * .5f;
        }

        private static float Cross(Vector2 left, Vector2 right) => left.x * right.y - left.y * right.x;

        private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = Cross(b - a, point - a);
            var bc = Cross(c - b, point - b);
            var ca = Cross(a - c, point - c);
            return ab >= 0f && bc >= 0f && ca >= 0f;
        }

        private static Vector2 Centroid(IReadOnlyList<Vector2> polygon)
        {
            var sum = Vector2.zero;
            for (var index = 0; index < polygon.Count; index++) sum += polygon[index];
            return sum / polygon.Count;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static bool SameRgb(Color left, Color right) =>
            Mathf.Approximately(left.r, right.r) && Mathf.Approximately(left.g, right.g) && Mathf.Approximately(left.b, right.b);

        private static void DestroyOwned(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
