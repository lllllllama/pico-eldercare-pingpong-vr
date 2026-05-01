using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PingPongBall : MonoBehaviour
{
    public float paddleVelocityMultiplier = 0.6f;
    public float forwardBoost = 1.8f;
    public float upwardBoost = 0.25f;
    public float maxSpeed = 8f;

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
        var normal = collision.contactCount > 0 ? contact.normal : -transform.forward;
        var direction = Vector3.Reflect(_rb.velocity, normal);

        ApplyPaddleHit(tracker, direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        var tracker = other.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker == null) return;

        ApplyPaddleHit(tracker, Vector3.forward + Vector3.up * 0.2f);
    }

    private void ApplyPaddleHit(PaddleVelocityTracker tracker, Vector3 direction)
    {
        if (Time.time - _lastPaddleHitTime < 0.08f) return;
        _lastPaddleHitTime = Time.time;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector3.forward + Vector3.up * 0.2f;
        }

        if (direction.z < 0.35f)
        {
            direction.z = 0.9f;
        }

        direction.Normalize();
        var speed = Mathf.Clamp(Mathf.Max(_rb.velocity.magnitude, forwardBoost) + tracker.Speed * 0.2f, 2.4f, maxSpeed);
        var paddleVelocity = tracker.Velocity * paddleVelocityMultiplier;
        var baseVelocity = direction * speed + paddleVelocity + Vector3.up * upwardBoost;

        if (baseVelocity.z < 0.8f) baseVelocity.z = 2.4f;
        _rb.velocity = Vector3.ClampMagnitude(baseVelocity, maxSpeed);

        if (!_hitRegistered)
        {
            _hitRegistered = true;
            PingPongEvents.BallHit();
        }
    }
}
