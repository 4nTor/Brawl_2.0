using UnityEngine;
using UnityEngine.Rendering;

public class SceneLightingStateReset : MonoBehaviour
{
    [Header("Optional")]
    public Light sceneSun;

    private void Awake()
    {
        // Reset global state that can leak from previous scenes.
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;

        if (sceneSun == null)
        {
            GameObject sunObj = GameObject.Find("Directional Light");
            if (sunObj != null)
            {
                sceneSun = sunObj.GetComponent<Light>();
            }
        }

        if (sceneSun != null)
        {
            RenderSettings.sun = sceneSun;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.Skybox;
        }

        DynamicGI.UpdateEnvironment();
    }
}

