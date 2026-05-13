using System.Collections.Generic;
using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class MovementEvaluator : MonoBehaviour
    {
        public RehabMovementId movementId = RehabMovementId.Baduanjin_TwoHandsLiftHeaven;
        public string movementName = "八段锦：双手托天理三焦";
        public float handsAboveHeadMeters = 0.15f;
        public float maximumHandHeightDifferenceMeters = 0.18f;
        public float minimumHoldSeconds = 2f;
        public float maximumHoldSeconds = 5f;

        public RehabTrainingMode trainingMode = RehabTrainingMode.Baduanjin;
        public BaduanjinEvaluator baduanjinEvaluator;
        public TaiChiEvaluator taiChiEvaluator;
        public MovementDefinition[] movementDefinitions;
        public bool autoCreateDefaultBaduanjinDefinitions = true;
        public float defaultStepTimeoutSeconds = 25f;

        private readonly List<RehabMovementResult> _movementResults = new List<RehabMovementResult>();
        private int _movementIndex;
        private int _stepIndex;
        private int _completedStepCount;
        private int _validStepCount;
        private int _movementStartSafetyWarningCount;
        private float _currentHoldSeconds;
        private float _bestHoldSeconds;
        private float _completionTimeSeconds = -1f;
        private float _stepElapsedSeconds;
        private float _movementStartSessionSeconds;
        private float _lastSymmetry = 1f;
        private float _lastTempo = 1f;
        private float _movementSymmetryTotal;
        private float _movementTempoTotal;
        private int _movementMetricCount;
        private bool _completed;
        private bool _movementStarted;
        private bool _currentMovementSkippedByTimeout;

        public float BestHoldSeconds
        {
            get { return _bestHoldSeconds; }
        }

        public float CompletionTimeSeconds
        {
            get { return _completionTimeSeconds; }
        }

        public bool Completed
        {
            get { return _completed; }
        }

        public MovementDefinition CurrentMovement
        {
            get
            {
                EnsureDefinitions();
                if (movementDefinitions == null || movementDefinitions.Length == 0) return null;
                return movementDefinitions[Mathf.Clamp(_movementIndex, 0, movementDefinitions.Length - 1)];
            }
        }

        public MovementStepDefinition CurrentStep
        {
            get
            {
                var movement = CurrentMovement;
                return movement != null ? movement.GetStep(_stepIndex) : null;
            }
        }

        public IReadOnlyList<RehabMovementResult> MovementResults
        {
            get { return _movementResults; }
        }

        public RehabMovementResult[] GetMovementResultsSnapshot()
        {
            return _movementResults.ToArray();
        }

        public void ResetEvaluation()
        {
            EnsureDefinitions();
            EnsureEvaluatorForMode();

            _movementIndex = 0;
            _stepIndex = 0;
            _completedStepCount = 0;
            _validStepCount = 0;
            _currentHoldSeconds = 0f;
            _bestHoldSeconds = 0f;
            _completionTimeSeconds = -1f;
            _stepElapsedSeconds = 0f;
            _movementStartSessionSeconds = 0f;
            _lastSymmetry = 1f;
            _lastTempo = 1f;
            _movementSymmetryTotal = 0f;
            _movementTempoTotal = 0f;
            _movementMetricCount = 0;
            _completed = false;
            _movementStarted = false;
            _currentMovementSkippedByTimeout = false;
            _movementStartSafetyWarningCount = 0;
            _movementResults.Clear();

            var movement = CurrentMovement;
            if (movement != null)
            {
                movementId = movement.movementId;
                movementName = movement.movementName;
            }
        }

        public RehabMovementEvaluation Evaluate(RehabPoseSample sample, float deltaTime, bool paused, float elapsedSessionSeconds)
        {
            return Evaluate(sample, deltaTime, paused, elapsedSessionSeconds, 0);
        }

        public RehabMovementEvaluation Evaluate(
            RehabPoseSample sample,
            float deltaTime,
            bool paused,
            float elapsedSessionSeconds,
            int safetyWarningCount)
        {
            EnsureDefinitions();
            EnsureEvaluatorForMode();

            if (_completed)
            {
                return CreateSnapshot(false, false, false, "全部八段锦动作已完成");
            }

            var movement = CurrentMovement;
            var step = CurrentStep;
            if (movement == null || step == null)
            {
                _completed = true;
                _completionTimeSeconds = elapsedSessionSeconds;
                return CreateSnapshot(false, false, false, "未配置动作，训练结束");
            }

            if (!_movementStarted)
            {
                StartCurrentMovement(sample, elapsedSessionSeconds, safetyWarningCount);
            }

            if (!sample.IsValid)
            {
                _currentHoldSeconds = 0f;
                return CreateSnapshot(false, false, false, "等待头显和左右手柄追踪");
            }

            if (paused)
            {
                return CreateSnapshot(false, false, false, "训练已暂停，请回到训练圈内");
            }

            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            _stepElapsedSeconds += safeDeltaTime;

            var stepEvaluation = EvaluateCurrentStep(movement, _stepIndex, sample, safeDeltaTime);
            _lastSymmetry = stepEvaluation.symmetry;
            _lastTempo = stepEvaluation.tempo;

            if (stepEvaluation.poseValid)
            {
                _currentHoldSeconds += safeDeltaTime;
                if (_currentHoldSeconds > _bestHoldSeconds)
                {
                    _bestHoldSeconds = _currentHoldSeconds;
                }
            }
            else
            {
                _currentHoldSeconds = 0f;
            }

            var requiredHoldSeconds = Mathf.Max(0.1f, step.requiredHoldSeconds > 0f ? step.requiredHoldSeconds : minimumHoldSeconds);
            if (_currentHoldSeconds >= requiredHoldSeconds)
            {
                CompleteCurrentStep(false, elapsedSessionSeconds, safetyWarningCount);
                return CreateSnapshot(true, true, false, "步骤完成");
            }

            var timeoutSeconds = Mathf.Max(1f, step.timeoutSeconds > 0f ? step.timeoutSeconds : defaultStepTimeoutSeconds);
            if (_stepElapsedSeconds >= timeoutSeconds)
            {
                CompleteCurrentStep(true, elapsedSessionSeconds, safetyWarningCount);
                return CreateSnapshot(false, true, true, "步骤超时，已自动跳过");
            }

            var message = stepEvaluation.poseValid
                ? string.Format("保持动作 {0:0.0}/{1:0.0}s", _currentHoldSeconds, requiredHoldSeconds)
                : stepEvaluation.statusMessage;

            return CreateSnapshot(stepEvaluation.poseValid, false, false, message);
        }

        public static bool IsTwoHandsLiftHeavenPoseValid(
            RehabPoseSample sample,
            float handsAboveHeadMeters,
            float maximumHandHeightDifferenceMeters)
        {
            if (!sample.IsValid) return false;

            var minimumHandHeight = sample.headPosition.y + handsAboveHeadMeters;
            var leftHighEnough = sample.leftHandPosition.y >= minimumHandHeight;
            var rightHighEnough = sample.rightHandPosition.y >= minimumHandHeight;
            var heightDifference = Mathf.Abs(sample.leftHandPosition.y - sample.rightHandPosition.y);

            return leftHighEnough &&
                   rightHighEnough &&
                   heightDifference <= maximumHandHeightDifferenceMeters;
        }

        private void StartCurrentMovement(RehabPoseSample sample, float elapsedSessionSeconds, int safetyWarningCount)
        {
            var movement = CurrentMovement;
            if (movement == null) return;

            _movementStarted = true;
            _movementStartSessionSeconds = elapsedSessionSeconds;
            _movementStartSafetyWarningCount = safetyWarningCount;
            _validStepCount = 0;
            _currentMovementSkippedByTimeout = false;
            _stepElapsedSeconds = 0f;
            _currentHoldSeconds = 0f;
            _lastSymmetry = 1f;
            _lastTempo = 1f;
            _movementSymmetryTotal = 0f;
            _movementTempoTotal = 0f;
            _movementMetricCount = 0;

            movementId = movement.movementId;
            movementName = movement.movementName;
            ResetEvaluatorForMovement(movement, sample);
        }

        private void CompleteCurrentStep(bool timedOut, float elapsedSessionSeconds, int safetyWarningCount)
        {
            var movement = CurrentMovement;
            if (movement == null) return;

            _completedStepCount++;
            if (!timedOut)
            {
                _validStepCount++;
            }
            else
            {
                _currentMovementSkippedByTimeout = true;
            }

            _movementSymmetryTotal += Mathf.Clamp01(_lastSymmetry);
            _movementTempoTotal += Mathf.Clamp01(_lastTempo);
            _movementMetricCount++;

            _stepIndex++;
            _currentHoldSeconds = 0f;
            _stepElapsedSeconds = 0f;

            if (_stepIndex >= Mathf.Max(1, movement.StepCount))
            {
                FinishCurrentMovement(elapsedSessionSeconds, safetyWarningCount);
                _movementIndex++;
                _stepIndex = 0;
                _movementStarted = false;

                if (_movementIndex >= MovementCount)
                {
                    _completed = true;
                    _completionTimeSeconds = elapsedSessionSeconds;
                }
            }
        }

        private void FinishCurrentMovement(float elapsedSessionSeconds, int safetyWarningCount)
        {
            var movement = CurrentMovement;
            if (movement == null) return;

            var stepCount = Mathf.Max(1, movement.StepCount);
            var metricCount = Mathf.Max(1, _movementMetricCount);
            _movementResults.Add(new RehabMovementResult
            {
                movementId = movement.movementId.ToString(),
                movementName = movement.movementName,
                duration = Mathf.Max(0f, elapsedSessionSeconds - _movementStartSessionSeconds),
                completion = Mathf.Clamp01((float)_validStepCount / stepCount),
                symmetry = Mathf.Clamp01(_movementSymmetryTotal / metricCount),
                tempo = Mathf.Clamp01(_movementTempoTotal / metricCount),
                safetyWarningCount = Mathf.Max(0, safetyWarningCount - _movementStartSafetyWarningCount),
                timestamp = System.DateTime.UtcNow.ToString("o"),
                skippedByTimeout = _currentMovementSkippedByTimeout
            });
        }

        public void FinalizeCurrentMovement(float elapsedSessionSeconds, int safetyWarningCount)
        {
            if (_completed || !_movementStarted) return;

            if (_movementMetricCount == 0)
            {
                _movementSymmetryTotal += Mathf.Clamp01(_lastSymmetry);
                _movementTempoTotal += Mathf.Clamp01(_lastTempo);
                _movementMetricCount++;
            }

            FinishCurrentMovement(elapsedSessionSeconds, safetyWarningCount);
            _movementStarted = false;
        }

        private RehabMovementEvaluation CreateSnapshot(bool poseValid, bool stepCompleted, bool stepTimedOut, string message)
        {
            var movement = CurrentMovement;
            var step = CurrentStep;
            var movementCount = MovementCount;
            var stepCount = movement != null ? Mathf.Max(1, movement.StepCount) : 1;
            var completedSteps = _completed ? TotalStepCount : _completedStepCount;
            var completion01 = TotalStepCount > 0 ? Mathf.Clamp01((float)completedSteps / TotalStepCount) : 0f;
            var timeoutSeconds = step != null ? Mathf.Max(1f, step.timeoutSeconds > 0f ? step.timeoutSeconds : defaultStepTimeoutSeconds) : defaultStepTimeoutSeconds;

            return new RehabMovementEvaluation
            {
                poseValid = poseValid,
                completed = _completed,
                stepCompleted = stepCompleted,
                stepTimedOut = stepTimedOut,
                currentHoldSeconds = _currentHoldSeconds,
                bestHoldSeconds = _bestHoldSeconds,
                completionTimeSeconds = _completionTimeSeconds,
                remainingSeconds = Mathf.Max(0f, timeoutSeconds - _stepElapsedSeconds),
                completion01 = completion01,
                symmetry = _lastSymmetry,
                tempo = _lastTempo,
                movementIndex = Mathf.Clamp(_movementIndex, 0, Mathf.Max(0, movementCount - 1)),
                movementCount = movementCount,
                stepIndex = Mathf.Clamp(_stepIndex, 0, stepCount - 1),
                stepCount = stepCount,
                movementName = movement != null ? movement.movementName : string.Empty,
                stepInstruction = step != null ? step.instruction : string.Empty,
                statusMessage = message
            };
        }

        private void EnsureDefinitions()
        {
            if (!autoCreateDefaultBaduanjinDefinitions && movementDefinitions != null && movementDefinitions.Length > 0) return;

            if (movementDefinitions == null ||
                movementDefinitions.Length == 0 ||
                DefinitionsDoNotMatchTrainingMode())
            {
                movementDefinitions = trainingMode == RehabTrainingMode.TaiChiTraining
                    ? TaiChiEvaluator.CreateDefaultMovements()
                    : BaduanjinEvaluator.CreateDefaultMovements();
            }
        }

        private void EnsureBaduanjinEvaluator()
        {
            if (baduanjinEvaluator == null)
            {
                baduanjinEvaluator = GetComponent<BaduanjinEvaluator>();
            }

            if (baduanjinEvaluator == null)
            {
                baduanjinEvaluator = gameObject.AddComponent<BaduanjinEvaluator>();
            }

            baduanjinEvaluator.handsAboveHeadMeters = handsAboveHeadMeters;
            baduanjinEvaluator.maximumHandHeightDifferenceMeters = maximumHandHeightDifferenceMeters;
        }

        private void EnsureTaiChiEvaluator()
        {
            if (taiChiEvaluator == null)
            {
                taiChiEvaluator = GetComponent<TaiChiEvaluator>();
            }

            if (taiChiEvaluator == null)
            {
                taiChiEvaluator = gameObject.AddComponent<TaiChiEvaluator>();
            }
        }

        private void EnsureEvaluatorForMode()
        {
            if (trainingMode == RehabTrainingMode.TaiChiTraining)
            {
                EnsureTaiChiEvaluator();
            }
            else
            {
                EnsureBaduanjinEvaluator();
            }
        }

        private BaduanjinStepEvaluation EvaluateCurrentStep(
            MovementDefinition movement,
            int stepIndex,
            RehabPoseSample sample,
            float deltaTime)
        {
            if (IsTaiChiMovement(movement))
            {
                EnsureTaiChiEvaluator();
                return taiChiEvaluator.EvaluateStep(movement, stepIndex, sample, deltaTime);
            }

            EnsureBaduanjinEvaluator();
            return baduanjinEvaluator.EvaluateStep(movement, stepIndex, sample, deltaTime);
        }

        private void ResetEvaluatorForMovement(MovementDefinition movement, RehabPoseSample sample)
        {
            if (IsTaiChiMovement(movement))
            {
                EnsureTaiChiEvaluator();
                taiChiEvaluator.ResetForMovement(movement.movementId, sample);
                return;
            }

            EnsureBaduanjinEvaluator();
            baduanjinEvaluator.ResetForMovement(movement.movementId, sample);
        }

        private static bool IsTaiChiMovement(MovementDefinition movement)
        {
            if (movement == null) return false;

            switch (movement.movementId)
            {
                case RehabMovementId.Taiji_Opening:
                case RehabMovementId.Taiji_CloudHands:
                case RehabMovementId.Taiji_PartWildHorsesMane:
                case RehabMovementId.Taiji_WhiteCraneSpreadsWings:
                case RehabMovementId.Taiji_BrushKneeTwistStep:
                case RehabMovementId.Taiji_Closing:
                    return true;
                default:
                    return false;
            }
        }

        private bool DefinitionsDoNotMatchTrainingMode()
        {
            if (movementDefinitions == null || movementDefinitions.Length == 0 || movementDefinitions[0] == null)
            {
                return true;
            }

            var firstIsTaiChi = IsTaiChiMovement(movementDefinitions[0]);
            return trainingMode == RehabTrainingMode.TaiChiTraining ? !firstIsTaiChi : firstIsTaiChi;
        }

        private int MovementCount
        {
            get
            {
                EnsureDefinitions();
                return movementDefinitions != null ? movementDefinitions.Length : 0;
            }
        }

        private int TotalStepCount
        {
            get
            {
                EnsureDefinitions();
                if (movementDefinitions == null) return 0;

                var count = 0;
                for (var i = 0; i < movementDefinitions.Length; i++)
                {
                    count += Mathf.Max(1, movementDefinitions[i] != null ? movementDefinitions[i].StepCount : 0);
                }

                return count;
            }
        }
    }
}
