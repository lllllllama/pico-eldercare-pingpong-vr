using UnityEngine;

public class PaddleVelocityTracker : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }
    public float Speed => Velocity.magnitude;
    public float MaxSpeed { get; private set; }

    private Vector3 _lastPosition;

    private void OnEnable()
    {
        _lastPosition = transform.position;
        Velocity = Vector3.zero;
        MaxSpeed = 0f;
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        if (dt <= Mathf.Epsilon)
        {
            Velocity = Vector3.zero;
            return;
        }

        var currentPosition = transform.position;
        Velocity = (currentPosition - _lastPosition) / dt;
        _lastPosition = currentPosition;

        if (Speed > MaxSpeed)
        {
            MaxSpeed = Speed;
        }
    }

    public void ResetMaxSpeed() => MaxSpeed = 0f;
}
