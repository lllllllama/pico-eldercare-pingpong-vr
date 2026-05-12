using UnityEngine;

namespace PicoElderCare.Rehab
{
    public struct RehabPoseSample
    {
        public Vector3 headPosition;
        public Quaternion headRotation;
        public Vector3 leftHandPosition;
        public Quaternion leftHandRotation;
        public Vector3 rightHandPosition;
        public Quaternion rightHandRotation;
        public bool hasHead;
        public bool hasLeftHand;
        public bool hasRightHand;

        public bool IsValid
        {
            get { return hasHead && hasLeftHand && hasRightHand; }
        }
    }
}
