using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles all fullscreen combat FX:
///  - Red vignette flash on phase transitions and player damage
///  - "BOSS APPROACHING" entry banner
///  - Phase change banner ("PHASE 2", "FINAL PHASE", etc.)
/// Self-builds its own Canvas — no Inspector wiring needed.
/// </summary>
public class CombatFX : MonoBehaviour
{
    public static CombatFX Instance { get; private set; }

    // Vignette
    private Image _vignetteImage;
    private Coroutine _vignetteRoutine;

    // Banner
    private GameObject _bannerRoot;
    private Text       _bannerTitle;
    private Text       _bannerSub;
    private Coroutine  _bannerRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<CombatFX>() != null) return;
        new GameObject("CombatFX").AddComponent<CombatFX>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnEnable()
    {
        CombatEventBus.OnBossPhaseChanged   += OnPhaseChanged;
        CombatEventBus.OnPlayerDied         += OnPlayerDied;
        CombatEventBus.OnBossAttackLanded   += OnPlayerHit;
    }

    private void OnDisable()
    {
        CombatEventBus.OnBossPhaseChanged   -= OnPhaseChanged;
        CombatEventBus.OnPlayerDied         -= OnPlayerDied;
        CombatEventBus.OnBossAttackLanded   -= OnPlayerHit;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void ShowBossEntry(string bossName)
    {
        ShowBanner(bossName.ToUpperInvariant(), "APPROACHES", new Color(0.75f, 0.12f, 0.08f), 3.5f);
    }

    // ── Events ─────────────────────────────────────────────────────────────────

    private void OnPhaseChanged(int phase)
    {
        FlashVignette(new Color(0.65f, 0.05f, 0.05f, 0.55f), 0.8f);

        string title = phase == 4 ? "FINAL PHASE" : $"PHASE  {phase}";
        Color  col   = phase == 4 ? new Color(0.9f, 0.2f, 0.05f) : new Color(0.85f, 0.65f, 0.15f);
        ShowBanner(title, phase == 4 ? "ENRAGED" : "", col, 2.5f);
    }

    private void OnPlayerDied()
    {
        FlashVignette(new Color(0.6f, 0f, 0f, 0.75f), 1.5f);
    }

    private void OnPlayerHit(float damage)
    {
        // Soft red flash when taking meaningful damage
        if (damage >= 5f)
            FlashVignette(new Color(0.7f, 0f, 0f, Mathf.Clamp(damage / 80f, 0.15f, 0.40f)), 0.3f);
    }

    // ── Vignette ───────────────────────────────────────────────────────────────

    private void FlashVignette(Color color, float duration)
    {
        if (_vignetteRoutine != null) StopCoroutine(_vignetteRoutine);
        _vignetteRoutine = StartCoroutine(VignetteRoutine(color, duration));
    }

    private IEnumerator VignetteRoutine(Color peak, float duration)
    {
        _vignetteImage.gameObject.SetActive(true);
        float half = duration * 0.3f;

        // Fade in
        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            _vignetteImage.color = Color.Lerp(Color.clear, peak, t / half);
            yield return null;
        }

        // Fade out
        for (float t = 0; t < duration - half; t += Time.unscaledDeltaTime)
        {
            _vignetteImage.color = Color.Lerp(peak, Color.clear, t / (duration - half));
            yield return null;
        }

        _vignetteImage.color = Color.clear;
        _vignetteImage.gameObject.SetActive(false);
        _vignetteRoutine = null;
    }

    // ── Banner ─────────────────────────────────────────────────────────────────

    private void ShowBanner(string title, string sub, Color accentColor, float duration)
    {
        if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
        _bannerRoutine = StartCoroutine(BannerRoutine(title, sub, accentColor, duration));
    }

    private IEnumerator BannerRoutine(string title, string sub, Color accent, float duration)
    {
        _bannerTitle.text  = title;
        _bannerTitle.color = accent;
        _bannerSub.text    = sub;
        _bannerRoot.SetActive(true);

        float fadeIn = 0.4f, fadeOut = 0.5f, hold = duration - fadeIn - fadeOut;

        // Fade in
        for (float t = 0; t < fadeIn; t += Time.unscaledDeltaTime)
        {
            SetBannerAlpha(t / fadeIn);
            yield return null;
        }
        SetBannerAlpha(1f);

        yield return new WaitForSecondsRealtime(hold);

        // Fade out
        for (float t = 0; t < fadeOut; t += Time.unscaledDeltaTime)
        {
            SetBannerAlpha(1f - t / fadeOut);
            yield return null;
        }

        _bannerRoot.SetActive(false);
        _bannerRoutine = null;
    }

    private void SetBannerAlpha(float a)
    {
        Color tc = _bannerTitle.color; tc.a = a; _bannerTitle.color = tc;
        Color sc = _bannerSub.color;   sc.a = a; _bannerSub.color   = sc;
    }

    // ── UI Construction ────────────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("CombatFXCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 18;   // under DeathScreen (20) but over game HUD
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Vignette — full screen image, starts hidden ───────────────────────
        GameObject vigGO = new GameObject("Vignette");
        vigGO.transform.SetParent(canvasGO.transform, false);
        RectTransform vigRT = vigGO.AddComponent<RectTransform>();
        vigRT.anchorMin = Vector2.zero; vigRT.anchorMax = Vector2.one;
        vigRT.offsetMin = Vector2.zero; vigRT.offsetMax = Vector2.zero;
        _vignetteImage = vigGO.AddComponent<Image>();
        _vignetteImage.color = Color.clear;
        _vignetteImage.raycastTarget = false;
        vigGO.SetActive(false);

        // ── Banner — centred, mid-screen ─────────────────────────────────────
        _bannerRoot = new GameObject("Banner");
        _bannerRoot.transform.SetParent(canvasGO.transform, false);
        RectTransform bannerRT = _bannerRoot.AddComponent<RectTransform>();
        bannerRT.anchorMin        = new Vector2(0.5f, 0.6f);
        bannerRT.anchorMax        = new Vector2(0.5f, 0.6f);
        bannerRT.pivot            = new Vector2(0.5f, 0.5f);
        bannerRT.sizeDelta        = new Vector2(800f, 110f);
        bannerRT.anchoredPosition = Vector2.zero;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _bannerTitle = MakeBannerText(_bannerRoot.transform, "Title", "", 62, FontStyle.Bold, font);
        _bannerSub   = MakeBannerText(_bannerRoot.transform, "Sub",   "", 26, FontStyle.Normal, font);

        RectTransform subRT = _bannerSub.GetComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0f, -44f);
        _bannerSub.color = new Color(0.9f, 0.88f, 0.82f, 0f);

        _bannerRoot.SetActive(false);
    }

    private static Text MakeBannerText(Transform parent, string name, string body,
                                       int size, FontStyle style, Font font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(780f, 70f);
        rt.anchoredPosition = Vector2.zero;

        Text t   = go.AddComponent<Text>();
        t.font      = font;
        t.text      = body;
        t.fontSize  = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = new Color(1f, 1f, 1f, 0f);

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(1f, -1f);

        return t;
    }
}
