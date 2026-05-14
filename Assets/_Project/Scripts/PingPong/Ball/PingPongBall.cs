using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PingPongBall : MonoBehaviour
{
    public const float DefaultMaxAngularVelocity = 180f;

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
    public bool ignoreNonGameplayColliders = true;
    public float collisionFilterRefreshInterval = 0.2f;
    public string ballPhysicsLayerName = "Ball";
    public string ignoredRoomSensingLayerName = "RoomSensing";
    public bool useAerodynamics = true;
    public float airDensity = 1.27f;
    public float dragCoefficient = 0.5f;
    public float magnusLiftCoefficient = 0.28f;
    public float maximumAerodynamicAcceleration = 45f;
    public float maxAngularVelocity = DefaultMaxAngularVelocity;
    [Range(0f, 1f)] public float rawPaddleVelocityBlend = 0.28f;
    [Range(0f, 1f)] public float highAccelerationRawBlend = 0.34f;

    private readonly RaycastHit[] _sweepHits = new RaycastHit[16];
    private Rigidbody _rb;
    private SphereCollider _sphereCollider;
    private ControllerBallGrabber _activeGrabber;
    private Collider _lastSurfaceCollider;
    private Vector3 _lastSweepPosition;
    private Vector3 _lastPhysicsVelocity;
    private Vector3 _lastPhysicsAngularVelocity;
    private bool _hitRegistered;
    private float _ignoreGrabUntilTime;
    private float _lastPaddleHitTime = -1f;
    private float _lastSurfaceHitTime = -1f;
    private float _nextCollisionFilterRefreshTime;

    public bool IsGrabbed => _activeGrabber != null;
    public bool CanBeGrabbed => !IsGrabbed && Time.time >= _ignoreGrabUntilTime;
    public bool HasRegisteredHit => _hitRegistered;

    private bool IsHeld => IsGrabbed || (_rb != null && _rb.isKinematic && transform.parent != null);

    private void Awake()
    {
        ConfigureRigidbody();
        ConfigureGameplayCollisionFilter(true);
        _sphereCollider = GetComponent<SphereCollider>();
        _lastSweepPosition = transform.position;
    }

    private void OnEnable()
    {
        _lastSweepPosition = transform.position;
        _lastSurfaceCollider = null;
        _lastSurfaceHitTime = -1f;
        ConfigureGameplayCollisionFilter(true);
    }

    public void ExcludeIgnoredRoomSensingLayerFromSweep()
    {
        if (string.IsNullOrEmpty(ignoredRoomSensingLayerName)) return;

        var ignoredLayer = LayerMask.NameToLayer(ignoredRoomSensingLayerName);
        if (ignoredLayer < 0) return;

        sweptSurfaceLayers = sweptSurfaceLayers & ~(1 << ignoredLayer);
    }

    public void ConfigureGameplayCollisionFilter(bool forceRefresh = false)
    {
        ConfigureBallLayer();
        ExcludeIgnoredRoomSensingLayerFromSweep();

        if (forceRefresh)
        {
            _nextCollisionFilterRefreshTime = 0f;
        }

        RefreshIgnoredNonGameplayColliders();
    }

    private void FixedUpdate()
    {
        if (_rb == null || _rb.isKinematic)
        {
            _lastSweepPosition = transform.position;
            return;
        }

        _lastPhysicsVelocity = _rb.velocity;
        _lastPhysicsAngularVelocity = _rb.angularVelocity;
        RefreshIgnoredNonGameplayColliders();
        ApplyAerodynamics();

        if (!enableSweptSurfaceFallback)
        {
            _lastSweepPosition = transform.position;
            return;
        }

        TryApplySweptSurfaceFallback(_lastSweepPosition, transform.position);
        _lastSweepPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (TryIgnoreNonGameplayCollision(collision))
        {
            return;
        }

        var tracker = collision.collider.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker != null)
        {
            var hitPoint = transform.position;
            var surfaceVelocity = GetResponsiveSurfaceVelocity(tracker, hitPoint);
            var normal = EstimatePaddleFaceNormal(tracker.transform, _rb.velocity, surfaceVelocity);
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

    private void OnCollisionStay(Collision collision)
    {
        TryIgnoreNonGameplayCollision(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyTriggerInteraction(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyTriggerInteraction(other);
    }

    private void ConfigureBallLayer()
    {
        if (string.IsNullOrEmpty(ballPhysicsLayerName)) return;

        var ballLayer = LayerMask.NameToLayer(ballPhysicsLayerName);
        if (ballLayer < 0) return;

        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child != null)
            {
                child.gameObject.layer = ballLayer;
            }
        }
    }

    private void RefreshIgnoredNonGameplayColliders()
    {
        if (!ignoreNonGameplayColliders) return;
        if (!Application.isPlaying) return;
        if (Time.time < _nextCollisionFilterRefreshTime) return;

        _nextCollisionFilterRefreshTime = Time.time + Mathf.Max(0.02f, collisionFilterRefreshInterval);

        var ownColliders = GetComponentsInChildren<Collider>(true);
        if (ownColliders == null || ownColliders.Length == 0) return;

        foreach (var candidate in FindObjectsOfType<Collider>(false))
        {
            if (candidate == null || !candidate.enabled) continue;
            if (IsOwnCollider(candidate, ownColliders)) continue;

            var ignoreCollision = !IsGameplayCollider(candidate);

            foreach (var ownCollider in ownColliders)
            {
                if (ownCollider != null && ownCollider.enabled)
                {
                    Physics.IgnoreCollision(ownCollider, candidate, ignoreCollision);
                }
            }
        }
    }

    private bool TryIgnoreNonGameplayCollision(Collision collision)
    {
        if (!ignoreNonGameplayColliders || collision == null || collision.collider == null) return false;
        if (IsGameplayCollider(collision.collider)) return false;

        var ownColliders = GetComponentsInChildren<Collider>(true);
        foreach (var ownCollider in ownColliders)
        {
            if (ownCollider != null && ownCollider.enabled)
            {
                Physics.IgnoreCollision(ownCollider, collision.collider, true);
            }
        }

        if (_rb != null && !_rb.isKinematic)
        {
            _rb.velocity = _lastPhysicsVelocity;
            _rb.angularVelocity = _lastPhysicsAngularVelocity;
        }

        _lastSweepPosition = transform.position;
        return true;
    }

    private bool IsGameplayCollider(Collider candidate)
    {
        if (candidate == null) return false;
        if (candidate.GetComponentInParent<PingPongBall>() != null) return false;
        if (candidate.GetComponentInParent<PlayerTableBoundary>() != null) return false;
        if (candidate.GetComponentInParent<PaddleVelocityTracker>() != null) return true;

        var surface = candidate.GetComponent<PingPongSurface>() ?? candidate.GetComponentInParent<PingPongSurface>();
        if (surface != null)
        {
            return IsGameplaySurface(surface.surfaceType);
        }

        return HasGameplayName(candidate.transform) && HasAncestorNamed(candidate.transform, "PingPong");
    }

    private static bool IsGameplaySurface(PingPongSurfaceType surfaceType)
    {
        return surfaceType == PingPongSurfaceType.Table ||
               surfaceType == PingPongSurfaceType.Net ||
               surfaceType == PingPongSurfaceType.PaddleBody ||
               surfaceType == PingPongSurfaceType.PaddleHitZone;
    }

    private static bool HasGameplayName(Transform transform)
    {
        while (transform != null)
        {
            var lowerName = transform.name.ToLowerInvariant();
            if (lowerName.Contains("table") ||
                lowerName.Contains("net") ||
                lowerName.Contains("paddle") ||
                lowerName.Contains("racket"))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool HasAncestorNamed(Transform transform, string ancestorName)
    {
        while (transform != null)
        {
            if (transform.name == ancestorName)
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool IsOwnCollider(Collider candidate, Collider[] ownColliders)
    {
        foreach (var ownCollider in ownColliders)
        {
            if (candidate == ownCollider)
            {
                return true;
            }
        }

        return false;
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
        _rb.drag = useAerodynamics ? 0f : PingPongGeometry.BallDrag;
        _rb.angularDrag = PingPongGeometry.BallAngularDrag;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        ConfigureSpinLimit(_rb, maxAngularVelocity);
    }

    public static void ConfigureSpinLimit(Rigidbody rb, float requiredAngularVelocity)
    {
        if (rb == null) return;

        rb.maxAngularVelocity = Mathf.Max(
            rb.maxAngularVelocity,
            Mathf.Max(DefaultMaxAngularVelocity, Mathf.Max(0f, requiredAngularVelocity)));
    }

    private void ApplyAerodynamics()
    {
        if (!useAerodynamics || _rb == null || _rb.isKinematic) return;

        var velocity = _rb.velocity;
        if (velocity.sqrMagnitude < 0.0001f) return;

        var acceleration = CalculateAerodynamicAcceleration(
            velocity,
            _rb.angularVelocity,
            GetWorldRadius(),
            _rb.mass,
            airDensity,
            dragCoefficient,
            magnusLiftCoefficient,
            maximumAerodynamicAcceleration);

        if (!IsFinite(acceleration) || acceleration.sqrMagnitude < 0.0001f) return;

        _rb.AddForce(acceleration, ForceMode.Acceleration);
    }

    public static Vector3 CalculateAerodynamicAcceleration(
        Vector3 velocity,
        Vector3 angularVelocity,
        float radius,
        float mass,
        float airDensity,
        float dragCoefficient,
        float magnusLiftCoefficient,
        float maximumAcceleration)
    {
        if (velocity.sqrMagnitude < 0.0001f) return Vector3.zero;

        var safeRadius = Mathf.Max(radius, 0.001f);
        var area = Mathf.PI * safeRadius * safeRadius;
        var inverseMass = 1f / Mathf.Max(mass, 0.0001f);
        var dragAcceleration = -0.5f * Mathf.Max(0f, airDensity) * Mathf.Max(0f, dragCoefficient) * area * velocity.magnitude * velocity * inverseMass;
        var magnusAcceleration = 0.5f * Mathf.Max(0f, airDensity) * Mathf.Max(0f, magnusLiftCoefficient) * area * safeRadius * Vector3.Cross(angularVelocity, velocity) * inverseMass;
        var acceleration = dragAcceleration + magnusAcceleration;
        if (!IsFinite(acceleration)) return Vector3.zero;
        return Vector3.ClampMagnitude(acceleration, Mathf.Max(0f, maximumAcceleration));
    }

    private void TryApplyTriggerInteraction(Collider other)
    {
        var tracker = other.GetComponentInParent<PaddleVelocityTracker>();
        if (tracker != null)
        {
            var paddleSpeed = Mathf.Max(tracker.Speed, tracker.RawVelocity.magnitude);
            if (!IsHeld && paddleSpeed < 0.2f && _rb.velocity.sqrMagnitude > 0.02f) return;

            var surfaceVelocity = GetResponsiveSurfaceVelocity(tracker, transform.position);
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
        var surfaceVelocity = GetResponsiveSurfaceVelocity(tracker, hitPoint);
        var paddleSpeed = Mathf.Max(tracker.Speed, tracker.RawVelocity.magnitude);
        if (wasHeld && paddleSpeed < heldBallMinimumSwingSpeed && Vector3.Dot(surfaceVelocity, Vector3.forward) < minimumClosingSpeed)
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

        var finalAngularVelocity = Vector3.ClampMagnitude(result.angularVelocity, DefaultMaxAngularVelocity);
        if (_activeGrabber != null && _activeGrabber.ForceRelease(this, velocity))
        {
            _activeGrabber = null;
        }
        else
        {
            DetachFromGrabIfNeeded();
            _rb.velocity = velocity;
        }

        _rb.angularVelocity = finalAngularVelocity;
        _lastSweepPosition = transform.position;

        var firstHitForBall = !_hitRegistered;
        PingPongEvents.BallHit(
            new BallHitInfo(
                gameObject,
                hitCollider,
                wasHeld ? PingPongHitType.HeldBallPaddle : PingPongHitType.Paddle,
                hitPoint,
                normal,
                incomingVelocity,
                velocity,
                surfaceVelocity,
                finalAngularVelocity,
                result.closingSpeed,
                firstHitForBall),
            firstHitForBall);

        if (!_hitRegistered)
        {
            _hitRegistered = true;
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

        var incomingVelocity = _rb.velocity;
        var input = PingPongHitSolver.CreateDefault(incomingVelocity, _rb.angularVelocity, normal, Vector3.zero);
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

        var finalAngularVelocity = Vector3.ClampMagnitude(result.angularVelocity, DefaultMaxAngularVelocity);
        _rb.velocity = result.velocity;
        _rb.angularVelocity = finalAngularVelocity;
        _lastSurfaceCollider = collider;
        _lastSurfaceHitTime = Time.time;
        _lastSweepPosition = transform.position;

        PingPongEvents.SurfaceBounce(new SurfaceBounceInfo(
            gameObject,
            collider,
            surface.surfaceType,
            contactPoint,
            normal,
            incomingVelocity,
            result.velocity,
            finalAngularVelocity,
            result.closingSpeed,
            forcePositionCorrection));
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
            if (!IsGameplayCollider(hit.collider)) continue;

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

    private Vector3 GetResponsiveSurfaceVelocity(PaddleVelocityTracker tracker, Vector3 worldPoint)
    {
        if (tracker == null) return Vector3.zero;

        var smoothed = tracker.GetSurfaceVelocity(worldPoint);
        var raw = tracker.GetRawSurfaceVelocity(worldPoint);
        var accelerationBlend = Mathf.InverseLerp(6f, 24f, tracker.RawAcceleration.magnitude) * highAccelerationRawBlend;
        return Vector3.Lerp(smoothed, raw, Mathf.Clamp01(rawPaddleVelocityBlend + accelerationBlend));
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

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) &&
               !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
    }
}
