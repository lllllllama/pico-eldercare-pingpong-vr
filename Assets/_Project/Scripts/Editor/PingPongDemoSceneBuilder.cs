using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PingPongDemoSceneBuilder
{
    private const string PrefabRoot = "Assets/_Project/Prefabs/PingPong";
    private const string MaterialRoot = "Assets/_Project/Materials/PingPong";

    [MenuItem("Tools/PICO ElderCare/Build PingPong Demo Scene")]
    public static void BuildDemoScene()
    {
        EnsureFolders();

        var environment = GetOrCreate("Environment");
        var pingPong = GetOrCreate("PingPong");
        var managers = GetOrCreate("Managers");
        var uiRoot = GetOrCreate("UI");

        EnsureFloor(environment.transform);
        EnsureLight(environment.transform);
        EnsureBackWall(environment.transform);

        var tablePrefab = LoadOrCreatePrefabAsset("PingPongTable", PrimitiveType.Cube, new Vector3(2.4f, 0.08f, 1.4f), CreateOrLoadMaterial("TableBlue", new Color(0.07f, 0.3f, 0.47f)));
        var netPrefab = LoadOrCreatePrefabAsset("PingPongNet", PrimitiveType.Cube, new Vector3(2.4f, 0.25f, 0.03f), CreateOrLoadMaterial("NetWhite", new Color(0.88f, 0.88f, 0.88f)));
        var paddlePrefab = LoadOrCreatePrefabAsset("PingPongPaddle", PrimitiveType.Cube, new Vector3(0.35f, 0.05f, 0.25f), CreateOrLoadMaterial("PaddleRed", new Color(0.66f, 0.11f, 0.11f)));
        var ballPrefab = CreateOrUpdateBallPrefab();

        var table = InstantiateOrReuse("Table", tablePrefab, pingPong.transform, new Vector3(0f, 0.75f, 2f), new Vector3(2.4f, 0.08f, 1.4f));
        var net = InstantiateOrReuse("Net", netPrefab, pingPong.transform, new Vector3(0f, 0.95f, 2f), new Vector3(2.4f, 0.25f, 0.03f));
        var paddle = InstantiateOrReuse("Paddle_Right", paddlePrefab, pingPong.transform, new Vector3(0.35f, 1.1f, 0.5f), new Vector3(0.35f, 0.05f, 0.25f));

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
        if (feedback.GetComponent<HitFeedbackManager>() == null) feedback.AddComponent<HitFeedbackManager>();

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
        var matPath = $"{MaterialRoot}/{materialName}.mat";
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
