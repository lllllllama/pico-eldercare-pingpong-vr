using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-50)]
public class TableDragHandle : MonoBehaviour
{
    public Transform tableRoot;
    public Transform controllerTransform;
    public XRNode controllerNode = XRNode.LeftHand;
    public ControllerBallGrabber ballGrabber;
    public Transform[] syncedTransforms;
    public BallSpawner[] syncedSpawners;
    public float activationRadius = 0.18f;
    public float tableBounceLocalZ = -0.55f;
    public bool lockTableHeight = true;
    public bool constrainToBounds = true;
    public Vector2 xBounds = new Vector2(-1.25f, 1.25f);
    public Vector2 zBounds = new Vector2(0.75f, 3.35f);

    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private Vector3 _tableOffsetFromController;
    private float _lockedTableY;
    private bool _dragging;
    private bool _wasGripPressed;

    public bool IsDragging => _dragging;

    private void OnEnable()
    {
        ResolveTableRoot();
        _lockedTableY = tableRoot != null ? tableRoot.position.y : PingPongGeometry.TableCenter.y;
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
            _dragging = false;
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
        _tableOffsetFromController = tableRoot.position - controllerTransform.position;
        _lockedTableY = tableRoot.position.y;
    }

    private void DragTable()
    {
        var nextPosition = controllerTransform.position + _tableOffsetFromController;
        if (lockTableHeight)
        {
            nextPosition.y = _lockedTableY;
        }

        if (constrainToBounds)
        {
            nextPosition.x = Mathf.Clamp(nextPosition.x, xBounds.x, xBounds.y);
            nextPosition.z = Mathf.Clamp(nextPosition.z, zBounds.x, zBounds.y);
        }

        MoveTable(nextPosition);
    }

    private void MoveTable(Vector3 nextPosition)
    {
        var previousPosition = tableRoot.position;
        var delta = nextPosition - previousPosition;
        if (delta.sqrMagnitude <= 0.0000001f) return;

        tableRoot.position = nextPosition;
        SyncBallSpawners();

        if (syncedTransforms == null) return;
        foreach (var syncedTransform in syncedTransforms)
        {
            if (syncedTransform == null || syncedTransform == tableRoot || syncedTransform.IsChildOf(tableRoot)) continue;
            syncedTransform.position += delta;
        }
    }

    private void SyncBallSpawners()
    {
        if (syncedSpawners == null) return;

        foreach (var spawner in syncedSpawners)
        {
            if (spawner == null) continue;

            spawner.netWorldZ = tableRoot.position.z;
            spawner.tableBounceWorldY = tableRoot.position.y + PingPongGeometry.TableThickness * 0.5f + PingPongGeometry.BallRadius;
            spawner.tableBounceWorldZ = tableRoot.position.z + tableBounceLocalZ;
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
