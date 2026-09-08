using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("Skybox Materials")]
    [SerializeField] private Material[] skyboxMaterials;

    [Header("Startup")]
    [SerializeField] private bool setSkyboxOnStart = true;
    [SerializeField] private int initialSkyboxIndex = 0;

    private void Start()
    {
        if (!setSkyboxOnStart)
            return;

        SetSkybox(initialSkyboxIndex);
    }

    /// <summary>
    /// Sets the active skybox using the supplied index.
    /// Example: SetSkybox(0), SetSkybox(1)
    /// </summary>
    public void SetSkybox(int index)
    {
        if (skyboxMaterials == null || skyboxMaterials.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(SkyboxController)}: No skybox materials assigned.",
                this
            );

            return;
        }

        if (index < 0 || index >= skyboxMaterials.Length)
        {
            Debug.LogWarning(
                $"{nameof(SkyboxController)}: Invalid skybox index {index}. " +
                $"Valid range: 0-{skyboxMaterials.Length - 1}.",
                this
            );

            return;
        }

        Material selectedSkybox = skyboxMaterials[index];

        if (selectedSkybox == null)
        {
            Debug.LogWarning(
                $"{nameof(SkyboxController)}: Skybox material at index {index} is null.",
                this
            );

            return;
        }

        RenderSettings.skybox = selectedSkybox;

        // Update the environment lighting/reflections.
        DynamicGI.UpdateEnvironment();
    }
}