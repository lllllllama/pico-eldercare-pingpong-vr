using UnityEngine;

public class PaddleVelocityTracker : MonoBehaviour
{
    public bool autoAlignColliders = true;
    public Vector3 bodyColliderCenter = PingPongGeometry.PaddleColliderCenter;
    public Vector3 bodyColliderSize = PingPongGeometry.PaddleColliderSize;
    public Vector3 hitZoneColliderCenter = PingPongGeometry.PaddleHitZoneCenter;
    public Vector3 hitZoneColliderSize = PingPongGeometry.PaddleHitZoneSize;
    [Range(0f, 1f)] public float velocitySmoothing = 0.35f;
    [Range(0f, 1f)] public float angularVelocitySmoothing = 0.35f;
    [Range(0f, 1f)] public float accelerationSmoothing = 0.45f;

    public Vector3 Velocity { get; private set; }
    public Vector3 RawVelocity { get; private set; }
    public Vector3 Acceleration { get; private set; }
    public Vector3 RawAcceleration { get; private set; }
    public Vector3 AngularVelocity { get; private set; }
    public Vector3 RawAngularVelocity { get; private set; }
    public float Speed => Velocity.magnitude;
    public float MaxSpeed { get; private set; }
    public float AccelerationMagnitude => Acceleration.magnitude;

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private Vector3 _lastRawVelocity;
    private bool _hasLastPose;

    private void Awake()
    {
        AlignColliders(true);
    }

    private void OnEnable()
    {
        AlignColliders(true);
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
        _hasLastPose = true;
        Velocity = Vector3.zero;
        RawVelocity = Vector3.zero;
        Acceleration = Vector3.zero;
        RawAcceleration = Vector3.zero;
        AngularVelocity = Vector3.zero;
        RawAngularVelocity = Vector3.zero;
        _lastRawVelocity = Vector3.zero;
        MaxSpeed = 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AlignColliders(false);
    }
#endif

    private void LateUpdate()
    {
        var dt = Time.deltaTime;
        if (dt <= Mathf.Epsilon || !_hasLastPose)
        {
            Velocity = Vector3.zero;
            RawVelocity = Vector3.zero;
            Acceleration = Vector3.zero;
            RawAcceleration = Vector3.zero;
            AngularVelocity = Vector3.zero;
            RawAngularVelocity = Vector3.zero;
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _lastRawVelocity = Vector3.zero;
            _hasLastPose = true;
            return;
        }

        var currentPosition = transform.position;
        var currentRotation = transform.rotation;
        RawVelocity = (currentPosition - _lastPosition) / dt;
        RawAcceleration = (RawVelocity - _lastRawVelocity) / dt;
        RawAngularVelocity = CalculateAngularVelocity(_lastRotation, currentRotation, dt);
        Velocity = Vector3.Lerp(RawVelocity, Velocity, Mathf.Clamp01(velocitySmoothing));
        Acceleration = Vector3.Lerp(RawAcceleration, Acceleration, Mathf.Clamp01(accelerationSmoothing));
        AngularVelocity = Vector3.Lerp(RawAngularVelocity, AngularVelocity, Mathf.Clamp01(angularVelocitySmoothing));
        _lastPosition = currentPosition;
        _lastRotation = currentRotation;
        _lastRawVelocity = RawVelocity;

        if (Speed > MaxSpeed)
        {
            MaxSpeed = Speed;
        }
    }

    public void ResetMaxSpeed() => MaxSpeed = 0f;

    public Vector3 GetSurfaceVelocity(Vector3 worldPoint)
    {
        return Velocity + Vector3.Cross(AngularVelocity, worldPoint - transform.position);
    }

    public Vector3 GetRawSurfaceVelocity(Vector3 worldPoint)
    {
        return RawVelocity + Vector3.Cross(RawAngularVelocity, worldPoint - transform.position);
    }

    public Vector3 GetPredictedPosition(float leadTime)
    {
        var t = Mathf.Max(0f, leadTime);
        return transform.position + Velocity * t + 0.5f * Acceleration * t * t;
    }

    public Vector3 GetCenteredLocalHit(Vector3 worldPoint, Collider hitCollider)
    {
        if (hitCollider is BoxCollider box)
        {
            return box.transform.InverseTransformPoint(worldPoint) - box.center;
        }

        return transform.InverseTransformPoint(worldPoint) - bodyColliderCenter;
    }

    private static Vector3 CalculateAngularVelocity(Quaternion previous, Quaternion current, float dt)
    {
        var delta = current * Quaternion.Inverse(previous);
        delta.ToAngleAxis(out var angleDegrees, out var axis);
        if (angleDegrees > 180f)
        {
            angleDegrees -= 360f;
        }

        if (axis.sqrMagnitude < 0.0001f || float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z))
        {
            return Vector3.zero;
        }

        return axis.normalized * (angleDegrees * Mathf.Deg2Rad / dt);
    }

    private void AlignColliders(bool allowAddSurface)
    {
        if (!autoAlignColliders) return;

        var body = GetComponent<BoxCollider>();
        if (body != null)
        {
            body.center = bodyColliderCenter;
            body.size = bodyColliderSize;
            body.isTrigger = false;
            ConfigureSurface(body.gameObject, PingPongSurfaceType.PaddleBody, allowAddSurface);
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
        ConfigureSurface(hitZoneCollider.gameObject, PingPongSurfaceType.PaddleHitZone, allowAddSurface);
    }

    private static void ConfigureSurface(GameObject target, PingPongSurfaceType surfaceType, bool allowAddSurface)
    {
        if (target == null) return;

        var surface = target.GetComponent<PingPongSurface>();
        if (surface == null && allowAddSurface)
        {
            surface = target.AddComponent<PingPongSurface>();
        }

        if (surface == null) return;

        surface.Configure(surfaceType);
    }
}
