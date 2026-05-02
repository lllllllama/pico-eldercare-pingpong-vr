using UnityEngine;

public class PaddleVelocityTracker : MonoBehaviour
{
    public bool autoAlignColliders = true;
    public Vector3 bodyColliderCenter = new Vector3(0f, 0f, 0.16f);
    public Vector3 bodyColliderSize = new Vector3(0.2f, 0.04f, 0.32f);
    public Vector3 hitZoneColliderCenter = new Vector3(0f, 0f, 0.16f);
    public Vector3 hitZoneColliderSize = new Vector3(0.22f, 0.06f, 0.32f);

    public Vector3 Velocity { get; private set; }
    public float Speed => Velocity.magnitude;
    public float MaxSpeed { get; private set; }

    private Vector3 _lastPosition;

    private void Awake()
    {
        AlignColliders();
    }

    private void OnEnable()
    {
        AlignColliders();
        _lastPosition = transform.position;
        Velocity = Vector3.zero;
        MaxSpeed = 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AlignColliders();
    }
#endif

    private void LateUpdate()
    {
        var dt = Time.deltaTime;
        if (dt <= Mathf.Epsilon)
        {
            Velocity = Vector3.zero;
            return;
        }

        var currentPosition = transform.position;
        Velocity = (currentPosition - _lastPosition) / dt;
        _lastPosition = currentPosition;

        if (Speed > MaxSpeed)
        {
            MaxSpeed = Speed;
        }
    }

    public void ResetMaxSpeed() => MaxSpeed = 0f;

    public Vector3 GetCenteredLocalHit(Vector3 worldPoint, Collider hitCollider)
    {
        if (hitCollider is BoxCollider box)
        {
            return box.transform.InverseTransformPoint(worldPoint) - box.center;
        }

        return transform.InverseTransformPoint(worldPoint) - bodyColliderCenter;
    }

    private void AlignColliders()
    {
        if (!autoAlignColliders) return;

        var body = GetComponent<BoxCollider>();
        if (body != null)
        {
            body.center = bodyColliderCenter;
            body.size = bodyColliderSize;
            body.isTrigger = false;
        }

        var hitZone = transform.Find("PaddleHitZone");
        if (hitZone == null) return;

        var hitZoneCollider = hitZone.GetComponent<BoxCollider>();
        if (hitZoneCollider == null) return;

        hitZone.localPosition = Vector3.zero;
        hitZone.localRotation = Quaternion.identity;
        hitZone.localScale = Vector3.one;
        hitZoneCollider.center = hitZoneColliderCenter;
        hitZoneCollider.size = hitZoneColliderSize;
        hitZoneCollider.isTrigger = true;
    }
}
