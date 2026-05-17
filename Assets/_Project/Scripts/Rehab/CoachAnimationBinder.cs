using System;
using UnityEngine;

namespace PicoElderCare.Rehab
{
    [Serializable]
    public class CoachAnimationBinding
    {
        public RehabMovementId movementId;
        public string movementName;
        public AnimationClip animationClip;

        public bool Matches(MovementDefinition movement)
        {
            if (movement == null) return false;

            var hasName = !string.IsNullOrWhiteSpace(movementName);
            var nameMatches = hasName &&
                              string.Equals(movementName, movement.movementName, StringComparison.OrdinalIgnoreCase);

            if (movement.movementId == movementId && (!hasName || nameMatches))
            {
                return true;
            }

            return nameMatches;
        }
    }

    public class CoachAnimationBinder : MonoBehaviour
    {
        public AnimationClip idleClip;
        public CoachAnimationBinding[] bindings;

        public bool TryGetClip(MovementDefinition movement, out AnimationClip clip)
        {
            clip = null;
            if (movement == null || bindings == null) return false;

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || binding.animationClip == null) continue;
                if (!binding.Matches(movement)) continue;

                clip = binding.animationClip;
                return true;
            }

            return false;
        }

        public AnimationClip GetClip(MovementDefinition movement)
        {
            return TryGetClip(movement, out var clip) ? clip : null;
        }

        [ContextMenu("Create Default Movement Bindings")]
        private void CreateDefaultMovementBindings()
        {
            SetDefaultMovementBindings();
        }

        public void SetDefaultMovementBindings()
        {
            var baduanjinMovements = BaduanjinEvaluator.CreateDefaultMovements();
            var taiChiMovements = TaiChiEvaluator.CreateDefaultMovements();
            bindings = new CoachAnimationBinding[baduanjinMovements.Length + taiChiMovements.Length];

            var index = 0;
            AddBindings(baduanjinMovements, ref index);
            AddBindings(taiChiMovements, ref index);
        }

        private void AddBindings(MovementDefinition[] movements, ref int index)
        {
            if (movements == null) return;

            for (var i = 0; i < movements.Length; i++)
            {
                var movement = movements[i];
                if (movement == null) continue;

                bindings[index] = new CoachAnimationBinding
                {
                    movementId = movement.movementId,
                    movementName = movement.movementName,
                    animationClip = null
                };
                index++;
            }
        }
    }
}
