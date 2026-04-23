using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles fullscreen combat FX such as phase banners, damage flashes, and low-health tint.
/// </summary>
public class CombatFX : MonoBehaviour
{
    public static CombatFX Instance { get; private set; }

    private Image _flashImage;
    private Image _lowHealthImage;
    private Coroutine _flashRoutine;
    private Coroutine _hitstopRoutine;
    private float _preHitstopTimeScale = 1f;
    private GameObject _bannerRoot;
    private Text _bannerTitle;
    private Text _bannerSub;
    private Coroutine _bannerRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<CombatFX>() != null)
            return;

        new GameObject("CombatFX").AddComponent<CombatFX>();
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
        CombatEventBus.OnBossPhaseChanged += OnPhaseChanged;
        CombatEventBus.OnPlayerDied       += OnPlayerDied;
        CombatEventBus.OnBossAttackLanded += OnPlayerHit;
        CombatEventBus.OnPlayerDamaged    += OnPlayerDamaged;
        CombatEventBus.OnBossRoar         += OnBossRoared;
        CombatEventBus.OnBossSecondWind   += OnBossSecondWind;
        CombatEventBus.OnPlayerParry      += OnPlayerParryResult;
    }

    private void OnDisable()
    {
        CombatEventBus.OnBossPhaseChanged -= OnPhaseChanged;
        CombatEventBus.OnPlayerDied       -= OnPlayerDied;
        CombatEventBus.OnBossAttackLanded -= OnPlayerHit;
        CombatEventBus.OnPlayerDamaged    -= OnPlayerDamaged;
        CombatEventBus.OnBossRoar         -= OnBossRoared;
        CombatEventBus.OnBossSecondWind   -= OnBossSecondWind;
        CombatEventBus.OnPlayerParry      -= OnPlayerParryResult;
    }

    private void Update()
    {
        UpdateLowHealthTint();
    }

    public void ShowBossEntry(string bossName)
    {
        ShowBanner(bossName.ToUpperInvariant(), "APPROACHES", new Color(0.80f, 0.30f, 0.16f), 3.5f);
    }

    private void OnBossRoared()
    {
        Flash(new Color(0.82f, 0.42f, 0.08f, 0.28f), 0.55f);
    }

    private void OnBossSecondWind()
    {
        Hitstop(0.22f);
        Flash(new Color(0.90f, 0.15f, 0.05f, 0.58f), 1.1f);
        ShowBanner("SECOND WIND", "THE BOSS REFUSES TO FALL", new Color(0.95f, 0.28f, 0.10f), 3.2f);
    }

    private void OnPlayerParryResult(bool success, float reactionMs)
    {
        if (!success) return;
        bool perfect = reactionMs < 150f;
        Flash(new Color(0.95f, 0.90f, 0.60f, perfect ? 0.35f : 0.18f), 0.18f);
        if (perfect) Hitstop(0.09f);
    }

    private void OnPhaseChanged(int phase)
    {
        Hitstop(phase >= 3 ? 0.16f : 0.10f);
        Flash(new Color(0.68f, 0.10f, 0.08f, 0.50f), 0.8f);

        FightProgressionManager.TrainingPhase resolvedPhase =
            (FightProgressionManager.TrainingPhase)Mathf.Clamp(phase, 1, 3);

        string title = FightProgressionManager.GetPhaseName(resolvedPhase).ToUpperInvariant();
        string sub = FightProgressionManager.GetPhaseSubtitle(resolvedPhase);
        Color color = phase >= 3
            ? new Color(0.92f, 0.25f, 0.12f)
            : phase == 2
                ? new Color(0.94f, 0.70f, 0.22f)
                : new Color(0.78f, 0.83f, 0.92f);

        ShowBanner(title, sub, color, 2.9f);
    }

    private void OnPlayerDied()
    {
        Flash(new Color(0.60f, 0f, 0f, 0.75f), 1.5f);
    }

    private void OnPlayerHit(float damage)
    {
        if (damage >= 5f)
            Flash(new Color(0.70f, 0f, 0f, Mathf.Clamp(damage / 80f, 0.15f, 0.40f)), 0.3f);
    }

    private void OnPlayerDamaged(float damage, string attackType)
    {
        if (damage >= 1f)
            Flash(new Color(0.72f, 0.03f, 0.03f, Mathf.Clamp(damage / 70f, 0.10f, 0.35f)), 0.25f);
    }

    private void UpdateLowHealthTint()
    {
        if (_lowHealthImage == null)
            return;

        PlayerHealth player = PlayerHealth.Instance;
        if (player == null || player.MaxHealth <= 0f || player.IsDead)
        {
            _lowHealthImage.color = Color.clear;
            return;
        }

        float healthRatio = Mathf.Clamp01(player.CurrentHealth / player.MaxHealth);
        float lowHealth = Mathf.InverseLerp(0.40f, 0.12f, healthRatio);
        float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 7f) * 0.15f;
        float alpha = lowHealth * 0.28f * pulse;
        _lowHealthImage.color = new Color(0.55f, 0.02f, 0.02f, alpha);
    }

    public void Hitstop(float duration)
    {
        // Only snapshot the "real" timescale on the first hitstop in a chain —
        // stopping a running coroutine doesn't restore timeScale, so we must not
        // overwrite the snapshot with the frozen 0.05 value.
        if (_hitstopRoutine == null)
            _preHitstopTimeScale = Time.timeScale;
        else
            StopCoroutine(_hitstopRoutine);

        _hitstopRoutine = StartCoroutine(HitstopRoutine(duration));
    }

    private IEnumerator HitstopRoutine(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = _preHitstopTimeScale;
        _hitstopRoutine = null;
    }

    private void Flash(Color color, float duration)
    {
        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color peak, float duration)
    {
        _flashImage.gameObject.SetActive(true);
        float half = duration * 0.3f;

        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            _flashImage.color = Color.Lerp(Color.clear, peak, t / Mathf.Max(0.01f, half));
            yield return null;
        }

        for (float t = 0; t < duration - half; t += Time.unscaledDeltaTime)
        {
            _flashImage.color = Color.Lerp(peak, Color.clear, t / Mathf.Max(0.01f, duration - half));
            yield return null;
        }

        _flashImage.color = Color.clear;
        _flashImage.gameObject.SetActive(false);
        _flashRoutine = null;
    }

    private void ShowBanner(string title, string sub, Color accentColor, float duration)
    {
        if (_bannerRoutine != null)
            StopCoroutine(_bannerRoutine);

        _bannerRoutine = StartCoroutine(BannerRoutine(title, sub, accentColor, duration));
    }

    private IEnumerator BannerRoutine(string title, string sub, Color accent, float duration)
    {
        _bannerTitle.text = title;
        _bannerTitle.color = accent;
        _bannerSub.text = sub;
        _bannerRoot.SetActive(true);

        float fadeIn = 0.4f;
        float fadeOut = 0.5f;
        float hold = Mathf.Max(0f, duration - fadeIn - fadeOut);

        for (float t = 0; t < fadeIn; t += Time.unscaledDeltaTime)
        {
            SetBannerAlpha(t / fadeIn);
            yield return null;
        }

        SetBannerAlpha(1f);
        yield return new WaitForSecondsRealtime(hold);

        for (float t = 0; t < fadeOut; t += Time.unscaledDeltaTime)
        {
            SetBannerAlpha(1f - (t / fadeOut));
            yield return null;
        }

        _bannerRoot.SetActive(false);
        _bannerRoutine = null;
    }

    private void SetBannerAlpha(float alpha)
    {
        Color title = _bannerTitle.color;
        title.a = alpha;
        _bannerTitle.color = title;

        Color sub = _bannerSub.color;
        sub.a = alpha;
        _bannerSub.color = sub;
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("CombatFXCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 18;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        _lowHealthImage = CreateFullscreenImage(canvasGO.transform, "LowHealthTint");
        _lowHealthImage.color = Color.clear;

        _flashImage = CreateFullscreenImage(canvasGO.transform, "Flash");
        _flashImage.color = Color.clear;
        _flashImage.gameObject.SetActive(false);

        _bannerRoot = new GameObject("Banner");
        _bannerRoot.transform.SetParent(canvasGO.transform, false);
        RectTransform bannerRT = _bannerRoot.AddComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0.5f, 0.60f);
        bannerRT.anchorMax = new Vector2(0.5f, 0.60f);
        bannerRT.pivot = new Vector2(0.5f, 0.5f);
        bannerRT.sizeDelta = new Vector2(920f, 140f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _bannerTitle = MakeBannerText(_bannerRoot.transform, "Title", 52, FontStyle.Bold, font);
        _bannerSub = MakeBannerText(_bannerRoot.transform, "Sub", 24, FontStyle.Normal, font);

        RectTransform subRT = _bannerSub.rectTransform;
        subRT.anchoredPosition = new Vector2(0f, -46f);
        _bannerSub.color = new Color(0.95f, 0.92f, 0.88f, 0f);

        _bannerRoot.SetActive(false);
    }

    private static Image CreateFullscreenImage(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static Text MakeBannerText(Transform parent, string name, int size, FontStyle style, Font font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(880f, 70f);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 1f, 1f, 0f);
        text.raycastTarget = false;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
        shadow.effectDistance = new Vector2(2f, -2f);

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
        outline.effectDistance = new Vector2(1f, -1f);

        return text;
    }
}
