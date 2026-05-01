using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PingPongBall : MonoBehaviour
{
    public float paddleVelocityMultiplier = 0.85f;
    public float forwardBoost = 2.2f;
    public float upwardBoost = 0.25f;
    public float maxSpeed = 9f;

    private Rigidbody _rb;
    private bool _hitRegistered;
    private float _lastPaddleHitTime = -1f;

    private void Awake()
    {
        if (!TryGetComponent(out _rb))
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        _rb.mass = 0.0027f;
        _rb.drag = 0.03f;
        _rb.angularDrag = 0.05f;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var tracker = collision.collider.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker == null) return;

        var contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
        var normal = collision.contactCount > 0 ? contact.normal : EstimatePaddleFaceNormal(tracker.transform);
        var hitPoint = collision.contactCount > 0 ? contact.point : transform.position;

        ApplyPaddleHit(tracker, normal, hitPoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        var tracker = other.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker == null) return;

        ApplyPaddleHit(tracker, EstimatePaddleFaceNormal(tracker.transform), transform.position);
    }

    private void ApplyPaddleHit(PaddleVelocityTracker tracker, Vector3 normal, Vector3 hitPoint)
    {
        if (Time.time - _lastPaddleHitTime < 0.08f) return;
        _lastPaddleHitTime = Time.time;

        var tableDirection = Vector3.forward;
        var incomingVelocity = _rb.velocity;
        if (incomingVelocity.sqrMagnitude < 0.01f)
        {
            incomingVelocity = -tableDirection * forwardBoost;
        }

        if (normal.sqrMagnitude < 0.01f)
        {
            normal = EstimatePaddleFaceNormal(tracker.transform);
        }

        normal.Normalize();
        var reflected = Vector3.Reflect(incomingVelocity, normal);
        if (reflected.sqrMagnitude < 0.01f)
        {
            reflected = tableDirection * forwardBoost;
        }

        var direction = reflected.normalized;
        if (Vector3.Dot(direction, tableDirection) < 0.15f)
        {
            direction = Vector3.Slerp(direction, tableDirection, 0.65f).normalized;
        }

        direction = ApplyContactPlacement(direction, tracker.transform, hitPoint);

        var swingVelocity = tracker.Velocity;
        var swingTowardTable = Mathf.Max(0f, Vector3.Dot(swingVelocity, tableDirection));
        var speed = Mathf.Clamp(
            Mathf.Max(reflected.magnitude * 0.85f, forwardBoost) + swingTowardTable * 0.75f + tracker.Speed * 0.22f,
            2.0f,
            maxSpeed);

        var paddleInfluence = new Vector3(swingVelocity.x * 0.18f, Mathf.Clamp(swingVelocity.y * 0.22f, -0.25f, 0.55f), 0f);
        var baseVelocity = direction * speed + paddleInfluence + Vector3.up * upwardBoost;

        if (baseVelocity.z < 0.5f) baseVelocity.z = Mathf.Lerp(baseVelocity.z, 2.0f, 0.6f);
        _rb.velocity = Vector3.ClampMagnitude(baseVelocity, maxSpeed);

        if (!_hitRegistered)
        {
            _hitRegistered = true;
            PingPongEvents.BallHit();
        }
    }

    private static Vector3 EstimatePaddleFaceNormal(Transform paddle)
    {
        if (paddle == null) return Vector3.forward;

        var normal = paddle.up;
        if (Vector3.Dot(normal, Vector3.forward) < 0f)
        {
            normal = -normal;
        }

        return normal;
    }

    private static Vector3 ApplyContactPlacement(Vector3 direction, Transform paddle, Vector3 hitPoint)
    {
        if (paddle == null) return direction;

        var localHit = paddle.InverseTransformPoint(hitPoint);
        var lateral = Mathf.Clamp(-localHit.x * 1.15f, -0.55f, 0.55f);
        var lift = Mathf.Clamp(localHit.z * 0.45f, -0.2f, 0.35f);
        var adjusted = direction + Vector3.right * lateral + Vector3.up * lift;

        return adjusted.sqrMagnitude > 0.01f ? adjusted.normalized : direction;
    }
}
