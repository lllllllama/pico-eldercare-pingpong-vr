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
    public float surfaceHitCooldown = 0.08f;
    public float minimumClosingSpeed = 0.15f;
    public float heldBallMinimumSwingSpeed = 0.35f;
    public float maxSpeed = 9f;
    public bool enableSweptSurfaceFallback = true;
    public LayerMask sweptSurfaceLayers = ~0;

    private readonly RaycastHit[] _sweepHits = new RaycastHit[16];
    private Rigidbody _rb;
    private SphereCollider _sphereCollider;
    private ControllerBallGrabber _activeGrabber;
    private Collider _lastSurfaceCollider;
    private Vector3 _lastSweepPosition;
    private bool _hitRegistered;
    private float _ignoreGrabUntilTime;
    private float _lastPaddleHitTime = -1f;
    private float _lastSurfaceHitTime = -1f;

    public bool IsGrabbed => _activeGrabber != null;
    public bool CanBeGrabbed => !IsGrabbed && Time.time >= _ignoreGrabUntilTime;

    private bool IsHeld => IsGrabbed || (_rb != null && _rb.isKinematic && transform.parent != null);

    private void Awake()
    {
        ConfigureRigidbody();
        _sphereCollider = GetComponent<SphereCollider>();
        _lastSweepPosition = transform.position;
    }

    private void OnEnable()
    {
        _lastSweepPosition = transform.position;
        _lastSurfaceCollider = null;
        _lastSurfaceHitTime = -1f;
    }

    private void FixedUpdate()
    {
        if (_rb == null || _rb.isKinematic || !enableSweptSurfaceFallback)
        {
            _lastSweepPosition = transform.position;
            return;
        }

        TryApplySweptSurfaceFallback(_lastSweepPosition, transform.position);
        _lastSweepPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var tracker = collision.collider.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker != null)
        {
            var hitPoint = transform.position;
            var normal = EstimatePaddleFaceNormal(tracker.transform, _rb.velocity, tracker.GetSurfaceVelocity(hitPoint));
            if (collision.contactCount > 0)
            {
                var contact = collision.GetContact(0);
                hitPoint = contact.point;
                normal = contact.normal;
            }

            ApplyPaddleHit(tracker, normal, hitPoint, collision.collider);
            return;
        }

        var surface = PingPongSurface.Find(collision.collider);
        if (surface == null) return;

        var surfacePoint = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.ClosestPoint(transform.position);
        var surfaceNormal = collision.contactCount > 0
            ? collision.GetContact(0).normal
            : PingPongSurface.EstimateNormal(collision.collider, transform.position, _rb.velocity);

        ApplySurfaceBounce(surface, collision.collider, surfaceNormal, surfacePoint, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyTriggerInteraction(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyTriggerInteraction(other);
    }

    public void SetGrabber(ControllerBallGrabber grabber)
    {
        _activeGrabber = grabber;
        if (grabber != null)
        {
            _hitRegistered = false;
            _ignoreGrabUntilTime = 0f;
        }
    }

    public void IgnoreGrabFor(float seconds)
    {
        _ignoreGrabUntilTime = Mathf.Max(_ignoreGrabUntilTime, Time.time + Mathf.Max(0f, seconds));
    }

    private void ConfigureRigidbody()
    {
        if (!TryGetComponent(out _rb))
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        _rb.mass = PingPongGeometry.BallMass;
        _rb.drag = PingPongGeometry.BallDrag;
        _rb.angularDrag = PingPongGeometry.BallAngularDrag;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void TryApplyTriggerInteraction(Collider other)
    {
        var tracker = other.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker != null)
        {
            if (!IsHeld && tracker.Speed < 0.2f && _rb.velocity.sqrMagnitude > 0.02f) return;

            var surfaceVelocity = tracker.GetSurfaceVelocity(transform.position);
            ApplyPaddleHit(
                tracker,
                EstimatePaddleFaceNormal(tracker.transform, _rb.velocity, surfaceVelocity),
                transform.position,
                other);
            return;
        }

        var surface = PingPongSurface.Find(other);
        if (surface == null || surface.surfaceType != PingPongSurfaceType.Net) return;

        var normal = PingPongSurface.EstimateNormal(other, transform.position, _rb.velocity);
        var point = other.ClosestPoint(transform.position);
        ApplySurfaceBounce(surface, other, normal, point, true);
    }

    private void ApplyPaddleHit(PaddleVelocityTracker tracker, Vector3 normal, Vector3 hitPoint, Collider hitCollider)
    {
        if (Time.time - _lastPaddleHitTime < paddleHitCooldown) return;
        if (tracker == null || _rb == null) return;

        var wasHeld = IsHeld;
        var surfaceVelocity = tracker.GetSurfaceVelocity(hitPoint);
        if (wasHeld && tracker.Speed < heldBallMinimumSwingSpeed && Vector3.Dot(surfaceVelocity, Vector3.forward) < minimumClosingSpeed)
        {
            return;
        }

        var incomingVelocity = wasHeld ? Vector3.zero : _rb.velocity;
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = EstimatePaddleFaceNormal(tracker.transform, incomingVelocity, surfaceVelocity);
        }

        normal = BlendTowardPaddleFace(normal, tracker.transform, incomingVelocity, surfaceVelocity);

        var input = PingPongHitSolver.CreateDefault(incomingVelocity, _rb.angularVelocity, normal, surfaceVelocity);
        input.normalRestitution = 0.78f;
        input.tangentialFriction = 0.58f;
        input.spinTransfer = 0.55f;
        input.minimumClosingSpeed = minimumClosingSpeed;
        input.minimumSpeed = wasHeld ? heldBallHitSpeed : minimumPaddleHitSpeed;
        input.maximumSpeed = maxSpeed;
        input.upwardBias = upwardBoost;
        input.preferredForward = Vector3.forward;
        input.minimumForwardDot = wasHeld ? 0.38f : 0.08f;
        input.forwardBlend = wasHeld ? 0.82f : 0.55f;
        input.biasTowardPreferredForward = true;

        var result = PingPongHitSolver.Solve(input);
        if (!result.accepted)
        {
            return;
        }

        _lastPaddleHitTime = Time.time;

        var velocity = PingPongHitSolver.ApplyPaddleContactPlacement(
            result.velocity,
            tracker.GetCenteredLocalHit(hitPoint, hitCollider),
            1.15f,
            0.35f);

        if (velocity.z < 0.5f)
        {
            velocity.z = Mathf.Lerp(velocity.z, forwardBoost, 0.45f);
        }

        velocity += Vector3.forward * Mathf.Max(0f, Vector3.Dot(surfaceVelocity, Vector3.forward)) * paddleVelocityMultiplier * 0.18f;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        if (_activeGrabber != null && _activeGrabber.ForceRelease(this, velocity))
        {
            _activeGrabber = null;
        }
        else
        {
            DetachFromGrabIfNeeded();
            _rb.velocity = velocity;
        }

        _rb.angularVelocity = Vector3.ClampMagnitude(result.angularVelocity, 180f);
        _lastSweepPosition = transform.position;

        if (!_hitRegistered)
        {
            _hitRegistered = true;
            PingPongEvents.BallHit();
        }
    }

    private void ApplySurfaceBounce(PingPongSurface surface, Collider collider, Vector3 normal, Vector3 contactPoint, bool forcePositionCorrection)
    {
        if (surface == null || collider == null || _rb == null || _rb.isKinematic) return;
        if (_lastSurfaceCollider == collider && Time.time - _lastSurfaceHitTime < surfaceHitCooldown) return;

        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = PingPongSurface.EstimateNormal(collider, transform.position, _rb.velocity);
        }

        normal.Normalize();
        if (Vector3.Dot(normal, _rb.velocity) > 0f)
        {
            normal = -normal;
        }

        var closingSpeed = -Vector3.Dot(_rb.velocity, normal);
        if (closingSpeed < 0.02f) return;

        var input = PingPongHitSolver.CreateDefault(_rb.velocity, _rb.angularVelocity, normal, Vector3.zero);
        input.normalRestitution = surface.normalRestitution;
        input.tangentialFriction = surface.tangentialFriction;
        input.spinTransfer = 0.35f;
        input.minimumClosingSpeed = 0.02f;
        input.minimumSpeed = 0f;
        input.maximumSpeed = maxSpeed;
        input.biasTowardPreferredForward = false;

        var result = PingPongHitSolver.Solve(input);
        if (!result.accepted) return;

        if (forcePositionCorrection)
        {
            transform.position = CorrectedSurfacePosition(contactPoint, normal);
        }

        _rb.velocity = result.velocity;
        _rb.angularVelocity = Vector3.ClampMagnitude(result.angularVelocity, 180f);
        _lastSurfaceCollider = collider;
        _lastSurfaceHitTime = Time.time;
        _lastSweepPosition = transform.position;
    }

    private void TryApplySweptSurfaceFallback(Vector3 start, Vector3 end)
    {
        var delta = end - start;
        var distance = delta.magnitude;
        if (distance <= 0.0001f) return;

        var radius = GetWorldRadius();
        var direction = delta / distance;
        var count = Physics.SphereCastNonAlloc(
            start,
            radius,
            direction,
            _sweepHits,
            distance,
            sweptSurfaceLayers,
            QueryTriggerInteraction.Collide);

        RaycastHit bestHit = new RaycastHit();
        PingPongSurface bestSurface = null;
        var bestDistance = float.MaxValue;

        for (var i = 0; i < count; i++)
        {
            var hit = _sweepHits[i];
            if (hit.collider == null || hit.collider.GetComponentInParent<PingPongBall>() == this) continue;

            var surface = PingPongSurface.Find(hit.collider);
            if (surface == null || !surface.useSweptFallback) continue;
            if (hit.collider.isTrigger &&
                surface.surfaceType != PingPongSurfaceType.Net &&
                surface.surfaceType != PingPongSurfaceType.PaddleHitZone)
            {
                continue;
            }

            if (hit.distance >= bestDistance) continue;

            bestDistance = hit.distance;
            bestHit = hit;
            bestSurface = surface;
        }

        if (bestSurface == null) return;

        var normal = bestHit.normal.sqrMagnitude > 0.0001f
            ? bestHit.normal
            : PingPongSurface.EstimateNormal(bestHit.collider, transform.position, _rb.velocity);

        if (bestSurface.IsPaddleSurface)
        {
            var tracker = bestHit.collider.GetComponentInParent<PaddleVelocityTracker>();
            if (tracker != null)
            {
                ApplyPaddleHit(tracker, normal, bestHit.point, bestHit.collider);
                return;
            }
        }

        transform.position = CorrectedSurfacePosition(bestHit.point, normal);
        ApplySurfaceBounce(bestSurface, bestHit.collider, normal, bestHit.point, false);
    }

    private Vector3 CorrectedSurfacePosition(Vector3 surfacePoint, Vector3 normal)
    {
        if (surfacePoint == Vector3.zero)
        {
            surfacePoint = transform.position;
        }

        return surfacePoint + normal.normalized * (GetWorldRadius() + 0.002f);
    }

    private void DetachFromGrabIfNeeded()
    {
        if (!_rb.isKinematic) return;

        transform.SetParent(null, true);
        _rb.isKinematic = false;
        _rb.useGravity = true;
    }

    private float GetWorldRadius()
    {
        if (_sphereCollider == null)
        {
            _sphereCollider = GetComponent<SphereCollider>();
        }

        if (_sphereCollider == null) return PingPongGeometry.BallRadius;

        var scale = _sphereCollider.transform.lossyScale;
        var maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return _sphereCollider.radius * maxScale;
    }

    private static Vector3 EstimatePaddleFaceNormal(Transform paddle, Vector3 incomingVelocity, Vector3 surfaceVelocity)
    {
        if (paddle == null) return Vector3.forward;

        var normal = paddle.up;
        var relativeVelocity = incomingVelocity - surfaceVelocity;
        if (relativeVelocity.sqrMagnitude > 0.0001f && Vector3.Dot(normal, relativeVelocity) > 0f)
        {
            normal = -normal;
        }
        else if (relativeVelocity.sqrMagnitude <= 0.0001f && Vector3.Dot(normal, Vector3.forward) < 0f)
        {
            normal = -normal;
        }

        return normal;
    }

    private static Vector3 BlendTowardPaddleFace(Vector3 collisionNormal, Transform paddle, Vector3 incomingVelocity, Vector3 surfaceVelocity)
    {
        if (paddle == null) return collisionNormal;

        var faceNormal = EstimatePaddleFaceNormal(paddle, incomingVelocity, surfaceVelocity);
        if (collisionNormal.sqrMagnitude < 0.0001f) return faceNormal;

        collisionNormal.Normalize();
        if (Vector3.Dot(collisionNormal, faceNormal) < 0.45f)
        {
            return faceNormal;
        }

        return Vector3.Slerp(collisionNormal, faceNormal, 0.35f);
    }
}
