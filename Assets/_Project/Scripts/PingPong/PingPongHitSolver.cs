using UnityEngine;

public struct PingPongHitInput
{
    public Vector3 incomingVelocity;
    public Vector3 incomingAngularVelocity;
    public Vector3 surfaceVelocity;
    public Vector3 normal;
    public Vector3 preferredForward;
    public float normalRestitution;
    public float tangentialFriction;
    public float spinTransfer;
    public float minimumClosingSpeed;
    public float minimumSpeed;
    public float maximumSpeed;
    public float upwardBias;
    public float minimumForwardDot;
    public float forwardBlend;
    public bool requireClosing;
    public bool biasTowardPreferredForward;
}

public struct PingPongHitResult
{
    public bool accepted;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public float closingSpeed;
}

public static class PingPongHitSolver
{
    public static PingPongHitInput CreateDefault(Vector3 incomingVelocity, Vector3 angularVelocity, Vector3 normal, Vector3 surfaceVelocity)
    {
        return new PingPongHitInput
        {
            incomingVelocity = incomingVelocity,
            incomingAngularVelocity = angularVelocity,
            surfaceVelocity = surfaceVelocity,
            normal = normal,
            preferredForward = Vector3.forward,
            normalRestitution = 0.78f,
            tangentialFriction = 0.2f,
            spinTransfer = 0.35f,
            minimumClosingSpeed = 0.05f,
            minimumSpeed = 0f,
            maximumSpeed = 9f,
            upwardBias = 0f,
            minimumForwardDot = -1f,
            forwardBlend = 0f,
            requireClosing = true,
            biasTowardPreferredForward = false
        };
    }

    public static PingPongHitResult Solve(PingPongHitInput input)
    {
        var normal = input.normal;
        if (normal.sqrMagnitude < 0.0001f)
        {
            return Rejected(0f);
        }

        normal.Normalize();

        var relativeVelocity = input.incomingVelocity - input.surfaceVelocity;
        var closingSpeed = -Vector3.Dot(relativeVelocity, normal);
        if (input.requireClosing && closingSpeed < Mathf.Max(0f, input.minimumClosingSpeed))
        {
            return Rejected(closingSpeed);
        }

        var restitution = Mathf.Clamp01(input.normalRestitution);
        var friction = Mathf.Clamp01(input.tangentialFriction);
        var outgoingRelative = relativeVelocity + (1f + restitution) * Mathf.Max(0f, closingSpeed) * normal;

        var spinAtContact = Vector3.Cross(input.incomingAngularVelocity, -normal * PingPongGeometry.BallRadius);
        var normalComponent = Vector3.Dot(relativeVelocity, normal) * normal;
        var tangentSlip = relativeVelocity - normalComponent + spinAtContact;
        var tangentImpulse = -tangentSlip * friction;
        outgoingRelative += tangentImpulse;

        var angularVelocity = input.incomingAngularVelocity;
        if (input.spinTransfer > 0f)
        {
            angularVelocity += Vector3.Cross(normal, tangentImpulse) *
                               (Mathf.Clamp01(input.spinTransfer) / Mathf.Max(PingPongGeometry.BallRadius, 0.001f));
        }

        var velocity = outgoingRelative + input.surfaceVelocity;
        if (input.upwardBias != 0f)
        {
            velocity += Vector3.up * input.upwardBias;
        }

        if (input.biasTowardPreferredForward && input.preferredForward.sqrMagnitude > 0.0001f && velocity.sqrMagnitude > 0.0001f)
        {
            velocity = BiasVelocityDirection(velocity, input.preferredForward.normalized, input.minimumForwardDot, input.forwardBlend);
        }

        velocity = ClampSpeed(velocity, input.minimumSpeed, input.maximumSpeed);

        return new PingPongHitResult
        {
            accepted = true,
            velocity = velocity,
            angularVelocity = angularVelocity,
            closingSpeed = closingSpeed
        };
    }

    public static Vector3 ApplyPaddleContactPlacement(Vector3 velocity, Vector3 localHit, float lateralScale, float liftScale)
    {
        if (velocity.sqrMagnitude < 0.0001f) return velocity;

        var speed = velocity.magnitude;
        var direction = velocity / speed;
        var lateral = Mathf.Clamp(-localHit.x * lateralScale, -0.45f, 0.45f);
        var lift = Mathf.Clamp(localHit.z * liftScale, -0.18f, 0.28f);
        var adjusted = direction + Vector3.right * lateral + Vector3.up * lift;
        if (adjusted.sqrMagnitude < 0.0001f) return velocity;

        return adjusted.normalized * speed;
    }

    private static Vector3 BiasVelocityDirection(Vector3 velocity, Vector3 preferredForward, float minimumForwardDot, float blend)
    {
        var speed = velocity.magnitude;
        var direction = velocity / speed;
        var dot = Vector3.Dot(direction, preferredForward);
        if (dot >= minimumForwardDot) return velocity;

        var biased = Vector3.Slerp(direction, preferredForward, Mathf.Clamp01(blend));
        if (biased.sqrMagnitude < 0.0001f) return velocity;
        return biased.normalized * speed;
    }

    private static Vector3 ClampSpeed(Vector3 velocity, float minimumSpeed, float maximumSpeed)
    {
        var speed = velocity.magnitude;
        if (speed < 0.0001f)
        {
            return velocity;
        }

        if (maximumSpeed > 0f && speed > maximumSpeed)
        {
            return velocity * (maximumSpeed / speed);
        }

        if (minimumSpeed > 0f && speed < minimumSpeed)
        {
            return velocity * (minimumSpeed / speed);
        }

        return velocity;
    }

    private static PingPongHitResult Rejected(float closingSpeed)
    {
        return new PingPongHitResult
        {
            accepted = false,
            velocity = Vector3.zero,
            angularVelocity = Vector3.zero,
            closingSpeed = closingSpeed
        };
    }
}
