using UnityEngine;

[DefaultExecutionOrder(-120)]
public class PingPongRoomSensingVisibilityGuard : MonoBehaviour
{
    public Transform roomSensingRoot;
    public bool hideAllRenderersUnderRoot = true;
    public bool addMissingMeshColliders = true;
    public float scanIntervalSeconds = 0.15f;

    private float _nextScanTime;

    private void OnEnable()
    {
        _nextScanTime = 0f;
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

        if (roomSensingRoot != null && hideAllRenderersUnderRoot)
        {
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
