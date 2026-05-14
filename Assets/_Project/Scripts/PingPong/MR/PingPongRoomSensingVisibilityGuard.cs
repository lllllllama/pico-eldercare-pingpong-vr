using UnityEngine;

[DefaultExecutionOrder(-120)]
public class PingPongRoomSensingVisibilityGuard : MonoBehaviour
{
    public Transform roomSensingRoot;
    public bool hideAllRenderersUnderRoot = true;
    public bool addMissingMeshColliders = true;
    public bool ignoreBallCollision = true;
    public string roomSensingPhysicsLayerName = "RoomSensing";
    public string ballPhysicsLayerName = "Ball";
    public float scanIntervalSeconds = 0.15f;

    private float _nextScanTime;

    private void OnEnable()
    {
        _nextScanTime = 0f;
        ConfigureBallCollisionIgnore();
        HideRoomSensingVisuals();
    }

    private void Update()
    {
        if (Time.time < _nextScanTime) return;
        _nextScanTime = Time.time + Mathf.Max(0.05f, scanIntervalSeconds);
        HideRoomSensingVisuals();
    }

    public void HideRoomSensingVisuals()
    {
        ResolveRoot();
        ConfigureBallCollisionIgnore();

        if (roomSensingRoot != null && hideAllRenderersUnderRoot)
        {
            ApplyRoomSensingLayerRecursively(roomSensingRoot);
            HideRenderersUnder(roomSensingRoot);
        }

        foreach (var transform in FindObjectsOfType<Transform>(true))
        {
            if (transform == null || !IsRoomSensingObject(transform.name)) continue;
            HideRendererAndPrepareCollider(transform);
        }
    }

    private void HideRenderersUnder(Transform root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter != null)
            {
                HideRendererAndPrepareCollider(meshFilter.transform);
            }
        }
    }

    private void HideRendererAndPrepareCollider(Transform target)
    {
        ApplyRoomSensingLayer(target);

        var renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        if (!addMissingMeshColliders) return;
        if (target.GetComponent<MeshFilter>() != null && target.GetComponent<MeshCollider>() == null)
        {
            target.gameObject.AddComponent<MeshCollider>();
        }
    }

    private void ConfigureBallCollisionIgnore()
    {
        if (!ignoreBallCollision) return;
        if (string.IsNullOrEmpty(roomSensingPhysicsLayerName) || string.IsNullOrEmpty(ballPhysicsLayerName)) return;

        var roomSensingLayer = LayerMask.NameToLayer(roomSensingPhysicsLayerName);
        var ballLayer = LayerMask.NameToLayer(ballPhysicsLayerName);
        if (roomSensingLayer < 0 || ballLayer < 0) return;

        Physics.IgnoreLayerCollision(ballLayer, roomSensingLayer, true);
    }

    private void ApplyRoomSensingLayerRecursively(Transform root)
    {
        if (string.IsNullOrEmpty(roomSensingPhysicsLayerName)) return;

        var roomSensingLayer = LayerMask.NameToLayer(roomSensingPhysicsLayerName);
        if (roomSensingLayer < 0 || root == null) return;

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != null)
            {
                child.gameObject.layer = roomSensingLayer;
            }
        }
    }

    private void ApplyRoomSensingLayer(Transform target)
    {
        if (string.IsNullOrEmpty(roomSensingPhysicsLayerName)) return;

        var roomSensingLayer = LayerMask.NameToLayer(roomSensingPhysicsLayerName);
        if (roomSensingLayer < 0 || target == null) return;

        target.gameObject.layer = roomSensingLayer;
    }

    private void ResolveRoot()
    {
        if (roomSensingRoot != null) return;

        foreach (var transform in FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.name == "MRSpaceSensing")
            {
                roomSensingRoot = transform;
                return;
            }
        }
    }

    private static bool IsRoomSensingObject(string objectName)
    {
        return objectName == "MRDetectedPlaneTemplate" ||
               objectName == "MRSpatialMeshTemplate" ||
               objectName.StartsWith("MRDetectedPlaneTemplate", System.StringComparison.Ordinal) ||
               objectName.StartsWith("MRSpatialMeshTemplate", System.StringComparison.Ordinal) ||
               objectName.StartsWith("PXR_Plane", System.StringComparison.Ordinal) ||
               objectName.StartsWith("PXRPlane", System.StringComparison.Ordinal) ||
               objectName.StartsWith("PXR_SpatialMesh", System.StringComparison.Ordinal) ||
               objectName.StartsWith("PXRSpatialMesh", System.StringComparison.Ordinal) ||
               objectName == "Plane" ||
               objectName.StartsWith("Plane ", System.StringComparison.Ordinal) ||
               objectName.StartsWith("Plane(", System.StringComparison.Ordinal);
    }
}
