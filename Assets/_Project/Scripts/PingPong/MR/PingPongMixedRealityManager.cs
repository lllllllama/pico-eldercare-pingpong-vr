using UnityEngine;
using Unity.XR.PXR;

#if PICO_OPENXR_SDK
using Unity.XR.OpenXR.Features.PICOSupport;
#endif

[DefaultExecutionOrder(-200)]
public class PingPongMixedRealityManager : MonoBehaviour
{
    public bool enableOnStart = true;
    public bool enableVideoSeeThrough = true;
    public bool configureTransparentCamera = true;
    public bool disableVirtualEnvironment = true;
    public Camera targetCamera;
    public GameObject[] virtualEnvironmentObjects;

    private void Awake()
    {
        if (enableOnStart)
        {
            ApplyMode();
        }
    }

    private void Start()
    {
        if (enableOnStart)
        {
            ApplyMode();
        }
    }

    public void ApplyMode()
    {
        ConfigureCamera();
        ConfigureVirtualEnvironment();
        ConfigurePicoPassthrough();
    }

    private void ConfigureCamera()
    {
        if (!configureTransparentCamera) return;

        var camera = targetCamera != null ? targetCamera : Camera.main;
        if (camera == null)
        {
            camera = FindObjectOfType<Camera>();
        }

        if (camera == null) return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        var clear = camera.backgroundColor;
        clear.a = 0f;
        camera.backgroundColor = clear;
    }

    private void ConfigureVirtualEnvironment()
    {
        if (!disableVirtualEnvironment || virtualEnvironmentObjects == null) return;

        foreach (var environmentObject in virtualEnvironmentObjects)
        {
            if (environmentObject != null)
            {
                environmentObject.SetActive(false);
            }
        }
    }

    private void ConfigurePicoPassthrough()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
#if PICO_OPENXR_SDK
        PassthroughFeature.EnableVideoSeeThrough = enableVideoSeeThrough;
#else
        PXR_Manager.EnableVideoSeeThrough = enableVideoSeeThrough;
#endif
        PXR_MixedReality.EnableVideoSeeThroughEffect(enableVideoSeeThrough);
#else
        if (enableVideoSeeThrough)
        {
            Debug.Log("PingPong MR passthrough is configured. Video see-through starts on a PICO Android runtime.");
        }
#endif
    }
}
