using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private sealed class MoveSlotUI
    {
        public RectTransform Root;
        public Image Background;
        public Image Accent;
        public Image IconPlate;
        public Image CooldownOverlay;
        public Text IconText;
        public Text KeyText;
        public Text NameText;
        public Text CooldownText;
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
    private GameObject _bossRoot;
    private RectTransform _bossHealthFill;
    private Text _bossNameText;
    private Text _bossHealthText;
    private readonly List<MoveSlotUI> _moveSlots = new List<MoveSlotUI>();

    private float _nextLookupTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<PlayerHUD>() != null)
            return;

        if (FindObjectOfType<PlayerCombatController>() == null &&
            FindObjectOfType<PlayerMovement>() == null)
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
        UpdateBossHealth();
        UpdateMoveStrip();
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

        _bossRoot = bossBar.gameObject;
        _bossRoot.SetActive(false);

        RectTransform bottomHud = CreateElement("BottomHud", transform);
        SetAnchoredBox(bottomHud, new Vector2(0.5f, 0f), new Vector2(1120f, 190f), new Vector2(0f, 18f));

        RectTransform playerPanel = CreateElement("PlayerPanel", bottomHud);
        SetAnchoredBox(playerPanel, new Vector2(0f, 0f), new Vector2(280f, 122f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        AddImage(playerPanel.gameObject, new Color(0.05f, 0.05f, 0.06f, 0.96f));
        CreateEdge(playerPanel, new Color(0.16f, 0.57f, 0.26f), 6f, edgeTop: true);

        CreateText(
            "PlayerLabel",
            playerPanel,
            "PLAYER",
            22,
            TextAnchor.UpperLeft,
            new Color(0.95f, 0.92f, 0.87f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -12f),
            new Vector2(160f, 26f));

        RectTransform playerBarFrame = CreateElement("PlayerBarFrame", playerPanel);
        SetAnchoredBox(playerBarFrame, new Vector2(0f, 0f), new Vector2(236f, 28f), new Vector2(20f, 36f), new Vector2(0f, 0f));
        AddImage(playerBarFrame.gameObject, new Color(0.13f, 0.12f, 0.12f, 1f));

        _playerHealthFill = CreateElement("PlayerHealthFill", playerBarFrame);
        Stretch(_playerHealthFill, 4f, 4f, 4f, 4f);
        _playerHealthFillImage = AddImage(_playerHealthFill.gameObject, new Color(0.21f, 0.74f, 0.28f));

        _playerHealthText = CreateText(
            "PlayerHealthText",
            playerPanel,
            "0 / 0",
            18,
            TextAnchor.MiddleLeft,
            Color.white,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(20f, 12f),
            new Vector2(200f, 20f));

        RectTransform moveStrip = CreateElement("MoveStrip", bottomHud);
        SetAnchoredBox(moveStrip, new Vector2(1f, 0f), new Vector2(814f, 126f), new Vector2(0f, 0f), new Vector2(1f, 0f));
        AddImage(moveStrip.gameObject, new Color(0.05f, 0.05f, 0.06f, 0.96f));
        CreateEdge(moveStrip, new Color(0.73f, 0.58f, 0.23f), 6f, edgeTop: true);

        HorizontalLayoutGroup layout = moveStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < 6; i++)
            _moveSlots.Add(BuildMoveSlot(moveStrip));
    }

    private MoveSlotUI BuildMoveSlot(Transform parent)
    {
        MoveSlotUI slot = new MoveSlotUI();

        slot.Root = CreateElement("MoveSlot", parent);
        LayoutElement layoutElement = slot.Root.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 118f;
        layoutElement.preferredHeight = 98f;

        slot.Background = AddImage(slot.Root.gameObject, new Color(0.12f, 0.12f, 0.14f, 1f));

        slot.Accent = AddImage(CreateElement("Accent", slot.Root).gameObject, new Color(0.73f, 0.58f, 0.23f));
        Stretch(slot.Accent.rectTransform, 0f, 92f, 0f, 0f);

        slot.IconPlate = AddImage(CreateElement("IconPlate", slot.Root).gameObject, new Color(0.2f, 0.2f, 0.24f, 1f));
        Stretch(slot.IconPlate.rectTransform, 7f, 28f, 7f, 8f);

        slot.IconText = CreateText(
            "IconText",
            slot.IconPlate.transform,
            "--",
            30,
            TextAnchor.MiddleCenter,
            Color.white,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(80f, 42f));

        slot.KeyText = CreateText(
            "KeyText",
            slot.Root,
            "--",
            12,
            TextAnchor.UpperLeft,
            new Color(0.96f, 0.84f, 0.42f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(10f, -8f),
            new Vector2(96f, 16f));
        slot.KeyText.resizeTextForBestFit = true;
        slot.KeyText.resizeTextMinSize = 8;
        slot.KeyText.resizeTextMaxSize = 12;

        slot.NameText = CreateText(
            "NameText",
            slot.Root,
            "Move",
            13,
            TextAnchor.LowerCenter,
            new Color(0.94f, 0.92f, 0.88f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 8f),
            new Vector2(104f, 18f));
        slot.NameText.resizeTextForBestFit = true;
        slot.NameText.resizeTextMinSize = 9;
        slot.NameText.resizeTextMaxSize = 13;

        slot.CooldownOverlay = AddImage(CreateElement("CooldownOverlay", slot.IconPlate.transform).gameObject, new Color(0.04f, 0.04f, 0.05f, 0.78f));
        Stretch(slot.CooldownOverlay.rectTransform, 0f, 0f, 0f, 0f);
        slot.CooldownOverlay.type = Image.Type.Filled;
        slot.CooldownOverlay.fillMethod = Image.FillMethod.Vertical;
        slot.CooldownOverlay.fillOrigin = (int)Image.OriginVertical.Top;
        slot.CooldownOverlay.fillAmount = 0f;

        slot.CooldownText = CreateText(
            "CooldownText",
            slot.IconPlate.transform,
            string.Empty,
            24,
            TextAnchor.MiddleCenter,
            Color.white,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(80f, 36f));
        slot.CooldownText.resizeTextForBestFit = true;
        slot.CooldownText.resizeTextMinSize = 16;
        slot.CooldownText.resizeTextMaxSize = 24;

        return slot;
    }

    private void TryResolveReferences()
    {
        if (Time.unscaledTime < _nextLookupTime)
            return;

        _nextLookupTime = Time.unscaledTime + 0.5f;

        if (_combat == null)
            _combat = FindObjectOfType<PlayerCombatController>();

        if (_playerHealth == null)
            _playerHealth = FindObjectOfType<PlayerHealth>();

        BossAIController trackedBoss = _combat != null ? _combat.CurrentBoss : null;
        if (trackedBoss != null)
            _boss = trackedBoss;
        else if (_boss == null)
            _boss = FindObjectOfType<BossAIController>();
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
            slot.KeyText.text = moveData.Keybind;
            slot.NameText.text = moveData.DisplayName;
            slot.IconText.text = moveData.IconLabel;

            bool onCooldown = moveData.RemainingCooldown > 0.01f;
            float cooldownRatio = moveData.Cooldown > 0f
                ? Mathf.Clamp01(moveData.RemainingCooldown / moveData.Cooldown)
                : 0f;

            slot.Accent.color = moveData.AccentColor;
            slot.CooldownOverlay.fillAmount = cooldownRatio;
            slot.CooldownOverlay.enabled = onCooldown;
            slot.CooldownText.text = onCooldown ? moveData.RemainingCooldown.ToString("0.0") : string.Empty;

            Color iconPlateColor = Color.Lerp(new Color(0.17f, 0.17f, 0.2f), moveData.AccentColor * 0.42f, 0.6f);
            if (onCooldown)
                iconPlateColor *= 0.65f;
            if (!moveData.IsUsable)
                iconPlateColor *= 0.55f;

            slot.IconPlate.color = iconPlateColor;
            slot.Background.color = moveData.IsUsable
                ? new Color(0.12f, 0.12f, 0.14f, 1f)
                : new Color(0.09f, 0.09f, 0.1f, 1f);

            slot.IconText.color = moveData.IsUsable
                ? new Color(0.97f, 0.96f, 0.94f, onCooldown ? 0.55f : 1f)
                : new Color(0.65f, 0.65f, 0.68f, 0.8f);

            slot.NameText.color = moveData.IsUsable
                ? new Color(0.94f, 0.92f, 0.88f)
                : new Color(0.56f, 0.56f, 0.58f);

            slot.KeyText.color = moveData.IsUsable
                ? moveData.AccentColor
                : new Color(0.45f, 0.45f, 0.47f);
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
