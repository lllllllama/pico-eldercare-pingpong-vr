using System;

namespace PicoElderCare.Rehab
{
    [Serializable]
    public class MovementDefinition
    {
        public RehabMovementId movementId;
        public string movementName;
        public string description;
        public MovementStepDefinition[] steps;

        public MovementDefinition()
        {
        }

        public MovementDefinition(
            RehabMovementId movementId,
            string movementName,
            string description,
            params MovementStepDefinition[] steps)
        {
            this.movementId = movementId;
            this.movementName = movementName;
            this.description = description;
            this.steps = steps;
        }

        public int StepCount
        {
            get { return steps != null ? steps.Length : 0; }
        }

        public MovementStepDefinition GetStep(int index)
        {
            if (steps == null || steps.Length == 0) return null;
            return steps[index < 0 ? 0 : index >= steps.Length ? steps.Length - 1 : index];
        }
    }
}
