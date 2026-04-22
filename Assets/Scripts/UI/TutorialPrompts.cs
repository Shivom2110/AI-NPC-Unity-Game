using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows contextual control hints at the start of the game and during the first boss fight.
/// Prompts are one-shot — once dismissed or timed out they never re-appear.
/// Self-builds its own UI — no Inspector wiring needed.
/// </summary>
public class TutorialPrompts : MonoBehaviour
{
    private Canvas    _canvas;
    private Image     _cardBg;
    private Text      _keyText;
    private Text      _descText;
    private Coroutine _sequence;

    private static bool _shown = false;   // survives scene reload via static

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<TutorialPrompts>() != null) return;
        new GameObject("TutorialPrompts").AddComponent<TutorialPrompts>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
        if (!_shown)
            _sequence = StartCoroutine(RunTutorial());
    }

    private void OnEnable()  => CombatEventBus.OnBossPhaseChanged += OnBossPhase;
    private void OnDisable() => CombatEventBus.OnBossPhaseChanged -= OnBossPhase;

    // ── Tutorial sequence ──────────────────────────────────────────────────────

    private IEnumerator RunTutorial()
    {
        _shown = true;

        // Movement — show immediately
        yield return ShowPrompt("W A S D", "Move",                   4f);
        yield return ShowPrompt("F",       "Draw / Sheathe Sword",   3.5f);
        yield return ShowPrompt("LMB",     "Light Attack",           3f);
        yield return ShowPrompt("RMB",     "Heavy Attack",           3f);
        yield return ShowPrompt("Q",       "Parry  (time it early)", 4f);
        yield return ShowPrompt("SPACE×2", "Dodge Roll  (costs stamina)", 4f);
        yield return ShowPrompt("E",       "Flashy Attack",          3f);
        yield return ShowPrompt("R",       "Ultimate",               3f);
        yield return ShowPrompt("T",       "Talk to NPC",            3f);
    }

    private void OnBossPhase(int phase)
    {
        if (phase != 1) return;  // only on first phase entry (boss fight start)
        StopAllCoroutines();
        StartCoroutine(BossTips());
    }

    private IEnumerator BossTips()
    {
        yield return new WaitForSeconds(1f);
        yield return ShowPrompt("Q",       "Parry glowing attacks",    4f);
        yield return ShowPrompt("SPACE×2", "Roll through dark attacks",4f);
    }

    // ── Core ───────────────────────────────────────────────────────────────────

    private IEnumerator ShowPrompt(string key, string description, float duration)
    {
        _keyText.text  = key;
        _descText.text = description;
        _cardBg.gameObject.SetActive(true);

        // Fade in
        yield return Fade(0f, 1f, 0.25f);

        // Hold
        yield return new WaitForSecondsRealtime(duration - 0.5f);

        // Fade out
        yield return Fade(1f, 0f, 0.25f);

        _cardBg.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(0.4f);
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
        {
            float a = Mathf.Lerp(from, to, t / dur);
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        Color bg = _cardBg.color;   bg.a = a * 0.88f; _cardBg.color  = bg;
        Color kt = _keyText.color;  kt.a = a;          _keyText.color = kt;
        Color dt = _descText.color; dt.a = a;          _descText.color = dt;
    }

    // ── UI Construction ────────────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("TutorialCanvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 12;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        // Card positioned bottom-centre
        GameObject card = new GameObject("TutorialCard");
        card.transform.SetParent(canvasGO.transform, false);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0.5f, 0f);
        cardRT.anchorMax        = new Vector2(0.5f, 0f);
        cardRT.pivot            = new Vector2(0.5f, 0f);
        cardRT.sizeDelta        = new Vector2(260f, 48f);
        cardRT.anchoredPosition = new Vector2(0f, 180f);

        _cardBg = card.AddComponent<Image>();
        _cardBg.color = new Color(0.06f, 0.07f, 0.10f, 0.88f);

        // Accent left edge
        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        RectTransform acRT = accent.AddComponent<RectTransform>();
        acRT.anchorMin = Vector2.zero; acRT.anchorMax = new Vector2(0f, 1f);
        acRT.pivot     = new Vector2(0f, 0.5f);
        acRT.sizeDelta = new Vector2(4f, 0f);
        acRT.anchoredPosition = Vector2.zero;
        accent.AddComponent<Image>().color = new Color(0.82f, 0.68f, 0.18f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Key pill (left)
        GameObject keyGO = new GameObject("Key");
        keyGO.transform.SetParent(card.transform, false);
        RectTransform keyRT = keyGO.AddComponent<RectTransform>();
        keyRT.anchorMin        = new Vector2(0f, 0.5f);
        keyRT.anchorMax        = new Vector2(0f, 0.5f);
        keyRT.pivot            = new Vector2(0f, 0.5f);
        keyRT.sizeDelta        = new Vector2(80f, 32f);
        keyRT.anchoredPosition = new Vector2(12f, 0f);
        _keyText = keyGO.AddComponent<Text>();
        _keyText.font      = font;
        _keyText.fontSize  = 15;
        _keyText.fontStyle = FontStyle.Bold;
        _keyText.alignment = TextAnchor.MiddleCenter;
        _keyText.color     = new Color(0.96f, 0.84f, 0.32f);

        // Description (right)
        GameObject descGO = new GameObject("Desc");
        descGO.transform.SetParent(card.transform, false);
        RectTransform descRT = descGO.AddComponent<RectTransform>();
        descRT.anchorMin        = new Vector2(0f, 0.5f);
        descRT.anchorMax        = new Vector2(1f, 0.5f);
        descRT.pivot            = new Vector2(0f, 0.5f);
        descRT.sizeDelta        = new Vector2(-108f, 32f);
        descRT.anchoredPosition = new Vector2(104f, 0f);
        _descText = descGO.AddComponent<Text>();
        _descText.font      = font;
        _descText.fontSize  = 13;
        _descText.alignment = TextAnchor.MiddleLeft;
        _descText.color     = new Color(0.92f, 0.90f, 0.86f);

        _cardBg.gameObject.SetActive(false);
    }
}
