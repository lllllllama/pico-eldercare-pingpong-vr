using UnityEngine;

public class BallLifetime : MonoBehaviour
{
    public float minY = -1f;
    public float maxDistanceFromOrigin = 20f;
    public float maxLifeSeconds = 15f;

    private float _spawnedAt;
    private bool _missedReported;

    private void OnEnable()
    {
        _spawnedAt = Time.time;
    }

    private void Update()
    {
        bool shouldDestroy = transform.position.y < minY ||
                             transform.position.magnitude > maxDistanceFromOrigin ||
                             (Time.time - _spawnedAt) > maxLifeSeconds;

        if (!shouldDestroy) return;

        ReportMissed();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        ReportMissed();
    }

    private void ReportMissed()
    {
        if (_missedReported) return;
        _missedReported = true;
        PingPongEvents.BallMissed();
    }
}
