using System;
using JoseonHunter.Domain.Combat;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    /// <summary>Reusable low-color warning geometry rendered beneath combatants.</summary>
    public sealed class BossTelegraphPresenter : IDisposable
    {
        private const int CircleSegments = 40;
        private readonly GameObject root;
        private readonly LineRenderer outer;
        private readonly LineRenderer inner;
        private readonly Material material;

        public BossTelegraphPresenter(Transform parent)
        {
            root = new GameObject("Boss Warning Telegraph");
            root.transform.SetParent(parent, false);
            var shader = Shader.Find("Sprites/Default");
            material = shader == null ? null : new Material(shader) { name = "Boss Warning Material" };
            outer = CreateLine("Dark Crimson Border", root.transform, 5);
            inner = CreateLine("Muted Red Pulse", root.transform, 6);
            Hide();
        }

        public bool IsVisible => root != null && root.activeSelf;

        public void Show(BossAttackKind kind, Vector2 bossPosition, Vector2 lockedTarget, float bodyScale, float time)
        {
            if (root == null) return;
            root.SetActive(true);
            var pulse = .5f + .5f * Mathf.Sin(time * 10f);
            outer.startColor = outer.endColor = new Color(.32f, .015f, .025f, .82f);
            inner.startColor = inner.endColor = new Color(.78f, .04f, .07f, Mathf.Lerp(.18f, .42f, pulse));
            if (kind == BossAttackKind.BloodCharge || kind == BossAttackKind.TripleCharge ||
                kind == BossAttackKind.ShieldPush)
            {
                ConfigureCorridor(bossPosition, lockedTarget);
                return;
            }

            var radius = kind == BossAttackKind.SpiritVolley || kind == BossAttackKind.Rockfall
                ? Mathf.Max(1.8f, bodyScale * 1.45f)
                : Mathf.Max(1.9f, bodyScale * 1.2f);
            ConfigureCircle(kind == BossAttackKind.SpiritVolley || kind == BossAttackKind.Rockfall
                ? bossPosition : lockedTarget, radius);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        public void Dispose()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            if (material != null) UnityEngine.Object.Destroy(material);
        }

        private LineRenderer CreateLine(string objectName, Transform parent, int sortingOrder)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.sortingOrder = sortingOrder;
            line.sharedMaterial = material;
            return line;
        }

        private void ConfigureCorridor(Vector2 start, Vector2 end)
        {
            outer.loop = false;
            inner.loop = false;
            outer.positionCount = inner.positionCount = 2;
            outer.startWidth = outer.endWidth = 1.5f;
            inner.startWidth = inner.endWidth = 1.24f;
            var first = new Vector3(start.x, start.y, 0f);
            var second = new Vector3(end.x, end.y, 0f);
            outer.SetPosition(0, first);
            outer.SetPosition(1, second);
            inner.SetPosition(0, first);
            inner.SetPosition(1, second);
        }

        private void ConfigureCircle(Vector2 center, float radius)
        {
            outer.loop = true;
            inner.loop = true;
            outer.positionCount = inner.positionCount = CircleSegments;
            outer.startWidth = outer.endWidth = .18f;
            inner.startWidth = inner.endWidth = .09f;
            for (var index = 0; index < CircleSegments; index++)
            {
                var angle = Mathf.PI * 2f * index / CircleSegments;
                var point = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    0f);
                outer.SetPosition(index, point);
                inner.SetPosition(index, point);
            }
        }
    }
}
