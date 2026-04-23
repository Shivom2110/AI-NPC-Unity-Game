using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    private sealed class MoveSlotUI
    {
        public RectTransform Root;
        public Image Background;
        public Image Accent;
        public Image ReadyGlow;
        public Image IconPlate;
        public Image IconSprite;
        public Image CooldownOverlay;
        public Text IconText;
        public Text KeyText;
        public Text NameText;
        public Text CooldownText;
    }

    private readonly Dictionary<string, Sprite> _abilitySprites = new Dictionary<string, Sprite>();

    private void PreloadAbilitySprites()
    {
        // Load every Sprite in the folder
        Sprite[] sprites = Resources.LoadAll<Sprite>("AbilityIcons");
        foreach (Sprite s in sprites)
            _abilitySprites[s.name.ToLowerInvariant()] = s;

        // Fallback: load as Texture2D for any not found as Sprite
        Texture2D[] textures = Resources.LoadAll<Texture2D>("AbilityIcons");
        foreach (Texture2D tex in textures)
        {
            string key = tex.name.ToLowerInvariant();
            if (!_abilitySprites.ContainsKey(key))
                _abilitySprites[key] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        Debug.Log($"[PlayerHUD] Loaded {_abilitySprites.Count} ability sprites: {string.Join(", ", _abilitySprites.Keys)}");
    }

    private Sprite GetAbilitySprite(string displayName)
    {
        string lookup = displayName.ToLowerInvariant();
        switch (lookup)
        {
            case "slash":
                lookup = "strike";
                break;
            case "flash":
                lookup = "flashy";
                break;
            case "roll":
                lookup = "parry";
                break;
        }

        _abilitySprites.TryGetValue(lookup, out Sprite s);
        return s;
    }

    private static PlayerHUD _instance;
    private static Sprite _whiteSprite;
    private static Font _defaultFont;

    private Canvas _canvas;
    private PlayerHealth _playerHealth;
    private PlayerCombatController _combat;
    private BossAIController _boss;

    private RectTransform _playerHealthFill;
    private Image _playerHealthFillImage;
    private Text _playerHealthText;
    private RectTransform _staminaFill;
    private Text _staminaText;
    private GameObject _bossRoot;
    private RectTransform _bossHealthFill;
    private Text _bossNameText;
    private Text _bossHealthText;
    private Text _bossPhaseText;
    private Text _bossSubtitleText;
    private readonly List<MoveSlotUI> _moveSlots = new List<MoveSlotUI>();

    // Status pill (heat mode / hidden assist)
    private GameObject _statusPill;
    private Text _statusText;
    private Image _statusPillImage;

    private float _nextLookupTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PlayerHUD>() != null)
            return;

        if (FindFirstObjectByType<PlayerCombatController>() == null &&
            FindFirstObjectByType<PlayerMovement>() == null)
            return;

        GameObject hudObject = new GameObject("PlayerHUD", typeof(RectTransform), typeof(PlayerHUD));
        hudObject.hideFlags = HideFlags.None;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        EnsureCanvas();
        BuildHud();
        PreloadAbilitySprites();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        TryResolveReferences();
        UpdatePlayerHealth();
        UpdateStamina();
        UpdateBossHealth();
        UpdateMoveStrip();
        UpdateStatusPill();
    }

    private void EnsureCanvas()
    {
        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 30;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }

    private void BuildHud()
    {
        RectTransform bossBar = CreateElement("BossBar", transform);
        SetAnchoredBox(bossBar, new Vector2(0.5f, 1f), new Vector2(760f, 88f), new Vector2(0f, -28f));
        AddImage(bossBar.gameObject, new Color(0.05f, 0.05f, 0.06f, 0.96f));
        CreateEdge(bossBar, new Color(0.63f, 0.16f, 0.12f), 6f, edgeTop: true);

        _bossNameText = CreateText(
            "BossName",
            bossBar,
            "BOSS",
            22,
            TextAnchor.UpperCenter,
            new Color(0.95f, 0.92f, 0.87f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -10f),
            new Vector2(660f, 26f));

        _bossPhaseText = CreateText(
            "BossPhaseText",
            bossBar,
            "PHASE I",
            13,
            TextAnchor.UpperLeft,
            new Color(0.95f, 0.79f, 0.32f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(18f, -12f),
            new Vector2(240f, 22f));

        RectTransform bossBarFrame = CreateElement("BossBarFrame", bossBar);
        SetAnchoredBox(bossBarFrame, new Vector2(0.5f, 0.5f), new Vector2(680f, 24f), new Vector2(0f, -4f));
        AddImage(bossBarFrame.gameObject, new Color(0.16f, 0.11f, 0.11f, 1f));

        _bossHealthFill = CreateElement("BossHealthFill", bossBarFrame);
        Stretch(_bossHealthFill, 3f, 3f, 3f, 3f);
        AddImage(_bossHealthFill.gameObject, new Color(0.82f, 0.2f, 0.18f));

        _bossHealthText = CreateText(
            "BossHealthText",
            bossBar,
            "0 / 0",
            18,
            TextAnchor.MiddleCenter,
            Color.white,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 12f),
            new Vector2(220f, 22f));

        _bossSubtitleText = CreateText(
            "BossSubtitleText",
            bossBar,
            string.Empty,
            12,
            TextAnchor.MiddleCenter,
            new Color(0.80f, 0.78f, 0.74f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 32f),
            new Vector2(560f, 18f));
        _bossSubtitleText.resizeTextForBestFit = true;
        _bossSubtitleText.resizeTextMinSize = 10;
        _bossSubtitleText.resizeTextMaxSize = 12;

        _bossRoot = bossBar.gameObject;
        _bossRoot.SetActive(false);

        RectTransform bottomHud = CreateElement("BottomHud", transform);
        SetAnchoredBox(bottomHud, new Vector2(0.5f, 0f), new Vector2(960f, 110f), new Vector2(0f, 14f));

        // ── Player panel — card style matching ability bar ──────────────────
        RectTransform playerPanel = CreateElement("PlayerPanel", bottomHud);
        SetAnchoredBox(playerPanel, new Vector2(0f, 0f), new Vector2(290f, 116f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        AddImage(playerPanel.gameObject, new Color(0.06f, 0.06f, 0.08f, 0.97f));

        // Gold borders — all 4 sides (3px), same as ability cards
        { var b = AddImage(CreateElement("BT", playerPanel).gameObject, new Color(0.85f, 0.68f, 0.18f, 1f));
          b.rectTransform.anchorMin = new Vector2(0f,1f); b.rectTransform.anchorMax = Vector2.one;
          b.rectTransform.pivot = new Vector2(0.5f,1f); b.rectTransform.sizeDelta = new Vector2(0f,3f); b.rectTransform.anchoredPosition = Vector2.zero; }
        { var b = AddImage(CreateElement("BB", playerPanel).gameObject, new Color(0.85f, 0.68f, 0.18f, 1f));
          b.rectTransform.anchorMin = Vector2.zero; b.rectTransform.anchorMax = new Vector2(1f,0f);
          b.rectTransform.pivot = new Vector2(0.5f,0f); b.rectTransform.sizeDelta = new Vector2(0f,3f); b.rectTransform.anchoredPosition = Vector2.zero; }
        { var b = AddImage(CreateElement("BL", playerPanel).gameObject, new Color(0.85f, 0.68f, 0.18f, 1f));
          b.rectTransform.anchorMin = Vector2.zero; b.rectTransform.anchorMax = new Vector2(0f,1f);
          b.rectTransform.pivot = new Vector2(0f,0.5f); b.rectTransform.sizeDelta = new Vector2(3f,0f); b.rectTransform.anchoredPosition = Vector2.zero; }
        { var b = AddImage(CreateElement("BR", playerPanel).gameObject, new Color(0.85f, 0.68f, 0.18f, 1f));
          b.rectTransform.anchorMin = new Vector2(1f,0f); b.rectTransform.anchorMax = Vector2.one;
          b.rectTransform.pivot = new Vector2(1f,0.5f); b.rectTransform.sizeDelta = new Vector2(3f,0f); b.rectTransform.anchoredPosition = Vector2.zero; }

        // Dark header strip (top ~32% of card)
        var ppHeader = AddImage(CreateElement("Header", playerPanel).gameObject, new Color(0.04f, 0.04f, 0.06f, 1f));
        ppHeader.rectTransform.anchorMin = new Vector2(0f, 0.68f); ppHeader.rectTransform.anchorMax = Vector2.one;
        ppHeader.rectTransform.offsetMin = new Vector2(3f, 0f); ppHeader.rectTransform.offsetMax = new Vector2(-3f, -3f);

        // "KARYS" in gold — left of header
        CreateText("PlayerLabel", ppHeader.transform, "KARYS", 20,
            TextAnchor.MiddleLeft, new Color(0.97f, 0.84f, 0.28f),
            Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
            new Vector2(12f, 0f), new Vector2(120f, 32f));

        // HP numbers — right of header
        _playerHealthText = CreateText("PlayerHealthText", ppHeader.transform, "0 / 0", 13,
            TextAnchor.MiddleRight, new Color(0.85f, 0.85f, 0.88f),
            Vector2.zero, Vector2.one, new Vector2(1f, 0.5f),
            new Vector2(-10f, 0f), new Vector2(120f, 32f));

        // Health bar — sits below header strip
        RectTransform playerBarFrame = CreateElement("PlayerBarFrame", playerPanel);
        playerBarFrame.anchorMin = new Vector2(0f, 0.36f); playerBarFrame.anchorMax = new Vector2(1f, 0.66f);
        playerBarFrame.offsetMin = new Vector2(10f, 0f); playerBarFrame.offsetMax = new Vector2(-10f, 0f);
        AddImage(playerBarFrame.gameObject, new Color(0.10f, 0.10f, 0.12f, 1f));

        _playerHealthFill = CreateElement("PlayerHealthFill", playerBarFrame);
        Stretch(_playerHealthFill, 3f, 3f, 3f, 3f);
        _playerHealthFillImage = AddImage(_playerHealthFill.gameObject, new Color(0.21f, 0.74f, 0.28f));

        // Thin gold accent line under health bar label
        { var acc = AddImage(CreateElement("HAcc", playerBarFrame).gameObject, new Color(0.85f, 0.68f, 0.18f, 0.5f));
          acc.rectTransform.anchorMin = Vector2.zero; acc.rectTransform.anchorMax = new Vector2(1f,0f);
          acc.rectTransform.pivot = new Vector2(0.5f,0f); acc.rectTransform.sizeDelta = new Vector2(0f,1f); acc.rectTransform.anchoredPosition = Vector2.zero; }

        // Stamina bar — bottom strip
        RectTransform staminaBarFrame = CreateElement("StaminaBarFrame", playerPanel);
        staminaBarFrame.anchorMin = new Vector2(0f, 0.08f); staminaBarFrame.anchorMax = new Vector2(1f, 0.32f);
        staminaBarFrame.offsetMin = new Vector2(10f, 0f); staminaBarFrame.offsetMax = new Vector2(-10f, 0f);
        AddImage(staminaBarFrame.gameObject, new Color(0.08f, 0.08f, 0.10f, 1f));

        _staminaFill = CreateElement("StaminaFill", staminaBarFrame);
        Stretch(_staminaFill, 3f, 3f, 3f, 3f);
        AddImage(_staminaFill.gameObject, new Color(0.22f, 0.60f, 0.90f));

        _staminaText = CreateText("StaminaText", staminaBarFrame.transform, "STA", 9,
            TextAnchor.MiddleLeft, new Color(0.55f, 0.80f, 1f),
            Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
            new Vector2(5f, 0f), new Vector2(40f, 18f));

        // ── Status pill — shows HEAT MODE / ASSIST in bottom-left ───────────
        _statusPill = new GameObject("StatusPill");
        _statusPill.transform.SetParent(transform, false);
        RectTransform pillRT = _statusPill.AddComponent<RectTransform>();
        pillRT.anchorMin = new Vector2(0f, 0f);
        pillRT.anchorMax = new Vector2(0f, 0f);
        pillRT.pivot     = new Vector2(0f, 0f);
        pillRT.sizeDelta = new Vector2(180f, 32f);
        pillRT.anchoredPosition = new Vector2(20f, 240f);
        _statusPillImage = _statusPill.AddComponent<Image>();
        _statusPillImage.sprite = GetWhiteSprite();
        _statusPillImage.color  = new Color(0.8f, 0.4f, 0.1f, 0.88f);
        _statusPillImage.raycastTarget = false;

        _statusText = CreateText(
            "StatusText",
            _statusPill.transform,
            "HEAT MODE",
            16,
            TextAnchor.MiddleCenter,
            Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(180f, 32f));
        _statusPill.SetActive(false);

        RectTransform moveStrip = CreateElement("MoveStrip", bottomHud);
        SetAnchoredBox(moveStrip, new Vector2(1f, 0f), new Vector2(640f, 96f), new Vector2(0f, 0f), new Vector2(1f, 0f));
        AddImage(moveStrip.gameObject, new Color(0.04f, 0.04f, 0.05f, 0.0f)); // transparent strip bg

        HorizontalLayoutGroup layout = moveStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding    = new RectOffset(0, 0, 0, 0);
        layout.spacing    = 6f;
        layout.childAlignment       = TextAnchor.MiddleCenter;
        layout.childControlWidth    = false;
        layout.childControlHeight   = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < 6; i++)
            _moveSlots.Add(BuildMoveSlot(moveStrip));
    }

    private MoveSlotUI BuildMoveSlot(Transform parent)
    {
        MoveSlotUI slot = new MoveSlotUI();

        // ── Card root ─────────────────────────────────────────────────────────
        slot.Root = CreateElement("MoveSlot", parent);
        var le = slot.Root.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth  = 114f;
        le.preferredHeight = 88f;

        // Dark card background
        slot.Background = AddImage(slot.Root.gameObject, new Color(0.06f, 0.06f, 0.08f, 0.96f));
        slot.ReadyGlow = AddImage(CreateElement("ReadyGlow", slot.Root).gameObject, new Color(0.96f, 0.80f, 0.24f, 0f));
        Stretch(slot.ReadyGlow.rectTransform, -3f, -3f, -3f, -3f);
        slot.ReadyGlow.enabled = false;

        // Gold border — top
        var borderTop = AddImage(CreateElement("BT", slot.Root).gameObject, new Color(0.85f, 0.68f, 0.18f, 1f));
        Stretch(borderTop.rectTransform, 0f, 0f, 0f, 0f);
        borderTop.rectTransform.anchorMin = new Vector2(0f, 1f);
        borderTop.rectTransform.anchorMax = new Vector2(1f, 1f);
        borderTop.rectTransform.pivot     = new Vector2(0.5f, 1f);
        borderTop.rectTransform.sizeDelta = new Vector2(0f, 2f);
        borderTop.rectTransform.anchoredPosition = Vector2.zero;

        // Gold border — bottom
        var borderBot = AddImage(CreateElement("BB", slot.Root).gameObject, new Color(0.85f, 0.68f, 0.18f, 1f));
        borderBot.rectTransform.anchorMin = Vector2.zero;
        borderBot.rectTransform.anchorMax = new Vector2(1f, 0f);
        borderBot.rectTransform.pivot     = new Vector2(0.5f, 0f);
        borderBot.rectTransform.sizeDelta = new Vector2(0f, 2f);
        borderBot.rectTransform.anchoredPosition = Vector2.zero;

        // Gold border — left
        var borderL = AddImage(CreateElement("BL", slot.Root).gameObject, new Color(0.85f, 0.68f, 0.18f, 1f));
        borderL.rectTransform.anchorMin = Vector2.zero;
        borderL.rectTransform.anchorMax = new Vector2(0f, 1f);
        borderL.rectTransform.pivot     = new Vector2(0f, 0.5f);
        borderL.rectTransform.sizeDelta = new Vector2(2f, 0f);
        borderL.rectTransform.anchoredPosition = Vector2.zero;

        // Gold border — right
        var borderR = AddImage(CreateElement("BR", slot.Root).gameObject, new Color(0.85f, 0.68f, 0.18f, 1f));
        borderR.rectTransform.anchorMin = new Vector2(1f, 0f);
        borderR.rectTransform.anchorMax = Vector2.one;
        borderR.rectTransform.pivot     = new Vector2(1f, 0.5f);
        borderR.rectTransform.sizeDelta = new Vector2(2f, 0f);
        borderR.rectTransform.anchoredPosition = Vector2.zero;

        slot.Accent = borderTop; // kept for tinting via AccentColor

        // ── Art fills the top ~65% of the card ───────────────────────────────
        slot.IconPlate = AddImage(CreateElement("Art", slot.Root).gameObject, new Color(0.12f, 0.12f, 0.16f, 1f));
        slot.IconPlate.rectTransform.anchorMin = new Vector2(0f, 0.28f);
        slot.IconPlate.rectTransform.anchorMax = Vector2.one;
        slot.IconPlate.rectTransform.offsetMin = new Vector2(2f, 0f);
        slot.IconPlate.rectTransform.offsetMax = new Vector2(-2f, -2f);
        slot.IconPlate.preserveAspect = false;
        slot.IconSprite = slot.IconPlate;

        // Fallback letter icon (centre of art area)
        slot.IconText = CreateText(
            "IconText", slot.IconPlate.transform, "--", 26,
            TextAnchor.MiddleCenter, new Color(0.97f, 0.92f, 0.82f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(90f, 40f));

        // ── Bottom info strip (bottom 28%) ────────────────────────────────────
        var infoStrip = AddImage(CreateElement("Info", slot.Root).gameObject, new Color(0.06f, 0.06f, 0.09f, 1f));
        infoStrip.rectTransform.anchorMin = Vector2.zero;
        infoStrip.rectTransform.anchorMax = new Vector2(1f, 0.28f);
        infoStrip.rectTransform.offsetMin = new Vector2(2f, 2f);
        infoStrip.rectTransform.offsetMax = new Vector2(-2f, 0f);

        // Key bind — top-left of info strip (gold)
        slot.KeyText = CreateText(
            "KeyText", infoStrip.transform, "--", 10,
            TextAnchor.UpperLeft, new Color(0.95f, 0.78f, 0.20f),
            Vector2.zero, Vector2.one, new Vector2(0f, 1f),
            new Vector2(4f, -2f), new Vector2(30f, 14f));

        // Ability name — bottom of info strip (white)
        slot.NameText = CreateText(
            "NameText", infoStrip.transform, "Move", 10,
            TextAnchor.LowerCenter, new Color(0.92f, 0.90f, 0.86f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0f),
            new Vector2(0f, 2f), new Vector2(110f, 14f));
        slot.NameText.resizeTextForBestFit = true;
        slot.NameText.resizeTextMinSize    = 8;
        slot.NameText.resizeTextMaxSize    = 10;

        // ── Cooldown overlay (over art) ───────────────────────────────────────
        slot.CooldownOverlay = AddImage(CreateElement("CDOverlay", slot.IconPlate.transform).gameObject,
            new Color(0.03f, 0.03f, 0.05f, 0.82f));
        Stretch(slot.CooldownOverlay.rectTransform, 0f, 0f, 0f, 0f);
        slot.CooldownOverlay.type        = Image.Type.Filled;
        slot.CooldownOverlay.fillMethod  = Image.FillMethod.Vertical;
        slot.CooldownOverlay.fillOrigin  = (int)Image.OriginVertical.Top;
        slot.CooldownOverlay.fillAmount  = 0f;

        slot.CooldownText = CreateText(
            "CDText", slot.IconPlate.transform, string.Empty, 20,
            TextAnchor.MiddleCenter, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(80f, 30f));
        slot.CooldownText.resizeTextForBestFit = true;
        slot.CooldownText.resizeTextMinSize    = 14;
        slot.CooldownText.resizeTextMaxSize    = 20;

        return slot;
    }

    private void TryResolveReferences()
    {
        if (Time.unscaledTime < _nextLookupTime)
            return;

        _nextLookupTime = Time.unscaledTime + 0.5f;

        if (_combat == null)
            _combat = FindFirstObjectByType<PlayerCombatController>();

        if (_playerHealth == null)
            _playerHealth = FindFirstObjectByType<PlayerHealth>();

        BossAIController trackedBoss = _combat != null ? _combat.CurrentBoss : null;
        if (trackedBoss != null)
            _boss = trackedBoss;
        else if (_boss == null)
            _boss = FindFirstObjectByType<BossAIController>();
    }

    private void UpdatePlayerHealth()
    {
        if (_playerHealth == null || _playerHealthFill == null)
            return;

        float maxHealth = Mathf.Max(1f, _playerHealth.MaxHealth);
        float ratio = Mathf.Clamp01(_playerHealth.CurrentHealth / maxHealth);
        SetFillWidth(_playerHealthFill, ratio, 4f);
        _playerHealthFillImage.color = Color.Lerp(new Color(0.77f, 0.16f, 0.16f), new Color(0.21f, 0.74f, 0.28f), ratio);
        _playerHealthText.text = $"{_playerHealth.CurrentHealth:0} / {_playerHealth.MaxHealth:0}";
    }

    private void UpdateStamina()
    {
        if (_staminaFill == null) return;

        RollSystem rs = RollSystem.Instance;
        if (rs == null) return;

        float ratio = rs.MaxStamina > 0f ? Mathf.Clamp01(rs.CurrentStamina / rs.MaxStamina) : 1f;
        SetFillWidth(_staminaFill, ratio, 3f);

        if (_staminaText != null)
        {
            _staminaText.text = $"STA  {rs.CurrentStamina:0}/{rs.MaxStamina:0}";
            _staminaText.color = ratio < 0.25f
                ? new Color(0.98f, 0.52f, 0.30f)
                : new Color(0.55f, 0.80f, 1f);
        }
    }

    private void UpdateStatusPill()
    {
        if (_statusPill == null) return;

        FightProgressionManager fpm = FightProgressionManager.Instance;
        if (fpm == null) { _statusPill.SetActive(false); return; }

        if (fpm.IsHeatModeActive)
        {
            _statusPill.SetActive(true);
            _statusText.text = "HEAT MODE";
            _statusPillImage.color = new Color(0.85f, 0.35f, 0.05f, 0.90f);
        }
        else if (fpm.IsHiddenAssistActive)
        {
            _statusPill.SetActive(true);
            _statusText.text = "ASSISTED";
            _statusPillImage.color = new Color(0.10f, 0.45f, 0.75f, 0.88f);
        }
        else
        {
            _statusPill.SetActive(false);
        }
    }

    private void UpdateBossHealth()
    {
        if (_bossRoot == null || _bossHealthFill == null)
            return;

        BossAIController trackedBoss = _combat != null ? _combat.CurrentBoss : null;
        if (trackedBoss != null)
            _boss = trackedBoss;

        bool showBoss = _boss != null &&
                        _boss.gameObject.activeInHierarchy &&
                        (_boss.IsInCombat() || _boss.GetCurrentHealth() < _boss.GetMaxHealth());

        _bossRoot.SetActive(showBoss);
        if (!showBoss)
            return;

        float maxHealth = Mathf.Max(1f, _boss.GetMaxHealth());
        float ratio = Mathf.Clamp01(_boss.GetCurrentHealth() / maxHealth);
        SetFillWidth(_bossHealthFill, ratio, 3f);
        _bossNameText.text = _boss.GetBossName().ToUpperInvariant();
        _bossHealthText.text = $"{_boss.GetCurrentHealth():0} / {_boss.GetMaxHealth():0}";

        FightProgressionManager fpm = FightProgressionManager.Instance;
        if (fpm != null)
        {
            _bossPhaseText.text = fpm.CurrentPhaseName.ToUpperInvariant();
            _bossSubtitleText.text = fpm.CurrentPhaseSubtitle;
            Image bossFillImage = _bossHealthFill.GetComponent<Image>();
            if (bossFillImage != null)
                bossFillImage.color = GetPhaseHealthColor(fpm.CurrentPhaseIndex);
        }
        else
        {
            _bossPhaseText.text = "BOSS ENGAGED";
            _bossSubtitleText.text = string.Empty;
        }
    }

    private void UpdateMoveStrip()
    {
        for (int i = 0; i < _moveSlots.Count; i++)
        {
            MoveSlotUI slot = _moveSlots[i];
            if (_combat == null || !_combat.TryGetHudMoveData(i, out PlayerCombatController.HudMoveData moveData))
            {
                slot.Root.gameObject.SetActive(false);
                continue;
            }

            slot.Root.gameObject.SetActive(true);
            slot.KeyText.text  = moveData.Keybind;
            slot.NameText.text = moveData.DisplayName;
            slot.IconText.text = moveData.IconLabel;

            // Show ability art or fallback letter
            Sprite art = GetAbilitySprite(moveData.DisplayName);
            if (art != null)
            {
                slot.IconPlate.sprite = art;
                slot.IconPlate.color  = moveData.IsUsable ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);
                slot.IconText.gameObject.SetActive(false);
            }
            else
            {
                slot.IconPlate.sprite = GetWhiteSprite();
                slot.IconPlate.color  = moveData.IsUsable
                    ? new Color(moveData.AccentColor.r * 0.45f, moveData.AccentColor.g * 0.45f, moveData.AccentColor.b * 0.45f, 1f)
                    : new Color(0.12f, 0.12f, 0.16f, 1f);
                slot.IconText.gameObject.SetActive(true);
            }

            bool onCooldown = moveData.RemainingCooldown > 0.01f;
            float cooldownRatio = moveData.Cooldown > 0f
                ? Mathf.Clamp01(moveData.RemainingCooldown / moveData.Cooldown)
                : 0f;
            bool readyPulse = moveData.IsUsable && !onCooldown && moveData.Cooldown > 0.9f;

            // Gold border dims when on cooldown or not usable
            float borderAlpha = !moveData.IsUsable ? 0.35f : onCooldown ? 0.6f : 1f;
            Color gold = new Color(0.85f, 0.68f, 0.18f, borderAlpha);
            slot.Accent.color = gold;

            slot.CooldownOverlay.fillAmount = cooldownRatio;
            slot.CooldownOverlay.enabled    = onCooldown;
            slot.CooldownText.text = onCooldown ? moveData.RemainingCooldown.ToString("0.0") : string.Empty;

            slot.Background.color = moveData.IsUsable
                ? new Color(0.06f, 0.06f, 0.08f, 0.96f)
                : new Color(0.04f, 0.04f, 0.05f, 0.96f);

            slot.IconText.color = moveData.IsUsable
                ? new Color(0.97f, 0.92f, 0.82f, onCooldown ? 0.5f : 1f)
                : new Color(0.55f, 0.55f, 0.58f, 0.8f);

            slot.NameText.color = moveData.IsUsable
                ? new Color(0.92f, 0.90f, 0.86f)
                : new Color(0.45f, 0.45f, 0.47f);

            slot.KeyText.color = moveData.IsUsable
                ? new Color(0.95f, 0.78f, 0.20f)
                : new Color(0.40f, 0.40f, 0.42f);

            if (readyPulse)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * 4.5f + i * 0.7f) * 0.5f + 0.5f;
                slot.ReadyGlow.enabled = true;
                slot.ReadyGlow.color = new Color(
                    moveData.AccentColor.r,
                    moveData.AccentColor.g,
                    moveData.AccentColor.b,
                    0.08f + pulse * 0.12f);
                slot.Root.localScale = Vector3.one * (1f + pulse * 0.025f);
            }
            else
            {
                slot.ReadyGlow.enabled = false;
                slot.Root.localScale = Vector3.one;
            }
        }
    }

    private static Color GetPhaseHealthColor(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 1:
                return new Color(0.82f, 0.38f, 0.20f);
            case 2:
                return new Color(0.86f, 0.18f, 0.18f);
            case 3:
                return new Color(0.95f, 0.12f, 0.28f);
            default:
                return new Color(0.82f, 0.2f, 0.18f);
        }
    }

    private static RectTransform CreateElement(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static void SetAnchoredBox(RectTransform rectTransform, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
    {
        SetAnchoredBox(rectTransform, anchor, size, anchoredPosition, anchor);
    }

    private static void SetAnchoredBox(RectTransform rectTransform, Vector2 anchor, Vector2 size, Vector2 anchoredPosition, Vector2 pivot)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private static void Stretch(RectTransform rectTransform, float left, float bottom, float right, float top)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private static void SetFillWidth(RectTransform rectTransform, float ratio, float inset)
    {
        ratio = Mathf.Clamp01(ratio);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = new Vector2(ratio, 1f);

        if (ratio <= 0.001f)
        {
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return;
        }

        rectTransform.offsetMin = new Vector2(inset, inset);
        rectTransform.offsetMax = new Vector2(-inset, -inset);
    }

    private static Image AddImage(GameObject gameObject, Color color)
    {
        Image image = gameObject.AddComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateEdge(RectTransform parent, Color color, float size, bool edgeTop)
    {
        RectTransform edge = CreateElement(edgeTop ? "TopEdge" : "BottomEdge", parent);
        edge.anchorMin = edgeTop ? new Vector2(0f, 1f) : Vector2.zero;
        edge.anchorMax = edgeTop ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        edge.pivot = edgeTop ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        edge.sizeDelta = new Vector2(0f, size);
        edge.anchoredPosition = Vector2.zero;
        AddImage(edge.gameObject, color);
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        TextAnchor alignment,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        RectTransform rectTransform = CreateElement(name, parent);
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        Text text = rectTransform.gameObject.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;

        Outline outline = rectTransform.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);

        return text;
    }

    private static Font GetDefaultFont()
    {
        if (_defaultFont == null)
        {
            try
            {
                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        return _defaultFont;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        _whiteSprite.name = "PlayerHUD_White";
        return _whiteSprite;
    }
}
