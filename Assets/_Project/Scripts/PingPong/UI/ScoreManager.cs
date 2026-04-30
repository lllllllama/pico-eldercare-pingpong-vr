using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public TMP_Text hitText;
    public TMP_Text servedText;
    public TMP_Text accuracyText;

    private int _servedCount;
    private int _hitCount;

    private void OnEnable()
    {
        PingPongEvents.OnBallServed += HandleBallServed;
        PingPongEvents.OnBallHit += HandleBallHit;
        RefreshUI();
    }

    private void OnDisable()
    {
        PingPongEvents.OnBallServed -= HandleBallServed;
        PingPongEvents.OnBallHit -= HandleBallHit;
    }

    public void ResetScore()
    {
        _servedCount = 0;
        _hitCount = 0;
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

    private void RefreshUI()
    {
        var acc = _servedCount > 0 ? (float)_hitCount / _servedCount * 100f : 0f;

        if (hitText != null) hitText.text = $"Hit: {_hitCount}";
        if (servedText != null) servedText.text = $"Served: {_servedCount}";
        if (accuracyText != null) accuracyText.text = $"Accuracy: {acc:0.0}%";
    }
}
