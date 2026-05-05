using System;
using UnityEngine;

public enum PingPongServeProfile
{
    Basic,
    Topspin,
    Backspin,
    Sidespin,
    RandomMixed
}

public enum PingPongHitType
{
    Paddle,
    HeldBallPaddle
}

public enum PingPongMissReason
{
    Unknown,
    FellBelowFloor,
    OutOfBounds,
    TimedOut
}

public struct BallServedInfo
{
    public GameObject ball;
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public PingPongServeProfile serveProfile;

    public float Speed => velocity.magnitude;
    public float SpinSpeed => angularVelocity.magnitude;

    public BallServedInfo(GameObject ball, Vector3 position, Vector3 velocity, Vector3 angularVelocity, PingPongServeProfile serveProfile)
    {
        this.ball = ball;
        this.position = position;
        this.velocity = velocity;
        this.angularVelocity = angularVelocity;
        this.serveProfile = serveProfile;
    }
}

public struct BallHitInfo
{
    public GameObject ball;
    public Collider collider;
    public PingPongHitType hitType;
    public Vector3 contactPoint;
    public Vector3 contactNormal;
    public Vector3 incomingVelocity;
    public Vector3 outgoingVelocity;
    public Vector3 paddleVelocity;
    public Vector3 outgoingAngularVelocity;
    public float closingSpeed;
    public bool firstHitForBall;

    public float IncomingSpeed => incomingVelocity.magnitude;
    public float OutgoingSpeed => outgoingVelocity.magnitude;
    public float PaddleSpeed => paddleVelocity.magnitude;
    public float SpinSpeed => outgoingAngularVelocity.magnitude;

    public BallHitInfo(
        GameObject ball,
        Collider collider,
        PingPongHitType hitType,
        Vector3 contactPoint,
        Vector3 contactNormal,
        Vector3 incomingVelocity,
        Vector3 outgoingVelocity,
        Vector3 paddleVelocity,
        Vector3 outgoingAngularVelocity,
        float closingSpeed,
        bool firstHitForBall)
    {
        this.ball = ball;
        this.collider = collider;
        this.hitType = hitType;
        this.contactPoint = contactPoint;
        this.contactNormal = contactNormal;
        this.incomingVelocity = incomingVelocity;
        this.outgoingVelocity = outgoingVelocity;
        this.paddleVelocity = paddleVelocity;
        this.outgoingAngularVelocity = outgoingAngularVelocity;
        this.closingSpeed = closingSpeed;
        this.firstHitForBall = firstHitForBall;
    }
}

public struct SurfaceBounceInfo
{
    public GameObject ball;
    public Collider collider;
    public PingPongSurfaceType surfaceType;
    public Vector3 contactPoint;
    public Vector3 contactNormal;
    public Vector3 incomingVelocity;
    public Vector3 outgoingVelocity;
    public Vector3 outgoingAngularVelocity;
    public float closingSpeed;
    public bool positionCorrected;

    public float IncomingSpeed => incomingVelocity.magnitude;
    public float OutgoingSpeed => outgoingVelocity.magnitude;
    public float SpinSpeed => outgoingAngularVelocity.magnitude;

    public SurfaceBounceInfo(
        GameObject ball,
        Collider collider,
        PingPongSurfaceType surfaceType,
        Vector3 contactPoint,
        Vector3 contactNormal,
        Vector3 incomingVelocity,
        Vector3 outgoingVelocity,
        Vector3 outgoingAngularVelocity,
        float closingSpeed,
        bool positionCorrected)
    {
        this.ball = ball;
        this.collider = collider;
        this.surfaceType = surfaceType;
        this.contactPoint = contactPoint;
        this.contactNormal = contactNormal;
        this.incomingVelocity = incomingVelocity;
        this.outgoingVelocity = outgoingVelocity;
        this.outgoingAngularVelocity = outgoingAngularVelocity;
        this.closingSpeed = closingSpeed;
        this.positionCorrected = positionCorrected;
    }
}

public struct BallMissedInfo
{
    public GameObject ball;
    public Vector3 position;
    public PingPongMissReason reason;
    public float ageSeconds;

    public BallMissedInfo(GameObject ball, Vector3 position, PingPongMissReason reason, float ageSeconds)
    {
        this.ball = ball;
        this.position = position;
        this.reason = reason;
        this.ageSeconds = ageSeconds;
    }
}

public static class PingPongEvents
{
    public static event Action OnBallServed;
    public static event Action OnBallHit;
    public static event Action OnBallMissed;
    public static event Action OnTrainingStarted;
    public static event Action OnTrainingFinished;

    public static event Action<BallServedInfo> OnBallServedDetailed;
    public static event Action<BallHitInfo> OnBallHitDetailed;
    public static event Action<SurfaceBounceInfo> OnSurfaceBounce;
    public static event Action<BallMissedInfo> OnBallMissedDetailed;

    public static void BallServed() => BallServed(default);

    public static void BallServed(BallServedInfo info)
    {
        OnBallServedDetailed?.Invoke(info);
        OnBallServed?.Invoke();
    }

    public static void BallHit() => BallHit(default, true);

    public static void BallHit(BallHitInfo info, bool countForScore = true)
    {
        OnBallHitDetailed?.Invoke(info);
        if (countForScore)
        {
            OnBallHit?.Invoke();
        }
    }

    public static void SurfaceBounce(SurfaceBounceInfo info) => OnSurfaceBounce?.Invoke(info);

    public static void BallMissed() => BallMissed(default);

    public static void BallMissed(BallMissedInfo info)
    {
        OnBallMissedDetailed?.Invoke(info);
        OnBallMissed?.Invoke();
    }

    public static void TrainingStarted() => OnTrainingStarted?.Invoke();
    public static void TrainingFinished() => OnTrainingFinished?.Invoke();
}
