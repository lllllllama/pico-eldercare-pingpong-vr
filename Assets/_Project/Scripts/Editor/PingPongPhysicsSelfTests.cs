using UnityEditor;
using UnityEngine;

public static class PingPongPhysicsSelfTests
{
    [MenuItem("Tools/PICO ElderCare/Run PingPong Physics Self Tests")]
    public static void RunAll()
    {
        HeldServeHitUsesPaddleVelocity();
        SideSwipeDoesNotLaunchHeldBall();
        TableBounceReflectsUpward();
        SolverClampsMaximumSpeed();
        ContactPlacementChangesLateralDirection();
        ServeProfilesCreateOppositeSpin();
        AerodynamicsDragAndTopspinAreDirectional();
        RigidbodySpinLimitCoversServeSpin();
        Debug.Log("PingPong physics self tests passed.");
    }

    private static void HeldServeHitUsesPaddleVelocity()
    {
        var input = PingPongHitSolver.CreateDefault(Vector3.zero, Vector3.zero, Vector3.forward, Vector3.forward * 2.4f);
        input.minimumClosingSpeed = 0.15f;
        input.minimumSpeed = 3.2f;
        input.maximumSpeed = 9f;
        input.biasTowardPreferredForward = true;
        input.minimumForwardDot = 0.38f;
        input.forwardBlend = 0.82f;

        var result = PingPongHitSolver.Solve(input);
        AssertTrue(result.accepted, "Held serve hit should be accepted when paddle moves into its face normal.");
        AssertTrue(result.velocity.z > 3f, "Held serve hit should launch toward the far side.");
    }

    private static void SideSwipeDoesNotLaunchHeldBall()
    {
        var input = PingPongHitSolver.CreateDefault(Vector3.zero, Vector3.zero, Vector3.forward, Vector3.right * 4f);
        input.minimumClosingSpeed = 0.15f;
        input.minimumSpeed = 3.2f;

        var result = PingPongHitSolver.Solve(input);
        AssertTrue(!result.accepted, "Side swipe should not launch a held ball when there is no closing speed.");
    }

    private static void TableBounceReflectsUpward()
    {
        var input = PingPongHitSolver.CreateDefault(Vector3.down * 3f, Vector3.zero, Vector3.up, Vector3.zero);
        input.normalRestitution = 0.86f;
        input.tangentialFriction = 0.08f;
        input.maximumSpeed = 9f;

        var result = PingPongHitSolver.Solve(input);
        AssertTrue(result.accepted, "Table bounce should be accepted for downward velocity.");
        AssertTrue(result.velocity.y > 2.4f, "Table bounce should reflect upward with restitution.");
    }

    private static void SolverClampsMaximumSpeed()
    {
        var input = PingPongHitSolver.CreateDefault(Vector3.back * 12f, Vector3.zero, Vector3.forward, Vector3.forward * 8f);
        input.maximumSpeed = 9f;

        var result = PingPongHitSolver.Solve(input);
        AssertTrue(result.accepted, "Fast paddle hit should be accepted.");
        AssertTrue(result.velocity.magnitude <= 9.001f, "Solver should clamp maximum speed.");
    }

    private static void ContactPlacementChangesLateralDirection()
    {
        var velocity = PingPongHitSolver.ApplyPaddleContactPlacement(Vector3.forward * 4f, new Vector3(0.12f, 0f, 0f), 1.15f, 0.35f);
        AssertTrue(velocity.x < -0.01f, "Right-side contact should add leftward lateral direction.");
        AssertTrue(Mathf.Abs(velocity.magnitude - 4f) < 0.001f, "Contact placement should preserve speed.");
    }

    private static void ServeProfilesCreateOppositeSpin()
    {
        var launchVelocity = Vector3.back * 3f;
        var topspin = BallSpawner.CalculateProfileSpin(PingPongServeProfile.Topspin, launchVelocity, 95f, 80f, 50f);
        var backspin = BallSpawner.CalculateProfileSpin(PingPongServeProfile.Backspin, launchVelocity, 95f, 80f, 50f);

        AssertTrue(topspin.sqrMagnitude > 1f, "Topspin serve should create angular velocity.");
        AssertTrue(backspin.sqrMagnitude > 1f, "Backspin serve should create angular velocity.");
        AssertTrue(Vector3.Dot(topspin.normalized, backspin.normalized) < -0.99f, "Topspin and backspin should use opposite spin axes.");
    }

    private static void AerodynamicsDragAndTopspinAreDirectional()
    {
        var velocity = Vector3.back * 6f;
        var topspin = BallSpawner.CalculateProfileSpin(PingPongServeProfile.Topspin, velocity, 95f, 80f, 50f);
        var acceleration = PingPongBall.CalculateAerodynamicAcceleration(
            velocity,
            topspin,
            PingPongGeometry.BallRadius,
            PingPongGeometry.BallMass,
            1.27f,
            0.5f,
            0.28f,
            45f);

        AssertTrue(Vector3.Dot(acceleration, velocity) < 0f, "Aerodynamic drag should oppose ball velocity.");
        AssertTrue(acceleration.y < 0f, "Topspin moving toward the player should add downward Magnus acceleration.");
    }

    private static void RigidbodySpinLimitCoversServeSpin()
    {
        var ballObject = new GameObject("SpinLimitTestBall");
        try
        {
            var rb = ballObject.AddComponent<Rigidbody>();
            rb.maxAngularVelocity = 7f;
            PingPongBall.ConfigureSpinLimit(rb, 140f);
            AssertTrue(rb.maxAngularVelocity >= PingPongBall.DefaultMaxAngularVelocity, "Spin limit should cover the 180 rad/s ball spin clamp.");

            rb.maxAngularVelocity = 7f;
            PingPongBall.ConfigureSpinLimit(rb, 240f);
            AssertTrue(rb.maxAngularVelocity >= 240f, "Spin limit should cover configured serve spin values above the default clamp.");
        }
        finally
        {
            Object.DestroyImmediate(ballObject);
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
