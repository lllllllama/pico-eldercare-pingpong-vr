using System;

namespace PicoElderCare.Rehab
{
    [Serializable]
    public class MovementStepDefinition
    {
        public string stepName;
        public string instruction;
        public float requiredHoldSeconds = 1.5f;
        public float timeoutSeconds = 25f;

        public MovementStepDefinition()
        {
        }

        public MovementStepDefinition(string stepName, string instruction, float requiredHoldSeconds, float timeoutSeconds)
        {
            this.stepName = stepName;
            this.instruction = instruction;
            this.requiredHoldSeconds = requiredHoldSeconds;
            this.timeoutSeconds = timeoutSeconds;
        }
    }
}
