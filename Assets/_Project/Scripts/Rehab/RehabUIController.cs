using TMPro;
using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class RehabUIController : MonoBehaviour
    {
        public TMP_Text movementNameText;
        public TMP_Text stepText;
        public TMP_Text remainingTimeText;
        public TMP_Text completionText;
        public TMP_Text safetyPromptText;
        public TMP_Text debugText;

        public void Refresh(
            RehabMovementEvaluation evaluation,
            RehabSafetyState safety,
            float sessionRemainingSeconds)
        {
            if (movementNameText != null)
            {
                movementNameText.text = evaluation.movementName;
            }

            if (stepText != null)
            {
                stepText.text = evaluation.stepInstruction;
            }

            if (remainingTimeText != null)
            {
                var remaining = Mathf.Max(0f, evaluation.remainingSeconds > 0f ? evaluation.remainingSeconds : sessionRemainingSeconds);
                remainingTimeText.text = string.Format("剩余 {0:00}:{1:00}", Mathf.FloorToInt(remaining / 60f), Mathf.FloorToInt(remaining % 60f));
            }

            if (completionText != null)
            {
                completionText.text = string.Format("完成度 {0:0}%", Mathf.Clamp01(evaluation.completion01) * 100f);
            }

            if (safetyPromptText != null)
            {
                safetyPromptText.text = safety.isPaused ? "请回到训练圈内，动作已暂停" : "保持舒适幅度，避免用力过猛";
            }

            if (debugText != null)
            {
                debugText.text = string.Format(
                    "动作 {0}/{1} | 步骤 {2}/{3} | 保持 {4:0.0}s | 距中心 {5:0.00}m",
                    evaluation.movementIndex + 1,
                    Mathf.Max(1, evaluation.movementCount),
                    evaluation.stepIndex + 1,
                    Mathf.Max(1, evaluation.stepCount),
                    evaluation.currentHoldSeconds,
                    safety.headDistanceFromCenterMeters);
            }
        }

        public void SetIdle(string movementName, string stepInstruction, float sessionRemainingSeconds)
        {
            if (movementNameText != null) movementNameText.text = movementName;
            if (stepText != null) stepText.text = stepInstruction;
            if (remainingTimeText != null)
            {
                remainingTimeText.text = string.Format("剩余 {0:00}:{1:00}", Mathf.FloorToInt(sessionRemainingSeconds / 60f), Mathf.FloorToInt(sessionRemainingSeconds % 60f));
            }

            if (completionText != null) completionText.text = "完成度 0%";
            if (safetyPromptText != null) safetyPromptText.text = "保持舒适幅度，准备开始";
        }
    }
}
