using UnityEngine;

namespace PicoElderCare.Rehab
{
    public struct RehabSafetyState
    {
        public bool isPaused;
        public float headDistanceFromCenterMeters;
        public int pauseCount;
        public float maxHeadDistanceFromCenterMeters;
    }

    public class SafetyMonitor : MonoBehaviour
    {
        public Transform hmdTransform;
        public float pauseDistanceMeters = 1.2f;
        public float resumeDistanceMeters = 1.1f;

        private bool _isPaused;
        private int _pauseCount;
        private float _maxHeadDistanceFromCenterMeters;

        public bool IsPaused
        {
            get { return _isPaused; }
        }

        public int PauseCount
        {
            get { return _pauseCount; }
        }

        public float MaxHeadDistanceFromCenterMeters
        {
            get { return _maxHeadDistanceFromCenterMeters; }
        }

        public void ResetMonitor()
        {
            _isPaused = false;
            _pauseCount = 0;
            _maxHeadDistanceFromCenterMeters = 0f;
        }

        public RehabSafetyState Evaluate(Vector3 headPosition, Vector3 trainingCenter, bool sessionActive)
        {
            var distance = CalculateHorizontalDistance(headPosition, trainingCenter);
            if (distance > _maxHeadDistanceFromCenterMeters)
            {
                _maxHeadDistanceFromCenterMeters = distance;
            }

            if (sessionActive)
            {
                if (_isPaused)
                {
                    if (distance <= resumeDistanceMeters)
                    {
                        _isPaused = false;
                    }
                }
                else if (distance > pauseDistanceMeters)
                {
                    _isPaused = true;
                    _pauseCount++;
                }
            }

            return new RehabSafetyState
            {
                isPaused = _isPaused,
                headDistanceFromCenterMeters = distance,
                pauseCount = _pauseCount,
                maxHeadDistanceFromCenterMeters = _maxHeadDistanceFromCenterMeters
            };
        }

        public RehabSafetyState Evaluate(Vector3 trainingCenter, bool sessionActive)
        {
            if (hmdTransform == null)
            {
                return new RehabSafetyState
                {
                    isPaused = _isPaused,
                    pauseCount = _pauseCount,
                    maxHeadDistanceFromCenterMeters = _maxHeadDistanceFromCenterMeters
                };
            }

            return Evaluate(hmdTransform.position, trainingCenter, sessionActive);
        }

        public static float CalculateHorizontalDistance(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
