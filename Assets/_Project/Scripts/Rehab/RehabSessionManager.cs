using TMPro;
using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class RehabSessionManager : MonoBehaviour
    {
        public HandPoseTracker handPoseTracker;
        public SafetyMonitor safetyMonitor;
        public MovementEvaluator movementEvaluator;
        public TrainingResultRecorder resultRecorder;

        public Transform trainingAreaRoot;
        public Transform promptCanvas;
        public TMP_Text titleText;
        public TMP_Text statusText;
        public TMP_Text timerText;
        public TMP_Text debugText;

        public float sessionDurationSeconds = 300f;
        public float trainingDistanceMeters = 1.5f;
        public float trainingFloorY = 0f;
        public float promptHeightMeters = 1.65f;
        public float promptForwardOffsetMeters = 0.85f;
        public bool autoStartSession = true;
        public bool placeTrainingAreaOnStart = true;
        public bool useOpenSpacePlacement = true;
        public bool refreshOpenSpaceAfterPlacement = false;
        public float openSpaceClearanceRadiusMeters = 0.85f;
        public float openSpaceClearanceHeightMeters = 1.7f;
        public float openSpaceMinDistanceMeters = 1.2f;
        public float openSpaceMaxDistanceMeters = 3.0f;
        public float openSpaceSearchDurationSeconds = 10f;
        public float openSpaceSearchIntervalSeconds = 0.5f;
        public LayerMask openSpaceObstacleMask = ~0;

        private RehabTrainingResult _currentResult;
        private Vector3 _trainingCenter;
        private float _elapsedTrainingSeconds;
        private bool _sessionActive;
        private bool _sessionEnded;
        private bool _trainingAreaPlaced;
        private float _openSpaceSearchUntilTime;
        private float _nextOpenSpaceSearchTime;

        public Vector3 TrainingCenter
        {
            get { return _trainingCenter; }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();

            if (placeTrainingAreaOnStart)
            {
                TryPlaceTrainingArea();
            }

            if (autoStartSession)
            {
                BeginSession();
            }
        }

        private void Update()
        {
            if (!_sessionActive || _sessionEnded) return;
            if (handPoseTracker == null || movementEvaluator == null || safetyMonitor == null) return;

            var sample = handPoseTracker.GetCurrentSample();
            if (!sample.IsValid)
            {
                SetStatus("等待 XR 追踪");
                return;
            }

            if (!_trainingAreaPlaced)
            {
                PlaceTrainingArea(sample);
            }
            else if (ShouldRefreshOpenSpacePlacement())
            {
                PlaceTrainingArea(sample);
                _nextOpenSpaceSearchTime = Time.time + Mathf.Max(0.1f, openSpaceSearchIntervalSeconds);
            }

            var safety = safetyMonitor.Evaluate(sample.headPosition, _trainingCenter, true);
            var paused = safety.isPaused;

            if (!paused)
            {
                _elapsedTrainingSeconds += Time.deltaTime;
            }

            var evaluation = movementEvaluator.Evaluate(sample, Time.deltaTime, paused, _elapsedTrainingSeconds);

            if (paused)
            {
                SetStatus("请回到训练圈");
            }
            else
            {
                SetStatus(evaluation.statusMessage);
            }

            RefreshTimer();
            RefreshDebug(evaluation, safety);

            if (evaluation.completed)
            {
                EndSession(RehabSessionEndReason.Completed);
                return;
            }

            if (_elapsedTrainingSeconds >= sessionDurationSeconds)
            {
                EndSession(RehabSessionEndReason.TimeLimit);
            }
        }

        public void BeginSession()
        {
            ResolveReferences();

            if (movementEvaluator == null || safetyMonitor == null || resultRecorder == null)
            {
                Debug.LogError("Rehab session cannot start because one or more required components are missing.");
                return;
            }

            movementEvaluator.ResetEvaluation();
            safetyMonitor.ResetMonitor();
            _elapsedTrainingSeconds = 0f;
            _sessionEnded = false;
            _sessionActive = true;
            _openSpaceSearchUntilTime = Time.time + Mathf.Max(0f, openSpaceSearchDurationSeconds);
            _nextOpenSpaceSearchTime = 0f;
            _currentResult = RehabTrainingResult.CreateStarted(
                movementEvaluator.movementId,
                movementEvaluator.movementName,
                sessionDurationSeconds);

            if (!_trainingAreaPlaced)
            {
                TryPlaceTrainingArea();
            }

            RefreshTitle();
            SetStatus("请准备：双手托天理三焦");
            RefreshTimer();
        }

        public void StopSession()
        {
            if (!_sessionActive || _sessionEnded) return;
            EndSession(RehabSessionEndReason.Stopped);
        }

        public void RecenterTrainingArea()
        {
            _trainingAreaPlaced = false;
            TryPlaceTrainingArea();
        }

        public void SetTrainingAreaCenter(Vector3 center, Vector3 forward, Vector3 headPosition)
        {
            center.y = trainingFloorY;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = center - headPosition;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            _trainingCenter = center;
            ApplyTrainingAreaTransform(forward.normalized, headPosition);
        }

        private void EndSession(RehabSessionEndReason reason)
        {
            if (_sessionEnded) return;

            _sessionEnded = true;
            _sessionActive = false;

            var completed = reason == RehabSessionEndReason.Completed;
            if (_currentResult != null)
            {
                _currentResult.Finish(
                    reason,
                    completed,
                    _elapsedTrainingSeconds,
                    movementEvaluator != null ? movementEvaluator.CompletionTimeSeconds : -1f,
                    movementEvaluator != null ? movementEvaluator.BestHoldSeconds : 0f,
                    safetyMonitor != null ? safetyMonitor.PauseCount : 0,
                    safetyMonitor != null ? safetyMonitor.MaxHeadDistanceFromCenterMeters : 0f);

                if (resultRecorder != null)
                {
                    resultRecorder.SaveResult(_currentResult);
                }
            }

            SetStatus(completed ? "动作完成" : "训练结束");
            RefreshTimer();
        }

        private void TryPlaceTrainingArea()
        {
            if (handPoseTracker == null) return;

            var sample = handPoseTracker.GetCurrentSample();
            if (sample.hasHead)
            {
                PlaceTrainingArea(sample);
                return;
            }

            _trainingCenter = new Vector3(0f, trainingFloorY, trainingDistanceMeters);
            ApplyTrainingAreaTransform(Vector3.forward, new Vector3(0f, 1.6f, 0f));
        }

        private void PlaceTrainingArea(RehabPoseSample sample)
        {
            var forward = sample.headRotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            if (useOpenSpacePlacement)
            {
                var placement = OpenSpacePlacementSolver.FindBestPlacement(
                    sample.headPosition,
                    sample.headRotation,
                    trainingFloorY,
                    trainingDistanceMeters,
                    openSpaceMinDistanceMeters,
                    openSpaceMaxDistanceMeters,
                    openSpaceClearanceRadiusMeters,
                    openSpaceClearanceHeightMeters,
                    openSpaceObstacleMask);

                _trainingCenter = placement.center;
                ApplyTrainingAreaTransform(placement.forward, sample.headPosition);
                return;
            }

            _trainingCenter = sample.headPosition + forward * trainingDistanceMeters;
            _trainingCenter.y = trainingFloorY;
            ApplyTrainingAreaTransform(forward, sample.headPosition);
        }

        private bool ShouldRefreshOpenSpacePlacement()
        {
            if (!useOpenSpacePlacement) return false;
            if (!refreshOpenSpaceAfterPlacement && _trainingAreaPlaced) return false;
            if (Time.time > _openSpaceSearchUntilTime) return false;
            return Time.time >= _nextOpenSpaceSearchTime;
        }

        private void ApplyTrainingAreaTransform(Vector3 forward, Vector3 headPosition)
        {
            if (trainingAreaRoot != null)
            {
                trainingAreaRoot.position = _trainingCenter;
                trainingAreaRoot.rotation = Quaternion.identity;
            }

            if (promptCanvas != null)
            {
                var promptForward = forward;
                promptForward.y = 0f;
                if (promptForward.sqrMagnitude < 0.0001f)
                {
                    promptForward = Vector3.forward;
                }

                promptForward.Normalize();
                promptCanvas.position = _trainingCenter + promptForward * promptForwardOffsetMeters + Vector3.up * promptHeightMeters;
                var toPrompt = promptCanvas.position - headPosition;
                toPrompt.y = 0f;
                if (toPrompt.sqrMagnitude < 0.0001f)
                {
                    toPrompt = forward;
                }

                promptCanvas.rotation = Quaternion.LookRotation(toPrompt.normalized, Vector3.up);
            }

            _trainingAreaPlaced = true;
        }

        private void ResolveReferences()
        {
            if (handPoseTracker == null) handPoseTracker = FindObjectOfType<HandPoseTracker>(true);
            if (safetyMonitor == null) safetyMonitor = FindObjectOfType<SafetyMonitor>(true);
            if (movementEvaluator == null) movementEvaluator = FindObjectOfType<MovementEvaluator>(true);
            if (resultRecorder == null) resultRecorder = FindObjectOfType<TrainingResultRecorder>(true);
        }

        private void RefreshTitle()
        {
            if (titleText != null && movementEvaluator != null)
            {
                titleText.text = movementEvaluator.movementName;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void RefreshTimer()
        {
            if (timerText == null) return;

            var remaining = Mathf.Max(0f, sessionDurationSeconds - _elapsedTrainingSeconds);
            timerText.text = string.Format("剩余 {0:00}:{1:00}", Mathf.FloorToInt(remaining / 60f), Mathf.FloorToInt(remaining % 60f));
        }

        private void RefreshDebug(RehabMovementEvaluation evaluation, RehabSafetyState safety)
        {
            if (debugText == null) return;

            debugText.text = string.Format(
                "保持 {0:0.0}s | 最佳 {1:0.0}s | 距中心 {2:0.00}m",
                evaluation.currentHoldSeconds,
                evaluation.bestHoldSeconds,
                safety.headDistanceFromCenterMeters);
        }
    }
}
