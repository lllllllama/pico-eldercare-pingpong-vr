using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ElderCareIconType
{
    Gamepad,
    Heart,
    MapPin,
    Video,
    Check,
    ArrowLeft
}

public class ElderCareLineIcon : MaskableGraphic
{
    public ElderCareIconType iconType = ElderCareIconType.Gamepad;
    public float strokeWidth = 8f;
    public int circleSegments = 24;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var rect = GetPixelAdjustedRect();
        var size = Mathf.Min(rect.width, rect.height);
        var center = rect.center;
        var stroke = Mathf.Max(1f, strokeWidth);
        var c = color;

        switch (iconType)
        {
            case ElderCareIconType.Gamepad:
                DrawGamepad(vh, center, size, stroke, c);
                break;
            case ElderCareIconType.Heart:
                DrawHeart(vh, center, size, stroke, c);
                break;
            case ElderCareIconType.MapPin:
                DrawMapPin(vh, center, size, stroke, c);
                break;
            case ElderCareIconType.Video:
                DrawVideo(vh, center, size, stroke, c);
                break;
            case ElderCareIconType.Check:
                DrawCheck(vh, center, size, stroke, c);
                break;
            case ElderCareIconType.ArrowLeft:
                DrawArrowLeft(vh, center, size, stroke, c);
                break;
        }
    }

    private void DrawGamepad(VertexHelper vh, Vector2 center, float size, float stroke, Color32 c)
    {
        var w = size * 0.72f;
        var h = size * 0.42f;
        var y = center.y - size * 0.02f;
        DrawPolyline(vh, new[]
        {
            new Vector2(center.x - w * 0.5f, y - h * 0.2f),
            new Vector2(center.x - w * 0.35f, y + h * 0.5f),
            new Vector2(center.x + w * 0.35f, y + h * 0.5f),
            new Vector2(center.x + w * 0.5f, y - h * 0.2f),
            new Vector2(center.x + w * 0.35f, y - h * 0.5f),
            new Vector2(center.x + w * 0.12f, y - h * 0.25f),
            new Vector2(center.x - w * 0.12f, y - h * 0.25f),
            new Vector2(center.x - w * 0.35f, y - h * 0.5f),
            new Vector2(center.x - w * 0.5f, y - h * 0.2f),
        }, stroke, c);

        DrawLine(vh, new Vector2(center.x - w * 0.31f, y), new Vector2(center.x - w * 0.13f, y), stroke, c);
        DrawLine(vh, new Vector2(center.x - w * 0.22f, y - h * 0.18f), new Vector2(center.x - w * 0.22f, y + h * 0.18f), stroke, c);
        DrawCircle(vh, new Vector2(center.x + w * 0.22f, y + h * 0.08f), size * 0.045f, stroke, c);
        DrawCircle(vh, new Vector2(center.x + w * 0.33f, y - h * 0.1f), size * 0.045f, stroke, c);
    }

    private void DrawHeart(VertexHelper vh, Vector2 center, float size, float stroke, Color32 c)
    {
        DrawPolyline(vh, new[]
        {
            center + new Vector2(-0.34f, 0.04f) * size,
            center + new Vector2(-0.43f, 0.22f) * size,
            center + new Vector2(-0.27f, 0.37f) * size,
            center + new Vector2(-0.08f, 0.31f) * size,
            center,
            center + new Vector2(0.08f, 0.31f) * size,
            center + new Vector2(0.27f, 0.37f) * size,
            center + new Vector2(0.43f, 0.22f) * size,
            center + new Vector2(0.34f, 0.04f) * size,
            center + new Vector2(0f, -0.36f) * size,
            center + new Vector2(-0.34f, 0.04f) * size,
        }, stroke, c);
    }

    private void DrawMapPin(VertexHelper vh, Vector2 center, float size, float stroke, Color32 c)
    {
        var pinCenter = center + Vector2.up * size * 0.13f;
        DrawCircle(vh, pinCenter, size * 0.22f, stroke, c);
        DrawCircle(vh, pinCenter, size * 0.07f, stroke, c);
        DrawPolyline(vh, new[]
        {
            pinCenter + new Vector2(-0.2f, -0.1f) * size,
            center + new Vector2(0f, -0.4f) * size,
            pinCenter + new Vector2(0.2f, -0.1f) * size,
        }, stroke, c);
    }

    private void DrawVideo(VertexHelper vh, Vector2 center, float size, float stroke, Color32 c)
    {
        var w = size * 0.62f;
        var h = size * 0.42f;
        DrawPolyline(vh, new[]
        {
            center + new Vector2(-w * 0.5f, -h * 0.5f),
            center + new Vector2(w * 0.2f, -h * 0.5f),
            center + new Vector2(w * 0.2f, h * 0.5f),
            center + new Vector2(-w * 0.5f, h * 0.5f),
            center + new Vector2(-w * 0.5f, -h * 0.5f),
        }, stroke, c);

        DrawPolyline(vh, new[]
        {
            center + new Vector2(w * 0.2f, h * 0.22f),
            center + new Vector2(w * 0.5f, h * 0.4f),
            center + new Vector2(w * 0.5f, -h * 0.4f),
            center + new Vector2(w * 0.2f, -h * 0.22f),
        }, stroke, c);
    }

    private void DrawCheck(VertexHelper vh, Vector2 center, float size, float stroke, Color32 c)
    {
        DrawPolyline(vh, new[]
        {
            center + new Vector2(-0.28f, -0.02f) * size,
            center + new Vector2(-0.08f, -0.24f) * size,
            center + new Vector2(0.32f, 0.24f) * size,
        }, stroke, c);
    }

    private void DrawArrowLeft(VertexHelper vh, Vector2 center, float size, float stroke, Color32 c)
    {
        DrawLine(vh, center + new Vector2(-0.34f, 0f) * size, center + new Vector2(0.34f, 0f) * size, stroke, c);
        DrawPolyline(vh, new[]
        {
            center + new Vector2(-0.34f, 0f) * size,
            center + new Vector2(-0.08f, 0.26f) * size,
        }, stroke, c);
        DrawPolyline(vh, new[]
        {
            center + new Vector2(-0.34f, 0f) * size,
            center + new Vector2(-0.08f, -0.26f) * size,
        }, stroke, c);
    }

    private void DrawPolyline(VertexHelper vh, IList<Vector2> points, float stroke, Color32 c)
    {
        for (var i = 0; i < points.Count - 1; i++)
        {
            DrawLine(vh, points[i], points[i + 1], stroke, c);
        }
    }

    private void DrawCircle(VertexHelper vh, Vector2 center, float radius, float stroke, Color32 c)
    {
        var segments = Mathf.Max(8, circleSegments);
        var previous = center + Vector2.right * radius;
        for (var i = 1; i <= segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            DrawLine(vh, previous, next, stroke, c);
            previous = next;
        }
    }

    private static void DrawLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color32 c)
    {
        var direction = b - a;
        if (direction.sqrMagnitude < 0.001f) return;

        var normal = new Vector2(-direction.y, direction.x).normalized * thickness * 0.5f;
        var index = vh.currentVertCount;
        vh.AddVert(a - normal, c, Vector2.zero);
        vh.AddVert(a + normal, c, Vector2.zero);
        vh.AddVert(b + normal, c, Vector2.zero);
        vh.AddVert(b - normal, c, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }
}
