using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Polished chat UI with bubble-style messages, quick-reply buttons, and NPC header.
/// Player messages appear on the right (blue). NPC messages appear on the left (gray).
/// Self-builds all UI at runtime — no Inspector wiring or prefabs required.
/// </summary>
public class ChatDialogueUI : MonoBehaviour
{
    public static ChatDialogueUI Instance { get; private set; }

    // ── State ──────────────────────────────────────────────────────────────────
    public bool IsOpen { get; private set; }
    private NPCController _npc;
    private readonly List<GameObject> _bubbles = new List<GameObject>();
    private Text  _chatLog;
    private string _chatText = "";

    // ── UI refs ────────────────────────────────────────────────────────────────
    private GameObject   _panel;
    private Canvas       _canvas;
    private Text         _npcNameText;
    private Text         _relationText;
    private Text         _iconInitial;
    private ScrollRect   _scroll;
    private RectTransform _content;
    private InputField   _input;
    private Button       _sendBtn;
    private Button       _leaveBtn;

    // ── Palette ────────────────────────────────────────────────────────────────
    private static readonly Color C_PanelBg    = new Color(0.09f, 0.10f, 0.15f, 0.97f);
    private static readonly Color C_HeaderBg   = new Color(0.12f, 0.15f, 0.25f, 1.00f);
    private static readonly Color C_HeaderLine = new Color(0.30f, 0.45f, 0.75f, 0.90f);
    private static readonly Color C_ScrollBg   = new Color(0.07f, 0.08f, 0.12f, 1.00f);
    private static readonly Color C_QRBarBg    = new Color(0.10f, 0.12f, 0.20f, 1.00f);
    private static readonly Color C_QRBtn      = new Color(0.16f, 0.22f, 0.40f, 1.00f);
    private static readonly Color C_QRHover    = new Color(0.24f, 0.34f, 0.58f, 1.00f);
    private static readonly Color C_InputBg    = new Color(0.13f, 0.16f, 0.24f, 1.00f);
    private static readonly Color C_InputRowBg = new Color(0.10f, 0.12f, 0.20f, 1.00f);
    private static readonly Color C_SendBtn    = new Color(0.16f, 0.48f, 0.88f, 1.00f);
    private static readonly Color C_LeaveBtn   = new Color(0.55f, 0.18f, 0.18f, 1.00f);
    private static readonly Color C_PlayerBub  = new Color(0.18f, 0.42f, 0.80f, 1.00f);
    private static readonly Color C_NPCBub     = new Color(0.22f, 0.28f, 0.42f, 1.00f);
    private static readonly Color C_Border     = new Color(0.25f, 0.38f, 0.65f, 0.60f);
    private static readonly Color C_Gold       = new Color(0.98f, 0.82f, 0.22f, 1.00f);
    private static readonly Color C_TextLight  = new Color(0.93f, 0.93f, 0.95f, 1.00f);
    private static readonly Color C_TextDim    = new Color(0.50f, 0.52f, 0.58f, 1.00f);
    private static readonly Color C_IconBg     = new Color(0.22f, 0.40f, 0.76f, 1.00f);

    private Font _font;

