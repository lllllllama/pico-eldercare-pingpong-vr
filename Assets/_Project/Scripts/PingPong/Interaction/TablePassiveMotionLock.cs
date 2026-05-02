using UnityEngine;

[DefaultExecutionOrder(200)]
public class TablePassiveMotionLock : MonoBehaviour
{
    public TableDragHandle dragHandle;
    public float restoreThreshold = 0.002f;

    private Rigidbody _rigidbody;
    private Vector3 _acceptedPosition;
    private Quaternion _acceptedRotation;

    private void OnEnable()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _acceptedPosition = transform.position;
        _acceptedRotation = transform.rotation;
        StabilizeRigidbody();
    }

    private void LateUpdate()
    {
        StabilizeRigidbody();

        if (dragHandle != null && dragHandle.IsDragging)
        {
            _acceptedPosition = transform.position;
            _acceptedRotation = transform.rotation;
            return;
        }

        if ((transform.position - _acceptedPosition).sqrMagnitude > restoreThreshold * restoreThreshold || Quaternion.Angle(transform.rotation, _acceptedRotation) > 0.1f)
        {
            transform.SetPositionAndRotation(_acceptedPosition, _acceptedRotation);
            StabilizeRigidbody();
        }
    }

    public void AcceptCurrentTransform()
    {
        _acceptedPosition = transform.position;
        _acceptedRotation = transform.rotation;
        StabilizeRigidbody();
    }

    private void StabilizeRigidbody()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        if (_rigidbody == null) return;

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }
}
