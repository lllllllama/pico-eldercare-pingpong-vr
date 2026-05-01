using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
                          LoadOrCreatePrefabAsset("PingPongTable", PrimitiveType.Cube, new Vector3(2.4f, 0.08f, 1.4f), CreateOrLoadMaterial("TableBlue", new Color(0.07f, 0.3f, 0.47f)));
        var paddlePrefab = LoadAdaptedPrefab("PingPongPaddle") ??
                           LoadOrCreatePrefabAsset("PingPongPaddle", PrimitiveType.Cube, new Vector3(0.35f, 0.05f, 0.25f), CreateOrLoadMaterial("PaddleRed", new Color(0.66f, 0.11f, 0.11f)));
        var ballPrefab = CreateOrUpdateBallPrefab();
        if (ballPrefab == null)
        {
            Debug.LogError("Could not create or load PingPong ball prefab. Demo scene generation stopped.");
            return;
        }

        var table = InstantiateOrReuse("Table", tablePrefab, pingPong.transform, new Vector3(0f, 0.75f, 2f), GetInstanceScale(tablePrefab, new Vector3(2.4f, 0.08f, 1.4f)));
        var net = SetupOptionalNet(tablePrefab, pingPong.transform);
        var paddle = InstantiateOrReuse("Paddle_Right", paddlePrefab, pingPong.transform, new Vector3(0.35f, 1.1f, 0.5f), GetInstanceScale(paddlePrefab, new Vector3(0.35f, 0.05f, 0.25f)));

        var spawn = GetOrCreate("BallSpawnPoint", pingPong.transform);
        spawn.transform.position = new Vector3(0f, 1.25f, 3.05f);
        var target = GetOrCreate("BallTargetPoint", pingPong.transform);
        target.transform.position = new Vector3(0.2f, 1.15f, 0.7f);
        var ballContainer = GetOrCreate("BallContainer", pingPong.transform);

        SetupPaddle(paddle);

        var spawnerObject = GetOrCreate("BallSpawner", managers.transform);
        var spawner = EnsureComponent<BallSpawner>(spawnerObject);
        if (spawner == null) return;
        spawner.ballPrefab = ballPrefab;
        spawner.spawnPoint = spawn.transform;
        spawner.targetPoint = target.transform;
        spawner.ballContainer = ballContainer.transform;
        spawner.autoStartOnPlay = true;
        spawner.serveSpeed = 2.7f;
        spawner.minimumNetClearanceHeight = 1.3f;
        spawner.netWorldZ = 2f;
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
        BindRightController(paddle.GetComponent<PaddleFollower>());

        EditorUtility.SetDirty(table);
        if (net != null) EditorUtility.SetDirty(net);
        EditorUtility.SetDirty(paddle);
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
        var environment = GetOrCreate("Environment");
        EnsureFloor(environment.transform);
        EnsureLight(environment.transform);
        EnsureBackWall(environment.transform);
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
        var path = AssetDatabase.GetAssetPath(prefab);
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
        var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>($"{OriginalModelRoot}/PingPondTable.fbx");
        if (sourceModel == null) return;

        var root = new GameObject("PingPongTable_Adapted");
        var visual = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
        if (visual != null)
        {
            visual.name = "Visual_PingPondTable";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
            visual.transform.localScale = Vector3.one * 40f;
            AssignMaterialsByName(visual, tableMaterial, netMaterial, darkMaterial);
        }

        var tableCollider = root.AddComponent<BoxCollider>();
        tableCollider.size = new Vector3(2.4f, 0.08f, 1.4f);
        tableCollider.center = Vector3.zero;

        var netCollider = new GameObject("NetCollider");
        netCollider.transform.SetParent(root.transform, false);
        netCollider.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        var box = netCollider.AddComponent<BoxCollider>();
        box.size = new Vector3(2.4f, 0.25f, 0.03f);

        SaveAdaptedPrefab(root, "PingPongTable");
    }

    private static void CreateOrUpdateNetPrefab(Material netMaterial)
    {
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = "PingPongNet_Adapted";
        temp.transform.localScale = new Vector3(2.4f, 0.25f, 0.03f);

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
        collider.size = new Vector3(0.35f, 0.05f, 0.25f);
        collider.center = Vector3.zero;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        root.AddComponent<PaddleFollower>();
        root.AddComponent<PaddleVelocityTracker>();

        SaveAdaptedPrefab(root, "PingPongPaddle");
    }

    private static void CreateOrUpdateAdaptedBallPrefab(Material ballMaterial)
    {
        GameObject temp = null;

        try
        {
            temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "PingPongBall_Adapted";
            temp.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);

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
            temp.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);

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

        rb.mass = 0.0027f;
        rb.drag = 0.03f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (ball.GetComponent<Collider>() == null) EnsureComponent<SphereCollider>(ball);
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

        if (paddle.GetComponent<Collider>() == null) EnsureComponent<BoxCollider>(paddle);
        EnsureComponent<PaddleFollower>(paddle);
        EnsureComponent<PaddleVelocityTracker>(paddle);
    }

    private static void SetupNet(GameObject net)
    {
        if (net == null) return;

        net.transform.position = new Vector3(0f, 0.95f, 2f);
        net.transform.localScale = new Vector3(2.4f, 0.25f, 0.03f);

        var collider = EnsureComponent<BoxCollider>(net);
        if (collider != null)
        {
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
        }

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
                        LoadOrCreatePrefabAsset("PingPongNet", PrimitiveType.Cube, new Vector3(2.4f, 0.25f, 0.03f), CreateOrLoadMaterial("NetWhite", new Color(0.88f, 0.88f, 0.88f)));
        var net = InstantiateOrReuse("Net", netPrefab, parent, new Vector3(0f, 0.95f, 2f), GetInstanceScale(netPrefab, new Vector3(2.4f, 0.25f, 0.03f)));
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

        TMP_Text MakeText(string name, Vector2 pos)
        {
            var go = GetOrCreate(name, canvasGo.transform);
            var text = EnsureComponent<TextMeshProUGUI>(go);
            if (text == null) return null;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(450f, 100f);
            rect.anchoredPosition = pos;
            text.fontSize = 64;
            text.color = Color.white;
            return text;
        }

        score.hitText = MakeText("HitText", new Vector2(0f, 90f));
        score.servedText = MakeText("ServedText", new Vector2(0f, 0f));
        score.accuracyText = MakeText("AccuracyText", new Vector2(0f, -90f));
    }

    private static void BindRightController(PaddleFollower follower)
    {
        if (follower == null) return;

        foreach (var t in Object.FindObjectsOfType<Transform>())
        {
            var n = t.name.ToLowerInvariant();
            if (n.Contains("righthand") || n.Contains("right controller") || n.Contains("rightcontroller") || n == "right")
            {
                follower.controllerTransform = t;
                return;
            }
        }

        Debug.Log("Right hand controller not auto-bound. Please assign XR Origin right-hand controller to PaddleFollower.controllerTransform manually.");
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
            EditorUtility.SetDirty(namedFloor);
            return;
        }

        foreach (var collider in Object.FindObjectsOfType<Collider>())
        {
            var n = collider.gameObject.name.ToLowerInvariant();
            if (n.Contains("floor") || n.Contains("ground") || n.Contains("plane"))
            {
                collider.transform.SetParent(parent);
                EditorUtility.SetDirty(collider.gameObject);
                return;
            }
        }

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = Vector3.one * 3f;
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
