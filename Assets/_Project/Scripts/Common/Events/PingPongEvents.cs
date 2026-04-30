using System;

public static class PingPongEvents
{
    public static event Action OnBallServed;
    public static event Action OnBallHit;
    public static event Action OnBallMissed;
    public static event Action OnTrainingStarted;
    public static event Action OnTrainingFinished;

    public static void BallServed() => OnBallServed?.Invoke();
    public static void BallHit() => OnBallHit?.Invoke();
    public static void BallMissed() => OnBallMissed?.Invoke();
    public static void TrainingStarted() => OnTrainingStarted?.Invoke();
    public static void TrainingFinished() => OnTrainingFinished?.Invoke();
}
