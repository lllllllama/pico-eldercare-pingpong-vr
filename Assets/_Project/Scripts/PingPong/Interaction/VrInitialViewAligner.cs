using System.Collections;
using UnityEngine;

public class VrInitialViewAligner : MonoBehaviour
{
    public Vector3 desiredHeadWorldPosition = new Vector3(0f, 1.6f, 0.25f);
    public Vector3 lookAtWorldPosition = new Vector3(0f, 1.1f, 2f);
    public bool alignOnStart = true;
    public bool alignPosition = true;

    private IEnumerator Start()
    {
        if (!alignOnStart) yield break;

        yield return null;
        AlignNow();
        yield return null;
        AlignNow();
    }

    public void AlignNow()
    {
        var camera = Camera.main;
        if (camera == null) return;

        var rigRoot = FindRigRoot(camera.transform);
        if (rigRoot == null) return;

        if (alignPosition)
        {
            var offset = desiredHeadWorldPosition - camera.transform.position;
            offset.y = 0f;
            rigRoot.position += offset;
        }

        var cameraForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
        var desiredForward = Vector3.ProjectOnPlane(lookAtWorldPosition - camera.transform.position, Vector3.up);
        if (cameraForward.sqrMagnitude > 0.001f && desiredForward.sqrMagnitude > 0.001f)
        {
            var yawDelta = Vector3.SignedAngle(cameraForward.normalized, desiredForward.normalized, Vector3.up);
            rigRoot.RotateAround(camera.transform.position, Vector3.up, yawDelta);
        }
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
