using UnityEngine;

namespace PicoElderCare.Rehab
{
    public struct BaduanjinStepEvaluation
    {
        public bool poseValid;
        public string statusMessage;
        public float symmetry;
        public float tempo;
    }

    public class BaduanjinEvaluator : MonoBehaviour
    {
        public float handsAboveHeadMeters = 0.15f;
        public float maximumHandHeightDifferenceMeters = 0.18f;
        public float bowHandLateralMeters = 0.42f;
        public float singleRaiseHighMeters = 0.12f;
        public float singleRaiseLowMeters = 0.35f;
        public float lookBackYawDegrees = 22f;
        public float maximumLookBackYawSpeed = 120f;
        public float gentleSwayMeters = 0.12f;
        public float reachDownBelowHeadMeters = 0.70f;
        public float punchForwardMeters = 0.36f;
        public float heelRaiseHeadRiseMeters = 0.04f;

        private RehabMovementId _activeMovementId;
        private bool _hasBaseline;
        private Vector3 _baselineHeadPosition;
        private float _baselineYaw;
        private float _lastYaw;
        private float _lastTempo = 1f;
        private bool _lookBackOverspeed;

        public void ResetForMovement(RehabMovementId movementId, RehabPoseSample sample)
        {
            _activeMovementId = movementId;
            _hasBaseline = sample.hasHead;
            _baselineHeadPosition = sample.hasHead ? sample.headPosition : Vector3.zero;
            _baselineYaw = sample.hasHead ? GetYaw(sample.headRotation) : 0f;
            _lastYaw = _baselineYaw;
            _lastTempo = 1f;
            _lookBackOverspeed = false;
        }

        public BaduanjinStepEvaluation EvaluateStep(
            MovementDefinition movement,
            int stepIndex,
            RehabPoseSample sample,
            float deltaTime)
        {
            if (movement == null)
            {
                return Invalid("未配置动作");
            }

            if (!sample.IsValid)
            {
                return Invalid("等待头显和左右手柄追踪");
            }

            if (!_hasBaseline || _activeMovementId != movement.movementId)
            {
                ResetForMovement(movement.movementId, sample);
            }

            switch (movement.movementId)
            {
                case RehabMovementId.Baduanjin_TwoHandsLiftHeaven:
                    return EvaluateTwoHandsLiftHeaven(sample);
                case RehabMovementId.Baduanjin_DrawBowShootHawk:
                    return EvaluateDrawBow(sample, stepIndex);
                case RehabMovementId.Baduanjin_SingleRaiseRegulateSpleen:
                    return EvaluateSingleRaise(sample, stepIndex);
                case RehabMovementId.Baduanjin_LookBackRelieveStrain:
                    return EvaluateLookBack(sample, stepIndex, deltaTime);
                case RehabMovementId.Baduanjin_SwayHeadTailClearHeartFire:
                    return EvaluateGentleSway(sample, stepIndex);
                case RehabMovementId.Baduanjin_TouchKneesStrengthenKidneys:
                    return EvaluateReachDown(sample);
                case RehabMovementId.Baduanjin_ClenchFistsAngryEyes:
                    return EvaluateGentlePunch(sample);
                case RehabMovementId.Baduanjin_HeelRaiseFinish:
                    return EvaluateHeelRaiseOrSeatedFinish(sample);
                default:
                    return Invalid("当前动作尚未接入八段锦判定");
            }
        }

