namespace PicoElderCare.Rehab
{
    public struct RehabMovementEvaluation
    {
        public bool poseValid;
        public bool completed;
        public float currentHoldSeconds;
        public float bestHoldSeconds;
        public float completionTimeSeconds;
        public string statusMessage;
    }
}
