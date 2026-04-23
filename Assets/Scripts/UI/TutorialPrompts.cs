using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows contextual control hints at the start of the game and during the first boss fight.
/// Prompts are one-shot - once dismissed or timed out they never re-appear.
/// Self-builds its own UI - no Inspector wiring needed.
/// </summary>
public class TutorialPrompts : MonoBehaviour
{
    private Canvas _canvas;
    private Image _cardBg;
    private Text _keyText;
    private Text _descText;
    private Coroutine _sequence;

    private static bool _shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<TutorialPrompts>() != null)
            return;

        new GameObject("TutorialPrompts").AddComponent<TutorialPrompts>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();

        if (!_shown)
            _sequence = StartCoroutine(RunTutorial());
    }

    private void OnEnable()
    {
        CombatEventBus.OnBossPhaseChanged += OnBossPhase;
    }

    private void OnDisable()
    {
        CombatEventBus.OnBossPhaseChanged -= OnBossPhase;
    }

    private IEnumerator RunTutorial()
    {
        _shown = true;

        yield return ShowPrompt("W A S D", "Move and reposition", 4f);
        yield return ShowPrompt("F", "Draw or sheathe your blade", 3.5f);
        yield return ShowPrompt("LMB", "Quick strike", 3f);
        yield return ShowPrompt("RMB", "Heavy strike", 3f);
        yield return ShowPrompt("Q", "Parry glowing attacks", 4f);
        yield return ShowPrompt("SPACE x2", "Dodge dark attacks", 4f);
        yield return ShowPrompt("E", "Flashy attack", 3f);
        yield return ShowPrompt("R", "Ultimate art", 3f);
        yield return ShowPrompt("T", "Talk to NPCs", 3f);
    }

    private void OnBossPhase(int phase)
    {
        if (phase != 1)
            return;

        StopAllCoroutines();
        StartCoroutine(BossTips());
    }

    private IEnumerator BossTips()
    {
        yield return new WaitForSeconds(1f);
        yield return ShowPrompt("Q", "Parry when the strike can be turned", 4f);
        yield return ShowPrompt("SPACE x2", "Dodge when the strike cannot be parried", 4f);
        yield return ShowPrompt("NOW!", "Counter fast after a perfect defense", 3.5f);
    }

    private IEnumerator ShowPrompt(string key, string description, float duration)
    {
        _keyText.text = key;
        _descText.text = description;
        _cardBg.gameObject.SetActive(true);

        yield return Fade(0f, 1f, 0.25f);
        yield return new WaitForSecondsRealtime(duration - 0.5f);
        yield return Fade(1f, 0f, 0.25f);

        _cardBg.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(0.4f);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        Color bg = _cardBg.color;
        bg.a = alpha * 0.88f;
        _cardBg.color = bg;

        Color key = _keyText.color;
        key.a = alpha;
        _keyText.color = key;

        Color desc = _descText.color;
        desc.a = alpha;
        _descText.color = desc;
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("TutorialCanvas");
        canvasGO.transform.SetParent(transform);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 12;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject card = new GameObject("TutorialCard");
        card.transform.SetParent(canvasGO.transform, false);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0f);
        cardRT.anchorMax = new Vector2(0.5f, 0f);
        cardRT.pivot = new Vector2(0.5f, 0f);
        cardRT.sizeDelta = new Vector2(320f, 56f);
        cardRT.anchoredPosition = new Vector2(0f, 180f);

        _cardBg = card.AddComponent<Image>();
        _cardBg.color = new Color(0.06f, 0.07f, 0.10f, 0.88f);

        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        RectTransform accentRT = accent.AddComponent<RectTransform>();
        accentRT.anchorMin = Vector2.zero;
        accentRT.anchorMax = new Vector2(0f, 1f);
        accentRT.pivot = new Vector2(0f, 0.5f);
        accentRT.sizeDelta = new Vector2(4f, 0f);
        accentRT.anchoredPosition = Vector2.zero;
        accent.AddComponent<Image>().color = new Color(0.82f, 0.68f, 0.18f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject keyGO = new GameObject("Key");
        keyGO.transform.SetParent(card.transform, false);
        RectTransform keyRT = keyGO.AddComponent<RectTransform>();
        keyRT.anchorMin = new Vector2(0f, 0.5f);
        keyRT.anchorMax = new Vector2(0f, 0.5f);
        keyRT.pivot = new Vector2(0f, 0.5f);
        keyRT.sizeDelta = new Vector2(96f, 36f);
        keyRT.anchoredPosition = new Vector2(12f, 0f);

        _keyText = keyGO.AddComponent<Text>();
        _keyText.font = font;
        _keyText.fontSize = 15;
        _keyText.fontStyle = FontStyle.Bold;
        _keyText.alignment = TextAnchor.MiddleCenter;
        _keyText.color = new Color(0.96f, 0.84f, 0.32f);

        GameObject descGO = new GameObject("Desc");
        descGO.transform.SetParent(card.transform, false);
        RectTransform descRT = descGO.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0f, 0.5f);
        descRT.anchorMax = new Vector2(1f, 0.5f);
        descRT.pivot = new Vector2(0f, 0.5f);
        descRT.sizeDelta = new Vector2(-124f, 32f);
        descRT.anchoredPosition = new Vector2(116f, 0f);

        _descText = descGO.AddComponent<Text>();
        _descText.font = font;
        _descText.fontSize = 13;
        _descText.alignment = TextAnchor.MiddleLeft;
        _descText.color = new Color(0.92f, 0.90f, 0.86f);

        _cardBg.gameObject.SetActive(false);
    }
}
