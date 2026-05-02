using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public static class PingPongDemoSceneBuilder
{
    private const string PrefabRoot = "Assets/_Project/Prefabs/PingPong";
    private const string MaterialRoot = "Assets/_Project/Materials/PingPong";
    private const string ExternalRoot = "Assets/_Project/External/VRTableTennis";
    private const string OriginalRoot = ExternalRoot + "/Original";
    private const string OriginalModelRoot = OriginalRoot + "/Models";
    private const string OriginalAudioRoot = OriginalRoot + "/Audio";
    private const string AdaptedRoot = ExternalRoot + "/Adapted";
    private const string AdaptedMaterialRoot = AdaptedRoot + "/Materials";
    private static readonly Vector3 TableColliderWorldSize = PingPongGeometry.TableColliderWorldSize;
    private static readonly Vector3 NetColliderWorldSize = PingPongGeometry.NetColliderWorldSize;
    private static readonly Vector3 PaddleColliderCenter = PingPongGeometry.PaddleColliderCenter;
    private static readonly Vector3 PaddleColliderSize = PingPongGeometry.PaddleColliderSize;
    private static readonly Vector3 PaddleHitZoneCenter = PingPongGeometry.PaddleHitZoneCenter;
    private static readonly Vector3 PaddleHitZoneSize = PingPongGeometry.PaddleHitZoneSize;

    [MenuItem("Tools/PICO ElderCare/Build VRTableTennis Adapted Assets")]
    public static void BuildVrTableTennisAdaptedAssets()
    {
        if (!EnsureEditMode()) return;

        EnsureFolders();
        RemoveRootLevelGeneratedBallObjects();
        TryCreateOrUpdateAdaptedPrefabs(true);
        RemoveRootLevelGeneratedBallObjects();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("PingPong", "VRTableTennis adapted prefab assets are ready.", "OK");
        }
    }

    [MenuItem("Tools/PICO ElderCare/Build PingPong Demo Scene")]
    public static void BuildDemoScene()
    {
        if (!EnsureEditMode()) return;

        EnsureFolders();
        RemoveRootLevelGeneratedBallObjects();
        TryCreateOrUpdateAdaptedPrefabs(false);
        RemoveRootLevelGeneratedBallObjects();
        RepairExistingBallObjectsInScene();

        var environment = GetOrCreate("Environment");
        var pingPong = GetOrCreate("PingPong");
        var managers = GetOrCreate("Managers");
        var uiRoot = GetOrCreate("UI");

        EnsureFloor(environment.transform);
        EnsureLight(environment.transform);
        EnsureBackWall(environment.transform);

        var tablePrefab = LoadAdaptedPrefab("PingPongTable") ??
                          LoadOrCreatePrefabAsset("PingPongTable", PrimitiveType.Cube, TableColliderWorldSize, CreateOrLoadMaterial("TableBlue", new Color(0.07f, 0.3f, 0.47f)));
        var paddlePrefab = LoadAdaptedPrefab("PingPongPaddle") ??
                           LoadOrCreatePrefabAsset("PingPongPaddle", PrimitiveType.Cube, PaddleColliderSize, CreateOrLoadMaterial("PaddleRed", new Color(0.66f, 0.11f, 0.11f)));
        var ballPrefab = CreateOrUpdateBallPrefab();
        if (ballPrefab == null)
        {
            Debug.LogError("Could not create or load PingPong ball prefab. Demo scene generation stopped.");
            return;
        }

        RemoveGeneratedObject("Table");
        RemoveGeneratedObject("Net");
        var table = InstantiateOrReuse("Table", tablePrefab, pingPong.transform, PingPongGeometry.TableCenter, GetInstanceScale(tablePrefab, TableColliderWorldSize));
        var net = SetupOptionalNet(tablePrefab, pingPong.transform);
        var rightPaddle = InstantiateOrReuse("Paddle_Right", paddlePrefab, pingPong.transform, new Vector3(0.35f, 1.1f, 0.5f), GetInstanceScale(paddlePrefab, PaddleColliderSize));
        RemoveGeneratedObject("Paddle_Left");
        var leftHand = SetupLeftHandGrabVisual(pingPong.transform);

        var spawn = GetOrCreate("BallSpawnPoint", pingPong.transform);
        spawn.transform.position = new Vector3(0f, 1.25f, 3.05f);
        var target = GetOrCreate("BallTargetPoint", pingPong.transform);
        target.transform.position = new Vector3(0.2f, 1.15f, 0.7f);
        var ballContainer = GetOrCreate("BallContainer", pingPong.transform);

        SetupPaddle(rightPaddle);
        SetupTablePhysics(table);
        var tableBlocker = SetupPlayerTableBlocker(pingPong.transform, table.transform);
        SetupControllerTableLimiter(rightPaddle, table.transform);
        SetupControllerTableLimiter(leftHand, table.transform);

        var spawnerObject = GetOrCreate("BallSpawner", managers.transform);
        var spawner = EnsureComponent<BallSpawner>(spawnerObject);
        if (spawner == null) return;
        spawner.ballPrefab = ballPrefab;
        spawner.spawnPoint = spawn.transform;
        spawner.targetPoint = target.transform;
        spawner.ballContainer = ballContainer.transform;
        spawner.autoStartOnPlay = true;
        spawner.serveSpeed = 3.0f;
        spawner.minimumNetClearanceHeight = PingPongGeometry.TableTopHeight + PingPongGeometry.NetHeight + 0.08f;
        spawner.netWorldZ = PingPongGeometry.TableCenter.z;
        spawner.bounceOnTableBeforePlayer = true;
        spawner.tableBounceWorldY = PingPongGeometry.TableTopHeight + PingPongGeometry.BallRadius;
        spawner.tableBounceWorldZ = 1.45f;
        spawner.horizontalRandomRange = 0.12f;
        spawner.verticalRandomRange = 0.04f;
        ValidateBallSpawnerBindings(spawner);

        var scoreObject = GetOrCreate("ScoreManager", managers.transform);
        var scoreManager = EnsureComponent<ScoreManager>(scoreObject);
        if (scoreManager == null) return;

        var feedback = GetOrCreate("HitFeedbackManager", managers.transform);
        var feedbackManager = EnsureComponent<HitFeedbackManager>(feedback);
        if (feedbackManager == null) return;
        SetupFeedbackAudio(feedback, feedbackManager);

        BuildUi(uiRoot.transform, scoreManager);
        BindController(rightPaddle.GetComponent<PaddleFollower>(), true);
        var leftController = BindController(leftHand.GetComponent<ControllerTransformFollower>(), false);
        var leftBallGrabber = SetupControllerBallGrabber(managers.transform, leftController);
        SetupTableDragHandle(pingPong.transform, table, leftController, leftBallGrabber, spawner, spawn.transform, target.transform, tableBlocker != null ? tableBlocker.transform : null);
        SetupInitialViewAligner(managers.transform);

        EditorUtility.SetDirty(table);
        if (net != null) EditorUtility.SetDirty(net);
        EditorUtility.SetDirty(rightPaddle);
        EditorUtility.SetDirty(leftHand);
        EditorUtility.SetDirty(spawnerObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("PingPong", "PingPong Demo scene objects and prefab assets are ready.", "OK");
    }

    [MenuItem("Tools/PICO ElderCare/Repair PingPong Demo Scene Objects")]
    public static void RepairPingPongDemoSceneObjects()
    {
        if (!EnsureEditMode()) return;

        EnsureFolders();
        TryCreateOrUpdateAdaptedPrefabs(false);
        var environment = GetOrCreate("Environment");
        var pingPong = GetOrCreate("PingPong");
        EnsureFloor(environment.transform);
        EnsureLight(environment.transform);
        EnsureBackWall(environment.transform);
        RemoveGeneratedObject("Paddle_Left");
        var leftHand = SetupLeftHandGrabVisual(pingPong.transform);
        var table = GameObject.Find("Table");
        GameObject tableBlocker = null;
        if (table != null)
        {
            SetupTablePhysics(table);
            tableBlocker = SetupPlayerTableBlocker(pingPong.transform, table.transform);
            SetupControllerTableLimiter(leftHand, table.transform);
            var rightPaddle = GameObject.Find("Paddle_Right");
            if (rightPaddle != null)
            {
                SetupControllerTableLimiter(rightPaddle, table.transform);
            }
        }
        else
        {
            tableBlocker = SetupPlayerTableBlocker(pingPong.transform);
        }
        var leftController = BindController(leftHand.GetComponent<ControllerTransformFollower>(), false);
        var leftBallGrabber = SetupControllerBallGrabber(GetOrCreate("Managers").transform, leftController);
        var spawner = Object.FindObjectOfType<BallSpawner>();
        if (table != null && spawner != null)
        {
            SetupTableDragHandle(pingPong.transform, table, leftController, leftBallGrabber, spawner, spawner.spawnPoint, spawner.targetPoint, tableBlocker != null ? tableBlocker.transform : null);
        }
        RemoveRootLevelGeneratedBallObjects();
        RepairExistingBallObjectsInScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("PingPong", "PingPong Demo scene objects have been repaired.", "OK");
    }

    private static void EnsureFolders()
    {
        EnsureFolderPath("Assets/_Project");
        EnsureFolderPath("Assets/_Project/Prefabs");
        EnsureFolderPath(PrefabRoot);
        EnsureFolderPath("Assets/_Project/Materials");
        EnsureFolderPath(MaterialRoot);
        EnsureFolderPath(ExternalRoot);
        EnsureFolderPath(OriginalRoot);
        EnsureFolderPath(AdaptedRoot);
        EnsureFolderPath(AdaptedMaterialRoot);
    }

    private static void EnsureFolderPath(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolderPath(parent);
        }

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color)
    {
        return CreateOrLoadMaterial(materialName, color, MaterialRoot);
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color, string materialRoot)
    {
        var matPath = $"{materialRoot}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material != null) return material;

        var shader =
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Legacy Shaders/Diffuse");

        if (shader == null)
        {
            Debug.LogError($"Could not find a valid shader for material: {materialName}");
            return null;
        }

        material = new Material(shader);
        material.color = color;
        AssetDatabase.CreateAsset(material, matPath);
        return material;
    }

    private static GameObject LoadAdaptedPrefab(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{AdaptedRoot}/{assetName}_Adapted.prefab");
    }

    private static GameObject LoadAdaptedBallPrefab()
    {
        var path = $"{AdaptedRoot}/PingPongBall_Adapted.prefab";
        RepairBallPrefabAsset(path);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Vector3 GetInstanceScale(GameObject prefab, Vector3 fallbackScale)
    {
        return IsAdaptedPrefab(prefab)
            ? Vector3.one
            : fallbackScale;
    }

    private static bool IsAdaptedPrefab(GameObject prefab)
    {
        var path = AssetDatabase.GetAssetPath(prefab);
        return !string.IsNullOrEmpty(path) && path.StartsWith(AdaptedRoot);
    }

    private static void CreateOrUpdateAdaptedPrefabs()
    {
        var tableMaterial = CreateOrLoadMaterial("VRTableTennis_TableGreen", new Color(0.03f, 0.48f, 0.18f), AdaptedMaterialRoot);
        var netMaterial = CreateOrLoadMaterial("VRTableTennis_NetWhite", new Color(0.9f, 0.9f, 0.86f), AdaptedMaterialRoot);
        var paddleMaterial = CreateOrLoadMaterial("VRTableTennis_PaddleRed", new Color(0.66f, 0.08f, 0.07f), AdaptedMaterialRoot);
        var darkMaterial = CreateOrLoadMaterial("VRTableTennis_DarkRubber", new Color(0.02f, 0.02f, 0.02f), AdaptedMaterialRoot);
        var ballMaterial = CreateOrLoadMaterial("VRTableTennis_BallWhite", Color.white, AdaptedMaterialRoot);

        CreateOrUpdateTablePrefab(tableMaterial, netMaterial, darkMaterial);
        CreateOrUpdateNetPrefab(netMaterial);
        CreateOrUpdatePaddlePrefab(paddleMaterial, darkMaterial);
        CreateOrUpdateAdaptedBallPrefab(ballMaterial);
    }

    private static void TryCreateOrUpdateAdaptedPrefabs(bool includeBall)
    {
        var tableMaterial = CreateOrLoadMaterial("VRTableTennis_TableGreen", new Color(0.03f, 0.48f, 0.18f), AdaptedMaterialRoot);
        var netMaterial = CreateOrLoadMaterial("VRTableTennis_NetWhite", new Color(0.9f, 0.9f, 0.86f), AdaptedMaterialRoot);
        var paddleMaterial = CreateOrLoadMaterial("VRTableTennis_PaddleRed", new Color(0.66f, 0.08f, 0.07f), AdaptedMaterialRoot);
        var darkMaterial = CreateOrLoadMaterial("VRTableTennis_DarkRubber", new Color(0.02f, 0.02f, 0.02f), AdaptedMaterialRoot);
        var ballMaterial = CreateOrLoadMaterial("VRTableTennis_BallWhite", Color.white, AdaptedMaterialRoot);

        TryRunAssetStep("PingPongTable_Adapted", () => CreateOrUpdateTablePrefab(tableMaterial, netMaterial, darkMaterial));
        TryRunAssetStep("PingPongNet_Adapted", () => CreateOrUpdateNetPrefab(netMaterial));
        TryRunAssetStep("PingPongPaddle_Adapted", () => CreateOrUpdatePaddlePrefab(paddleMaterial, darkMaterial));

        if (includeBall)
        {
            TryRunAssetStep("PingPongBall_Adapted", () => CreateOrUpdateAdaptedBallPrefab(ballMaterial));
        }
    }

    private static void TryRunAssetStep(string stepName, System.Action action)
    {
        try
        {
            action?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{stepName} generation failed and was skipped. {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void CreateOrUpdateTablePrefab(Material tableMaterial, Material netMaterial, Material darkMaterial)
    {
        var root = new GameObject("PingPongTable_Adapted");
        BuildStandardTableVisual(root.transform, tableMaterial, netMaterial, darkMaterial);

        var tableCollider = root.AddComponent<BoxCollider>();
        tableCollider.size = TableColliderWorldSize;
        tableCollider.center = Vector3.zero;
        ConfigureSurface(root, PingPongSurfaceType.Table);

        var netCollider = new GameObject("NetCollider");
        netCollider.transform.SetParent(root.transform, false);
        netCollider.transform.localPosition = PingPongGeometry.NetLocalCenter;
        var box = netCollider.AddComponent<BoxCollider>();
        box.size = NetColliderWorldSize;
        box.isTrigger = true;
        ConfigureSurface(netCollider, PingPongSurfaceType.Net);

        SaveAdaptedPrefab(root, "PingPongTable");
    }

    private static void BuildStandardTableVisual(Transform root, Material tableMaterial, Material netMaterial, Material darkMaterial)
    {
        ConfigureVisualPrimitive(root, "TableTopVisual", PrimitiveType.Cube, Vector3.zero, Vector3.zero, TableColliderWorldSize, tableMaterial);
        ConfigureVisualPrimitive(root, "NetVisual", PrimitiveType.Cube, PingPongGeometry.NetLocalCenter, Vector3.zero, NetColliderWorldSize, netMaterial);

        var legHeight = PingPongGeometry.TableTopHeight - PingPongGeometry.TableThickness;
        var legCenterY = -PingPongGeometry.TableThickness * 0.5f - legHeight * 0.5f;
        var legOffsetX = PingPongGeometry.TableWidth * 0.5f - 0.09f;
        var legOffsetZ = PingPongGeometry.TableLength * 0.5f - 0.16f;
        var legSize = new Vector3(0.045f, legHeight, 0.045f);

        ConfigureVisualPrimitive(root, "LegFrontLeft", PrimitiveType.Cube, new Vector3(-legOffsetX, legCenterY, -legOffsetZ), Vector3.zero, legSize, darkMaterial);
        ConfigureVisualPrimitive(root, "LegFrontRight", PrimitiveType.Cube, new Vector3(legOffsetX, legCenterY, -legOffsetZ), Vector3.zero, legSize, darkMaterial);
        ConfigureVisualPrimitive(root, "LegBackLeft", PrimitiveType.Cube, new Vector3(-legOffsetX, legCenterY, legOffsetZ), Vector3.zero, legSize, darkMaterial);
        ConfigureVisualPrimitive(root, "LegBackRight", PrimitiveType.Cube, new Vector3(legOffsetX, legCenterY, legOffsetZ), Vector3.zero, legSize, darkMaterial);

        var postHeight = PingPongGeometry.NetHeight + 0.06f;
        var postCenterY = PingPongGeometry.TableThickness * 0.5f + postHeight * 0.5f;
        var postOffsetX = PingPongGeometry.TableWidth * 0.5f + 0.02f;
        var postSize = new Vector3(0.025f, postHeight, 0.025f);

        ConfigureVisualPrimitive(root, "NetPostLeft", PrimitiveType.Cube, new Vector3(-postOffsetX, postCenterY, 0f), Vector3.zero, postSize, darkMaterial);
        ConfigureVisualPrimitive(root, "NetPostRight", PrimitiveType.Cube, new Vector3(postOffsetX, postCenterY, 0f), Vector3.zero, postSize, darkMaterial);
    }

    private static void CreateOrUpdateNetPrefab(Material netMaterial)
    {
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = "PingPongNet_Adapted";
        temp.transform.localScale = NetColliderWorldSize;
        var collider = temp.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        ConfigureSurface(temp, PingPongSurfaceType.Net);

        var renderer = temp.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = netMaterial;

        SaveAdaptedPrefab(temp, "PingPongNet");
    }

    private static void CreateOrUpdatePaddlePrefab(Material paddleMaterial, Material darkMaterial)
    {
        var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>($"{OriginalModelRoot}/PPPaddle.fbx");
        if (sourceModel == null) return;

        var root = new GameObject("PingPongPaddle_Adapted");
        var visual = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
        if (visual != null)
        {
            visual.name = "Visual_PPPaddle";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 4f;
            AssignMaterialsByName(visual, paddleMaterial, paddleMaterial, darkMaterial);
        }

        var collider = root.AddComponent<BoxCollider>();
        collider.center = PaddleColliderCenter;
        collider.size = PaddleColliderSize;
        ConfigureSurface(root, PingPongSurfaceType.PaddleBody);

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var hitZone = new GameObject("PaddleHitZone");
        hitZone.transform.SetParent(root.transform, false);
        var hitZoneCollider = hitZone.AddComponent<BoxCollider>();
        hitZoneCollider.center = PaddleHitZoneCenter;
        hitZoneCollider.size = PaddleHitZoneSize;
        hitZoneCollider.isTrigger = true;
        ConfigureSurface(hitZone, PingPongSurfaceType.PaddleHitZone);

        root.AddComponent<PaddleFollower>();
        ConfigurePaddleTracker(root.AddComponent<PaddleVelocityTracker>());

        SaveAdaptedPrefab(root, "PingPongPaddle");
    }

    private static void CreateOrUpdateAdaptedBallPrefab(Material ballMaterial)
    {
        GameObject temp = null;

        try
        {
            temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "PingPongBall_Adapted";
            temp.transform.localScale = PingPongGeometry.BallPrefabScale;

            var renderer = temp.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = ballMaterial;

            var collider = temp.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = temp.AddComponent<SphereCollider>();
            }
            collider.radius = 0.5f;

            ConfigureBallComponents(temp);
            AttachBounceAudio(temp);

            SaveAdaptedPrefab(temp, "PingPongBall");
            temp = null;
        }
        finally
        {
            if (temp != null)
            {
                Object.DestroyImmediate(temp);
            }
        }
    }

    private static void AssignMaterialsByName(GameObject root, Material primary, Material net, Material dark)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var sourceName = materials[i] != null ? materials[i].name.ToLowerInvariant() : string.Empty;
                if (sourceName.Contains("net") || sourceName.Contains("white"))
                {
                    materials[i] = net;
                }
                else if (sourceName.Contains("black") || sourceName.Contains("dark"))
                {
                    materials[i] = dark;
                }
                else
                {
                    materials[i] = primary;
                }
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static void SaveAdaptedPrefab(GameObject root, string assetName)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }

        PrefabUtility.SaveAsPrefabAsset(root, $"{AdaptedRoot}/{assetName}_Adapted.prefab");
        Object.DestroyImmediate(root);
    }

    private static bool EnsureEditMode()
    {
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) return true;

        Debug.LogError("PingPong scene builder tools must be run in Edit Mode. Exit Play Mode and run the tool again.");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("PingPong", "Please exit Play Mode before running this tool.", "OK");
        }

        return false;
    }

    private static void AttachBounceAudio(GameObject target)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{OriginalAudioRoot}/single_bounce.mp3");
        if (clip == null) return;

        var source = EnsureComponent<AudioSource>(target);
        if (source == null) return;
        source.clip = clip;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
    }

    private static void SetupFeedbackAudio(GameObject feedback, HitFeedbackManager feedbackManager)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{OriginalAudioRoot}/single_bounce.mp3");
        if (clip == null || feedbackManager == null) return;

        var source = feedbackManager.hitAudioSource;
        if (source == null)
        {
            source = EnsureComponent<AudioSource>(feedback);
        }

        if (source == null) return;
        source.clip = clip;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        feedbackManager.hitAudioSource = source;
    }

    private static GameObject LoadOrCreatePrefabAsset(string assetName, PrimitiveType primitiveType, Vector3 scale, Material material)
    {
        var path = $"{PrefabRoot}/{assetName}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var temp = GameObject.CreatePrimitive(primitiveType);
        temp.name = assetName;
        temp.transform.localScale = scale;

        var renderer = temp.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
    }

    private static GameObject CreateOrUpdateBallPrefab()
    {
        var path = $"{PrefabRoot}/PingPongBall.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var ballMaterial = CreateOrLoadMaterial("BallWhite", Color.white);
        GameObject temp = null;

        try
        {
            temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            temp.name = "PingPongBall";
            temp.transform.localScale = PingPongGeometry.BallPrefabScale;

            var renderer = temp.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = ballMaterial;

            var collider = temp.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = temp.AddComponent<SphereCollider>();
            }
            collider.radius = 0.5f;

            ConfigureBallComponents(temp);

            var saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            return saved;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PingPongBall prefab generation failed. {ex.GetType().Name}: {ex.Message}");
            return prefab;
        }
        finally
        {
            if (temp != null)
            {
                Object.DestroyImmediate(temp);
            }
        }
    }

    private static void ConfigureBallComponents(GameObject ball)
    {
        if (ball == null) return;

        var rb = EnsureComponent<Rigidbody>(ball);
        if (rb == null) return;

        ball.transform.localScale = PingPongGeometry.BallPrefabScale;
        rb.mass = PingPongGeometry.BallMass;
        rb.drag = PingPongGeometry.BallDrag;
        rb.angularDrag = PingPongGeometry.BallAngularDrag;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var collider = EnsureComponent<SphereCollider>(ball);
        if (collider != null)
        {
            collider.radius = 0.5f;
            collider.isTrigger = false;
        }

        EnsureComponent<PingPongBall>(ball);
        EnsureComponent<BallLifetime>(ball);
    }

    private static void RepairBallPrefabAsset(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) return;

        ConfigureBallComponents(root);
        AttachBounceAudio(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void RepairExistingBallObjectsInScene()
    {
        foreach (var ball in Object.FindObjectsOfType<PingPongBall>(true))
        {
            ConfigureBallComponents(ball.gameObject);
            EditorUtility.SetDirty(ball.gameObject);
        }

        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name == "PingPongBall" || transform.name == "PingPongBall_Adapted")
            {
                ConfigureBallComponents(transform.gameObject);
                EditorUtility.SetDirty(transform.gameObject);
            }
        }
    }

    private static void ValidateBallSpawnerBindings(BallSpawner spawner)
    {
        if (spawner == null) return;

        var hasError = false;
        if (spawner.ballPrefab == null)
        {
            Debug.LogError("BallSpawner.ballPrefab is not assigned.");
            hasError = true;
        }
        else if (spawner.ballPrefab.GetComponent<Rigidbody>() == null)
        {
            Debug.LogError($"BallSpawner.ballPrefab '{spawner.ballPrefab.name}' has no Rigidbody.");
            hasError = true;
        }

        if (spawner.spawnPoint == null)
        {
            Debug.LogError("BallSpawner.spawnPoint is not assigned.");
            hasError = true;
        }

        if (spawner.targetPoint == null)
        {
            Debug.LogError("BallSpawner.targetPoint is not assigned.");
            hasError = true;
        }

        if (spawner.ballContainer == null)
        {
            Debug.LogError("BallSpawner.ballContainer is not assigned.");
            hasError = true;
        }

        if (!hasError)
        {
            Debug.Log("BallSpawner bindings are valid.");
        }
    }

    private static void RemoveRootLevelGeneratedBallObjects()
    {
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.parent != null) continue;
            if (transform.name != "PingPongBall" && transform.name != "PingPongBall_Adapted") continue;

            Object.DestroyImmediate(transform.gameObject);
        }
    }

    private static GameObject InstantiateOrReuse(string name, GameObject prefab, Transform parent, Vector3 position, Vector3 scale)
    {
        var existing = GameObject.Find(name);
        var go = existing != null ? existing : PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (go == null)
        {
            go = new GameObject(name);
        }

        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        EditorUtility.SetDirty(go);
        return go;
    }

    private static void SetupPaddle(GameObject paddle)
    {
        var rb = EnsureComponent<Rigidbody>(paddle);
        if (rb == null) return;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var paddleCollider = EnsureComponent<BoxCollider>(paddle);
        if (paddleCollider != null)
        {
            paddleCollider.center = PaddleColliderCenter;
            paddleCollider.size = PaddleColliderSize;
            paddleCollider.isTrigger = false;
        }
        ConfigureSurface(paddle, PingPongSurfaceType.PaddleBody);

        var hitZone = GetOrCreateChild("PaddleHitZone", paddle.transform);
        hitZone.transform.localPosition = Vector3.zero;
        hitZone.transform.localRotation = Quaternion.identity;
        hitZone.transform.localScale = Vector3.one;
        var hitZoneCollider = EnsureComponent<BoxCollider>(hitZone);
        if (hitZoneCollider != null)
        {
            hitZoneCollider.center = PaddleHitZoneCenter;
            hitZoneCollider.size = PaddleHitZoneSize;
            hitZoneCollider.isTrigger = true;
        }
        ConfigureSurface(hitZone, PingPongSurfaceType.PaddleHitZone);

        EnsureComponent<PaddleFollower>(paddle);
        ConfigurePaddleTracker(EnsureComponent<PaddleVelocityTracker>(paddle));
    }

    private static void ConfigurePaddleTracker(PaddleVelocityTracker tracker)
    {
        if (tracker == null) return;

        tracker.autoAlignColliders = true;
        tracker.bodyColliderCenter = PaddleColliderCenter;
        tracker.bodyColliderSize = PaddleColliderSize;
        tracker.hitZoneColliderCenter = PaddleHitZoneCenter;
        tracker.hitZoneColliderSize = PaddleHitZoneSize;
    }

    private static void ConfigureSurface(GameObject target, PingPongSurfaceType surfaceType)
    {
        var surface = EnsureComponent<PingPongSurface>(target);
        if (surface == null) return;

        surface.useTypeDefaults = true;
        surface.Configure(surfaceType);
        EditorUtility.SetDirty(target);
    }

    private static GameObject SetupLeftHandGrabVisual(Transform parent)
    {
        var hand = GetOrCreate("Left_GrabHand", parent);
        hand.transform.position = new Vector3(-0.35f, 1.1f, 0.5f);
        hand.transform.localScale = Vector3.one;

        RemoveComponentIfExists<Rigidbody>(hand);
        RemoveComponentIfExists<PaddleFollower>(hand);
        RemoveComponentIfExists<PaddleVelocityTracker>(hand);

        var collider = hand.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        var oldVisual = hand.transform.Find("HandVisual");
        if (oldVisual != null)
        {
            Object.DestroyImmediate(oldVisual.gameObject);
        }

        var handMaterial = CreateOrLoadMaterial("LeftHandSkin", new Color(0.95f, 0.72f, 0.55f));
        ConfigureVisualPrimitive(hand.transform, "Palm", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(0.09f, 0.06f, 0.12f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "Thumb", PrimitiveType.Capsule, new Vector3(0.065f, -0.01f, 0.015f), new Vector3(35f, 0f, -55f), new Vector3(0.022f, 0.05f, 0.022f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "IndexFinger", PrimitiveType.Capsule, new Vector3(0.045f, 0.005f, 0.09f), new Vector3(90f, 0f, 0f), new Vector3(0.018f, 0.065f, 0.018f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "MiddleFinger", PrimitiveType.Capsule, new Vector3(0.015f, 0.008f, 0.1f), new Vector3(90f, 0f, 0f), new Vector3(0.019f, 0.075f, 0.019f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "RingFinger", PrimitiveType.Capsule, new Vector3(-0.017f, 0.005f, 0.09f), new Vector3(90f, 0f, 0f), new Vector3(0.018f, 0.065f, 0.018f), handMaterial);
        ConfigureVisualPrimitive(hand.transform, "LittleFinger", PrimitiveType.Capsule, new Vector3(-0.047f, 0f, 0.078f), new Vector3(90f, 0f, 0f), new Vector3(0.016f, 0.052f, 0.016f), handMaterial);

        var follower = EnsureComponent<ControllerTransformFollower>(hand);
        if (follower != null)
        {
            follower.positionOffset = Vector3.zero;
            follower.rotationOffsetEuler = Vector3.zero;
        }

        var poseAnimator = EnsureComponent<GrabHandPoseAnimator>(hand);
        if (poseAnimator != null)
        {
            poseAnimator.controllerNode = XRNode.LeftHand;
            poseAnimator.closedPoseSpeed = 12f;
            poseAnimator.readControllerGrip = true;
            poseAnimator.mirrorX = true;
            poseAnimator.RebuildPoseCache();
        }

        EditorUtility.SetDirty(hand);
        return hand;
    }

    private static void ConfigureVisualPrimitive(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localRotation, Vector3 localScale, Material material)
    {
        var visual = GetOrCreateChild(name, parent);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.Euler(localRotation);
        visual.transform.localScale = localScale;

        var meshFilter = EnsureComponent<MeshFilter>(visual);
        var meshRenderer = EnsureComponent<MeshRenderer>(visual);
        var collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        if (meshFilter != null)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            meshFilter.sharedMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(primitive);
        }

        if (meshRenderer != null)
        {
            meshRenderer.sharedMaterial = material;
        }
    }

    private static void SetupTablePhysics(GameObject table)
    {
        if (table == null) return;

        RemoveChildIfExists(table.transform, "Visual_PingPongTable");
        var tableMaterial = CreateOrLoadMaterial("VRTableTennis_TableGreen", new Color(0.03f, 0.48f, 0.18f), AdaptedMaterialRoot);
        var netMaterial = CreateOrLoadMaterial("VRTableTennis_NetWhite", new Color(0.9f, 0.9f, 0.86f), AdaptedMaterialRoot);
        var darkMaterial = CreateOrLoadMaterial("VRTableTennis_DarkRubber", new Color(0.02f, 0.02f, 0.02f), AdaptedMaterialRoot);
        BuildStandardTableVisual(table.transform, tableMaterial, netMaterial, darkMaterial);

        var rb = EnsureComponent<Rigidbody>(table);
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        var passiveLock = EnsureComponent<TablePassiveMotionLock>(table);
        if (passiveLock != null)
        {
            passiveLock.AcceptCurrentTransform();
        }

        var tableCollider = EnsureComponent<BoxCollider>(table);
        if (tableCollider != null)
        {
            tableCollider.center = Vector3.zero;
            tableCollider.size = LocalSizeForWorldSize(table.transform, TableColliderWorldSize);
            tableCollider.isTrigger = false;
        }
        ConfigureSurface(table, PingPongSurfaceType.Table);

        var netColliderObject = GetOrCreateChild("NetCollider", table.transform);
        netColliderObject.transform.localPosition = PingPongGeometry.NetLocalCenter;
        netColliderObject.transform.localRotation = Quaternion.identity;
        netColliderObject.transform.localScale = Vector3.one;
        var netCollider = EnsureComponent<BoxCollider>(netColliderObject);
        if (netCollider != null)
        {
            netCollider.center = Vector3.zero;
            netCollider.size = LocalSizeForWorldSize(netColliderObject.transform, NetColliderWorldSize);
            netCollider.isTrigger = true;
        }
        ConfigureSurface(netColliderObject, PingPongSurfaceType.Net);

        foreach (var collider in table.GetComponentsInChildren<BoxCollider>(true))
        {
            if (collider == tableCollider) continue;

            var lowerName = collider.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("net"))
            {
                collider.isTrigger = true;
                collider.size = LocalSizeForWorldSize(collider.transform, NetColliderWorldSize);
                ConfigureSurface(collider.gameObject, PingPongSurfaceType.Net);
            }
        }

        EditorUtility.SetDirty(table);
    }

    private static GameObject SetupPlayerTableBlocker(Transform parent, Transform tableTransform = null)
    {
        var blocker = GetOrCreate("TablePlayerBlocker", parent);
        var tableCenter = tableTransform != null ? tableTransform.position : PingPongGeometry.TableCenter;
        blocker.transform.position = tableCenter + Vector3.up * 0.1f;
        blocker.transform.localRotation = Quaternion.identity;
        blocker.transform.localScale = Vector3.one;

        var collider = EnsureComponent<BoxCollider>(blocker);
        if (collider != null)
        {
            collider.center = Vector3.zero;
            collider.size = new Vector3(TableColliderWorldSize.x + 0.24f, 1.4f, TableColliderWorldSize.z + 0.24f);
            collider.isTrigger = true;
        }

        var renderer = blocker.GetComponent<Renderer>();
        if (renderer != null)
        {
            Object.DestroyImmediate(renderer);
        }

        var boundary = EnsureComponent<PlayerTableBoundary>(blocker);
        if (boundary != null)
        {
            boundary.tableTransform = tableTransform;
            boundary.tableCenter = tableCenter;
            boundary.tableSize = PingPongGeometry.TableBlockerSize(0.24f);
            boundary.margin = 0.12f;
            boundary.moveRigWhenInside = false;
        }

        var surface = EnsureComponent<PingPongSurface>(blocker);
        if (surface != null)
        {
            surface.useTypeDefaults = true;
            surface.Configure(PingPongSurfaceType.Unknown);
        }

        EditorUtility.SetDirty(blocker);
        return blocker;
    }

    private static Vector3 LocalSizeForWorldSize(Transform transform, Vector3 worldSize)
    {
        if (transform == null) return worldSize;

        var scale = transform.lossyScale;
        return new Vector3(
            worldSize.x / Mathf.Max(Mathf.Abs(scale.x), 0.001f),
            worldSize.y / Mathf.Max(Mathf.Abs(scale.y), 0.001f),
            worldSize.z / Mathf.Max(Mathf.Abs(scale.z), 0.001f));
    }

    private static void SetupNet(GameObject net)
    {
        if (net == null) return;

        net.transform.position = PingPongGeometry.TableCenter + PingPongGeometry.NetLocalCenter;
        net.transform.localScale = NetColliderWorldSize;

        var collider = EnsureComponent<BoxCollider>(net);
        if (collider != null)
        {
            collider.center = Vector3.zero;
            collider.size = LocalSizeForWorldSize(net.transform, NetColliderWorldSize);
            collider.isTrigger = true;
        }
        ConfigureSurface(net, PingPongSurfaceType.Net);

        EditorUtility.SetDirty(net);
    }

    private static GameObject SetupOptionalNet(GameObject tablePrefab, Transform parent)
    {
        var existing = GameObject.Find("Net");
        var tableHasNet = IsAdaptedPrefab(tablePrefab);

        if (tableHasNet)
        {
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            return null;
        }

        var netPrefab = LoadAdaptedPrefab("PingPongNet") ??
                        LoadOrCreatePrefabAsset("PingPongNet", PrimitiveType.Cube, NetColliderWorldSize, CreateOrLoadMaterial("NetWhite", new Color(0.88f, 0.88f, 0.88f)));
        var net = InstantiateOrReuse("Net", netPrefab, parent, PingPongGeometry.TableCenter + PingPongGeometry.NetLocalCenter, GetInstanceScale(netPrefab, NetColliderWorldSize));
        SetupNet(net);
        return net;
    }

    private static void BuildUi(Transform parent, ScoreManager score)
    {
        var canvasGo = GetOrCreate("WorldSpaceCanvas", parent);
        var canvas = EnsureComponent<Canvas>(canvasGo);
        if (canvas == null) return;
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.transform.position = new Vector3(0f, 1.6f, 1.2f);
        canvasGo.transform.localScale = Vector3.one * 0.002f;

        EnsureComponent<CanvasScaler>(canvasGo);
        EnsureComponent<GraphicRaycaster>(canvasGo);

        score.hitText = CreateScoreText(canvasGo.transform, "HitText", new Vector2(0f, 90f));
        score.servedText = CreateScoreText(canvasGo.transform, "ServedText", new Vector2(0f, 0f));
        score.accuracyText = CreateScoreText(canvasGo.transform, "AccuracyText", new Vector2(0f, -90f));
    }

    private static TMP_Text CreateScoreText(Transform canvasTransform, string name, Vector2 position)
    {
        var go = GetOrCreate(name, canvasTransform);
        var text = EnsureComponent<TextMeshProUGUI>(go);
        if (text == null) return null;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(450f, 100f);
        rect.anchoredPosition = position;
        text.fontSize = 64;
        text.color = Color.white;
        return text;
    }

    private static Transform BindController(PaddleFollower follower, bool rightHand)
    {
        if (follower == null) return null;

        var controller = FindControllerTransform(rightHand);
        if (controller != null)
        {
            follower.controllerTransform = controller;
            return controller;
        }

        Debug.Log($"{(rightHand ? "Right" : "Left")} hand controller not auto-bound. Please assign XR Origin controller to PaddleFollower.controllerTransform manually.");
        return null;
    }

    private static Transform BindController(ControllerTransformFollower follower, bool rightHand)
    {
        if (follower == null) return null;

        var controller = FindControllerTransform(rightHand);
        if (controller != null)
        {
            follower.controllerTransform = controller;
            return controller;
        }

        Debug.Log($"{(rightHand ? "Right" : "Left")} hand controller not auto-bound. Please assign XR Origin controller to ControllerTransformFollower.controllerTransform manually.");
        return null;
    }

    private static Transform FindControllerTransform(bool rightHand)
    {
        foreach (var t in Object.FindObjectsOfType<Transform>())
        {
            var n = t.name.ToLowerInvariant();
            if (rightHand && (n.Contains("righthand") || n.Contains("right controller") || n.Contains("rightcontroller") || n == "right"))
            {
                return t;
            }

            if (!rightHand && (n.Contains("lefthand") || n.Contains("left controller") || n.Contains("leftcontroller") || n == "left"))
            {
                return t;
            }
        }

        return null;
    }

    private static ControllerBallGrabber SetupControllerBallGrabber(Transform parent, Transform leftController)
    {
        var grabberObject = GetOrCreate("LeftBallGrabber", parent);
        var grabber = EnsureComponent<ControllerBallGrabber>(grabberObject);
        if (grabber == null) return null;

        grabber.controllerTransform = leftController;
        grabber.controllerNode = XRNode.LeftHand;
        grabber.grabRadius = 0.28f;
        grabber.releaseSpeedMultiplier = 1.0f;
        grabber.minimumReleaseSpeed = 0.35f;
        grabber.grabScanInterval = 0.04f;
        grabber.grabLayers = ~0;
        grabber.holdOffset = new Vector3(0f, 0f, 0.08f);
        grabber.suppressGrab = false;
        EditorUtility.SetDirty(grabberObject);
        return grabber;
    }

    private static void SetupControllerTableLimiter(GameObject controllerVisual, Transform tableTransform)
    {
        if (controllerVisual == null) return;

        var limiter = EnsureComponent<ControllerTableCollisionLimiter>(controllerVisual);
        if (limiter == null) return;

        limiter.tableTransform = tableTransform;
        limiter.tableSize = new Vector2(PingPongGeometry.TableWidth, PingPongGeometry.TableLength);
        limiter.tableTopY = PingPongGeometry.TableTopHeight;
        limiter.horizontalMargin = 0.04f;
        limiter.verticalMargin = 0.03f;
        EditorUtility.SetDirty(controllerVisual);
    }

    private static void SetupTableDragHandle(Transform parent, GameObject table, Transform leftController, ControllerBallGrabber leftBallGrabber, BallSpawner spawner, Transform spawn, Transform target, Transform tableBlocker)
    {
        if (table == null) return;

        var handle = GetOrCreate("LeftTableDragHandle", parent);
        handle.transform.SetParent(table.transform, false);
        handle.transform.localPosition = new Vector3(-PingPongGeometry.TableWidth * 0.5f - 0.08f, PingPongGeometry.TableThickness * 0.5f + 0.08f, -PingPongGeometry.TableLength * 0.5f + 0.16f);
        handle.transform.localRotation = Quaternion.identity;
        handle.transform.localScale = Vector3.one;

        var material = CreateOrLoadMaterial("TableDragHandleYellow", new Color(1f, 0.75f, 0.08f));
        ConfigureVisualPrimitive(handle.transform, "HandleVisual", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.11f, material);

        var collider = EnsureComponent<SphereCollider>(handle);
        if (collider != null)
        {
            collider.center = Vector3.zero;
            collider.radius = 0.08f;
            collider.isTrigger = true;
        }

        var dragHandle = EnsureComponent<TableDragHandle>(handle);
        if (dragHandle != null)
        {
            var tableBounceLocalZ = 1.45f - PingPongGeometry.TableCenter.z;
            dragHandle.tableRoot = table.transform;
            dragHandle.controllerTransform = leftController;
            dragHandle.controllerNode = XRNode.LeftHand;
            dragHandle.ballGrabber = leftBallGrabber;
            dragHandle.syncedTransforms = new[] { spawn, target, tableBlocker };
            dragHandle.syncedSpawners = spawner != null ? new[] { spawner } : null;
            dragHandle.activationRadius = 0.2f;
            dragHandle.tableBounceLocalZ = tableBounceLocalZ;
            dragHandle.lockTableHeight = true;
            dragHandle.constrainToBounds = true;
            dragHandle.xBounds = new Vector2(-1.5f, 1.5f);
            dragHandle.zBounds = new Vector2(0.55f, 3.8f);

            if (spawner != null)
            {
                spawner.netWorldZ = table.transform.position.z;
                spawner.tableBounceWorldY = table.transform.position.y + PingPongGeometry.TableThickness * 0.5f + PingPongGeometry.BallRadius;
                spawner.tableBounceWorldZ = table.transform.position.z + tableBounceLocalZ;
                EditorUtility.SetDirty(spawner);
            }
        }

        var passiveLock = EnsureComponent<TablePassiveMotionLock>(table);
        if (passiveLock != null)
        {
            passiveLock.dragHandle = dragHandle;
            passiveLock.AcceptCurrentTransform();
            EditorUtility.SetDirty(table);
        }

        EditorUtility.SetDirty(handle);
    }

    private static void SetupInitialViewAligner(Transform parent)
    {
        var alignerObject = GetOrCreate("InitialViewAligner", parent);
        var aligner = EnsureComponent<VrInitialViewAligner>(alignerObject);
        if (aligner == null) return;

        aligner.desiredHeadWorldPosition = new Vector3(0f, 1.6f, 0.25f);
        aligner.lookAtWorldPosition = new Vector3(0f, PingPongGeometry.TableTopHeight + 0.35f, PingPongGeometry.TableCenter.z);
        aligner.alignOnStart = true;
        aligner.alignPosition = true;
        EditorUtility.SetDirty(alignerObject);
    }

    private static void EnsureFloor(Transform parent)
    {
        var namedFloor = GameObject.Find("Floor");
        if (namedFloor != null)
        {
            namedFloor.transform.SetParent(parent);
            namedFloor.transform.position = Vector3.zero;
            namedFloor.transform.localScale = Vector3.one * 3f;
            if (namedFloor.GetComponent<Collider>() == null) namedFloor.AddComponent<MeshCollider>();
            ConfigureSurface(namedFloor, PingPongSurfaceType.Floor);
            EditorUtility.SetDirty(namedFloor);
            return;
        }

        foreach (var collider in Object.FindObjectsOfType<Collider>())
        {
            var n = collider.gameObject.name.ToLowerInvariant();
            if (n.Contains("floor") || n.Contains("ground") || n.Contains("plane"))
            {
                collider.transform.SetParent(parent);
                ConfigureSurface(collider.gameObject, PingPongSurfaceType.Floor);
                EditorUtility.SetDirty(collider.gameObject);
                return;
            }
        }

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = Vector3.one * 3f;
        ConfigureSurface(floor, PingPongSurfaceType.Floor);
        EditorUtility.SetDirty(floor);
    }

    private static void EnsureLight(Transform parent)
    {
        GameObject primary = null;

        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name != "Directional Light" && transform.name != "DirectionalLight") continue;

            if (primary == null) primary = transform.gameObject;
            ConfigureDirectionalLight(transform.gameObject);
        }

        if (primary == null)
        {
            primary = new GameObject("Directional Light");
            ConfigureDirectionalLight(primary);
        }

        primary.name = "Directional Light";
        primary.transform.SetParent(parent);
        primary.transform.position = Vector3.zero;
        primary.transform.localScale = Vector3.one;
        ConfigureDirectionalLight(primary);
    }

    private static void ConfigureDirectionalLight(GameObject lightGo)
    {
        if (lightGo == null) return;

        var light = EnsureComponent<Light>(lightGo);
        if (light == null) return;

        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        EditorUtility.SetDirty(lightGo);
    }

    private static void EnsureBackWall(Transform parent)
    {
        var backWall = GameObject.Find("BackWall") ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "BackWall";
        backWall.transform.SetParent(parent);
        backWall.transform.position = new Vector3(0f, 1.5f, 4.2f);
        backWall.transform.localScale = new Vector3(6f, 3f, 0.05f);
    }

    private static GameObject GetOrCreate(string name, Transform parent = null)
    {
        var go = GameObject.Find(name) ?? new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        return go;
    }

    private static GameObject GetOrCreateChild(string name, Transform parent)
    {
        if (parent == null) return GameObject.Find(name) ?? new GameObject(name);

        var child = parent.Find(name);
        if (child != null) return child.gameObject;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void RemoveGeneratedObject(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
        {
            Object.DestroyImmediate(go);
        }
    }

    private static void RemoveChildIfExists(Transform parent, string childName)
    {
        if (parent == null) return;

        var child = parent.Find(childName);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void RemoveComponentIfExists<T>(GameObject go) where T : Component
    {
        if (go == null) return;

        var component = go.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go == null) return null;

        var component = go.GetComponent<T>();
        if (component != null) return component;

        try
        {
            component = go.AddComponent<T>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Could not add {typeof(T).Name} component to '{go.name}'. {ex.GetType().Name}: {ex.Message}");
            return null;
        }

        if (component == null)
        {
            Debug.LogError($"Could not add {typeof(T).Name} component to '{go.name}'.");
        }

        return component;
    }
}
