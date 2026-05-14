using UnityEngine;

public static class PingPongGeometry
{
    public const float TableWidth = 1.525f;
    public const float TableLength = 2.74f;
    public const float TableTopHeight = 1.0f;
    public const float TableThickness = 0.08f;
    public const float NetHeight = 0.1525f;
    public const float NetThickness = 0.01f;
    public const float BallRadius = 0.02f;
    public const float BallDiameter = BallRadius * 2f;
    public const float BallMass = 0.0027f;
    public const float BallDrag = 0.03f;
    public const float BallAngularDrag = 0.05f;

    public const float PaddleBladeWidth = 0.2f;
    public const float PaddleBladeHeight = 0.32f;
    public const float PaddleBladeThickness = 0.04f;
    public const float PaddleHitZonePadding = 0.02f;

    public static readonly Vector3 TableCenter = new Vector3(0f, TableTopHeight - TableThickness * 0.5f, 2f);
    public static readonly Vector3 TableColliderWorldSize = new Vector3(TableWidth, TableThickness, TableLength);
    public static readonly Vector3 NetColliderWorldSize = new Vector3(TableWidth, NetHeight, NetThickness);
    public static readonly Vector3 NetLocalCenter = new Vector3(0f, TableThickness * 0.5f + NetHeight * 0.5f, 0f);
    public static readonly Vector3 BallPrefabScale = Vector3.one * BallDiameter;

    public static readonly Vector3 PaddleColliderCenter = new Vector3(0f, 0f, PaddleBladeHeight * 0.5f);
    public static readonly Vector3 PaddleColliderSize = new Vector3(PaddleBladeWidth, PaddleBladeThickness, PaddleBladeHeight);
    public static readonly Vector3 PaddleHitZoneCenter = PaddleColliderCenter;
    public static readonly Vector3 PaddleHitZoneSize = new Vector3(
        PaddleBladeWidth + PaddleHitZonePadding,
        PaddleBladeThickness + PaddleHitZonePadding,
        PaddleBladeHeight);

    public static Vector2 TableBlockerSize(float padding)
    {
        return new Vector2(TableWidth + padding, TableLength + padding);
    }

    public static Vector3 LocalSizeForWorldSize(Transform transform, Vector3 worldSize)
    {
        if (transform == null) return worldSize;

        var scale = transform.lossyScale;
        return new Vector3(
            worldSize.x / Mathf.Max(Mathf.Abs(scale.x), 0.001f),
            worldSize.y / Mathf.Max(Mathf.Abs(scale.y), 0.001f),
            worldSize.z / Mathf.Max(Mathf.Abs(scale.z), 0.001f));
    }
}
