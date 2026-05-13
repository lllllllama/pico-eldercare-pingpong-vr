using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace PicoElderCare.Rehab
{
    public class RehabTrainingAreaDragHandle : MonoBehaviour
    {
        public RehabSessionManager sessionManager;
        public Transform trainingAreaRoot;
        public Transform controllerTransform;
        public Transform hmdTransform;
        public XRNode controllerNode = XRNode.LeftHand;
        public float activationRadiusMeters = 0.95f;
        public float maxRayDistanceMeters = 4.5f;
        public float floorY = 0f;

        private readonly List<InputDevice> _devices = new List<InputDevice>();
        private bool _dragging;
        private bool _wasGripPressed;

        public bool IsDragging
        {
            get { return _dragging; }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            if (trainingAreaRoot == null || controllerTransform == null) return;

            var gripPressed = IsGripPressed();
            if (!gripPressed)
            {
                _dragging = false;
                _wasGripPressed = false;
                return;
            }

            if (!_dragging && !_wasGripPressed && TryGetControllerFloorPoint(out var startPoint))
            {
                var distanceToCenter = Vector2.Distance(
                    new Vector2(startPoint.x, startPoint.z),
                    new Vector2(trainingAreaRoot.position.x, trainingAreaRoot.position.z));
                if (distanceToCenter <= activationRadiusMeters)
                {
                    _dragging = true;
                }
            }

            if (_dragging && TryGetControllerFloorPoint(out var floorPoint))
            {
                MoveTrainingArea(floorPoint);
            }

            _wasGripPressed = gripPressed;
        }

        private void MoveTrainingArea(Vector3 floorPoint)
        {
            var center = new Vector3(floorPoint.x, floorY, floorPoint.z);
            var headPosition = hmdTransform != null ? hmdTransform.position : center - Vector3.forward;
            var forward = center - headPosition;
            forward.y = 0f;

            if (sessionManager != null)
            {
                sessionManager.SetTrainingAreaCenter(center, forward, headPosition);
                return;
            }

            trainingAreaRoot.position = center;
        }

        private bool TryGetControllerFloorPoint(out Vector3 point)
        {
            point = Vector3.zero;
            var ray = new Ray(controllerTransform.position, controllerTransform.forward);
            var floorPlane = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
            if (!floorPlane.Raycast(ray, out var distance)) return false;
            if (distance < 0f || distance > maxRayDistanceMeters) return false;

            point = ray.GetPoint(distance);
            return true;
        }

        private bool IsGripPressed()
        {
            InputDevices.GetDevicesAtXRNode(controllerNode, _devices);
            foreach (var device in _devices)
            {
                if (device.TryGetFeatureValue(CommonUsages.gripButton, out var gripButton) && gripButton)
                {
                    return true;
                }

                if (device.TryGetFeatureValue(CommonUsages.grip, out var gripValue) && gripValue > 0.55f)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (sessionManager == null)
            {
                sessionManager = FindObjectOfType<RehabSessionManager>(true);
            }

            if (trainingAreaRoot == null)
            {
                var area = GameObject.Find("TrainingArea");
                if (area != null)
                {
                    trainingAreaRoot = area.transform;
                }
            }

            if (controllerTransform == null)
            {
                controllerTransform = FindTransformByName("Left Controller");
            }

            if (hmdTransform == null)
            {
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
        }

        private static Transform FindTransformByName(string objectName)
        {
            var transforms = FindObjectsOfType<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == objectName)
                {
                    return transforms[i];
                }
            }

            return null;
        }
    }
}
