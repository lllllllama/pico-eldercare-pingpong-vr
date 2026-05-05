using System.Collections;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform spawnPoint;
    public Transform targetPoint;
    public Transform ballContainer;
    public bool autoStartOnPlay = true;

    public float serveInterval = 4.0f;
    public float serveSpeed = 2.6f;
    public PingPongServeProfile serveProfile = PingPongServeProfile.RandomMixed;
    public float upwardArc = 0.35f;
    public float minimumNetClearanceHeight = PingPongGeometry.TableTopHeight + PingPongGeometry.NetHeight + 0.08f;
    public float netWorldZ = PingPongGeometry.TableCenter.z;
    public bool bounceOnTableBeforePlayer = true;
    public float tableBounceWorldY = PingPongGeometry.TableTopHeight + PingPongGeometry.BallRadius;
    public float tableBounceWorldZ = 1.45f;
    public float horizontalRandomRange = 0.18f;
    public float verticalRandomRange = 0.08f;
    public float topspinRadiansPerSecond = 95f;
    public float backspinRadiansPerSecond = 80f;
    public float sidespinRadiansPerSecond = 50f;
    [Range(0f, 1f)] public float serveSpinRandomness = 0.18f;
    public float maxServeSpin = 140f;

    private static PhysicMaterial _ballPhysicsMaterial;
    private Coroutine _serveRoutine;

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartServing();
        }
    }

    public void StartServing()
    {
        if (_serveRoutine == null)
        {
            _serveRoutine = StartCoroutine(ServeLoop());
            PingPongEvents.TrainingStarted();
        }
    }

    public void StopServing()
    {
        if (_serveRoutine != null)
        {
            StopCoroutine(_serveRoutine);
            _serveRoutine = null;
            PingPongEvents.TrainingFinished();
        }
    }

    public void ClearBalls()
    {
        if (ballContainer == null) return;
        for (int i = ballContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(ballContainer.GetChild(i).gameObject);
        }
    }

    private IEnumerator ServeLoop()
    {
        while (true)
        {
            SpawnBall();
            yield return new WaitForSeconds(serveInterval);
        }
    }

    private void SpawnBall()
    {
        if (ballPrefab == null || spawnPoint == null || targetPoint == null) return;

        var ballObj = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity, ballContainer);
        ballObj.transform.localScale = PingPongGeometry.BallPrefabScale;
        var rb = ConfigureSpawnedBall(ballObj);
        if (rb == null) return;

        Vector3 target = targetPoint.position;
        target.x += Random.Range(-horizontalRandomRange, horizontalRandomRange);
        target.y += Random.Range(-verticalRandomRange, verticalRandomRange);

        var trajectoryTarget = target;
        if (bounceOnTableBeforePlayer)
        {
            trajectoryTarget = new Vector3(target.x, tableBounceWorldY, tableBounceWorldZ);
        }

        var velocity = CalculateServeVelocity(spawnPoint.position, trajectoryTarget);
        var actualProfile = SelectServeProfile();
        var spin = CalculateProfileSpin(actualProfile, velocity, topspinRadiansPerSecond, backspinRadiansPerSecond, sidespinRadiansPerSecond);
        spin = ApplySpinRandomness(spin);

        rb.velocity = velocity;
        PingPongBall.ConfigureSpinLimit(rb, maxServeSpin);
        rb.angularVelocity = Vector3.ClampMagnitude(spin, maxServeSpin);

        PingPongEvents.BallServed(new BallServedInfo(ballObj, ballObj.transform.position, rb.velocity, rb.angularVelocity, actualProfile));
    }

    private Vector3 CalculateServeVelocity(Vector3 start, Vector3 target)
    {
        var horizontalDelta = new Vector3(target.x - start.x, 0f, target.z - start.z);
        var horizontalDistance = horizontalDelta.magnitude;
        if (horizontalDistance <= 0.001f)
        {
            return (target - start).normalized * serveSpeed;
        }

        var timeToTarget = horizontalDistance / Mathf.Max(serveSpeed, 0.1f);
        var arcFactor = Mathf.Lerp(0.92f, 1.18f, Mathf.Clamp01(upwardArc));
        timeToTarget = Mathf.Clamp(timeToTarget * arcFactor, 0.55f, 1.05f);

        var velocity = horizontalDelta / timeToTarget;
        velocity.y = (target.y - start.y - 0.5f * Physics.gravity.y * timeToTarget * timeToTarget) / timeToTarget;

        if (Mathf.Abs(velocity.z) > 0.001f)
        {
            var timeToNet = (netWorldZ - start.z) / velocity.z;
            if (timeToNet > 0f && timeToNet < timeToTarget)
            {
                var yAtNet = start.y + velocity.y * timeToNet + 0.5f * Physics.gravity.y * timeToNet * timeToNet;
                if (yAtNet < minimumNetClearanceHeight)
                {
                    velocity.y += (minimumNetClearanceHeight - yAtNet) / timeToNet;
                }
            }
        }

        return velocity;
    }

    public static Vector3 CalculateProfileSpin(
        PingPongServeProfile profile,
        Vector3 launchVelocity,
        float topspinRadiansPerSecond,
        float backspinRadiansPerSecond,
        float sidespinRadiansPerSecond)
    {
        var flatVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);
        if (flatVelocity.sqrMagnitude < 0.0001f)
        {
            flatVelocity = Vector3.back;
        }

        var forwardRollAxis = Vector3.Cross(Vector3.up, flatVelocity.normalized);
        if (forwardRollAxis.sqrMagnitude < 0.0001f)
        {
            forwardRollAxis = Vector3.right;
        }

        forwardRollAxis.Normalize();

        switch (profile)
        {
            case PingPongServeProfile.Topspin:
                return forwardRollAxis * Mathf.Max(0f, topspinRadiansPerSecond);
            case PingPongServeProfile.Backspin:
                return -forwardRollAxis * Mathf.Max(0f, backspinRadiansPerSecond);
            case PingPongServeProfile.Sidespin:
                return Vector3.up * Mathf.Max(0f, sidespinRadiansPerSecond);
            default:
                return Vector3.zero;
        }
    }

    private PingPongServeProfile SelectServeProfile()
    {
        if (serveProfile != PingPongServeProfile.RandomMixed)
        {
            return serveProfile;
        }

        var roll = Random.value;
        if (roll < 0.25f) return PingPongServeProfile.Basic;
        if (roll < 0.58f) return PingPongServeProfile.Topspin;
        if (roll < 0.84f) return PingPongServeProfile.Backspin;
        return PingPongServeProfile.Sidespin;
    }

    private Vector3 ApplySpinRandomness(Vector3 spin)
    {
        var spinMagnitude = spin.magnitude;
        if (spinMagnitude <= 0.001f || serveSpinRandomness <= 0f)
        {
            return spin;
        }

        return spin + Random.insideUnitSphere * (spinMagnitude * serveSpinRandomness);
    }

    private static Rigidbody ConfigureSpawnedBall(GameObject ballObj)
    {
        var rb = ballObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = ballObj.AddComponent<Rigidbody>();
        }

        if (rb == null) return null;

        rb.mass = PingPongGeometry.BallMass;
        var pingPongBall = ballObj.GetComponent<PingPongBall>();
        if (pingPongBall == null)
        {
            pingPongBall = ballObj.AddComponent<PingPongBall>();
        }

        rb.drag = pingPongBall != null && pingPongBall.useAerodynamics ? 0f : PingPongGeometry.BallDrag;
        rb.angularDrag = PingPongGeometry.BallAngularDrag;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        PingPongBall.ConfigureSpinLimit(rb, pingPongBall != null ? pingPongBall.maxAngularVelocity : PingPongBall.DefaultMaxAngularVelocity);

        var collider = ballObj.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = ballObj.AddComponent<SphereCollider>();
        }

        ballObj.transform.localScale = PingPongGeometry.BallPrefabScale;
        collider.radius = 0.5f;
        collider.isTrigger = false;
        collider.sharedMaterial = GetBallPhysicsMaterial();

        if (ballObj.GetComponent<BallLifetime>() == null) ballObj.AddComponent<BallLifetime>();

        return rb;
    }

    private static PhysicMaterial GetBallPhysicsMaterial()
    {
        if (_ballPhysicsMaterial != null) return _ballPhysicsMaterial;

        _ballPhysicsMaterial = new PhysicMaterial("PingPongBallPhysics")
        {
            bounciness = 0.72f,
            dynamicFriction = 0.02f,
            staticFriction = 0.02f,
            bounceCombine = PhysicMaterialCombine.Maximum,
            frictionCombine = PhysicMaterialCombine.Minimum
        };
        return _ballPhysicsMaterial;
    }
}
