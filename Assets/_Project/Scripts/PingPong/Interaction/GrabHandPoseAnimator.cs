using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class GrabHandPoseAnimator : MonoBehaviour
{
    public XRNode controllerNode = XRNode.LeftHand;
    public float closedPoseSpeed = 12f;
    public bool readControllerGrip = true;
    public bool mirrorX = true;

    private static readonly string[] FingerNames =
    {
        "Thumb",
        "IndexFinger",
        "MiddleFinger",
        "RingFinger",
        "LittleFinger"
    };

    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private readonly Transform[] _fingers = new Transform[FingerNames.Length];
    private readonly Vector3[] _openPositions = new Vector3[FingerNames.Length];
    private readonly Quaternion[] _openRotations = new Quaternion[FingerNames.Length];
    private readonly Vector3[] _closedPositions = new Vector3[FingerNames.Length];
    private readonly Quaternion[] _closedRotations = new Quaternion[FingerNames.Length];
    private readonly bool[] _fingerPoseCached = new bool[FingerNames.Length];

    private float _gripBlend;
    private bool _allFingersFound;
    private bool _cachedMirrorX;

    private void OnEnable()
    {
        _cachedMirrorX = mirrorX;
        RebuildPoseCache();
        ApplyPose(0f);
    }

    private void OnDisable()
    {
        ApplyPose(0f);
        _gripBlend = 0f;
    }

    private void Update()
    {
        if (_cachedMirrorX != mirrorX)
        {
            _cachedMirrorX = mirrorX;
            RebuildPoseCache();
        }

        if (!_allFingersFound)
        {
            RefreshFingerBindings();
        }

        var target = readControllerGrip ? ReadGripValue() : 0f;
        _gripBlend = Mathf.MoveTowards(_gripBlend, target, closedPoseSpeed * Time.deltaTime);
        ApplyPose(_gripBlend);
    }

    private void RefreshFingerBindings()
    {
        _allFingersFound = true;

        for (var i = 0; i < FingerNames.Length; i++)
        {
            var finger = _fingers[i] != null ? _fingers[i] : transform.Find(FingerNames[i]);
            _fingers[i] = finger;

            if (finger == null)
            {
                _allFingersFound = false;
                continue;
            }

            if (_fingerPoseCached[i]) continue;

            _openPositions[i] = finger.localPosition;
            _openRotations[i] = finger.localRotation;
            BuildClosedPose(FingerNames[i], _openPositions[i], _openRotations[i], mirrorX, out _closedPositions[i], out _closedRotations[i]);
            _fingerPoseCached[i] = true;
        }
    }

    public void RebuildPoseCache()
    {
        for (var i = 0; i < _fingerPoseCached.Length; i++)
        {
            _fingerPoseCached[i] = false;
        }

        RefreshFingerBindings();
        ApplyPose(_gripBlend);
    }

    private static void BuildClosedPose(string fingerName, Vector3 openPosition, Quaternion openRotation, bool mirrorX, out Vector3 closedPosition, out Quaternion closedRotation)
    {
        var side = Mathf.Abs(openPosition.x) > 0.001f ? Mathf.Sign(openPosition.x) : (mirrorX ? 1f : -1f);

        if (fingerName == "Thumb")
        {
            closedPosition = openPosition + new Vector3(-side * 0.055f, -0.012f, 0.04f);
            closedRotation = openRotation * Quaternion.Euler(35f, mirrorX ? -8f : 8f, mirrorX ? -58f : 58f);
            return;
        }

        closedPosition = new Vector3(openPosition.x * 0.45f, openPosition.y - 0.035f, openPosition.z - 0.065f);
        closedRotation = openRotation * Quaternion.Euler(58f, 0f, mirrorX ? -4f : 4f);
    }

    private void ApplyPose(float blend)
    {
        for (var i = 0; i < _fingers.Length; i++)
        {
            var finger = _fingers[i];
            if (finger == null || !_fingerPoseCached[i]) continue;

            var fingerBlend = FingerBlend(i, blend);
            finger.localPosition = Vector3.Lerp(_openPositions[i], _closedPositions[i], fingerBlend);
            finger.localRotation = Quaternion.Slerp(_openRotations[i], _closedRotations[i], fingerBlend);
        }
    }

    private static float FingerBlend(int fingerIndex, float blend)
    {
        var delay = fingerIndex == 0 ? 0f : 0.035f * fingerIndex;
        return Mathf.Clamp01((blend - delay) / Mathf.Max(1f - delay, 0.01f));
    }

    private float ReadGripValue()
    {
        InputDevices.GetDevicesAtXRNode(controllerNode, _devices);
        var strongestGrip = 0f;

        foreach (var device in _devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.grip, out var gripValue))
            {
                strongestGrip = Mathf.Max(strongestGrip, gripValue);
            }

            if (device.TryGetFeatureValue(CommonUsages.gripButton, out var gripButton) && gripButton)
            {
                strongestGrip = 1f;
            }
        }

        return Mathf.Clamp01(strongestGrip);
    }
}
