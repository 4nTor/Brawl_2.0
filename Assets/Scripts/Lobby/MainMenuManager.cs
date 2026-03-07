using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string lobbySceneName = "Lobby";

    [Header("Interaction")]
    public float rotationSpeed = 5f;
    private GameObject playerDisplay;
    private bool isDragging = false;
    private float lastMouseX;

    void Start()
    {
        SetupUI();
        SetupVisuals();
        SetupCameraAndPlayer();
        FixEverythingWhite();
    }

    void Update()
    {
        HandleCharacterRotation();
    }

    void HandleCharacterRotation()
    {
        if (playerDisplay == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMouseX = Input.mousePosition.x;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float deltaX = Input.mousePosition.x - lastMouseX;
            playerDisplay.transform.Rotate(Vector3.up, -deltaX * rotationSpeed * Time.deltaTime * 10f);
            lastMouseX = Input.mousePosition.x;
        }
    }

    void SetupUI()
    {
        Button playBtn = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (playBtn != null)
        {
            playBtn.onClick.RemoveAllListeners();
            playBtn.onClick.AddListener(PlayGame);
            playBtn.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 1f);

            var rt = playBtn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 40);
            rt.sizeDelta = new Vector2(250, 70);
        }

        Button quitBtn = GameObject.Find("QuitButton")?.GetComponent<Button>();
        if (quitBtn != null)
        {
            quitBtn.onClick.RemoveAllListeners();
            quitBtn.onClick.AddListener(QuitGame);
            quitBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var rt = quitBtn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -40);
            rt.sizeDelta = new Vector2(250, 70);
        }
    }

    void SetupVisuals()
    {
        var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 5;
        }

        var scaler = GameObject.Find("Canvas")?.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        var title = GameObject.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (title != null)
        {
            title.text = "BRAWL VERSE";
            title.color = Color.yellow;
            title.fontSize = 120;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;

            var rt = title.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 250);
            rt.sizeDelta = new Vector2(1200, 250);
        }

        var playTxt = GameObject.Find("PlayText")?.GetComponent<TextMeshProUGUI>();
        if (playTxt != null)
        {
            playTxt.text = "PLAY";
            playTxt.color = Color.white;
            playTxt.fontSize = 35;
            playTxt.alignment = TextAlignmentOptions.Center;
            var rt = playTxt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        var quitTxt = GameObject.Find("QuitText")?.GetComponent<TextMeshProUGUI>();
        if (quitTxt != null)
        {
            quitTxt.text = "QUIT";
            quitTxt.color = Color.white;
            quitTxt.fontSize = 35;
            quitTxt.alignment = TextAlignmentOptions.Center;
            var rt = quitTxt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    void SetupCameraAndPlayer()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(-4, 2, -6);
            cam.transform.LookAt(new Vector3(0, 1, 0));
        }

        playerDisplay = GameObject.Find("PlayerDisplay");
        if (playerDisplay != null)
        {
            playerDisplay.transform.position = new Vector3(2.5f, 0, 1.0f);
            playerDisplay.transform.rotation = Quaternion.Euler(0, 150, 0);

            var cc = playerDisplay.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var psm = playerDisplay.GetComponent<PlayerStateMachine>();
            if (psm != null) psm.enabled = false;
        }
    }

    void FixEverythingWhite()
    {
        Light light = GameObject.Find("Directional Light")?.GetComponent<Light>();
        if (light != null)
        {
            light.color = new Color(1f, 0.95f, 0.8f);
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
        }

        RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.25f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    public void PlayGame()
    {
        Debug.Log("Loading Lobby: " + lobbySceneName);
        SceneManager.LoadScene(lobbySceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
