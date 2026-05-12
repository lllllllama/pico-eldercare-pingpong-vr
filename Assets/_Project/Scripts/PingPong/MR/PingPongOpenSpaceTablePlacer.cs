using System.Collections.Generic;
using PicoElderCare.Rehab;
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-45)]
public class PingPongOpenSpaceTablePlacer : MonoBehaviour
{
    public Transform tableRoot;
    public TableDragHandle tableDragHandle;
    public Transform hmdTransform;
    public Transform remoteDragControllerTransform;
    public ControllerBallGrabber ballGrabber;
    public RemoteTableDragController remoteTableDragController;
    public SimpleGripInteractionState interactionState;
    public Transform roomSensingRoot;
    public BallSpawner[] ballSpawners;
    public XRNode remoteDragControllerNode = XRNode.LeftHand;

    public bool autoPlaceOnStart = true;
    public bool clearSavedPlacementOnStart = true;
    public bool controlServing = true;
    public bool clearBallsWhenTableMoves = true;
    public bool startServingAfterClearPlacement = true;
    public bool startServingAfterManualPlacement = true;
    public bool startServingAfterConfirmedPlacementOnly = true;
    public bool requireRoomSensingColliderForAutoPlacement = true;
    public int minimumRoomSensingColliderCount = 1;
    public float desiredDistanceMeters = 2.05f;
    public float minDistanceMeters = 1.35f;
    public float maxDistanceMeters = 3.8f;
    public float clearanceRadiusMeters = 1.65f;
    public float clearanceHeightMeters = 1.15f;
    public float fallbackFloorY = 0f;
    public float tableCenterHeightAboveFloor = PingPongGeometry.TableTopHeight - PingPongGeometry.TableThickness * 0.5f;
    public float searchDurationSeconds = 8f;
    public float searchIntervalSeconds = 0.5f;
    public LayerMask obstacleMask = ~0;

    public bool enableRemoteDrag = true;
    public float remoteGrabSelectableRadiusMeters = 2.35f;
    public float remoteGrabMaxDistanceMeters = 8f;
    public float remoteDragActivationRadiusMeters = 1.85f;
    public float remoteDragMaxRayDistanceMeters = 6f;
    public float positionSensitivity = 0.25f;
    public float rotationSensitivity = 0.35f;
    public float maxMoveSpeedMetersPerSecond = 0.35f;
    public float positionSmoothingSeconds = 0.12f;
    public float dragDeadZoneMeters = 0.01f;
    public float minUserTableDistanceMeters = 0.5f;
    public float maxUserTableDistanceMeters = 3f;

    private bool _searching;
    private bool _servingSuppressed;
    private float _searchUntilTime;
    private float _nextSearchTime;
    private float _currentFloorY;

    public bool IsDragging
    {
        get { return remoteTableDragController != null && remoteTableDragController.IsDragging; }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        PrepareRoomSensingTemplates();
        EnsureRoomSensingVisibilityGuard();
        EnsureBackgroundVisualSuppressor();
        EnsureRemoteTableDragController();
        _currentFloorY = fallbackFloorY;

        if (clearSavedPlacementOnStart && tableDragHandle != null)
        {
            tableDragHandle.ClearSavedPlacement();
        }

        if (controlServing)
        {
            StopServing(true);
        }

        if (autoPlaceOnStart)
        {
            BeginSearch();
        }
    }

    private void Update()
    {
        ResolveReferences();
        EnsureRemoteTableDragController();

        if (!_searching || IsDragging) return;
        if (Time.time < _nextSearchTime) return;

        if (!HasRequiredRoomSensingColliders())
        {
            _nextSearchTime = Time.time + Mathf.Max(0.1f, searchIntervalSeconds);
            _searchUntilTime = Time.time + Mathf.Max(1f, searchDurationSeconds);
            return;
        }

        if (Time.time > _searchUntilTime) return;

        TryAutoPlace();
        _nextSearchTime = Time.time + Mathf.Max(0.1f, searchIntervalSeconds);
    }

    public void BeginSearch()
    {
        _searching = true;
        _searchUntilTime = Time.time + Mathf.Max(0f, searchDurationSeconds);
        _nextSearchTime = 0f;

        if (controlServing)
        {
            StopServing(true);
        }
    }

