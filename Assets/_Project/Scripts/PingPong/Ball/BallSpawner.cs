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
        var rb = ballObj.GetComponent<Rigidbody>();
        if (rb == null) rb = ballObj.AddComponent<Rigidbody>();

        Vector3 target = targetPoint.position;
        target.x += Random.Range(-horizontalRandomRange, horizontalRandomRange);
        target.y += Random.Range(-verticalRandomRange, verticalRandomRange);

        var dir = (target - spawnPoint.position).normalized;
        dir.y += upwardArc;
        dir.Normalize();
        rb.velocity = dir * serveSpeed;

        PingPongEvents.BallServed();
    }
}
