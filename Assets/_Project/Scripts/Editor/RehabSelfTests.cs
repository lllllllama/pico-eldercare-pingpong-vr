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
        BaduanjinDefinitionsContainEightMovements();
        BaduanjinCorePosesCanComplete();
        LookBackRejectsFastTurnUntilRecentered();
        MovementTimeoutSkipsAndRecordsResult();
        FinalizeCurrentMovementRecordsPartialResult();
        TaiChiDefinitionsContainSixMovements();
        TaiChiCorePosesValidate();
        TaiChiClosingRequiresLoweringAfterMovementStart();
        MovementEvaluatorSwitchesDefaultDefinitionsByTrainingMode();
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
            ConfigureSingleTwoHandsMovement(evaluator);
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
            ConfigureSingleTwoHandsMovement(evaluator);
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

    private static void BaduanjinDefinitionsContainEightMovements()
    {
        var definitions = BaduanjinEvaluator.CreateDefaultMovements();
        AssertTrue(definitions.Length == 8, "Baduanjin mode should define eight movements.");
        AssertTrue(definitions[0].movementId == RehabMovementId.Baduanjin_TwoHandsLiftHeaven, "The existing first movement should remain first.");
        AssertTrue(definitions[7].movementId == RehabMovementId.Baduanjin_HeelRaiseFinish, "The final movement should be the simplified heel-raise finish.");
    }

    private static void BaduanjinCorePosesCanComplete()
    {
        var evaluatorObject = new GameObject("BaduanjinEvaluatorPoseTest");
        try
        {
            var baduanjin = evaluatorObject.AddComponent<BaduanjinEvaluator>();
            var definitions = BaduanjinEvaluator.CreateDefaultMovements();

            AssertStepValid(baduanjin, definitions[0], 0, CreateSample(1.6f, 1.82f, 1.83f), "Two hands lift should validate.");
            AssertStepValid(baduanjin, definitions[1], 0, CreateSampleWithHands(1.6f, new Vector3(-0.52f, 1.2f, 0.15f), new Vector3(0.08f, 1.2f, 0.15f)), "Left draw-bow should validate.");
            AssertStepValid(baduanjin, definitions[2], 0, CreateSampleWithHands(1.6f, new Vector3(-0.2f, 1.78f, 0.2f), new Vector3(0.2f, 1.15f, 0.2f)), "Single raise should validate.");
            AssertStepValid(baduanjin, definitions[3], 0, CreateSampleWithHeadYaw(1.6f, -28f), "Gentle left look-back should validate.");
            AssertStepValid(baduanjin, definitions[4], 0, CreateSampleWithHeadPosition(new Vector3(-0.16f, 1.6f, 0f)), "Gentle left sway should validate.");
            AssertStepValid(baduanjin, definitions[5], 0, CreateSample(1.6f, 0.82f, 0.83f), "Simplified reach-down should validate.");
            AssertStepValid(baduanjin, definitions[6], 0, CreateSampleWithHands(1.6f, new Vector3(-0.18f, 1.18f, 0.48f), new Vector3(0.18f, 1.18f, 0.48f)), "Gentle punch should validate.");
            AssertStepValid(baduanjin, definitions[7], 0, CreateSampleWithHeadPosition(new Vector3(0f, 1.66f, 0f)), "Heel raise or seated finish should validate.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void MovementTimeoutSkipsAndRecordsResult()
    {
        var evaluatorObject = new GameObject("MovementTimeoutTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            evaluator.autoCreateDefaultBaduanjinDefinitions = false;
            evaluator.movementDefinitions = new[]
            {
                new MovementDefinition(
                    RehabMovementId.Baduanjin_TouchKneesStrengthenKidneys,
                    "两手攀足固肾腰",
                    "timeout test",
                    new MovementStepDefinition("下探", "双手下探", 1f, 0.5f))
            };

            evaluator.ResetEvaluation();
            var invalidPose = CreateSample(1.6f, 1.45f, 1.45f);
            var result = evaluator.Evaluate(invalidPose, 1.1f, false, 1.1f, 2);
            AssertTrue(result.stepTimedOut, "A step should time out when the user does not reach the pose.");
            AssertTrue(result.completed, "A one-step movement sequence should complete after timeout skip.");
            AssertTrue(evaluator.MovementResults.Count == 1, "Timeout should still record the movement result.");
            AssertTrue(evaluator.MovementResults[0].skippedByTimeout, "Movement result should flag timeout skip.");
            AssertTrue(evaluator.MovementResults[0].completion < 0.01f, "Timed-out movement completion should stay at zero.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void LookBackRejectsFastTurnUntilRecentered()
    {
        var evaluatorObject = new GameObject("LookBackSpeedTest");
        try
        {
            var baduanjin = evaluatorObject.AddComponent<BaduanjinEvaluator>();
            var movement = BaduanjinEvaluator.CreateDefaultMovements()[3];
            var baseline = CreateSampleWithHeadYaw(1.6f, 0f);
            var leftTurn = CreateSampleWithHeadYaw(1.6f, -28f);

            baduanjin.ResetForMovement(movement.movementId, baseline);
            var fastTurn = baduanjin.EvaluateStep(movement, 0, leftTurn, 0.1f);
            AssertTrue(!fastTurn.poseValid, "Fast look-back turn should be rejected.");

            var heldAfterFastTurn = baduanjin.EvaluateStep(movement, 0, leftTurn, 1f);
            AssertTrue(!heldAfterFastTurn.poseValid, "Holding after a fast turn should stay rejected until recentered.");

            baduanjin.EvaluateStep(movement, 0, baseline, 1f);
            var slowTurn = baduanjin.EvaluateStep(movement, 0, leftTurn, 1f);
            AssertTrue(slowTurn.poseValid, "Slow look-back turn should validate after recentering.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void FinalizeCurrentMovementRecordsPartialResult()
    {
        var evaluatorObject = new GameObject("MovementFinalizePartialTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            evaluator.autoCreateDefaultBaduanjinDefinitions = false;
            evaluator.movementDefinitions = new[]
            {
                new MovementDefinition(
                    RehabMovementId.Baduanjin_DrawBowShootHawk,
                    "左右开弓似射雕",
                    "partial test",
                    new MovementStepDefinition("向左开弓", "左手向左侧打开", 0.5f, 10f),
                    new MovementStepDefinition("向右开弓", "右手向右侧打开", 0.5f, 10f))
            };

            evaluator.ResetEvaluation();
            var leftBow = CreateSampleWithHands(1.6f, new Vector3(-0.52f, 1.2f, 0.15f), new Vector3(0.08f, 1.2f, 0.15f));
            evaluator.Evaluate(leftBow, 0.5f, false, 0.5f, 1);
            evaluator.FinalizeCurrentMovement(0.8f, 2);

            AssertTrue(evaluator.MovementResults.Count == 1, "Finalizing should record the in-progress movement.");
            AssertTrue(evaluator.MovementResults[0].completion > 0.49f && evaluator.MovementResults[0].completion < 0.51f, "Partial result should preserve completed step ratio.");
            AssertTrue(evaluator.MovementResults[0].safetyWarningCount == 1, "Partial result should include warnings since movement start.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void TaiChiDefinitionsContainSixMovements()
    {
        var definitions = TaiChiEvaluator.CreateDefaultMovements();
        AssertTrue(definitions.Length == 6, "TaiChiTraining should define six base movements.");
        AssertTrue(definitions[0].movementId == RehabMovementId.Taiji_Opening, "TaiChiTraining should start with opening.");
        AssertTrue(definitions[5].movementId == RehabMovementId.Taiji_Closing, "TaiChiTraining should end with closing.");
    }

    private static void TaiChiCorePosesValidate()
    {
        var evaluatorObject = new GameObject("TaiChiEvaluatorPoseTest");
        try
        {
            var taiChi = evaluatorObject.AddComponent<TaiChiEvaluator>();
            var definitions = TaiChiEvaluator.CreateDefaultMovements();
            var baseline = CreateTaiChiSample(
                new Vector3(-0.2f, -0.45f, 0.25f),
                new Vector3(0.2f, -0.45f, 0.25f));

            AssertTaiChiStepValid(taiChi, definitions[0], 0, baseline, CreateTaiChiSample(new Vector3(-0.2f, -0.18f, 0.28f), new Vector3(0.2f, -0.18f, 0.28f)), "Opening raise should validate.");
            AssertTaiChiStepValid(taiChi, definitions[0], 1, baseline, CreateTaiChiSample(new Vector3(-0.2f, -0.40f, 0.28f), new Vector3(0.2f, -0.40f, 0.28f)), "Opening lower should validate.");
            AssertTaiChiStepValid(taiChi, definitions[1], 0, baseline, CreateTaiChiSample(new Vector3(-0.35f, -0.22f, 0.35f), new Vector3(-0.25f, -0.24f, 0.35f)), "Cloud hands left should validate.");
            AssertTaiChiStepValid(taiChi, definitions[1], 1, baseline, CreateTaiChiSample(new Vector3(0.25f, -0.22f, 0.35f), new Vector3(0.35f, -0.24f, 0.35f)), "Cloud hands right should validate.");
            AssertTaiChiStepValid(taiChi, definitions[2], 0, baseline, CreateTaiChiSample(new Vector3(-0.18f, -0.24f, 0.42f), new Vector3(0.18f, -0.28f, -0.25f)), "Wild horse left-forward should validate.");
            AssertTaiChiStepValid(taiChi, definitions[3], 0, baseline, CreateTaiChiSample(new Vector3(-0.2f, 0.24f, 0.2f), new Vector3(0.2f, -0.42f, 0.2f)), "White crane should validate.");
            AssertTaiChiStepValid(taiChi, definitions[4], 0, baseline, CreateTaiChiSample(new Vector3(-0.2f, -0.45f, 0.15f), new Vector3(0.2f, -0.24f, 0.42f)), "Brush knee should validate.");
            AssertTaiChiStepValid(
                taiChi,
                definitions[5],
                0,
                CreateTaiChiSample(new Vector3(-0.2f, -0.18f, 0.22f), new Vector3(0.2f, -0.18f, 0.22f)),
                CreateTaiChiSample(new Vector3(-0.2f, -0.36f, 0.22f), new Vector3(0.2f, -0.36f, 0.22f)),
                "Closing should validate after the hands lower from the starting pose.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void TaiChiClosingRequiresLoweringAfterMovementStart()
    {
        var evaluatorObject = new GameObject("TaiChiClosingRegressionTest");
        try
        {
            var taiChi = evaluatorObject.AddComponent<TaiChiEvaluator>();
            var closing = TaiChiEvaluator.CreateDefaultMovements()[5];
            var brushKneeEnd = CreateTaiChiSample(
                new Vector3(-0.2f, -0.45f, 0.15f),
                new Vector3(0.2f, -0.24f, 0.42f));

            taiChi.ResetForMovement(closing.movementId, brushKneeEnd);
            var unchanged = taiChi.EvaluateStep(closing, 0, brushKneeEnd, 1f);
            AssertTrue(!unchanged.poseValid, "Closing should not validate just because the previous pose already leaves both hands low.");

            var lowered = CreateTaiChiSample(
                new Vector3(-0.2f, -0.52f, 0.18f),
                new Vector3(0.2f, -0.42f, 0.25f));
            var afterLowering = taiChi.EvaluateStep(closing, 0, lowered, 1f);
            AssertTrue(afterLowering.poseValid, "Closing should validate after the hands visibly lower from the movement start pose.");
        }
        finally
        {
            Object.DestroyImmediate(evaluatorObject);
        }
    }

    private static void MovementEvaluatorSwitchesDefaultDefinitionsByTrainingMode()
    {
        var evaluatorObject = new GameObject("MovementEvaluatorTrainingModeTest");
        try
        {
            var evaluator = evaluatorObject.AddComponent<MovementEvaluator>();
            evaluator.trainingMode = RehabTrainingMode.TaiChiTraining;
            evaluator.ResetEvaluation();

            AssertTrue(evaluator.CurrentMovement != null && evaluator.CurrentMovement.movementId == RehabMovementId.Taiji_Opening, "TaiChiTraining mode should auto-load TaiChi default definitions.");
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

    private static RehabPoseSample CreateSampleWithHands(float headY, Vector3 leftLocal, Vector3 rightLocal)
    {
        return new RehabPoseSample
        {
            hasHead = true,
            hasLeftHand = true,
            hasRightHand = true,
            headPosition = new Vector3(0f, headY, 0f),
            headRotation = Quaternion.identity,
            leftHandPosition = new Vector3(leftLocal.x, headY + leftLocal.y - 1.6f, leftLocal.z),
            leftHandRotation = Quaternion.identity,
            rightHandPosition = new Vector3(rightLocal.x, headY + rightLocal.y - 1.6f, rightLocal.z),
            rightHandRotation = Quaternion.identity
        };
    }

    private static RehabPoseSample CreateSampleWithHeadYaw(float headY, float yawDegrees)
    {
        var sample = CreateSample(headY, headY - 0.3f, headY - 0.3f);
        sample.headRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        return sample;
    }

    private static RehabPoseSample CreateSampleWithHeadPosition(Vector3 headPosition)
    {
        return new RehabPoseSample
        {
            hasHead = true,
            hasLeftHand = true,
            hasRightHand = true,
            headPosition = headPosition,
            headRotation = Quaternion.identity,
            leftHandPosition = headPosition + new Vector3(-0.2f, -0.45f, 0.2f),
            leftHandRotation = Quaternion.identity,
            rightHandPosition = headPosition + new Vector3(0.2f, -0.45f, 0.2f),
            rightHandRotation = Quaternion.identity
        };
    }

    private static RehabPoseSample CreateTaiChiSample(Vector3 leftLocal, Vector3 rightLocal)
    {
        var head = new Vector3(0f, 1.6f, 0f);
        return new RehabPoseSample
        {
            hasHead = true,
            hasLeftHand = true,
            hasRightHand = true,
            headPosition = head,
            headRotation = Quaternion.identity,
            leftHandPosition = head + leftLocal,
            leftHandRotation = Quaternion.identity,
            rightHandPosition = head + rightLocal,
            rightHandRotation = Quaternion.identity
        };
    }

    private static void ConfigureSingleTwoHandsMovement(MovementEvaluator evaluator)
    {
        evaluator.autoCreateDefaultBaduanjinDefinitions = false;
        evaluator.movementDefinitions = new[]
        {
            new MovementDefinition(
                RehabMovementId.Baduanjin_TwoHandsLiftHeaven,
                "双手托天理三焦",
                "test",
                new MovementStepDefinition("上举保持", "双手举至头顶上方", 2f, 25f))
        };
    }

    private static void AssertStepValid(BaduanjinEvaluator evaluator, MovementDefinition movement, int stepIndex, RehabPoseSample sample, string message)
    {
        evaluator.ResetForMovement(movement.movementId, CreateSample(1.6f, 1.2f, 1.2f));
        var result = evaluator.EvaluateStep(movement, stepIndex, sample, 1f);
        AssertTrue(result.poseValid, message);
    }

    private static void AssertTaiChiStepValid(TaiChiEvaluator evaluator, MovementDefinition movement, int stepIndex, RehabPoseSample baseline, RehabPoseSample sample, string message)
    {
        evaluator.ResetForMovement(movement.movementId, baseline);
        var result = evaluator.EvaluateStep(movement, stepIndex, sample, 1f);
        AssertTrue(result.poseValid, message);
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
