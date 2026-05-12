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

        private float _currentHoldSeconds;
        private float _bestHoldSeconds;
        private float _completionTimeSeconds = -1f;
        private bool _completed;

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

        public void ResetEvaluation()
        {
            _currentHoldSeconds = 0f;
            _bestHoldSeconds = 0f;
            _completionTimeSeconds = -1f;
            _completed = false;
        }

        public RehabMovementEvaluation Evaluate(RehabPoseSample sample, float deltaTime, bool paused, float elapsedSessionSeconds)
        {
            if (_completed)
            {
                return CreateSnapshot(true, "动作完成");
            }

            if (movementId != RehabMovementId.Baduanjin_TwoHandsLiftHeaven)
            {
                _currentHoldSeconds = 0f;
                return CreateSnapshot(false, "当前动作暂未实现");
            }

            if (!sample.IsValid)
            {
                _currentHoldSeconds = 0f;
                return CreateSnapshot(false, "等待 XR 追踪");
            }

            var poseValid = IsTwoHandsLiftHeavenPoseValid(
                sample,
                handsAboveHeadMeters,
                maximumHandHeightDifferenceMeters);

            if (!paused)
            {
                if (poseValid)
                {
                    _currentHoldSeconds += Mathf.Max(0f, deltaTime);
                    if (_currentHoldSeconds > _bestHoldSeconds)
                    {
                        _bestHoldSeconds = _currentHoldSeconds;
                    }

                    if (_currentHoldSeconds >= minimumHoldSeconds && _currentHoldSeconds <= maximumHoldSeconds)
                    {
                        _completed = true;
                        _completionTimeSeconds = elapsedSessionSeconds;
                    }
                }
                else
                {
                    _currentHoldSeconds = 0f;
                }
            }

            var message = _completed
                ? "动作完成"
                : poseValid
                    ? string.Format("保持动作 {0:0.0}/{1:0.0}s", _currentHoldSeconds, minimumHoldSeconds)
                    : "请双手托举至头顶上方，并保持左右手齐平";

            return CreateSnapshot(poseValid, message);
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

        private RehabMovementEvaluation CreateSnapshot(bool poseValid, string message)
        {
            return new RehabMovementEvaluation
            {
                poseValid = poseValid,
                completed = _completed,
                currentHoldSeconds = _currentHoldSeconds,
                bestHoldSeconds = _bestHoldSeconds,
                completionTimeSeconds = _completionTimeSeconds,
                statusMessage = message
            };
        }
    }
}