        public static MovementDefinition[] CreateDefaultMovements()
        {
            return new[]
            {
                new MovementDefinition(
                    RehabMovementId.Baduanjin_TwoHandsLiftHeaven,
                    "双手托天理三焦",
                    "双手缓慢上举到头顶上方，保持左右高度接近。",
                    new MovementStepDefinition("上举保持", "双手举至头顶上方，左右手保持齐平", 2f, 25f)),
                new MovementDefinition(
                    RehabMovementId.Baduanjin_DrawBowShootHawk,
                    "左右开弓似射雕",
                    "康复简化版：左右交替做温和开弓，不追求大幅拉伸。",
                    new MovementStepDefinition("向左开弓", "左手向左侧打开，右手停在胸前", 1.5f, 25f),
                    new MovementStepDefinition("向右开弓", "右手向右侧打开，左手停在胸前", 1.5f, 25f)),
                new MovementDefinition(
                    RehabMovementId.Baduanjin_SingleRaiseRegulateSpleen,
                    "调理脾胃须单举",
                    "左右手一上一下交替，动作幅度以舒适为准。",
                    new MovementStepDefinition("左手上举", "左手上举，右手自然下按", 1.5f, 25f),
                    new MovementStepDefinition("右手上举", "右手上举，左手自然下按", 1.5f, 25f)),
                new MovementDefinition(
                    RehabMovementId.Baduanjin_LookBackRelieveStrain,
                    "五劳七伤往后瞧",
                    "康复简化版：只做缓慢左右转头，不要求极限回头。",
                    new MovementStepDefinition("向左转头", "头部缓慢向左转，保持舒适角度", 1.2f, 25f),
                    new MovementStepDefinition("向右转头", "头部缓慢向右转，保持舒适角度", 1.2f, 25f)),
                new MovementDefinition(
                    RehabMovementId.Baduanjin_SwayHeadTailClearHeartFire,
                    "摇头摆尾去心火",
                    "康复简化版：坐姿或站姿轻微左右移动，不做大幅弯腰。",
                    new MovementStepDefinition("轻移向左", "身体重心轻微向左移动，保持头部稳定", 1.2f, 25f),
                    new MovementStepDefinition("轻移向右", "身体重心轻微向右移动，保持头部稳定", 1.2f, 25f)),
                new MovementDefinition(
                    RehabMovementId.Baduanjin_TouchKneesStrengthenKidneys,
                    "两手攀足固肾腰",
                    "康复简化版：双手向膝盖或小腿方向下探，不要求摸脚。",
                    new MovementStepDefinition("双手下探", "双手缓慢下降到膝盖或小腿方向", 1.5f, 30f)),
                new MovementDefinition(
                    RehabMovementId.Baduanjin_ClenchFistsAngryEyes,
                    "攒拳怒目增气力",
                    "双手在胸前温和向前出拳，避免快速用力。",
                    new MovementStepDefinition("双拳前推", "双手握拳姿态向前轻推，保持肩膀放松", 1.5f, 25f)),
                new MovementDefinition(
                    RehabMovementId.Baduanjin_HeelRaiseFinish,
                    "背后七颠百病消",
                    "康复简化版：轻微提踵或坐姿收尾，不做震脚。",
                    new MovementStepDefinition("轻提踵收尾", "轻微抬高身体或双手放松收尾，保持稳定", 1.5f, 25f))
            };
        }

        private BaduanjinStepEvaluation EvaluateTwoHandsLiftHeaven(RehabPoseSample sample)
        {
            var valid = MovementEvaluator.IsTwoHandsLiftHeavenPoseValid(
                sample,
                handsAboveHeadMeters,
                maximumHandHeightDifferenceMeters);
            var symmetry = 1f - Mathf.Clamp01(Mathf.Abs(sample.leftHandPosition.y - sample.rightHandPosition.y) / Mathf.Max(0.01f, maximumHandHeightDifferenceMeters));
            return Result(valid, "双手举至头顶上方，并保持左右齐平", symmetry, 1f);
        }

        private BaduanjinStepEvaluation EvaluateDrawBow(RehabPoseSample sample, int stepIndex)
        {
            var leftStep = stepIndex == 0;
            var left = ToHeadLocal(sample, sample.leftHandPosition);
            var right = ToHeadLocal(sample, sample.rightHandPosition);
            var chestMin = -0.55f;
            var chestMax = 0.10f;
            var openHand = leftStep ? left : right;
            var chestHand = leftStep ? right : left;
            var lateralValid = leftStep ? openHand.x <= -bowHandLateralMeters : openHand.x >= bowHandLateralMeters;
            var chestValid = Mathf.Abs(chestHand.x) <= 0.28f && chestHand.y >= chestMin && chestHand.y <= chestMax;
            var heightValid = openHand.y >= chestMin && openHand.y <= chestMax + 0.18f;
            var symmetry = 1f - Mathf.Clamp01(Mathf.Abs(Mathf.Abs(openHand.x) - Mathf.Abs(chestHand.x + (leftStep ? 0.12f : -0.12f))) / 0.8f);
            return Result(lateralValid && chestValid && heightValid, leftStep ? "左手向左打开，右手留在胸前" : "右手向右打开，左手留在胸前", symmetry, 1f);
        }

        private BaduanjinStepEvaluation EvaluateSingleRaise(RehabPoseSample sample, int stepIndex)
        {
            var leftStep = stepIndex == 0;
            var high = leftStep ? sample.leftHandPosition : sample.rightHandPosition;
            var low = leftStep ? sample.rightHandPosition : sample.leftHandPosition;
            var highValid = high.y >= sample.headPosition.y + singleRaiseHighMeters;
            var lowValid = low.y <= sample.headPosition.y - singleRaiseLowMeters;
            var symmetry = Mathf.Clamp01((high.y - low.y) / 1.0f);
            return Result(highValid && lowValid, leftStep ? "左手上举，右手下按" : "右手上举，左手下按", symmetry, 1f);
        }

