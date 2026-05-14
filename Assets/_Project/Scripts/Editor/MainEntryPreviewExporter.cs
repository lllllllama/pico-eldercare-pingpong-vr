using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainEntryPreviewExporter
{
    private const string ScenePath = "Assets/_Project/Scenes/00_MainEntry.unity";
    private const string OutputPath = "Screenshots/main-entry-four-card-preview.png";

    public static void Export()
    {
        Directory.CreateDirectory("Screenshots");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (!scene.IsValid())
        {
            throw new FileNotFoundException("Could not open main entry scene.", ScenePath);
        }

        var camera = Camera.main;
        if (camera == null)
        {
            camera = Object.FindObjectOfType<Camera>();
        }

        if (camera == null)
        {
            throw new MissingReferenceException("Main entry scene has no camera to render.");
        }

        HideControllerPreviewGeometry();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.015f, 0.025f, 1f);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 20f;

        const int width = 1600;
        const int height = 1000;
        var previousTarget = camera.targetTexture;
        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        try
        {
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
            Debug.Log("Exported main entry preview to " + OutputPath);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = null;
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(texture);
        }
    }

    private static void HideControllerPreviewGeometry()
    {
        var renderers = Object.FindObjectsOfType<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            if (IsControllerTransform(renderer.transform))
            {
                renderer.enabled = false;
            }
        }
    }

    private static bool IsControllerTransform(Transform transform)
    {
        while (transform != null)
        {
            var name = transform.name;
            if (name.Contains("Left Controller") ||
                name.Contains("Right Controller") ||
                name.Contains("Controller Model") ||
                name.Contains("PICO Controller"))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }
}
