using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public enum PingPongTableSafetyState
{
    Clear,
    Warning,
    Blocked
}

[DefaultExecutionOrder(120)]
public class PingPongPlayerTableSafety : MonoBehaviour
{
    public Transform tableTransform;
    public TableDragHandle tableDragHandle;
    public Transform hmdTransform;
    public PingPongPlayerBodyProxy playerBodyProxy;
    public BallSpawner[] ballSpawners;
    public Vector2 tableSize = new Vector2(PingPongGeometry.TableWidth, PingPongGeometry.TableLength);
    public float safetyMargin = 0.20f;
    public float hardMargin = 0.08f;
    public float repulsionStrength = 0.45f;
    public float maxRepulsionSpeed = 0.35f;
    public float warningOnlyDistance = 0.35f;
    public float hardPauseDistance = 0.06f;
    public float blockedMarginMeters = 0.25f;
    public float warningMarginMeters = 0.35f;
    public float resumeStableSeconds = 0.5f;
    public float tableCenterHeightAboveFloor = PingPongGeometry.TableTopHeight - PingPongGeometry.TableThickness * 0.5f;
    public bool controlServing = true;
    public bool clearBallsOnBlock = true;
    public bool moveRigWhenInside = false;
    public bool createRuntimePrompt = true;
    public bool createRuntimeBoundary = true;
    public Transform warningCanvasTransform;
    public TMP_Text warningText;
    public LineRenderer boundaryLine;
    public float promptHeightMeters = 1.35f;
    public float promptOuterOffsetMeters = 0.45f;
    public float hapticAmplitude = 0.12f;
    public float hapticDurationSeconds = 0.08f;
    public float hapticIntervalSeconds = 0.75f;

    private const string WarningPrompt = "\u8bf7\u4e0e\u7403\u684c\u4fdd\u6301\u8ddd\u79bb";
    private const string BlockedPrompt = "\u8bf7\u9000\u5230\u7403\u684c\u5916";

    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private Camera _camera;
    private bool _servingStoppedBySafety;
    private bool _wasBlocked;
    private float _safeSinceTime = -1f;
    private float _nextHapticTime;

    public PingPongTableSafetyState CurrentState { get; private set; }
    public bool IsBlocked => CurrentState == PingPongTableSafetyState.Blocked;

    private void OnEnable()
    {
        _safeSinceTime = -1f;
        CurrentState = PingPongTableSafetyState.Clear;
        ConfigureSafetyLayerIgnores();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (tableTransform == null) return;

        var headPosition = GetHeadPosition();
        var evaluation = EvaluateSafety(headPosition);
        CurrentState = evaluation.state;

        UpdateBoundary(evaluation.state != PingPongTableSafetyState.Clear);
        UpdatePrompt(evaluation.state);

        if (evaluation.state != PingPongTableSafetyState.Clear)
        {
            ApplyRepulsion(evaluation);
            SendHapticWarning(evaluation.state == PingPongTableSafetyState.Blocked ? hapticAmplitude : hapticAmplitude * 0.5f);
        }

        if (evaluation.state == PingPongTableSafetyState.Blocked)
        {
            _safeSinceTime = -1f;
            if (controlServing)
            {
                StopServingForSafety();
            }

            _wasBlocked = true;
            return;
        }

        if (_wasBlocked || _servingStoppedBySafety)
        {
            if (_safeSinceTime < 0f)
            {
                _safeSinceTime = Time.time;
            }

            if (Time.time - _safeSinceTime >= Mathf.Max(0f, resumeStableSeconds))
            {
                ResumeServingAfterSafety();
                _wasBlocked = false;
            }
        }
    }

    public PingPongTableSafetyState EvaluateHeadPosition(Vector3 headPosition)
    {
        return EvaluateSafety(headPosition).state;
    }

