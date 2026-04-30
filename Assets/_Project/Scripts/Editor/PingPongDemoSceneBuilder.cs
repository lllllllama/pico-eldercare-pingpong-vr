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
        EnsureFolders();
        CreateOrUpdateAdaptedPrefabs();
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
        EnsureFolders();
        CreateOrUpdateAdaptedPrefabs();

        var environment = GetOrCreate("Environment");
        var pingPong = GetOrCreate("PingPong");
        var managers = GetOrCreate("Managers");
        var uiRoot = GetOrCreate("UI");

        EnsureFloor(environment.transform);
        EnsureLight(environment.transform);
        EnsureBackWall(environment.transform);

        var tablePrefab = LoadAdaptedPrefab("PingPongTable") ??
                          LoadOrCreatePrefabAsset("PingPongTable", PrimitiveType.Cube, new Vector3(2.4f, 0.08f, 1.4f), CreateOrLoadMaterial("TableBlue", new Color(0.07f, 0.3f, 0.47f)));
        var netPrefab = LoadAdaptedPrefab("PingPongNet") ??
                        LoadOrCreatePrefabAsset("PingPongNet", PrimitiveType.Cube, new Vector3(2.4f, 0.25f, 0.03f), CreateOrLoadMaterial("NetWhite", new Color(0.88f, 0.88f, 0.88f)));
        var paddlePrefab = LoadAdaptedPrefab("PingPongPaddle") ??
                           LoadOrCreatePrefabAsset("PingPongPaddle", PrimitiveType.Cube, new Vector3(0.35f, 0.05f, 0.25f), CreateOrLoadMaterial("PaddleRed", new Color(0.66f, 0.11f, 0.11f)));
        var ballPrefab = LoadAdaptedPrefab("PingPongBall") ?? CreateOrUpdateBallPrefab();

        var table = InstantiateOrReuse("Table", tablePrefab, pingPong.transform, new Vector3(0f, 0.75f, 2f), GetInstanceScale(tablePrefab, new Vector3(2.4f, 0.08f, 1.4f)));
        var net = InstantiateOrReuse("Net", netPrefab, pingPong.transform, new Vector3(0f, 0.95f, 2f), GetInstanceScale(netPrefab, new Vector3(2.4f, 0.25f, 0.03f)));
        var paddle = InstantiateOrReuse("Paddle_Right", paddlePrefab, pingPong.transform, new Vector3(0.35f, 1.1f, 0.5f), GetInstanceScale(paddlePrefab, new Vector3(0.35f, 0.05f, 0.25f)));

        var spawn = GetOrCreate("BallSpawnPoint", pingPong.transform);
        spawn.transform.position = new Vector3(0f, 1.15f, 3f);
        var target = GetOrCreate("BallTargetPoint", pingPong.transform);
        target.transform.position = new Vector3(0.25f, 1.1f, 0.8f);
        var ballContainer = GetOrCreate("BallContainer", pingPong.transform);

        SetupPaddle(paddle);

        var spawnerObject = GetOrCreate("BallSpawner", managers.transform);
        var spawner = spawnerObject.GetComponent<BallSpawner>() ?? spawnerObject.AddComponent<BallSpawner>();
        spawner.ballPrefab = ballPrefab;
        spawner.spawnPoint = spawn.transform;
        spawner.targetPoint = target.transform;
        spawner.ballContainer = ballContainer.transform;
        spawner.autoStartOnPlay = true;

        var scoreObject = GetOrCreate("ScoreManager", managers.transform);
        var scoreManager = scoreObject.GetComponent<ScoreManager>() ?? scoreObject.AddComponent<ScoreManager>();

        var feedback = GetOrCreate("HitFeedbackManager", managers.transform);
        var feedbackManager = feedback.GetComponent<HitFeedbackManager>() ?? feedback.AddComponent<HitFeedbackManager>();
        SetupFeedbackAudio(feedback, feedbackManager);

        BuildUi(uiRoot.transform, scoreManager);
        BindRightController(paddle.GetComponent<PaddleFollower>());

        EditorUtility.SetDirty(table);
        EditorUtility.SetDirty(net);
        EditorUtility.SetDirty(paddle);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("PingPong", "PingPong Demo scene objects and prefab assets are ready.", "OK");
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

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (material.shader == null)
        {
            material = new Material(Shader.Find("Standard"));
        }
        material.color = color;
        AssetDatabase.CreateAsset(material, matPath);
        return material;
    }

    private static GameObject LoadAdaptedPrefab(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{AdaptedRoot}/{assetName}_Adapted.prefab");
    }

    private static Vector3 GetInstanceScale(GameObject prefab, Vector3 fallbackScale)
    {
        var path = AssetDatabase.GetAssetPath(prefab);
        return !string.IsNullOrEmpty(path) && path.StartsWith(AdaptedRoot)
            ? Vector3.one
            : fallbackScale;
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
        var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        temp.name = "PingPongBall_Adapted";
        temp.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);

        var renderer = temp.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = ballMaterial;

        var collider = temp.GetComponent<SphereCollider>() ?? temp.AddComponent<SphereCollider>();
        collider.radius = 0.5f;

        var rb = temp.GetComponent<Rigidbody>() ?? temp.AddComponent<Rigidbody>();
        rb.mass = 0.0027f;
        rb.drag = 0.03f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        temp.AddComponent<PingPongBall>();
        temp.AddComponent<BallLifetime>();
        AttachBounceAudio(temp);

        SaveAdaptedPrefab(temp, "PingPongBall");
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

    private static void AttachBounceAudio(GameObject target)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{OriginalAudioRoot}/single_bounce.mp3");
        if (clip == null) return;

        var source = target.GetComponent<AudioSource>() ?? target.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
    }

    private static void SetupFeedbackAudio(GameObject feedback, HitFeedbackManager feedbackManager)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{OriginalAudioRoot}/single_bounce.mp3");
        if (clip == null || feedbackManager == null) return;

        var source = feedbackManager.hitAudioSource ?? feedback.GetComponent<AudioSource>() ?? feedback.AddComponent<AudioSource>();
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

        var temp = prefab != null
            ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        temp.name = "PingPongBall";
        temp.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);

        var renderer = temp.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = ballMaterial;

        var collider = temp.GetComponent<SphereCollider>() ?? temp.AddComponent<SphereCollider>();
        collider.radius = 0.5f;

        var rb = temp.GetComponent<Rigidbody>() ?? temp.AddComponent<Rigidbody>();
        rb.mass = 0.0027f;
        rb.drag = 0.03f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (temp.GetComponent<PingPongBall>() == null) temp.AddComponent<PingPongBall>();
        if (temp.GetComponent<BallLifetime>() == null) temp.AddComponent<BallLifetime>();

        var saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return saved;
    }

    private static GameObject InstantiateOrReuse(string name, GameObject prefab, Transform parent, Vector3 position, Vector3 scale)
    {
        var existing = GameObject.Find(name);
        var go = existing != null ? existing : PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        return go;
    }

    private static void SetupPaddle(GameObject paddle)
    {
        var rb = paddle.GetComponent<Rigidbody>() ?? paddle.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (paddle.GetComponent<Collider>() == null) paddle.AddComponent<BoxCollider>();
        if (paddle.GetComponent<PaddleFollower>() == null) paddle.AddComponent<PaddleFollower>();
        if (paddle.GetComponent<PaddleVelocityTracker>() == null) paddle.AddComponent<PaddleVelocityTracker>();
    }

    private static void BuildUi(Transform parent, ScoreManager score)
    {
        var canvasGo = GetOrCreate("WorldSpaceCanvas", parent);
        var canvas = canvasGo.GetComponent<Canvas>() ?? canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.transform.position = new Vector3(0f, 1.6f, 1.2f);
        canvasGo.transform.localScale = Vector3.one * 0.002f;

        if (canvasGo.GetComponent<CanvasScaler>() == null) canvasGo.AddComponent<CanvasScaler>();
        if (canvasGo.GetComponent<GraphicRaycaster>() == null) canvasGo.AddComponent<GraphicRaycaster>();

        TMP_Text MakeText(string name, Vector2 pos)
        {
            var go = GetOrCreate(name, canvasGo.transform);
            var text = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
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
        if (namedFloor != null) return;

        foreach (var collider in Object.FindObjectsOfType<Collider>())
        {
            var n = collider.gameObject.name.ToLowerInvariant();
            if (n.Contains("floor") || n.Contains("ground") || n.Contains("plane"))
            {
                return;
            }
        }

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = Vector3.one * 3f;
    }

    private static void EnsureLight(Transform parent)
    {
        if (Object.FindObjectOfType<Light>() != null) return;

        var lightGo = GetOrCreate("Directional Light", parent);
        var light = lightGo.GetComponent<Light>() ?? lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
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
}
