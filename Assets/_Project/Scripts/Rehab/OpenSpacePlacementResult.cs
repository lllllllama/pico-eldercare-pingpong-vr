using UnityEngine;

namespace PicoElderCare.Rehab
{
    public struct OpenSpacePlacementResult
    {
        public bool foundClearSpace;
        public bool usedFallback;
        public Vector3 center;
        public Vector3 forward;
        public float floorY;
        public float score;
        public int candidatesTested;
        public int rejectedCandidates;
        public string message;
    }
}
