using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PingPongBall : MonoBehaviour
{
    public float paddleVelocityMultiplier = 0.85f;
    public float forwardBoost = 2.2f;
    public float upwardBoost = 0.25f;
    public float minimumPaddleHitSpeed = 2.6f;
    public float heldBallHitSpeed = 3.2f;
    public float paddleHitCooldown = 0.12f;
    public float minimumClosingSpeed = 0.15f;
    public float heldBallMinimumSwingSpeed = 0.35f;
    public float maxSpeed = 9f;

    private Rigidbody _rb;
    private ControllerBallGrabber _activeGrabber;
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
        var normal = collision.contactCount > 0 ? contact.normal : EstimatePaddleFaceNormal(tracker.transform, _rb.velocity);
        var hitPoint = collision.contactCount > 0 ? contact.point : transform.position;

        ApplyPaddleHit(tracker, normal, hitPoint, collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyTriggerHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyTriggerHit(other);
    }

    private void TryApplyTriggerHit(Collider other)
    {
        var tracker = other.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker == null) return;

        if (!IsHeld && tracker.Speed < 0.2f && _rb.velocity.sqrMagnitude > 0.02f) return;

        ApplyPaddleHit(tracker, EstimatePaddleFaceNormal(tracker.transform, _rb.velocity), transform.position, other);
    }

    public void SetGrabber(ControllerBallGrabber grabber)
    {
        _activeGrabber = grabber;
        if (grabber != null)
        {
            _hitRegistered = false;
        }
    }

    public bool IsGrabbed => _activeGrabber != null;

    private bool IsHeld => IsGrabbed || (_rb != null && _rb.isKinematic && transform.parent != null);

    private void ApplyPaddleHit(PaddleVelocityTracker tracker, Vector3 normal, Vector3 hitPoint, Collider hitCollider)
    {
        if (Time.time - _lastPaddleHitTime < paddleHitCooldown) return;

        var tableDirection = Vector3.forward;
        var wasHeld = IsHeld;
        var actualBallVelocity = _rb.velocity;
        var incomingVelocity = actualBallVelocity;
        if (incomingVelocity.sqrMagnitude < 0.01f || wasHeld)
        {
            incomingVelocity = -tableDirection * forwardBoost;
        }

        if (normal.sqrMagnitude < 0.01f)
        {
            normal = EstimatePaddleFaceNormal(tracker.transform, incomingVelocity);
        }

        normal = BlendTowardPaddleFace(normal, tracker.transform, incomingVelocity);
        normal.Normalize();

        if (!ShouldAcceptPaddleHit(tracker, normal, actualBallVelocity, wasHeld))
        {
            return;
        }

        _lastPaddleHitTime = Time.time;

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

        direction = ApplyContactPlacement(direction, tracker, hitCollider, hitPoint);

        var swingVelocity = tracker.Velocity;
        var swingTowardTable = Mathf.Max(0f, Vector3.Dot(swingVelocity, tableDirection));
        var minimumSpeed = wasHeld ? heldBallHitSpeed : minimumPaddleHitSpeed;
        var speed = Mathf.Clamp(
            Mathf.Max(reflected.magnitude * 0.85f, forwardBoost) + swingTowardTable * paddleVelocityMultiplier + tracker.Speed * 0.22f,
            minimumSpeed,
            maxSpeed);

        var paddleInfluence = new Vector3(swingVelocity.x * 0.18f, Mathf.Clamp(swingVelocity.y * 0.22f, -0.25f, 0.55f), 0f);
        var baseVelocity = direction * speed + paddleInfluence + Vector3.up * upwardBoost;

        if (baseVelocity.z < 0.5f) baseVelocity.z = Mathf.Lerp(baseVelocity.z, 2.0f, 0.6f);
        baseVelocity = Vector3.ClampMagnitude(baseVelocity, maxSpeed);

        if (_activeGrabber != null && _activeGrabber.ForceRelease(this, baseVelocity))
        {
            _activeGrabber = null;
        }
        else
        {
            if (_rb.isKinematic)
            {
                transform.SetParent(null, true);
                _rb.isKinematic = false;
                _rb.useGravity = true;
            }

            _rb.velocity = baseVelocity;
        }

        if (!_hitRegistered)
        {
            _hitRegistered = true;
            PingPongEvents.BallHit();
        }
    }

    private static Vector3 EstimatePaddleFaceNormal(Transform paddle, Vector3 incomingVelocity)
    {
        if (paddle == null) return Vector3.forward;

        var normal = paddle.up;
        if (incomingVelocity.sqrMagnitude > 0.01f && Vector3.Dot(normal, incomingVelocity) > 0f)
        {
            normal = -normal;
        }
        else if (incomingVelocity.sqrMagnitude <= 0.01f && Vector3.Dot(normal, Vector3.forward) < 0f)
        {
            normal = -normal;
        }

        return normal;
    }

    private static Vector3 BlendTowardPaddleFace(Vector3 collisionNormal, Transform paddle, Vector3 incomingVelocity)
    {
        if (paddle == null) return collisionNormal;

        var faceNormal = EstimatePaddleFaceNormal(paddle, incomingVelocity);
        if (collisionNormal.sqrMagnitude < 0.01f) return faceNormal;

        collisionNormal.Normalize();
        if (Mathf.Abs(Vector3.Dot(collisionNormal, faceNormal)) < 0.45f)
        {
            return faceNormal;
        }

        return Vector3.Slerp(collisionNormal, faceNormal, 0.35f);
    }

    private bool ShouldAcceptPaddleHit(PaddleVelocityTracker tracker, Vector3 normal, Vector3 ballVelocity, bool wasHeld)
    {
        if (tracker == null) return false;

        if (wasHeld)
        {
            return tracker.Speed >= heldBallMinimumSwingSpeed || Vector3.Dot(tracker.Velocity, Vector3.forward) > minimumClosingSpeed;
        }

        var closingSpeed = Vector3.Dot(tracker.Velocity - ballVelocity, normal);
        return closingSpeed >= minimumClosingSpeed ||
               (ballVelocity.sqrMagnitude < 0.04f && tracker.Speed >= heldBallMinimumSwingSpeed);
    }

    private static Vector3 ApplyContactPlacement(Vector3 direction, PaddleVelocityTracker tracker, Collider hitCollider, Vector3 hitPoint)
    {
        if (tracker == null) return direction;

        var localHit = tracker.GetCenteredLocalHit(hitPoint, hitCollider);
        var lateral = Mathf.Clamp(-localHit.x * 1.15f, -0.45f, 0.45f);
        var lift = Mathf.Clamp(localHit.z * 0.35f, -0.18f, 0.28f);
        var adjusted = direction + Vector3.right * lateral + Vector3.up * lift;

        return adjusted.sqrMagnitude > 0.01f ? adjusted.normalized : direction;
    }
}