    public void SetTableCenterOnFloor(Vector3 floorCenter, bool manualPlacement)
    {
        ResolveReferences();
        _currentFloorY = floorCenter.y;

        var targetPosition = floorCenter + Vector3.up * tableCenterHeightAboveFloor;
        MoveTable(targetPosition, clearBallsWhenTableMoves);

        if (manualPlacement)
        {
            _searching = false;
        }
    }

    public void NotifyManualRemoteDragStarted()
    {
        _searching = false;
    }

    private void TryAutoPlace()
    {
        if (tableRoot == null || hmdTransform == null) return;
        if (!HasRequiredRoomSensingColliders()) return;

        var result = OpenSpacePlacementSolver.FindBestPlacement(
            hmdTransform.position,
            hmdTransform.rotation,
            fallbackFloorY,
            desiredDistanceMeters,
            minDistanceMeters,
            maxDistanceMeters,
            clearanceRadiusMeters,
            clearanceHeightMeters,
            obstacleMask,
            BuildIgnoredRoots());

        if (!result.foundClearSpace) return;

        _currentFloorY = result.floorY;
        var targetPosition = new Vector3(result.center.x, result.floorY + tableCenterHeightAboveFloor, result.center.z);
        MoveTable(targetPosition, true);
        _searching = false;

        if (startServingAfterClearPlacement)
        {
            StartServing();
        }
    }

    private void MoveTable(Vector3 targetPosition, bool clearBalls)
    {
        ResolveReferences();
        if (tableRoot == null) return;

        if (tableDragHandle != null)
        {
            tableDragHandle.SetTablePosition(targetPosition);
            tableDragHandle.SyncHeightDependentValues();
        }
        else
        {
            tableRoot.position = targetPosition;
        }

        if (clearBalls && controlServing)
        {
            StopServing(true);
        }
    }

    public bool HasRequiredRoomSensingColliders()
    {
        if (!requireRoomSensingColliderForAutoPlacement) return true;
        ResolveRoomSensingRoot();
        if (roomSensingRoot == null) return false;
        return CountRoomSensingColliders() >= Mathf.Max(1, minimumRoomSensingColliderCount);
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

    private int CountRoomSensingColliders()
    {
        if (roomSensingRoot == null) return 0;

        var count = 0;
        var colliders = roomSensingRoot.GetComponentsInChildren<Collider>(false);
        foreach (var collider in colliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) continue;
            if (!collider.gameObject.activeInHierarchy) continue;
            if (collider is MeshCollider meshCollider && meshCollider.sharedMesh == null) continue;
            count++;
        }

        return count;
    }

    private Transform[] BuildIgnoredRoots()
    {
        var ignored = new List<Transform>();
        AddIgnoredRoot(ignored, tableRoot);
        if (tableDragHandle != null)
        {
            AddIgnoredRoot(ignored, tableDragHandle.transform);
            if (tableDragHandle.syncedTransforms != null)
            {
                foreach (var syncedTransform in tableDragHandle.syncedTransforms)
                {
                    AddIgnoredRoot(ignored, syncedTransform);
                }
            }
        }

        return ignored.ToArray();
    }

    private static void AddIgnoredRoot(List<Transform> ignored, Transform root)
    {
        if (root == null || ignored.Contains(root)) return;
        ignored.Add(root);
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

        if (ballGrabber == null && tableDragHandle != null)
        {
            ballGrabber = tableDragHandle.ballGrabber;
        }

        if (ballGrabber == null)
        {
            ballGrabber = FindObjectOfType<ControllerBallGrabber>(true);
        }

        if (hmdTransform == null)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = FindObjectOfType<Camera>(true);
            }

