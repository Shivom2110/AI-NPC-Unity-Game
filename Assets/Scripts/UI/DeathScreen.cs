using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Self-building death screen. No Inspector wiring needed — just have this
/// component exist on any active GameObject. PlayerHealth.Die() calls Show().
/// </summary>
public class DeathScreen : MonoBehaviour
{
    public static DeathScreen Instance { get; private set; }

    private GameObject _panel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<DeathScreen>() != null) return;
        new GameObject("DeathScreen").AddComponent<DeathScreen>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureEventSystem();
        BuildUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void BuildUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("DeathScreenCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Dark overlay panel ────────────────────────────────────────────────
        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        Image panelImg = _panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.82f);

        // ── "YOU DIED" text ───────────────────────────────────────────────────
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(_panel.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0.5f, 0.65f);
        titleRT.anchorMax        = new Vector2(0.5f, 0.65f);
        titleRT.sizeDelta        = new Vector2(600f, 100f);
        titleRT.anchoredPosition = Vector2.zero;
        Text titleText = titleGO.AddComponent<Text>();
        titleText.text      = "YOU DIED";
        titleText.fontSize  = 72;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color     = new Color(0.85f, 0.15f, 0.15f, 1f);
        titleText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── Restart button ────────────────────────────────────────────────────
        Button restartBtn = CreateButton(_panel.transform, "RestartButton",
            "Try Again", new Vector2(0f, -20f),
            new Color(0.15f, 0.55f, 0.15f, 1f));
        restartBtn.onClick.AddListener(Restart);

        // ── Quit button ───────────────────────────────────────────────────────
        Button quitBtn = CreateButton(_panel.transform, "QuitButton",
            "Quit", new Vector2(0f, -90f),
            new Color(0.45f, 0.12f, 0.12f, 1f));
        quitBtn.onClick.AddListener(Quit);

        _panel.SetActive(false);
    }

    private static Button CreateButton(Transform parent, string name, string label,
                                       Vector2 anchoredPos, Color bgColor)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(260f, 56f);
        rt.anchoredPosition = anchoredPos;

        Image img   = btnGO.AddComponent<Image>();
        img.color   = bgColor;
        Button btn  = btnGO.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.highlightedColor = Color.white * 1.25f;
        btn.colors = cb;

        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        Text txt      = textGO.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 28;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btn;
    }

    public void Show()
    {
        if (_panel != null) _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 0.3f;
    }

    private void Restart()
    {
        HideImmediate();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Quit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HideImmediate();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HideImmediate()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        DontDestroyOnLoad(go);
    }
}
