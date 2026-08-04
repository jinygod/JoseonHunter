using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class PixelOvalGraphic : MaskableGraphic
    {
        private const int SegmentCount = 24;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var bounds = GetPixelAdjustedRect();
            var center = bounds.center;
            var radius = bounds.size * .5f;
            var vertexColor = color;

            vertexHelper.AddVert(center, vertexColor, new Vector2(.5f, .5f));
            for (var index = 0; index < SegmentCount; index++)
            {
                var angle = Mathf.PI * 2f * index / SegmentCount;
                var point = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                vertexHelper.AddVert(point, vertexColor, Vector2.zero);
            }

            for (var index = 0; index < SegmentCount; index++)
            {
                vertexHelper.AddTriangle(0, index + 1, (index + 1) % SegmentCount + 1);
            }
        }
    }
}
