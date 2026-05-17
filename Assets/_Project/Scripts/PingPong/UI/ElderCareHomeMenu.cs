using UnityEngine;
using UnityEngine.UI;

public class ElderCareHomeMenu : MonoBehaviour
{
    public GameObject homeRoot;
    public GameObject[] pingPongGameplayRoots;
    public BallSpawner ballSpawner;
    public ScoreManager scoreManager;
    public VrInitialViewAligner initialViewAligner;
    public ComfortWorldSpaceUIPlacer uiPlacer;
    public Text statusText;
    public ElderCareModuleCard[] moduleCards;
    public Font uiFont;
    public bool showHomeOnStart = true;
    public bool clearBallsWhenLeavingPingPong = true;
    public bool placeHomeUiOnShow = true;

    private Font _runtimeFont;

    private void Awake()
    {
        if (homeRoot == null)
        {
            homeRoot = gameObject;
        }

        if (uiPlacer == null)
        {
            uiPlacer = homeRoot.GetComponentInParent<ComfortWorldSpaceUIPlacer>();
        }

        ApplyReadableFont(homeRoot);
    }

    private void Start()
    {
        if (showHomeOnStart)
        {
            ShowHome();
        }
        else
        {
            StartPingPongModule();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowHome();
        }
    }

    public void SelectModule(string moduleId, string moduleTitle)
    {
        if (moduleId == "pingpong")
        {
            StartPingPongModule();
            return;
        }

        ShowFutureModule(moduleTitle);
    }

    public void ShowHome()
    {
        SetHomeActive(true);
        SetPingPongGameplayActive(false);
        PlaceHomeUiIfNeeded();

        if (ballSpawner != null)
        {
            ballSpawner.StopServing();
            if (clearBallsWhenLeavingPingPong)
            {
                ballSpawner.ClearBalls();
            }
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        SetStatus("使用手柄或手势选择功能");
    }

    public void ResetHomeUiPosition()
    {
        if (uiPlacer != null)
        {
            uiPlacer.PlaceInFrontOfUser();
        }
    }

    public void StartPingPongModule()
    {
        SetHomeActive(false);
        SetPingPongGameplayActive(true);

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        if (ballSpawner != null)
        {
            ballSpawner.ClearBalls();
            ballSpawner.StartServing();
        }

        if (initialViewAligner != null)
        {
            initialViewAligner.AlignNow();
        }
    }

    private void ShowFutureModule(string moduleTitle)
    {
        SetHomeActive(true);
        SetPingPongGameplayActive(false);

        if (ballSpawner != null)
        {
            ballSpawner.StopServing();
            ballSpawner.ClearBalls();
        }

        SetStatus($"{moduleTitle} 功能正在接入");
    }

    private void SetHomeActive(bool active)
    {
        if (homeRoot != null)
        {
            homeRoot.SetActive(active);
        }
    }

    private void SetPingPongGameplayActive(bool active)
    {
        if (pingPongGameplayRoots == null) return;

        foreach (var gameplayRoot in pingPongGameplayRoots)
        {
            if (gameplayRoot != null)
            {
                gameplayRoot.SetActive(active);
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void PlaceHomeUiIfNeeded()
    {
        if (!placeHomeUiOnShow || uiPlacer == null) return;

        uiPlacer.PlaceInFrontOfUser();
    }

    private void ApplyReadableFont(GameObject root)
    {
        if (root == null) return;

        var texts = root.GetComponentsInChildren<Text>(true);
        foreach (var text in texts)
        {
            text.font = ResolveUiFont();
        }
    }

    private Font ResolveUiFont()
    {
        return uiFont != null ? uiFont : GetRuntimeFont();
    }

    private Font GetRuntimeFont()
    {
        if (_runtimeFont != null) return _runtimeFont;

        _runtimeFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Source Han Sans SC", "Arial" },
            64);

        if (_runtimeFont == null)
        {
            _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return _runtimeFont;
    }
}
