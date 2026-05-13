using System;

namespace PicoElderCare.Rehab
{
    [Serializable]
    public class RehabMovementResult
    {
        public string movementId;
        public string movementName;
        public float duration;
        public float completion;
        public float symmetry;
        public float tempo;
        public int safetyWarningCount;
        public string timestamp;
        public bool skippedByTimeout;
    }
}
