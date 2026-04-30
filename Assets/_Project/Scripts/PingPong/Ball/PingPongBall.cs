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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var tracker = collision.collider.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker == null) return;

        var contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
        var normal = collision.contactCount > 0 ? contact.normal : -transform.forward;
        var reflect = Vector3.Reflect(_rb.velocity.normalized, normal);

        var paddleVelocity = tracker.Velocity * paddleVelocityMultiplier;
        var baseVelocity = reflect * forwardBoost + paddleVelocity + Vector3.up * upwardBoost;

        if (baseVelocity.sqrMagnitude < 0.01f)
        {
            baseVelocity = (transform.forward + Vector3.up * 0.2f).normalized * forwardBoost;
        }

        _rb.velocity = Vector3.ClampMagnitude(baseVelocity, maxSpeed);

        if (!_hitRegistered)
        {
            _hitRegistered = true;
            PingPongEvents.BallHit();
        }
    }
}