    // ── Bootstrap ──────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ChatDialogueUI>() != null) return;
        new GameObject("ChatDialogueUI").AddComponent<ChatDialogueUI>();
    }

    // ── Quick reply topics ─────────────────────────────────────────────────────
    private static readonly string[] QuickReplies = {
        "Controls?",
        "How to fight?",
        "How to parry?",
        "How to dodge?",
        "Special moves?",
        "About the boss?",
        "Any tips?",
    };

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
        if (_panel != null)
            _panel.SetActive(false);
    }

    private void Start()
    {
        _sendBtn.onClick.AddListener(OnSend);
        _leaveBtn.onClick.AddListener(OnLeave);
        _input.onEndEdit.AddListener(s =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnSend();
        });
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) CloseChat();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void OpenChat(NPCController npc)
    {
        if (npc == null) return;
        if (_panel == null || _input == null || _chatLog == null)
        {
            Debug.LogWarning("[ChatDialogueUI] UI was not fully initialized. Rebuilding now.");
            BuildUI();
        }

        if (_panel == null || _input == null || _chatLog == null)
        {
            Debug.LogError("[ChatDialogueUI] Chat UI failed to initialize.");
            return;
        }

        _npc   = npc;
        IsOpen = true;

        ClearBubbles();
        _panel.SetActive(true);
        RefreshHeader();

        string greeting = _npc.GetResponse("hello");
        AddBubble(greeting, isPlayer: false);
        Debug.Log($"[ChatDialogueUI] Opened chat with {_npc.GetNPCId()}. Greeting length={greeting.Length}");

        FocusInput();
        SetPlayerMovement(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void CloseChat()
    {
        if (!IsOpen) return;
        IsOpen = false;
        _panel.SetActive(false);
        SetPlayerMovement(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        _npc = null;
    }

    // ── Input ──────────────────────────────────────────────────────────────────

    private void OnSend()
    {
        string text = _input.text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _input.text = string.Empty;
        Dispatch(text);
        FocusInput();
    }

    private void OnLeave()
    {
        Dispatch("Goodbye.");
        StartCoroutine(DelayClose(1.3f));
    }

    /// <summary>Send a quick-reply message (called by quick-reply buttons).</summary>
    public void SendQuickReply(string text)
    {
        _input.text = string.Empty;
        Dispatch(text);
        FocusInput();
    }

    private void Dispatch(string text)
    {
        if (_npc == null) return;
        AddBubble(text, isPlayer: true);

        // Use Groq AI when an API key is configured; keyword matching otherwise.
        if (GroqNPCResponder.Instance != null && GroqNPCResponder.Instance.IsAvailable)
        {
            GroqNPCResponder.Instance.Ask(
                _npc.GetNPCId(),
                text,
                onReply:    reply => { AddBubble(reply, isPlayer: false); RefreshHeader(); },
                onFallback: ()    => FallbackReply(text));
        }
        else
        {
            FallbackReply(text);
        }
    }

    private void FallbackReply(string text)
    {
        string reply = _npc != null ? _npc.GetResponse(text) : "...";
        StartCoroutine(NPCReplyAfterDelay(reply, 0.25f));
    }

    private IEnumerator NPCReplyAfterDelay(string text, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        AddBubble(text, isPlayer: false);
        RefreshHeader();
    }

    // ── Bubbles ────────────────────────────────────────────────────────────────

    private void AddBubble(string text, bool isPlayer)
    {
        // Append to the single text log — no complex RectTransform layout needed.
        string label = isPlayer
            ? "<color=#6ab0f5><b>You</b></color>"
            : $"<color=#ffd966><b>{GetNpcDisplayName()}</b></color>";
        string line = $"{label}:  {text}";
        _chatText = string.IsNullOrEmpty(_chatText) ? line : _chatText + "\n\n" + line;

        if (_chatLog != null)
        {
            _chatLog.text = _chatText;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_chatLog.rectTransform);

            RectTransform logRect = _chatLog.GetComponent<RectTransform>();
            logRect.offsetMin = new Vector2(12f, -12f);
            logRect.offsetMax = new Vector2(-12f, 0f);
            logRect.sizeDelta = new Vector2(0f, Mathf.Max(_chatLog.preferredHeight, 24f));

            float h = _chatLog.preferredHeight + 36f;
            if (_content != null)
                _content.sizeDelta = new Vector2(0f, Mathf.Max(h, 40f));
        }

        Debug.Log($"[ChatDialogueUI] Added {(isPlayer ? "player" : "npc")} line. Total chars={_chatText.Length}, preferredHeight={_chatLog.preferredHeight}");

        StartCoroutine(ScrollToBottom());
    }

    private static void AddSpacer(Transform parent)
    {
        var s  = MakeRect(parent, "Spc");
        var le = s.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
    }

    private void ClearBubbles()
    {
        foreach (var b in _bubbles) if (b) Destroy(b);
        _bubbles.Clear();
        _chatText  = "";
        if (_chatLog  != null) _chatLog.text = "";
        if (_content  != null) _content.sizeDelta = new Vector2(0f, 40f);
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (_scroll == null || _content == null) yield break;
        // Only scroll when content is taller than the viewport; otherwise leave at top.
        float viewH = _scroll.viewport != null ? _scroll.viewport.rect.height : 0f;
        if (_content.sizeDelta.y > viewH && viewH > 0f)
            _scroll.verticalNormalizedPosition = 0f;
    }

    private IEnumerator DelayClose(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        CloseChat();
    }

    // ── Header ─────────────────────────────────────────────────────────────────

    private void RefreshHeader()
    {
        if (_npc == null) return;
        string id = _npc.GetNPCId();
        _npcNameText.text = id;
        if (_iconInitial != null)
            _iconInitial.text = id.Length > 0 ? id[0].ToString().ToUpper() : "?";

        if (NPCMemoryManager.Instance != null)
        {
            NPCMemory mem = _npc.GetMemory();
            _relationText.text = mem != null
                ? NPCMemoryManager.GetRelationshipLevel(mem.relationshipScore)
                : "Stranger";
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void FocusInput()
    {
        _input.ActivateInputField();
        _input.Select();
    }

    private string GetNpcDisplayName()
    {
        return _npc != null ? _npc.GetNPCId() : "NPC";
    }

    private void SetPlayerMovement(bool on)
    {
        var pm  = FindFirstObjectByType<PlayerMovement>();
        if (pm  != null) pm.enabled  = on;
        var pcc = FindFirstObjectByType<PlayerCombatController>();
        if (pcc != null) pcc.enabled = on;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI CONSTRUCTION
    // ══════════════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        EnsureEventSystem();

        Transform existingCanvas = transform.Find("ChatCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas.gameObject);

        // Dedicated canvas
        var cvGO = new GameObject("ChatCanvas");
        cvGO.transform.SetParent(transform);
        _canvas = cvGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 40;
        var scaler = cvGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        cvGO.AddComponent<GraphicRaycaster>();
        var ct = cvGO.transform;

        // ── Root panel — right side of screen, compact ───────────────────────
        // Occupies the right ~38% of the screen so the game world stays visible.
        _panel = MakeRect(ct, "ChatPanel");
        SetImg(_panel, C_PanelBg);
        SetAnchors(_panel, 0.62f, 0.14f, 0.99f, 0.92f);

        // Thin border
        var border = MakeRect(_panel.transform, "Border");
        SetImg(border, C_Border);
        var bRT = border.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(-1, -1); bRT.offsetMax = new Vector2(1, 1);
        border.transform.SetAsFirstSibling();

        // ── Header (top 11%) ─────────────────────────────────────────────────
        var header = MakeRect(_panel.transform, "Header");
        SetImg(header, C_HeaderBg);
        SetAnchors(header, 0f, 0.89f, 1f, 1f);

        // Icon circle — left
        var iconGO = MakeRect(header.transform, "Icon");
        SetImg(iconGO, C_IconBg);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0f, 0.5f); iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot     = new Vector2(0f, 0.5f);
        iconRT.sizeDelta = new Vector2(32f, 32f);
        iconRT.anchoredPosition = new Vector2(10f, 0f);
        _iconInitial = MakeText(iconGO.transform, "?", 16, FontStyle.Bold,
            TextAnchor.MiddleCenter, Color.white);
        SetAnchors(_iconInitial.gameObject, 0f, 0f, 1f, 1f);

        // NPC name
        _npcNameText = MakeText(header.transform, "NPC", 14, FontStyle.Bold,
            TextAnchor.MiddleLeft, Color.white);
        var nameRT = _npcNameText.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0f); nameRT.anchorMax = new Vector2(0.6f, 1f);
        nameRT.offsetMin = new Vector2(50f, 0f); nameRT.offsetMax = Vector2.zero;

        // Relationship pill — right
        var pillGO = MakeRect(header.transform, "Pill");
        SetImg(pillGO, new Color(0.16f, 0.22f, 0.38f, 1f));
        var pillRT = pillGO.GetComponent<RectTransform>();
        pillRT.anchorMin = new Vector2(1f, 0.5f); pillRT.anchorMax = new Vector2(1f, 0.5f);
        pillRT.pivot     = new Vector2(1f, 0.5f);
        pillRT.sizeDelta = new Vector2(90f, 20f);
        pillRT.anchoredPosition = new Vector2(-8f, 0f);
        _relationText = MakeText(pillGO.transform, "Stranger", 10, FontStyle.Normal,
            TextAnchor.MiddleCenter, C_Gold);
        SetAnchors(_relationText.gameObject, 0f, 0f, 1f, 1f);

        // Divider under header
        var div = MakeRect(_panel.transform, "Div");
        SetImg(div, C_HeaderLine);
        SetAnchors(div, 0f, 0.887f, 1f, 0.890f);

        // ── Quick-reply bar (above input, ~7% height) ────────────────────────
        var qrBar = MakeRect(_panel.transform, "QRBar");
        SetImg(qrBar, C_QRBarBg);
        SetAnchors(qrBar, 0f, 0.115f, 1f, 0.175f);

        var qrDiv = MakeRect(_panel.transform, "QRDiv");
        SetImg(qrDiv, C_HeaderLine);
        SetAnchors(qrDiv, 0f, 0.172f, 1f, 0.175f);

        var qrHLG = qrBar.AddComponent<HorizontalLayoutGroup>();
        qrHLG.childControlHeight    = true;
        qrHLG.childForceExpandHeight = true;
        qrHLG.childControlWidth     = false;
        qrHLG.childForceExpandWidth = false;
        qrHLG.spacing = 5;
        qrHLG.padding = new RectOffset(8, 8, 5, 5);

        foreach (var qr in QuickReplies)
        {
            var btnGO = MakeRect(qrBar.transform, "QR");
            SetImg(btnGO, C_QRBtn);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(95f, 24f);
            var btn  = btnGO.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = C_QRBtn;
            cols.highlightedColor = C_QRHover;
            cols.pressedColor     = new Color(0.09f, 0.13f, 0.24f, 1f);
            btn.colors = cols;
            var lbl = MakeText(btnGO.transform, qr, 10, FontStyle.Normal,
                TextAnchor.MiddleCenter, C_TextLight);
            SetAnchors(lbl.gameObject, 0f, 0f, 1f, 1f);
            string cap = qr;
            btn.onClick.AddListener(() => SendQuickReply(cap));
        }

        // ── Scroll area (header bottom → qr bar top) ────────────────────────
        var scrollGO = MakeRect(_panel.transform, "Scroll");
        SetImg(scrollGO, C_ScrollBg);
        SetAnchors(scrollGO, 0f, 0.175f, 1f, 0.887f);
        _scroll = scrollGO.AddComponent<ScrollRect>();

        var vpGO = MakeRect(scrollGO.transform, "VP");
        SetImg(vpGO, Color.clear);
        SetAnchors(vpGO, 0f, 0f, 1f, 1f);
        vpGO.AddComponent<RectMask2D>();

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        _content           = contentGO.AddComponent<RectTransform>();
        _content.anchorMin        = new Vector2(0f, 1f);
        _content.anchorMax        = new Vector2(1f, 1f);
        _content.pivot            = new Vector2(0.5f, 1f);
        _content.sizeDelta        = new Vector2(0f, 40f);
        _content.anchoredPosition = Vector2.zero;

        // Single text component — all messages appended here, no layout complexity.
        var logGO = new GameObject("Log", typeof(RectTransform), typeof(Text));
        logGO.transform.SetParent(_content, false);
        var logRT = logGO.GetComponent<RectTransform>();
        logRT.anchorMin        = new Vector2(0f, 1f);
        logRT.anchorMax        = new Vector2(1f, 1f);
        logRT.pivot            = new Vector2(0.5f, 1f);
        logRT.anchoredPosition = new Vector2(12f, -12f);
        logRT.offsetMin        = new Vector2(12f, -12f);
        logRT.offsetMax        = new Vector2(-12f, 0f);
        logRT.sizeDelta        = new Vector2(0f, 24f);
        _chatLog = logGO.GetComponent<Text>();
        _chatLog.font               = _font;
        _chatLog.fontSize           = 18;
        _chatLog.color              = C_TextLight;
        _chatLog.alignment          = TextAnchor.UpperLeft;
        _chatLog.horizontalOverflow = HorizontalWrapMode.Wrap;
        _chatLog.verticalOverflow   = VerticalWrapMode.Overflow;
        _chatLog.supportRichText    = true;
        _chatLog.raycastTarget      = false;

        _scroll.content          = _content;
        _scroll.viewport         = vpGO.GetComponent<RectTransform>();
        _scroll.horizontal       = false;
        _scroll.vertical         = true;
        _scroll.scrollSensitivity = 25f;
        _scroll.movementType     = ScrollRect.MovementType.Clamped;

        // ── Input row (bottom 11.5%) ─────────────────────────────────────────
        var inputRow = MakeRect(_panel.transform, "InputRow");
        SetImg(inputRow, C_InputRowBg);
        SetAnchors(inputRow, 0f, 0f, 1f, 0.115f);
        var iHLG = inputRow.AddComponent<HorizontalLayoutGroup>();
        iHLG.childControlWidth    = true;
        iHLG.childForceExpandWidth = false;
        iHLG.childControlHeight   = true;
        iHLG.childForceExpandHeight = true;
        iHLG.spacing = 6;
        iHLG.padding = new RectOffset(10, 10, 8, 8);

        _input  = BuildInputField(inputRow.transform);
        _input.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        _sendBtn  = BuildButton(inputRow.transform, "Send",  C_SendBtn,  68f);
        _leaveBtn = BuildButton(inputRow.transform, "Leave", C_LeaveBtn, 62f);
    }

    // ── UI factory helpers ─────────────────────────────────────────────────────

    private static GameObject MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static Image SetImg(GameObject go, Color col)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = col;
        return img;
    }

    private Text MakeText(Transform parent, string body, int size, FontStyle style,
        TextAnchor align, Color col)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = _font; t.text = body; t.fontSize = size;
        t.fontStyle = style; t.alignment = align; t.color = col;
        return t;
    }

    private static void SetAnchors(GameObject go, float x0, float y0, float x1, float y1)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
    }

    private Button BuildButton(Transform parent, string label, Color bg, float minW)
    {
        var go = MakeRect(parent, label + "Btn");
        SetImg(go, bg);
        go.AddComponent<LayoutElement>().minWidth = minW;
        var btn  = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.normalColor      = bg;
        cols.highlightedColor = bg * 1.25f;
        cols.pressedColor     = bg * 0.70f;
        btn.colors = cols;
        var lbl = MakeText(go.transform, label, 13, FontStyle.Bold,
            TextAnchor.MiddleCenter, Color.white);
        SetAnchors(lbl.gameObject, 0f, 0f, 1f, 1f);
        return btn;
    }

    private InputField BuildInputField(Transform parent)
    {
        var go = MakeRect(parent, "Input");
        SetImg(go, C_InputBg);
        var field = go.AddComponent<InputField>();

        var ph   = MakeRect(go.transform, "PH");
        var phRT = ph.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(10, 4); phRT.offsetMax = new Vector2(-6, -4);
        var phT = ph.AddComponent<Text>();
        phT.font = _font; phT.text = "Say something, or use a quick reply above…";
        phT.fontSize = 13; phT.fontStyle = FontStyle.Italic;
        phT.color = C_TextDim; phT.alignment = TextAnchor.MiddleLeft;

        var tx   = MakeRect(go.transform, "TX");
        var txRT = tx.GetComponent<RectTransform>();
        txRT.anchorMin = Vector2.zero; txRT.anchorMax = Vector2.one;
        txRT.offsetMin = new Vector2(10, 4); txRT.offsetMax = new Vector2(-6, -4);
        var txT = tx.AddComponent<Text>();
        txT.font = _font; txT.fontSize = 13;
        txT.color = Color.white; txT.alignment = TextAnchor.MiddleLeft;
        txT.supportRichText = false;

        field.placeholder   = phT;
        field.textComponent = txT;
        return field;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        DontDestroyOnLoad(go);
    }
}
