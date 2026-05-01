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
    public float upwardArc = 0.35f;
    public float minimumNetClearanceHeight = 1.25f;
    public float netWorldZ = 2.0f;
    public float horizontalRandomRange = 0.18f;
    public float verticalRandomRange = 0.08f;

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
        var rb = ConfigureSpawnedBall(ballObj);
        if (rb == null) return;

        Vector3 target = targetPoint.position;
        target.x += Random.Range(-horizontalRandomRange, horizontalRandomRange);
        target.y += Random.Range(-verticalRandomRange, verticalRandomRange);

        rb.velocity = CalculateServeVelocity(spawnPoint.position, target);
        rb.angularVelocity = Vector3.zero;

        PingPongEvents.BallServed();
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
        timeToTarget = Mathf.Clamp(timeToTarget, 0.55f, 0.9f);

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

    private static Rigidbody ConfigureSpawnedBall(GameObject ballObj)
    {
        var rb = ballObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = ballObj.AddComponent<Rigidbody>();
        }

        if (rb == null) return null;

        rb.mass = 0.0027f;
        rb.drag = 0.03f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (ballObj.GetComponent<Collider>() == null)
        {
            var collider = ballObj.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
        }

        if (ballObj.GetComponent<PingPongBall>() == null) ballObj.AddComponent<PingPongBall>();
        if (ballObj.GetComponent<BallLifetime>() == null) ballObj.AddComponent<BallLifetime>();

        return rb;
    }
}