    private SafetyEvaluation EvaluateSafety(Vector3 headPosition)
    {
        if (tableTransform == null)
        {
            return SafetyEvaluation.Clear;
        }

        var local = GetHorizontalTableLocal(headPosition);
        var halfX = tableSize.x * 0.5f;
        var halfZ = tableSize.y * 0.5f;
        var safetyHalfX = halfX + Mathf.Max(0f, safetyMargin);
        var safetyHalfZ = halfZ + Mathf.Max(0f, safetyMargin);
        var warningHalfX = safetyHalfX + Mathf.Max(0f, warningOnlyDistance);
        var warningHalfZ = safetyHalfZ + Mathf.Max(0f, warningOnlyDistance);
        var hardHalfX = halfX + Mathf.Max(Mathf.Max(0f, hardMargin), Mathf.Max(0f, hardPauseDistance));
        var hardHalfZ = halfZ + Mathf.Max(Mathf.Max(0f, hardMargin), Mathf.Max(0f, hardPauseDistance));

        var absX = Mathf.Abs(local.x);
        var absZ = Mathf.Abs(local.y);
        var insideSafety = absX <= safetyHalfX && absZ <= safetyHalfZ;
        var insideWarning = absX <= warningHalfX && absZ <= warningHalfZ;
        var insideHard = absX <= hardHalfX && absZ <= hardHalfZ;

        if (!insideWarning)
        {
            return SafetyEvaluation.Clear;
        }

        var state = insideHard ? PingPongTableSafetyState.Blocked : PingPongTableSafetyState.Warning;
        var exitDirection = Vector3.zero;
        var penetration = 0f;

        if (insideSafety)
        {
            var penetrationX = safetyHalfX - absX;
            var penetrationZ = safetyHalfZ - absZ;
            if (penetrationX < penetrationZ)
            {
                exitDirection = tableTransform.right.normalized * (local.x >= 0f ? 1f : -1f);
                penetration = penetrationX;
            }
            else
            {
                exitDirection = tableTransform.forward.normalized * (local.y >= 0f ? 1f : -1f);
                penetration = penetrationZ;
            }
        }

        return new SafetyEvaluation(state, exitDirection, Mathf.Max(0f, penetration), insideSafety);
    }

    private void ApplyRepulsion(SafetyEvaluation evaluation)
    {
        if (!evaluation.insideRepulsionZone || evaluation.exitDirection.sqrMagnitude < 0.0001f) return;
        if (tableTransform == null) return;

        var moveAwayFromHead = -evaluation.exitDirection.normalized;
        var moveDistance = Mathf.Max(0f, evaluation.penetrationMeters) * Mathf.Max(0f, repulsionStrength);
        if (moveDistance <= 0f) return;

        var targetPosition = tableTransform.position + moveAwayFromHead * moveDistance;
        targetPosition.y = tableTransform.position.y;
        var nextPosition = Vector3.MoveTowards(tableTransform.position, targetPosition, Mathf.Max(0.01f, maxRepulsionSpeed) * Time.deltaTime);

        if (tableDragHandle != null)
        {
            tableDragHandle.SetTablePosition(nextPosition);
            tableDragHandle.SyncHeightDependentValues();
        }
        else
        {
            tableTransform.position = nextPosition;
        }
    }

    private void StopServingForSafety()
    {
        var spawners = ResolveBallSpawners();
        var hadServing = false;
        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;
            hadServing |= spawner.IsServing;
            spawner.StopServing();
            if (clearBallsOnBlock)
            {
                spawner.ClearBalls();
            }
        }

