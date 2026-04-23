using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flashes a cinematic ability card on screen when the player uses a major ability.
/// Self-builds its own UI — no Inspector wiring needed.
/// Place ability art sprites in Assets/Resources/AbilityIcons/ named:
///   strike.png, heavy.png, flashy.png, ultimate.png, parry.png
/// </summary>
public class AbilityCardFlash : MonoBehaviour
{
    private Image     _cardBg;
    private Image     _art;
    private Image     _overlay;
    private Text      _nameText;
    private Text      _descText;
    private GameObject _card;
    private Coroutine  _active;

    private static readonly Dictionary<PlayerAttackType, (string sprite, string name, string desc)> _data =
        new Dictionary<PlayerAttackType, (string, string, string)>
        {
            { PlayerAttackType.AutoAttack, ("strike",   "Quick Fang",              "") },
            { PlayerAttackType.Attack2,    ("heavy",    "Sun-Sunder",              "") },
            { PlayerAttackType.Attack3,    ("flashy",   "Serpent's Grace",         "A sudden flourish that punishes\nhesitation with poisoned steel.") },
            { PlayerAttackType.Attack4,    ("parry",    "Retaliator's Shield",     "Turn the blow aside and open\na killing lane for the counter.") },
            { PlayerAttackType.Ultimate,   ("ultimate", "Solar Cobra's Judgement", "A brutal finishing art meant to\nbreak the boss's rhythm in one burst.") },
        };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<AbilityCardFlash>() != null) return;
        new GameObject("AbilityCardFlash").AddComponent<AbilityCardFlash>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnEnable()  => CombatEventBus.OnPlayerAttack += HandleAttack;
    private void OnDisable() => CombatEventBus.OnPlayerAttack -= HandleAttack;

    private void HandleAttack(PlayerAttackType type, bool landed, float damage)
    {
        if (type == PlayerAttackType.AutoAttack || type == PlayerAttackType.Attack2)
            return;

        if (!landed && type != PlayerAttackType.Attack4)
            return;

        if (!_data.TryGetValue(type, out var info))
            return;

        Sprite sprite = Resources.Load<Sprite>($"AbilityIcons/{info.sprite}");
        if (sprite == null)
            return;

        if (_active != null)
            StopCoroutine(_active);

        _active = StartCoroutine(Flash(sprite, info.name, info.desc));
    }

    private IEnumerator Flash(Sprite sprite, string abilityName, string desc)
    {
        _art.sprite     = sprite;
        _nameText.text  = abilityName;
        _descText.text  = desc;
        _card.SetActive(true);

        yield return Fade(0f, 1f, 0.18f);
        yield return new WaitForSecondsRealtime(1.1f);
        yield return Fade(1f, 0f, 0.38f);

        _card.SetActive(false);
        _active = null;
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        Color c;
        c = _cardBg.color;   c.a = a * 0.93f; _cardBg.color   = c;
        c = _art.color;      c.a = a;          _art.color      = c;
        c = _overlay.color;  c.a = a * 0.78f;  _overlay.color  = c;
        c = _nameText.color; c.a = a;          _nameText.color = c;
        c = _descText.color; c.a = a;          _descText.color = c;
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("AbilityFlashCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 24;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        // Card — bottom-left, landscape
        _card = new GameObject("AbilityCard");
        _card.transform.SetParent(canvasGO.transform, false);
        var cardRT = _card.AddComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0f, 0f);
        cardRT.anchorMax        = new Vector2(0f, 0f);
        cardRT.pivot            = new Vector2(0f, 0f);
        cardRT.sizeDelta        = new Vector2(340f, 196f);
        cardRT.anchoredPosition = new Vector2(24f, 250f);

        // Dark background frame with golden top border
        _cardBg = _card.AddComponent<Image>();
        _cardBg.color = new Color(0.05f, 0.04f, 0.03f, 0.93f);

        var accentGO = new GameObject("Border");
        accentGO.transform.SetParent(_card.transform, false);
        var acRT = accentGO.AddComponent<RectTransform>();
        acRT.anchorMin = new Vector2(0f, 1f); acRT.anchorMax = new Vector2(1f, 1f);
        acRT.pivot     = new Vector2(0.5f, 1f);
        acRT.sizeDelta = new Vector2(0f, 4f); acRT.anchoredPosition = Vector2.zero;
        accentGO.AddComponent<Image>().color = new Color(0.96f, 0.80f, 0.20f);

        var sideAccentGO = new GameObject("SideAccent");
        sideAccentGO.transform.SetParent(_card.transform, false);
        var sideAccentRT = sideAccentGO.AddComponent<RectTransform>();
        sideAccentRT.anchorMin = Vector2.zero;
        sideAccentRT.anchorMax = new Vector2(0f, 1f);
        sideAccentRT.pivot = new Vector2(0f, 0.5f);
        sideAccentRT.sizeDelta = new Vector2(5f, 0f);
        sideAccentGO.AddComponent<Image>().color = new Color(0.72f, 0.18f, 0.12f, 0.9f);

        // Full-card art (fills card, preserves aspect)
        var artGO = new GameObject("Art");
        artGO.transform.SetParent(_card.transform, false);
        var artRT = artGO.AddComponent<RectTransform>();
        artRT.anchorMin = Vector2.zero; artRT.anchorMax = Vector2.one;
        artRT.offsetMin = new Vector2(4f, 4f); artRT.offsetMax = new Vector2(-4f, -4f);
        _art = artGO.AddComponent<Image>();
        _art.preserveAspect = false;
        _art.raycastTarget  = false;

        // Gradient overlay at the bottom for text legibility
        var overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(_card.transform, false);
        var overlayRT = overlayGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero; overlayRT.anchorMax = new Vector2(1f, 0.42f);
        overlayRT.offsetMin = new Vector2(4f, 4f); overlayRT.offsetMax = new Vector2(-4f, 0f);
        _overlay = overlayGO.AddComponent<Image>();
        _overlay.color = new Color(0.06f, 0.03f, 0.02f, 0.80f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Ability name (gold, bold)
        var nmGO = new GameObject("Name");
        nmGO.transform.SetParent(_card.transform, false);
        var nmRT = nmGO.AddComponent<RectTransform>();
        nmRT.anchorMin = new Vector2(0f, 0.22f); nmRT.anchorMax = new Vector2(1f, 0.42f);
        nmRT.offsetMin = new Vector2(12f, 0f); nmRT.offsetMax = new Vector2(-12f, 0f);
        _nameText = nmGO.AddComponent<Text>();
        _nameText.font      = font;
        _nameText.fontSize  = 18;
        _nameText.fontStyle = FontStyle.Bold;
        _nameText.alignment = TextAnchor.LowerLeft;
        _nameText.color     = new Color(0.97f, 0.84f, 0.28f);
        _nameText.resizeTextForBestFit = true;
        _nameText.resizeTextMinSize    = 12;
        _nameText.resizeTextMaxSize    = 18;

        // Description (light grey)
        var dcGO = new GameObject("Desc");
        dcGO.transform.SetParent(_card.transform, false);
        var dcRT = dcGO.AddComponent<RectTransform>();
        dcRT.anchorMin = new Vector2(0f, 0.04f); dcRT.anchorMax = new Vector2(1f, 0.22f);
        dcRT.offsetMin = new Vector2(12f, 0f); dcRT.offsetMax = new Vector2(-12f, 0f);
        _descText = dcGO.AddComponent<Text>();
        _descText.font      = font;
        _descText.fontSize  = 12;
        _descText.alignment = TextAnchor.UpperLeft;
        _descText.color     = new Color(0.84f, 0.82f, 0.80f);

        _card.SetActive(false);
    }
}
