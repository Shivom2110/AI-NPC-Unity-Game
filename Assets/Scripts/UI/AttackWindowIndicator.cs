using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen indicator that shows the current parry / dodge / counter window.
/// Positioned bottom-right so it doesn't cover health bars or tutorial cards.
///
/// States:
///   Telegraph (parryable)   → green pulsing card  "Q  /  PARRY"
///   Telegraph (unparryable) → blue pulsing card   "SPACE×2  /  DODGE"
///   Hitbox open             → fast-pulsing orange "NOW!"
///   After parry/dodge hit   → gold card           "COUNTER!"  (1.5 s then hides)
///
/// Self-builds its own UI — no Inspector wiring needed.
/// </summary>
public class AttackWindowIndicator : MonoBehaviour
{
    public static AttackWindowIndicator Instance { get; private set; }

    private enum WindowState { Hidden, TelegraphParry, TelegraphDodge, HitboxOpen, Counter }

    private WindowState _state   = WindowState.Hidden;
    private float       _hideAt;   // Time.unscaledTime when Counter auto-hides

    // ── UI ──────────────────────────────────────────────────────────────────────
    private GameObject _card;
    private Image      _cardBg;
    private Image      _accent;
    private Text       _keyText;
    private Text       _labelText;

    // ── Palette ─────────────────────────────────────────────────────────────────
    private static readonly Color ColParry   = new Color(0.15f, 0.80f, 0.30f);   // green
    private static readonly Color ColDodge   = new Color(0.15f, 0.55f, 0.95f);   // blue
    private static readonly Color ColNow     = new Color(1.00f, 0.55f, 0.05f);   // orange
    private static readonly Color ColCounter = new Color(0.98f, 0.85f, 0.18f);   // gold

