using System;

namespace PicoElderCare.Rehab
{
    public enum RehabSessionEndReason
    {
        None,
        Completed,
        TimeLimit,
        Stopped
    }

    [Serializable]
    public class RehabTrainingResult
    {
        public string sessionId;
        public string movementId;
        public string movementName;
        public string startedUtc;
        public string endedUtc;
        public float configuredDurationSeconds;
        public float elapsedSeconds;
        public bool completed;
        public float completionTimeSeconds;
        public float bestHoldSeconds;
        public int pauseCount;
        public string endReason;
        public float maxHeadDistanceFromCenterMeters;

        public static RehabTrainingResult CreateStarted(
            RehabMovementId movement,
            string displayName,
            float configuredDurationSeconds)
        {
            return new RehabTrainingResult
            {
                sessionId = Guid.NewGuid().ToString("N"),
                movementId = movement.ToString(),
                movementName = displayName,
                startedUtc = DateTime.UtcNow.ToString("o"),
                configuredDurationSeconds = configuredDurationSeconds,
                completionTimeSeconds = -1f,
                endReason = RehabSessionEndReason.None.ToString()
            };
        }

        public void Finish(
            RehabSessionEndReason reason,
            bool isCompleted,
            float elapsed,
            float completionTime,
            float bestHold,
            int pauses,
            float maxHeadDistance)
        {
            endedUtc = DateTime.UtcNow.ToString("o");
            endReason = reason.ToString();
            completed = isCompleted;
            elapsedSeconds = elapsed;
            completionTimeSeconds = completionTime;
            bestHoldSeconds = bestHold;
            pauseCount = pauses;
            maxHeadDistanceFromCenterMeters = maxHeadDistance;
        }
    }
}