        private BaduanjinStepEvaluation EvaluateLookBack(RehabPoseSample sample, int stepIndex, float deltaTime)
        {
            var yaw = GetYaw(sample.headRotation);
            var yawDelta = Mathf.DeltaAngle(_baselineYaw, yaw);
            var yawSpeed = deltaTime > 0f ? Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw)) / deltaTime : 0f;
            _lastYaw = yaw;
            _lastTempo = Mathf.Clamp01(1f - Mathf.Max(0f, yawSpeed - maximumLookBackYawSpeed) / maximumLookBackYawSpeed);
            if (yawSpeed > maximumLookBackYawSpeed && Mathf.Abs(yawDelta) > 5f)
            {
                _lookBackOverspeed = true;
            }
            else if (Mathf.Abs(yawDelta) < 5f)
            {
                _lookBackOverspeed = false;
            }

            var leftStep = stepIndex == 0;
            var reachedAngle = leftStep ? yawDelta <= -lookBackYawDegrees : yawDelta >= lookBackYawDegrees;
            var valid = reachedAngle && !_lookBackOverspeed;
            return Result(valid, leftStep ? "头部缓慢向左转到舒适角度" : "头部缓慢向右转到舒适角度", 1f, _lastTempo);
        }

        private BaduanjinStepEvaluation EvaluateGentleSway(RehabPoseSample sample, int stepIndex)
        {
            var localHead = ToBaselineLocal(sample.headPosition);
            var leftStep = stepIndex == 0;
            var lateralValid = leftStep ? localHead.x <= -gentleSwayMeters : localHead.x >= gentleSwayMeters;
            var heightStable = Mathf.Abs(sample.headPosition.y - _baselineHeadPosition.y) <= 0.25f;
            return Result(lateralValid && heightStable, leftStep ? "身体轻微向左移动，保持稳定" : "身体轻微向右移动，保持稳定", 1f, 1f);
        }

        private BaduanjinStepEvaluation EvaluateReachDown(RehabPoseSample sample)
        {
            var targetY = sample.headPosition.y - reachDownBelowHeadMeters;
            var leftValid = sample.leftHandPosition.y <= targetY;
            var rightValid = sample.rightHandPosition.y <= targetY;
            var symmetry = 1f - Mathf.Clamp01(Mathf.Abs(sample.leftHandPosition.y - sample.rightHandPosition.y) / 0.25f);
            return Result(leftValid && rightValid, "双手向膝盖或小腿方向缓慢下降", symmetry, 1f);
        }

        private BaduanjinStepEvaluation EvaluateGentlePunch(RehabPoseSample sample)
        {
            var left = ToHeadLocal(sample, sample.leftHandPosition);
            var right = ToHeadLocal(sample, sample.rightHandPosition);
            var leftValid = left.z >= punchForwardMeters && left.y >= -0.55f && left.y <= 0.05f;
            var rightValid = right.z >= punchForwardMeters && right.y >= -0.55f && right.y <= 0.05f;
            var symmetry = 1f - Mathf.Clamp01(Mathf.Abs(left.z - right.z) / 0.35f);
            return Result(leftValid && rightValid, "双手在胸前向前轻推，避免快速用力", symmetry, 1f);
        }

        private BaduanjinStepEvaluation EvaluateHeelRaiseOrSeatedFinish(RehabPoseSample sample)
        {
            var headRise = sample.headPosition.y - _baselineHeadPosition.y;
            var left = ToHeadLocal(sample, sample.leftHandPosition);
            var right = ToHeadLocal(sample, sample.rightHandPosition);
            var seatedFinish = left.y <= -0.35f && right.y <= -0.35f && Mathf.Abs(left.x) <= 0.55f && Mathf.Abs(right.x) <= 0.55f;
            var valid = headRise >= heelRaiseHeadRiseMeters || seatedFinish;
            return Result(valid, "轻微提踵，或坐姿下双手自然放松收尾", 1f, 1f);
        }

        private Vector3 ToHeadLocal(RehabPoseSample sample, Vector3 worldPosition)
        {
            var yawOnly = Quaternion.Euler(0f, GetYaw(sample.headRotation), 0f);
            return Quaternion.Inverse(yawOnly) * (worldPosition - sample.headPosition);
        }

        private Vector3 ToBaselineLocal(Vector3 worldPosition)
        {
            var yawOnly = Quaternion.Euler(0f, _baselineYaw, 0f);
            return Quaternion.Inverse(yawOnly) * (worldPosition - _baselineHeadPosition);
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
