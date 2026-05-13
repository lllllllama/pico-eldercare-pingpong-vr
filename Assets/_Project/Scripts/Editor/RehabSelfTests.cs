using PicoElderCare.Rehab;
using UnityEditor;
using UnityEngine;

public static class RehabSelfTests
{
    [MenuItem("Tools/PICO ElderCare/Run Rehab Self Tests")]
    public static void RunAll()
    {
        TwoHandsAboveHeadPoseAccumulatesHold();
        LowHandFailsPose();
        UnevenHandsFailPose();
        HoldMustReachMinimumDuration();
        SafetyMonitorPausesAndResumesWithHysteresis();
        TrainingResultSerializesToJson();
        OpenSpacePlacementAvoidsObstacleInFront();
        OpenSpacePlacementCanIgnoreExistingModuleObjects();
        ManualTrainingAreaPlacementUpdatesSessionCenter();
        PromptPanelStaysOutsideTrainingCircle();
        Debug.Log("Rehab self tests passed.");
    }

    private static void TwoHandsAboveHeadPoseAccumulatesHold()
    {
        var evaluatorObject = new GameObject("MovementEvaluatorTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            evaluator.minimumHoldSeconds = 2f;
            evaluator.ResetEvaluation();

            var sample = CreateSample(1.6f, 1.82f, 1.84f);
            var result = evaluator.Evaluate(sample, 1f, false, 1f);
            AssertTrue(result.poseValid, "Two hands above the head with a small height difference should be valid.");
            AssertTrue(result.currentHoldSeconds > 0.99f && result.currentHoldSeconds < 1.01f, "Valid pose should accumulate hold time.");
            AssertTrue(!result.completed, "One second of hold should not complete a two second movement.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void LowHandFailsPose()
    {
        var sample = CreateSample(1.6f, 1.8f, 1.7f);
        var valid = MovementEvaluator.IsTwoHandsLiftHeavenPoseValid(sample, 0.15f, 0.18f);
        AssertTrue(!valid, "A hand below head plus threshold should fail the pose.");
    }

    private static void UnevenHandsFailPose()
    {
        var sample = CreateSample(1.6f, 1.86f, 2.08f);
        var valid = MovementEvaluator.IsTwoHandsLiftHeavenPoseValid(sample, 0.15f, 0.18f);
        AssertTrue(!valid, "Hands with a height difference over the threshold should fail the pose.");
    }

    private static void HoldMustReachMinimumDuration()
    {
        var evaluatorObject = new GameObject("MovementEvaluatorDurationTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            evaluator.minimumHoldSeconds = 2f;
            evaluator.maximumHoldSeconds = 5f;
            evaluator.ResetEvaluation();

            var sample = CreateSample(1.6f, 1.82f, 1.83f);
            var first = evaluator.Evaluate(sample, 1.5f, false, 1.5f);
            AssertTrue(!first.completed, "Hold shorter than the minimum duration should not complete.");

            var second = evaluator.Evaluate(sample, 0.5f, false, 2f);
            AssertTrue(second.completed, "Hold at the minimum duration should complete.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void SafetyMonitorPausesAndResumesWithHysteresis()
    {
        var monitorObject = new GameObject("SafetyMonitorTest");
        try
        {
            var monitor = monitorObject.AddComponent<SafetyMonitor>();
            monitor.pauseDistanceMeters = 1.2f;
            monitor.resumeDistanceMeters = 1.1f;
            monitor.ResetMonitor();

            var center = Vector3.zero;
            var paused = monitor.Evaluate(new Vector3(1.21f, 1.6f, 0f), center, true);
            AssertTrue(paused.isPaused, "Head distance over pause threshold should pause the session.");
            AssertTrue(paused.pauseCount == 1, "First safety pause should increment pause count.");

            var stillPaused = monitor.Evaluate(new Vector3(1.15f, 1.6f, 0f), center, true);
            AssertTrue(stillPaused.isPaused, "Head distance between pause and resume thresholds should stay paused.");

            var resumed = monitor.Evaluate(new Vector3(1.0f, 1.6f, 0f), center, true);
            AssertTrue(!resumed.isPaused, "Head distance under resume threshold should resume the session.");
        }
        finally
        {
            Object.DestroyImmediate(monitorObject);
        }
    }

    private static void TrainingResultSerializesToJson()
    {
        var result = RehabTrainingResult.CreateStarted(
            RehabMovementId.Baduanjin_TwoHandsLiftHeaven,
            "八段锦：双手托天理三焦",
            300f);
        result.Finish(RehabSessionEndReason.Completed, true, 2.1f, 2.1f, 2.1f, 1, 1.25f);

        var json = JsonUtility.ToJson(result, true);
        AssertTrue(json.Contains("sessionId"), "Serialized result should include sessionId.");
        AssertTrue(json.Contains("movementId"), "Serialized result should include movementId.");
        AssertTrue(json.Contains("maxHeadDistanceFromCenterMeters"), "Serialized result should include max head distance.");
    }

    private static void OpenSpacePlacementAvoidsObstacleInFront()
    {
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            obstacle.name = "OpenSpacePlacementObstacle";
            obstacle.transform.position = new Vector3(0f, 0.55f, 51.5f);
            obstacle.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);

            var result = OpenSpacePlacementSolver.FindBestPlacement(
                new Vector3(0f, 1.6f, 50f),
                Quaternion.identity,
                0f,
                1.5f,
                1.2f,
                2.2f,
                0.65f,
                1.7f,
                ~0);

            var obstacleHorizontal = new Vector2(obstacle.transform.position.x, obstacle.transform.position.z);
            var resultHorizontal = new Vector2(result.center.x, result.center.z);
            AssertTrue(result.foundClearSpace, "Open-space solver should find an alternate clear candidate.");
            AssertTrue(Vector2.Distance(obstacleHorizontal, resultHorizontal) > 0.9f, "Open-space solver should not place the center inside the obstacle in front.");
        }
        finally
        {
            Object.DestroyImmediate(obstacle);
        }
    }

    private static void OpenSpacePlacementCanIgnoreExistingModuleObjects()
    {
        var existingModuleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            existingModuleObject.name = "ExistingModuleObject";
            existingModuleObject.transform.position = new Vector3(0f, 0.55f, 61.5f);
            existingModuleObject.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);

            var result = OpenSpacePlacementSolver.FindBestPlacement(
                new Vector3(0f, 1.6f, 60f),
                Quaternion.identity,
                0f,
                1.5f,
                1.5f,
                1.5f,
                0.65f,
                1.7f,
                ~0,
                new[] { existingModuleObject.transform });

            AssertTrue(result.foundClearSpace, "Open-space solver should ignore existing module objects when requested.");
            AssertTrue(Vector3.Distance(result.center, new Vector3(0f, 0f, 61.5f)) < 0.05f, "Ignored module objects should not force a different placement.");
        }
        finally
        {
            Object.DestroyImmediate(existingModuleObject);
        }
    }

    private static void ManualTrainingAreaPlacementUpdatesSessionCenter()
    {
        var sessionObject = new GameObject("RehabSessionManualPlacementTest");
        var areaObject = new GameObject("TrainingArea");
        var promptObject = new GameObject("PromptCanvas");
        try
        {
            var session = sessionObject.AddComponent<RehabSessionManager>();
            session.trainingAreaRoot = areaObject.transform;
            session.promptCanvas = promptObject.transform;
            session.trainingFloorY = 0f;
            session.promptHeightMeters = 1.65f;
            session.promptForwardOffsetMeters = 0.85f;

            var requestedCenter = new Vector3(1.2f, 2f, 2.4f);
            session.SetTrainingAreaCenter(requestedCenter, Vector3.forward, new Vector3(0f, 1.6f, 0f));

            AssertTrue(Vector3.Distance(session.TrainingCenter, new Vector3(1.2f, 0f, 2.4f)) < 0.001f, "Manual placement should update the safety/evaluation training center.");
            AssertTrue(Vector3.Distance(areaObject.transform.position, session.TrainingCenter) < 0.001f, "Manual placement should move the training area root.");
            AssertTrue(Mathf.Abs(promptObject.transform.position.y - 1.65f) < 0.001f, "Manual placement should keep the prompt at the configured height.");
        }
        finally
        {
            Object.DestroyImmediate(promptObject);
            Object.DestroyImmediate(areaObject);
            Object.DestroyImmediate(sessionObject);
        }
    }

    private static void PromptPanelStaysOutsideTrainingCircle()
    {
        var sessionObject = new GameObject("RehabPromptPlacementTest");
        var areaObject = new GameObject("TrainingArea");
        var promptObject = new GameObject("PromptCanvas");
        try
        {
            var session = sessionObject.AddComponent<RehabSessionManager>();
            session.trainingAreaRoot = areaObject.transform;
            session.promptCanvas = promptObject.transform;
            session.trainingFloorY = 0f;
            session.promptHeightMeters = 1.65f;
            session.promptForwardOffsetMeters = 0.85f;

            var center = new Vector3(0f, 0f, 1.5f);
            session.SetTrainingAreaCenter(center, Vector3.forward, new Vector3(0f, 1.6f, 0f));

            var horizontalOffset = new Vector2(
                promptObject.transform.position.x - center.x,
                promptObject.transform.position.z - center.z).magnitude;

            AssertTrue(horizontalOffset > 0.8f, "Prompt panel should sit outside the training circle center.");
            AssertTrue(promptObject.transform.position.z > center.z, "Prompt panel should sit in front of the training circle.");
            AssertTrue(Mathf.Abs(promptObject.transform.position.y - 1.65f) < 0.001f, "Prompt panel should use eye-level height.");
        }
        finally
        {
            Object.DestroyImmediate(promptObject);
            Object.DestroyImmediate(areaObject);
            Object.DestroyImmediate(sessionObject);
        }
    }

    private static RehabPoseSample CreateSample(float headY, float leftY, float rightY)
    {
        return new RehabPoseSample
        {
            hasHead = true,
            hasLeftHand = true,
            hasRightHand = true,
            headPosition = new Vector3(0f, headY, 0f),
            headRotation = Quaternion.identity,
            leftHandPosition = new Vector3(-0.2f, leftY, 0.4f),
            leftHandRotation = Quaternion.identity,
            rightHandPosition = new Vector3(0.2f, rightY, 0.4f),
            rightHandRotation = Quaternion.identity
        };
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
