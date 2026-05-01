using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ControllerBallGrabber : MonoBehaviour
{
    public Transform controllerTransform;
    public XRNode controllerNode = XRNode.LeftHand;
    public float grabRadius = 0.28f;
    public float releaseSpeedMultiplier = 1.0f;
    public Vector3 holdOffset = new Vector3(0f, 0f, 0.08f);

    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private PingPongBall _grabbedBall;
    private Rigidbody _grabbedRigidbody;
    private Transform _originalParent;
    private Vector3 _lastPosition;
    private Vector3 _controllerVelocity;
    private bool _wasGripPressed;

    private void OnEnable()
    {
        _lastPosition = GetControllerPosition();
        _controllerVelocity = Vector3.zero;
        _wasGripPressed = false;
    }

    private void Update()
    {
        UpdateControllerVelocity();

        var gripPressed = IsGripPressed();
        if (gripPressed && _grabbedBall == null)
        {
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
        foreach (var ball in FindObjectsOfType<PingPongBall>())
        {
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
        if (_grabbedBall == null) return;

        var releasedBall = _grabbedBall;
        releasedBall.transform.SetParent(_originalParent, true);

        if (_grabbedRigidbody != null)
        {
            _grabbedRigidbody.isKinematic = false;
            _grabbedRigidbody.useGravity = true;
            _grabbedRigidbody.velocity = _controllerVelocity * releaseSpeedMultiplier;
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
}