    // ── Bootstrap ───────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<AttackWindowIndicator>() != null) return;
        new GameObject("AttackWindowIndicator").AddComponent<AttackWindowIndicator>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    // ── Event wiring ────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        CombatEventBus.OnBossAttackTelegraph += OnTelegraph;
        CombatEventBus.OnBossAttackHitbox    += OnHitboxOpen;
        CombatEventBus.OnBossAttackEnded     += OnAttackEnded;
        CombatEventBus.OnBossDied            += OnFightOver;
        CombatEventBus.OnPlayerDied          += OnFightOver;
        CombatEventSystem.OnPlayerParry      += OnParry;
        CombatEventSystem.OnPlayerDodge      += OnDodge;
    }

    private void OnDisable()
    {
        CombatEventBus.OnBossAttackTelegraph -= OnTelegraph;
        CombatEventBus.OnBossAttackHitbox    -= OnHitboxOpen;
        CombatEventBus.OnBossAttackEnded     -= OnAttackEnded;
        CombatEventBus.OnBossDied            -= OnFightOver;
        CombatEventBus.OnPlayerDied          -= OnFightOver;
        CombatEventSystem.OnPlayerParry      -= OnParry;
        CombatEventSystem.OnPlayerDodge      -= OnDodge;
    }

    // ── Handlers ────────────────────────────────────────────────────────────────

    private void OnTelegraph(BossAttack atk, float duration)
    {
        SetState(atk.IsParryable || atk.isParryable
            ? WindowState.TelegraphParry
            : WindowState.TelegraphDodge);
    }

    private void OnHitboxOpen(BossAttack atk)   => SetState(WindowState.HitboxOpen);

    private void OnAttackEnded()
    {
        // Keep Counter window if it's already showing (parry/dodge just succeeded)
        if (_state != WindowState.Counter) SetState(WindowState.Hidden);
    }

    private void OnParry(bool success, float timingMs)
    {
        if (success) ShowCounter();
    }

    private void OnDodge(bool success, float timingMs)
    {
        if (success) ShowCounter();
    }

    private void OnFightOver() => SetState(WindowState.Hidden);

    // ── State helpers ────────────────────────────────────────────────────────────

    private void ShowCounter()
    {
        _hideAt = Time.unscaledTime + 1.5f;
        SetState(WindowState.Counter);
    }

    private void SetState(WindowState next)
    {
        _state = next;

        if (next == WindowState.Hidden)
        {
            _card.SetActive(false);
            return;
        }

        _card.SetActive(true);

        switch (next)
        {
            case WindowState.TelegraphParry:
                _keyText.text   = "Q";
                _labelText.text = "PARRY";
                _accent.color   = ColParry;
                _keyText.color  = ColParry;
                break;

            case WindowState.TelegraphDodge:
                _keyText.text   = "SPACE×2";
                _labelText.text = "DODGE";
                _accent.color   = ColDodge;
                _keyText.color  = ColDodge;
                break;

            case WindowState.HitboxOpen:
                _keyText.text   = "NOW!";
                _labelText.text = "WINDOW OPEN";
                _accent.color   = ColNow;
                _keyText.color  = ColNow;
                break;

            case WindowState.Counter:
                _keyText.text   = "COUNTER!";
                _labelText.text = "Attack now";
                _accent.color   = ColCounter;
                _keyText.color  = ColCounter;
                break;
        }
    }

    // ── Per-frame pulse ──────────────────────────────────────────────────────────

    private void Update()
    {
        if (_state == WindowState.Hidden) return;

        // Auto-hide counter window after its duration
        if (_state == WindowState.Counter && Time.unscaledTime >= _hideAt)
        {
            SetState(WindowState.Hidden);
            return;
        }

        // Pulse frequency: slow during telegraph, fast when window is NOW open
        float freq  = _state == WindowState.HitboxOpen ? 9f : 2.5f;
        float pulse = Mathf.Sin(Time.unscaledTime * freq * Mathf.PI) * 0.5f + 0.5f; // 0–1

        // Pulse the accent bar opacity
        Color a = _accent.color;
        a.a = 0.65f + pulse * 0.35f;
        _accent.color = a;

        // Pulse the background slightly
        Color bg = _cardBg.color;
        bg.a = 0.72f + pulse * 0.12f;
        _cardBg.color = bg;

        // Scale key text for a "breathing" feel
        float s = 1f + pulse * 0.08f;
        _keyText.transform.localScale = new Vector3(s, s, 1f);
    }

    // ── UI construction ──────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Overlay canvas
        GameObject canvasGO = new GameObject("AWI_Canvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Card — bottom-right corner
        _card = new GameObject("WindowCard");
        _card.transform.SetParent(canvasGO.transform, false);
        RectTransform cardRT = _card.AddComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(1f, 0f);
        cardRT.anchorMax        = new Vector2(1f, 0f);
        cardRT.pivot            = new Vector2(1f, 0f);
        cardRT.sizeDelta        = new Vector2(210f, 72f);
        cardRT.anchoredPosition = new Vector2(-20f, 170f);   // just above stamina bar

        _cardBg = _card.AddComponent<Image>();
        _cardBg.color = new Color(0.05f, 0.07f, 0.12f, 0.88f);

        // Left accent bar (colour-coded per state)
        GameObject acGO = new GameObject("Accent");
        acGO.transform.SetParent(_card.transform, false);
        RectTransform acRT = acGO.AddComponent<RectTransform>();
        acRT.anchorMin = Vector2.zero;
        acRT.anchorMax = new Vector2(0f, 1f);
        acRT.pivot     = new Vector2(0f, 0.5f);
        acRT.sizeDelta = new Vector2(5f, 0f);
        acRT.anchoredPosition = Vector2.zero;
        _accent = acGO.AddComponent<Image>();
        _accent.color = ColParry;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Key label — large, top half
        GameObject keyGO = new GameObject("Key");
        keyGO.transform.SetParent(_card.transform, false);
        RectTransform keyRT = keyGO.AddComponent<RectTransform>();
        keyRT.anchorMin        = new Vector2(0f, 0.5f);
        keyRT.anchorMax        = new Vector2(1f, 1f);
        keyRT.offsetMin        = new Vector2(14f, 0f);
        keyRT.offsetMax        = new Vector2(-8f, 0f);
        _keyText = keyGO.AddComponent<Text>();
        _keyText.font      = font;
        _keyText.fontSize  = 24;
        _keyText.fontStyle = FontStyle.Bold;
        _keyText.alignment = TextAnchor.MiddleLeft;
        _keyText.color     = ColParry;

        // Action label — smaller, bottom half
        GameObject lblGO = new GameObject("Label");
        lblGO.transform.SetParent(_card.transform, false);
        RectTransform lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin        = new Vector2(0f, 0f);
        lblRT.anchorMax        = new Vector2(1f, 0.5f);
        lblRT.offsetMin        = new Vector2(14f, 3f);
        lblRT.offsetMax        = new Vector2(-8f, 0f);
        _labelText = lblGO.AddComponent<Text>();
        _labelText.font      = font;
        _labelText.fontSize  = 14;
        _labelText.alignment = TextAnchor.MiddleLeft;
        _labelText.color     = new Color(0.85f, 0.85f, 0.85f);

        _card.SetActive(false);
    }
}
