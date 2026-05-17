using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class OpenSpaceCanvasPlacer : MonoBehaviour
    {
        public Transform hmdTransform;
        public Transform targetTransform;
        public bool useOpenSpacePlacement = true;
        public float desiredDistanceMeters = 2.2f;
        public float minDistanceMeters = 1.2f;
        public float maxDistanceMeters = 3.0f;
        public float clearanceRadiusMeters = 0.55f;
        public float clearanceHeightMeters = 1.55f;
        public float floorY = 0f;
        public float canvasHeightMeters = 1.45f;
        public float searchDurationSeconds = 8f;
        public float searchIntervalSeconds = 0.5f;
        public LayerMask obstacleMask = ~0;
        public bool placeOnStart = true;
        public bool placeOnEnable;
        public bool repeatPlacementDuringSearch;
        public bool useHeadYawOnly = true;
        public bool useHmdHeightOffset = true;
        public float hmdHeightOffsetMeters = -0.1f;

        private float _searchUntilTime;
        private float _nextSearchTime;
        private bool _placedOnce;

        private void Awake()
        {
            if (targetTransform == null)
            {
                targetTransform = transform;
            }

            ResolveReferences();
        }

        private void OnEnable()
        {
            _searchUntilTime = Time.time + Mathf.Max(0f, searchDurationSeconds);
            _nextSearchTime = 0f;
            _placedOnce = false;

            if (placeOnEnable)
            {
                PlaceCanvas();
            }
        }

        private void Start()
        {
            if (placeOnStart)
            {
                PlaceCanvas();
            }
        }

        private void Update()
        {
            if (!repeatPlacementDuringSearch) return;
            if (!useOpenSpacePlacement) return;
            if (Time.time > _searchUntilTime && _placedOnce) return;
            if (Time.time < _nextSearchTime) return;

            PlaceCanvas();
            _nextSearchTime = Time.time + Mathf.Max(0.1f, searchIntervalSeconds);
        }

        public void PlaceCanvas()
        {
            ResolveReferences();
            if (targetTransform == null || hmdTransform == null) return;
            if (!useOpenSpacePlacement) return;

            var result = OpenSpacePlacementSolver.FindBestPlacement(
                hmdTransform.position,
                useHeadYawOnly ? Quaternion.LookRotation(GetHeadYawForward(), Vector3.up) : hmdTransform.rotation,
                floorY,
                desiredDistanceMeters,
                minDistanceMeters,
                maxDistanceMeters,
                clearanceRadiusMeters,
                clearanceHeightMeters,
                obstacleMask);

            targetTransform.position = result.center + Vector3.up * canvasHeightMeters;
            if (useHmdHeightOffset)
            {
                var position = targetTransform.position;
                position.y = hmdTransform.position.y + hmdHeightOffsetMeters;
                targetTransform.position = position;
            }

            var toCanvas = targetTransform.position - hmdTransform.position;
            toCanvas.y = 0f;
            if (toCanvas.sqrMagnitude < 0.0001f)
            {
                toCanvas = result.forward;
            }

            targetTransform.rotation = Quaternion.LookRotation(toCanvas.normalized, Vector3.up);
            _placedOnce = true;
        }

        private void ResolveReferences()
        {
            if (hmdTransform != null) return;

            var camera = Camera.main;
            if (camera == null)
            {
                camera = FindObjectOfType<Camera>(true);
            }

            if (camera != null)
            {
                hmdTransform = camera.transform;
            }
        }

        private Vector3 GetHeadYawForward()
        {
            var forward = hmdTransform != null
                ? Vector3.ProjectOnPlane(hmdTransform.forward, Vector3.up)
                : Vector3.forward;

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }
    }
}
