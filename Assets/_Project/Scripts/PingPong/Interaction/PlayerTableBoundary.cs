using UnityEngine;

public class PlayerTableBoundary : MonoBehaviour
{
    public Transform tableTransform;
    public Vector3 tableCenter = PingPongGeometry.TableCenter;
    public Vector2 tableSize = PingPongGeometry.TableBlockerSize(0.24f);
    public float margin = 0.12f;
    public bool moveRigWhenInside = false;

    private Transform _rigRoot;
    private Camera _camera;

    private void LateUpdate()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera == null) return;

        if (_rigRoot == null)
        {
            _rigRoot = FindRigRoot(_camera.transform);
        }

        if (_rigRoot == null) return;
        if (tableTransform == null)
        {
            var table = GameObject.Find("Table");
            if (table != null)
            {
                tableTransform = table.transform;
            }
        }

        var head = _camera.transform.position;
        var center = tableTransform != null ? tableTransform.position : tableCenter;
        var halfX = tableSize.x * 0.5f + margin;
        var halfZ = tableSize.y * 0.5f + margin;
        var localX = head.x - center.x;
        var localZ = head.z - center.z;

        if (Mathf.Abs(localX) > halfX || Mathf.Abs(localZ) > halfZ) return;
        if (!moveRigWhenInside) return;

        var pushToX = halfX - Mathf.Abs(localX);
        var pushToZ = halfZ - Mathf.Abs(localZ);
        var correction = Vector3.zero;

        if (pushToX < pushToZ)
        {
            correction.x = (localX >= 0f ? 1f : -1f) * pushToX;
        }
        else
        {
            correction.z = (localZ >= 0f ? 1f : -1f) * pushToZ;
        }

        _rigRoot.position += correction;
    }

    private static Transform FindRigRoot(Transform cameraTransform)
    {
        var current = cameraTransform;
        while (current.parent != null)
        {
            var parentName = current.parent.name.ToLowerInvariant();
            if (parentName.Contains("building block") || parentName.Contains("xr origin") || parentName.Contains("xrorigin") || parentName.Contains("rig"))
            {
                return current.parent;
            }

            current = current.parent;
        }

        return current;
    }
}
