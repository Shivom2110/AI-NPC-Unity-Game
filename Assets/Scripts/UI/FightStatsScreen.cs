using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-building post-fight stats panel.
/// Shows on VICTORY (boss defeated). Player death is handled by DeathScreen.
/// No Inspector wiring needed — bootstraps itself at scene load.
/// </summary>
public class FightStatsScreen : MonoBehaviour
{
    public static FightStatsScreen Instance { get; private set; }

    private GameObject _panel;
    private Text _titleText;
    private Text _timeText;
    private Text _skillText;
    private Text _parryText;
    private Text _dodgeText;
    private Text _damageDealtText;
    private Text _damageTakenText;
    private Text _comboText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<FightStatsScreen>() != null) return;
        new GameObject("FightStatsScreen").AddComponent<FightStatsScreen>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnEnable()  => CombatEventSystem.OnFightEnd += OnFightEnd;
    private void OnDisable() => CombatEventSystem.OnFightEnd -= OnFightEnd;

    // ── UI Construction ────────────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("FightStatsCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Dark overlay ──────────────────────────────────────────────────────
        _panel = new GameObject("StatsPanel");
        _panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot     = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(520f, 560f);
        panelRT.anchoredPosition = Vector2.zero;
        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.08f, 0.97f);

        // Gold top edge
        GameObject edge = new GameObject("TopEdge");
        edge.transform.SetParent(_panel.transform, false);
        RectTransform edgeRT = edge.AddComponent<RectTransform>();
        edgeRT.anchorMin = new Vector2(0f, 1f);
        edgeRT.anchorMax = new Vector2(1f, 1f);
        edgeRT.pivot     = new Vector2(0.5f, 1f);
        edgeRT.sizeDelta = new Vector2(0f, 5f);
        edgeRT.anchoredPosition = Vector2.zero;
        Image edgeImg = edge.AddComponent<Image>();
        edgeImg.color = new Color(0.82f, 0.68f, 0.18f);

        // ── Title ─────────────────────────────────────────────────────────────
        _titleText = MakeText(_panel.transform, "TitleText", "VICTORY",
            52, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Color(0.85f, 0.75f, 0.20f), new Vector2(0f, 248f), new Vector2(480f, 64f));

        // ── Divider ───────────────────────────────────────────────────────────
        MakeDivider(_panel.transform, new Vector2(0f, 192f));

        // ── Stat rows ─────────────────────────────────────────────────────────
        float y = 148f;
        float step = 46f;

        _timeText       = MakeStatRow(_panel.transform, "TIME SURVIVED",     ref y, step);
        _skillText      = MakeStatRow(_panel.transform, "SKILL SCORE",       ref y, step);
        _parryText      = MakeStatRow(_panel.transform, "PARRY SUCCESS",     ref y, step);
        _dodgeText      = MakeStatRow(_panel.transform, "DODGE SUCCESS",     ref y, step);
        _damageDealtText = MakeStatRow(_panel.transform, "DAMAGE DEALT",     ref y, step);
        _damageTakenText = MakeStatRow(_panel.transform, "DAMAGE TAKEN",     ref y, step);
        _comboText      = MakeStatRow(_panel.transform, "BEST COMBO",        ref y, step);

        // ── Divider ───────────────────────────────────────────────────────────
        MakeDivider(_panel.transform, new Vector2(0f, y + 8f));

        // ── Continue button ───────────────────────────────────────────────────
        Button continueBtn = MakeButton(_panel.transform, "ContinueBtn", "Continue",
            new Vector2(0f, -238f), new Color(0.14f, 0.42f, 0.14f));
        continueBtn.onClick.AddListener(Close);

        // ── Restart button ────────────────────────────────────────────────────
        Button restartBtn = MakeButton(_panel.transform, "RestartBtn", "Fight Again",
            new Vector2(0f, -292f), new Color(0.30f, 0.20f, 0.05f));
        restartBtn.onClick.AddListener(Restart);

        _panel.SetActive(false);
    }

    // ── Event handler ──────────────────────────────────────────────────────────

    private void OnFightEnd(bool playerWon, float fightDuration, float finalSkillScore)
    {
        if (!playerWon) return;  // DeathScreen handles player defeats

        // Pull live snapshot for detailed stats
        CombatAnalyticsSnapshot snap = CombatTracker.Instance != null
            ? CombatTracker.Instance.CurrentSnapshot
            : default;

        string bestCombo = ComboHitSystem.Instance != null && ComboHitSystem.Instance.ComboHistory.Count > 0
            ? FindLongestCombo()
            : "None";

        PopulateStats(fightDuration, finalSkillScore, snap, bestCombo);
        Show();
    }

    private void PopulateStats(float duration, float skillScore,
                               CombatAnalyticsSnapshot snap, string bestCombo)
    {
        _titleText.text = "VICTORY";

        int min = Mathf.FloorToInt(duration / 60f);
        int sec = Mathf.FloorToInt(duration % 60f);
        _timeText.text        = $"{min:0}:{sec:00}";
        _skillText.text       = $"{skillScore:0}  /  100";
        _parryText.text       = $"{snap.parrySuccessRate * 100f:0}%";
        _dodgeText.text       = $"{snap.dodgeSuccessRate * 100f:0}%";
        _damageDealtText.text = $"{snap.damageRatio * 100f:0}%  dealt ratio";
        _damageTakenText.text = snap.averageReactionTime < 900f
            ? $"{snap.averageReactionTime:0} ms avg reaction"
            : "—";
        _comboText.text       = bestCombo;
    }

    private string FindLongestCombo()
    {
        string best = "None";
        int    bestLen = 0;
        foreach (var sig in ComboHitSystem.Instance.ComboHistory)
        {
            int len = sig.Split('-').Length;
            if (len > bestLen) { bestLen = len; best = sig; }
        }
        return best;
    }

    // ── Show / Hide ────────────────────────────────────────────────────────────

    public void Show()
    {
        if (_panel != null) _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 0f;
    }

    private void Close()
    {
        if (_panel != null) _panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        Time.timeScale   = 1f;
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── UI Helpers ─────────────────────────────────────────────────────────────

    private Text MakeStatRow(Transform parent, string label, ref float y, float step)
    {
        // Label (left)
        MakeText(parent, label + "_Label", label + ":",
            17, FontStyle.Normal, TextAnchor.MiddleLeft,
            new Color(0.62f, 0.62f, 0.65f), new Vector2(-170f, y), new Vector2(220f, 36f));

        // Value (right)
        Text valText = MakeText(parent, label + "_Value", "—",
            18, FontStyle.Bold, TextAnchor.MiddleRight,
            new Color(0.95f, 0.93f, 0.88f), new Vector2(170f, y), new Vector2(220f, 36f));

        y -= step;
        return valText;
    }

    private static void MakeDivider(Transform parent, Vector2 pos)
    {
        GameObject go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(460f, 1f);
        rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.28f, 0.32f, 1f);
    }

    private static Text MakeText(Transform parent, string name, string body,
        int size, FontStyle style, TextAnchor align, Color color,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        Text t   = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = body;
        t.fontSize  = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color     = color;
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label,
                                     Vector2 anchoredPos, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(280f, 46f);
        rt.anchoredPosition = anchoredPos;
        Image img  = go.AddComponent<Image>();
        img.color  = bgColor;
        Button btn = go.AddComponent<Button>();

        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
        Text t   = textGO.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = label;
        t.fontSize  = 24;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = Color.white;
        return btn;
    }
}