            if (camera != null)
            {
                hmdTransform = camera.transform;
            }
        }

        if (remoteDragControllerTransform == null && tableDragHandle != null)
        {
            remoteDragControllerTransform = tableDragHandle.controllerTransform;
        }

        if (interactionState == null)
        {
            interactionState = SimpleGripInteractionState.EnsureInstance();
        }

        ResolveRoomSensingRoot();
    }

    private void ResolveRoomSensingRoot()
    {
        if (roomSensingRoot != null) return;

        var root = FindObjectByNameIncludingInactive("MRSpaceSensing");
        if (root != null)
        {
            roomSensingRoot = root.transform;
        }
    }

    private void EnsureRoomSensingVisibilityGuard()
    {
        ResolveRoomSensingRoot();
        var host = roomSensingRoot != null ? roomSensingRoot.gameObject : gameObject;
        var guard = host.GetComponent<PingPongRoomSensingVisibilityGuard>();
        if (guard == null)
        {
            guard = host.AddComponent<PingPongRoomSensingVisibilityGuard>();
        }

        guard.roomSensingRoot = roomSensingRoot;
        guard.hideAllRenderersUnderRoot = true;
        guard.addMissingMeshColliders = true;
    }

    private void EnsureBackgroundVisualSuppressor()
    {
        var suppressor = FindObjectOfType<MrBackgroundVisualSuppressor>(true);
        if (suppressor == null)
        {
            suppressor = gameObject.AddComponent<MrBackgroundVisualSuppressor>();
        }

        suppressor.hideAllEnvironmentRenderers = true;
        suppressor.hideAllRoomSensingRenderers = true;
        suppressor.HideBackgroundVisuals();
    }

    private void EnsureRemoteTableDragController()
    {
        if (!enableRemoteDrag) return;

        if (remoteTableDragController == null)
        {
            remoteTableDragController = GetComponent<RemoteTableDragController>();
            if (remoteTableDragController == null)
            {
                remoteTableDragController = gameObject.AddComponent<RemoteTableDragController>();
            }
        }

        if (interactionState == null)
        {
            interactionState = SimpleGripInteractionState.EnsureInstance();
        }

        remoteTableDragController.enableRemoteDrag = true;
        remoteTableDragController.tableRoot = tableRoot;
        remoteTableDragController.tableDragHandle = tableDragHandle;
        remoteTableDragController.controllerTransform = remoteDragControllerTransform;
        remoteTableDragController.controllerNode = remoteDragControllerNode;
        remoteTableDragController.hmdTransform = hmdTransform;
        remoteTableDragController.ballGrabber = ballGrabber;
        remoteTableDragController.interactionState = interactionState;
        remoteTableDragController.openSpaceTablePlacer = this;
        remoteTableDragController.ballSpawners = ResolveBallSpawners();
        remoteTableDragController.remoteGrabMaxDistanceMeters = remoteGrabMaxDistanceMeters;
        remoteTableDragController.positionSensitivity = positionSensitivity;
        remoteTableDragController.maxMoveSpeed = maxMoveSpeedMetersPerSecond;
        remoteTableDragController.positionSmoothing = positionSmoothingSeconds;
        remoteTableDragController.dragDeadZone = dragDeadZoneMeters;
        remoteTableDragController.minDistanceFromUser = 0.7f;
        remoteTableDragController.maxDistanceFromUser = maxUserTableDistanceMeters;
        remoteTableDragController.controlServing = controlServing;
        remoteTableDragController.clearBallsWhenDragging = clearBallsWhenTableMoves;
        remoteTableDragController.resumeServingOnRelease = startServingAfterManualPlacement;
    }

    private static void PrepareRoomSensingTemplates()
    {
        foreach (var transform in FindObjectsOfType<Transform>(true))
        {
            if (transform == null) continue;
            if (!IsRoomSensingTemplate(transform.name)) continue;

            var renderer = transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            if (transform.GetComponent<MeshFilter>() != null && transform.GetComponent<MeshCollider>() == null)
            {
                transform.gameObject.AddComponent<MeshCollider>();
            }
        }
    }

    private static bool IsRoomSensingTemplate(string objectName)
    {
        return objectName == "MRDetectedPlaneTemplate" ||
               objectName == "MRSpatialMeshTemplate" ||
               objectName.StartsWith("MRDetectedPlaneTemplate", System.StringComparison.Ordinal) ||
               objectName.StartsWith("MRSpatialMeshTemplate", System.StringComparison.Ordinal);
    }

    private static GameObject FindObjectByNameIncludingInactive(string objectName)
    {
        foreach (var transform in FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }
}
