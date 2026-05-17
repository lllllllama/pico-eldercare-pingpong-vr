using UnityEngine;

public class ComfortWorldSpaceUIPlacer : MonoBehaviour
{
    public Transform headTransform;
    public Transform uiRoot;
    public float distanceMeters = 2f;
    public float hmdHeightOffsetMeters = -0.1f;
    public bool placeOnStart = true;
    public bool placeOnEnable = false;
    public bool comfortFollowEnabled;
    public float followYawThresholdDegrees = 35f;
    public float followPositionThresholdMeters = 0.8f;
    public float followSmoothTime = 0.35f;
    public float followRotationSlerpSpeed = 4f;
    public float maxFollowSpeedMetersPerSecond = 1.25f;

    private Vector3 _followVelocity;
    private Vector3 _followTargetPosition;
    private Quaternion _followTargetRotation;
    private bool _hasFollowTarget;
    private bool _started;

    private Transform TargetRoot => uiRoot != null ? uiRoot : transform;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        _hasFollowTarget = false;

        if (_started && placeOnEnable)
        {
            PlaceInFrontOfUser();
        }
    }

    private void Start()
    {
        _started = true;

        if (placeOnStart)
        {
            PlaceInFrontOfUser();
        }
    }

    private void LateUpdate()
    {
        if (comfortFollowEnabled)
        {
            UpdateComfortFollow();
        }
    }

    public void PlaceInFrontOfUser()
    {
        if (!TryGetComfortPose(out var position, out var rotation)) return;

        var target = TargetRoot;
        target.position = position;
        target.rotation = rotation;
        _followTargetPosition = position;
        _followTargetRotation = rotation;
        _followVelocity = Vector3.zero;
        _hasFollowTarget = false;
    }

    public void ResetUiPosition()
    {
        PlaceInFrontOfUser();
    }

    public void PlaceOnOpen()
    {
        PlaceInFrontOfUser();
    }

    private void UpdateComfortFollow()
    {
        var target = TargetRoot;
        if (!TryGetComfortPose(out var desiredPosition, out var desiredRotation)) return;

        if (!_hasFollowTarget && ShouldRefreshComfortTarget(target, desiredPosition))
        {
            _followTargetPosition = desiredPosition;
            _followTargetRotation = desiredRotation;
            _hasFollowTarget = true;
        }

        if (!_hasFollowTarget) return;

        target.position = Vector3.SmoothDamp(
            target.position,
            _followTargetPosition,
            ref _followVelocity,
            Mathf.Max(0.01f, followSmoothTime),
            Mathf.Max(0.01f, maxFollowSpeedMetersPerSecond));

        target.rotation = Quaternion.Slerp(
            target.rotation,
            _followTargetRotation,
            Mathf.Max(0.01f, followRotationSlerpSpeed) * Time.deltaTime);

        if (Vector3.Distance(target.position, _followTargetPosition) < 0.02f &&
            Quaternion.Angle(target.rotation, _followTargetRotation) < 1f)
        {
            _hasFollowTarget = false;
            _followVelocity = Vector3.zero;
        }
    }

    private bool ShouldRefreshComfortTarget(Transform target, Vector3 desiredPosition)
    {
        if (headTransform == null || target == null) return false;

        var desiredDirection = GetHeadYawForward();
        var currentDirection = Vector3.ProjectOnPlane(target.position - headTransform.position, Vector3.up);
        if (currentDirection.sqrMagnitude < 0.0001f)
        {
            currentDirection = desiredDirection;
        }

        currentDirection.Normalize();
        var yawDelta = Vector3.Angle(currentDirection, desiredDirection);
        var positionDelta = Vector3.Distance(target.position, desiredPosition);
        return yawDelta > Mathf.Max(0f, followYawThresholdDegrees) ||
               positionDelta > Mathf.Max(0f, followPositionThresholdMeters);
    }

    private bool TryGetComfortPose(out Vector3 position, out Quaternion rotation)
    {
        ResolveReferences();
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (headTransform == null)
        {
            return false;
        }

        var forward = GetHeadYawForward();
        position = headTransform.position + forward * Mathf.Max(0.01f, distanceMeters);
        position.y = headTransform.position.y + hmdHeightOffsetMeters;
        rotation = Quaternion.LookRotation(forward, Vector3.up);
        return true;
    }

    private Vector3 GetHeadYawForward()
    {
        var forward = Vector3.forward;
        if (headTransform != null)
        {
            forward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = TargetRoot != null
                ? Vector3.ProjectOnPlane(TargetRoot.forward, Vector3.up)
                : Vector3.forward;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return forward.normalized;
    }

    private void ResolveReferences()
    {
        if (uiRoot == null)
        {
            uiRoot = transform;
        }

        if (headTransform != null) return;

        var camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>(true);
        if (camera != null)
        {
            headTransform = camera.transform;
        }
    }
}
