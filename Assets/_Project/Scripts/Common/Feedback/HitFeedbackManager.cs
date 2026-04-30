using UnityEngine;

public class HitFeedbackManager : MonoBehaviour
{
    public AudioSource hitAudioSource;
    public GameObject hitEffectPrefab;
    public Transform effectSpawnPoint;

    private void OnEnable()
    {
        PingPongEvents.OnBallHit += HandleBallHit;
    }

    private void OnDisable()
    {
        PingPongEvents.OnBallHit -= HandleBallHit;
    }

    private void HandleBallHit()
    {
        if (hitAudioSource != null)
        {
            hitAudioSource.Play();
        }

        if (hitEffectPrefab != null)
        {
            var pos = effectSpawnPoint != null ? effectSpawnPoint.position : Vector3.zero;
            Instantiate(hitEffectPrefab, pos, Quaternion.identity);
        }
    }
}
