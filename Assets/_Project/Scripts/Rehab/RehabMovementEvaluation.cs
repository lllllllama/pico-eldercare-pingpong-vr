namespace PicoElderCare.Rehab
{
    public struct RehabMovementEvaluation
    {
        public bool poseValid;
        public bool completed;
        public bool stepCompleted;
        public bool stepTimedOut;
        public float currentHoldSeconds;
        public float bestHoldSeconds;
        public float completionTimeSeconds;
        public float remainingSeconds;
        public float completion01;
        public float symmetry;
        public float tempo;
        public int movementIndex;
        public int movementCount;
        public int stepIndex;
        public int stepCount;
        public string movementName;
        public string stepInstruction;
        public string statusMessage;
    }
}
