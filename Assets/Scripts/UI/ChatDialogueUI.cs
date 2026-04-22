using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-contained chat UI. Creates all UI at runtime — no prefabs or Inspector wiring needed.
/// Just add this component to any always-active GameObject (e.g. GameInitializer) and it works.
/// </summary>
public class ChatDialogueUI : MonoBehaviour
{
    public static ChatDialogueUI Instance { get; private set; }

    // ── Runtime-created UI references ──────────────────────────────────────────
    private GameObject  _panel;
    private Text        _npcNameText;
    private Text        _relationText;
    private ScrollRect  _scroll;
    private RectTransform _content;
    private InputField  _input;
    private Button      _sendBtn;
    private Button      _leaveBtn;

    // ── State ──────────────────────────────────────────────────────────────────
    public bool IsOpen { get; private set; }

    private NPCController _npc;
    private readonly List<GameObject> _lines = new List<GameObject>();

    private readonly Color _playerColour = new Color(0.75f, 0.93f, 1.00f);
    private readonly Color _npcColour    = new Color(1.00f, 0.92f, 0.70f);

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Debug.Log("[ChatDialogueUI] Awake — building UI");
        BuildUI();
        _panel.SetActive(false);
    }

    private void Start()
    {
        _sendBtn.onClick.AddListener(OnSendClicked);
        _leaveBtn.onClick.AddListener(OnLeaveClicked);
        _input.onEndEdit.AddListener(s =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnSendClicked();
        });
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseChat();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void OpenChat(NPCController npc)
    {
        if (npc == null) return;
        _npc   = npc;
        IsOpen = true;

        ClearHistory();
        _panel.SetActive(true);
        RefreshHeader();

        string greeting = _npc.InteractWithPlayer("greet") ?? "Hello.";
        AppendMessage(_npc.GetNPCId(), greeting, _npcColour);
        RefreshHeader();

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

    private void OnSendClicked()
    {
        string text = _input.text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _input.text = string.Empty;
        ProcessText(text);
        FocusInput();
    }

    private void OnLeaveClicked()
    {
        AppendMessage("You", "Farewell.", _playerColour);
        string r = _npc?.InteractWithPlayer("bye") ?? "Goodbye.";
        AppendMessage(_npc?.GetNPCId() ?? "NPC", r, _npcColour);
        StartCoroutine(DelayClose(1.0f));
    }

    private void ProcessText(string raw)
    {
        if (_npc == null) return;
        string lo = raw.ToLowerInvariant();

        string action;
        if      (Has(lo, "hello","hi","hey","greet","howdy","morning","evening")) action = "greet";
        else if (Has(lo, "buy","sell","trade","shop","deal","goods","item","price","wares")) action = "trade";
        else if (Has(lo, "help","assist","quest","task","need","favour","favor","job")) action = "help";
        else if (Has(lo, "threat","kill","die","attack","hurt","warn","fight")) action = "threaten";
        else if (Has(lo, "bye","goodbye","farewell","later","leave","done","nothing")) action = "bye";
        else    action = "greet";

        AppendMessage("You", raw, _playerColour);
        string response = _npc.InteractWithPlayer(action) ?? "...";
        AppendMessage(_npc.GetNPCId(), response, _npcColour);
        RefreshHeader();

        if (action == "bye") StartCoroutine(DelayClose(1.0f));
    }

    // ── Chat history ───────────────────────────────────────────────────────────

    private void AppendMessage(string speaker, string message, Color col)
    {
        // Row
        var row   = new GameObject("Row");
        row.transform.SetParent(_content, false);
        var rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0,1); rowRT.anchorMax = new Vector2(1,1);
        rowRT.pivot     = new Vector2(0.5f,1f);
        var rowCSF = row.AddComponent<ContentSizeFitter>();
        rowCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var rowVLG = row.AddComponent<VerticalLayoutGroup>();
        rowVLG.childControlWidth = true; rowVLG.childForceExpandWidth = true;
        rowVLG.childControlHeight = true; rowVLG.childForceExpandHeight = false;
        rowVLG.spacing = 2;

        // Speaker label
        AddText(row.transform, speaker, 12, FontStyle.Bold, col, 20);

        // Message
        AddText(row.transform, message, 15, FontStyle.Normal, Color.white, 0);

        _lines.Add(row);
        StartCoroutine(ScrollBottom());
    }

    private void AddText(Transform parent, string body, int size, FontStyle style,
                         Color colour, int minHeight)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = body;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = colour;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        var csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        if (minHeight > 0) le.minHeight = minHeight;
    }

    private void ClearHistory()
    {
        foreach (var l in _lines) if (l != null) Destroy(l);
        _lines.Clear();
    }

    private IEnumerator ScrollBottom()
    {
        yield return new WaitForEndOfFrame();
        if (_scroll != null) _scroll.verticalNormalizedPosition = 0f;
    }

    private IEnumerator DelayClose(float t)
    {
        yield return new WaitForSeconds(t);
        CloseChat();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void RefreshHeader()
    {
        if (_npc == null) return;
        _npcNameText.text = _npc.GetNPCId();
        if (NPCMemoryManager.Instance != null)
        {
            NPCMemory mem = _npc.GetMemory();
            _relationText.text = mem != null
                ? NPCMemoryManager.Instance.GetRelationshipLevel(mem.relationshipScore)
                : "Unknown";
        }
    }

    private void FocusInput()
    {
        _input.ActivateInputField();
        _input.Select();
    }

    private void SetPlayerMovement(bool on)
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) pm.enabled = on;
        var pcc = FindFirstObjectByType<PlayerCombatController>();
        if (pcc != null) pcc.enabled = on;
    }

    private static bool Has(string src, params string[] kw)
    {
        foreach (var k in kw) if (src.Contains(k)) return true;
        return false;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI CONSTRUCTION — called once in Awake
    // ══════════════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // Find or create Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        Transform canvasT;
        if (canvas != null)
        {
            canvasT = canvas.transform;
        }
        else
        {
            var cgo = new GameObject("Canvas");
            var c   = cgo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            canvasT = cgo.transform;
        }

        // ── Root panel ────────────────────────────────────────────────────
        _panel = MakeImage(canvasT, "ChatPanel", new Color(0.08f, 0.09f, 0.12f, 0.97f));
        Anchors(_panel, 0.20f, 0.15f, 0.80f, 0.85f);

        // ── Header ────────────────────────────────────────────────────────
        var header = MakeImage(_panel.transform, "Header", new Color(0.13f, 0.15f, 0.23f, 1f));
        Anchors(header, 0f, 0.88f, 1f, 1f);

        _npcNameText = MakeLabel(header.transform, "NPCName", "NPC", 18, FontStyle.Bold,
            TextAnchor.MiddleLeft, Color.white);
        Anchors(_npcNameText.gameObject, 0f, 0f, 0.65f, 1f);
        _npcNameText.GetComponent<RectTransform>().offsetMin = new Vector2(12, 0);

        _relationText = MakeLabel(header.transform, "Relation", "Neutral", 13, FontStyle.Normal,
            TextAnchor.MiddleRight, new Color(1f, 0.82f, 0.25f));
        Anchors(_relationText.gameObject, 0.55f, 0f, 1f, 1f);
        _relationText.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);

        // ── Input row ─────────────────────────────────────────────────────
        var inputRow = MakeImage(_panel.transform, "InputRow", new Color(0.10f, 0.11f, 0.16f, 1f));
        Anchors(inputRow, 0f, 0f, 1f, 0.13f);

        var hlg = inputRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;  hlg.childForceExpandWidth  = false;
        hlg.childControlHeight     = true;  hlg.childForceExpandHeight = true;
        hlg.spacing = 6; hlg.padding = new RectOffset(8, 8, 6, 6);

        _input    = MakeInputField(inputRow.transform);
        _input.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        _sendBtn  = MakeBtn(inputRow.transform, "Send",  new Color(0.15f, 0.48f, 0.85f), 70);
        _leaveBtn = MakeBtn(inputRow.transform, "Leave", new Color(0.42f, 0.16f, 0.16f), 62);

        // ── Scroll area ───────────────────────────────────────────────────
        var scrollGO = MakeImage(_panel.transform, "Scroll", new Color(0.05f, 0.06f, 0.09f, 1f));
        Anchors(scrollGO, 0f, 0.13f, 1f, 0.88f);
        _scroll = scrollGO.AddComponent<ScrollRect>();

        var vp = MakeImage(scrollGO.transform, "Viewport", new Color(0,0,0,0));
        Anchors(vp, 0f, 0f, 1f, 1f);
        vp.AddComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vp.transform, false);
        _content            = contentGO.AddComponent<RectTransform>();
        _content.anchorMin  = new Vector2(0f, 1f);
        _content.anchorMax  = new Vector2(1f, 1f);
        _content.pivot      = new Vector2(0.5f, 1f);
        _content.sizeDelta  = Vector2.zero;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childForceExpandWidth  = true;
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
        vlg.spacing = 8; vlg.padding = new RectOffset(10,10,8,8);
        contentGO.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        _scroll.content          = _content;
        _scroll.viewport         = vp.GetComponent<RectTransform>();
        _scroll.horizontal       = false;
        _scroll.vertical         = true;
        _scroll.scrollSensitivity = 20f;
        _scroll.movementType     = ScrollRect.MovementType.Clamped;
    }

    // ── UI factory methods ────────────────────────────────────────────────────

    static GameObject MakeImage(Transform parent, string name, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = col;
        return go;
    }

    static Text MakeLabel(Transform parent, string name, string body, int size,
        FontStyle style, TextAnchor align, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t       = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = body; t.fontSize = size; t.fontStyle = style;
        t.alignment = align; t.color = col;
        return t;
    }

    static void Anchors(GameObject go, float ax, float ay, float bx, float by)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(bx, by);
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
    }

    static Button MakeBtn(Transform parent, string label, Color bg, float minW)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = bg;
        var btn  = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.highlightedColor = bg * 1.3f; cols.pressedColor = bg * 0.7f;
        btn.colors = cols;
        go.AddComponent<LayoutElement>().minWidth = minW;

        var lbl = new GameObject("T");
        lbl.transform.SetParent(go.transform, false);
        var rt       = lbl.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var t        = lbl.AddComponent<Text>();
        t.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text       = label; t.fontSize = 13; t.fontStyle = FontStyle.Bold;
        t.alignment  = TextAnchor.MiddleCenter; t.color = Color.white;
        return btn;
    }

    InputField MakeInputField(Transform parent)
    {
        var go = new GameObject("Input");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = new Color(0.16f, 0.17f, 0.22f, 1f);
        var field = go.AddComponent<InputField>();

        var ph = new GameObject("PH");
        ph.transform.SetParent(go.transform, false);
        var phRT = ph.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(8,2); phRT.offsetMax = new Vector2(-4,-2);
        var phT = ph.AddComponent<Text>();
        phT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phT.text = "Type a message..."; phT.fontSize = 13;
        phT.fontStyle = FontStyle.Italic;
        phT.color = new Color(0.55f,0.55f,0.60f);
        phT.alignment = TextAnchor.MiddleLeft;

        var tx = new GameObject("TX");
        tx.transform.SetParent(go.transform, false);
        var txRT = tx.AddComponent<RectTransform>();
        txRT.anchorMin = Vector2.zero; txRT.anchorMax = Vector2.one;
        txRT.offsetMin = new Vector2(8,2); txRT.offsetMax = new Vector2(-4,-2);
        var txT = tx.AddComponent<Text>();
        txT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txT.fontSize = 13; txT.color = Color.white;
        txT.alignment = TextAnchor.MiddleLeft;
        txT.supportRichText = false;

        field.placeholder   = phT;
        field.textComponent = txT;
        return field;
    }
}
