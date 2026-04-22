using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Builds the Chat Dialogue UI in one click.
/// Menu: Tools → Build Chat UI
/// </summary>
public static class ChatUIBuilder
{
    [MenuItem("Tools/Build Chat UI")]
    public static void Build()
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        Canvas existingCanvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasGO;

        if (existingCanvas != null)
        {
            canvasGO = existingCanvas.gameObject;
        }
        else
        {
            canvasGO = new GameObject("Canvas");
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        if (canvasGO.transform.Find("ChatPanel") != null)
        {
            Debug.LogWarning("[ChatUIBuilder] ChatPanel already exists — delete it first.");
            return;
        }

        var oldUI = canvasGO.GetComponent<ChatDialogueUI>();
        if (oldUI != null) Object.DestroyImmediate(oldUI);

        // ════════════════════════════════════════════════════════════════════
        // CHAT PANEL — anchored to centre 60% wide, 70% tall of screen
        // ════════════════════════════════════════════════════════════════════
        GameObject chatPanel = new GameObject("ChatPanel");
        chatPanel.transform.SetParent(canvasGO.transform, false);
        chatPanel.AddComponent<RectTransform>();
        chatPanel.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        SetAnchors(chatPanel, 0.20f, 0.15f, 0.80f, 0.85f, 0, 0);

        // ── HEADER ── top 12% of panel ────────────────────────────────────
        GameObject header = new GameObject("Header");
        header.transform.SetParent(chatPanel.transform, false);
        header.AddComponent<RectTransform>();
        header.AddComponent<Image>().color = new Color(0.13f, 0.15f, 0.23f, 1f);
        SetAnchors(header, 0f, 0.88f, 1f, 1f, 0, 0);

        Text npcNameText = MakeText(header.transform, "NPCNameText",
            "NPC", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        npcNameText.color = Color.white;
        SetAnchors(npcNameText.gameObject, 0f, 0f, 0.65f, 1f, 12, -4);

        Text relationText = MakeText(header.transform, "RelationText",
            "Neutral", 13, FontStyle.Normal, TextAnchor.MiddleRight);
        relationText.color = new Color(1f, 0.82f, 0.25f);
        SetAnchors(relationText.gameObject, 0.65f, 0f, 1f, 1f, 0, -8);

        // ── INPUT ROW ── bottom 12% of panel ─────────────────────────────
        GameObject inputRow = new GameObject("InputRow");
        inputRow.transform.SetParent(chatPanel.transform, false);
        inputRow.AddComponent<RectTransform>();
        inputRow.AddComponent<Image>().color = new Color(0.10f, 0.11f, 0.16f, 1f);
        SetAnchors(inputRow, 0f, 0f, 1f, 0.12f, 0, 0);

        var hlg = inputRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;
        hlg.childForceExpandWidth  = false;
        hlg.childControlHeight     = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 6;
        hlg.padding = new RectOffset(8, 8, 6, 6);

        InputField inputField = MakeInputField(inputRow.transform, "PlayerInput");
        inputField.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        Button sendBtn = MakeButton(inputRow.transform, "SendButton", "Send",
            new Color(0.15f, 0.48f, 0.85f));
        sendBtn.gameObject.AddComponent<LayoutElement>().minWidth = 70;

        Button leaveBtn = MakeButton(inputRow.transform, "LeaveButton", "Leave",
            new Color(0.42f, 0.16f, 0.16f));
        leaveBtn.gameObject.AddComponent<LayoutElement>().minWidth = 62;

        // ── SCROLL AREA ── middle 76% of panel ───────────────────────────
        GameObject scrollGO = new GameObject("ChatScroll");
        scrollGO.transform.SetParent(chatPanel.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 1f);
        SetAnchors(scrollGO, 0f, 0.12f, 1f, 0.88f, 0, 0);

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        viewport.AddComponent<RectTransform>();
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        SetAnchors(viewport, 0f, 0f, 1f, 1f, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT       = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0, 0);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childForceExpandWidth  = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8;
        vlg.padding = new RectOffset(10, 10, 8, 8);
        content.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content          = contentRT;
        scrollRect.viewport         = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal       = false;
        scrollRect.vertical         = true;
        scrollRect.scrollSensitivity = 20f;
        scrollRect.movementType     = ScrollRect.MovementType.Clamped;

        // ── Wire ─────────────────────────────────────────────────────────
        ChatDialogueUI ui = canvasGO.AddComponent<ChatDialogueUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("chatPanel").objectReferenceValue    = chatPanel;
        so.FindProperty("npcNameText").objectReferenceValue  = npcNameText;
        so.FindProperty("relationText").objectReferenceValue = relationText;
        so.FindProperty("chatScroll").objectReferenceValue   = scrollRect;
        so.FindProperty("chatContent").objectReferenceValue  = contentRT;
        so.FindProperty("playerInput").objectReferenceValue  = inputField;
        so.FindProperty("sendButton").objectReferenceValue   = sendBtn;
        so.FindProperty("leaveButton").objectReferenceValue  = leaveBtn;
        so.ApplyModifiedProperties();

        chatPanel.SetActive(false);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[ChatUIBuilder] Done — save your scene (Ctrl+S).");
        Selection.activeGameObject = chatPanel;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// anchorMin(ax,ay) → anchorMax(bx,by), with offsetMin=(ox,oy)
    static void SetAnchors(GameObject go,
        float ax, float ay, float bx, float by,
        float offsetLeft = 0, float offsetRight = 0)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = new Vector2(offsetLeft,  0);
        rt.offsetMax = new Vector2(-offsetRight, 0);
    }

    static Text MakeText(Transform parent, string name, string body,
        int size, FontStyle style, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t       = go.AddComponent<Text>();
        t.text      = body;
        t.fontSize  = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color     = Color.white;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = bg;
        var btn  = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.highlightedColor = bg * 1.3f;
        cols.pressedColor     = bg * 0.7f;
        btn.colors = cols;

        var lbl = new GameObject("Text");
        lbl.transform.SetParent(go.transform, false);
        var rt       = lbl.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var t        = lbl.AddComponent<Text>();
        t.text       = label;
        t.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize   = 13;
        t.fontStyle  = FontStyle.Bold;
        t.alignment  = TextAnchor.MiddleCenter;
        t.color      = Color.white;
        return btn;
    }

    static InputField MakeInputField(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = new Color(0.16f, 0.17f, 0.22f, 1f);
        var field = go.AddComponent<InputField>();

        var ph = MakeInnerText(go.transform, "Placeholder",
            "Type a message...", 13, FontStyle.Italic, new Color(0.55f, 0.55f, 0.60f));
        var txt = MakeInnerText(go.transform, "Text",
            "", 13, FontStyle.Normal, Color.white);
        txt.GetComponent<Text>().supportRichText = false;

        field.placeholder   = ph.GetComponent<Text>();
        field.textComponent = txt.GetComponent<Text>();
        return field;
    }

    static GameObject MakeInnerText(Transform parent, string name, string body,
        int size, FontStyle style, Color colour)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8, 2); rt.offsetMax = new Vector2(-4, -2);
        var t        = go.AddComponent<Text>();
        t.text       = body;
        t.fontSize   = size;
        t.fontStyle  = style;
        t.color      = colour;
        t.alignment  = TextAnchor.MiddleLeft;
        t.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }
}
