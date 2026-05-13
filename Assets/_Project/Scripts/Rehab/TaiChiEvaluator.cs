using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class TaiChiEvaluator : MonoBehaviour
    {
        public float maximumHeadDriftMeters = 0.18f;
        public float maximumHeadYawDriftDegrees = 25f;
        public float minimumTempoSeconds = 0.55f;
        public float openingRaiseMeters = 0.18f;
        public float openingLowerMeters = 0.15f;
        public float closingLowerMeters = 0.10f;
        public float cloudHandsLateralMeters = 0.24f;
        public float cloudHandsForwardMinMeters = 0.18f;
        public float partManeForwardMeters = 0.32f;
        public float partManeBackMeters = 0.18f;
        public float craneHighMeters = 0.20f;
        public float craneLowMeters = 0.30f;
        public float brushKneeLowMeters = 0.34f;
        public float brushKneePushMeters = 0.32f;

        private RehabMovementId _activeMovementId;
        private bool _hasBaseline;
        private Vector3 _baselineHeadPosition;
        private Vector3 _baselineLeftHandPosition;
        private Vector3 _baselineRightHandPosition;
        private float _baselineYaw;
        private float _stepElapsedSeconds;
        private int _activeStepIndex = -1;

        public void ResetForMovement(RehabMovementId movementId, RehabPoseSample sample)
        {
            _activeMovementId = movementId;
            _hasBaseline = sample.IsValid;
            _baselineHeadPosition = sample.hasHead ? sample.headPosition : Vector3.zero;
            _baselineLeftHandPosition = sample.hasLeftHand ? sample.leftHandPosition : Vector3.zero;
            _baselineRightHandPosition = sample.hasRightHand ? sample.rightHandPosition : Vector3.zero;
            _baselineYaw = sample.hasHead ? GetYaw(sample.headRotation) : 0f;
            _stepElapsedSeconds = 0f;
            _activeStepIndex = -1;
        }

        public BaduanjinStepEvaluation EvaluateStep(
            MovementDefinition movement,
            int stepIndex,
            RehabPoseSample sample,
            float deltaTime)
        {
            if (movement == null)
            {
                return Invalid("未配置太极动作");
            }

            if (!sample.IsValid)
            {
                return Invalid("等待头显和左右手柄追踪");
            }

            if (!_hasBaseline || _activeMovementId != movement.movementId)
            {
                ResetForMovement(movement.movementId, sample);
            }

            if (_activeStepIndex != stepIndex)
            {
                _activeStepIndex = stepIndex;
                _stepElapsedSeconds = 0f;
            }

            _stepElapsedSeconds += Mathf.Max(0f, deltaTime);

            switch (movement.movementId)
            {
                case RehabMovementId.Taiji_Opening:
                    return EvaluateOpening(sample, stepIndex);
                case RehabMovementId.Taiji_CloudHands:
                    return EvaluateCloudHands(sample, stepIndex);
                case RehabMovementId.Taiji_PartWildHorsesMane:
                    return EvaluatePartWildHorsesMane(sample, stepIndex);
                case RehabMovementId.Taiji_WhiteCraneSpreadsWings:
                    return EvaluateWhiteCrane(sample);
                case RehabMovementId.Taiji_BrushKneeTwistStep:
                    return EvaluateBrushKnee(sample, stepIndex);
                case RehabMovementId.Taiji_Closing:
                    return EvaluateClosing(sample);
                default:
                    return Invalid("当前动作尚未接入太极判定");
            }
        }

        public static MovementDefinition[] CreateDefaultMovements()
        {
            return new[]
            {
                new MovementDefinition(
                    RehabMovementId.Taiji_Opening,
                    "太极：起势",
                    "双手同步缓慢抬起，再缓慢下落。",
                    new MovementStepDefinition("双手抬起", "双手在身体前方同步缓慢抬起", 1.0f, 25f),
                    new MovementStepDefinition("双手下落", "双手在身体前方同步缓慢下落", 1.0f, 25f)),
                new MovementDefinition(
                    RehabMovementId.Taiji_CloudHands,
                    "太极：云手",
                    "双手在身体前方左右移动，完成 3 个温和周期。",
                    new MovementStepDefinition("云手向左 1", "双手在身体前方向左移动", 0.6f, 18f),
                    new MovementStepDefinition("云手向右 1", "双手在身体前方向右移动", 0.6f, 18f),
                    new MovementStepDefinition("云手向左 2", "双手在身体前方向左移动", 0.6f, 18f),
                    new MovementStepDefinition("云手向右 2", "双手在身体前方向右移动", 0.6f, 18f),
                    new MovementStepDefinition("云手向左 3", "双手在身体前方向左移动", 0.6f, 18f),
                    new MovementStepDefinition("云手向右 3", "双手在身体前方向右移动", 0.6f, 18f)),
                new MovementDefinition(
                    RehabMovementId.Taiji_PartWildHorsesMane,
                    "太极：野马分鬃",
                    "一手前推，另一手后分，左右切换。",
                    new MovementStepDefinition("左手前推", "左手向前推出，右手向后分开", 0.8f, 22f),
                    new MovementStepDefinition("右手前推", "右手向前推出，左手向后分开", 0.8f, 22f)),
                new MovementDefinition(
                    RehabMovementId.Taiji_WhiteCraneSpreadsWings,
                    "太极：白鹤亮翅",
                    "一手上方，一手下方，稳定保持。",
                    new MovementStepDefinition("亮翅保持", "一手上方，一手下方，保持 0.8 秒", 0.8f, 22f)),
                new MovementDefinition(
                    RehabMovementId.Taiji_BrushKneeTwistStep,
                    "太极：搂膝拗步",
                    "一手下搂，一手前推，不判断步法。",
                    new MovementStepDefinition("左搂右推", "左手下搂，右手前推", 0.8f, 22f),
                    new MovementStepDefinition("右搂左推", "右手下搂，左手前推", 0.8f, 22f)),
                new MovementDefinition(
                    RehabMovementId.Taiji_Closing,
                    "太极：收势",
                    "双手缓慢下落，训练结束。",
                    new MovementStepDefinition("双手下落收势", "双手缓慢下落并放松", 1.0f, 25f))
            };
        }

        private BaduanjinStepEvaluation EvaluateOpening(RehabPoseSample sample, int stepIndex)
        {
            var leftRise = sample.leftHandPosition.y - _baselineLeftHandPosition.y;
            var rightRise = sample.rightHandPosition.y - _baselineRightHandPosition.y;
            var averageRise = (leftRise + rightRise) * 0.5f;
            var handsSynced = Mathf.Abs(sample.leftHandPosition.y - sample.rightHandPosition.y) <= 0.18f;
            var tempo = TempoScore();
            var stable = IsHeadStable(sample);

            if (stepIndex == 0)
            {
                return Result(stable && handsSynced && averageRise >= openingRaiseMeters && tempo > 0.2f, "双手同步缓慢抬起", SymmetryFromHeight(sample), tempo);
            }

            return Result(stable && handsSynced && averageRise <= openingLowerMeters && tempo > 0.2f, "双手同步缓慢下落", SymmetryFromHeight(sample), tempo);
        }

        private BaduanjinStepEvaluation EvaluateCloudHands(RehabPoseSample sample, int stepIndex)
        {
            var left = ToHeadLocal(sample, sample.leftHandPosition);
            var right = ToHeadLocal(sample, sample.rightHandPosition);
            var averageX = (left.x + right.x) * 0.5f;
            var forwardValid = left.z >= cloudHandsForwardMinMeters && right.z >= cloudHandsForwardMinMeters;
            var movingLeft = stepIndex % 2 == 0;
            var lateralValid = movingLeft ? averageX <= -cloudHandsLateralMeters : averageX >= cloudHandsLateralMeters;
            var tempo = TempoScore();
            return Result(IsHeadStable(sample) && forwardValid && lateralValid && tempo > 0.2f, movingLeft ? "双手在身体前方向左移动" : "双手在身体前方向右移动", SymmetryFromHeight(sample), tempo);
        }

        private BaduanjinStepEvaluation EvaluatePartWildHorsesMane(RehabPoseSample sample, int stepIndex)
        {
            var leftForward = stepIndex == 0;
            var left = ToHeadLocal(sample, sample.leftHandPosition);
            var right = ToHeadLocal(sample, sample.rightHandPosition);
            var frontHand = leftForward ? left : right;
            var backHand = leftForward ? right : left;
            var valid = frontHand.z >= partManeForwardMeters && backHand.z <= -partManeBackMeters && Mathf.Abs(frontHand.y - backHand.y) <= 0.45f;
            return Result(IsHeadStable(sample) && valid && TempoScore() > 0.2f, leftForward ? "左手前推，右手后分" : "右手前推，左手后分", 1f, TempoScore());
        }

        private BaduanjinStepEvaluation EvaluateWhiteCrane(RehabPoseSample sample)
        {
            var leftHigh = sample.leftHandPosition.y >= sample.headPosition.y + craneHighMeters && sample.rightHandPosition.y <= sample.headPosition.y - craneLowMeters;
            var rightHigh = sample.rightHandPosition.y >= sample.headPosition.y + craneHighMeters && sample.leftHandPosition.y <= sample.headPosition.y - craneLowMeters;
            return Result(IsHeadStable(sample) && (leftHigh || rightHigh), "一手上方，一手下方，稳定保持", 1f, TempoScore());
        }

        private BaduanjinStepEvaluation EvaluateBrushKnee(RehabPoseSample sample, int stepIndex)
        {
            var leftLow = stepIndex == 0;
            var left = ToHeadLocal(sample, sample.leftHandPosition);
            var right = ToHeadLocal(sample, sample.rightHandPosition);
            var lowHand = leftLow ? left : right;
            var pushHand = leftLow ? right : left;
            var valid = lowHand.y <= -brushKneeLowMeters && pushHand.z >= brushKneePushMeters && pushHand.y >= -0.55f && pushHand.y <= 0.10f;
            return Result(IsHeadStable(sample) && valid && TempoScore() > 0.2f, leftLow ? "左手下搂，右手前推" : "右手下搂，左手前推", 1f, TempoScore());
        }

        private BaduanjinStepEvaluation EvaluateClosing(RehabPoseSample sample)
        {
            var leftDrop = _baselineLeftHandPosition.y - sample.leftHandPosition.y;
            var rightDrop = _baselineRightHandPosition.y - sample.rightHandPosition.y;
            var averageDrop = (leftDrop + rightDrop) * 0.5f;
            var leftLow = sample.leftHandPosition.y <= sample.headPosition.y - openingLowerMeters;
            var rightLow = sample.rightHandPosition.y <= sample.headPosition.y - openingLowerMeters;
            var tempo = TempoScore();
            return Result(
                IsHeadStable(sample) && leftLow && rightLow && averageDrop >= closingLowerMeters && tempo > 0.2f,
                "双手缓慢下落并放松",
                SymmetryFromHeight(sample),
                tempo);
        }

        private bool IsHeadStable(RehabPoseSample sample)
        {
            var drift = sample.headPosition - _baselineHeadPosition;
            drift.y = 0f;
            var yawDrift = Mathf.Abs(Mathf.DeltaAngle(_baselineYaw, GetYaw(sample.headRotation)));
            return drift.magnitude <= maximumHeadDriftMeters && yawDrift <= maximumHeadYawDriftDegrees;
        }

        private float TempoScore()
        {
            return Mathf.Clamp01(_stepElapsedSeconds / Mathf.Max(0.1f, minimumTempoSeconds));
        }

        private float SymmetryFromHeight(RehabPoseSample sample)
        {
            return 1f - Mathf.Clamp01(Mathf.Abs(sample.leftHandPosition.y - sample.rightHandPosition.y) / 0.30f);
        }

        private Vector3 ToHeadLocal(RehabPoseSample sample, Vector3 worldPosition)
        {
            var yawOnly = Quaternion.Euler(0f, GetYaw(sample.headRotation), 0f);
            return Quaternion.Inverse(yawOnly) * (worldPosition - sample.headPosition);
        }

        private static float GetYaw(Quaternion rotation)
        {
            return rotation.eulerAngles.y;
        }

        private static BaduanjinStepEvaluation Invalid(string message)
        {
            return new BaduanjinStepEvaluation
            {
                poseValid = false,
                statusMessage = message,
                symmetry = 0f,
                tempo = 0f
            };
        }

        private static BaduanjinStepEvaluation Result(bool valid, string message, float symmetry, float tempo)
        {
            return new BaduanjinStepEvaluation
            {
                poseValid = valid,
                statusMessage = message,
                symmetry = Mathf.Clamp01(symmetry),
                tempo = Mathf.Clamp01(tempo)
            };
        }
    }
}
