using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen indicator that shows whether the current boss attack should be parried or dodged.
/// </summary>
public class AttackWindowIndicator : MonoBehaviour
{
    public static AttackWindowIndicator Instance { get; private set; }

    private enum WindowState
    {
        Hidden,
        TelegraphParry,
        TelegraphDodge,
        HitboxOpen,
        Counter
    }

    private static readonly Color ColParry = new Color(0.15f, 0.80f, 0.30f);
    private static readonly Color ColDodge = new Color(0.15f, 0.55f, 0.95f);
    private static readonly Color ColNow = new Color(1.00f, 0.55f, 0.05f);
    private static readonly Color ColCounter = new Color(0.98f, 0.85f, 0.18f);

    private WindowState _state = WindowState.Hidden;
    private float _hideAt;

    private GameObject _card;
    private Image _cardBg;
    private Image _accent;
    private Text _keyText;
    private Text _labelText;
    private Text _detailText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<AttackWindowIndicator>() != null)
            return;

        new GameObject("AttackWindowIndicator").AddComponent<AttackWindowIndicator>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnEnable()
    {
        CombatEventBus.OnBossAttackTelegraph += OnTelegraph;
        CombatEventBus.OnBossAttackHitbox += OnHitboxOpen;
        CombatEventBus.OnBossAttackEnded += OnAttackEnded;
        CombatEventBus.OnBossDied += OnFightOver;
        CombatEventBus.OnPlayerDied += OnFightOver;
        CombatEventSystem.OnPlayerParry += OnParry;
        CombatEventSystem.OnPlayerDodge += OnDodge;
    }

    private void OnDisable()
    {
        CombatEventBus.OnBossAttackTelegraph -= OnTelegraph;
        CombatEventBus.OnBossAttackHitbox -= OnHitboxOpen;
        CombatEventBus.OnBossAttackEnded -= OnAttackEnded;
        CombatEventBus.OnBossDied -= OnFightOver;
        CombatEventBus.OnPlayerDied -= OnFightOver;
        CombatEventSystem.OnPlayerParry -= OnParry;
        CombatEventSystem.OnPlayerDodge -= OnDodge;
    }

    private void Update()
    {
        if (_state == WindowState.Hidden)
            return;

        if (_state == WindowState.Counter && Time.unscaledTime >= _hideAt)
        {
            SetState(WindowState.Hidden);
            return;
        }

        float frequency = _state == WindowState.HitboxOpen ? 9f : 2.5f;
        float pulse = Mathf.Sin(Time.unscaledTime * frequency * Mathf.PI) * 0.5f + 0.5f;

        Color accent = _accent.color;
        accent.a = 0.65f + pulse * 0.35f;
        _accent.color = accent;

        Color bg = _cardBg.color;
        bg.a = 0.72f + pulse * 0.12f;
        _cardBg.color = bg;

        float scale = 1f + pulse * 0.08f;
        _keyText.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void OnTelegraph(BossAttack attack, float duration)
    {
        bool parryable = attack.IsParryable || attack.isParryable;
        SetState(parryable ? WindowState.TelegraphParry : WindowState.TelegraphDodge);
        _detailText.text = string.IsNullOrWhiteSpace(attack.name)
            ? "READ THE WIND-UP"
            : attack.name.ToUpperInvariant();
    }

    private void OnHitboxOpen(BossAttack attack)
    {
        SetState(WindowState.HitboxOpen);
        _detailText.text = "COMMIT NOW";
    }

    private void OnAttackEnded()
    {
        if (_state != WindowState.Counter)
            SetState(WindowState.Hidden);
    }

    private void OnParry(bool success, float timingMs)
    {
        if (success)
            ShowCounter();
    }

    private void OnDodge(bool success, float timingMs)
    {
        if (success)
            ShowCounter();
    }

    private void OnFightOver()
    {
        SetState(WindowState.Hidden);
    }

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
                _keyText.text = "Q";
                _labelText.text = "PARRY";
                _accent.color = ColParry;
                _keyText.color = ColParry;
                break;
            case WindowState.TelegraphDodge:
                _keyText.text = "SPACE x2";
                _labelText.text = "DODGE";
                _accent.color = ColDodge;
                _keyText.color = ColDodge;
                break;
            case WindowState.HitboxOpen:
                _keyText.text = "NOW!";
                _labelText.text = "WINDOW OPEN";
                _accent.color = ColNow;
                _keyText.color = ColNow;
                break;
            case WindowState.Counter:
                _keyText.text = "COUNTER!";
                _labelText.text = "ATTACK NOW";
                _detailText.text = "BOSS IS VULNERABLE";
                _accent.color = ColCounter;
                _keyText.color = ColCounter;
                break;
        }
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("AWI_Canvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        _card = new GameObject("WindowCard");
        _card.transform.SetParent(canvasGO.transform, false);
        RectTransform cardRT = _card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(1f, 0f);
        cardRT.anchorMax = new Vector2(1f, 0f);
        cardRT.pivot = new Vector2(1f, 0f);
        cardRT.sizeDelta = new Vector2(240f, 84f);
        cardRT.anchoredPosition = new Vector2(-20f, 170f);

        _cardBg = _card.AddComponent<Image>();
        _cardBg.color = new Color(0.05f, 0.07f, 0.12f, 0.88f);

        GameObject accentGO = new GameObject("Accent");
        accentGO.transform.SetParent(_card.transform, false);
        RectTransform accentRT = accentGO.AddComponent<RectTransform>();
        accentRT.anchorMin = Vector2.zero;
        accentRT.anchorMax = new Vector2(0f, 1f);
        accentRT.pivot = new Vector2(0f, 0.5f);
        accentRT.sizeDelta = new Vector2(5f, 0f);
        _accent = accentGO.AddComponent<Image>();
        _accent.color = ColParry;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject keyGO = new GameObject("Key");
        keyGO.transform.SetParent(_card.transform, false);
        RectTransform keyRT = keyGO.AddComponent<RectTransform>();
        keyRT.anchorMin = new Vector2(0f, 0.5f);
        keyRT.anchorMax = new Vector2(1f, 1f);
        keyRT.offsetMin = new Vector2(14f, 0f);
        keyRT.offsetMax = new Vector2(-8f, 0f);
        _keyText = keyGO.AddComponent<Text>();
        _keyText.font = font;
        _keyText.fontSize = 24;
        _keyText.fontStyle = FontStyle.Bold;
        _keyText.alignment = TextAnchor.MiddleLeft;
        _keyText.color = ColParry;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(_card.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0.26f);
        labelRT.anchorMax = new Vector2(1f, 0.58f);
        labelRT.offsetMin = new Vector2(14f, 0f);
        labelRT.offsetMax = new Vector2(-8f, 0f);
        _labelText = labelGO.AddComponent<Text>();
        _labelText.font = font;
        _labelText.fontSize = 14;
        _labelText.alignment = TextAnchor.MiddleLeft;
        _labelText.color = new Color(0.85f, 0.85f, 0.85f);

        GameObject detailGO = new GameObject("Detail");
        detailGO.transform.SetParent(_card.transform, false);
        RectTransform detailRT = detailGO.AddComponent<RectTransform>();
        detailRT.anchorMin = new Vector2(0f, 0f);
        detailRT.anchorMax = new Vector2(1f, 0.26f);
        detailRT.offsetMin = new Vector2(14f, 2f);
        detailRT.offsetMax = new Vector2(-8f, 0f);
        _detailText = detailGO.AddComponent<Text>();
        _detailText.font = font;
        _detailText.fontSize = 11;
        _detailText.alignment = TextAnchor.LowerLeft;
        _detailText.color = new Color(0.72f, 0.72f, 0.76f);

        _card.SetActive(false);
    }
}
