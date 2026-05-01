using UnityEngine;

public class ControllerTransformFollower : MonoBehaviour
{
    public Transform controllerTransform;
    public Vector3 positionOffset;
    public Vector3 rotationOffsetEuler;

    private void LateUpdate()
    {
        if (controllerTransform == null) return;

        transform.position = controllerTransform.TransformPoint(positionOffset);
        transform.rotation = controllerTransform.rotation * Quaternion.Euler(rotationOffsetEuler);
    }
}
