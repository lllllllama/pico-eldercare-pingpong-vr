using UnityEngine;

public class BallLifetime : MonoBehaviour
{
    public float minY = -1f;
    public float maxDistanceFromOrigin = 20f;
    public float maxLifeSeconds = 15f;
    public bool reportMissOnlyIfUnhit = true;

    private float _spawnedAt;
    private bool _missedReported;

    private void OnEnable()
    {
        _spawnedAt = Time.time;
        _missedReported = false;
    }

    private void Update()
    {
        if (transform.position.y < minY)
        {
            DestroyAsMissed(PingPongMissReason.FellBelowFloor);
            return;
        }

        if (transform.position.magnitude > maxDistanceFromOrigin)
        {
            DestroyAsMissed(PingPongMissReason.OutOfBounds);
            return;
        }

        if ((Time.time - _spawnedAt) > maxLifeSeconds)
        {
            DestroyAsMissed(PingPongMissReason.TimedOut);
        }
    }

    private void DestroyAsMissed(PingPongMissReason reason)
    {
        ReportMissed(reason);
        Destroy(gameObject);
    }

    private void ReportMissed(PingPongMissReason reason)
    {
        if (_missedReported) return;
        var ball = GetComponent<PingPongBall>();
        if (reportMissOnlyIfUnhit && ball != null && ball.HasRegisteredHit) return;

        _missedReported = true;
        PingPongEvents.BallMissed(new BallMissedInfo(gameObject, transform.position, reason, Time.time - _spawnedAt));
    }
}
