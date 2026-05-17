using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PicoElderCare.Rehab
{
    public enum CoachPlaybackState
    {
        Idle,
        Demonstration,
        PracticeLoop
    }

    public class VirtualCoachController : MonoBehaviour
    {
        public Transform userHeadTransform;
        public Transform coachRoot;
        public Transform modelRoot;
        public Animator animator;
        public CoachAnimationBinder animationBinder;

        public CoachPlaybackState defaultMovementPlaybackState = CoachPlaybackState.Demonstration;
        public CoachPlaybackState playbackState = CoachPlaybackState.Idle;

        public float preferredDistanceMeters = 2f;
        public float minDistanceMeters = 1.8f;
        public float maxDistanceMeters = 2.2f;
        public float floorY = 0f;
        public float faceTurnSpeedDegreesPerSecond = 540f;
        public bool keepInFrontOfUser = true;
        public bool placeInFrontOnStart = true;
        public bool useComfortFollow = true;
        public float followYawThresholdDegrees = 35f;
        public float followPositionThresholdMeters = 0.8f;
        public float followSmoothTime = 0.35f;
        public float maxFollowSpeedMetersPerSecond = 1.25f;
        public float followRotationSlerpSpeed = 4f;
        public bool faceUser = true;

        public GameObject placeholderCue;
        public bool autoCreatePlaceholderCue = true;
        public float placeholderHeightMeters = 1.35f;
        public float placeholderBobMeters = 0.06f;
        public float placeholderBobSpeed = 2f;

        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;
        private AnimationClip _activeClip;
        private MovementDefinition _activeMovement;
        private TextMesh _placeholderLabel;
        private Vector3 _placeholderBaseLocalPosition;
        private Vector3 _followVelocity;
        private Vector3 _followTargetPosition;
        private Quaternion _followTargetRotation;
        private bool _hasFollowTarget;
        private bool _placedOnce;

        private void Awake()
        {
            ResolveReferences();
            EnsurePlaceholderCue();
            SetIdle();
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsurePlaceholderCue();
            _hasFollowTarget = false;
        }

        private void Start()
        {
            if (keepInFrontOfUser && placeInFrontOnStart)
            {
                PlaceCoachInFrontOfUser(true);
            }
        }

        private void LateUpdate()
        {
            if (keepInFrontOfUser)
            {
                PlaceCoachInFrontOfUser();
            }

            if (faceUser)
            {
                FaceUser();
            }

            UpdateClipPlayback();
            UpdatePlaceholderCue();
        }

        private void OnDisable()
        {
            StopPlayableGraph();
        }

        private void OnDestroy()
        {
            StopPlayableGraph();
        }

        public void PlayMovement(MovementDefinition movement)
        {
            PlayMovement(movement, defaultMovementPlaybackState);
        }

        public void PlayMovement(MovementDefinition movement, CoachPlaybackState state)
        {
            ResolveReferences();
            EnsurePlaceholderCue();
            if (keepInFrontOfUser)
            {
                PlaceCoachInFrontOfUser(true);
            }

            _activeMovement = movement;
            if (movement == null)
            {
                SetIdle();
                return;
            }

            if (animationBinder != null && animationBinder.TryGetClip(movement, out var clip))
            {
                if (PlayClip(clip, state))
                {
                    SetPlaceholderVisible(false, string.Empty);
                    return;
                }

                playbackState = state;
                SetPlaceholderVisible(true, movement.movementName + "\n\u7f3a\u5c11 Animator");
                return;
            }

            playbackState = state;
            StopPlayableGraph();
            SetPlaceholderVisible(true, movement.movementName + "\n\u6682\u65e0\u6559\u7ec3\u52a8\u753b");
        }

        public void SetPlaybackState(CoachPlaybackState state)
        {
            if (state == CoachPlaybackState.Idle)
            {
                SetIdle();
                return;
            }

            PlayMovement(_activeMovement, state);
        }

        public void SetIdle()
        {
            playbackState = CoachPlaybackState.Idle;
            _activeMovement = null;

            if (animationBinder != null && animationBinder.idleClip != null && PlayClip(animationBinder.idleClip, CoachPlaybackState.Idle))
            {
                SetPlaceholderVisible(false, string.Empty);
                return;
            }

            StopPlayableGraph();
            SetPlaceholderVisible(false, string.Empty);
        }

        private void ResolveReferences()
        {
            if (coachRoot == null) coachRoot = transform;
            if (animationBinder == null) animationBinder = GetComponent<CoachAnimationBinder>();
            if (animator == null)
            {
                animator = modelRoot != null
                    ? modelRoot.GetComponentInChildren<Animator>(true)
                    : GetComponentInChildren<Animator>(true);
            }

            if (userHeadTransform == null && Camera.main != null)
            {
                userHeadTransform = Camera.main.transform;
            }
        }

        private bool PlayClip(AnimationClip clip, CoachPlaybackState state)
        {
            if (clip == null || animator == null) return false;

            StopPlayableGraph();
            _activeClip = clip;
            playbackState = state;

            _graph = PlayableGraph.Create("VirtualCoach_" + clip.name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
            _clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            _clipPlayable.SetTime(0d);
            _clipPlayable.SetSpeed(1d);
            output.SetSourcePlayable(_clipPlayable);
            _graph.Play();
            return true;
        }

        private void StopPlayableGraph()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            _activeClip = null;
        }

        private void UpdateClipPlayback()
        {
            if (_activeClip == null || !_graph.IsValid() || !_clipPlayable.IsValid()) return;

            var length = Mathf.Max(0.01f, _activeClip.length);
            var currentTime = _clipPlayable.GetTime();
            if (currentTime < length) return;

            if (playbackState == CoachPlaybackState.Demonstration)
            {
                SetIdle();
                return;
            }

            if (playbackState == CoachPlaybackState.PracticeLoop || playbackState == CoachPlaybackState.Idle)
            {
                _clipPlayable.SetTime(currentTime % length);
                _clipPlayable.SetDone(false);
            }
        }

        private void PlaceCoachInFrontOfUser()
        {
            PlaceCoachInFrontOfUser(false);
        }

        private void PlaceCoachInFrontOfUser(bool force)
        {
            if (coachRoot == null || userHeadTransform == null) return;

            var desiredPosition = GetDesiredCoachPosition(out var desiredRotation);
            if (!useComfortFollow || force || !_placedOnce)
            {
                coachRoot.position = desiredPosition;
                coachRoot.rotation = desiredRotation;
                _followTargetPosition = desiredPosition;
                _followTargetRotation = desiredRotation;
                _followVelocity = Vector3.zero;
                _hasFollowTarget = false;
                _placedOnce = true;
                return;
            }

            if (!_hasFollowTarget && ShouldRefreshFollowTarget(desiredPosition))
            {
                _followTargetPosition = desiredPosition;
                _followTargetRotation = desiredRotation;
                _hasFollowTarget = true;
            }

            if (!_hasFollowTarget) return;

            coachRoot.position = Vector3.SmoothDamp(
                coachRoot.position,
                _followTargetPosition,
                ref _followVelocity,
                Mathf.Max(0.01f, followSmoothTime),
                Mathf.Max(0.01f, maxFollowSpeedMetersPerSecond));

            coachRoot.rotation = Quaternion.Slerp(
                coachRoot.rotation,
                _followTargetRotation,
                Mathf.Max(0.01f, followRotationSlerpSpeed) * Time.deltaTime);

            if (Vector3.Distance(coachRoot.position, _followTargetPosition) < 0.02f &&
                Quaternion.Angle(coachRoot.rotation, _followTargetRotation) < 1f)
            {
                _hasFollowTarget = false;
                _followVelocity = Vector3.zero;
            }
        }

        private Vector3 GetDesiredCoachPosition(out Quaternion desiredRotation)
        {
            var forward = GetUserYawForward();
            var distance = Mathf.Clamp(preferredDistanceMeters, minDistanceMeters, maxDistanceMeters);
            var targetPosition = userHeadTransform.position + forward * distance;
            targetPosition.y = floorY;

            var toUser = userHeadTransform.position - targetPosition;
            toUser.y = 0f;
            desiredRotation = toUser.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(toUser.normalized, Vector3.up)
                : coachRoot.rotation;

            return targetPosition;
        }

        private bool ShouldRefreshFollowTarget(Vector3 desiredPosition)
        {
            var desiredDirection = GetUserYawForward();
            var currentDirection = Vector3.ProjectOnPlane(coachRoot.position - userHeadTransform.position, Vector3.up);
            if (currentDirection.sqrMagnitude < 0.0001f)
            {
                currentDirection = desiredDirection;
            }

            currentDirection.Normalize();
            var yawDelta = Vector3.Angle(currentDirection, desiredDirection);
            var positionDelta = Vector3.Distance(coachRoot.position, desiredPosition);
            return yawDelta > Mathf.Max(0f, followYawThresholdDegrees) ||
                   positionDelta > Mathf.Max(0f, followPositionThresholdMeters);
        }

        private Vector3 GetUserYawForward()
        {
            var forward = Vector3.ProjectOnPlane(userHeadTransform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }

        private void FaceUser()
        {
            if (coachRoot == null || userHeadTransform == null) return;

            var toUser = userHeadTransform.position - coachRoot.position;
            toUser.y = 0f;
            if (toUser.sqrMagnitude < 0.0001f) return;

            var targetRotation = Quaternion.LookRotation(toUser.normalized, Vector3.up);
            coachRoot.rotation = Quaternion.RotateTowards(
                coachRoot.rotation,
                targetRotation,
                faceTurnSpeedDegreesPerSecond * Time.deltaTime);
        }

        private void EnsurePlaceholderCue()
        {
            if (!autoCreatePlaceholderCue || placeholderCue != null || coachRoot == null) return;

            placeholderCue = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            placeholderCue.name = "CoachPlaceholderCue";
            placeholderCue.transform.SetParent(coachRoot, false);
            placeholderCue.transform.localPosition = new Vector3(0f, placeholderHeightMeters, 0f);
            placeholderCue.transform.localScale = Vector3.one * 0.16f;

            var collider = placeholderCue.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = placeholderCue.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                             Shader.Find("Sprites/Default") ??
                             Shader.Find("Unlit/Color") ??
                             Shader.Find("Standard");
                if (shader != null)
                {
                    renderer.material = new Material(shader)
                    {
                        color = new Color(0.1f, 0.75f, 1f, 0.9f)
                    };
                }
            }

            var labelObject = new GameObject("CoachPlaceholderLabel");
            labelObject.transform.SetParent(placeholderCue.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one * 0.02f;
            _placeholderLabel = labelObject.AddComponent<TextMesh>();
            _placeholderLabel.anchor = TextAnchor.MiddleCenter;
            _placeholderLabel.alignment = TextAlignment.Center;
            _placeholderLabel.fontSize = 42;
            _placeholderLabel.color = Color.white;

            _placeholderBaseLocalPosition = placeholderCue.transform.localPosition;
            placeholderCue.SetActive(false);
        }

        private void SetPlaceholderVisible(bool visible, string message)
        {
            if (placeholderCue == null) return;

            placeholderCue.SetActive(visible);
            if (!visible) return;

            if (_placeholderLabel == null)
            {
                _placeholderLabel = placeholderCue.GetComponentInChildren<TextMesh>(true);
            }

            if (_placeholderLabel != null)
            {
                _placeholderLabel.text = message;
            }

            _placeholderBaseLocalPosition = new Vector3(0f, placeholderHeightMeters, 0f);
            placeholderCue.transform.localPosition = _placeholderBaseLocalPosition;
        }

        private void UpdatePlaceholderCue()
        {
            if (placeholderCue == null || !placeholderCue.activeSelf) return;

            var offset = Mathf.Sin(Time.time * placeholderBobSpeed) * placeholderBobMeters;
            placeholderCue.transform.localPosition = _placeholderBaseLocalPosition + Vector3.up * offset;

            if (_placeholderLabel != null && userHeadTransform != null)
            {
                var toUser = userHeadTransform.position - _placeholderLabel.transform.position;
                if (toUser.sqrMagnitude > 0.0001f)
                {
                    _placeholderLabel.transform.rotation = Quaternion.LookRotation(-toUser.normalized, Vector3.up);
                }
            }
        }
    }
}
