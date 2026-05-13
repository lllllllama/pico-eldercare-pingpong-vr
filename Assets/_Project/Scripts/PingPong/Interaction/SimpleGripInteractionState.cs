using UnityEngine;

public enum SimpleGripInteractionMode
{
    None,
    RemoteTableDrag,
    BallGrab
}

[DefaultExecutionOrder(-90)]
public class SimpleGripInteractionState : MonoBehaviour
{
    private static SimpleGripInteractionState _instance;

    [SerializeField]
    private SimpleGripInteractionMode currentMode = SimpleGripInteractionMode.None;

    public static SimpleGripInteractionState Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SimpleGripInteractionState>(true);
            }

            return _instance;
        }
    }

    public static SimpleGripInteractionMode CurrentMode
    {
        get
        {
            var instance = Instance;
            return instance != null ? instance.currentMode : SimpleGripInteractionMode.None;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(SimpleGripInteractionState)} found on {name}; keeping the first instance.");
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public bool TryBegin(SimpleGripInteractionMode mode)
    {
        if (mode == SimpleGripInteractionMode.None)
        {
            return false;
        }

        if (currentMode != SimpleGripInteractionMode.None)
        {
            LogGripIgnored(currentMode);
            return false;
        }

        currentMode = mode;
        Debug.Log($"Begin {mode}");
        return true;
    }

    public bool End(SimpleGripInteractionMode mode)
    {
        if (mode == SimpleGripInteractionMode.None || currentMode != mode)
        {
            return false;
        }

        Debug.Log($"End {mode}");
        currentMode = SimpleGripInteractionMode.None;
        return true;
    }

    public void ResetState()
    {
        currentMode = SimpleGripInteractionMode.None;
    }

    public static bool TryBegin(SimpleGripInteractionState state, SimpleGripInteractionMode mode)
    {
        var target = state != null ? state : EnsureInstance();
        return target != null && target.TryBegin(mode);
    }

    public static bool End(SimpleGripInteractionState state, SimpleGripInteractionMode mode)
    {
        var target = state != null ? state : Instance;
        return target != null && target.End(mode);
    }

    public static void LogGripIgnored(SimpleGripInteractionMode blockingMode)
    {
        if (blockingMode != SimpleGripInteractionMode.None)
        {
            Debug.Log($"Grip ignored because current mode is {blockingMode}");
        }
    }

    public static SimpleGripInteractionState EnsureInstance()
    {
        var instance = Instance;
        if (instance != null)
        {
            return instance;
        }

        var go = new GameObject(nameof(SimpleGripInteractionState));
        _instance = go.AddComponent<SimpleGripInteractionState>();
        return _instance;
    }
}
