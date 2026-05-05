using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public TMP_Text hitText;
    public TMP_Text servedText;
    public TMP_Text missedText;
    public TMP_Text accuracyText;
    public TMP_Text lastSpeedText;
    public TMP_Text lastSpinText;

    private int _servedCount;
    private int _hitCount;
    private int _missedCount;
    private float _lastHitSpeed;
    private float _lastSpinSpeed;

    private void OnEnable()
    {
        PingPongEvents.OnBallServed += HandleBallServed;
        PingPongEvents.OnBallHit += HandleBallHit;
        PingPongEvents.OnBallHitDetailed += HandleBallHitDetailed;
        PingPongEvents.OnBallMissed += HandleBallMissed;
        RefreshUI();
    }

    private void OnDisable()
    {
        PingPongEvents.OnBallServed -= HandleBallServed;
        PingPongEvents.OnBallHit -= HandleBallHit;
        PingPongEvents.OnBallHitDetailed -= HandleBallHitDetailed;
        PingPongEvents.OnBallMissed -= HandleBallMissed;
    }

    public void ResetScore()
    {
        _servedCount = 0;
        _hitCount = 0;
        _missedCount = 0;
        _lastHitSpeed = 0f;
        _lastSpinSpeed = 0f;
        RefreshUI();
    }

    private void HandleBallServed()
    {
        _servedCount++;
        RefreshUI();
    }

    private void HandleBallHit()
    {
        _hitCount++;
        RefreshUI();
    }

    private void HandleBallHitDetailed(BallHitInfo info)
    {
        _lastHitSpeed = info.OutgoingSpeed;
        _lastSpinSpeed = info.SpinSpeed;
        RefreshUI();
    }

    private void HandleBallMissed()
    {
        _missedCount++;
        RefreshUI();
    }

    private void RefreshUI()
    {
        var acc = _servedCount > 0 ? (float)_hitCount / _servedCount * 100f : 0f;

        if (hitText != null) hitText.text = $"Hit: {_hitCount}";
        if (servedText != null) servedText.text = $"Served: {_servedCount}";
        if (missedText != null) missedText.text = $"Missed: {_missedCount}";
        if (accuracyText != null) accuracyText.text = $"Accuracy: {acc:0.0}%";
        if (lastSpeedText != null) lastSpeedText.text = $"Speed: {_lastHitSpeed:0.0} m/s";
        if (lastSpinText != null) lastSpinText.text = $"Spin: {_lastSpinSpeed:0} rad/s";
    }
}
