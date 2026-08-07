using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    /// <summary>One reusable warning line per ranged enemy; built at spawn and never allocated during Tick.</summary>
    public sealed class EnemyAttackPresenter : IDisposable
    {
        private readonly GameObject root;
        private readonly LineRenderer border;
        private readonly LineRenderer pulse;
        private readonly Material material;

        public EnemyAttackPresenter(Transform parent)
        {
            root = new GameObject("Enemy Attack Warning");
            root.transform.SetParent(parent, false);
            var shader = Shader.Find("Sprites/Default");
            material = shader == null ? null : new Material(shader) { name = "Enemy Warning Material" };
            border = CreateLine("Dark Warning Border", 5, .32f);
            pulse = CreateLine("Red Warning Pulse", 6, .16f);
            Hide();
        }

        public bool IsVisible => root != null && root.activeSelf;

        public void ShowLine(Vector2 start, Vector2 end, float time)
        {
            if (root == null) return;
            root.SetActive(true);
            var first = new Vector3(start.x, start.y, 0f);
            var second = new Vector3(end.x, end.y, 0f);
            border.SetPosition(0, first);
            border.SetPosition(1, second);
            pulse.SetPosition(0, first);
            pulse.SetPosition(1, second);
            border.startColor = border.endColor = new Color(.22f, .025f, .02f, .9f);
            var alpha = Mathf.Lerp(.28f, .72f, .5f + .5f * Mathf.Sin(time * 11f));
            pulse.startColor = pulse.endColor = new Color(.82f, .11f, .04f, alpha);
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

        private LineRenderer CreateLine(string objectName, int sortingOrder, float width)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(root.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = line.endWidth = width;
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.sortingOrder = sortingOrder;
            line.sharedMaterial = material;
            return line;
        }
    }
}
