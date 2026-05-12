using UnityEngine;

namespace PicoElderCare.Rehab
{
    public static class OpenSpacePlacementSolver
    {
        private const float FloorRaycastUpMeters = 1.6f;
        private const float FloorRaycastDownMeters = 3.2f;
        private const float MaximumFloorOffsetMeters = 0.35f;
        private const float MinimumFloorNormalY = 0.65f;
        private const float ObstacleBoxBottomClearanceMeters = 0.08f;

        private static readonly float[] AngleOffsets =
        {
            0f, 20f, -20f, 40f, -40f, 65f, -65f, 90f, -90f, 120f, -120f, 150f, -150f, 180f
        };

        private static readonly Collider[] OverlapHits = new Collider[64];
        private static readonly RaycastHit[] FloorHits = new RaycastHit[32];

        public static OpenSpacePlacementResult FindBestPlacement(
            Vector3 headPosition,
            Quaternion headRotation,
            float fallbackFloorY,
            float desiredDistanceMeters,
            float minDistanceMeters,
            float maxDistanceMeters,
            float clearanceRadiusMeters,
            float clearanceHeightMeters,
            LayerMask collisionMask,
            Transform[] ignoredRoots = null)
        {
            var forward = Vector3.ProjectOnPlane(headRotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            minDistanceMeters = Mathf.Max(0.25f, minDistanceMeters);
            maxDistanceMeters = Mathf.Max(minDistanceMeters, maxDistanceMeters);
            desiredDistanceMeters = Mathf.Clamp(desiredDistanceMeters, minDistanceMeters, maxDistanceMeters);
            clearanceRadiusMeters = Mathf.Max(0.1f, clearanceRadiusMeters);
            clearanceHeightMeters = Mathf.Max(0.5f, clearanceHeightMeters);

            // Spatial mesh/plane objects can be created or moved immediately before this scan.
            Physics.SyncTransforms();

            var result = CreateFallbackResult(headPosition, forward, fallbackFloorY, desiredDistanceMeters);
            var bestScore = float.NegativeInfinity;
            var tested = 0;
            var rejected = 0;

            for (var distance = minDistanceMeters; distance <= maxDistanceMeters + 0.001f; distance += 0.25f)
            {
                for (var i = 0; i < AngleOffsets.Length; i++)
                {
                    var angle = AngleOffsets[i];
                    var candidateForward = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                    candidateForward.Normalize();

                    var candidateCenter = headPosition + candidateForward * distance;
                    var floorY = fallbackFloorY;
                    TryResolveFloorY(candidateCenter, fallbackFloorY, collisionMask, out floorY);
                    candidateCenter.y = floorY;
                    tested++;

                    if (!IsClear(candidateCenter, floorY, clearanceRadiusMeters, clearanceHeightMeters, collisionMask, ignoredRoots))
                    {
                        rejected++;
                        continue;
                    }

                    var score = ScoreCandidate(candidateForward, forward, distance, desiredDistanceMeters, angle);
                    if (score <= bestScore) continue;

                    bestScore = score;
                    result = new OpenSpacePlacementResult
                    {
                        foundClearSpace = true,
                        usedFallback = false,
                        center = candidateCenter,
                        forward = candidateForward,
                        floorY = floorY,
                        score = score,
                        message = "已找到空旷训练区域"
                    };
                }
            }

            result.candidatesTested = tested;
            result.rejectedCandidates = rejected;
            return result;
        }

        public static bool IsClear(
            Vector3 center,
            float floorY,
            float clearanceRadiusMeters,
            float clearanceHeightMeters,
            LayerMask collisionMask,
            Transform[] ignoredRoots = null)
        {
            var halfExtents = new Vector3(
                clearanceRadiusMeters,
                clearanceHeightMeters * 0.5f,
                clearanceRadiusMeters);
            var boxCenter = new Vector3(
                center.x,
                floorY + ObstacleBoxBottomClearanceMeters + halfExtents.y,
                center.z);

            var hitCount = Physics.OverlapBoxNonAlloc(
                boxCenter,
                halfExtents,
                OverlapHits,
                Quaternion.identity,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hitCount; i++)
            {
                var hit = OverlapHits[i];
                OverlapHits[i] = null;
                if (hit == null) continue;
                if (IsIgnoredHit(hit, ignoredRoots)) continue;
                if (IsIgnorableFloorHit(hit, floorY)) continue;
                return false;
            }

            return true;
        }

        private static bool IsIgnoredHit(Collider hit, Transform[] ignoredRoots)
        {
            if (ignoredRoots == null || ignoredRoots.Length == 0) return false;

            var hitTransform = hit.transform;
            for (var i = 0; i < ignoredRoots.Length; i++)
            {
                var ignoredRoot = ignoredRoots[i];
                if (ignoredRoot == null) continue;
                if (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot)) return true;
            }

            return false;
        }

        private static bool TryResolveFloorY(Vector3 candidateCenter, float fallbackFloorY, LayerMask collisionMask, out float floorY)
        {
            floorY = fallbackFloorY;
            var origin = candidateCenter + Vector3.up * FloorRaycastUpMeters;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                FloorHits,
                FloorRaycastUpMeters + FloorRaycastDownMeters,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            var bestDistance = float.MaxValue;
            var found = false;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = FloorHits[i];
                FloorHits[i] = default;
                if (hit.collider == null) continue;
                if (hit.normal.y < MinimumFloorNormalY) continue;
                if (Mathf.Abs(hit.point.y - fallbackFloorY) > MaximumFloorOffsetMeters) continue;
                if (hit.distance >= bestDistance) continue;

                bestDistance = hit.distance;
                floorY = hit.point.y;
                found = true;
            }

            return found;
        }

        private static bool IsIgnorableFloorHit(Collider hit, float floorY)
        {
            var bounds = hit.bounds;
            return bounds.max.y <= floorY + 0.06f;
        }

        private static float ScoreCandidate(
            Vector3 candidateForward,
            Vector3 desiredForward,
            float distance,
            float desiredDistance,
            float angle)
        {
            var forwardScore = Vector3.Dot(candidateForward, desiredForward);
            var distancePenalty = Mathf.Abs(distance - desiredDistance) * 0.35f;
            var anglePenalty = Mathf.Abs(angle) / 360f;
            return forwardScore - distancePenalty - anglePenalty;
        }

        private static OpenSpacePlacementResult CreateFallbackResult(
            Vector3 headPosition,
            Vector3 forward,
            float floorY,
            float desiredDistanceMeters)
        {
            return new OpenSpacePlacementResult
            {
                foundClearSpace = false,
                usedFallback = true,
                center = new Vector3(
                    headPosition.x + forward.x * desiredDistanceMeters,
                    floorY,
                    headPosition.z + forward.z * desiredDistanceMeters),
                forward = forward,
                floorY = floorY,
                score = float.NegativeInfinity,
                message = "等待空间网格，暂用前方区域"
            };
        }
    }
}