        _servingStoppedBySafety |= hadServing;
    }

    private void ResumeServingAfterSafety()
    {
        if (!_servingStoppedBySafety) return;

        var spawners = ResolveBallSpawners();
        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;
            spawner.StartServing();
        }

        _servingStoppedBySafety = false;
    }

    private void UpdateBoundary(bool visible)
    {
        if (boundaryLine == null && createRuntimeBoundary)
        {
            CreateBoundaryLine();
        }

        if (boundaryLine == null) return;

        boundaryLine.enabled = visible;
        if (!visible || tableTransform == null) return;

        var halfX = tableSize.x * 0.5f + Mathf.Max(0f, safetyMargin);
        var halfZ = tableSize.y * 0.5f + Mathf.Max(0f, safetyMargin);
        var floorY = tableTransform.position.y - tableCenterHeightAboveFloor + 0.02f;
        var center = new Vector3(tableTransform.position.x, floorY, tableTransform.position.z);
        var right = tableTransform.right.normalized;
        var forward = tableTransform.forward.normalized;

        boundaryLine.positionCount = 5;
        boundaryLine.SetPosition(0, center + right * -halfX + forward * -halfZ);
        boundaryLine.SetPosition(1, center + right * halfX + forward * -halfZ);
        boundaryLine.SetPosition(2, center + right * halfX + forward * halfZ);
        boundaryLine.SetPosition(3, center + right * -halfX + forward * halfZ);
        boundaryLine.SetPosition(4, center + right * -halfX + forward * -halfZ);
    }

    private void UpdatePrompt(PingPongTableSafetyState state)
    {
        if (warningText == null && createRuntimePrompt)
        {
            CreatePrompt();
        }

        if (warningCanvasTransform == null || warningText == null || tableTransform == null) return;

        var visible = state != PingPongTableSafetyState.Clear;
        warningCanvasTransform.gameObject.SetActive(visible);
        if (!visible) return;

        warningText.text = state == PingPongTableSafetyState.Blocked ? BlockedPrompt : WarningPrompt;
        warningText.color = state == PingPongTableSafetyState.Blocked ? new Color(1f, 0.42f, 0.36f) : new Color(1f, 0.82f, 0.35f);

        var headPosition = GetHeadPosition();
        var toHead = headPosition - tableTransform.position;
        toHead.y = 0f;
        if (toHead.sqrMagnitude < 0.0001f)
        {
            toHead = -tableTransform.forward;
        }

        toHead.Normalize();
        var promptDistance = Mathf.Max(tableSize.x, tableSize.y) * 0.5f + safetyMargin + warningOnlyDistance + promptOuterOffsetMeters;
        warningCanvasTransform.position = tableTransform.position + toHead * promptDistance + Vector3.up * (promptHeightMeters - tableTransform.position.y);

        var toPrompt = warningCanvasTransform.position - headPosition;
        toPrompt.y = 0f;
        if (toPrompt.sqrMagnitude > 0.0001f)
        {
            warningCanvasTransform.rotation = Quaternion.LookRotation(toPrompt.normalized, Vector3.up);
        }
    }

    private void CreateBoundaryLine()
    {
        var go = new GameObject("TableSafetyBoundary");
        go.transform.SetParent(transform, false);
        boundaryLine = go.AddComponent<LineRenderer>();
        boundaryLine.useWorldSpace = true;
        boundaryLine.loop = false;
        boundaryLine.startWidth = 0.035f;
        boundaryLine.endWidth = 0.035f;
        boundaryLine.startColor = new Color(1f, 0.15f, 0.1f, 0.82f);
        boundaryLine.endColor = new Color(1f, 0.15f, 0.1f, 0.82f);
        boundaryLine.material = new Material(Shader.Find("Sprites/Default"));
        boundaryLine.enabled = false;
    }

    private void CreatePrompt()
    {
        var canvasObject = new GameObject("TableSafetyPrompt");
        canvasObject.transform.SetParent(transform, false);
        warningCanvasTransform = canvasObject.transform;

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObject.transform.localScale = Vector3.one * 0.002f;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(720f, 160f);
        panelRect.anchoredPosition = Vector2.zero;
        var image = panel.AddComponent<Image>();
        image.color = new Color(0.08f, 0.02f, 0.02f, 0.78f);

        var textObject = new GameObject("WarningText");
        textObject.transform.SetParent(panel.transform, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(660f, 110f);
        rect.anchoredPosition = Vector2.zero;
        warningText = textObject.AddComponent<TextMeshProUGUI>();
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.fontSize = 42f;
        warningText.text = WarningPrompt;
        warningText.color = new Color(1f, 0.82f, 0.35f);

        canvasObject.SetActive(false);
    }

    private void SendHapticWarning(float amplitude)
    {
        if (amplitude <= 0f || Time.time < _nextHapticTime) return;
        _nextHapticTime = Time.time + Mathf.Max(0.1f, hapticIntervalSeconds);
        SendHapticWarning(XRNode.LeftHand, amplitude);
        SendHapticWarning(XRNode.RightHand, amplitude);
    }

    private void SendHapticWarning(XRNode node, float amplitude)
    {
        InputDevices.GetDevicesAtXRNode(node, _devices);
        foreach (var device in _devices)
        {
            if (!device.isValid) continue;
            device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0.01f, hapticDurationSeconds));
        }
    }

    private Vector3 GetHeadPosition()
    {
        if (hmdTransform != null)
        {
            return hmdTransform.position;
        }

        if (playerBodyProxy != null)
        {
            return playerBodyProxy.transform.position;
        }

        return tableTransform != null ? tableTransform.position + tableTransform.forward * 3f : Vector3.zero;
    }

    private Vector2 GetHorizontalTableLocal(Vector3 worldPosition)
    {
        var delta = worldPosition - tableTransform.position;
        return new Vector2(Vector3.Dot(delta, tableTransform.right.normalized), Vector3.Dot(delta, tableTransform.forward.normalized));
    }

    private BallSpawner[] ResolveBallSpawners()
    {
        if (ballSpawners != null && ballSpawners.Length > 0)
        {
            return ballSpawners;
        }

        ballSpawners = FindObjectsOfType<BallSpawner>(true);
        return ballSpawners;
    }

    private void ResolveReferences()
    {
        if (tableTransform == null)
        {
            var table = GameObject.Find("Table");
            if (table != null)
            {
                tableTransform = table.transform;
            }
        }

        if (tableDragHandle == null)
        {
            tableDragHandle = FindObjectOfType<TableDragHandle>(true);
        }

        if (hmdTransform == null)
        {
            if (_camera == null)
            {
                _camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>(true);
            }

            if (_camera != null)
            {
                hmdTransform = _camera.transform;
            }
        }

        if (playerBodyProxy == null)
        {
            playerBodyProxy = FindObjectOfType<PingPongPlayerBodyProxy>(true);
        }

        ResolveBallSpawners();
    }

    private static void ConfigureSafetyLayerIgnores()
    {
        var safetyLayer = LayerMask.NameToLayer("TableSafetyZone");
        if (safetyLayer < 0) return;

        IgnoreSafetyCollisionWith(safetyLayer, "Racket");
        IgnoreSafetyCollisionWith(safetyLayer, "Controller");
        IgnoreSafetyCollisionWith(safetyLayer, "Ball");
        IgnoreSafetyCollisionWith(safetyLayer, "Table");
    }

    private static void IgnoreSafetyCollisionWith(int safetyLayer, string layerName)
    {
        var otherLayer = LayerMask.NameToLayer(layerName);
        if (otherLayer >= 0)
        {
            Physics.IgnoreLayerCollision(safetyLayer, otherLayer, true);
        }
    }

    private struct SafetyEvaluation
    {
        public static readonly SafetyEvaluation Clear = new SafetyEvaluation(PingPongTableSafetyState.Clear, Vector3.zero, 0f, false);

        public readonly PingPongTableSafetyState state;
        public readonly Vector3 exitDirection;
        public readonly float penetrationMeters;
        public readonly bool insideRepulsionZone;

        public SafetyEvaluation(PingPongTableSafetyState state, Vector3 exitDirection, float penetrationMeters, bool insideRepulsionZone)
        {
            this.state = state;
            this.exitDirection = exitDirection;
            this.penetrationMeters = penetrationMeters;
            this.insideRepulsionZone = insideRepulsionZone;
        }
    }
}
