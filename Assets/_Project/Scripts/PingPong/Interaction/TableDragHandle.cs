using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-50)]
public class TableDragHandle : MonoBehaviour
{
    private const string SavedFlagSuffix = ".Saved";
    private const string PositionXSuffix = ".Position.X";
    private const string PositionYSuffix = ".Position.Y";
    private const string PositionZSuffix = ".Position.Z";

    public Transform tableRoot;
    public Transform controllerTransform;
    public XRNode controllerNode = XRNode.LeftHand;
    public ControllerBallGrabber ballGrabber;
    public Transform[] syncedTransforms;
    public BallSpawner[] syncedSpawners;
    public ControllerTableCollisionLimiter[] syncedControllerLimiters;
    public float activationRadius = 0.18f;
    public float tableBounceLocalZ = -0.55f;
    public float minimumNetClearanceAboveNet = 0.08f;
    public bool lockTableHeight = true;
    public bool constrainToBounds = true;
    public Vector2 xBounds = new Vector2(-1.25f, 1.25f);
    public Vector2 zBounds = new Vector2(0.75f, 3.35f);
    public bool loadSavedPlacementOnEnable;
    public bool savePlacementOnRelease;
    public string placementSaveKey = "PingPong.MixedReality.Table";

    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private Vector3 _lastControllerPosition;
    private float _lockedTableY;
    private bool _dragging;
    private bool _wasGripPressed;
    private bool _loadedSavedPlacement;

    public bool IsDragging => _dragging;
    public bool HasSavedPlacement => PlayerPrefs.GetInt(placementSaveKey + SavedFlagSuffix, 0) == 1;

    private void OnEnable()
    {
        ResolveTableRoot();
        _lockedTableY = tableRoot != null ? tableRoot.position.y : PingPongGeometry.TableCenter.y;
        _loadedSavedPlacement = false;
        SyncHeightDependentValues();

        if (loadSavedPlacementOnEnable)
        {
            LoadSavedPlacement();
        }
    }

    private void OnDisable()
    {
        if (ballGrabber != null)
        {
            ballGrabber.suppressGrab = false;
        }

        _dragging = false;
        _wasGripPressed = false;
    }

    private void Update()
    {
        ResolveTableRoot();
        if (tableRoot == null || controllerTransform == null) return;

        var gripPressed = IsGripPressed();
        var nearHandle = Vector3.Distance(controllerTransform.position, transform.position) <= activationRadius;

        if (ballGrabber != null)
        {
            ballGrabber.suppressGrab = nearHandle || _dragging;
        }

        if (!gripPressed)
        {
            if (_dragging)
            {
                EndDrag();
            }

            _wasGripPressed = false;
            return;
        }

        if (!_dragging && !_wasGripPressed && nearHandle && (ballGrabber == null || !ballGrabber.IsHoldingBall))
        {
            BeginDrag();
        }

        if (_dragging)
        {
            DragTable();
        }

        _wasGripPressed = gripPressed;
    }

    private void BeginDrag()
    {
        _dragging = true;
        _lastControllerPosition = controllerTransform.position;
        _lockedTableY = tableRoot.position.y;
    }

    private void EndDrag()
    {
        _dragging = false;

        if (savePlacementOnRelease)
        {
            SavePlacement();
        }
    }

    private void DragTable()
    {
        var controllerDelta = controllerTransform.position - _lastControllerPosition;
        _lastControllerPosition = controllerTransform.position;

        var nextPosition = tableRoot.position + new Vector3(controllerDelta.x, 0f, controllerDelta.z);
        if (lockTableHeight)
        {
            nextPosition.y = _lockedTableY;
        }

        if (constrainToBounds)
        {
            nextPosition.x = Mathf.Clamp(nextPosition.x, xBounds.x, xBounds.y);
            nextPosition.z = Mathf.Clamp(nextPosition.z, zBounds.x, zBounds.y);
        }

        SetTablePosition(nextPosition);
    }

