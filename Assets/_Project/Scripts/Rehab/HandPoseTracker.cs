using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class HandPoseTracker : MonoBehaviour
    {
        public Transform hmdTransform;
        public Transform leftControllerTransform;
        public Transform rightControllerTransform;
        public bool autoResolveReferences = true;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
        }

        public RehabPoseSample GetCurrentSample()
        {
            if (autoResolveReferences &&
                (hmdTransform == null || leftControllerTransform == null || rightControllerTransform == null))
            {
                ResolveReferences();
            }

            var sample = new RehabPoseSample();
            if (hmdTransform != null)
            {
                sample.hasHead = true;
                sample.headPosition = hmdTransform.position;
                sample.headRotation = hmdTransform.rotation;
            }

            if (leftControllerTransform != null)
            {
                sample.hasLeftHand = true;
                sample.leftHandPosition = leftControllerTransform.position;
                sample.leftHandRotation = leftControllerTransform.rotation;
            }

            if (rightControllerTransform != null)
            {
                sample.hasRightHand = true;
                sample.rightHandPosition = rightControllerTransform.position;
                sample.rightHandRotation = rightControllerTransform.rotation;
            }

            return sample;
        }

        public void ResolveReferences()
        {
            if (!autoResolveReferences) return;

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

            if (leftControllerTransform == null)
            {
                leftControllerTransform = FindTransformByName("Left Controller");
            }

            if (rightControllerTransform == null)
            {
                rightControllerTransform = FindTransformByName("Right Controller");
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
