using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-40)]
public class RemoteTableDragController : MonoBehaviour
{
    public bool enableRemoteDrag = true;
    public Transform tableRoot;
    public TableDragHandle tableDragHandle;
    public Transform controllerTransform;
    public XRNode controllerNode = XRNode.LeftHand;
    public Transform hmdTransform;
    public ControllerBallGrabber ballGrabber;
    public SimpleGripInteractionState interactionState;
    public PingPongOpenSpaceTablePlacer openSpaceTablePlacer;
    public BallSpawner[] ballSpawners;
    public LayerMask tableRaycastLayers = ~0;

    public float remoteGrabMaxDistanceMeters = 8f;
    public float positionSensitivity = 0.45f;
    public float maxMoveSpeed = 0.65f;
    public float positionSmoothing = 0.08f;
    public float dragDeadZone = 0.01f;
    public float minDistanceFromUser = 0.7f;
    public float maxDistanceFromUser = 3.0f;
    public bool controlServing = true;
    public bool clearBallsWhenDragging = true;
    public bool resumeServingOnRelease = true;

    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private readonly RaycastHit[] _raycastHits = new RaycastHit[24];
    private Vector3 _dragControllerStartPosition;
    private Vector3 _dragTableStartPosition;
    private Vector3 _dragSmoothVelocity;
    private float _lockedTableY;
    private bool _dragging;
    private bool _wasGripPressed;
    private bool _servingSuppressed;

    public bool IsDragging
    {
        get { return _dragging; }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        _dragging = false;
        _wasGripPressed = false;
        _dragSmoothVelocity = Vector3.zero;
    }

    private void OnDisable()
    {
        if (_dragging)
        {
            EndRemoteDrag();
        }

        _wasGripPressed = false;
    }

    private void Update()
    {
        ResolveReferences();
        if (!enableRemoteDrag || tableRoot == null || controllerTransform == null) return;

        var gripPressed = IsGripPressed();
        if (!gripPressed)
        {
            if (_dragging)
            {
                EndRemoteDrag();
            }

            _wasGripPressed = false;
            return;
        }

        if (!_dragging && !_wasGripPressed)
        {
            TryBeginFromGripPress();
        }

        if (_dragging)
        {
            DragTableFromControllerDelta();
        }

        _wasGripPressed = gripPressed;
    }

    private void TryBeginFromGripPress()
    {
        var currentMode = SimpleGripInteractionState.CurrentMode;
        if (currentMode != SimpleGripInteractionMode.None)
        {
            SimpleGripInteractionState.LogGripIgnored(currentMode);
            return;
        }

        if (ballGrabber != null && ballGrabber.CanGrabBall())
        {
            return;
        }

        if (!TryRaycastTable(out _))
        {
            return;
        }

        if (SimpleGripInteractionState.TryBegin(interactionState, SimpleGripInteractionMode.RemoteTableDrag))
        {
            BeginRemoteDrag();
        }
    }

    private void BeginRemoteDrag()
    {
        _dragging = true;
        if (openSpaceTablePlacer != null)
        {
            openSpaceTablePlacer.NotifyManualRemoteDragStarted();
        }

        _dragControllerStartPosition = controllerTransform.position;
        _dragTableStartPosition = tableRoot.position;
        _lockedTableY = tableRoot.position.y;
        _dragSmoothVelocity = Vector3.zero;

        if (controlServing)
        {
            StopServing(clearBallsWhenDragging);
        }
    }

    private void EndRemoteDrag()
    {
        _dragging = false;
        _dragSmoothVelocity = Vector3.zero;
        SyncHeightDependentValues();
        LockTableUprightYawOnly();
        SimpleGripInteractionState.End(interactionState, SimpleGripInteractionMode.RemoteTableDrag);

        if (resumeServingOnRelease)
        {
            StartServing();
        }
    }

    private void DragTableFromControllerDelta()
    {
        var controllerDelta = Vector3.ProjectOnPlane(controllerTransform.position - _dragControllerStartPosition, Vector3.up);
        if (controllerDelta.magnitude < Mathf.Max(0f, dragDeadZone))
        {
            controllerDelta = Vector3.zero;
        }

        var targetPosition = _dragTableStartPosition + controllerDelta * Mathf.Max(0f, positionSensitivity);
        targetPosition.y = _lockedTableY;
        targetPosition = ConstrainTableDistanceFromUser(targetPosition);

        var smoothTime = Mathf.Max(0.001f, positionSmoothing);
        var maxSpeed = Mathf.Max(0.01f, maxMoveSpeed);
        var nextPosition = Vector3.SmoothDamp(tableRoot.position, targetPosition, ref _dragSmoothVelocity, smoothTime, maxSpeed, Time.deltaTime);
        nextPosition.y = _lockedTableY;

        MoveTable(nextPosition);
        LockTableUprightYawOnly();
    }

    private void MoveTable(Vector3 nextPosition)
    {
        if (tableDragHandle != null)
        {
            tableDragHandle.SetTablePosition(nextPosition);
            return;
        }

        tableRoot.position = nextPosition;
        AcceptTableTransform();
    }

