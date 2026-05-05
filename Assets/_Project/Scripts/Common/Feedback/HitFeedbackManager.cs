using UnityEngine;

public class HitFeedbackManager : MonoBehaviour
{
    public AudioSource hitAudioSource;
    public AudioSource bounceAudioSource;
    public AudioClip paddleHitClip;
    public AudioClip tableBounceClip;
    public AudioClip netBounceClip;
    public AudioClip fastSwingClip;
    public GameObject hitEffectPrefab;
    public Transform effectSpawnPoint;
    public float minAudibleSpeed = 0.25f;
    public float fullVolumeSpeed = 8f;
    public float fastSwingSpeed = 5.2f;
    [Range(0f, 1f)] public float fastSwingVolume = 0.28f;

    private void OnEnable()
    {
        PingPongEvents.OnBallHitDetailed += HandleBallHit;
        PingPongEvents.OnSurfaceBounce += HandleSurfaceBounce;
    }

    private void OnDisable()
    {
        PingPongEvents.OnBallHitDetailed -= HandleBallHit;
        PingPongEvents.OnSurfaceBounce -= HandleSurfaceBounce;
    }

    private void HandleBallHit(BallHitInfo info)
    {
        var speed = Mathf.Max(info.OutgoingSpeed, Mathf.Abs(info.closingSpeed), info.PaddleSpeed);
        PlayClip(hitAudioSource, paddleHitClip, info.contactPoint, speed, 0.95f, 1.18f);

        if (fastSwingClip != null && info.PaddleSpeed >= fastSwingSpeed)
        {
            PlayClip(hitAudioSource, fastSwingClip, info.contactPoint, info.PaddleSpeed, 1f, 1.06f, fastSwingVolume);
        }

        if (hitEffectPrefab != null)
        {
            var pos = info.contactPoint != Vector3.zero ? info.contactPoint : effectSpawnPoint != null ? effectSpawnPoint.position : Vector3.zero;
            Instantiate(hitEffectPrefab, pos, Quaternion.identity);
        }
    }

    private void HandleSurfaceBounce(SurfaceBounceInfo info)
    {
        if (info.surfaceType != PingPongSurfaceType.Table && info.surfaceType != PingPongSurfaceType.Net)
        {
            return;
        }

        var clip = info.surfaceType == PingPongSurfaceType.Net && netBounceClip != null
            ? netBounceClip
            : tableBounceClip;
        var speed = Mathf.Max(info.IncomingSpeed, Mathf.Abs(info.closingSpeed));
        PlayClip(bounceAudioSource != null ? bounceAudioSource : hitAudioSource, clip, info.contactPoint, speed, 0.86f, 1.12f);
    }

    private void PlayClip(AudioSource source, AudioClip clip, Vector3 position, float speed, float minPitch, float maxPitch, float volumeScale = 1f)
    {
        if (source == null || speed < minAudibleSpeed) return;

        if (position != Vector3.zero)
        {
            source.transform.position = position;
        }

        var selectedClip = clip != null ? clip : source.clip;
        if (selectedClip == null) return;

        var normalized = Mathf.InverseLerp(minAudibleSpeed, Mathf.Max(minAudibleSpeed + 0.01f, fullVolumeSpeed), speed);
        source.pitch = Mathf.Lerp(minPitch, maxPitch, normalized);
        source.PlayOneShot(selectedClip, Mathf.Clamp01(normalized * volumeScale));
    }
}
