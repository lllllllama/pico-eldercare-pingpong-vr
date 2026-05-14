using PicoElderCare.Rehab;
using TMPro;
using Unity.XR.CoreUtils;
using Unity.XR.PXR;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class RehabSceneBuilder
{
    private const string MainEntryScenePath = "Assets/_Project/Scenes/00_MainEntry.unity";
    private const string DeviceTestScenePath = "Assets/_Project/Scenes/00_DeviceTest.unity";
    private const string PingPongScenePath = "Assets/_Project/Scenes/01_PingPongDemo.unity";
    private const string RehabScenePath = "Assets/_Project/Scenes/MR_Rehab_Main.unity";
    private const string MaterialRoot = "Assets/_Project/Materials/Rehab";
    private const string FontRoot = "Assets/_Project/Fonts/Rehab";
    private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string XrUiInputModulePresetPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/Presets/XRI Default XR UI Input Module.preset";
    private const string XriDefaultInputActionsPath = "Assets/Samples/XR Interaction Toolkit/2.6.4/Starter Assets/XRI Default Input Actions.inputactions";
    private const string RehabChineseFontSourcePath = FontRoot + "/NotoSansSC-VF.ttf";
    private const string RehabChineseFontAssetPath = MaterialRoot + "/RehabChineseTMP.asset";

    private static TMP_FontAsset rehabFontAsset;

    [MenuItem("Tools/PICO ElderCare/Build Main Entry Scene")]
    public static void BuildMainEntryScene()
    {
        if (!EnsureEditMode()) return;
        EnsureFolders();
        ConfigureMixedRealityProjectSettings();
        BuildMainEntrySceneInternal();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/PICO ElderCare/Build MR Rehab Main Scene")]
    public static void BuildMrRehabMainScene()
    {
        if (!EnsureEditMode()) return;
        EnsureFolders();
        ConfigureMixedRealityProjectSettings();
        BuildRehabSceneInternal();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/PICO ElderCare/Build Unified MVP Scenes")]
    public static void BuildUnifiedMvpScenes()
    {
        if (!EnsureEditMode()) return;
        EnsureFolders();
        ConfigureMixedRealityProjectSettings();
        PingPongDemoSceneBuilder.BuildMixedRealityDemoScene();
        BuildMainEntrySceneInternal();
        BuildRehabSceneInternal();
        AddReturnHomePanelToPingPongSceneInternal();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildMainEntrySceneInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var xrOrigin = CreateXrOrigin();
        var mainCamera = FindMainCamera();

        EnsureLight();
        EnsureXrInteractionSupport();

        var managers = new GameObject("EntryManagers");
        var menu = managers.AddComponent<UnifiedEntryMenu>();
        menu.pingPongSceneName = "01_PingPongDemo";
        menu.rehabSceneName = "MR_Rehab_Main";

        var mrManager = managers.AddComponent<RehabMixedRealityManager>();
        mrManager.targetCamera = mainCamera;
        mrManager.enableOnStart = true;
        mrManager.enableVideoSeeThrough = true;
        mrManager.configureTransparentCamera = true;
        mrManager.suppressBackgroundVisuals = true;

        var backgroundSuppressor = managers.AddComponent<MrBackgroundVisualSuppressor>();
        backgroundSuppressor.hideAllEnvironmentRenderers = true;
        backgroundSuppressor.hideAllRoomSensingRenderers = true;
        backgroundSuppressor.scanIntervalSeconds = 0.15f;

        SetupPicoRoomSensingManagers(managers.transform);

        var entryCanvas = BuildEntryCanvas(menu, mainCamera != null ? mainCamera.transform : null);
        var entryPlacer = entryCanvas.AddComponent<OpenSpaceCanvasPlacer>();
        entryPlacer.hmdTransform = mainCamera != null ? mainCamera.transform : null;
        entryPlacer.targetTransform = entryCanvas.transform;
        entryPlacer.desiredDistanceMeters = 2.2f;
        entryPlacer.minDistanceMeters = 1.2f;
        entryPlacer.maxDistanceMeters = 3.0f;
        entryPlacer.clearanceRadiusMeters = 0.55f;
        entryPlacer.clearanceHeightMeters = 1.55f;
        entryPlacer.canvasHeightMeters = 1.45f;
        entryPlacer.searchDurationSeconds = 8f;

        EditorUtility.SetDirty(managers);
        EditorUtility.SetDirty(entryCanvas);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);

        EditorSceneManager.SaveScene(scene, MainEntryScenePath);
    }

    private static void BuildRehabSceneInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var xrOrigin = CreateXrOrigin();
        var mainCamera = FindMainCamera();
        var hmd = mainCamera != null ? mainCamera.transform : null;
        var leftController = FindChildByName(xrOrigin != null ? xrOrigin.transform : null, "Left Controller");
        var rightController = FindChildByName(xrOrigin != null ? xrOrigin.transform : null, "Right Controller");

        EnsureLight();
        EnsureXrInteractionSupport();

        var rehabRoot = new GameObject("Rehab");
        var visualRoot = new GameObject("RehabVisuals");
        visualRoot.transform.SetParent(rehabRoot.transform, false);
        var managers = new GameObject("RehabManagers");
        managers.transform.SetParent(rehabRoot.transform, false);
        var homeMenu = managers.AddComponent<ModuleHomeMenu>();

        var trainingArea = BuildTrainingArea(visualRoot.transform, out var trainingAreaDragHandle);
        var promptCanvas = BuildRehabPromptCanvas(visualRoot.transform, mainCamera, homeMenu, out var title, out var status, out var timer, out var debug);

        var poseTracker = managers.AddComponent<HandPoseTracker>();
        poseTracker.hmdTransform = hmd;
        poseTracker.leftControllerTransform = leftController;
        poseTracker.rightControllerTransform = rightController;

        var safetyMonitor = managers.AddComponent<SafetyMonitor>();
        safetyMonitor.hmdTransform = hmd;
        safetyMonitor.pauseDistanceMeters = 1.2f;
        safetyMonitor.resumeDistanceMeters = 1.1f;

        var evaluator = managers.AddComponent<MovementEvaluator>();
        evaluator.movementId = RehabMovementId.Baduanjin_TwoHandsLiftHeaven;
        evaluator.movementName = "八段锦：双手托天理三焦";
        evaluator.handsAboveHeadMeters = 0.15f;
        evaluator.maximumHandHeightDifferenceMeters = 0.18f;
        evaluator.minimumHoldSeconds = 2f;
        evaluator.maximumHoldSeconds = 5f;

        var recorder = managers.AddComponent<TrainingResultRecorder>();
        var session = managers.AddComponent<RehabSessionManager>();
        session.handPoseTracker = poseTracker;
        session.safetyMonitor = safetyMonitor;
        session.movementEvaluator = evaluator;
        session.resultRecorder = recorder;
        session.trainingAreaRoot = trainingArea.transform;
        session.promptCanvas = promptCanvas.transform;
        session.titleText = title;
        session.statusText = status;
        session.timerText = timer;
        session.debugText = debug;
        session.sessionDurationSeconds = 300f;
        session.trainingDistanceMeters = 1.5f;
        session.trainingFloorY = 0f;
        session.promptHeightMeters = 1.65f;
        session.promptForwardOffsetMeters = 0.85f;
        session.useOpenSpacePlacement = true;
        session.refreshOpenSpaceAfterPlacement = false;
        session.openSpaceClearanceRadiusMeters = 0.85f;
        session.openSpaceClearanceHeightMeters = 1.7f;
        session.openSpaceMinDistanceMeters = 1.2f;
        session.openSpaceMaxDistanceMeters = 3.0f;
        session.openSpaceSearchDurationSeconds = 10f;
        session.openSpaceSearchIntervalSeconds = 0.5f;

        if (trainingAreaDragHandle != null)
        {
            trainingAreaDragHandle.sessionManager = session;
            trainingAreaDragHandle.trainingAreaRoot = trainingArea.transform;
            trainingAreaDragHandle.controllerTransform = leftController;
            trainingAreaDragHandle.hmdTransform = hmd;
            trainingAreaDragHandle.floorY = 0f;
            trainingAreaDragHandle.activationRadiusMeters = 0.95f;
            trainingAreaDragHandle.maxRayDistanceMeters = 4.5f;
        }

        var mrManager = managers.AddComponent<RehabMixedRealityManager>();
        mrManager.targetCamera = mainCamera;
        mrManager.enableOnStart = true;
        mrManager.enableVideoSeeThrough = true;
        mrManager.configureTransparentCamera = true;
        mrManager.suppressBackgroundVisuals = true;

        var backgroundSuppressor = managers.AddComponent<MrBackgroundVisualSuppressor>();
        backgroundSuppressor.hideAllEnvironmentRenderers = true;
        backgroundSuppressor.hideAllRoomSensingRenderers = true;
        backgroundSuppressor.scanIntervalSeconds = 0.15f;

        SetupPicoRoomSensingManagers(managers.transform);

        EditorUtility.SetDirty(rehabRoot);
        if (xrOrigin != null) EditorUtility.SetDirty(xrOrigin);
        EditorSceneManager.SaveScene(scene, RehabScenePath);
    }

    private static GameObject BuildEntryCanvas(UnifiedEntryMenu menu, Transform cameraTransform)
    {
        var canvasGo = CreateWorldCanvas("MainEntryCanvas", cameraTransform, new Vector3(0f, 1.45f, 2.2f), new Vector2(1180f, 820f));
        var panel = CreateUiObject("Panel", canvasGo.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.05f, 0.08f, 0.86f);

        CreateEntryStar(panel.transform, "StarA", new Vector2(-430f, 260f), 8f, 0.46f);
        CreateEntryStar(panel.transform, "StarB", new Vector2(390f, 210f), 7f, 0.38f);
        CreateEntryStar(panel.transform, "StarC", new Vector2(-370f, -270f), 6f, 0.32f);
        CreateEntryStar(panel.transform, "StarD", new Vector2(420f, -250f), 9f, 0.42f);

        var title = CreateText(panel.transform, "Title", "VR康养服务", 74, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 326f), new Vector2(980f, 96f));
        title.characterSpacing = 8f;
        title.color = new Color(1f, 1f, 1f, 0.98f);
        CreateEntryDivider(panel.transform, "TitleDivider", new Vector2(0f, 264f), new Vector2(260f, 4f), new Color(1f, 1f, 1f, 0.52f));

        CreateEntryModuleCard(
            panel.transform,
            "Module_HealthGame",
            "健康游戏",
            "乒乓球、投篮等趣味运动",
            ElderCareIconType.Gamepad,
            new Vector2(-285f, 82f),
            new Vector2(460f, 220f),
            new Color(0.18f, 0.46f, 0.91f, 0.96f),
            true,
            menu.LoadPingPong);

        CreateEntryModuleCard(
            panel.transform,
            "Module_Rehab",
            "康复运动",
            "太极拳、八段锦养生功法",
            ElderCareIconType.Heart,
            new Vector2(285f, 82f),
            new Vector2(460f, 220f),
            new Color(0.15f, 0.66f, 0.34f, 0.96f),
            true,
            menu.LoadRehab);

        CreateEntryModuleCard(
            panel.transform,
            "Module_Travel",
            "VR旅游",
            "长城、故宫名胜古迹",
            ElderCareIconType.MapPin,
            new Vector2(-285f, -190f),
            new Vector2(460f, 220f),
            new Color(0.55f, 0.29f, 0.89f, 0.72f),
            false,
            null);

        CreateEntryModuleCard(
            panel.transform,
            "Module_Video",
            "场景视频",
            "VR看房、生活场景体验",
            ElderCareIconType.Video,
            new Vector2(285f, -190f),
            new Vector2(460f, 220f),
            new Color(0.91f, 0.42f, 0.12f, 0.72f),
            false,
            null);

        var footer = CreateText(panel.transform, "FooterHint", "使用手柄或手势选择功能", 28, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -365f), new Vector2(900f, 50f));
        footer.color = new Color(1f, 1f, 1f, 0.62f);

        return canvasGo;
    }

    private static Button CreateEntryModuleCard(
        Transform parent,
        string name,
        string title,
        string description,
        ElderCareIconType iconType,
        Vector2 anchoredPosition,
        Vector2 size,
        Color baseColor,
        bool enabled,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var panel = go.AddComponent<Image>();
        panel.color = enabled ? baseColor : new Color(baseColor.r, baseColor.g, baseColor.b, 0.6f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = panel;
        button.interactable = enabled;
        var colors = button.colors;
        colors.normalColor = panel.color;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.16f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = panel.color;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (enabled && onClick != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, onClick);
        }

        CreateEntryDivider(go.transform, "TopHighlight", new Vector2(0f, size.y * 0.5f - 4f), new Vector2(size.x, 8f), new Color(1f, 1f, 1f, enabled ? 0.18f : 0.09f));

        var icon = CreateText(go.transform, "Icon", GetEntryIconText(iconType), 62, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 52f), new Vector2(130f, 82f));
        icon.color = new Color(1f, 1f, 1f, enabled ? 0.95f : 0.5f);
        icon.raycastTarget = false;

        var cardTitle = CreateText(go.transform, "Title", title, 42, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -28f), new Vector2(size.x - 36f, 58f));
        cardTitle.color = new Color(1f, 1f, 1f, enabled ? 0.98f : 0.62f);
        cardTitle.raycastTarget = false;

        var cardDescription = CreateText(go.transform, "Description", description, 23, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -78f), new Vector2(size.x - 44f, 46f));
        cardDescription.color = new Color(1f, 1f, 1f, enabled ? 0.88f : 0.5f);
        cardDescription.raycastTarget = false;

        if (!enabled)
        {
            var badge = CreateText(go.transform, "StatusBadge", "待接入", 20, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(size.x * 0.5f - 58f, size.y * 0.5f - 28f), new Vector2(86f, 34f));
            badge.color = new Color(1f, 1f, 1f, 0.58f);
            badge.raycastTarget = false;
        }

        return button;
    }

    private static string GetEntryIconText(ElderCareIconType iconType)
    {
        switch (iconType)
        {
            case ElderCareIconType.Gamepad:
                return "游";
            case ElderCareIconType.Heart:
                return "康";
            case ElderCareIconType.MapPin:
                return "旅";
            case ElderCareIconType.Video:
                return "影";
            default:
                return "·";
        }
    }

    private static void CreateEntryDivider(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, float cornerRadius = 2f)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void CreateEntryStar(Transform parent, string name, Vector2 anchoredPosition, float size, float alpha)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);
        var image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, alpha);
        image.raycastTarget = false;
    }

    private static GameObject BuildTrainingArea(Transform parent, out RehabTrainingAreaDragHandle dragHandle)
    {
        dragHandle = null;
        var root = new GameObject("TrainingArea");
        root.transform.SetParent(parent, false);

        var circle = new GameObject("TrainingCircle");
        circle.transform.SetParent(root.transform, false);
        circle.transform.localPosition = Vector3.zero;

        var renderer = circle.AddComponent<LineRenderer>();
        renderer.useWorldSpace = false;
        renderer.loop = true;
        renderer.widthMultiplier = 0.025f;
        renderer.numCornerVertices = 4;
        renderer.numCapVertices = 4;
        renderer.sharedMaterial = CreateOrLoadMaterial("RehabTrainingCircle", new Color(0.1f, 0.95f, 0.72f, 0.85f));

        const int segments = 96;
        const float radius = 0.6f;
        renderer.positionCount = segments;
        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            renderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.015f, Mathf.Sin(angle) * radius));
        }

        var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        handle.name = "TrainingAreaDragHandle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = new Vector3(0f, 0.08f, -radius);
        handle.transform.localScale = Vector3.one * 0.12f;
        var handleRenderer = handle.GetComponent<Renderer>();
        if (handleRenderer != null)
        {
            handleRenderer.sharedMaterial = CreateOrLoadMaterial("RehabTrainingDragHandle", new Color(1f, 0.78f, 0.12f, 0.92f));
        }

        var handleCollider = handle.GetComponent<SphereCollider>();
        if (handleCollider != null)
        {
            handleCollider.isTrigger = true;
        }

        dragHandle = handle.AddComponent<RehabTrainingAreaDragHandle>();
        dragHandle.trainingAreaRoot = root.transform;

        return root;
    }

    private static GameObject BuildRehabPromptCanvas(
        Transform parent,
        Camera mainCamera,
        ModuleHomeMenu homeMenu,
        out TMP_Text title,
        out TMP_Text status,
        out TMP_Text timer,
        out TMP_Text debug)
    {
        var canvasGo = CreateWorldCanvas("RehabPromptCanvas", null, new Vector3(0f, 1.65f, 2.35f), new Vector2(900f, 460f));
        canvasGo.transform.SetParent(parent, true);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.worldCamera = mainCamera;

        var panel = CreateUiObject("Panel", canvasGo.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.05f, 0.06f, 0.72f);

        title = CreateText(panel.transform, "MovementTitle", "八段锦：双手托天理三焦", 38, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 135f), new Vector2(800f, 80f));
        status = CreateText(panel.transform, "StatusText", "请准备：双手托天理三焦", 34, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 35f), new Vector2(800f, 90f));
        timer = CreateText(panel.transform, "TimerText", "剩余 05:00", 30, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -55f), new Vector2(800f, 70f));
        debug = CreateText(panel.transform, "DebugText", "保持 0.0s | 最佳 0.0s | 距中心 0.00m", 22, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -135f), new Vector2(820f, 60f));
        var homeButton = CreateButton(panel.transform, "HomeButton", "返回主页", new Vector2(330f, -198f), new Vector2(180f, 54f));
        UnityEventTools.AddPersistentListener(homeButton.onClick, homeMenu.LoadMainEntry);

        return canvasGo;
    }

    private static GameObject CreateXrOrigin()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrOriginPrefabPath);
        GameObject root;
        if (prefab != null)
        {
            root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
        else
        {
            root = new GameObject("XR Origin (XR Rig)");
            root.AddComponent<XROrigin>();
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(root.transform, false);
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(cameraOffset.transform, false);
            cameraGo.AddComponent<Camera>();
            new GameObject("Left Controller").transform.SetParent(cameraOffset.transform, false);
            new GameObject("Right Controller").transform.SetParent(cameraOffset.transform, false);
        }

        if (root == null) return null;

        root.name = "[Building Block] PICO Controller Tracking XR Origin (XR Rig)";
        var pxrManager = EnsureComponent<PXR_Manager>(root);
        pxrManager.openMRC = true;
        pxrManager.useRecommendedAntiAliasingLevel = true;

        var origin = root.GetComponent<XROrigin>();
        var camera = FindChildByName(root.transform, "Main Camera")?.GetComponent<Camera>();
            var cameraOffsetTransform = FindChildByName(root.transform, "Camera Offset");
            if (origin != null)
            {
                origin.Camera = camera;
                if (cameraOffsetTransform != null)
                {
                    origin.CameraFloorOffsetObject = cameraOffsetTransform.gameObject;
                }
            }

        if (camera != null)
        {
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            var clear = Color.black;
            clear.a = 0f;
            camera.backgroundColor = clear;
        }

        return root;
    }

    private static GameObject CreateWorldCanvas(string name, Transform cameraTransform, Vector3 fallbackPosition, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() ?? FindMainCamera() : FindMainCamera();
        go.transform.localScale = Vector3.one * 0.002f;

        if (cameraTransform != null)
        {
            var forward = cameraTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            go.transform.position = cameraTransform.position + forward * 2.2f + Vector3.up * 0.1f;
            go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
        else
        {
            go.transform.position = fallbackPosition;
            go.transform.rotation = Quaternion.identity;
        }

        return go;
    }

    private static void AddReturnHomePanelToPingPongSceneInternal()
    {
        if (!System.IO.File.Exists(PingPongScenePath))
        {
            Debug.LogWarning("PingPong scene was not found at " + PingPongScenePath);
            return;
        }

        var previousActiveScene = SceneManager.GetActiveScene();
        var pingPongScene = EditorSceneManager.OpenScene(PingPongScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(pingPongScene);

        try
        {
            DestroySceneObjectIfFound(pingPongScene, "PingPongHomeCanvas");
            DestroySceneObjectIfFound(pingPongScene, "PingPongHomeMenu");

            var mainCamera = FindMainCameraInScene(pingPongScene);
            var menuGo = new GameObject("PingPongHomeMenu");
            var homeMenu = menuGo.AddComponent<ModuleHomeMenu>();

            var canvasGo = BuildModuleHomeCanvas("PingPongHomeCanvas", homeMenu, mainCamera != null ? mainCamera.transform : null, new Vector3(-0.85f, 1.3f, 0.85f));
            var placer = canvasGo.AddComponent<OpenSpaceCanvasPlacer>();
            placer.hmdTransform = mainCamera != null ? mainCamera.transform : null;
            placer.targetTransform = canvasGo.transform;
            placer.desiredDistanceMeters = 1.35f;
            placer.minDistanceMeters = 0.9f;
            placer.maxDistanceMeters = 2.4f;
            placer.clearanceRadiusMeters = 0.45f;
            placer.clearanceHeightMeters = 1.2f;
            placer.canvasHeightMeters = 1.25f;
            placer.searchDurationSeconds = 6f;

            EditorUtility.SetDirty(menuGo);
            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.SaveScene(pingPongScene);
        }
        finally
        {
            if (previousActiveScene.IsValid())
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }

            EditorSceneManager.CloseScene(pingPongScene, true);
        }
    }

    private static GameObject BuildModuleHomeCanvas(string canvasName, ModuleHomeMenu homeMenu, Transform cameraTransform, Vector3 fallbackPosition)
    {
        var canvasGo = CreateWorldCanvas(canvasName, cameraTransform, fallbackPosition, new Vector2(300f, 120f));
        var panel = CreateUiObject("Panel", canvasGo.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.05f, 0.06f, 0.78f);

        var homeButton = CreateButton(panel.transform, "HomeButton", "返回主页", Vector2.zero, new Vector2(240f, 72f));
        UnityEventTools.AddPersistentListener(homeButton.onClick, homeMenu.LoadMainEntry);
        return canvasGo;
    }

    private static void SetupPicoRoomSensingManagers(Transform parent)
    {
        var sensingRoot = new GameObject("MRSpaceSensing");
        sensingRoot.transform.SetParent(parent, false);
        sensingRoot.AddComponent<RehabRoomSensingStarter>();

        var sensingMaterial = CreateOrLoadMaterial("RehabRoomSensingHidden", new Color(0.25f, 0.5f, 1f, 0.04f));
        var planeTemplate = SetupRoomSensingTemplate(sensingRoot.transform, "MRDetectedPlaneTemplate", sensingMaterial);
        var planeManager = sensingRoot.AddComponent<PXR_PlaneDetectionManager>();
        planeManager.planePrefab = planeTemplate;

        var meshTemplate = SetupRoomSensingTemplate(sensingRoot.transform, "MRSpatialMeshTemplate", sensingMaterial);
        var meshManager = sensingRoot.AddComponent<PXR_SpatialMeshManager>();
        meshManager.meshPrefab = meshTemplate;

        EditorUtility.SetDirty(sensingRoot);
    }

    private static GameObject SetupRoomSensingTemplate(Transform parent, string name, Material material)
    {
        var template = new GameObject(name);
        template.transform.SetParent(parent, false);
        template.transform.localPosition = Vector3.zero;
        template.transform.localRotation = Quaternion.identity;
        template.transform.localScale = Vector3.one;

        template.AddComponent<MeshFilter>();
        var renderer = template.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.enabled = false;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        template.AddComponent<MeshCollider>();
        template.SetActive(false);
        EditorUtility.SetDirty(template);
        return template;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        var label = go.AddComponent<TextMeshProUGUI>();
        var fontAsset = GetRehabFontAsset();
        if (fontAsset != null)
        {
            label.font = fontAsset;
        }

        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = Color.white;
        label.enableWordWrapping = true;
        return label;
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        var image = go.AddComponent<Image>();
        image.color = new Color(0.08f, 0.44f, 0.72f, 0.95f);
        var button = go.AddComponent<Button>();

        var label = CreateText(go.transform, "Label", text, 30, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, size);
        label.raycastTarget = false;
        return button;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void EnsureXrInteractionSupport()
    {
        EnsureInputActionManager();
        EnsureXrInteractionManager();
        EnsureEventSystem();
    }

    private static void EnsureInputActionManager()
    {
        var inputActionManager = Object.FindObjectOfType<InputActionManager>();
        if (inputActionManager == null)
        {
            var go = new GameObject("Input Action Manager");
            inputActionManager = go.AddComponent<InputActionManager>();
        }

        var actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XriDefaultInputActionsPath);
        if (actionAsset == null)
        {
            Debug.LogWarning("Could not find XRI default input actions at " + XriDefaultInputActionsPath);
            return;
        }

        if (inputActionManager.actionAssets == null)
        {
            inputActionManager.actionAssets = new System.Collections.Generic.List<InputActionAsset>();
        }

        if (!inputActionManager.actionAssets.Contains(actionAsset))
        {
            inputActionManager.actionAssets.Add(actionAsset);
        }

        EditorUtility.SetDirty(inputActionManager);
    }

    private static void EnsureXrInteractionManager()
    {
        if (Object.FindObjectOfType<XRInteractionManager>() != null) return;

        var go = new GameObject("XR Interaction Manager");
        go.AddComponent<XRInteractionManager>();
        EditorUtility.SetDirty(go);
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
        }

        var xrUiInputModule = eventSystem.GetComponent<XRUIInputModule>();
        if (xrUiInputModule == null)
        {
            xrUiInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        ApplyXrUiInputModulePreset(xrUiInputModule);
        EditorUtility.SetDirty(eventSystem);
    }

    private static void ApplyXrUiInputModulePreset(XRUIInputModule inputModule)
    {
        var preset = AssetDatabase.LoadAssetAtPath<Preset>(XrUiInputModulePresetPath);
        if (preset == null)
        {
            Debug.LogWarning("Could not find XRI UI input module preset at " + XrUiInputModulePresetPath);
            return;
        }

        if (!preset.CanBeAppliedTo(inputModule))
        {
            Debug.LogWarning("XRI UI input module preset could not be applied to " + inputModule.name);
            return;
        }

        preset.ApplyTo(inputModule);
        EditorUtility.SetDirty(inputModule);
    }

    private static void EnsureLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainEntryScenePath, true),
            new EditorBuildSettingsScene(DeviceTestScenePath, false),
            new EditorBuildSettingsScene(PingPongScenePath, true),
            new EditorBuildSettingsScene(RehabScenePath, true)
        };
    }

    private static void ConfigureMixedRealityProjectSettings()
    {
        var config = PXR_ProjectSetting.GetProjectConfig();
        if (config == null) return;

        config.openMRC = true;
        config.videoSeeThrough = true;
        config.spatialAnchor = true;
        config.sceneCapture = true;
        config.spatialMesh = true;
        config.planeDetection = true;
        config.mrSafeguard = true;
        config.meshLod = PxrMeshLod.Low;
        PXR_ProjectSetting.SaveAssets();
    }

    private static void EnsureFolders()
    {
        EnsureFolderPath("Assets/_Project");
        EnsureFolderPath("Assets/_Project/Scenes");
        EnsureFolderPath("Assets/_Project/Materials");
        EnsureFolderPath(MaterialRoot);
        EnsureFolderPath("Assets/_Project/Fonts");
        EnsureFolderPath(FontRoot);
    }

    private static void EnsureFolderPath(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folder = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolderPath(parent);
        }

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static TMP_FontAsset GetRehabFontAsset()
    {
        if (rehabFontAsset != null) return rehabFontAsset;

        rehabFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RehabChineseFontAssetPath);
        if (rehabFontAsset != null)
        {
            if (HasSavedFontAtlasAndMaterial(rehabFontAsset))
            {
                return rehabFontAsset;
            }

            AssetDatabase.DeleteAsset(RehabChineseFontAssetPath);
            rehabFontAsset = null;
        }

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(RehabChineseFontSourcePath);
        if (sourceFont == null)
        {
            Debug.LogWarning("Could not find rehab Chinese font at " + RehabChineseFontSourcePath + ". TextMeshPro will use the project default font.");
            return null;
        }

        rehabFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        rehabFontAsset.name = "RehabChineseTMP";
        rehabFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        rehabFontAsset.isMultiAtlasTexturesEnabled = true;
        AssetDatabase.CreateAsset(rehabFontAsset, RehabChineseFontAssetPath);
        SaveGeneratedFontSubAssets(rehabFontAsset);
        EditorUtility.SetDirty(rehabFontAsset);
        return rehabFontAsset;
    }

    private static bool HasSavedFontAtlasAndMaterial(TMP_FontAsset fontAsset)
    {
        return fontAsset.material != null &&
               fontAsset.atlasTextures != null &&
               fontAsset.atlasTextures.Length > 0 &&
               fontAsset.atlasTextures[0] != null &&
               fontAsset.atlasWidth == 1024 &&
               fontAsset.atlasHeight == 1024;
    }

    private static void SaveGeneratedFontSubAssets(TMP_FontAsset fontAsset)
    {
        var atlasTextures = fontAsset.atlasTextures;
        if (atlasTextures != null && atlasTextures.Length > 0 && atlasTextures[0] != null)
        {
            atlasTextures[0].name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlasTextures[0], fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Atlas Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color)
    {
        var path = MaterialRoot + "/" + materialName + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        var shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");

        material = new Material(shader);
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Camera FindMainCamera()
    {
        var camera = Camera.main;
        if (camera != null) return camera;

        var cameras = Object.FindObjectsOfType<Camera>(true);
        return cameras.Length > 0 ? cameras[0] : null;
    }

    private static Camera FindMainCameraInScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var cameras = roots[i].GetComponentsInChildren<Camera>(true);
            for (var j = 0; j < cameras.Length; j++)
            {
                if (cameras[j] != null && cameras[j].CompareTag("MainCamera"))
                {
                    return cameras[j];
                }
            }
        }

        for (var i = 0; i < roots.Length; i++)
        {
            var camera = roots[i].GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                return camera;
            }
        }

        return null;
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var found = FindChildByName(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static void RemoveSceneComponentsInScene<T>(Scene scene) where T : Component
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var components = roots[i].GetComponentsInChildren<T>(true);
            for (var j = 0; j < components.Length; j++)
            {
                if (components[j] != null)
                {
                    Object.DestroyImmediate(components[j]);
                }
            }
        }
    }

    private static void DestroySceneObjectIfFound(Scene scene, string objectName)
    {
        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var found = FindChildByName(roots[i].transform, objectName);
            if (found != null)
            {
                Object.DestroyImmediate(found.gameObject);
                return;
            }
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;

        for (var i = 0; i < root.childCount; i++)
        {
            var found = FindChildByName(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static bool EnsureEditMode()
    {
        if (!Application.isPlaying) return true;
        Debug.LogWarning("Rehab scene builder can only run in edit mode.");
        return false;
    }
}