    private Vector3 ConstrainTableDistanceFromUser(Vector3 targetPosition)
    {
        if (hmdTransform == null) return targetPosition;

        var headPosition = hmdTransform.position;
        var toTable = Vector3.ProjectOnPlane(targetPosition - headPosition, Vector3.up);
        if (toTable.sqrMagnitude < 0.0001f)
        {
            toTable = Vector3.ProjectOnPlane(hmdTransform.forward, Vector3.up);
        }

        var headForward = Vector3.ProjectOnPlane(hmdTransform.forward, Vector3.up);
        if (headForward.sqrMagnitude > 0.0001f)
        {
            headForward.Normalize();
            if (toTable.sqrMagnitude < 0.0001f || Vector3.Dot(toTable.normalized, headForward) < 0.1f)
            {
                toTable = headForward * Mathf.Max(toTable.magnitude, Mathf.Max(0f, minDistanceFromUser));
            }
        }

        if (toTable.sqrMagnitude < 0.0001f)
        {
            toTable = Vector3.forward;
        }

        var distance = Mathf.Clamp(
            toTable.magnitude,
            Mathf.Max(0f, minDistanceFromUser),
            Mathf.Max(Mathf.Max(0f, minDistanceFromUser), maxDistanceFromUser));
        var constrained = headPosition + toTable.normalized * distance;
        constrained.y = targetPosition.y;
        return constrained;
    }

    private bool TryRaycastTable(out RaycastHit selectedHit)
    {
        selectedHit = default;
        var ray = new Ray(controllerTransform.position, controllerTransform.forward);
        var count = Physics.RaycastNonAlloc(
            ray,
            _raycastHits,
            Mathf.Max(0.1f, remoteGrabMaxDistanceMeters),
            tableRaycastLayers,
            QueryTriggerInteraction.Collide);

        if (count <= 0) return false;

        var bestDistance = float.MaxValue;
        var found = false;
        for (var i = 0; i < count; i++)
        {
            var hit = _raycastHits[i];
            if (hit.collider == null || !IsTableHit(hit.collider.transform)) continue;
            if (hit.distance >= bestDistance) continue;

            bestDistance = hit.distance;
            selectedHit = hit;
            found = true;
        }

        return found;
    }

    private bool IsTableHit(Transform hitTransform)
    {
        if (hitTransform == null || tableRoot == null) return false;
        if (hitTransform == tableRoot || hitTransform.IsChildOf(tableRoot)) return true;

        var surface = hitTransform.GetComponentInParent<PingPongSurface>();
        return surface != null &&
               (surface.surfaceType == PingPongSurfaceType.Table || surface.surfaceType == PingPongSurfaceType.Net);
    }

    private void SyncHeightDependentValues()
    {
        if (tableDragHandle != null)
        {
            tableDragHandle.SyncHeightDependentValues();
        }
    }

    private void LockTableUprightYawOnly()
    {
        if (tableRoot == null) return;

        var euler = tableRoot.eulerAngles;
        tableRoot.rotation = Quaternion.Euler(0f, euler.y, 0f);
    }

    private void AcceptTableTransform()
    {
        if (tableRoot == null) return;

        var motionLock = tableRoot.GetComponent<TablePassiveMotionLock>();
        if (motionLock != null)
        {
            motionLock.AcceptCurrentTransform();
        }
    }

    private void StopServing(bool clearBalls)
    {
        var spawners = ResolveBallSpawners();
        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;
            spawner.StopServing();
            if (clearBalls)
            {
                spawner.ClearBalls();
            }
        }

        _servingSuppressed = true;
    }

    private void StartServing()
    {
        if (!controlServing || !_servingSuppressed) return;

        var spawners = ResolveBallSpawners();
        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;
            spawner.StartServing();
        }

        _servingSuppressed = false;
    }

    private BallSpawner[] ResolveBallSpawners()
    {
        if (ballSpawners != null && ballSpawners.Length > 0)
        {
            return ballSpawners;
        }

        if (tableDragHandle != null && tableDragHandle.syncedSpawners != null && tableDragHandle.syncedSpawners.Length > 0)
        {
            ballSpawners = tableDragHandle.syncedSpawners;
            return ballSpawners;
        }

        ballSpawners = FindObjectsOfType<BallSpawner>(true);
        return ballSpawners;
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

    private void ResolveReferences()
    {
        if (tableRoot == null)
        {
            var table = GameObject.Find("Table");
            if (table != null)
            {
                tableRoot = table.transform;
            }
        }

        if (tableDragHandle == null)
        {
            tableDragHandle = FindObjectOfType<TableDragHandle>(true);
        }

        if (controllerTransform == null && tableDragHandle != null)
        {
            controllerTransform = tableDragHandle.controllerTransform;
        }

        if (ballGrabber == null && tableDragHandle != null)
        {
            ballGrabber = tableDragHandle.ballGrabber;
        }

        if (ballGrabber == null)
        {
            ballGrabber = FindObjectOfType<ControllerBallGrabber>(true);
        }

        if (interactionState == null)
        {
            interactionState = SimpleGripInteractionState.EnsureInstance();
        }

        if (openSpaceTablePlacer == null)
        {
            openSpaceTablePlacer = GetComponent<PingPongOpenSpaceTablePlacer>();
        }

        if (hmdTransform == null)
        {
            var camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>(true);
            if (camera != null)
            {
                hmdTransform = camera.transform;
            }
        }
    }
}
