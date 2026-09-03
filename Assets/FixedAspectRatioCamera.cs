using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
public sealed class FixedAspectRatioCamera : MonoBehaviour
{
    private const float DefaultTargetAspectRatio = 16f / 9f;
    private const string BackgroundCameraName = "Aspect Ratio Background Camera";

    [Header("Aspect Ratio")]
    [SerializeField]
    [Min(0.1f)]
    private float targetAspectRatio = DefaultTargetAspectRatio;

    [Header("Background")]
    [SerializeField]
    private Color backgroundColor = Color.black;

    private Camera mainCamera;
    private UniversalAdditionalCameraData mainCameraData;

    private Camera backgroundCamera;
    private UniversalAdditionalCameraData backgroundCameraData;

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void Update()
    {
        if (Screen.width == lastScreenWidth &&
            Screen.height == lastScreenHeight)
        {
            return;
        }

        ApplyViewport();
    }

    private void Initialize()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }

        if (mainCameraData == null)
        {
            mainCameraData =
                GetComponent<UniversalAdditionalCameraData>();
        }

        if (mainCamera == null || mainCameraData == null)
        {
            return;
        }

        ConfigureMainCamera();
        CreateOrFindBackgroundCamera();
        ConfigureBackgroundCamera();
        ApplyViewport();
    }

    private void ConfigureMainCamera()
    {
        mainCameraData.renderType = CameraRenderType.Overlay;
    }

    private void CreateOrFindBackgroundCamera()
    {
        Transform existingBackground =
            transform.Find(BackgroundCameraName);

        if (existingBackground != null)
        {
            backgroundCamera =
                existingBackground.GetComponent<Camera>();

            backgroundCameraData =
                existingBackground
                    .GetComponent<UniversalAdditionalCameraData>();
        }

        if (backgroundCamera != null &&
            backgroundCameraData != null)
        {
            return;
        }

        GameObject backgroundObject =
            new GameObject(BackgroundCameraName);

        backgroundObject.transform.SetParent(transform, false);

        backgroundCamera =
            backgroundObject.AddComponent<Camera>();

        backgroundCameraData =
            backgroundObject
                .AddComponent<UniversalAdditionalCameraData>();
    }

    private void ConfigureBackgroundCamera()
    {
        if (backgroundCamera == null ||
            backgroundCameraData == null)
        {
            return;
        }

        backgroundCameraData.renderType =
            CameraRenderType.Base;

        backgroundCamera.clearFlags =
            CameraClearFlags.SolidColor;

        backgroundCamera.backgroundColor =
            backgroundColor;

        // Background camera only clears the screen.
        backgroundCamera.cullingMask = 0;

        // Full screen.
        backgroundCamera.rect =
            new Rect(0f, 0f, 1f, 1f);

        AddMainCameraToStack();
    }

    private void AddMainCameraToStack()
    {
        if (backgroundCameraData == null)
        {
            return;
        }

        // cameraStack is read-only, but the List itself
        // can be modified.
        List<Camera> cameraStack =
            backgroundCameraData.cameraStack;

        if (cameraStack == null)
        {
            return;
        }

        if (!cameraStack.Contains(mainCamera))
        {
            cameraStack.Add(mainCamera);
        }
    }

    private void ApplyViewport()
    {
        if (mainCamera == null)
        {
            return;
        }

        if (Screen.width <= 0 ||
            Screen.height <= 0)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float screenAspect =
            (float)Screen.width / Screen.height;

        if (screenAspect > targetAspectRatio)
        {
            ApplyHorizontalLetterboxing(screenAspect);
        }
        else
        {
            ApplyVerticalLetterboxing(screenAspect);
        }

        // Background camera always covers the complete display.
        if (backgroundCamera != null)
        {
            backgroundCamera.rect =
                new Rect(0f, 0f, 1f, 1f);

            backgroundCamera.backgroundColor =
                backgroundColor;
        }
    }

    private void ApplyHorizontalLetterboxing(float screenAspect)
    {
        float viewportWidth =
            targetAspectRatio / screenAspect;

        float xOffset =
            (1f - viewportWidth) * 0.5f;

        mainCamera.rect =
            new Rect(
                xOffset,
                0f,
                viewportWidth,
                1f
            );
    }

    private void ApplyVerticalLetterboxing(float screenAspect)
    {
        float viewportHeight =
            screenAspect / targetAspectRatio;

        float yOffset =
            (1f - viewportHeight) * 0.5f;

        mainCamera.rect =
            new Rect(
                0f,
                yOffset,
                1f,
                viewportHeight
            );
    }

    private void OnDisable()
    {
        if (mainCamera != null)
        {
            mainCamera.rect =
                new Rect(0f, 0f, 1f, 1f);
        }

        RemoveMainCameraFromStack();
    }

    private void RemoveMainCameraFromStack()
    {
        if (backgroundCameraData == null)
        {
            return;
        }

        List<Camera> cameraStack =
            backgroundCameraData.cameraStack;

        if (cameraStack != null)
        {
            cameraStack.Remove(mainCamera);
        }
    }
}