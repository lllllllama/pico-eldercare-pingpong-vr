using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ControllerBallGrabber : MonoBehaviour
{
    public Transform controllerTransform;
    public XRNode controllerNode = XRNode.LeftHand;
    public float grabRadius = 0.28f;
    public float releaseSpeedMultiplier = 1.0f;
    public float minimumReleaseSpeed = 0.35f;
    public float grabScanInterval = 0.04f;
    public float normalReleaseRegrabCooldown = 0.08f;
    public float forcedReleaseRegrabCooldown = 0.28f;
    public LayerMask grabLayers = ~0;
    public Vector3 holdOffset = new Vector3(0f, 0f, 0.08f);
    public GrabHandPoseAnimator handPoseAnimator;

    private readonly Collider[] _grabCandidates = new Collider[64];
    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private PingPongBall _grabbedBall;
    private Rigidbody _grabbedRigidbody;
    private Transform _originalParent;
    private Vector3 _lastPosition;
    private Vector3 _controllerVelocity;
    private float _nextGrabScanTime;
    private float _nextHandPoseSearchTime;
    private bool _waitForGripReleaseBeforeGrab;
    private bool _wasGripPressed;

    private void OnEnable()
    {
        _lastPosition = GetControllerPosition();
        _controllerVelocity = Vector3.zero;
        _nextGrabScanTime = 0f;
        _nextHandPoseSearchTime = 0f;
        _waitForGripReleaseBeforeGrab = false;
        _wasGripPressed = false;
        TryBindHandPoseAnimator();
    }

    private void Update()
    {
        UpdateControllerVelocity();
        TryBindHandPoseAnimator();

        var gripPressed = IsGripPressed();
        if (!gripPressed)
        {
            _waitForGripReleaseBeforeGrab = false;
        }

        if (gripPressed && !_waitForGripReleaseBeforeGrab && _grabbedBall == null && Time.time >= _nextGrabScanTime)
        {
            _nextGrabScanTime = Time.time + grabScanInterval;
            TryGrabNearestBall();
        }
        else if (!gripPressed && _wasGripPressed)
        {
            ReleaseBall();
        }

        if (_grabbedBall != null)
        {
            HoldGrabbedBall();
        }

        _wasGripPressed = gripPressed;
    }

    private void OnDisable()
    {
        ReleaseBall();
    }

    private void TryGrabNearestBall()
    {
        if (controllerTransform == null || _grabbedBall != null) return;

        PingPongBall nearest = null;
        var nearestDistance = grabRadius;
        var count = Physics.OverlapSphereNonAlloc(
            controllerTransform.position,
            grabRadius,
            _grabCandidates,
            grabLayers,
            QueryTriggerInteraction.Ignore);

        for (var i = 0; i < count; i++)
        {
            var candidate = _grabCandidates[i];
            if (candidate == null) continue;

            var ball = candidate.GetComponentInParent<PingPongBall>();
            if (ball == null || ball.IsGrabbed || !ball.CanBeGrabbed) continue;

            var distance = Vector3.Distance(controllerTransform.position, ball.transform.position);
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = ball;
            }
        }

        if (nearest == null) return;

        _grabbedBall = nearest;
        _grabbedRigidbody = nearest.GetComponent<Rigidbody>();
        _originalParent = nearest.transform.parent;

        if (_grabbedRigidbody != null)
        {
            _grabbedRigidbody.velocity = Vector3.zero;
            _grabbedRigidbody.angularVelocity = Vector3.zero;
            _grabbedRigidbody.useGravity = false;
            _grabbedRigidbody.isKinematic = true;
        }

        nearest.SetGrabber(this);
        nearest.transform.SetParent(controllerTransform, false);
        nearest.transform.localPosition = holdOffset;
        nearest.transform.localRotation = Quaternion.identity;
    }

    private void HoldGrabbedBall()
    {
        if (controllerTransform == null)
        {
            ReleaseBall();
            return;
        }

        _grabbedBall.transform.localPosition = holdOffset;
        _grabbedBall.transform.localRotation = Quaternion.identity;
    }

    private void ReleaseBall()
    {
        var releaseVelocity = _controllerVelocity * releaseSpeedMultiplier;
        if (releaseVelocity.sqrMagnitude < minimumReleaseSpeed * minimumReleaseSpeed)
        {
            releaseVelocity = GetControllerForward() * minimumReleaseSpeed;
        }

        ReleaseBall(releaseVelocity, false);
    }

    public bool ForceRelease(PingPongBall ball, Vector3 velocity)
    {
        if (_grabbedBall == null || _grabbedBall != ball) return false;

        ReleaseBall(velocity, true);
        _waitForGripReleaseBeforeGrab = true;
        return true;
    }

    public bool IsHolding(PingPongBall ball)
    {
        return _grabbedBall != null && _grabbedBall == ball;
    }

    private void ReleaseBall(Vector3 releaseVelocity, bool forcedByPaddle)
    {
        if (_grabbedBall == null) return;

        var releasedBall = _grabbedBall;
        releasedBall.transform.SetParent(_originalParent, true);
        releasedBall.SetGrabber(null);
        releasedBall.IgnoreGrabFor(forcedByPaddle ? forcedReleaseRegrabCooldown : normalReleaseRegrabCooldown);

        if (_grabbedRigidbody != null)
        {
            _grabbedRigidbody.isKinematic = false;
            _grabbedRigidbody.useGravity = true;
            _grabbedRigidbody.velocity = releaseVelocity;
            _grabbedRigidbody.angularVelocity = Vector3.zero;
        }

        _grabbedBall = null;
        _grabbedRigidbody = null;
        _originalParent = null;
    }

    private void UpdateControllerVelocity()
    {
        var currentPosition = GetControllerPosition();
        var dt = Time.deltaTime;
        _controllerVelocity = dt > Mathf.Epsilon ? (currentPosition - _lastPosition) / dt : Vector3.zero;
        _lastPosition = currentPosition;
    }

    private Vector3 GetControllerPosition()
    {
        return controllerTransform != null ? controllerTransform.position : transform.position;
    }

    private Vector3 GetControllerForward()
    {
        return controllerTransform != null ? controllerTransform.forward : transform.forward;
    }

    private bool IsGripPressed()
    {
        InputDevices.GetDevicesAtXRNode(controllerNode, _devices);
        foreach (var device in _devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.gripButton, out var gripButton) && gripButton)
            {
                return true;
            }

            if (device.TryGetFeatureValue(CommonUsages.grip, out var gripValue) && gripValue > 0.55f)
            {
                return true;
            }
        }

        return false;
    }

    private void TryBindHandPoseAnimator()
    {
        if (handPoseAnimator != null || Time.time < _nextHandPoseSearchTime) return;
        _nextHandPoseSearchTime = Time.time + 0.5f;

        var visualName = controllerNode == XRNode.LeftHand ? "Left_GrabHand" : "Right_GrabHand";
        var visual = GameObject.Find(visualName);
        if (visual == null) return;

        handPoseAnimator = visual.GetComponent<GrabHandPoseAnimator>();
        if (handPoseAnimator == null)
        {
            handPoseAnimator = visual.AddComponent<GrabHandPoseAnimator>();
        }

        handPoseAnimator.controllerNode = controllerNode;
        handPoseAnimator.readControllerGrip = true;
    }
}