    public void SetTablePosition(Vector3 nextPosition)
    {
        ResolveTableRoot();
        if (tableRoot == null) return;

        var previousPosition = tableRoot.position;
        var delta = nextPosition - previousPosition;
        if (delta.sqrMagnitude <= 0.0000001f) return;

        tableRoot.position = nextPosition;
        SyncHeightDependentValues();
        AcceptTableTransform();

        if (syncedTransforms == null) return;
        foreach (var syncedTransform in syncedTransforms)
        {
            if (syncedTransform == null || syncedTransform == tableRoot || syncedTransform.IsChildOf(tableRoot)) continue;
            syncedTransform.position += delta;
        }
    }

    public bool LoadSavedPlacement()
    {
        ResolveTableRoot();
        if (tableRoot == null || _loadedSavedPlacement || !HasSavedPlacement) return false;

        var position = new Vector3(
            PlayerPrefs.GetFloat(placementSaveKey + PositionXSuffix, tableRoot.position.x),
            PlayerPrefs.GetFloat(placementSaveKey + PositionYSuffix, tableRoot.position.y),
            PlayerPrefs.GetFloat(placementSaveKey + PositionZSuffix, tableRoot.position.z));

        SetTablePosition(position);
        _lockedTableY = position.y;
        _loadedSavedPlacement = true;
        return true;
    }

    public void SavePlacement()
    {
        ResolveTableRoot();
        if (tableRoot == null || string.IsNullOrEmpty(placementSaveKey)) return;

        PlayerPrefs.SetInt(placementSaveKey + SavedFlagSuffix, 1);
        PlayerPrefs.SetFloat(placementSaveKey + PositionXSuffix, tableRoot.position.x);
        PlayerPrefs.SetFloat(placementSaveKey + PositionYSuffix, tableRoot.position.y);
        PlayerPrefs.SetFloat(placementSaveKey + PositionZSuffix, tableRoot.position.z);
        PlayerPrefs.Save();
    }

    public void ClearSavedPlacement()
    {
        PlayerPrefs.DeleteKey(placementSaveKey + SavedFlagSuffix);
        PlayerPrefs.DeleteKey(placementSaveKey + PositionXSuffix);
        PlayerPrefs.DeleteKey(placementSaveKey + PositionYSuffix);
        PlayerPrefs.DeleteKey(placementSaveKey + PositionZSuffix);
        PlayerPrefs.Save();
    }

    public void SyncHeightDependentValues()
    {
        ResolveTableRoot();
        if (tableRoot == null) return;

        var tableTopY = GetTableTopY();
        SyncBallSpawners(tableTopY);
        SyncControllerLimiters(tableTopY);
    }

    private void SyncBallSpawners(float tableTopY)
    {
        if (syncedSpawners == null) return;

        foreach (var spawner in syncedSpawners)
        {
            if (spawner == null) continue;

            spawner.netWorldZ = tableRoot.position.z;
            spawner.minimumNetClearanceHeight = tableTopY + PingPongGeometry.NetHeight + minimumNetClearanceAboveNet;
            spawner.tableBounceWorldY = tableTopY + PingPongGeometry.BallRadius;
            spawner.tableBounceWorldZ = tableRoot.position.z + tableBounceLocalZ;
        }
    }

    private void SyncControllerLimiters(float tableTopY)
    {
        if (syncedControllerLimiters == null || syncedControllerLimiters.Length == 0)
        {
            syncedControllerLimiters = FindObjectsOfType<ControllerTableCollisionLimiter>(true);
        }

        if (syncedControllerLimiters == null) return;

        foreach (var limiter in syncedControllerLimiters)
        {
            if (limiter == null) continue;

            limiter.tableTransform = tableRoot;
            limiter.tableTopY = tableTopY;
        }
    }

    private float GetTableTopY()
    {
        return tableRoot.position.y + PingPongGeometry.TableThickness * 0.5f;
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

    private void ResolveTableRoot()
    {
        if (tableRoot != null) return;

        var table = GameObject.Find("Table");
        if (table != null)
        {
            tableRoot = table.transform;
        }
    }
}
