using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ElderCareRoundedPanel : MaskableGraphic
{
    public float cornerRadius = 32f;
    public int cornerSegments = 8;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var rect = GetPixelAdjustedRect();
        var radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
        var segments = Mathf.Max(2, cornerSegments);
        var points = new List<Vector2>();

        AddArc(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, -90f, 0f, segments);
        AddArc(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
        AddArc(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
        AddArc(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);

        var c = (Color32)color;
        vh.AddVert(rect.center, c, Vector2.zero);
        for (var i = 0; i < points.Count; i++)
        {
            vh.AddVert(points[i], c, Vector2.zero);
        }

        for (var i = 0; i < points.Count; i++)
        {
            var next = i == points.Count - 1 ? 1 : i + 2;
            vh.AddTriangle(0, i + 1, next);
        }
    }

    private static void AddArc(List<Vector2> points, Vector2 center, float radius, float startDegrees, float endDegrees, int segments)
    {
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }
}
